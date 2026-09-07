using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion.Combat;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public sealed class RunCombatTelemetry : IDisposable
    {
        private sealed class CombatTotals
        {
            public int Attacks;
            public int Skills;
            public int Resolutions;
            public int Hits;
            public int Damage;
            public int Kills;
        }

        private sealed class SynergyTotals
        {
            public SynergyTier Tier;
            public int Activations;
            public int Started;
            public int Completed;
            public int Hits;
            public int Damage;
            public int Kills;
        }

        private readonly CanonicalCompanionCastStream _casts;
        private readonly Dictionary<string, CombatTotals> _combatBySource = new Dictionary<string, CombatTotals>(StringComparer.Ordinal);
        private readonly Dictionary<string, SynergyTotals> _synergyById = new Dictionary<string, SynergyTotals>(StringComparer.Ordinal);
        private readonly List<string> _keys = new List<string>(32);
        private bool _summaryLogged;
        private bool _disposed;

        public RunCombatTelemetry(CanonicalCompanionCastStream casts)
        {
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            _casts.Completed += OnCastCompleted;
        }

        public void Reset()
        {
            _combatBySource.Clear();
            _synergyById.Clear();
            _keys.Clear();
            _summaryLogged = false;
        }

        public void RecordHit(
            global::MonsterController target,
            string sourceId,
            CombatKillSourceCategory sourceCategory,
            int damage,
            bool willKill)
        {
            if (_disposed || target == null || damage <= 0)
                return;

            sourceId = Normalize(sourceId);
            var stats = target.RuntimeStats;
            string enemyId = stats?.Data == null ? "unknown" : Normalize(stats.Data.Id);
            if (sourceCategory == CombatKillSourceCategory.SynergyAction)
            {
                SynergyTotals synergyTotals = GetSynergyTotals(sourceId, default);
                synergyTotals.Hits++;
                synergyTotals.Damage += damage;
                if (willKill)
                    synergyTotals.Kills++;
                RunTelemetry.Log(
                    RunTelemetry.SynergyHitApplied,
                    $"synergy_id={sourceId}",
                    $"target_id={enemyId}",
                    $"target_instance_id={target.GetInstanceID()}",
                    $"damage={damage}",
                    $"will_kill={willKill.ToString().ToLowerInvariant()}");
                return;
            }

            if (sourceCategory != CombatKillSourceCategory.CompanionOwnedAction)
                return;

            CombatTotals totals = GetCombatTotals(sourceId);
            totals.Hits++;
            totals.Damage += damage;
            if (willKill)
                totals.Kills++;
            RunTelemetry.Log(
                RunTelemetry.CompanionHitApplied,
                $"source_id={sourceId}",
                $"target_id={enemyId}",
                $"target_instance_id={target.GetInstanceID()}",
                $"damage={damage}",
                $"will_kill={willKill.ToString().ToLowerInvariant()}",
                $"is_elite={target.IsElite.ToString().ToLowerInvariant()}",
                $"is_boss={target.IsBoss.ToString().ToLowerInvariant()}");
        }

        public void RecordSynergyActivated(in SynergyStateSnapshot synergy)
        {
            if (_disposed)
                return;

            SynergyTotals totals = GetSynergyTotals(synergy.SynergyId, synergy.Tier);
            totals.Activations++;
            RunTelemetry.Log(
                RunTelemetry.SynergyActivated,
                $"synergy_id={synergy.SynergyId}",
                $"tier={(int)synergy.Tier}",
                $"caster_unit_id={synergy.CasterUnitId}",
                $"presentation={synergy.Presentation.ToString().ToLowerInvariant()}");
        }

        public void RecordSynergyExecutionStarted(string synergyId, SynergyTier tier, long executionId, string casterUnitId, int stepCount)
        {
            if (_disposed)
                return;

            GetSynergyTotals(synergyId, tier).Started++;
            RunTelemetry.Log(
                RunTelemetry.SynergyExecutionStarted,
                $"synergy_id={Normalize(synergyId)}",
                $"tier={(int)tier}",
                $"execution_id={executionId}",
                $"caster_unit_id={Normalize(casterUnitId)}",
                $"effect_step_count={stepCount}");
        }

        public void RecordSynergyExecutionCompleted(string synergyId, SynergyTier tier, long executionId, int stepCount)
        {
            if (_disposed)
                return;

            GetSynergyTotals(synergyId, tier).Completed++;
            RunTelemetry.Log(
                RunTelemetry.SynergyExecutionCompleted,
                $"synergy_id={Normalize(synergyId)}",
                $"tier={(int)tier}",
                $"execution_id={executionId}",
                $"effect_step_count={stepCount}");
        }

        public void LogSummary(string reason)
        {
            if (_disposed || _summaryLogged)
                return;

            _summaryLogged = true;
            if (_combatBySource.Count == 0)
            {
                RunTelemetry.Log(RunTelemetry.CompanionCombatSummary, $"reason={Normalize(reason)}", "source_id=none", "attacks=0", "skills=0", "resolutions=0", "hits=0", "damage=0", "kills=0");
            }
            else
            {
                CopySortedKeys(_combatBySource);
                for (int index = 0; index < _keys.Count; index++)
                {
                    string sourceId = _keys[index];
                    CombatTotals totals = _combatBySource[sourceId];
                    RunTelemetry.Log(
                        RunTelemetry.CompanionCombatSummary,
                        $"reason={Normalize(reason)}",
                        $"source_id={sourceId}",
                        $"attacks={totals.Attacks}",
                        $"skills={totals.Skills}",
                        $"resolutions={totals.Resolutions}",
                        $"hits={totals.Hits}",
                        $"damage={totals.Damage}",
                        $"kills={totals.Kills}");
                }
            }

            if (_synergyById.Count == 0)
            {
                RunTelemetry.Log(RunTelemetry.SynergyCombatSummary, $"reason={Normalize(reason)}", "synergy_id=none", "activated=0", "started=0", "completed=0", "hits=0", "damage=0", "kills=0");
                return;
            }

            CopySortedKeys(_synergyById);
            for (int index = 0; index < _keys.Count; index++)
            {
                string synergyId = _keys[index];
                SynergyTotals totals = _synergyById[synergyId];
                RunTelemetry.Log(
                    RunTelemetry.SynergyCombatSummary,
                    $"reason={Normalize(reason)}",
                    $"synergy_id={synergyId}",
                    $"tier={(int)totals.Tier}",
                    $"activated={totals.Activations}",
                    $"started={totals.Started}",
                    $"completed={totals.Completed}",
                    $"hits={totals.Hits}",
                    $"damage={totals.Damage}",
                    $"kills={totals.Kills}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _casts.Completed -= OnCastCompleted;
            _combatBySource.Clear();
            _synergyById.Clear();
            _keys.Clear();
        }

        private void OnCastCompleted(CanonicalCompanionCastCompleted cast)
        {
            if (_disposed)
                return;

            string sourceId = Normalize(cast.BaseUnitId);
            CombatTotals totals = GetCombatTotals(sourceId);
            switch (cast.ActionKind)
            {
                case CanonicalCompanionActionKind.BasicAttack:
                    totals.Attacks++;
                    break;
                case CanonicalCompanionActionKind.ActiveSkill:
                    totals.Skills++;
                    break;
                case CanonicalCompanionActionKind.ReturningLightResolved:
                case CanonicalCompanionActionKind.ReturningAttackResolved:
                    totals.Resolutions++;
                    break;
            }
            RunTelemetry.Log(
                RunTelemetry.CompanionAttackCompleted,
                $"cast_id={cast.CastId}",
                $"source_id={sourceId}",
                $"roster_slot_id={Normalize(cast.RosterSlotId)}",
                $"attack_id={Normalize(cast.AttackId)}",
                $"action_kind={cast.ActionKind.ToString().ToLowerInvariant()}");
        }

        private CombatTotals GetCombatTotals(string sourceId)
        {
            if (!_combatBySource.TryGetValue(sourceId, out CombatTotals totals))
            {
                totals = new CombatTotals();
                _combatBySource.Add(sourceId, totals);
            }
            return totals;
        }

        private SynergyTotals GetSynergyTotals(string synergyId, SynergyTier tier)
        {
            synergyId = Normalize(synergyId);
            if (!_synergyById.TryGetValue(synergyId, out SynergyTotals totals))
            {
                totals = new SynergyTotals { Tier = tier };
                _synergyById.Add(synergyId, totals);
            }
            else if ((int)tier > 0)
            {
                totals.Tier = tier;
            }
            return totals;
        }

        private void CopySortedKeys<T>(Dictionary<string, T> source)
        {
            _keys.Clear();
            foreach (string key in source.Keys)
                _keys.Add(key);
            _keys.Sort(StringComparer.Ordinal);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        }
    }
}
