using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class CompanionSynergyProductionHost : IDisposable
    {
        const float PeriodicTriggerSeconds = 6.0f;
        const int CounterThreshold = 3;

        readonly IDataProvider _data;
        readonly RuntimeObjectRegistry _registry;
        readonly CombatImmediateHitModule _hits;
        readonly CompanionRuntimeProductionHost _companions;
        readonly CanonicalCompanionCastStream _casts;
        readonly RunCombatTelemetry _telemetry;
        readonly PairSynergyDefinitionSet _pairDefinitions;
        readonly TrioSynergyDefinitionSet _trioDefinitions;
        readonly SynergyRuntime _scheduler;
        readonly PairSynergyRuntime _pairs;
        readonly TrioSynergyRuntime _trios;
        readonly SynergyEffectExecutor _effectExecutor;
        readonly Dictionary<string, int> _actionCounters = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly List<MonsterController> _targets = new List<MonsterController>(32);
        long _nextTriggerId = 1;
        float _nextPeriodicTriggerTime;
        Vector3 _fireFieldCenter;
        float _fireFieldRadius;
        float _fireFieldUntil;
        bool _disposed;

        public CompanionSynergyProductionHost(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            CombatImmediateHitModule hits,
            CompanionRuntimeProductionHost companions,
            CanonicalCompanionCastStream casts,
            RunCombatTelemetry telemetry)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _hits = hits ?? throw new ArgumentNullException(nameof(hits));
            _companions = companions ?? throw new ArgumentNullException(nameof(companions));
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));

            _pairDefinitions = PairSynergyDefinitionSet.Combine(
                PairSynergyCatalog.CreateFirstSet(CreatePairBalance()),
                PairSynergyCatalog.CreateSecondSet(CreatePairSecondBalance()));
            _trioDefinitions = TrioSynergyDefinitionSet.Combine(
                TrioSynergyCatalog.CreateFirstSet(CreateTrioBalance()),
                TrioSynergyCatalog.CreateSecondSet(CreateTrioSecondBalance()));

            SynergyDefinition[] definitions = new SynergyDefinition[_pairDefinitions.Count + _trioDefinitions.Count];
            for (int index = 0; index < _pairDefinitions.Count; index++)
                definitions[index] = _pairDefinitions.GetAt(index).RuntimeDefinition;
            for (int index = 0; index < _trioDefinitions.Count; index++)
                definitions[_pairDefinitions.Count + index] = _trioDefinitions.GetAt(index).RuntimeDefinition;

            _scheduler = new SynergyRuntime(new SynergyRuntimeDefinition(3, 2, definitions));
            _pairs = new PairSynergyRuntime(_pairDefinitions, _scheduler);
            _trios = new TrioSynergyRuntime(_trioDefinitions, _scheduler);
            _effectExecutor = new SynergyEffectExecutor(_registry, _hits);
            _casts.Completed += OnCastCompleted;
            _hits.Applied += OnImmediateHitApplied;
            RefreshProgression();
        }

        public int CatalogCount => _pairDefinitions.Count + _trioDefinitions.Count;
        public SynergyRuntimeSnapshot CurrentSnapshot => _scheduler.CreateSnapshot();
        public int ActiveCount
        {
            get
            {
                SynergyRuntimeSnapshot snapshot = _scheduler.CreateSnapshot();
                int active = 0;
                for (int index = 0; index < _pairDefinitions.Count; index++)
                    if (snapshot.GetSynergy(_pairDefinitions.GetAt(index).RuntimeDefinition.SynergyId).IsActive) active++;
                for (int index = 0; index < _trioDefinitions.Count; index++)
                    if (snapshot.GetSynergy(_trioDefinitions.GetAt(index).RuntimeDefinition.SynergyId).IsActive) active++;
                return active;
            }
        }
        public int ResolvedCount { get; private set; }

        public void RefreshProgression()
        {
            if (_disposed)
                return;

            SynergyRuntimeSnapshot before = _scheduler.CreateSnapshot();
            CompanionRunSnapshot snapshot = _companions.Module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index++)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                _scheduler.SetLegionProgression(squad.CompanionId, Mathf.Clamp(squad.MemberCount, 0, 3));
            }

            SynergyRuntimeSnapshot after = _scheduler.CreateSnapshot();
            for (int index = 0; index < after.SynergyCount; index++)
            {
                SynergyStateSnapshot current = after.GetSynergyAt(index);
                if (current.IsActive && !before.GetSynergy(current.SynergyId).IsActive)
                    _telemetry.RecordSynergyActivated(current);
            }
        }

        public void Advance(float deltaTime, float currentTime, bool isPaused, Transform commander)
        {
            if (_disposed || isPaused || deltaTime <= 0.0f || commander == null)
                return;

            _scheduler.Advance(deltaTime);
            if (currentTime < _nextPeriodicTriggerTime)
                return;

            _nextPeriodicTriggerTime = currentTime + PeriodicTriggerSeconds;
            if (TryGetNearest(commander.position, out MonsterController target))
            {
                Vector3 direction = target.transform.position - commander.position;
                if (direction.sqrMagnitude > 0.0001f)
                    direction.Normalize();
                TryExecuteTrio(TrioSynergyTrigger.ForPeriodicDirection(
                    NextTriggerId(),
                    TrioSynergyTriggerKind.GuardPeriodReady,
                    ToRunPoint(direction),
                    CountTargets(commander.position, 3.0f)));
            }

            _trios.TryGrantSanctuaryCharge();
        }

        public bool TryInterceptCommanderDamage(MonsterController attacker)
        {
            if (_disposed || attacker == null)
                return false;
            if (_trios.TryBlockWithSanctuary(
                    NextTriggerId(),
                    StableEntityId(attacker),
                    ToRunPoint(attacker.transform.position),
                    0) == false)
            {
                return false;
            }

            if (_trios.TryStartNext(out TrioSynergyExecutionSnapshot execution) == false)
                return false;

            _telemetry.RecordSynergyExecutionStarted(execution.SynergyId, SynergyTier.Trio, execution.ExecutionId, execution.CasterUnitId, execution.StepCount);
            ExecuteTrio(execution);
            if (_trios.Complete(execution.ExecutionId))
                _telemetry.RecordSynergyExecutionCompleted(execution.SynergyId, SynergyTier.Trio, execution.ExecutionId, execution.StepCount);
            ResolvedCount++;
            return true;
        }

        public void ReportEnemyDeath(
            int entityId,
            Vector3 position,
            in CompanionEnemyDeathStatusSnapshot statuses,
            in CountableKillAttribution attribution)
        {
            if (_disposed || entityId == 0)
                return;

            bool insideFire = IsInsideFire(position);
            if (statuses.WasCursed && insideFire)
                TryExecutePair(PairSynergyTrigger.ForDeathInBaseFire(NextTriggerId(), entityId, ToRunPoint(position), true));
            if (statuses.WasCursed && statuses.WasShocked)
                TryExecutePair(PairSynergyTrigger.ForCursedShockDeath(NextTriggerId(), entityId, ToRunPoint(position), true, true));
            if (statuses.WasCursed && statuses.WasShocked && insideFire)
                TryExecuteTrio(TrioSynergyTrigger.ForCompoundDeath(
                    NextTriggerId(), TrioSynergyTriggerKind.MagicCompoundDeath, ToRunPoint(position), true, true, true));

            bool wolfKill = string.Equals(attribution.SourceId, LegionIds.WolfTamer, StringComparison.Ordinal);
            if (wolfKill && WasRecentlyReady(LegionIds.ShieldGuard) && WasRecentlyReady(LegionIds.SwordSoldier))
            {
                TryExecuteTrio(TrioSynergyTrigger.ForLinkedKill(
                    NextTriggerId(), entityId, ToRunPoint(position), true, true, true));
            }
        }

        public void Reset()
        {
            _actionCounters.Clear();
            _nextTriggerId = 1;
            _nextPeriodicTriggerTime = 0.0f;
            _fireFieldCenter = Vector3.zero;
            _fireFieldRadius = 0.0f;
            _fireFieldUntil = 0.0f;
            _effectExecutor.Reset();
            ResolvedCount = 0;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _casts.Completed -= OnCastCompleted;
            _hits.Applied -= OnImmediateHitApplied;
            _actionCounters.Clear();
            _targets.Clear();
            _effectExecutor.Reset();
        }

        void OnCastCompleted(CanonicalCompanionCastCompleted cast)
        {
            if (_disposed || cast.ActionKind != CanonicalCompanionActionKind.BasicAttack)
                return;

            RecordAction(cast.BaseUnitId);
            Vector3 point = cast.Position + (cast.Direction.sqrMagnitude > 0.0001f ? cast.Direction.normalized : Vector3.right);
            MonsterController target = TryGetNearest(point, out MonsterController nearest) ? nearest : null;
            if (target != null)
                point = target.transform.position;

            switch (cast.BaseUnitId)
            {
                case LegionIds.ShieldGuard:
                    TryExecutePair(PairSynergyTrigger.ForShieldHit(NextTriggerId(), ToRunPoint(point), ForcedMovementOutcome.Applied));
                    TryExecutePair(PairSynergyTrigger.ForShieldReturnStarted(NextTriggerId(), ToRunPoint(point), ForcedMovementOutcome.Applied));
                    break;
                case LegionIds.SwordSoldier:
                    TryExecutePair(PairSynergyTrigger.At(
                        NextTriggerId(), PairSynergyTriggerKind.SwordBasicAreaHit, SynergyTriggerSource.BasicAction,
                        ToRunPoint(point), StableEntityId(target),
                        target != null && target.ResolveCompanionIncomingDamageMultiplier(Time.time) > 1.0f));
                    break;
                case LegionIds.WraithKnight:
                    TryExecutePair(PairSynergyTrigger.At(
                        NextTriggerId(), PairSynergyTriggerKind.WraithBasicHit, SynergyTriggerSource.BasicAction,
                        ToRunPoint(point), StableEntityId(target),
                        target != null && target.ResolveCompanionIncomingDamageMultiplier(Time.time) > 1.0f));
                    break;
                case LegionIds.Cleric:
                    TryExecutePair(PairSynergyTrigger.At(
                        NextTriggerId(), PairSynergyTriggerKind.ClericBasicProjectileHit, SynergyTriggerSource.BasicAction,
                        ToRunPoint(point), StableEntityId(target),
                        isInsideBaseFireField: IsInsideFire(point)));
                    break;
                case LegionIds.FireMage:
                    CaptureFireField(point);
                    break;
                case LegionIds.SkeletonScythe:
                    if (target != null && target.HasCompanionShockFrom(LegionIds.LightningMage, Time.time))
                        TryExecutePair(PairSynergyTrigger.ForScytheOutboundShockHit(NextTriggerId(), StableEntityId(target), ToRunPoint(point)));
                    if (target != null)
                        TryExecutePair(PairSynergyTrigger.ForScytheRoundTripCandidate(NextTriggerId(), StableEntityId(target), ToRunPoint(point), target.IsValid()));
                    break;
                case LegionIds.WolfTamer:
                    if (target != null)
                        TryExecutePair(PairSynergyTrigger.ForWolfChainTransition(NextTriggerId(), StableEntityId(target), ToRunPoint(point), true));
                    break;
                case LegionIds.FalconArcher:
                    if (target != null)
                        TryExecutePair(PairSynergyTrigger.ForArrowCompleted(NextTriggerId(), StableEntityId(target), ToRunPoint(point), 2));
                    break;
                case LegionIds.Bombardier:
                    if (target != null)
                    {
                        bool vulnerable = target.ResolveCompanionIncomingDamageMultiplier(Time.time) > 1.0f;
                        TryExecuteTrio(TrioSynergyTrigger.ForAlchemyBombHit(
                            NextTriggerId(), StableEntityId(target), ToRunPoint(point), vulnerable, IsInsideFire(point)));
                    }
                    break;
            }

            TryExecuteCounterTrios(point);
        }

        void OnImmediateHitApplied(CombatImmediateHitRequest request)
        {
            if (_disposed || request.Mode != CombatImmediateHitMode.EnemyContact || request.EnemySource == null)
                return;

            MonsterController attacker = request.EnemySource;
            if (attacker.ConsumeLastOutgoingDamageWeakeningMarker())
            {
                TryExecutePair(PairSynergyTrigger.ForCommanderDamage(
                    NextTriggerId(), StableEntityId(attacker), StableEntityId(_registry.Player), request.Damage, true));
            }

        }

        void TryExecuteCounterTrios(Vector3 point)
        {
            TryCounterTrio(TrioSynergyTriggerKind.RangedCountersReady, point,
                LegionIds.FalconArcher, LegionIds.Bombardier, LegionIds.SkeletonScythe);
            TryCounterTrio(TrioSynergyTriggerKind.TrackingCountersReady, point,
                LegionIds.FalconArcher, LegionIds.FieldHerbalist, LegionIds.WolfTamer);
            TryCounterTrio(TrioSynergyTriggerKind.UndeadCountersReady, point,
                LegionIds.WraithKnight, LegionIds.Necromancer, LegionIds.SkeletonScythe);
        }

        void TryCounterTrio(TrioSynergyTriggerKind kind, Vector3 point, string first, string second, string third)
        {
            bool firstReady = GetActionCount(first) >= CounterThreshold;
            bool secondReady = GetActionCount(second) >= CounterThreshold;
            bool thirdReady = GetActionCount(third) >= CounterThreshold;
            if (!firstReady || !secondReady || !thirdReady)
                return;
            if (TryExecuteTrio(TrioSynergyTrigger.ForSeparateCounters(
                NextTriggerId(), kind, ToRunPoint(point), firstReady, secondReady, thirdReady)))
            {
                _actionCounters[first] = 0;
                _actionCounters[second] = 0;
                _actionCounters[third] = 0;
            }
        }

        bool TryExecutePair(PairSynergyTrigger trigger)
        {
            if (_pairs.TryReact(trigger, out PairSynergyReactionSnapshot reaction) == false)
                return false;
            _telemetry.RecordSynergyExecutionStarted(reaction.SynergyId, SynergyTier.Pair, reaction.ExecutionId, reaction.CasterUnitId, reaction.StepCount);
            ExecutePair(reaction);
            if (_pairs.Complete(reaction.ExecutionId))
                _telemetry.RecordSynergyExecutionCompleted(reaction.SynergyId, SynergyTier.Pair, reaction.ExecutionId, reaction.StepCount);
            ResolvedCount++;
            return true;
        }

        bool TryExecuteTrio(TrioSynergyTrigger trigger)
        {
            if (_trios.TryQueue(trigger) == false || _trios.TryStartNext(out TrioSynergyExecutionSnapshot execution) == false)
                return false;
            _telemetry.RecordSynergyExecutionStarted(execution.SynergyId, SynergyTier.Trio, execution.ExecutionId, execution.CasterUnitId, execution.StepCount);
            ExecuteTrio(execution);
            if (_trios.Complete(execution.ExecutionId))
                _telemetry.RecordSynergyExecutionCompleted(execution.SynergyId, SynergyTier.Trio, execution.ExecutionId, execution.StepCount);
            ResolvedCount++;
            return true;
        }

        void ExecutePair(PairSynergyReactionSnapshot reaction)
        {
            for (int index = 0; index < reaction.StepCount; index++)
            {
                PairSynergyEffectStep step = reaction.GetStep(index);
                SynergyEffectCommand command = SynergyEffectCommand.From(step);
                _effectExecutor.Execute(reaction.SynergyId, in command);
            }
        }

        void ExecuteTrio(TrioSynergyExecutionSnapshot execution)
        {
            if (TryGetPromotedCaster(execution.CasterUnitId, out CompanionCombatRepresentative caster))
                AttackVisual.Spawn(caster.Transform.position, AttackVisualKind.SingleHit);

            for (int index = 0; index < execution.StepCount; index++)
            {
                TrioSynergyEffectStep step = execution.GetStep(index);
                SynergyEffectCommand command = SynergyEffectCommand.From(step);
                _effectExecutor.Execute(execution.SynergyId, in command);
            }
        }

        void CollectTargets(Vector3 center, float radius, int limit)
        {
            _targets.Clear();
            float radiusSquared = radius * radius;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || (target.transform.position - center).sqrMagnitude > radiusSquared)
                    continue;
                _targets.Add(target);
            }
            _targets.Sort((left, right) =>
                ((left.transform.position - center).sqrMagnitude).CompareTo((right.transform.position - center).sqrMagnitude));
            int cap = limit > 0 ? limit : _targets.Count;
            if (_targets.Count > cap)
                _targets.RemoveRange(cap, _targets.Count - cap);
        }

        bool TryGetNearest(Vector3 origin, out MonsterController nearest)
        {
            nearest = null;
            float best = float.MaxValue;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false)
                    continue;
                float distance = (target.transform.position - origin).sqrMagnitude;
                if (distance >= best)
                    continue;
                best = distance;
                nearest = target;
            }
            return nearest != null;
        }

        int CountTargets(Vector3 center, float radius)
        {
            int count = 0;
            float radiusSquared = radius * radius;
            foreach (MonsterController target in _registry.Enemies)
                if (target != null && target.IsValid() && (target.transform.position - center).sqrMagnitude <= radiusSquared) count++;
            return count;
        }

        void CaptureFireField(Vector3 target)
        {
            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(LegionIds.FireMage);
            CombatEffectData effect = profile == null ? null : _data.GetCombatEffect(profile.BasicEffectId);
            _fireFieldCenter = target;
            _fireFieldRadius = effect == null ? 2.0f : Mathf.Max(0.1f, effect.Radius);
            _fireFieldUntil = Time.time + (effect == null ? 2.0f : Mathf.Max(0.1f, effect.Duration));
        }

        bool IsInsideFire(Vector3 point)
        {
            return Time.time < _fireFieldUntil && (point - _fireFieldCenter).sqrMagnitude <= _fireFieldRadius * _fireFieldRadius;
        }

        void RecordAction(string legionId)
        {
            _actionCounters[legionId] = GetActionCount(legionId) + 1;
        }

        int GetActionCount(string legionId)
        {
            return _actionCounters.TryGetValue(legionId, out int count) ? count : 0;
        }

        bool WasRecentlyReady(string legionId) => GetActionCount(legionId) > 0;

        bool TryGetPromotedCaster(string promotedUnitId, out CompanionCombatRepresentative representative)
        {
            IReadOnlyList<CompanionRosterData> roster = _data.CompanionRoster;
            for (int index = 0; index < roster.Count; index++)
            {
                CompanionRosterData entry = roster[index];
                CompanionPromotionData promotion = _data.GetCompanionPromotion(entry.PromotionProfileId);
                if (promotion == null
                    || string.Equals(promotion.PromotedUnitId, promotedUnitId, StringComparison.Ordinal) == false)
                {
                    continue;
                }

                return _companions.TryGetPromotedRepresentative(
                    promotion.BaseUnitId,
                    0,
                    out representative);
            }

            representative = default;
            return false;
        }

        long NextTriggerId()
        {
            if (_nextTriggerId == long.MaxValue)
                _nextTriggerId = 1;
            return _nextTriggerId++;
        }

        public static int StableEntityId(UnityEngine.Object target)
        {
            if (target == null)
                return 0;

            int instanceId = target.GetInstanceID();
            int stableId = instanceId & int.MaxValue;
            return stableId == 0 ? 1 : stableId;
        }

        static RunPoint ToRunPoint(Vector3 value) => new RunPoint(value.x, value.y);
        static Vector3 ToVector(RunPoint value) => new Vector3(value.X, value.Y, 0.0f);

        static PairSynergyBalance CreatePairBalance() => new PairSynergyBalance(
            15, 1.5f, 12, 1.2f, 0.2f, 2.0f, 3, 0.2f, 2.0f,
            5, 10, 1.0f, 2.0f, 1.0f, 20, 1.5f, 6.0f);

        static PairSynergySecondBalance CreatePairSecondBalance() => new PairSynergySecondBalance(
            2.0f, 1.0f, 20, 3, 8, 4, 25, 5.0f, 18, 1.2f, 0.2f,
            30, 1.0f, 0.3f, 15, 2.0f, 0.4f, 6.0f);

        static TrioSynergyBalance CreateTrioBalance() => new TrioSynergyBalance(
            10, 3.0f, 1.0f, 12, 5, 10, 15, 20, 3.0f, 1.0f,
            2.0f, 8, 15, 25, 0.2f, 2.0f, 10, 25, 2.0f, 10.0f);

        static TrioSynergySecondBalance CreateTrioSecondBalance() => new TrioSynergySecondBalance(
            10, 0.2f, 2.0f, 20, 0.25f, 2.0f, 10, 25, 5, 2.0f,
            8, 10, 25, 3.0f, 1.0f, 20, 2.0f, 10.0f);
    }
}
