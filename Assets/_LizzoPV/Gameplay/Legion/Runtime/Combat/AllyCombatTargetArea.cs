using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        internal MonsterController FindNearestTargetAreaCastTarget()
        {
            if (_targetRule == Lizzo.PV.Data.CombatTargetRule.DensestCluster)
            {
                List<TargetAreaImpactCandidate> candidates = this.CollectPrimaryTargetCandidates(_range);
                if (CompanionPrimaryTargetSelector.TrySelectDensestCluster(
                        candidates,
                        transform.position,
                        _range,
                        TargetAreaRadius,
                        out TargetAreaImpactCandidate selected))
                {
                    return selected.Target;
                }
            }

            MonsterController nearest = null;
            float nearestSqrDistance = _range * _range;
            int nearestId = int.MaxValue;

            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster.IsValid() == false || monster.gameObject == gameObject)
                    continue;

                float sqrDistance = this.GetSqrDistanceToTarget(monster);
                int instanceId = monster.GetInstanceID();
                if (sqrDistance > nearestSqrDistance
                    || (Mathf.Approximately(sqrDistance, nearestSqrDistance) && instanceId >= nearestId))
                {
                    continue;
                }

                nearest = monster;
                nearestSqrDistance = sqrDistance;
                nearestId = instanceId;
            }

            return nearest;
        }

        internal List<TargetAreaImpactCandidate> CollectTargetAreaImpactTargets(Vector3 impactPoint)
        {
            List<TargetAreaImpactCandidate> candidates = _targetAreaCandidates;
            candidates.Clear();
            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster.IsValid() == false || monster.gameObject == gameObject)
                    continue;

                candidates.Add(new TargetAreaImpactCandidate(
                    monster,
                    AllyTargeting.ResolveTargetPoint(monster, impactPoint),
                    monster.GetInstanceID()));
            }

            TargetAreaImpactCollector.Collect(
                candidates,
                impactPoint,
                TargetAreaRadius,
                TargetAreaMaxTargets,
                _targetAreaImpactTargets);
            return _targetAreaImpactTargets;
        }

        internal bool TryApplyTargetAreaPush(TargetAreaPushRequest request)
        {
            if (request.IsRequested == false
                || request.TargetClass != TargetAreaImpactTargetClass.Normal
                || request.Target.IsValid() == false)
                return false;

            int targetId = request.Target.GetInstanceID();
            if (NextKnockbackAllowedTimeByTarget.TryGetValue(targetId, out float nextAllowedTime)
                && Time.time < nextAllowedTime)
                return false;

            NextKnockbackAllowedTimeByTarget[targetId] = Time.time + KNOCKBACK_INTERNAL_COOLDOWN;
            request.Target.ApplySmoothKnockback(request.Direction, request.Distance, KNOCKBACK_SLIDE_DURATION);
            return true;
        }

        internal bool UpdateCanonicalTargetArea(float currentTime)
        {
            TargetAreaCastState state = _targetAreaCastState;
            if (state == null)
                return false;

            if (state.TryConsumeImpact(currentTime, ResolveAttackIntervalDivisor(), out Vector3 impactPoint))
            {
                ResolveCanonicalTargetAreaImpact(impactPoint, state.PrimaryTargetInstanceId);
                return true;
            }

            if (state.IsReadyForTarget(currentTime) == false)
                return false;

            MonsterController castTarget = this.FindNearestTargetAreaCastTarget();
            if (castTarget == null)
            {
                state.RecordNoTarget(currentTime);
                return false;
            }

            this.FaceTarget(castTarget);
            Vector3 lockedImpactPoint = AllyTargeting.ResolveTargetPoint(castTarget, transform.position);
            if (state.TryBeginCast(currentTime, lockedImpactPoint, castTarget.GetInstanceID()) == false)
                return false;

            _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);

            if (state.TryConsumeImpact(currentTime, ResolveAttackIntervalDivisor(), out impactPoint))
                ResolveCanonicalTargetAreaImpact(impactPoint, state.PrimaryTargetInstanceId);

            return true;
        }

        private void ResolveCanonicalTargetAreaImpact(Vector3 impactPoint, int primaryTargetInstanceId)
        {
            List<TargetAreaImpactCandidate> targets = this.CollectTargetAreaImpactTargets(impactPoint);
            if (targets.Count == 0)
                return;

            P0BossDpsTracker.RecordAttackCast(GetSourceId(), targets[0].Target);
            this.SpawnCanonicalCompanionAttack(impactPoint, impactPoint - transform.position);
            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i].Target;
                if (target == null || target.IsValid() == false)
                    continue;

                this.TryDamageTarget(target, _damage, AttackVisualKind.AreaHit, false, ResolveFuseLinkEffectId(GetSourceId()));
                TargetAreaPushRequest pushRequest = TargetAreaPushRequest.Create(
                    TargetAreaNormalPush,
                    TargetAreaEliteBossPush,
                    targets[i],
                    impactPoint);
                this.TryApplyTargetAreaPush(pushRequest);
            }

            if (HasPromotedTargetAreaFollowUp == false || _isDown)
                return;

            if (PromotedTargetAreaFollowUpSelector.TrySelect(
                    _targetAreaCandidates,
                    impactPoint,
                    primaryTargetInstanceId,
                    _promotedTargetAreaFollowUp.Radius,
                    out TargetAreaImpactCandidate followUp) == false)
            {
                return;
            }

            MonsterController followUpTarget = followUp.Target;
            if (followUpTarget == null || followUpTarget.IsValid() == false)
                return;

            int followUpDamage = _promotedTargetAreaFollowUp.ResolveDamage(_damage);
            this.TryDamageTarget(followUpTarget, followUpDamage, AttackVisualKind.SingleHit, spawnHitVisual: false);
        }

        private static string ResolveFuseLinkEffectId(string sourceId)
        {
            return sourceId == "bombardier" ? "dmg_bomb_explosion_v1"
                : sourceId == "skeleton_bomber" ? "dmg_skeleton_bomb_v1"
                : null;
        }

        public void SetCanonicalTargetAreaInfo(CompanionTargetAreaCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedArea;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = 1.0f;
            _targetAreaRadius = Mathf.Max(setup.Radius, MIN_ATTACK_RANGE);
            _targetAreaMaxTargets = Mathf.Max(1, setup.MaxTargets);
            _targetAreaNormalPush = setup.NormalPush;
            _targetAreaEliteBossPush = setup.EliteBossPush;
            _targetRule = setup.TargetRule;
            _hasPromotedTargetAreaFollowUp = false;
            _targetAreaCastState = new TargetAreaCastState();
            _targetAreaCastState.Configure(setup, Time.time, UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetPromotedTargetAreaFollowUp(PromotedTargetAreaFollowUpSetup setup)
        {
            if (_sourceIdOverride != setup.SourceId || setup.SourceId != "skeleton_bomber")
            {
                throw new InvalidOperationException(
                    "Bone Artillery follow-up requires the active skeleton_bomber target-area setup.");
            }

            _promotedTargetAreaFollowUp = setup;
            _hasPromotedTargetAreaFollowUp = true;
        }
    }

}
