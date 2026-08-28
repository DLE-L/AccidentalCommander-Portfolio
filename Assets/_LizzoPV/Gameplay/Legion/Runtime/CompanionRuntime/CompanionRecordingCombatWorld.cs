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
using Lizzo.PV.Flow;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRecordingCombatWorld : ICompanionCombatWorld,
        IRangedCompanionTargetWorld,
        ICompanionTargetReservationWorld,
        ICompanionAdvanceScopeWorld
    {
        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly CompanionRecordingSpawnedDeliveryResolver _spawnedDeliveries;
        private readonly CompanionRecordingImmediateTargetCollector _immediateTargets =
            new CompanionRecordingImmediateTargetCollector();
        private readonly List<TargetAreaImpactCandidate> _specialCandidates =
            new List<TargetAreaImpactCandidate>(16);
        private readonly List<TargetAreaImpactCandidate> _specialTargets =
            new List<TargetAreaImpactCandidate>(8);
        private readonly HashSet<int> _specialVisitedTargets = new HashSet<int>();
        private readonly HashSet<int> _reservedCompanionTargets = new HashSet<int>();
        private readonly CompanionShieldPushDeduplicator _shieldPushDeduplicator =
            new CompanionShieldPushDeduplicator();
        private Vector3 _commanderPosition;
        private float _elapsedSeconds;
        private readonly bool _usesBaselineCombat;

        internal CompanionRecordingCombatWorld(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields,
            RunDefinition definition)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            ICombatProjectileModule checkedProjectiles = projectiles
                ?? throw new ArgumentNullException(nameof(projectiles));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _usesBaselineCombat = (definition ?? throw new ArgumentNullException(nameof(definition)))
                .UsesBaselineCombatProfile;
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
            _specialCandidates.Clear();
            _specialTargets.Clear();
            _specialVisitedTargets.Clear();
            _reservedCompanionTargets.Clear();
            _shieldPushDeduplicator.BeginAdvance();
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

        public void BeginCompanionAdvance()
        {
            _shieldPushDeduplicator.BeginAdvance();
        }

        public void BeginTargetReservationScope()
        {
            _reservedCompanionTargets.Clear();
        }

        public void ReserveTargetPosition(CompanionPoint targetPosition)
        {
            if (CompanionRecordingTargetSelector.TrySelect(
                    _registry.Enemies,
                    targetPosition,
                    0.01f,
                    null,
                    out _,
                    out int selectedTargetId))
            {
                _reservedCompanionTargets.Add(selectedTargetId);
            }
        }

        public bool TrySelectUnreservedTargetPosition(
            CompanionPoint origin,
            float maxRange,
            out CompanionPoint targetPosition)
        {
            if (CompanionRecordingTargetSelector.TrySelect(
                    _registry.Enemies,
                    origin,
                    maxRange,
                    _reservedCompanionTargets,
                    out targetPosition,
                    out int selectedTargetId))
            {
                _reservedCompanionTargets.Add(selectedTargetId);
                return true;
            }

            return TrySelectTargetPosition(origin, maxRange, out targetPosition);
        }

        public EffectResolution Resolve(in EffectIntent intent)
        {
            CombatEffectData effect = _data.GetCombatEffect(intent.EffectId);
            if (_usesBaselineCombat && effect != null)
            {
                effect = effect.EffectKind == CombatEffectKind.Heal
                    ? TutorialCompanionCombatBaseline.ResolveSecondary(effect, intent.SourceCompanionId)
                    : TutorialCompanionCombatBaseline.ResolvePrimary(effect, intent.SourceCompanionId);
            }
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
                case AttackDelivery.ReturningProjectile:
                    return ResolveReturningProjectile(
                        in intent,
                        effect,
                        source,
                        target,
                        damage,
                        attribution);
                case AttackDelivery.OwnedProxy:
                    return ResolveOwnedProxy(
                        in intent,
                        effect,
                        source,
                        damage,
                        attribution);
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
            CountableKillAttribution attribution)
        {
            Vector3 direction = target - source;
            if (direction.sqrMagnitude <= 0.0001f)
                return new EffectResolution(false, intent.EffectId, 0.0f, 0);

            direction.Normalize();
            Vector3 end = source + direction * Mathf.Max(0.01f, effect.Range);
            ReturningAttackPass pass = intent.ChainDepth > 0
                ? ReturningAttackPass.Return
                : ReturningAttackPass.Outbound;
            CollectSpecialCandidates();
            CompanionReturningAttackTargetSelector.Collect(
                _specialCandidates,
                source,
                end,
                Mathf.Max(0.01f, effect.Radius),
                Mathf.Max(1, effect.MaxTargets),
                pass,
                _specialTargets);

            int affected = 0;
            for (int index = 0; index < _specialTargets.Count; index += 1)
            {
                MonsterController enemy = _specialTargets[index].Target;
                if (CompanionRecordingTargetSelector.IsValid(enemy)
                    && _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                        intent.SourceCompanionId,
                        enemy,
                        pass == ReturningAttackPass.Outbound ? source : end,
                        enemy.transform.position,
                        damage,
                        AttackVisualKind.SingleHit,
                        false,
                        attribution,
                        effect.Id)))
                {
                    affected += 1;
                }
            }

            Vector3 visualSource = pass == ReturningAttackPass.Outbound ? source : end;
            Vector3 visualTarget = pass == ReturningAttackPass.Outbound ? end : source;
            CompanionRecordingEffectPresenter.Present(
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
                        Mathf.Max(0.01f, effect.Duration)),
                }
                : Array.Empty<IndependentEffectRequest>();
            return new EffectResolution(
                affected > 0,
                effect.Id,
                affected > 0 ? damage : 0.0f,
                affected,
                followUps);
        }

        private EffectResolution ResolveOwnedProxy(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            int damage,
            CountableKillAttribution attribution)
        {
            CollectSpecialCandidates();
            _specialVisitedTargets.Clear();
            Vector3 chainOrigin = source;
            Vector3 firstTarget = source;
            int affected = 0;
            int chainLimit = Mathf.Max(1, effect.TriggerCount);
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
                if (!CompanionRecordingTargetSelector.IsValid(enemy))
                    continue;

                if (affected == 0)
                    firstTarget = selected.Point;
                chainOrigin = selected.Point;
                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    intent.SourceCompanionId,
                    enemy,
                    source,
                    enemy.transform.position,
                    damage,
                    AttackVisualKind.SingleHit,
                    false,
                    attribution,
                    effect.Id)))
                {
                    affected += 1;
                }

                if (CompanionRecordingTargetSelector.IsValid(enemy))
                    break;
            }

            CompanionRecordingEffectPresenter.Present(
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
                if (!CompanionRecordingTargetSelector.IsValid(enemy))
                    continue;

                _specialCandidates.Add(new TargetAreaImpactCandidate(
                    enemy,
                    enemy.transform.position,
                    enemy.GetInstanceID()));
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
                    float push = ResolvePush(effect, intent.SourceCompanionId, enemy);
                    if (push > 0.0f
                        && _shieldPushDeduplicator.ShouldApply(
                            _usesBaselineCombat,
                            intent.SourceCompanionId,
                            enemy.GetInstanceID()))
                    {
                        enemy.ApplySmoothKnockback(forward, push);
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

        private float ResolvePush(CombatEffectData effect, string companionId, MonsterController enemy)
        {
            if (!_usesBaselineCombat
                || !string.Equals(companionId, "shield_guard", StringComparison.Ordinal))
            {
                return effect.Push;
            }

            EnemyData enemyData = enemy?.RuntimeStats?.Data;
            if (enemyData == null || string.Equals(enemyData.Type, "boss", StringComparison.Ordinal))
                return 0.0f;
            return string.Equals(enemyData.Id, "shield_orc", StringComparison.Ordinal) ? 0.5f : 1.0f;
        }
    }

    internal sealed class CompanionShieldPushDeduplicator
    {
        private readonly HashSet<int> _pushedTargetIds = new HashSet<int>();

        internal void BeginAdvance()
        {
            _pushedTargetIds.Clear();
        }

        internal bool ShouldApply(bool isTutorial, string companionId, int targetId)
        {
            return !isTutorial
                || !string.Equals(companionId, "shield_guard", StringComparison.Ordinal)
                || _pushedTargetIds.Add(targetId);
        }
    }
}
