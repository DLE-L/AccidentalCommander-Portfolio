using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    internal sealed class Build1ExplosionReadyRuntime
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly SynergyDamageData _ready;
        private readonly List<MonsterController> _targets = new List<MonsterController>(6);
        private int _killCount;

        internal Build1ExplosionReadyRuntime(
            RuntimeObjectRegistry registry,
            ICombatImmediateHitModule immediateHits,
            SynergyDamageData ready)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _ready = ready ?? throw new ArgumentNullException(nameof(ready));
        }

        internal void ReportKill(in CountableKillAttribution attribution)
        {
            if (attribution.IsCountable == false)
                return;

            _killCount++;
            if (_killCount == 1 || _killCount == 6 || _killCount == _ready.TriggerThreshold)
            {
                Build1RuntimeDiagnostics.Log("synergy_ready_progress",
                    Build1RuntimeDiagnostics.Text("synergy_id", _ready.SynergyId),
                    Build1RuntimeDiagnostics.Int("progress", _killCount),
                    Build1RuntimeDiagnostics.Int("threshold", _ready.TriggerThreshold));
            }

            while (_killCount >= _ready.TriggerThreshold)
            {
                _killCount -= _ready.TriggerThreshold;
                Resolve(attribution.LethalPosition);
            }
        }

        internal void Reset()
        {
            _killCount = 0;
            _targets.Clear();
        }

        private void Resolve(Vector3 origin)
        {
            bool presentationPlayed = RetroVfx.Spawn(RetroVfxKind.BlastStaffExplosion, origin);
            _targets.Clear();
            float radiusSquared = _ready.Radius * _ready.Radius;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.Hp <= 0 || target.SpawnSequence <= 0L)
                    continue;
                if ((target.transform.position - origin).sqrMagnitude > radiusSquared)
                    continue;
                InsertTarget(target, origin);
            }

            int appliedTargetCount = 0;
            CountableKillAttribution attribution = new CountableKillAttribution(
                0,
                _ready.SynergyId,
                CombatKillSourceCategory.SynergyAction);
            for (int index = 0; index < _targets.Count; index++)
            {
                MonsterController target = _targets[index];
                int damage = Mathf.RoundToInt(_ready.BaseValue);
                if (target.IsBoss)
                    damage = Mathf.Max(1, Mathf.Min(damage, Mathf.FloorToInt(target.MaxHp * _ready.BossMaxHpPercent)));

                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    _ready.SynergyId,
                    target,
                    origin,
                    target.transform.position,
                    damage,
                    AttackVisualKind.SingleHit,
                    false,
                    attribution,
                    _ready.Id)))
                {
                    appliedTargetCount++;
                }
            }

            Build1RuntimeDiagnostics.Log("synergy_ready_effect",
                Build1RuntimeDiagnostics.Text("synergy_id", _ready.SynergyId),
                Build1RuntimeDiagnostics.Float("position_x", origin.x),
                Build1RuntimeDiagnostics.Float("position_y", origin.y),
                Build1RuntimeDiagnostics.Int("configured_damage", Mathf.RoundToInt(_ready.BaseValue)),
                Build1RuntimeDiagnostics.Float("radius", _ready.Radius),
                Build1RuntimeDiagnostics.Int("max_targets", _ready.MaxTargets),
                Build1RuntimeDiagnostics.Int("actual_target_count", appliedTargetCount),
                Build1RuntimeDiagnostics.Bool("presentation_played", presentationPlayed),
                Build1RuntimeDiagnostics.Bool("countable_attribution", attribution.IsCountable));
        }

        private void InsertTarget(MonsterController candidate, Vector3 origin)
        {
            float distance = (candidate.transform.position - origin).sqrMagnitude;
            int index = 0;
            while (index < _targets.Count)
            {
                MonsterController existing = _targets[index];
                float existingDistance = (existing.transform.position - origin).sqrMagnitude;
                if (distance < existingDistance
                    || (Mathf.Approximately(distance, existingDistance) && candidate.SpawnSequence < existing.SpawnSequence))
                {
                    break;
                }
                index++;
            }

            if (index >= _ready.MaxTargets)
                return;
            _targets.Insert(index, candidate);
            if (_targets.Count > _ready.MaxTargets)
                _targets.RemoveAt(_ready.MaxTargets);
        }
    }

}
