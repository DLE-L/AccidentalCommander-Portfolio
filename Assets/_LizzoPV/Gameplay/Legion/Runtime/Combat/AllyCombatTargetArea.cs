using System.Collections.Generic;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.Gameplay.Telemetry;
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
                ResolveCanonicalTargetAreaImpact(impactPoint);
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
                ResolveCanonicalTargetAreaImpact(impactPoint);

            return true;
        }

        private void ResolveCanonicalTargetAreaImpact(Vector3 impactPoint)
        {
            List<TargetAreaImpactCandidate> targets = this.CollectTargetAreaImpactTargets(impactPoint);
            if (targets.Count == 0)
                return;

            RunBossDpsTracker.RecordAttackCast(GetSourceId(), targets[0].Target);
            this.SpawnCanonicalCompanionAttack(impactPoint, impactPoint - transform.position);
            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i].Target;
                if (target == null || target.IsValid() == false)
                    continue;

                this.TryDamageTarget(target, _damage, AttackVisualKind.AreaHit, false, ResolveCombatEffectId(GetSourceId()));
                if (target.IsValid() && _targetAreaStatusKind != Lizzo.PV.Data.CompanionEnemyStatusKind.None)
                {
                    CompanionRuntime runtime = GetRuntime();
                    int ownerId = runtime == null ? GetInstanceID() : runtime.GetInstanceID();
                    target.ApplyCompanionStatus(
                        _targetAreaStatusKind,
                        new CompanionStatusSource(GetSourceId(), ownerId),
                        _targetAreaStatusMagnitude,
                        _targetAreaStatusDuration,
                        Time.time);
                }
                TargetAreaPushRequest pushRequest = TargetAreaPushRequest.Create(
                    TargetAreaNormalPush,
                    TargetAreaEliteBossPush,
                    targets[i],
                    impactPoint);
                this.TryApplyTargetAreaPush(pushRequest);
            }

        }

        private static string ResolveCombatEffectId(string sourceId)
        {
            return sourceId == "bombardier" ? "dmg_bomb_explosion_v1"
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
            _targetAreaStatusKind = setup.AppliedStatusKind;
            _targetAreaStatusMagnitude = setup.StatusMagnitude;
            _targetAreaStatusDuration = setup.StatusDuration;
            _targetAreaCastState = new TargetAreaCastState();
            _targetAreaCastState.Configure(setup, Time.time, UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

    }

}
