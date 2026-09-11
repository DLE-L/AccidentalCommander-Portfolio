using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRuntimeCombatWorld : ICompanionCombatWorld, IRangedCompanionTargetWorld, ICompanionReturnPathWorld
    {
        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly ICompanionRuntimeModifierSource _modifiers;
        private readonly CompanionRuntimeSpawnedDeliveryResolver _spawnedDeliveries;
        private readonly CompanionImmediateAttack _immediateAttack;
        private readonly CompanionAreaVolley _areaVolley;
        internal void CommitAreaVolley(EffectIntent intent) => _areaVolley.Commit(intent);
        private readonly CompanionChainAttack _chainAttack;
        private readonly CompanionReturningAttack _returningAttack;
        private readonly CompanionCombatEvents _events;
        private readonly CompanionReturnPathAttack _returnPath;
        internal CompanionReturningAttack ReturningAttack => _returningAttack;
        internal CompanionReturningLight ReturningLight { get; }
        internal CompanionWolfAttack WolfAttack { get; }
        private Vector3 _commanderPosition;
        private float _elapsedSeconds;

        internal CompanionRuntimeCombatWorld(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields,
            ICompanionRuntimeModifierSource modifiers = null,
            CompanionCombatEvents presentation = null)
        {
            ReturningLight = new CompanionReturningLight(data, registry, immediateHits);
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            ICombatProjectileModule checkedProjectiles = projectiles
                ?? throw new ArgumentNullException(nameof(projectiles));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            WolfAttack = new CompanionWolfAttack(registry, checkedProjectiles, immediateHits);
            _modifiers = modifiers;
            _events = presentation;
            _returnPath = new CompanionReturnPathAttack(data, registry, immediateHits, ResolveModifiers, presentation);
            _immediateAttack = new CompanionImmediateAttack(_registry, _immediateHits, presentation, checkedProjectiles);
            _areaVolley = new CompanionAreaVolley(data, ResolveModifiers, intent => Resolve(in intent), presentation);
            _chainAttack = new CompanionChainAttack(_registry, _immediateHits, ApplyStatus, presentation);
            _returningAttack = new CompanionReturningAttack(_data, _registry, _immediateHits, ResolveModifiers, ApplyStatus);
            ICombatPersistentFieldModule checkedPersistentFields = persistentFields
                ?? throw new ArgumentNullException(nameof(persistentFields));
            _spawnedDeliveries = new CompanionRuntimeSpawnedDeliveryResolver(
                checkedProjectiles,
                checkedPersistentFields, presentation);
        }

        internal void AdvanceDeferredAttacks(float deltaSeconds)
        {
            WolfAttack.Advance(deltaSeconds);
            _areaVolley.Advance(deltaSeconds);
            _immediateAttack.Advance(deltaSeconds);
            _spawnedDeliveries.Advance(deltaSeconds);
            ReturningLight.Advance(deltaSeconds);
        }

        internal void CancelDeferredAttacks() { WolfAttack.Reset(); _areaVolley.Reset(); _immediateAttack.Reset(); _spawnedDeliveries.Reset(); }

        internal void SetCommanderPosition(Vector3 position)
        {
            _commanderPosition = position;
            _elapsedSeconds = Time.time;
        }

        internal void Reset()
        {
            WolfAttack.Reset();
            _spawnedDeliveries.Reset();
            ReturningLight.Cancel();
            _returnPath.Reset();
            _returningAttack.CancelReturningFlights();
            _areaVolley.Reset();
            _immediateAttack.Reset();
            _chainAttack.Reset();
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

        public void ResolveReturnPath(string squadId, string companionId, in CompanionReturnSegment segment)
            => _returnPath.Resolve(squadId, companionId, in segment, _commanderPosition);

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
                    return _immediateAttack.Resolve(in intent, effect, source, target, in _commanderPosition, damage, attribution, false, combatModifiers);
                case AttackDelivery.Area:
                    return _immediateAttack.Resolve(in intent, effect, source, target, in _commanderPosition, damage, attribution, true, combatModifiers);
                case AttackDelivery.Chain:
                    return _chainAttack.Resolve(
                        in intent,
                        effect,
                        source,
                        damage,
                        ownerId,
                        attribution,
                        combatModifiers);
                case AttackDelivery.Projectile:
                    if (_data.GetCompanionRoster(intent.SourceCompanionId)?.PrimaryAction == CompanionPrimaryActionKind.ReturningLight)
                        return ReturningLight.Launch(in intent, effect, source, target, damage, combatModifiers);
                    return _spawnedDeliveries.ResolveProjectile(
                        in intent,
                        effect,
                        source,
                        target,
                        damage,
                        attribution,
                        combatModifiers.ProjectileSpeedMultiplier,
                        CreateStatusPayload(intent.SourceCompanionId, ownerId, effect, combatModifiers),
                        combatModifiers.ProjectileCount,
                        combatModifiers.ProjectilePierceBonus,
                        combatModifiers.PenetrationDamageStep,
                        combatModifiers.ExtraHitCount);
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
                    return _returningAttack.Resolve(
                        in intent,
                        effect);
                case AttackDelivery.OwnedProxy:
                    return ResolveOwnedProxy(
                        in intent,
                        effect,
                        source,
                        Mathf.Max(1, Mathf.RoundToInt(intent.SourceMagnitude * combatModifiers.DamageMultiplier)),
                        attribution,
                        combatModifiers);
                default:
                    return new EffectResolution(false, intent.EffectId, 0.0f, 0);
            }
        }

        private EffectResolution ResolveOwnedProxy(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            int damage,
            CountableKillAttribution attribution,
            CompanionPassiveCombatModifiers modifiers)
        {
            bool launched = WolfAttack.Launch(effect, source, damage, attribution, modifiers);
            return new EffectResolution(launched, effect.Id, launched ? damage : 0f, launched ? 1 : 0);
        }

        private EffectResolution ResolveCommanderHeal(
            in EffectIntent intent,
            CombatEffectData effect,
            float healMultiplier)
        {
            CommanderActor commander = _registry.Player;
            if (commander == null
                || !commander.isActiveAndEnabled
                || commander.MaxHp <= 0
                || commander.Hp <= 0)
            {
                return new EffectResolution(false, effect.Id, 0.0f, 0);
            }

            int requested = Mathf.Max(1, Mathf.RoundToInt(intent.SourceMagnitude * healMultiplier));
            int before = commander.Hp;
            commander.Heal(requested);
            int actual = commander.Hp - before;
            commander.NotifyHealingResolved(actual);
            return new EffectResolution(true, effect.Id, actual, actual > 0 ? 1 : 0);
        }

        private CompanionPassiveCombatModifiers ResolveModifiers(string companionId)
        {
            return _modifiers == null
                ? CompanionPassiveCombatModifiers.Identity
                : _modifiers.Resolve(companionId);
        }



        internal static CombatStatusPayload CreateStatusPayload(
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

            return new CombatStatusPayload(
                effect.StatusKind,
                new CompanionStatusSource(companionId, ownerId),
                modifiers.ResolveStatusMagnitude(effect.StatusKind, effect.StatusMagnitude),
                effect.StatusDuration * modifiers.StatusDurationMultiplier);
        }

        private static void ApplyStatus(
            EnemyActor enemy,
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
                modifiers.ResolveStatusMagnitude(effect.StatusKind, effect.StatusMagnitude),
                effect.StatusDuration * modifiers.StatusDurationMultiplier,
                Time.time);
        }
    }
}
