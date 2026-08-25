using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
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
            if (TryResolveNearestTargetForward(out Vector3 targetForward))
                return targetForward;

            return _party.Formation.ResolveForward();
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
            _nextAttackTime = Time.time + UnityEngine.Random.Range(0.1f, 0.35f);
        }
    }

    public readonly struct CompanionMeleeCombatSetup
    {
        public readonly AllyAttackStyle AttackStyle;
        public readonly int Damage;
        public readonly float Period;
        public readonly float Range;
        public readonly float Angle;
        public readonly float Knockback;
        public readonly int MaxTargets;
        public readonly float NoTargetRetrySeconds;

        public CompanionMeleeCombatSetup(
            AllyAttackStyle attackStyle,
            int damage,
            float period,
            float range,
            float angle,
            float knockback,
            int maxTargets,
            float noTargetRetrySeconds)
        {
            AttackStyle = attackStyle;
            Damage = damage;
            Period = period;
            Range = range;
            Angle = angle;
            Knockback = knockback;
            MaxTargets = maxTargets;
            NoTargetRetrySeconds = noTargetRetrySeconds;
        }

        internal CompanionMeleeCombatSetup WithShieldAreaPushCompatibilityOverride()
        {
            return new CompanionMeleeCombatSetup(
                AllyAttackStyle.AreaPulse,
                Damage,
                Period,
                Mathf.Max(Range, 1.8f),
                Angle,
                Mathf.Max(Knockback, 0.9f),
                MaxTargets,
                NoTargetRetrySeconds);
        }

        public CompanionMeleeCombatSetup WithPromotedShieldCaptainGeometry()
        {
            return new CompanionMeleeCombatSetup(
                AllyAttackStyle.ForwardPush,
                Damage,
                Period,
                1.8f,
                90.0f,
                0.9f,
                MaxTargets,
                NoTargetRetrySeconds);
        }

        public CompanionMeleeCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionMeleeCombatSetup(
                AttackStyle,
                Mathf.Max(1, Mathf.RoundToInt(Damage * scale.EffectMultiplier)),
                Mathf.Max(0.01f, Period * scale.IntervalMultiplier),
                Range,
                Angle,
                Knockback,
                MaxTargets,
                NoTargetRetrySeconds);
        }

        public CompanionMeleeCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionMeleeCombatSetup(AttackStyle, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, Angle, Knockback, MaxTargets, NoTargetRetrySeconds);
        }
    }

    public readonly struct CompanionWraithMeleeDefenseSetup
    {
        public readonly CompanionMeleeCombatSetup Melee;
        public readonly PersonalDamageMitigationSetup PersonalDefense;

        public CompanionWraithMeleeDefenseSetup(
            CompanionMeleeCombatSetup melee,
            PersonalDamageMitigationSetup personalDefense)
        {
            Melee = melee;
            PersonalDefense = personalDefense;
        }

        public CompanionWraithMeleeDefenseSetup WithPromotedWraithGuardianDefense()
        {
            return new CompanionWraithMeleeDefenseSetup(
                Melee,
                new PersonalDamageMitigationSetup(0.50f, 5.0f, 1.5f));
        }

        public CompanionWraithMeleeDefenseSetup WithPromotedWraithGuardianGeometry()
        {
            return new CompanionWraithMeleeDefenseSetup(
                new CompanionMeleeCombatSetup(
                    Melee.AttackStyle,
                    Melee.Damage,
                    Melee.Period,
                    1.4f,
                    75.0f,
                    Melee.Knockback,
                    3,
                    Melee.NoTargetRetrySeconds),
                PersonalDefense);
        }
    }

    public sealed class CompanionMeleeCombatResolver
    {
        private readonly IDataProvider _data;

        public CompanionMeleeCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionMeleeCombatSetup setup)
        {
            if (IsMigratedBaseUnit(baseUnitId) == false)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (profile == null)
                throw new InvalidOperationException($"Canonical melee profile is missing: {baseUnitId}");

            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId);
            if (effect == null)
                throw new InvalidOperationException($"Canonical melee effect is missing: {profile.BasicEffectId}");

            if (effect.OwnerUnitId != baseUnitId
                || effect.SkillId != profile.BasicSkillId
                || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Cone
                || effect.MaxTargets <= 0
                || effect.CastInterval <= 0.0f
                || effect.Range <= 0.0f
                || effect.Angle <= 0.0f
                || profile.NoTargetRetrySeconds <= 0.0f)
            {
                throw new InvalidOperationException($"Canonical melee data is invalid: {baseUnitId}");
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            AllyAttackStyle attackStyle = effect.Push > 0.0f
                ? AllyAttackStyle.ForwardPush
                : AllyAttackStyle.ForwardSlash;
            setup = new CompanionMeleeCombatSetup(
                attackStyle,
                damage,
                effect.CastInterval,
                effect.Range,
                effect.Angle,
                effect.Push,
                effect.MaxTargets,
                profile.NoTargetRetrySeconds);
            return true;
        }

        public bool TryResolveWraithMeleeDefense(
            float attackMultiplier,
            out CompanionWraithMeleeDefenseSetup setup)
        {
            const string wraithKnightId = "wraith_knight";
            if (TryResolve(wraithKnightId, attackMultiplier, out CompanionMeleeCombatSetup melee) == false)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(wraithKnightId)
                ?? throw new InvalidOperationException("Canonical Wraith profile is missing.");
            CombatEffectData defense = _data.GetCombatEffect(profile.SecondaryEffectId)
                ?? throw new InvalidOperationException("Canonical Wraith defense effect is missing.");

            if (defense.OwnerUnitId != wraithKnightId
                || defense.SkillId != profile.SecondarySkillId
                || defense.EffectKind != CombatEffectKind.DamageReduction
                || defense.DeliveryKind != CombatDeliveryKind.Self
                || defense.TargetRule != CombatTargetRule.Self
                || defense.BaseValue <= 0.0f
                || defense.BaseValue > 1.0f
                || defense.CastInterval <= 0.0f
                || defense.Duration <= 0.0f
                || defense.MaxTargets != 1)
            {
                throw new InvalidOperationException("Canonical Wraith defense data is invalid.");
            }

            setup = new CompanionWraithMeleeDefenseSetup(
                melee,
                new PersonalDamageMitigationSetup(
                    defense.BaseValue,
                    defense.CastInterval,
                    defense.Duration));
            return true;
        }

        private static bool IsMigratedBaseUnit(string baseUnitId)
        {
            return baseUnitId == "shield_guard"
                || baseUnitId == "sword_soldier"
                || baseUnitId == "wraith_knight";
        }
    }
}
