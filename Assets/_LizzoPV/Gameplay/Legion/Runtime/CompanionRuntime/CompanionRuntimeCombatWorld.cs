using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRuntimeCombatWorld : ICompanionCombatWorld, IRangedCompanionTargetWorld
    {
        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly ICompanionRuntimeModifierSource _modifiers;
        private readonly CompanionRuntimeSpawnedDeliveryResolver _spawnedDeliveries;
        private readonly CompanionRuntimeImmediateTargetCollector _immediateTargets =
            new CompanionRuntimeImmediateTargetCollector();
        private readonly List<TargetAreaImpactCandidate> _specialCandidates =
            new List<TargetAreaImpactCandidate>(16);
        private readonly List<TargetAreaImpactCandidate> _specialTargets =
            new List<TargetAreaImpactCandidate>(8);
        private readonly List<ChainTargetCandidate> _chainCandidates =
            new List<ChainTargetCandidate>(16);
        private readonly List<ChainTargetCandidate> _chainTargets =
            new List<ChainTargetCandidate>(8);
        private readonly HashSet<int> _specialVisitedTargets = new HashSet<int>();
        private Vector3 _commanderPosition;
        private float _elapsedSeconds;

        internal CompanionRuntimeCombatWorld(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields,
            ICompanionRuntimeModifierSource modifiers = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            ICombatProjectileModule checkedProjectiles = projectiles
                ?? throw new ArgumentNullException(nameof(projectiles));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _modifiers = modifiers;
            ICombatPersistentFieldModule checkedPersistentFields = persistentFields
                ?? throw new ArgumentNullException(nameof(persistentFields));
            _spawnedDeliveries = new CompanionRuntimeSpawnedDeliveryResolver(
                checkedProjectiles,
                checkedPersistentFields);
        }

        internal void SetCommanderPosition(Vector3 position)
        {
            _commanderPosition = position;
            _elapsedSeconds = Time.time;
        }

        internal void Reset()
        {
            _immediateTargets.Reset();
            _specialCandidates.Clear();
            _specialTargets.Clear();
            _chainCandidates.Clear();
            _chainTargets.Clear();
            _specialVisitedTargets.Clear();
            _commanderPosition = Vector3.zero;
            _elapsedSeconds = 0.0f;
        }

        public bool TrySelectTargetPosition(out CompanionPoint targetPosition)
        {
            return TrySelectTargetPosition(
                new CompanionPoint(_commanderPosition.x, _commanderPosition.y),
                0.0f,
                out targetPosition);
        }

        public bool TrySelectTargetPosition(
            CompanionPoint origin,
            float maxRange,
            out CompanionPoint targetPosition)
        {
            return CompanionRuntimeTargetSelector.TrySelect(
                _registry.Enemies,
                origin,
                maxRange,
                out targetPosition);
        }

        public EffectResolution Resolve(in EffectIntent intent)
        {
            CombatEffectData effect = _data.GetCombatEffect(intent.EffectId);
            if (effect == null || effect.BaseValue <= 0.0f)
            {
                return new EffectResolution(false, intent.EffectId, 0.0f, 0);
            }

            if (effect.EffectKind == CombatEffectKind.Heal)
            {
                CompanionPassiveCombatModifiers healModifiers = ResolveModifiers(intent.SourceCompanionId);
                return ResolveCommanderHeal(in intent, effect, healModifiers.HealMultiplier);
            }

            Vector3 source = new Vector3(intent.SourcePosition.X, intent.SourcePosition.Y, 0.0f);
            Vector3 target = new Vector3(intent.TargetPosition.X, intent.TargetPosition.Y, 0.0f);
            CompanionPassiveCombatModifiers combatModifiers = ResolveModifiers(intent.SourceCompanionId);
            float promotedMultiplier = intent.MemberOrder == 2 ? combatModifiers.PromotedDamageMultiplier : 1.0f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(intent.SourceMagnitude * combatModifiers.DamageMultiplier * promotedMultiplier));
            CompanionRuntimeEffectOwnership ownership = CompanionRuntimeEffectAttribution.Resolve(in intent);
            int ownerId = ownership.OwnerId;
            CountableKillAttribution attribution = ownership.KillAttribution;

            switch (intent.Delivery)
            {
                case AttackDelivery.Direct:
                    return ResolveImmediate(in intent, effect, source, target, damage, attribution, false, combatModifiers);
                case AttackDelivery.Area:
                    return ResolveImmediate(in intent, effect, source, target, damage, attribution, true, combatModifiers);
                case AttackDelivery.Chain:
                    return ResolveChain(
                        in intent,
                        effect,
                        source,
                        damage,
                        ownerId,
                        attribution,
                        combatModifiers);
                case AttackDelivery.Projectile:
                    return _spawnedDeliveries.ResolveProjectile(
                        in intent,
                        effect,
                        source,
                        target,
                        damage,
                        attribution,
                        combatModifiers.ProjectileSpeedMultiplier,
                        CreateStatusPayload(intent.SourceCompanionId, ownerId, effect, combatModifiers),
                        combatModifiers.ProjectileCount * (1 + combatModifiers.ExtraHitCount),
                        combatModifiers.ProjectilePierceBonus,
                        string.Equals(intent.SourceCompanionId, "skeleton_scythe_thrower", StringComparison.Ordinal),
                        combatModifiers.PenetrationDamageStep);
                case AttackDelivery.SpawnedActor:
                    return _spawnedDeliveries.ResolvePersistentField(
                        in intent,
                        effect,
                        source,
                        target,
                        damage,
                        ownerId,
                        _elapsedSeconds,
                        combatModifiers.AreaRadiusMultiplier,
                        combatModifiers.OwnedEffectDurationMultiplier,
                        combatModifiers.FieldTickIntervalMultiplier,
                        combatModifiers.FieldCapacityBonus);
                case AttackDelivery.ReturningProjectile:
                    return ResolveReturningProjectile(
                        in intent,
                        effect,
                        source,
                        target,
                        damage,
                        attribution,
                        combatModifiers);
                case AttackDelivery.OwnedProxy:
                    return ResolveOwnedProxy(
                        in intent,
                        effect,
                        source,
                        damage,
                        attribution,
                        combatModifiers);
                default:
                    return new EffectResolution(false, intent.EffectId, 0.0f, 0);
            }
        }

        private EffectResolution ResolveReturningProjectile(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            int damage,
            CountableKillAttribution attribution,
            CompanionPassiveCombatModifiers modifiers)
        {
            Vector3 direction = target - source;
            if (direction.sqrMagnitude <= 0.0001f)
                return new EffectResolution(false, intent.EffectId, 0.0f, 0);

            int ownerId = CompanionRuntimeEffectAttribution.Resolve(in intent).OwnerId;

            direction.Normalize();
            Vector3 end = source + direction * Mathf.Max(0.01f, effect.Range * modifiers.RangeMultiplier);
            ReturningAttackPass pass = intent.ChainDepth > 0
                ? ReturningAttackPass.Return
                : ReturningAttackPass.Outbound;
            CollectSpecialCandidates();
            CompanionReturningAttackTargetSelector.Collect(
                _specialCandidates,
                source,
                end,
                Mathf.Max(0.01f, effect.Radius * modifiers.AreaRadiusMultiplier),
                Mathf.Max(1, effect.MaxTargets + modifiers.ProjectilePierceBonus),
                pass,
                _specialTargets);

            int affected = 0;
            for (int index = 0; index < _specialTargets.Count; index += 1)
            {
                MonsterController enemy = _specialTargets[index].Target;
                if (CompanionRuntimeTargetSelector.IsValid(enemy)
                    && _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                        intent.SourceCompanionId,
                        enemy,
                        pass == ReturningAttackPass.Outbound ? source : end,
                        enemy.transform.position,
                        pass == ReturningAttackPass.Return
                            ? Mathf.Max(1, Mathf.RoundToInt(damage * modifiers.ReturnDamageMultiplier))
                            : damage,
                        AttackVisualKind.SingleHit,
                        false,
                        attribution,
                        effect.Id)))
                {
                    affected += 1;
                    ApplyStatus(enemy, intent.SourceCompanionId, ownerId, effect, modifiers);
                }
            }

            Vector3 visualSource = pass == ReturningAttackPass.Outbound ? source : end;
            Vector3 visualTarget = pass == ReturningAttackPass.Outbound ? end : source;
            CompanionRuntimeEffectPresenter.Present(
                effect.Id,
                intent.PresentationCueId,
                visualSource,
                visualTarget,
                visualTarget - visualSource,
                effect.Range,
                effect.Radius,
                intent.MemberOrder);

            IReadOnlyList<IndependentEffectRequest> followUps = pass == ReturningAttackPass.Outbound
                ? new[]
                {
                    new IndependentEffectRequest(
                        intent.SquadId,
                        intent.SourceCompanionId,
                        intent.EffectId,
                        intent.SourceMagnitude,
                        intent.TargetPosition,
                        intent.Motion,
                        intent.Delivery,
                        intent.MemberOrder,
                        intent.PresentationCueId,
                        Mathf.Max(0.01f, effect.Duration / Mathf.Max(0.01f, modifiers.ReturnSpeedMultiplier))),
                }
                : Array.Empty<IndependentEffectRequest>();
            return new EffectResolution(
                affected > 0,
                effect.Id,
                affected > 0 ? damage : 0.0f,
                affected,
                followUps);
        }

        private EffectResolution ResolveChain(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            int damage,
            int ownerId,
            CountableKillAttribution attribution,
            CompanionPassiveCombatModifiers modifiers)
        {
            _chainCandidates.Clear();
            foreach (MonsterController enemy in _registry.Enemies)
            {
                if (!CompanionRuntimeTargetSelector.IsValid(enemy))
                    continue;

                _chainCandidates.Add(new ChainTargetCandidate(
                    enemy,
                    enemy.transform.position,
                    enemy.GetInstanceID()));
            }

            ChainTargetSelector.Collect(
                _chainCandidates,
                source,
                Mathf.Max(0.01f, effect.Range * modifiers.RangeMultiplier),
                Mathf.Max(0.01f, effect.ChainDistance),
                Mathf.Max(1, effect.MaxTargets + modifiers.ChainTargetBonus),
                _chainTargets);

            int affected = 0;
            Vector3 firstTarget = source;
            float retention = Mathf.Clamp(0.75f + modifiers.ChainDamageRetentionBonus, 0.0f, 1.0f);
            for (int index = 0; index < _chainTargets.Count; index += 1)
            {
                ChainTargetCandidate selected = _chainTargets[index];
                MonsterController enemy = selected.Target;
                if (!CompanionRuntimeTargetSelector.IsValid(enemy))
                    continue;

                if (affected == 0)
                    firstTarget = selected.Point;
                int hitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Pow(retention, index)));
                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    intent.SourceCompanionId,
                    enemy,
                    source,
                    selected.Point,
                    hitDamage,
                    AttackVisualKind.SingleHit,
                    false,
                    attribution,
                    effect.Id)))
                {
                    if (affected == 0)
                        ApplyStatus(enemy, intent.SourceCompanionId, ownerId, effect, modifiers);
                    affected += 1;
                }
            }

            CompanionRuntimeEffectPresenter.Present(
                effect.Id,
                intent.PresentationCueId,
                source,
                firstTarget,
                firstTarget - source,
                effect.Range,
                effect.ChainDistance,
                intent.MemberOrder);
            return new EffectResolution(
                affected > 0,
                effect.Id,
                affected > 0 ? damage : 0.0f,
                affected);
        }

        private EffectResolution ResolveOwnedProxy(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            int damage,
            CountableKillAttribution attribution,
            CompanionPassiveCombatModifiers modifiers)
        {
            CollectSpecialCandidates();
            int ownerId = CompanionRuntimeEffectAttribution.Resolve(in intent).OwnerId;
            _specialVisitedTargets.Clear();
            Vector3 chainOrigin = source;
            Vector3 firstTarget = source;
            int affected = 0;
            int chainLimit = Mathf.Max(1, effect.TriggerCount + modifiers.ChainTargetBonus + modifiers.OwnedActorCountBonus);
            for (int chainIndex = 0; chainIndex < chainLimit; chainIndex += 1)
            {
                float range = chainIndex == 0
                    ? Mathf.Max(0.01f, effect.Range)
                    : Mathf.Max(0.01f, effect.Radius);
                if (!CompanionPrimaryTargetSelector.TrySelectLowestHealth(
                        _specialCandidates,
                        chainOrigin,
                        range,
                        _specialVisitedTargets,
                        out TargetAreaImpactCandidate selected))
                {
                    break;
                }

                MonsterController enemy = selected.Target;
                _specialVisitedTargets.Add(selected.InstanceId);
                if (!CompanionRuntimeTargetSelector.IsValid(enemy))
                    continue;

                if (affected == 0)
                    firstTarget = selected.Point;
                chainOrigin = selected.Point;
                int hitDamage = damage;
                if (string.Equals(intent.SourceCompanionId, "lightning_mage", StringComparison.Ordinal) && chainIndex > 0)
                {
                    float retention = Mathf.Clamp(0.75f + modifiers.ChainDamageRetentionBonus, 0.0f, 1.0f);
                    hitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Pow(retention, chainIndex)));
                }
                if (modifiers.ExecutionThreshold > 0.0f
                    && enemy.MaxHp > 0
                    && (float)enemy.Hp / enemy.MaxHp <= modifiers.ExecutionThreshold)
                {
                    hitDamage = Mathf.Max(hitDamage, enemy.Hp);
                }

                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    intent.SourceCompanionId,
                    enemy,
                    source,
                    enemy.transform.position,
                    hitDamage,
                    AttackVisualKind.SingleHit,
                    false,
                    attribution,
                    effect.Id)))
                {
                    affected += 1;
                    ApplyStatus(enemy, intent.SourceCompanionId, ownerId, effect, modifiers);
                }

                if (CompanionRuntimeTargetSelector.IsValid(enemy))
                    break;
            }

            CompanionRuntimeEffectPresenter.Present(
                effect.Id,
                intent.PresentationCueId,
                source,
                firstTarget,
                firstTarget - source,
                effect.Range,
                effect.Radius,
                intent.MemberOrder);
            return new EffectResolution(
                affected > 0,
                effect.Id,
                affected > 0 ? damage : 0.0f,
                affected);
        }

        private void CollectSpecialCandidates()
        {
            _specialCandidates.Clear();
            foreach (MonsterController enemy in _registry.Enemies)
            {
                if (!CompanionRuntimeTargetSelector.IsValid(enemy))
                    continue;

                _specialCandidates.Add(new TargetAreaImpactCandidate(
                    enemy,
                    enemy.transform.position,
                    enemy.GetInstanceID()));
            }
        }

        private EffectResolution ResolveCommanderHeal(
            in EffectIntent intent,
            CombatEffectData effect,
            float healMultiplier)
        {
            PlayerController commander = _registry.Player;
            if (commander == null
                || !commander.isActiveAndEnabled
                || commander.MaxHp <= 0
                || commander.Hp <= 0)
            {
                return new EffectResolution(false, effect.Id, 0.0f, 0);
            }

            int requested = Mathf.Max(1, Mathf.RoundToInt(intent.SourceMagnitude * healMultiplier));
            int before = commander.Hp;
            commander.Hp = Mathf.Min(commander.MaxHp, commander.Hp + requested);
            int actual = commander.Hp - before;
            if (actual > 0)
            {
                FloatingDamageText.ShowHeal(commander.transform.position, actual);
            }

            AttackVisual.SpawnAttached(
                commander.transform,
                AttackVisualKind.HealingReceived,
                new Vector3(0.0f, 0.32f, 0.0f),
                1.65f);
            return new EffectResolution(true, effect.Id, actual, actual > 0 ? 1 : 0);
        }

        private EffectResolution ResolveImmediate(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            int damage,
            CountableKillAttribution attribution,
            bool area,
            CompanionPassiveCombatModifiers modifiers)
        {
            IReadOnlyList<MonsterController> targets = _immediateTargets.Collect(
                _registry.Enemies,
                effect,
                source,
                target,
                area,
                area ? modifiers.AreaRadiusMultiplier : modifiers.RangeMultiplier);
            int affected = 0;
            int ownerId = CompanionRuntimeEffectAttribution.Resolve(in intent).OwnerId;
            Vector3 forward = target - source;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
            }

            int maxTargets = effect.AffectsAllTargetsInShape
                ? targets.Count
                : Mathf.Max(1, effect.MaxTargets + modifiers.ChainTargetBonus);
            for (int index = 0; index < targets.Count && affected < maxTargets; index += 1)
            {
                MonsterController enemy = targets[index];
                int hitDamage = damage;
                if (string.Equals(intent.SourceCompanionId, "shield_guard", StringComparison.Ordinal)
                    && (enemy.transform.position - _commanderPosition).sqrMagnitude <= 4.0f)
                {
                    hitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * modifiers.CloseDamageMultiplier));
                }
                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    intent.SourceCompanionId,
                    enemy,
                    source,
                    enemy.transform.position,
                    Mathf.Max(1, Mathf.RoundToInt(hitDamage * (area ? modifiers.CenterDamageMultiplier : 1.0f))),
                    area ? AttackVisualKind.AreaHit : AttackVisualKind.ForwardSlash,
                    false,
                    attribution,
                    effect.Id)))
                {
                    affected += 1;
                    if (intent.SourceCompanionId != "lightning_mage" || affected == 1)
                        ApplyStatus(enemy, intent.SourceCompanionId, ownerId, effect, modifiers);
                    if (effect.Push > 0.0f)
                    {
                        enemy.ApplySmoothKnockback(forward, effect.Push * modifiers.ForcedMovementMultiplier);
                    }
                }
            }

            int duplicateHits = Mathf.Max(0, modifiers.ExtraHitCount + modifiers.FragmentCount);
            if (affected > 0 && duplicateHits > 0)
            {
                int repeatDamage = modifiers.FragmentCount > 0
                    ? Mathf.Max(1, damage / 3)
                    : damage;
                for (int repeat = 0; repeat < duplicateHits; repeat++)
                {
                    MonsterController enemy = targets[repeat % targets.Count];
                    if (CompanionRuntimeTargetSelector.IsValid(enemy)
                        && _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                            intent.SourceCompanionId,
                            enemy,
                            source,
                            enemy.transform.position,
                            repeatDamage,
                            AttackVisualKind.SingleHit,
                            false,
                            attribution,
                            effect.Id)))
                    {
                        affected += 1;
                    }
                }
            }

            CompanionRuntimeEffectPresenter.Present(
                effect.Id,
                intent.PresentationCueId,
                source,
                target,
                forward,
                effect.Range,
                effect.Radius,
                intent.MemberOrder);
            return new EffectResolution(affected > 0, effect.Id, affected > 0 ? damage : 0.0f, affected);
        }

        private CompanionPassiveCombatModifiers ResolveModifiers(string companionId)
        {
            return _modifiers == null
                ? CompanionPassiveCombatModifiers.Identity
                : _modifiers.Resolve(companionId);
        }

        private static CompanionProjectileStatusPayload CreateStatusPayload(
            string companionId,
            int ownerId,
            CombatEffectData effect,
            CompanionPassiveCombatModifiers modifiers)
        {
            if (effect.StatusKind == CompanionEnemyStatusKind.None
                || effect.StatusDuration <= 0.0f)
            {
                return default;
            }

            return new CompanionProjectileStatusPayload(
                effect.StatusKind,
                new CompanionStatusSource(companionId, ownerId),
                effect.StatusMagnitude + modifiers.StatusMagnitudeBonus,
                effect.StatusDuration * modifiers.StatusDurationMultiplier);
        }

        private static void ApplyStatus(
            MonsterController enemy,
            string companionId,
            int ownerId,
            CombatEffectData effect,
            CompanionPassiveCombatModifiers modifiers)
        {
            if (enemy == null
                || enemy.IsValid() == false
                || effect.StatusKind == CompanionEnemyStatusKind.None
                || effect.StatusDuration <= 0.0f)
            {
                return;
            }

            enemy.ApplyCompanionStatus(
                effect.StatusKind,
                new CompanionStatusSource(companionId, ownerId),
                effect.StatusMagnitude + modifiers.StatusMagnitudeBonus,
                effect.StatusDuration * modifiers.StatusDurationMultiplier,
                Time.time);
        }
    }
}
