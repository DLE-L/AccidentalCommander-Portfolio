using System.Collections.Generic;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        internal List<MonsterController> CollectForwardTargets(Vector3 forward)
        {
            _forwardTargets.Clear();
            if (_party.Registry == null || _party.Registry.Enemies == null)
                return _forwardTargets;

            foreach (MonsterController target in _party.Registry.Enemies)
            {
                if (target.IsValid() == false)
                    continue;

                Vector3 delta = this.GetClosestDeltaToTarget(target);
                if (IsInForwardHitbox(delta, forward))
                    AddForwardTarget(target);
            }

            return _forwardTargets;
        }

        private void AddForwardTarget(MonsterController candidate)
        {
            if (MaxForwardTargetCount == int.MaxValue)
            {
                _forwardTargets.Add(candidate);
                return;
            }

            List<MonsterController> targets = _forwardTargets;
            float candidateDistance = this.GetSqrDistanceToTarget(candidate);
            int candidateId = candidate.GetInstanceID();
            int insertIndex = 0;
            while (insertIndex < targets.Count)
            {
                MonsterController existing = targets[insertIndex];
                float existingDistance = this.GetSqrDistanceToTarget(existing);
                if (candidateDistance < existingDistance
                    || (Mathf.Approximately(candidateDistance, existingDistance)
                        && candidateId < existing.GetInstanceID()))
                {
                    break;
                }

                insertIndex++;
            }

            if (CanAcceptForwardTarget(insertIndex) == false)
                return;

            targets.Insert(insertIndex, candidate);
            if (targets.Count > MaxForwardTargetCount)
                targets.RemoveAt(MaxForwardTargetCount);
        }

        internal Vector3 ResolveForwardAttackDirection()
        {
            if (TryResolvePrimaryMeleeTargetForward(out Vector3 targetForward))
                return targetForward;

            return _party.Formation.ResolveForward();
        }

        internal bool TryResolvePrimaryMeleeTargetForward(out Vector3 forward)
        {
            forward = Vector3.zero;
            float maxRange = _range + FORWARD_HITBOX_RANGE_PADDING;
            List<TargetAreaImpactCandidate> candidates = this.CollectPrimaryTargetCandidates(maxRange);
            TargetAreaImpactCandidate selected = default;
            bool found = _targetRule == Lizzo.PV.Data.CombatTargetRule.CommanderThreat
                ? CompanionPrimaryTargetSelector.TrySelectCommanderThreat(
                    candidates,
                    transform.position,
                    _party.Registry.Player == null ? transform.position : _party.Registry.Player.transform.position,
                    maxRange,
                    out selected)
                : _targetRule == Lizzo.PV.Data.CombatTargetRule.DensestCluster
                    ? CompanionPrimaryTargetSelector.TrySelectDensestCluster(
                        candidates,
                        transform.position,
                        maxRange,
                        _range,
                        out selected)
                    : false;
            if (found == false)
                return TryResolveNearestTargetForward(out forward);

            Vector3 delta = selected.Point - transform.position;
            if (delta.sqrMagnitude <= 0.0001f && selected.Target != null)
                delta = selected.Target.transform.position - transform.position;
            if (delta.sqrMagnitude <= 0.0001f)
                return false;

            forward = delta.normalized;
            return true;
        }

        internal bool TryResolveNearestTargetForward(out Vector3 forward)
        {
            forward = Vector3.zero;

            MonsterController target = this.FindNearestMonster(_range + FORWARD_HITBOX_RANGE_PADDING);
            if (target == null)
                return false;

            Vector3 delta = this.GetFacingDeltaToTarget(target);
            if (delta.sqrMagnitude <= 0.0001f)
                return false;

            forward = delta.normalized;
            return true;
        }

        internal bool IsInForwardHitbox(Vector3 delta, Vector3 forward)
        {
            if (forward.sqrMagnitude <= 0.0001f)
                return false;

            Vector3 normalizedForward = forward.normalized;
            Vector3 right = new Vector3(normalizedForward.y, -normalizedForward.x, 0.0f);
            float forwardDistance = Vector3.Dot(delta, normalizedForward);
            float sideDistance = Mathf.Abs(Vector3.Dot(delta, right));
            float maxForwardDistance = _range + FORWARD_HITBOX_RANGE_PADDING;
            float maxSideDistance = Mathf.Max(FORWARD_HITBOX_HALF_WIDTH_MIN, _range * FORWARD_HITBOX_HALF_WIDTH_FACTOR);

            return forwardDistance >= -FORWARD_HITBOX_BACK_PADDING
                && forwardDistance <= maxForwardDistance
                && sideDistance <= maxSideDistance;
        }

        internal bool TryApplyKnockback(MonsterController target, Vector3 direction)
        {
            if (_knockback <= 0.0f || target.IsValid() == false || direction.sqrMagnitude <= 0.0001f)
                return false;

            if (IsKnockbackImmune(target))
                return false;

            int targetId = target.GetInstanceID();
            if (NextKnockbackAllowedTimeByTarget.TryGetValue(targetId, out float nextAllowedTime)
                && Time.time < nextAllowedTime)
                return false;

            NextKnockbackAllowedTimeByTarget[targetId] = Time.time + KNOCKBACK_INTERNAL_COOLDOWN;
            target.ApplySmoothKnockback(direction, _knockback, KNOCKBACK_SLIDE_DURATION);
            return true;
        }

        private static bool IsKnockbackImmune(MonsterController target)
        {
            EnemyRuntimeStats stats = target.RuntimeStats;
            return stats != null && stats.Data != null && stats.Data.Type == "boss";
        }

        internal bool AttackForwardSlash()
        {
            Vector3 forward = this.ResolveForwardAttackDirection();
            bool resolved = AttackPlayerForward(forward, AttackVisualKind.ForwardSlash, pushTargets: false);
            if (resolved)
                this.SpawnCanonicalCompanionAttack(this.ResolveForwardAttackVisualPosition(), forward);
            return resolved;
        }

        internal bool AttackForwardPush()
        {
            Vector3 forward = this.ResolveForwardAttackDirection();
            bool resolved = AttackPlayerForward(forward, AttackVisualKind.ShieldPush, pushTargets: true);
            if (resolved)
                this.SpawnCanonicalCompanionAttack(this.ResolveForwardAttackVisualPosition(), forward);
            return resolved;
        }

        internal bool AttackPlayerForward(Vector3 forward, AttackVisualKind visualKind, bool pushTargets)
        {
            List<MonsterController> targets = this.CollectForwardTargets(forward);
            if (targets.Count == 0)
                return false;

            this.FaceDirection(forward);
            P0BossDpsTracker.RecordAttackCast(GetSourceId(), this.PickSummaryTarget(targets));
            MonsterController statusTarget = ResolveMeleeStatusTarget(targets);

            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i];
                if (target == null || target.IsValid() == false)
                    continue;

                this.DamageTarget(target, visualKind, spawnHitVisual: false);
                if (target == statusTarget && target.IsValid() && _meleeStatusKind != Lizzo.PV.Data.CompanionEnemyStatusKind.None)
                {
                    CompanionRuntime runtime = GetRuntime();
                    int ownerId = runtime == null ? GetInstanceID() : runtime.GetInstanceID();
                    target.ApplyCompanionStatus(
                        _meleeStatusKind,
                        new CompanionStatusSource(GetSourceId(), ownerId),
                        _meleeStatusMagnitude,
                        _meleeStatusDuration,
                        Time.time);
                }
                if (pushTargets)
                {
                    Vector3 pushDirection = ResolvePushDirection(target, forward);
                    bool didPush = this.TryApplyKnockback(target, pushDirection);
                    if (didPush)
                        AllyTargeting.SpawnShieldPushImpact(target, pushDirection);
                }
            }

            return true;
        }

        private MonsterController ResolveMeleeStatusTarget(List<MonsterController> targets)
        {
            if (targets == null || targets.Count == 0 || _targetRule != Lizzo.PV.Data.CombatTargetRule.CommanderThreat)
                return targets == null || targets.Count == 0 ? null : targets[0];

            Vector3 commanderPosition = _party.Registry.Player == null
                ? transform.position
                : _party.Registry.Player.transform.position;
            MonsterController best = null;
            float bestDistance = float.PositiveInfinity;
            int bestInstanceId = int.MaxValue;
            for (int index = 0; index < targets.Count; index += 1)
            {
                MonsterController candidate = targets[index];
                if (candidate == null || candidate.IsValid() == false)
                    continue;

                float distance = (AllyTargeting.ResolveTargetPoint(candidate, commanderPosition) - commanderPosition).sqrMagnitude;
                int instanceId = candidate.GetInstanceID();
                if (distance > bestDistance
                    || (Mathf.Approximately(distance, bestDistance) && instanceId >= bestInstanceId))
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
                bestInstanceId = instanceId;
            }

            return best;
        }

        public void SetCanonicalWraithMeleeDefenseInfo(CompanionWraithMeleeDefenseSetup setup)
        {
            SetCanonicalMeleeInfo(setup.Melee);
            _personalMitigation = new PersonalDamageMitigationState();
            _personalMitigation.Configure(setup.PersonalDefense, Time.time);
        }

        public void SetCanonicalMeleeInfo(CompanionMeleeCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = setup.AttackStyle;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = setup.Knockback;
            _angle = setup.Angle;
            _maxForwardTargetCount = Mathf.Max(1, setup.MaxTargets);
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = null;
            _projectileSpeedMultiplier = 1.0f;
            _targetRule = setup.TargetRule;
            _meleeStatusKind = setup.AppliedStatusKind;
            _meleeStatusMagnitude = setup.StatusMagnitude;
            _meleeStatusDuration = setup.StatusDuration;
            _meleeMovement = setup.Movement;
            _meleeMovementTarget = null;
            _meleeMovementPhase = CompanionMoveActionPhase.AtFormation;
            _nextAttackTime = Time.time + UnityEngine.Random.Range(0.1f, 0.35f);
        }

        internal bool UpdateCanonicalMeleeMovement(float currentTime)
        {
            if (_meleeMovement.IsConfigured == false || _party?.Registry == null)
                return true;

            AllyFollower follower = ResolveFollower();
            if (follower == null)
                return false;

            if (_meleeMovementPhase == CompanionMoveActionPhase.Returning)
            {
                follower.ClearCombatDestination(_meleeMovement.ReturnMoveSpeed);
                if (follower.IsAtFormationTarget() == false)
                    return false;

                _meleeMovementPhase = CompanionMoveActionPhase.AtFormation;
            }

            Vector3 formationPosition = follower.ResolveFormationTargetPosition();
            Vector3 commanderPosition = _party.Registry.Player == null
                ? formationPosition
                : _party.Registry.Player.transform.position;
            Vector3 acquisitionOrigin = _meleeMovement.Kind == CompanionMeleeMovementKind.ShieldIntercept
                ? commanderPosition
                : formationPosition;

            if (_meleeMovementPhase == CompanionMoveActionPhase.AtFormation)
            {
                if (currentTime < _nextAttackTime)
                    return false;

                _meleeMovementTarget = SelectMeleeMovementTarget(acquisitionOrigin, commanderPosition);
                if (_meleeMovementTarget == null)
                {
                    follower.ClearCombatDestination(_meleeMovement.ReturnMoveSpeed);
                    return false;
                }

                _meleeMovementPhase = CompanionMoveActionPhase.Approaching;
            }

            if (IsMovementTargetValid(_meleeMovementTarget, acquisitionOrigin) == false)
            {
                BeginMeleeReturn();
                return false;
            }

            Vector3 targetPoint = AllyTargeting.ResolveTargetPoint(_meleeMovementTarget, acquisitionOrigin);
            Vector2 approachOrigin = _meleeMovement.Kind == CompanionMeleeMovementKind.ShieldIntercept
                ? (Vector2)commanderPosition
                : (Vector2)formationPosition;
            Vector2 desiredPosition = ResolveMeleeApproachPosition(
                approachOrigin,
                targetPoint,
                _range);
            desiredPosition = AllyFollower.ClampExcursionDestination(
                formationPosition,
                desiredPosition,
                _meleeMovement.MaxExcursionDistance);
            follower.SetCombatDestination(desiredPosition, _meleeMovement.EngageMoveSpeed);

            return CompanionMoveActionCycle.CanAttemptAction(
                _meleeMovement.Kind,
                _meleeMovementPhase,
                follower.IsAtCombatDestination(),
                IsMovementTargetInAttackRange(_meleeMovementTarget));
        }

        internal void ClearCanonicalMeleeMovement()
        {
            float returnMoveSpeed = _meleeMovement.ReturnMoveSpeed;
            _meleeMovement = default;
            ResetCanonicalMeleeMovementState(returnMoveSpeed);
        }

        internal void ResetCanonicalMeleeMovementState()
        {
            ResetCanonicalMeleeMovementState(_meleeMovement.ReturnMoveSpeed);
        }

        private void ResetCanonicalMeleeMovementState(float returnMoveSpeed)
        {
            _meleeMovementTarget = null;
            _meleeMovementPhase = CompanionMoveActionPhase.AtFormation;
            ResolveFollower()?.ClearCombatDestination(returnMoveSpeed);
        }

        private AllyFollower ResolveFollower()
        {
            if (_follower == null)
                _follower = GetComponent<AllyFollower>();
            return _follower;
        }

        private bool IsMovementTargetValid(MonsterController target, Vector3 acquisitionOrigin)
        {
            if (target.IsValid() == false)
                return false;

            Vector3 point = AllyTargeting.ResolveTargetPoint(target, acquisitionOrigin);
            float range = _meleeMovement.EngagementRange;
            return (point - acquisitionOrigin).sqrMagnitude <= range * range;
        }

        private MonsterController SelectMeleeMovementTarget(Vector3 acquisitionOrigin, Vector3 commanderPosition)
        {
            List<TargetAreaImpactCandidate> candidates = _targetAreaCandidates;
            candidates.Clear();
            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster.IsValid() == false)
                    continue;

                Vector3 point = AllyTargeting.ResolveTargetPoint(monster, acquisitionOrigin);
                candidates.Add(new TargetAreaImpactCandidate(monster, point, monster.GetInstanceID()));
            }

            TargetAreaImpactCandidate selected;
            bool found = _meleeMovement.Kind == CompanionMeleeMovementKind.ShieldIntercept
                ? CompanionPrimaryTargetSelector.TrySelectCommanderThreat(
                    candidates,
                    acquisitionOrigin,
                    commanderPosition,
                    _meleeMovement.EngagementRange,
                    out selected)
                : CompanionPrimaryTargetSelector.TrySelectDensestCluster(
                    candidates,
                    acquisitionOrigin,
                    _meleeMovement.EngagementRange,
                    _range,
                    out selected);
            return found ? selected.Target : null;
        }

        private bool IsMovementTargetInAttackRange(MonsterController target)
        {
            if (target.IsValid() == false)
                return false;

            float maxRange = _range + FORWARD_HITBOX_RANGE_PADDING;
            return this.GetClosestDeltaToTarget(target).sqrMagnitude <= maxRange * maxRange;
        }

        private Vector3 ResolvePushDirection(MonsterController target, Vector3 fallback)
        {
            if (_meleeMovement.Kind != CompanionMeleeMovementKind.ShieldIntercept
                || _party?.Registry?.Player == null
                || target == null)
            {
                return fallback;
            }

            return ResolveCommanderOutwardDirection(
                _party.Registry.Player.transform.position,
                target.transform.position,
                fallback);
        }

        private void BeginMeleeReturn()
        {
            if (_meleeMovement.IsConfigured == false)
                return;

            _meleeMovementTarget = null;
            _meleeMovementPhase = CompanionMoveActionCycle.AfterSuccessfulAction(_meleeMovement.Kind);
            ResolveFollower()?.ClearCombatDestination(_meleeMovement.ReturnMoveSpeed);
        }

        public static Vector2 ResolveMeleeApproachPosition(
            Vector2 approachOrigin,
            Vector2 targetPosition,
            float attackRange)
        {
            Vector2 delta = targetPosition - approachOrigin;
            if (delta.sqrMagnitude <= 0.0001f)
                return targetPosition;

            float standOffDistance = Mathf.Min(
                delta.magnitude * 0.5f,
                Mathf.Max(MIN_ATTACK_RANGE, attackRange * 0.65f));
            return targetPosition - delta.normalized * standOffDistance;
        }

        public static Vector3 ResolveCommanderOutwardDirection(
            Vector3 commanderPosition,
            Vector3 targetPosition,
            Vector3 fallback)
        {
            Vector3 outward = targetPosition - commanderPosition;
            outward.z = 0.0f;
            if (outward.sqrMagnitude > 0.0001f)
                return outward.normalized;

            fallback.z = 0.0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.right;
        }
    }
}
