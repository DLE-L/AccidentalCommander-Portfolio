using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRecordingHostState : ICompanionRunClock
    {
        long _advanceSequence;

        public bool IsPaused { get; private set; }
        internal bool IsDisposed { get; private set; }

        internal long NextAdvanceSequence()
        {
            _advanceSequence += 1L;
            return _advanceSequence;
        }

        internal void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
        }

        internal void Reset()
        {
            _advanceSequence = 0L;
            IsPaused = false;
        }

        internal bool TryDispose()
        {
            if (IsDisposed)
            {
                return false;
            }

            IsDisposed = true;
            return true;
        }
    }

    public sealed class CompanionRecordingProductionHost : IDisposable
    {
        private readonly CompanionRecordingHostState _state;
        private readonly CompanionRecordingCombatWorld _world;
        private readonly CompanionRecordingPresentationHost _presentation;

        public CompanionRecordingProductionHost(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields,
            CompanionRuntimePresentationSet presentationSet)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (presentationSet == null)
                throw new ArgumentNullException(nameof(presentationSet));

            _state = new CompanionRecordingHostState();
            CompanionRecordingDefinitionCatalog definitions = new CompanionRecordingDefinitionCatalog(data);
            _world = new CompanionRecordingCombatWorld(
                data,
                registry,
                projectiles,
                immediateHits,
                persistentFields);
            Module = new CompanionRunModule(new RunCombatContext(0xC3F1A6EUL, definitions, _world, _state));
            Adapter = new CompanionRunExternalAdapter(Module, data);
            _presentation = new CompanionRecordingPresentationHost(Adapter, presentationSet);
        }

        public CompanionRunModule Module { get; }

        public CompanionRunExternalAdapter Adapter { get; }

        public ICompanionCardInput CardInput => Adapter;

        public static bool IsRecordingProfile(CardPoolDefinition pool)
        {
            return pool != null
                && string.Equals(pool.ProfileId, CardPoolProfileIds.Recording, StringComparison.Ordinal);
        }

        public void Advance(float deltaSeconds, bool isPaused, Transform commander)
        {
            if (_state.IsDisposed || commander == null || deltaSeconds <= 0.0f)
                return;

            Vector3 position = commander.position;
            _state.SetPaused(isPaused);
            _world.SetCommanderPosition(position);
            Module.Advance(new CompanionAdvanceRequest(
                _state.NextAdvanceSequence(),
                deltaSeconds,
                new CompanionPoint(position.x, position.y)));
            _presentation.Consume(commander, deltaSeconds);
        }

        public void Reset()
        {
            if (_state.IsDisposed)
                return;

            Module.Reset();
            _state.Reset();
            _world.Reset();
            _presentation.Reset();
        }

        public void StopForResult()
        {
            if (_state.IsDisposed)
                return;

            _state.SetPaused(true);
            Module.CancelActiveActions();
            _presentation.Reset();
        }

        public void Dispose()
        {
            if (_state.TryDispose() == false)
                return;

            _presentation.Dispose();
            Module.Dispose();
        }
    }

    internal static class CompanionRecordingTargetSelector
    {
        internal static bool TrySelect(
            IReadOnlyCollection<MonsterController> candidates,
            CompanionPoint origin,
            float maxRange,
            out CompanionPoint targetPosition)
        {
            MonsterController selected = null;
            float selectedDistance = float.PositiveInfinity;
            long selectedSequence = long.MaxValue;
            Vector3 worldOrigin = new Vector3(origin.X, origin.Y, 0.0f);
            float maxRangeSquared = maxRange > 0.0f ? maxRange * maxRange : float.PositiveInfinity;
            foreach (MonsterController candidate in candidates)
            {
                if (!IsValid(candidate))
                    continue;

                float distance = (candidate.transform.position - worldOrigin).sqrMagnitude;
                if (distance > maxRangeSquared)
                    continue;
                if (distance < selectedDistance
                    || (Mathf.Approximately(distance, selectedDistance)
                        && candidate.SpawnSequence < selectedSequence))
                {
                    selected = candidate;
                    selectedDistance = distance;
                    selectedSequence = candidate.SpawnSequence;
                }
            }

            if (selected == null)
            {
                targetPosition = default;
                return false;
            }

            Vector3 position = selected.transform.position;
            targetPosition = new CompanionPoint(position.x, position.y);
            return true;
        }

        internal static bool IsValid(MonsterController target)
        {
            return target != null && target.isActiveAndEnabled && target.Hp > 0;
        }
    }

    internal readonly struct CompanionRecordingEffectOwnership
    {
        internal CompanionRecordingEffectOwnership(
            int ownerId,
            CountableKillAttribution killAttribution)
        {
            OwnerId = ownerId;
            KillAttribution = killAttribution;
        }

        internal int OwnerId { get; }
        internal CountableKillAttribution KillAttribution { get; }
    }

    internal static class CompanionRecordingEffectAttribution
    {
        internal static CompanionRecordingEffectOwnership Resolve(in EffectIntent intent)
        {
            int ownerId = StableOwnerId(intent.SquadId);
            return new CompanionRecordingEffectOwnership(
                ownerId,
                new CountableKillAttribution(
                    ownerId,
                    intent.SourceCompanionId,
                    CombatKillSourceCategory.CompanionOwnedAction));
        }

        static int StableOwnerId(string squadId)
        {
            unchecked
            {
                int hash = 17;
                if (squadId != null)
                {
                    for (int index = 0; index < squadId.Length; index += 1)
                        hash = hash * 31 + squadId[index];
                }
                return hash == 0 ? 1 : hash;
            }
        }
    }

    internal sealed class CompanionRecordingImmediateTargetCollector
    {
        readonly List<MonsterController> _targets = new List<MonsterController>(16);

        internal IReadOnlyList<MonsterController> Collect(
            IReadOnlyCollection<MonsterController> candidates,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            bool area)
        {
            _targets.Clear();
            float radius = area ? Mathf.Max(0.01f, effect.Radius) : Mathf.Max(0.01f, effect.Range);
            float radiusSquared = radius * radius;
            Vector3 forward = target - source;
            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();

            foreach (MonsterController candidate in candidates)
            {
                if (!CompanionRecordingTargetSelector.IsValid(candidate))
                    continue;

                Vector3 origin = area ? target : source;
                Vector3 delta = candidate.transform.position - origin;
                if (delta.sqrMagnitude > radiusSquared)
                    continue;
                if (!area && effect.Angle > 0.0f && Vector3.Angle(forward, delta) > effect.Angle * 0.5f)
                    continue;
                _targets.Add(candidate);
            }

            _targets.Sort((left, right) =>
            {
                Vector3 origin = area ? target : source;
                int distanceOrder = (left.transform.position - origin).sqrMagnitude.CompareTo(
                    (right.transform.position - origin).sqrMagnitude);
                return distanceOrder != 0
                    ? distanceOrder
                    : left.SpawnSequence.CompareTo(right.SpawnSequence);
            });
            return _targets;
        }

        internal void Reset()
        {
            _targets.Clear();
        }
    }

    internal sealed class CompanionRecordingSpawnedDeliveryResolver
    {
        const float DefaultProjectileSpeed = 7.0f;
        const float DefaultProjectileLifetime = 2.0f;

        readonly ICombatProjectileModule _projectiles;
        readonly ICombatPersistentFieldModule _persistentFields;

        internal CompanionRecordingSpawnedDeliveryResolver(
            ICombatProjectileModule projectiles,
            ICombatPersistentFieldModule persistentFields)
        {
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _persistentFields = persistentFields ?? throw new ArgumentNullException(nameof(persistentFields));
        }

        internal EffectResolution ResolveProjectile(
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
            bool spawned = _projectiles.TrySpawn(CombatProjectileRequest.CreateStraight(
                intent.SourceCompanionId,
                null,
                source,
                direction,
                damage,
                DefaultProjectileSpeed,
                effect.ProjectileLifetime > 0.0f ? effect.ProjectileLifetime : DefaultProjectileLifetime,
                RetroVfxKind.None,
                CombatProjectileFaction.Ally,
                attribution,
                Mathf.Max(1, effect.MaxTargets),
                presentationId: effect.Id));
            if (spawned)
            {
                CompanionRecordingPresentationHost.PresentRecordingVideoEffect(
                    effect.Id,
                    intent.PresentationCueId,
                    source,
                    target,
                    direction,
                    effect.Range,
                    effect.Radius,
                    intent.MemberOrder);
            }
            return new EffectResolution(spawned, effect.Id, spawned ? damage : 0.0f, spawned ? 1 : 0);
        }

        internal EffectResolution ResolvePersistentField(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            int damage,
            int ownerId,
            float elapsedSeconds)
        {
            bool spawned = _persistentFields.TrySpawn(
                CombatPersistentFieldRequest.CreateAllyDamage(
                    intent.SourceCompanionId,
                    effect.Id,
                    ownerId,
                    target,
                    damage,
                    Mathf.Max(0.01f, effect.Radius),
                    Mathf.Max(0.01f, effect.TickInterval),
                    Mathf.Max(0.01f, effect.Duration),
                    Mathf.Max(1, effect.MaxTargets),
                    Mathf.Max(1, effect.MaxActiveCount)),
                elapsedSeconds);
            if (spawned)
            {
                CompanionRecordingPresentationHost.PresentRecordingVideoEffect(
                    effect.Id,
                    intent.PresentationCueId,
                    source,
                    target,
                    target - source,
                    effect.Range,
                    effect.Radius,
                    intent.MemberOrder);
            }
            return new EffectResolution(spawned, effect.Id, spawned ? damage : 0.0f, spawned ? 1 : 0);
        }
    }

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
                return new EffectResolution(false, intent.EffectId, 0.0f, 0);

            if (effect.EffectKind == CombatEffectKind.Heal)
                return ResolveCommanderHeal(in intent, effect);

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
            if (commander == null || !commander.isActiveAndEnabled || commander.MaxHp <= 0 || commander.Hp <= 0)
                return new EffectResolution(false, effect.Id, 0.0f, 0);

            int requested = Mathf.Max(1, Mathf.RoundToInt(intent.SourceMagnitude));
            int before = commander.Hp;
            commander.Hp = Mathf.Min(commander.MaxHp, commander.Hp + requested);
            int actual = commander.Hp - before;
            if (actual > 0)
                FloatingDamageText.ShowHeal(commander.transform.position, actual);
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
                forward.Normalize();

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
                        enemy.ApplySmoothKnockback(forward, effect.Push);
                }
            }

            CompanionRecordingPresentationHost.PresentRecordingVideoEffect(
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
