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
    internal static class CompanionRecordingLineageIds
    {
        static readonly string[] Canonical =
        {
            "shield_guard",
            "sword_soldier",
            "cleric",
            "bombardier",
            "fire_mage",
        };

        internal static string[] CreateCopy()
        {
            return (string[])Canonical.Clone();
        }
    }

    internal readonly struct CompanionRecordingDefinitionInputs
    {
        internal CompanionRecordingDefinitionInputs(
            CombatEffectData primaryEffect,
            CombatEffectData secondaryEffect,
            CompanionPromotionData promotion)
        {
            PrimaryEffect = primaryEffect;
            SecondaryEffect = secondaryEffect;
            Promotion = promotion;
        }

        internal CombatEffectData PrimaryEffect { get; }
        internal CombatEffectData SecondaryEffect { get; }
        internal CompanionPromotionData Promotion { get; }
    }

    internal static class CompanionRecordingDefinitionInputsResolver
    {
        internal static CompanionRecordingDefinitionInputs Resolve(IDataProvider data, string companionId)
        {
            CompanionRosterData roster = data.GetCompanionRoster(companionId)
                ?? throw new InvalidOperationException("Recording companion roster is missing: " + companionId);
            CompanionCombatProfileData profile = data.GetCompanionCombatProfile(companionId);
            string effectId = string.IsNullOrWhiteSpace(profile?.BasicEffectId)
                ? roster.EffectRef
                : profile.BasicEffectId;
            CombatEffectData effect = data.GetCombatEffect(effectId)
                ?? throw new InvalidOperationException("Recording companion effect is missing: " + effectId);
            CombatEffectData secondaryCandidate = string.IsNullOrWhiteSpace(profile?.SecondaryEffectId)
                ? null
                : data.GetCombatEffect(profile.SecondaryEffectId);
            CombatEffectData secondaryEffect = secondaryCandidate != null
                && secondaryCandidate.EffectKind == CombatEffectKind.Heal
                && string.Equals(secondaryCandidate.OwnerUnitId, companionId, StringComparison.Ordinal)
                && secondaryCandidate.BaseValue > 0.0f
                    ? secondaryCandidate
                    : null;
            CompanionPromotionData promotion = data.GetCompanionPromotion(roster.PromotionProfileId)
                ?? throw new InvalidOperationException("Recording companion promotion is missing: " + roster.PromotionProfileId);

            if (!string.Equals(effect.OwnerUnitId, companionId, StringComparison.Ordinal)
                || effect.BaseValue <= 0.0f
                || effect.CastInterval <= 0.0f
                || promotion.RequiredUnitCount != 3
                || promotion.VisualUnitCount != 3
                || promotion.EffectMultiplier <= 0.0f
                || promotion.IntervalMultiplier <= 0.0f)
            {
                throw new InvalidOperationException("Recording companion data is invalid: " + companionId);
            }

            return new CompanionRecordingDefinitionInputs(effect, secondaryEffect, promotion);
        }
    }

    internal static class CompanionRecordingDeliveryResolver
    {
        internal static AttackDelivery Resolve(CombatDeliveryKind delivery, string companionId)
        {
            return delivery switch
            {
                CombatDeliveryKind.Cone => AttackDelivery.Direct,
                CombatDeliveryKind.Projectile => AttackDelivery.Projectile,
                CombatDeliveryKind.Circle => AttackDelivery.Area,
                CombatDeliveryKind.Field => AttackDelivery.SpawnedActor,
                _ => throw new InvalidOperationException(
                    "Recording companion delivery is unsupported: " + companionId + ":" + delivery),
            };
        }
    }

    internal static class CompanionRecordingPresentationCueResolver
    {
        internal static string Resolve(
            string effectId,
            AttackDelivery delivery,
            bool promoted,
            string companionId)
        {
            if (promoted && string.Equals(companionId, "sword_soldier", StringComparison.Ordinal))
            {
                return CompanionPresentationCueIds.TravelingForward;
            }

            return delivery == AttackDelivery.Area
                ? CompanionPresentationCueIds.TravelingArea
                : effectId;
        }
    }

    internal static class CompanionRecordingActionStepFactory
    {
        const float CommanderRelativeSlotRangeAllowance = 1.10f;
        const float SwordExcursionActionDuration = 0.12f;
        const float SwordExcursionSpeed = 7.5f;
        const float SwordExcursionStandOff = 1.35f;
        const float SwordExcursionLateral = 0.30f;

        internal static ActionStep CreateBase(
            CombatEffectData effect,
            AttackDelivery delivery,
            string companionId)
        {
            bool isSword = IsSword(companionId);
            CombatMotion motion = isSword ? CombatMotion.Excursion : CombatMotion.Stationary;
            return new ActionStep(
                motion,
                delivery,
                effect.Id,
                effect.BaseValue,
                CompanionRecordingPresentationCueResolver.Resolve(effect.Id, delivery, false, companionId),
                isSword ? SwordExcursionActionDuration : 0.0f,
                isSword ? SwordExcursionSpeed : 0.0f,
                Mathf.Max(0.0f, effect.CastDelay),
                isSword ? SwordExcursionStandOff : 0.0f,
                isSword ? SwordExcursionLateral : 0.0f,
                ResolveTargetAcquisitionRange(effect));
        }

        internal static ActionStep CreatePromoted(
            CombatEffectData effect,
            AttackDelivery delivery,
            string companionId,
            float magnitudeMultiplier)
        {
            bool isSword = IsSword(companionId);
            return new ActionStep(
                CombatMotion.Stationary,
                delivery,
                effect.Id,
                effect.BaseValue * magnitudeMultiplier,
                CompanionRecordingPresentationCueResolver.Resolve(effect.Id, delivery, true, companionId),
                isSword ? SwordExcursionActionDuration : 0.0f,
                isSword ? SwordExcursionSpeed : 0.0f,
                Mathf.Max(0.0f, effect.CastDelay),
                0.0f,
                0.0f,
                ResolveTargetAcquisitionRange(effect));
        }

        internal static ActionStep CreateSecondaryHeal(CombatEffectData effect, float magnitudeMultiplier)
        {
            return new ActionStep(
                CombatMotion.Stationary,
                CompanionRecordingDeliveryResolver.Resolve(effect.DeliveryKind, effect.OwnerUnitId),
                effect.Id,
                effect.BaseValue * magnitudeMultiplier,
                effect.Id,
                0.0f,
                0.0f,
                Mathf.Max(0.0f, effect.CastDelay));
        }

        static float ResolveTargetAcquisitionRange(CombatEffectData effect)
        {
            return Mathf.Max(0.0f, effect.Range) + CommanderRelativeSlotRangeAllowance;
        }

        static bool IsSword(string companionId)
        {
            return string.Equals(companionId, "sword_soldier", StringComparison.Ordinal);
        }
    }

    public sealed class CompanionRecordingDefinitionCatalog : ICompanionDefinitionCatalog
    {
        private readonly Dictionary<string, CompanionDefinition> _definitions =
            new Dictionary<string, CompanionDefinition>(StringComparer.Ordinal);
        private readonly IReadOnlyList<string> _lineageIds;

        public CompanionRecordingDefinitionCatalog(IDataProvider data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string[] copiedIds = CompanionRecordingLineageIds.CreateCopy();
            _lineageIds = Array.AsReadOnly(copiedIds);
            for (int index = 0; index < copiedIds.Length; index += 1)
            {
                string companionId = copiedIds[index];
                _definitions.Add(companionId, CreateDefinition(data, companionId));
            }
        }

        public IReadOnlyList<string> LineageIds => _lineageIds;

        public bool TryGetDefinition(string companionId, out CompanionDefinition definition)
        {
            return _definitions.TryGetValue(companionId ?? string.Empty, out definition);
        }

        private static CompanionDefinition CreateDefinition(IDataProvider data, string companionId)
        {
            CompanionRecordingDefinitionInputs inputs =
                CompanionRecordingDefinitionInputsResolver.Resolve(data, companionId);
            CombatEffectData effect = inputs.PrimaryEffect;
            CombatEffectData secondaryEffect = inputs.SecondaryEffect;
            CompanionPromotionData promotion = inputs.Promotion;
            AttackDelivery delivery = CompanionRecordingDeliveryResolver.Resolve(effect.DeliveryKind, companionId);
            ActionStep baseStep = CompanionRecordingActionStepFactory.CreateBase(effect, delivery, companionId);
            List<ActionStep> baseSteps = new List<ActionStep>(2) { baseStep };
            if (secondaryEffect != null)
            {
                baseSteps.Add(CompanionRecordingActionStepFactory.CreateSecondaryHeal(secondaryEffect, 1.0f));
            }
            ActionSet baseSet = new ActionSet(companionId + "-base", effect.CastInterval, baseSteps);

            float promotedCooldown = effect.CastInterval * promotion.IntervalMultiplier;
            ActionStep promotedStep = CompanionRecordingActionStepFactory.CreatePromoted(
                effect,
                delivery,
                companionId,
                promotion.EffectMultiplier);
            List<ActionStep> promotedSteps = new List<ActionStep>(2) { promotedStep };
            if (!string.Equals(companionId, "sword_soldier", StringComparison.Ordinal)
                && secondaryEffect != null)
            {
                promotedSteps.Add(CompanionRecordingActionStepFactory.CreateSecondaryHeal(
                    secondaryEffect,
                    promotion.EffectMultiplier));
            }
            ActionSet promotedSet = new ActionSet(companionId + "-promoted", promotedCooldown, promotedSteps);

            return new CompanionDefinition(companionId, baseSet, promotedSet);
        }

    }

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

    internal sealed class CompanionRecordingCombatWorld : ICompanionCombatWorld, IRangedCompanionTargetWorld
    {
        private const float DefaultProjectileSpeed = 7.0f;
        private const float DefaultProjectileLifetime = 2.0f;

        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatProjectileModule _projectiles;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly ICombatPersistentFieldModule _persistentFields;
        private readonly List<MonsterController> _targets = new List<MonsterController>(16);
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
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _persistentFields = persistentFields ?? throw new ArgumentNullException(nameof(persistentFields));
        }

        internal void SetCommanderPosition(Vector3 position)
        {
            _commanderPosition = position;
            _elapsedSeconds = Time.time;
        }

        internal void Reset()
        {
            _targets.Clear();
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
            MonsterController selected = null;
            float selectedDistance = float.PositiveInfinity;
            long selectedSequence = long.MaxValue;
            Vector3 worldOrigin = new Vector3(origin.X, origin.Y, 0.0f);
            float maxRangeSquared = maxRange > 0.0f ? maxRange * maxRange : float.PositiveInfinity;
            foreach (MonsterController candidate in _registry.Enemies)
            {
                if (!IsValidTarget(candidate))
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
            int ownerId = StableOwnerId(intent.SquadId);
            CountableKillAttribution attribution = new CountableKillAttribution(
                ownerId,
                intent.SourceCompanionId,
                CombatKillSourceCategory.CompanionOwnedAction);

            switch (intent.Delivery)
            {
                case AttackDelivery.Direct:
                    return ResolveImmediate(in intent, effect, source, target, damage, attribution, false);
                case AttackDelivery.Area:
                    return ResolveImmediate(in intent, effect, source, target, damage, attribution, true);
                case AttackDelivery.Projectile:
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
                        CompanionRecordingPresentationHost.PresentRecordingVideoEffect(
                            effect.Id,
                            intent.PresentationCueId,
                            source,
                            target,
                            direction,
                            effect.Range,
                            effect.Radius,
                            intent.MemberOrder);
                    return new EffectResolution(spawned, effect.Id, spawned ? damage : 0.0f, spawned ? 1 : 0);
                }
                case AttackDelivery.SpawnedActor:
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
                        _elapsedSeconds);
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
            CollectImmediateTargets(effect, source, target, area);
            int affected = 0;
            Vector3 forward = target - source;
            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();

            int maxTargets = effect.AffectsAllTargetsInShape
                ? _targets.Count
                : Mathf.Max(1, effect.MaxTargets);
            for (int index = 0; index < _targets.Count && affected < maxTargets; index += 1)
            {
                MonsterController enemy = _targets[index];
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

        private void CollectImmediateTargets(CombatEffectData effect, Vector3 source, Vector3 target, bool area)
        {
            _targets.Clear();
            float radius = area ? Mathf.Max(0.01f, effect.Radius) : Mathf.Max(0.01f, effect.Range);
            float radiusSquared = radius * radius;
            Vector3 forward = target - source;
            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();

            foreach (MonsterController candidate in _registry.Enemies)
            {
                if (!IsValidTarget(candidate))
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
        }

        private static bool IsValidTarget(MonsterController target)
        {
            return target != null && target.isActiveAndEnabled && target.Hp > 0;
        }

        private static int StableOwnerId(string squadId)
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
}
