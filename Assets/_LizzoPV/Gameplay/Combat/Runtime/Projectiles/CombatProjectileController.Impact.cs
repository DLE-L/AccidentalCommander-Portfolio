using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public sealed partial class CombatProjectileController
    {
        private bool TryHitCommander(Vector2 segmentStart)
        {
            CommanderActor player = _registry?.Player;
            if (_hitCollider == null || player == null || !player.isActiveAndEnabled || player.Hp <= 0)
                return false;
            Vector2 segmentEnd = _hitCollider.transform.TransformPoint(_hitCollider.offset);
            Vector3 scale = _hitCollider.transform.lossyScale;
            float radius = _hitCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            if (!player.IsHurtboxOverlappingCapsule(segmentStart, segmentEnd, radius))
                return false;

            ICombatImmediateHitModule module = _immediateHits;
            if (module == null)
                Debug.LogError("Enemy projectile requires CombatImmediateHitModule.", this);
            else
                module.TryApply(CombatImmediateHitRequest.CreateEnemyContact(
                    ((EnemyActor)_request.Source).GetDamageEnemyId(), player, segmentEnd, _direction,
                    _request.Damage, _request.SourceId, _request.StraightHitFeedback, source: _request.Source));
            // Collision consumes the shot even when commander invulnerability rejects its damage.
            Release();
            return true;
        }

        public bool TryHit(EnemyActor target)
        {
            if (_initialized == false || _released || RunPauseController.IsResultGameplayLocked || target == null || target.IsValid() == false)
                return false;

            if (_immediateHits == null)
            {
                Debug.LogError("Projectile requires its run CombatImmediateHitModule binding.", this);
                Release();
                return false;
            }

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
                Vector3 origin = _request.Source == null ? _request.Origin : _request.Source.transform.position;
                Vector3 feedbackPosition = _request.KillAttribution.IsAttributable
                    ? target.transform.position : CombatTargeting.ResolveTargetPoint(target, origin);
                _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    _request.SourceId, target, origin, feedbackPosition, _request.Damage,
                    _request.HomingHitFeedback, true, _request.KillAttribution, effectId: _request.PresentationId, statusPayload: _request.StatusPayload));
            }
            else
            {
                ApplyStraightDamage(target, transform.position);

                RegisterHitTarget(target);
                if (_request.MaxDistinctTargetHits == 0 || _distinctTargetHitCount < _request.MaxDistinctTargetHits)
                    return true;
            }

            Release();
            return true;
        }

        private void ResolveImpact(EnemyActor directTarget)
        {
            Vector3 impactPoint = transform.position;
            _impactTargetSelector.Begin(impactPoint, _request.ImpactRadius, _request.ImpactMaxTargets);
            ConsiderImpactTarget(directTarget);

            if (_registry?.Enemies != null)
            {
                foreach (EnemyActor candidate in _registry.Enemies)
                    ConsiderImpactTarget(candidate);
            }

            for (int i = 0; i < _impactTargetSelector.Count; i++)
                ApplyStraightDamage(_impactTargetSelector.GetTarget(i), impactPoint);
        }

        private void ConsiderImpactTarget(EnemyActor target)
        {
            _impactTargetSelector.Consider(new CombatProjectileImpactTargetCandidate(
                target,
                target == null ? Vector3.zero : target.transform.position,
                target == null ? 0L : target.SpawnSequence,
                target != null && target.IsValid() && target.Hp > 0));
        }

        private void ApplyStraightDamage(EnemyActor target, Vector3 impactPoint)
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(
                _request.Damage * (1.0f + _request.PenetrationDamageStep * _distinctTargetHitCount)));
            string sourceId = _request.KillAttribution.IsAttributable
                ? _request.SourceId
                : _request.Source is CommanderActor ? CombatIds.Commander : CombatIds.Projectile;
            _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                sourceId, target, impactPoint, impactPoint, damage,
                AttackVisualKind.SingleHit, false, _request.KillAttribution, effectId: _request.PresentationId, statusPayload: _request.StatusPayload));
            RetroVfx.Spawn(_request.StraightHitFeedback, impactPoint, _direction, 1.0f);
        }

        private bool HasHitTarget(EnemyActor target) => _hitTargets.Contains((target.GetInstanceID(), target.SpawnSequence));
        private void RegisterHitTarget(EnemyActor target)
        {
            if (_hitTargets.Add((target.GetInstanceID(), target.SpawnSequence))) _distinctTargetHitCount++;
        }
        private void ClearHitTargets()
        {
            _hitTargets.Clear();
            _distinctTargetHitCount = 0;
        }
    }
}
