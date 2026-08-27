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
            PromotedMultiHitSequence sequence = _promotedMultiHitSequence;
            if (sequence == null)
            {
                bool singleResolved = AttackPlayerForward(forward, AttackVisualKind.ForwardSlash, pushTargets: false);
                if (singleResolved)
                    this.SpawnCanonicalCompanionAttack(this.ResolveForwardAttackVisualPosition(), forward);
                return singleResolved;
            }

            int originalDamage = _damage;
            _damage = Mathf.Max(1, Mathf.RoundToInt(originalDamage * sequence.DamageRatio));
            bool resolved = false;
            sequence.BeginCast();
            for (int pass = 0; pass < sequence.PassCount; pass++)
            {
                if (AttackPlayerForward(forward, AttackVisualKind.ForwardSlash, pushTargets: false) == false)
                    break;

                resolved = true;
                sequence.TryRecordResolvedPass();
            }
            _damage = originalDamage;
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

            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i];
                if (target == null || target.IsValid() == false)
                    continue;

                this.DamageTarget(target, visualKind, spawnHitVisual: false);
                if (pushTargets)
                {
                    bool didPush = this.TryApplyKnockback(target, forward);
                    if (didPush)
                        AllyTargeting.SpawnShieldPushImpact(target, forward);
                }
            }

            return true;
        }

        public void SetCanonicalWraithMeleeDefenseInfo(CompanionWraithMeleeDefenseSetup setup)
        {
            SetCanonicalMeleeInfo(setup.Melee);
            _personalMitigation = new PersonalDamageMitigationState();
            _personalMitigation.Configure(setup.PersonalDefense, Time.time);
        }

        public void SetPromotedMultiHitSequence(PromotedMultiHitSequence sequence)
        {
            _promotedMultiHitSequence = sequence;
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
            _nextAttackTime = Time.time + UnityEngine.Random.Range(0.1f, 0.35f);
        }
    }
}
