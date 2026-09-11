using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander
{
    public sealed class CommanderDamageReceiver
    {
        readonly CommanderActor _owner;
        readonly Dictionary<string, float> _nextDamageTimeBySource = new Dictionary<string, float>();

        bool _loggedLowHp30;
        bool _loggedLowHp10;
        float _invulnerableUntil;
        public bool IsInvulnerable(float time) => time < _invulnerableUntil;
#if UNITY_EDITOR
        bool _editorAutomationInfiniteHp;
#endif

        public CommanderDamageReceiver(CommanderActor owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public bool TryApply(EnemyActor monster, int damage, string overridePatternId = null)
        {
            if (damage <= 0 || _owner.Hp <= 0)
                return false;

            string enemyId = monster == null ? CombatIds.Unknown : monster.GetDamageEnemyId();
            string patternId = string.IsNullOrEmpty(overridePatternId)
                ? monster == null ? CombatIds.Unknown : monster.GetDamagePatternId()
                : overridePatternId;
            string sourceKey = ResolveDamageSourceKey(monster, patternId);

            if (monster != null)
            {
                RunDiagnostics.RecordCommanderHurtboxContact(enemyId, patternId);
                RunTelemetry.Log(RunTelemetry.HurtboxContact, "target=commander", $"enemy_id={enemyId}", $"pattern_id={patternId}");
            }

            if (_nextDamageTimeBySource.TryGetValue(sourceKey, out float nextSourceDamageTime)
                && Time.time < nextSourceDamageTime)
            {
                if (CombatIds.IsBossPattern(patternId))
                {
                    RunTelemetry.Log(
                        RunTelemetry.BossPatternRepeatBlock,
                        "target=commander",
                        $"enemy_id={enemyId}",
                        $"pattern_id={patternId}",
                        $"remaining={nextSourceDamageTime - Time.time:0.##}");
                }

                RunTelemetry.Log(
                    RunTelemetry.DamageBlockedCooldown,
                    "target=commander",
                    $"enemy_id={enemyId}",
                    $"pattern_id={patternId}",
                    $"remaining={nextSourceDamageTime - Time.time:0.##}");
                return false;
            }

            if (Time.time < _invulnerableUntil)
            {
                RunTelemetry.Log(
                    RunTelemetry.DamageBlockedInvulnerable,
                    "target=commander",
                    $"enemy_id={enemyId}",
                    $"pattern_id={patternId}",
                    $"remaining={_invulnerableUntil - Time.time:0.##}");
                return false;
            }

            if (monster != null)
                damage = monster.ResolveCommanderIncomingDamage(damage, Time.time);

            if (monster != null)
                RunDeathReasonTracker.RecordEnemyDamage(monster, patternId);

            RunContext context = _owner.Context;
            TutorialDamageResolution tutorialDamage = TutorialDefeatProtection.Resolve(
                context,
                _owner.Hp,
                _owner.MaxHp,
                damage);
            int appliedDamage = tutorialDamage.AppliedDamage;
#if UNITY_EDITOR
            if (_editorAutomationInfiniteHp)
                appliedDamage = Mathf.Min(damage, Mathf.Max(0, _owner.Hp - 1));
#endif
            if (appliedDamage <= 0)
            {
                if (tutorialDamage.PreventedDefeat)
                    _owner.RestoreHealth(Mathf.Clamp(tutorialDamage.RecoveryHp, 1, _owner.MaxHp));
                return false;
            }

            int hpBefore = _owner.Hp;
            _owner.ApplyDamageFromReceiver(monster, appliedDamage);
            int actualDamage = Mathf.Max(0, hpBefore - _owner.Hp);
#if UNITY_EDITOR
            if (_editorAutomationInfiniteHp && _owner.MaxHp > 0)
                _owner.RestoreHealth(_owner.MaxHp);
            else if (tutorialDamage.PreventedDefeat)
                _owner.RestoreHealth(Mathf.Clamp(tutorialDamage.RecoveryHp, 1, _owner.MaxHp));
#else
            if (tutorialDamage.PreventedDefeat)
                _owner.RestoreHealth(Mathf.Clamp(tutorialDamage.RecoveryHp, 1, _owner.MaxHp));
#endif

            if (actualDamage <= 0)
                return false;

            RunDiagnostics.RecordCommanderDamage(enemyId, patternId, actualDamage, GetHpPercent());
            RunTelemetry.Log(RunTelemetry.CommanderDamage, $"damage={actualDamage}", $"enemy_id={enemyId}", $"pattern_id={patternId}", $"hp_percent={GetHpPercent()}");
            RunTelemetry.Log(RunTelemetry.DamageApply, "target=commander", $"damage={actualDamage}", $"enemy_id={enemyId}", $"pattern_id={patternId}");
            if (enemyId == CombatIds.RedCharger && patternId == CombatIds.RedChargerImpactGrace)
                RunTelemetry.Log(RunTelemetry.RedChargerImpactGraceHit, $"damage={actualDamage}", $"hp_percent={GetHpPercent()}");
            else if (enemyId == CombatIds.RedCharger && patternId == CombatIds.RedChargerDash)
                RunTelemetry.Log(RunTelemetry.RedChargerImpactHit, $"damage={actualDamage}", $"pattern_id={patternId}", $"hp_percent={GetHpPercent()}");
            if (CombatIds.IsBossPattern(patternId))
                RunTelemetry.Log(RunTelemetry.BossPatternHit, "target=commander", $"pattern_id={patternId}", $"damage={actualDamage}");
            if (monster != null)
                RunDiagnostics.RecordEnemyContactDamage(monster);

            if (monster != null)
            {
                _invulnerableUntil = Time.time + _owner.Tuning.CommanderPostHitInvuln;
                _nextDamageTimeBySource[sourceKey] = Time.time + _owner.Tuning.ContactDamageSourceCooldown;
                RunTelemetry.Log(RunTelemetry.CommanderInvulnStart, $"duration={_owner.Tuning.CommanderPostHitInvuln:0.##}");
            }

            LogCommanderLowHp();
            _owner.NotifyDamageApplied(actualDamage);
            return true;
        }

        public void ResetForSpawn()
        {
            ResetLowHpWarnings();
            _invulnerableUntil = 0.0f;
            _nextDamageTimeBySource.Clear();
#if UNITY_EDITOR
            _editorAutomationInfiniteHp = false;
#endif
        }

        public void ResetLowHpWarnings()
        {
            _loggedLowHp30 = false;
            _loggedLowHp10 = false;
        }

#if UNITY_EDITOR
        public void SetEditorAutomationInfiniteHp(bool enabled)
        {
            _editorAutomationInfiniteHp = enabled;
            if (enabled && _owner.MaxHp > 0)
                _owner.RestoreHealth(_owner.MaxHp);
        }

        public bool EditorAutomationInfiniteHpEnabled => _editorAutomationInfiniteHp;
#endif

        string ResolveDamageSourceKey(EnemyActor monster, string patternId)
        {
            if (monster == null)
                return CombatIds.Unknown;

            if (string.IsNullOrEmpty(patternId))
                return monster.GetDamageSourceKey();

            return CombatIds.DamageCooldownKey(monster.GetDamageEnemyId(), monster.GetInstanceID(), patternId);
        }

        int GetHpPercent()
        {
            if (_owner.MaxHp <= 0)
                return 0;

            return Mathf.Clamp(Mathf.RoundToInt((float)_owner.Hp / _owner.MaxHp * 100.0f), 0, 100);
        }

        void LogCommanderLowHp()
        {
            if (_owner.MaxHp <= 0 || _owner.Hp <= 0)
                return;

            int hpPercent = Mathf.CeilToInt((float)_owner.Hp / _owner.MaxHp * 100.0f);
            if (_loggedLowHp30 == false && hpPercent <= 30)
            {
                _loggedLowHp30 = true;
                RunTelemetry.Log(RunTelemetry.CommanderLowHp, "threshold=30", $"hp_percent={hpPercent}");
            }

            if (_loggedLowHp10 == false && hpPercent <= 10)
            {
                _loggedLowHp10 = true;
                RunTelemetry.Log(RunTelemetry.CommanderLowHp, "threshold=10", $"hp_percent={hpPercent}");
            }
        }
    }
}
