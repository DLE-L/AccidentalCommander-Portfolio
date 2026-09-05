using System;

namespace Lizzo.PV.Gameplay.Run
{
    public enum SummonedLineage
    {
        WolfTamer,
        WraithKnight,
        Necromancer,
    }

    public enum SummonedActionPhase
    {
        Idle,
        ProxyOutbound,
        Approaching,
        Returning,
    }

    public enum SummonedPassiveId
    {
        WolfFang,
        WolfRelentlessHunt,
        WolfExecutionSense,
        WolfPackFerocity,
        WraithWideSlash,
        WraithDeepWeakening,
        WraithLingeringWeakening,
        WraithWidePatrol,
        NecromancerLongCurse,
        NecromancerStrongPull,
        NecromancerAdditionalSkeleton,
        NecromancerLongRitual,
    }

    public sealed class WolfProxyDefinition
    {
        public int Damage { get; }
        public int KillChainCount { get; }
        public int PackKillTrigger { get; }
        public int PackCount { get; }
        public int PackDamage { get; }

        private WolfProxyDefinition(int damage, int killChainCount, int packKillTrigger, int packCount, int packDamage)
        {
            Damage = damage;
            KillChainCount = killChainCount;
            PackKillTrigger = packKillTrigger;
            PackCount = packCount;
            PackDamage = packDamage;
        }

        public static WolfProxyDefinition Create(
            int damage,
            int killChainCount,
            int packKillTrigger,
            int packCount,
            int packDamage)
        {
            ValidatePositive(damage, nameof(damage));
            if (killChainCount < 0)
                throw new ArgumentOutOfRangeException(nameof(killChainCount));
            ValidatePositive(packKillTrigger, nameof(packKillTrigger));
            ValidatePositive(packCount, nameof(packCount));
            ValidatePositive(packDamage, nameof(packDamage));
            return new WolfProxyDefinition(damage, killChainCount, packKillTrigger, packCount, packDamage);
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class WraithActionDefinition
    {
        public int Damage { get; }
        public float Radius { get; }
        public float WeakenMagnitude { get; }
        public float WeakenDuration { get; }
        public int PatrolTrigger { get; }
        public int PatrolDamage { get; }
        public float PatrolRadius { get; }

        private WraithActionDefinition(
            int damage,
            float radius,
            float weakenMagnitude,
            float weakenDuration,
            int patrolTrigger,
            int patrolDamage,
            float patrolRadius)
        {
            Damage = damage;
            Radius = radius;
            WeakenMagnitude = weakenMagnitude;
            WeakenDuration = weakenDuration;
            PatrolTrigger = patrolTrigger;
            PatrolDamage = patrolDamage;
            PatrolRadius = patrolRadius;
        }

        public static WraithActionDefinition Create(
            int damage,
            float radius,
            float weakenMagnitude,
            float weakenDuration,
            int patrolTrigger,
            int patrolDamage,
            float patrolRadius)
        {
            ValidatePositive(damage, nameof(damage));
            ValidatePositive(radius, nameof(radius));
            ValidatePositive(weakenMagnitude, nameof(weakenMagnitude));
            ValidatePositive(weakenDuration, nameof(weakenDuration));
            ValidatePositive(patrolTrigger, nameof(patrolTrigger));
            ValidatePositive(patrolDamage, nameof(patrolDamage));
            ValidatePositive(patrolRadius, nameof(patrolRadius));
            return new WraithActionDefinition(
                damage,
                radius,
                weakenMagnitude,
                weakenDuration,
                patrolTrigger,
                patrolDamage,
                patrolRadius);
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class NecromancerActionDefinition
    {
        public int Damage { get; }
        public float CurseMagnitude { get; }
        public float CurseDuration { get; }
        public float PullRadius { get; }
        public float PullDistance { get; }
        public float PullStrength { get; }
        public int RitualKillTrigger { get; }
        public int SkeletonCount { get; }
        public int SkeletonDamage { get; }
        public float SkeletonAttackInterval { get; }
        public float RitualDuration { get; }

        private NecromancerActionDefinition(
            int damage,
            float curseMagnitude,
            float curseDuration,
            float pullRadius,
            float pullDistance,
            float pullStrength,
            int ritualKillTrigger,
            int skeletonCount,
            int skeletonDamage,
            float skeletonAttackInterval,
            float ritualDuration)
        {
            Damage = damage;
            CurseMagnitude = curseMagnitude;
            CurseDuration = curseDuration;
            PullRadius = pullRadius;
            PullDistance = pullDistance;
            PullStrength = pullStrength;
            RitualKillTrigger = ritualKillTrigger;
            SkeletonCount = skeletonCount;
            SkeletonDamage = skeletonDamage;
            SkeletonAttackInterval = skeletonAttackInterval;
            RitualDuration = ritualDuration;
        }

        public static NecromancerActionDefinition Create(
            int damage,
            float curseMagnitude,
            float curseDuration,
            float pullRadius,
            float pullDistance,
            float pullStrength,
            int ritualKillTrigger,
            int skeletonCount,
            int skeletonDamage,
            float skeletonAttackInterval,
            float ritualDuration)
        {
            ValidatePositive(damage, nameof(damage));
            ValidatePositive(curseMagnitude, nameof(curseMagnitude));
            ValidatePositive(curseDuration, nameof(curseDuration));
            ValidatePositive(pullRadius, nameof(pullRadius));
            ValidatePositive(pullDistance, nameof(pullDistance));
            ValidatePositive(pullStrength, nameof(pullStrength));
            ValidatePositive(ritualKillTrigger, nameof(ritualKillTrigger));
            ValidatePositive(skeletonCount, nameof(skeletonCount));
            ValidatePositive(skeletonDamage, nameof(skeletonDamage));
            ValidatePositive(skeletonAttackInterval, nameof(skeletonAttackInterval));
            ValidatePositive(ritualDuration, nameof(ritualDuration));
            return new NecromancerActionDefinition(
                damage,
                curseMagnitude,
                curseDuration,
                pullRadius,
                pullDistance,
                pullStrength,
                ritualKillTrigger,
                skeletonCount,
                skeletonDamage,
                skeletonAttackInterval,
                ritualDuration);
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class SummonedLegionDefinition
    {
        public static SummonedLegionDefinition Disabled { get; } = new SummonedLegionDefinition();

        internal bool IsEnabled { get; }
        public int CommanderEntityId { get; }
        public float TargetRange { get; }
        public float BaseAttackPeriod { get; }
        public WolfProxyDefinition Wolf { get; }
        public WraithActionDefinition Wraith { get; }
        public NecromancerActionDefinition Necromancer { get; }

        private SummonedLegionDefinition()
        {
        }

        private SummonedLegionDefinition(
            int commanderEntityId,
            float targetRange,
            float baseAttackPeriod,
            WolfProxyDefinition wolf,
            WraithActionDefinition wraith,
            NecromancerActionDefinition necromancer)
        {
            IsEnabled = true;
            CommanderEntityId = commanderEntityId;
            TargetRange = targetRange;
            BaseAttackPeriod = baseAttackPeriod;
            Wolf = wolf;
            Wraith = wraith;
            Necromancer = necromancer;
        }

        public static SummonedLegionDefinition Create(
            int commanderEntityId,
            float targetRange,
            float baseAttackPeriod,
            WolfProxyDefinition wolf,
            WraithActionDefinition wraith,
            NecromancerActionDefinition necromancer)
        {
            if (commanderEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(commanderEntityId));
            ValidatePositive(targetRange, nameof(targetRange));
            if (float.IsNaN(baseAttackPeriod) || float.IsInfinity(baseAttackPeriod) || baseAttackPeriod < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(baseAttackPeriod));
            return new SummonedLegionDefinition(
                commanderEntityId,
                targetRange,
                baseAttackPeriod,
                wolf ?? throw new ArgumentNullException(nameof(wolf)),
                wraith ?? throw new ArgumentNullException(nameof(wraith)),
                necromancer ?? throw new ArgumentNullException(nameof(necromancer)));
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public readonly struct SummonedModifierSnapshot
    {
        public int AppliedPassiveCount { get; }
        public float WolfDamageMultiplier { get; }
        public int WolfChainBonus { get; }
        public float WolfExecutionThreshold { get; }
        public float PackDamageMultiplier { get; }
        public float WraithRadiusMultiplier { get; }
        public float WeakenMagnitudeBonus { get; }
        public float WeakenDurationMultiplier { get; }
        public float PatrolRadiusMultiplier { get; }
        public float CurseDurationMultiplier { get; }
        public float PullDistanceMultiplier { get; }
        public int SkeletonCountBonus { get; }
        public float RitualDurationMultiplier { get; }

        internal SummonedModifierSnapshot(bool[] passives)
        {
            int count = 0;
            for (int index = 0; index < passives.Length; index++)
            {
                if (passives[index])
                    count++;
            }
            AppliedPassiveCount = count;
            WolfDamageMultiplier = passives[(int)SummonedPassiveId.WolfFang] ? 1.25f : 1.0f;
            WolfChainBonus = passives[(int)SummonedPassiveId.WolfRelentlessHunt] ? 1 : 0;
            WolfExecutionThreshold = passives[(int)SummonedPassiveId.WolfExecutionSense] ? 0.2f : 0.0f;
            PackDamageMultiplier = passives[(int)SummonedPassiveId.WolfPackFerocity] ? 1.3f : 1.0f;
            WraithRadiusMultiplier = passives[(int)SummonedPassiveId.WraithWideSlash] ? 1.3f : 1.0f;
            WeakenMagnitudeBonus = passives[(int)SummonedPassiveId.WraithDeepWeakening] ? 0.1f : 0.0f;
            WeakenDurationMultiplier = passives[(int)SummonedPassiveId.WraithLingeringWeakening] ? 1.5f : 1.0f;
            PatrolRadiusMultiplier = passives[(int)SummonedPassiveId.WraithWidePatrol] ? 1.35f : 1.0f;
            CurseDurationMultiplier = passives[(int)SummonedPassiveId.NecromancerLongCurse] ? 1.5f : 1.0f;
            PullDistanceMultiplier = passives[(int)SummonedPassiveId.NecromancerStrongPull] ? 1.4f : 1.0f;
            SkeletonCountBonus = passives[(int)SummonedPassiveId.NecromancerAdditionalSkeleton] ? 2 : 0;
            RitualDurationMultiplier = passives[(int)SummonedPassiveId.NecromancerLongRitual] ? 1.5f : 1.0f;
        }
    }

    public readonly struct SummonedMemberSnapshot
    {
        public SummonedLineage Lineage { get; }
        public int MemberIndex { get; }
        public bool IsActive { get; }
        public SummonedActionPhase Phase { get; }
        public RunPoint Slot { get; }
        public RunPoint CurrentPosition { get; }
        public RunPoint LockedPoint { get; }
        public int ProxyTargetId { get; }
        public int RemainingWolfChains { get; }
        public float CooldownRemaining { get; }

        internal SummonedMemberSnapshot(SummonedMemberState state, bool isActive)
        {
            Lineage = state.Lineage;
            MemberIndex = state.MemberIndex;
            IsActive = isActive;
            Phase = state.Phase;
            Slot = state.Slot;
            CurrentPosition = state.CurrentPosition;
            LockedPoint = state.LockedPoint;
            ProxyTargetId = state.ProxyTargetId;
            RemainingWolfChains = state.RemainingWolfChains;
            CooldownRemaining = state.CooldownRemaining;
        }
    }

    public readonly struct SummonedLegionSnapshot
    {
        private readonly SummonedMemberSnapshot[] _members;
        private readonly int[] _progressions;

        public bool IsEnabled { get; }
        public bool WolfPackReady { get; }
        public bool WolfPackActive { get; }
        public int WolfPackCastCount { get; }
        public bool WraithPatrolReady { get; }
        public int WraithPatrolCastCount { get; }
        public RunPoint LastPatrolCenter { get; }
        public bool DarkRitualReady { get; }
        public int DarkRitualCastCount { get; }
        public int ActiveSkeletonCount { get; }
        public SummonedModifierSnapshot Modifiers { get; }
        internal ulong StateDigest { get; }

        internal SummonedLegionSnapshot(
            SummonedMemberSnapshot[] members,
            int[] progressions,
            bool wolfPackReady,
            bool wolfPackActive,
            int wolfPackCastCount,
            bool wraithPatrolReady,
            int wraithPatrolCastCount,
            RunPoint lastPatrolCenter,
            bool darkRitualReady,
            int darkRitualCastCount,
            int activeSkeletonCount,
            SummonedModifierSnapshot modifiers,
            ulong stateDigest)
        {
            IsEnabled = true;
            _members = members;
            _progressions = progressions;
            WolfPackReady = wolfPackReady;
            WolfPackActive = wolfPackActive;
            WolfPackCastCount = wolfPackCastCount;
            WraithPatrolReady = wraithPatrolReady;
            WraithPatrolCastCount = wraithPatrolCastCount;
            LastPatrolCenter = lastPatrolCenter;
            DarkRitualReady = darkRitualReady;
            DarkRitualCastCount = darkRitualCastCount;
            ActiveSkeletonCount = activeSkeletonCount;
            Modifiers = modifiers;
            StateDigest = stateDigest;
        }

        public SummonedMemberSnapshot GetMember(SummonedLineage lineage, int memberIndex)
        {
            if (_members == null || memberIndex < 1 || memberIndex > 2)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return _members[(int)lineage * 2 + memberIndex - 1];
        }

        public int GetProgression(SummonedLineage lineage)
        {
            if (_progressions == null || (int)lineage < 0 || (int)lineage >= 3)
                return 0;
            return _progressions[(int)lineage];
        }
    }

    public sealed class SummonedLegionRuntime
    {
        private const float TimeEpsilon = 0.0001f;

        private readonly SummonedLegionDefinition _definition;
        private readonly CombatResolver _resolver;
        private readonly SummonedMemberState[] _members = new SummonedMemberState[6];
        private readonly int[] _progressions = new int[3];
        private readonly bool[] _passives = new bool[12];
        private int[] _snapshotProgressions = new int[3];
        private SummonedMemberSnapshot[] _snapshotMembers = Array.Empty<SummonedMemberSnapshot>();
        private int[] _packTargets = Array.Empty<int>();
        private int _wolfKillProgress;
        private bool _wolfPackReady;
        private bool _wolfPackActive;
        private int _wolfPackCastCount;
        private int _wraithActionProgress;
        private bool _wraithPatrolReady;
        private int _wraithPatrolCastCount;
        private RunPoint _lastPatrolCenter;
        private int _ritualKillProgress;
        private bool _darkRitualReady;
        private int _darkRitualCastCount;
        private int _activeSkeletonCount;
        private float _ritualRemaining;
        private float _skeletonAttackRemaining;
        private int _stateVersion;
        private int _snapshotVersion = -1;

        internal bool IsEnabled => _definition.IsEnabled;

        public SummonedLegionRuntime(SummonedLegionDefinition definition, CombatResolver resolver)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            for (int lineage = 0; lineage < 3; lineage++)
            {
                for (int memberIndex = 1; memberIndex <= 2; memberIndex++)
                    _members[lineage * 2 + memberIndex - 1] = new SummonedMemberState((SummonedLineage)lineage, memberIndex);
            }
        }

        public void SetProgression(SummonedLineage lineage, int progression)
        {
            if (IsEnabled == false)
                return;
            ValidateLineage(lineage);
            int index = (int)lineage;
            if (progression < _progressions[index] || progression > 3)
                throw new ArgumentOutOfRangeException(nameof(progression));
            if (_progressions[index] == progression)
                return;
            _progressions[index] = progression;
            int[] progressions = new int[3];
            Array.Copy(_progressions, progressions, 3);
            _snapshotProgressions = progressions;
            MarkChanged();
        }

        internal bool TryApplyGrowth(string baseUnitId, int progression)
        {
            SummonedLineage lineage;
            if (string.Equals(baseUnitId, "wolf_tamer", StringComparison.Ordinal))
                lineage = SummonedLineage.WolfTamer;
            else if (string.Equals(baseUnitId, "wraith_knight", StringComparison.Ordinal))
                lineage = SummonedLineage.WraithKnight;
            else if (string.Equals(baseUnitId, "necromancer", StringComparison.Ordinal))
                lineage = SummonedLineage.Necromancer;
            else
                return false;
            SetProgression(lineage, progression);
            return true;
        }

        public void SetSlot(SummonedLineage lineage, int memberIndex, RunPoint slot)
        {
            if (IsEnabled == false)
                return;
            SummonedMemberState member = GetMember(lineage, memberIndex);
            member.Slot = slot;
            if (member.Phase == SummonedActionPhase.Idle || lineage != SummonedLineage.WraithKnight)
                member.CurrentPosition = slot;
            MarkChanged();
        }

        public void ApplyPassive(SummonedPassiveId passive)
        {
            if (IsEnabled == false)
                return;
            int index = (int)passive;
            if (index < 0 || index >= _passives.Length)
                throw new ArgumentOutOfRangeException(nameof(passive));
            if (_passives[index])
                return;
            _passives[index] = true;
            MarkChanged();
        }

        public bool TryBeginWolfAttack(int memberIndex)
        {
            SummonedMemberState member = GetMember(SummonedLineage.WolfTamer, memberIndex);
            if (CanBegin(member) == false || TrySelectLowestHealthEnemy(out CombatEntitySnapshot target) == false)
                return false;
            SummonedModifierSnapshot modifiers = CurrentModifiers();
            member.CooldownRemaining = _definition.BaseAttackPeriod;
            member.Phase = SummonedActionPhase.ProxyOutbound;
            member.ProxyTargetId = target.EntityId;
            member.LockedPoint = target.Position;
            member.RemainingWolfChains = _definition.Wolf.KillChainCount + modifiers.WolfChainBonus;
            member.PromotedAtStart = _progressions[(int)SummonedLineage.WolfTamer] >= 3;
            MarkChanged();
            return true;
        }

        public bool ResolveWolfImpact(int memberIndex)
        {
            SummonedMemberState member = GetMember(SummonedLineage.WolfTamer, memberIndex);
            if (member.Phase != SummonedActionPhase.ProxyOutbound)
                return false;
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot target = combat.GetEntity(member.ProxyTargetId);
            bool killed = false;
            if (target.IsDead == false)
            {
                SummonedModifierSnapshot modifiers = CurrentModifiers();
                float damage = _definition.Wolf.Damage * modifiers.WolfDamageMultiplier;
                if (modifiers.WolfExecutionThreshold > 0.0f &&
                    target.Health <= target.MaximumHealth * modifiers.WolfExecutionThreshold)
                {
                    damage = Math.Max(damage, target.Health);
                }
                killed = _resolver.ApplyDamage(new DamageRequest(
                    SourceId(SummonedLineage.WolfTamer, memberIndex),
                    target.EntityId,
                    damage,
                    CombatDamageKind.Direct,
                    attackId: "wolf-proxy-bite")).DidKill;
            }

            if (killed)
            {
                if (member.PromotedAtStart)
                    RecordWolfKill();
                if (member.RemainingWolfChains > 0 && TrySelectLowestHealthEnemy(out CombatEntitySnapshot next))
                {
                    member.RemainingWolfChains--;
                    member.ProxyTargetId = next.EntityId;
                    member.LockedPoint = next.Position;
                    MarkChanged();
                    return true;
                }
            }
            member.Phase = SummonedActionPhase.Idle;
            member.ProxyTargetId = 0;
            member.RemainingWolfChains = 0;
            MarkChanged();
            return false;
        }

        public bool TryLaunchWolfPack()
        {
            if (_wolfPackReady == false || _wolfPackActive)
                return false;
            int count = _definition.Wolf.PackCount;
            _packTargets = new int[count];
            for (int index = 0; index < count; index++)
            {
                if (TrySelectHighestHealthEnemy(out CombatEntitySnapshot target))
                    _packTargets[index] = target.EntityId;
            }
            bool found = false;
            for (int index = 0; index < _packTargets.Length; index++)
                found |= _packTargets[index] > 0;
            if (found == false)
                return false;
            _wolfPackReady = false;
            _wolfPackActive = true;
            _wolfPackCastCount++;
            MarkChanged();
            return true;
        }

        public void ResolveWolfPack()
        {
            if (_wolfPackActive == false)
                return;
            SummonedModifierSnapshot modifiers = CurrentModifiers();
            for (int index = 0; index < _packTargets.Length; index++)
            {
                int targetId = _packTargets[index];
                if (targetId <= 0 || IsEnemyAlive(targetId) == false)
                    continue;
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(SummonedLineage.WolfTamer, 3),
                    targetId,
                    _definition.Wolf.PackDamage,
                    CombatDamageKind.Direct,
                    attackerDamageMultiplier: modifiers.PackDamageMultiplier,
                    attackId: "wolf-pack"));
            }
            _wolfPackActive = false;
            _packTargets = Array.Empty<int>();
            MarkChanged();
        }

        public bool TryBeginWraithSlash(int memberIndex)
        {
            SummonedMemberState member = GetMember(SummonedLineage.WraithKnight, memberIndex);
            if (CanBegin(member) == false || TrySelectClosestEnemy(out CombatEntitySnapshot target) == false)
                return false;
            member.CooldownRemaining = _definition.BaseAttackPeriod;
            member.LockedPoint = target.Position;
            member.Phase = SummonedActionPhase.Approaching;
            member.PromotedAtStart = _progressions[(int)SummonedLineage.WraithKnight] >= 3;
            MarkChanged();
            return true;
        }

        public void ResolveWraithSlash(int memberIndex)
        {
            SummonedMemberState member = GetMember(SummonedLineage.WraithKnight, memberIndex);
            if (member.Phase != SummonedActionPhase.Approaching)
                return;
            member.CurrentPosition = member.LockedPoint;
            SummonedModifierSnapshot modifiers = CurrentModifiers();
            float radius = _definition.Wraith.Radius * modifiers.WraithRadiusMultiplier;
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float radiusSquared = radius * radius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    RunPoint.DistanceSquared(enemy.Position, member.LockedPoint) > radiusSquared)
                {
                    continue;
                }
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(SummonedLineage.WraithKnight, memberIndex),
                    enemy.EntityId,
                    _definition.Wraith.Damage,
                    CombatDamageKind.Area,
                    attackId: "wraith-slash"));
                if (IsEnemyAlive(enemy.EntityId))
                {
                    _resolver.ApplyStatus(new StatusRequest(
                        SourceId(SummonedLineage.WraithKnight, memberIndex),
                        enemy.EntityId,
                        CombatStatusKind.Weakened,
                        _definition.Wraith.WeakenMagnitude + modifiers.WeakenMagnitudeBonus,
                        _definition.Wraith.WeakenDuration * modifiers.WeakenDurationMultiplier,
                        charges: 1));
                }
            }
            if (member.PromotedAtStart && _wraithPatrolReady == false)
            {
                _wraithActionProgress++;
                if (_wraithActionProgress >= _definition.Wraith.PatrolTrigger)
                {
                    _wraithActionProgress -= _definition.Wraith.PatrolTrigger;
                    _wraithPatrolReady = true;
                }
            }
            member.Phase = SummonedActionPhase.Returning;
            MarkChanged();
        }

        public void CompleteWraithReturn(int memberIndex)
        {
            SummonedMemberState member = GetMember(SummonedLineage.WraithKnight, memberIndex);
            if (member.Phase != SummonedActionPhase.Returning)
                return;
            member.CurrentPosition = member.Slot;
            member.Phase = SummonedActionPhase.Idle;
            MarkChanged();
        }

        public bool TryResolveWraithPatrol()
        {
            if (_wraithPatrolReady == false)
                return false;
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            SummonedModifierSnapshot modifiers = CurrentModifiers();
            float radius = _definition.Wraith.PatrolRadius * modifiers.PatrolRadiusMultiplier;
            if (HasEnemyInRadius(commander.Position, radius) == false)
                return false;
            _lastPatrolCenter = commander.Position;
            ApplyAreaDamage(
                SourceId(SummonedLineage.WraithKnight, 3),
                commander.Position,
                radius,
                _definition.Wraith.PatrolDamage,
                "wraith-patrol");
            _wraithPatrolReady = false;
            _wraithPatrolCastCount++;
            MarkChanged();
            return true;
        }

        public bool TryCastCurseBolt(int memberIndex)
        {
            SummonedMemberState member = GetMember(SummonedLineage.Necromancer, memberIndex);
            if (CanBegin(member) == false || TrySelectLowestHealthEnemy(out CombatEntitySnapshot target) == false)
                return false;
            member.CooldownRemaining = _definition.BaseAttackPeriod;
            member.CurrentPosition = member.Slot;
            member.PromotedAtStart = _progressions[(int)SummonedLineage.Necromancer] >= 3;
            SummonedModifierSnapshot modifiers = CurrentModifiers();
            int sourceId = SourceId(SummonedLineage.Necromancer, memberIndex);
            _resolver.ApplyStatus(new StatusRequest(
                sourceId,
                target.EntityId,
                CombatStatusKind.Curse,
                _definition.Necromancer.CurseMagnitude,
                _definition.Necromancer.CurseDuration * modifiers.CurseDurationMultiplier));
            _resolver.ApplyDamage(new DamageRequest(
                sourceId,
                target.EntityId,
                _definition.Necromancer.Damage,
                CombatDamageKind.Projectile,
                attackId: "curse-bolt"));
            MarkChanged();
            return true;
        }

        public bool TryResolveCurseDeath(int deadEnemyId)
        {
            if (_progressions[(int)SummonedLineage.Necromancer] < 3)
                return false;
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot dead = combat.GetEntity(deadEnemyId);
            if (dead.IsDead == false || HasNecromancerCurse(dead) == false)
                return false;

            SummonedModifierSnapshot modifiers = CurrentModifiers();
            float radiusSquared = _definition.Necromancer.PullRadius * _definition.Necromancer.PullRadius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead || enemy.EntityId == deadEnemyId ||
                    RunPoint.DistanceSquared(enemy.Position, dead.Position) > radiusSquared)
                {
                    continue;
                }
                RunPoint direction = Normalize(dead.Position.X - enemy.Position.X, dead.Position.Y - enemy.Position.Y);
                _resolver.ResolveForcedMovement(new[]
                {
                    new ForcedMovementRequest(
                        SourceId(SummonedLineage.Necromancer, 3),
                        enemy.EntityId,
                        ForcedMovementKind.Pull,
                        direction,
                        _definition.Necromancer.PullDistance * modifiers.PullDistanceMultiplier,
                        priority: 10,
                        strength: _definition.Necromancer.PullStrength,
                        maximumSeconds: 1.0f),
                });
            }
            if (_darkRitualReady == false)
            {
                _ritualKillProgress++;
                if (_ritualKillProgress >= _definition.Necromancer.RitualKillTrigger)
                {
                    _ritualKillProgress -= _definition.Necromancer.RitualKillTrigger;
                    _darkRitualReady = true;
                }
            }
            MarkChanged();
            return true;
        }

        public bool TryBeginDarkRitual()
        {
            if (_darkRitualReady == false || _activeSkeletonCount > 0 ||
                TrySelectClosestEnemy(out _) == false)
            {
                return false;
            }
            SummonedModifierSnapshot modifiers = CurrentModifiers();
            _darkRitualReady = false;
            _darkRitualCastCount++;
            _activeSkeletonCount = _definition.Necromancer.SkeletonCount + modifiers.SkeletonCountBonus;
            _ritualRemaining = _definition.Necromancer.RitualDuration * modifiers.RitualDurationMultiplier;
            _skeletonAttackRemaining = _definition.Necromancer.SkeletonAttackInterval;
            MarkChanged();
            return true;
        }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (IsEnabled == false || deltaSeconds <= 0.0f)
                return;
            bool changed = false;
            for (int index = 0; index < _members.Length; index++)
            {
                SummonedMemberState member = _members[index];
                if (member.CooldownRemaining <= 0.0f)
                    continue;
                member.CooldownRemaining = Math.Max(0.0f, member.CooldownRemaining - deltaSeconds);
                changed = true;
            }
            if (_activeSkeletonCount > 0)
            {
                float activeDelta = Math.Min(deltaSeconds, _ritualRemaining);
                _ritualRemaining -= deltaSeconds;
                _skeletonAttackRemaining -= activeDelta;
                while (_skeletonAttackRemaining <= TimeEpsilon && activeDelta > 0.0f)
                {
                    ResolveSkeletonVolley();
                    _skeletonAttackRemaining += _definition.Necromancer.SkeletonAttackInterval;
                }
                if (_ritualRemaining <= TimeEpsilon)
                {
                    _activeSkeletonCount = 0;
                    _ritualRemaining = 0.0f;
                }
                changed = true;
            }
            if (changed)
                MarkChanged();
        }

        public SummonedLegionSnapshot CreateSnapshot()
        {
            if (IsEnabled == false)
                return default;
            if (_snapshotVersion != _stateVersion)
            {
                SummonedMemberSnapshot[] members = new SummonedMemberSnapshot[6];
                for (int index = 0; index < _members.Length; index++)
                {
                    SummonedMemberState member = _members[index];
                    members[index] = new SummonedMemberSnapshot(
                        member,
                        IsMemberActive(member.Lineage, member.MemberIndex));
                }
                _snapshotMembers = members;
                _snapshotVersion = _stateVersion;
            }
            return new SummonedLegionSnapshot(
                _snapshotMembers,
                _snapshotProgressions,
                _wolfPackReady,
                _wolfPackActive,
                _wolfPackCastCount,
                _wraithPatrolReady,
                _wraithPatrolCastCount,
                _lastPatrolCenter,
                _darkRitualReady,
                _darkRitualCastCount,
                _activeSkeletonCount,
                CurrentModifiers(),
                CalculateDigest());
        }

        private void ResolveSkeletonVolley()
        {
            for (int index = 0; index < _activeSkeletonCount; index++)
            {
                if (TrySelectClosestEnemy(out CombatEntitySnapshot target) == false)
                    return;
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(SummonedLineage.Necromancer, 3),
                    target.EntityId,
                    _definition.Necromancer.SkeletonDamage,
                    CombatDamageKind.Direct,
                    attackId: "ritual-skeleton"));
            }
        }

        private void RecordWolfKill()
        {
            if (_wolfPackReady)
                return;
            _wolfKillProgress++;
            if (_wolfKillProgress >= _definition.Wolf.PackKillTrigger)
            {
                _wolfKillProgress -= _definition.Wolf.PackKillTrigger;
                _wolfPackReady = true;
            }
        }

        private bool CanBegin(SummonedMemberState member)
        {
            return IsEnabled && IsMemberActive(member.Lineage, member.MemberIndex) &&
                member.Phase == SummonedActionPhase.Idle && member.CooldownRemaining <= TimeEpsilon;
        }

        private bool TrySelectLowestHealthEnemy(out CombatEntitySnapshot selected)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            bool found = false;
            selected = default;
            float rangeSquared = _definition.TargetRange * _definition.TargetRange;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    RunPoint.DistanceSquared(enemy.Position, commander.Position) > rangeSquared)
                {
                    continue;
                }
                if (found && (enemy.Health > selected.Health ||
                    (enemy.Health == selected.Health && enemy.EntityId >= selected.EntityId)))
                {
                    continue;
                }
                found = true;
                selected = enemy;
            }
            return found;
        }

        private bool TrySelectHighestHealthEnemy(out CombatEntitySnapshot selected)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            bool found = false;
            selected = default;
            float rangeSquared = _definition.TargetRange * _definition.TargetRange;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    RunPoint.DistanceSquared(enemy.Position, commander.Position) > rangeSquared)
                {
                    continue;
                }
                if (found && (enemy.Health < selected.Health ||
                    (enemy.Health == selected.Health && enemy.EntityId >= selected.EntityId)))
                {
                    continue;
                }
                found = true;
                selected = enemy;
            }
            return found;
        }

        private bool TrySelectClosestEnemy(out CombatEntitySnapshot selected)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            bool found = false;
            selected = default;
            float bestDistance = _definition.TargetRange * _definition.TargetRange;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead)
                    continue;
                float distance = RunPoint.DistanceSquared(enemy.Position, commander.Position);
                if (distance > bestDistance ||
                    (found && distance == bestDistance && enemy.EntityId >= selected.EntityId))
                {
                    continue;
                }
                found = true;
                bestDistance = distance;
                selected = enemy;
            }
            return found;
        }

        private bool HasEnemyInRadius(RunPoint center, float radius)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float radiusSquared = radius * radius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) && enemy.IsDead == false &&
                    RunPoint.DistanceSquared(enemy.Position, center) <= radiusSquared)
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplyAreaDamage(int sourceId, RunPoint center, float radius, int damage, string attackId)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float radiusSquared = radius * radius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    RunPoint.DistanceSquared(enemy.Position, center) > radiusSquared)
                {
                    continue;
                }
                _resolver.ApplyDamage(new DamageRequest(
                    sourceId,
                    enemy.EntityId,
                    damage,
                    CombatDamageKind.Area,
                    attackId: attackId));
            }
        }

        private bool HasNecromancerCurse(CombatEntitySnapshot enemy)
        {
            for (int memberIndex = 1; memberIndex <= 2; memberIndex++)
            {
                if (enemy.GetStatusRemaining(
                    CombatStatusKind.Curse,
                    SourceId(SummonedLineage.Necromancer, memberIndex)) > 0.0f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsEnemyAlive(int entityId)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot entity = combat.GetEntityAt(index);
                if (entity.EntityId == entityId)
                    return IsEnemy(entity) && entity.IsDead == false;
            }
            return false;
        }

        private SummonedModifierSnapshot CurrentModifiers()
        {
            return new SummonedModifierSnapshot(_passives);
        }

        private bool IsMemberActive(SummonedLineage lineage, int memberIndex)
        {
            int progression = _progressions[(int)lineage];
            return memberIndex == 1 ? progression >= 1 : progression >= 2;
        }

        private SummonedMemberState GetMember(SummonedLineage lineage, int memberIndex)
        {
            ValidateLineage(lineage);
            if (memberIndex < 1 || memberIndex > 2)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return _members[(int)lineage * 2 + memberIndex - 1];
        }

        private void MarkChanged()
        {
            _stateVersion++;
        }

        private ulong CalculateDigest()
        {
            ulong digest = 14695981039346656037UL;
            for (int index = 0; index < _progressions.Length; index++)
                AddDigest(ref digest, (ulong)(uint)_progressions[index]);
            for (int index = 0; index < _passives.Length; index++)
                AddDigest(ref digest, _passives[index] ? 1UL : 0UL);
            for (int index = 0; index < _members.Length; index++)
            {
                SummonedMemberState member = _members[index];
                AddDigest(ref digest, (ulong)(uint)member.Phase);
                AddPointDigest(ref digest, member.Slot);
                AddPointDigest(ref digest, member.CurrentPosition);
                AddPointDigest(ref digest, member.LockedPoint);
                AddDigest(ref digest, (ulong)(uint)member.ProxyTargetId);
                AddDigest(ref digest, (ulong)(uint)member.RemainingWolfChains);
                AddFloatDigest(ref digest, member.CooldownRemaining);
            }
            AddDigest(ref digest, (ulong)(uint)_wolfKillProgress);
            AddDigest(ref digest, _wolfPackReady ? 1UL : 0UL);
            AddDigest(ref digest, _wolfPackActive ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_wolfPackCastCount);
            AddDigest(ref digest, (ulong)(uint)_wraithActionProgress);
            AddDigest(ref digest, _wraithPatrolReady ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_wraithPatrolCastCount);
            AddPointDigest(ref digest, _lastPatrolCenter);
            AddDigest(ref digest, (ulong)(uint)_ritualKillProgress);
            AddDigest(ref digest, _darkRitualReady ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_darkRitualCastCount);
            AddDigest(ref digest, (ulong)(uint)_activeSkeletonCount);
            AddFloatDigest(ref digest, _ritualRemaining);
            AddFloatDigest(ref digest, _skeletonAttackRemaining);
            return digest;
        }

        private static RunPoint Normalize(float x, float y)
        {
            float length = (float)Math.Sqrt(x * x + y * y);
            return length <= TimeEpsilon ? new RunPoint(1.0f, 0.0f) : new RunPoint(x / length, y / length);
        }

        private static bool IsEnemy(CombatEntitySnapshot entity)
        {
            return entity.Kind == CombatEntityKind.NormalEnemy ||
                entity.Kind == CombatEntityKind.EliteEnemy ||
                entity.Kind == CombatEntityKind.BossEnemy;
        }

        private static int SourceId(SummonedLineage lineage, int memberIndex)
        {
            return 13000 + (int)lineage * 10 + memberIndex;
        }

        private static void ValidateLineage(SummonedLineage lineage)
        {
            if ((int)lineage < 0 || (int)lineage >= 3)
                throw new ArgumentOutOfRangeException(nameof(lineage));
        }

        private static void AddPointDigest(ref ulong digest, RunPoint point)
        {
            AddFloatDigest(ref digest, point.X);
            AddFloatDigest(ref digest, point.Y);
        }

        private static void AddFloatDigest(ref ulong digest, float value)
        {
            AddDigest(ref digest, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(value)));
        }

        private static void AddDigest(ref ulong digest, ulong value)
        {
            const ulong prime = 1099511628211UL;
            for (int shift = 0; shift < 64; shift += 8)
            {
                digest ^= (byte)(value >> shift);
                digest *= prime;
            }
        }
    }

    internal sealed class SummonedMemberState
    {
        internal SummonedLineage Lineage { get; }
        internal int MemberIndex { get; }
        internal SummonedActionPhase Phase { get; set; }
        internal RunPoint Slot { get; set; }
        internal RunPoint CurrentPosition { get; set; }
        internal RunPoint LockedPoint { get; set; }
        internal int ProxyTargetId { get; set; }
        internal int RemainingWolfChains { get; set; }
        internal float CooldownRemaining { get; set; }
        internal bool PromotedAtStart { get; set; }

        internal SummonedMemberState(SummonedLineage lineage, int memberIndex)
        {
            Lineage = lineage;
            MemberIndex = memberIndex;
        }
    }
}
