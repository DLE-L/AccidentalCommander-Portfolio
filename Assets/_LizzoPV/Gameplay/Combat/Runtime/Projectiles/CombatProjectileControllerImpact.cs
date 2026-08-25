using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public sealed partial class CombatProjectileController
    {
        public bool TryHit(MonsterController target)
        {
            if (_initialized == false || _released || RunPauseController.IsResultGameplayLocked || target == null || target.IsValid() == false)
                return false;

            if (_request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget && target != _request.Target)
                return false;

            if (_request.Faction != CombatProjectileFaction.Ally)
                return false;

            if (_request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision && HasHitTarget(target))
                return false;

            if (_request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision && _request.HasImpactArea)
            {
                ResolveImpact(target);
                Release();
                return true;
            }

            if (_request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget)
            {
                if (_request.KillAttribution.IsAttributable)
                {
                    ICombatImmediateHitModule module = target.Services?.ImmediateHitModule;
                    if (module == null)
                    {
                        Release();
                        return false;
                    }
                    CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateAllyDirectTarget(
                        _request.SourceId,
                        target,
                        _request.Source == null ? _request.Origin : _request.Source.transform.position,
                        target.transform.position,
                        _request.Damage,
                        _request.HomingHitFeedback,
                        spawnFeedback: true,
                        killAttribution: _request.KillAttribution);
                    module.TryApply(request);
                }
                else
                {
                    AllyAttackExecutor.ApplyDamageToTarget(
                        target,
                        _request.Source == null ? _request.Origin : _request.Source.transform.position,
                        _request.Damage,
                        _request.HomingHitFeedback,
                        spawnHitVisual: true,
                        _request.SourceId);
                }
            }
            else
            {
                ApplyStraightDamage(target, transform.position);

                RegisterHitTarget(target);
                if (_distinctTargetHitCount < _request.MaxDistinctTargetHits)
                    return true;
            }

            Release();
            return true;
        }

        private void ResolveImpact(MonsterController directTarget)
        {
            Vector3 impactPoint = transform.position;
            _impactTargetSelector.Begin(impactPoint, _request.ImpactRadius, _request.ImpactMaxTargets);
            ConsiderImpactTarget(directTarget);

            if (_registry?.Enemies != null)
            {
                foreach (MonsterController candidate in _registry.Enemies)
                    ConsiderImpactTarget(candidate);
            }

            for (int i = 0; i < _impactTargetSelector.Count; i++)
                ApplyStraightDamage(_impactTargetSelector.GetTarget(i), impactPoint);
        }

        private void ConsiderImpactTarget(MonsterController target)
        {
            _impactTargetSelector.Consider(new CombatProjectileImpactTargetCandidate(
                target,
                target == null ? Vector3.zero : target.transform.position,
                target == null ? 0L : target.SpawnSequence,
                target != null && target.IsValid() && target.Hp > 0));
        }

        private void ApplyStraightDamage(MonsterController target, Vector3 impactPoint)
        {
            int beforeHp = target.Hp;
            if (_request.Source is PlayerController)
                P0BossDpsTracker.RecordBossDamage(CombatIds.Commander, target, _request.Damage);

            target.OnDamagedFromPosition(impactPoint, _request.Damage, _request.Source is PlayerController ? CombatIds.Commander : CombatIds.Projectile);
            RetroVfx.Spawn(_request.StraightHitFeedback, impactPoint, _direction, 1.0f);
            if (_request.Source is PlayerController)
                Lizzo.PV.P0.Units.CommanderAttack.DebugRecordProjectileHit(beforeHp > 0 && target.Hp <= 0);
        }

        private bool HasHitTarget(MonsterController target)
        {
            for (int i = 0; i < _distinctTargetHitCount; i++)
            {
                if (_hitTargets[i] == target)
                    return true;
            }

            return false;
        }

        private void RegisterHitTarget(MonsterController target)
        {
            if (_distinctTargetHitCount >= _hitTargets.Length)
                return;

            _hitTargets[_distinctTargetHitCount] = target;
            _distinctTargetHitCount++;
        }

        private void ClearHitTargets()
        {
            for (int i = 0; i < _distinctTargetHitCount; i++)
                _hitTargets[i] = null;

            _distinctTargetHitCount = 0;
        }
    }
}
