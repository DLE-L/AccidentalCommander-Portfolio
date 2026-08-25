using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRecordingCombatWorld : ICompanionCombatWorld, IRangedCompanionTargetWorld
    {
        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly CompanionRecordingSpawnedDeliveryResolver _spawnedDeliveries;
        private readonly CompanionRecordingImmediateTargetCollector _immediateTargets =
            new CompanionRecordingImmediateTargetCollector();
        private Vector3 _commanderPosition;
        private float _elapsedSeconds;

        internal CompanionRecordingCombatWorld(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            ICombatProjectileModule checkedProjectiles = projectiles
                ?? throw new ArgumentNullException(nameof(projectiles));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            ICombatPersistentFieldModule checkedPersistentFields = persistentFields
                ?? throw new ArgumentNullException(nameof(persistentFields));
            _spawnedDeliveries = new CompanionRecordingSpawnedDeliveryResolver(
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
            return CompanionRecordingTargetSelector.TrySelect(
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
                return ResolveCommanderHeal(in intent, effect);
            }

            Vector3 source = new Vector3(intent.SourcePosition.X, intent.SourcePosition.Y, 0.0f);
            Vector3 target = new Vector3(intent.TargetPosition.X, intent.TargetPosition.Y, 0.0f);
            int damage = Mathf.Max(1, Mathf.RoundToInt(intent.SourceMagnitude));
            CompanionRecordingEffectOwnership ownership = CompanionRecordingEffectAttribution.Resolve(in intent);
            int ownerId = ownership.OwnerId;
            CountableKillAttribution attribution = ownership.KillAttribution;

            switch (intent.Delivery)
            {
                case AttackDelivery.Direct:
                    return ResolveImmediate(in intent, effect, source, target, damage, attribution, false);
                case AttackDelivery.Area:
                    return ResolveImmediate(in intent, effect, source, target, damage, attribution, true);
                case AttackDelivery.Projectile:
                    return _spawnedDeliveries.ResolveProjectile(
                        in intent,
                        effect,
                        source,
                        target,
                        damage,
                        attribution);
                case AttackDelivery.SpawnedActor:
                    return _spawnedDeliveries.ResolvePersistentField(
                        in intent,
                        effect,
                        source,
                        target,
                        damage,
                        ownerId,
                        _elapsedSeconds);
                default:
                    return new EffectResolution(false, intent.EffectId, 0.0f, 0);
            }
        }

        private EffectResolution ResolveCommanderHeal(in EffectIntent intent, CombatEffectData effect)
        {
            PlayerController commander = _registry.Player;
            if (commander == null
                || !commander.isActiveAndEnabled
                || commander.MaxHp <= 0
                || commander.Hp <= 0)
            {
                return new EffectResolution(false, effect.Id, 0.0f, 0);
            }

            int requested = Mathf.Max(1, Mathf.RoundToInt(intent.SourceMagnitude));
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
            bool area)
        {
            IReadOnlyList<MonsterController> targets = _immediateTargets.Collect(
                _registry.Enemies,
                effect,
                source,
                target,
                area);
            int affected = 0;
            Vector3 forward = target - source;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
            }

            int maxTargets = effect.AffectsAllTargetsInShape
                ? targets.Count
                : Mathf.Max(1, effect.MaxTargets);
            for (int index = 0; index < targets.Count && affected < maxTargets; index += 1)
            {
                MonsterController enemy = targets[index];
                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    intent.SourceCompanionId,
                    enemy,
                    source,
                    enemy.transform.position,
                    damage,
                    area ? AttackVisualKind.AreaHit : AttackVisualKind.ForwardSlash,
                    false,
                    attribution,
                    effect.Id)))
                {
                    affected += 1;
                    if (effect.Push > 0.0f)
                    {
                        enemy.ApplySmoothKnockback(forward, effect.Push);
                    }
                }
            }

            CompanionRecordingEffectPresenter.Present(
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
    }
}
