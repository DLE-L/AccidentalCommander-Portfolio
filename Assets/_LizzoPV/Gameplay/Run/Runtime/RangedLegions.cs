using System;

namespace Lizzo.PV.Gameplay.Run
{
    public enum RangedLineage
    {
        Archer,
        Bombardier,
        Scythe,
    }

    public enum RangedActionPhase
    {
        Idle,
        ProjectileOutbound,
        Fuse,
        ProjectileReturning,
    }

    public enum RangedPassiveId
    {
        ArcherMultiShot,
        ArcherDoubleVolley,
        ArcherPiercingArrow,
        ArcherPenetrationAcceleration,
        BombDoubleThrow,
        BombShortFuse,
        BombFragments,
        BombCompressedPowder,
        ScytheGiantBlade,
        ScytheSwiftReturn,
        ScytheDoubleDirection,
        ScytheRoundTripHarvest,
    }

    public sealed class RangedLegionDefinition
    {
        public static RangedLegionDefinition Disabled { get; } = new RangedLegionDefinition();

        internal bool IsEnabled { get; }
        public int CommanderEntityId { get; }
        public float TargetRange { get; }
        public float BaseAttackPeriod { get; }
        public int ArcherDamage { get; }
        public float ArrowWidth { get; }
        public int ArrowPierceCount { get; }
        public int FalconTrigger { get; }
        public int FalconDamage { get; }
        public int BombDamage { get; }
        public float BombRadius { get; }
        public float BombFuseSeconds { get; }
        public int ClusterTrigger { get; }
        public int ClusterMiniDamage { get; }
        public float ClusterMiniRadius { get; }
        public int ClusterMiniCount { get; }
        public float DoubleBombDelay { get; }
        public int ScytheDamage { get; }
        public float ScytheWidth { get; }
        public float ScytheDistance { get; }
        public int GiantScytheHitTrigger { get; }
        public int GiantScytheDamage { get; }
        public float GiantScytheRadius { get; }

        private RangedLegionDefinition()
        {
        }

        private RangedLegionDefinition(
            int commanderEntityId,
            float targetRange,
            float baseAttackPeriod,
            int archerDamage,
            float arrowWidth,
            int arrowPierceCount,
            int falconTrigger,
            int falconDamage,
            int bombDamage,
            float bombRadius,
            float bombFuseSeconds,
            int clusterTrigger,
            int clusterMiniDamage,
            float clusterMiniRadius,
            int clusterMiniCount,
            float doubleBombDelay,
            int scytheDamage,
            float scytheWidth,
            float scytheDistance,
            int giantScytheHitTrigger,
            int giantScytheDamage,
            float giantScytheRadius)
        {
            IsEnabled = true;
            CommanderEntityId = commanderEntityId;
            TargetRange = targetRange;
            BaseAttackPeriod = baseAttackPeriod;
            ArcherDamage = archerDamage;
            ArrowWidth = arrowWidth;
            ArrowPierceCount = arrowPierceCount;
            FalconTrigger = falconTrigger;
            FalconDamage = falconDamage;
            BombDamage = bombDamage;
            BombRadius = bombRadius;
            BombFuseSeconds = bombFuseSeconds;
            ClusterTrigger = clusterTrigger;
            ClusterMiniDamage = clusterMiniDamage;
            ClusterMiniRadius = clusterMiniRadius;
            ClusterMiniCount = clusterMiniCount;
            DoubleBombDelay = doubleBombDelay;
            ScytheDamage = scytheDamage;
            ScytheWidth = scytheWidth;
            ScytheDistance = scytheDistance;
            GiantScytheHitTrigger = giantScytheHitTrigger;
            GiantScytheDamage = giantScytheDamage;
            GiantScytheRadius = giantScytheRadius;
        }

        public static RangedLegionDefinition Create(
            int commanderEntityId,
            float targetRange,
            float baseAttackPeriod,
            int archerDamage,
            float arrowWidth,
            int arrowPierceCount,
            int falconTrigger,
            int falconDamage,
            int bombDamage,
            float bombRadius,
            float bombFuseSeconds,
            int clusterTrigger,
            int clusterMiniDamage,
            float clusterMiniRadius,
            int clusterMiniCount,
            float doubleBombDelay,
            int scytheDamage,
            float scytheWidth,
            float scytheDistance,
            int giantScytheHitTrigger,
            int giantScytheDamage,
            float giantScytheRadius)
        {
            if (commanderEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(commanderEntityId));
            ValidatePositive(targetRange, nameof(targetRange));
            ValidateNonNegative(baseAttackPeriod, nameof(baseAttackPeriod));
            ValidatePositive(archerDamage, nameof(archerDamage));
            ValidatePositive(arrowWidth, nameof(arrowWidth));
            ValidatePositive(arrowPierceCount, nameof(arrowPierceCount));
            ValidatePositive(falconTrigger, nameof(falconTrigger));
            ValidatePositive(falconDamage, nameof(falconDamage));
            ValidatePositive(bombDamage, nameof(bombDamage));
            ValidatePositive(bombRadius, nameof(bombRadius));
            ValidateNonNegative(bombFuseSeconds, nameof(bombFuseSeconds));
            ValidatePositive(clusterTrigger, nameof(clusterTrigger));
            ValidatePositive(clusterMiniDamage, nameof(clusterMiniDamage));
            ValidatePositive(clusterMiniRadius, nameof(clusterMiniRadius));
            ValidatePositive(clusterMiniCount, nameof(clusterMiniCount));
            ValidateNonNegative(doubleBombDelay, nameof(doubleBombDelay));
            ValidatePositive(scytheDamage, nameof(scytheDamage));
            ValidatePositive(scytheWidth, nameof(scytheWidth));
            ValidatePositive(scytheDistance, nameof(scytheDistance));
            ValidatePositive(giantScytheHitTrigger, nameof(giantScytheHitTrigger));
            ValidatePositive(giantScytheDamage, nameof(giantScytheDamage));
            ValidatePositive(giantScytheRadius, nameof(giantScytheRadius));
            return new RangedLegionDefinition(
                commanderEntityId,
                targetRange,
                baseAttackPeriod,
                archerDamage,
                arrowWidth,
                arrowPierceCount,
                falconTrigger,
                falconDamage,
                bombDamage,
                bombRadius,
                bombFuseSeconds,
                clusterTrigger,
                clusterMiniDamage,
                clusterMiniRadius,
                clusterMiniCount,
                doubleBombDelay,
                scytheDamage,
                scytheWidth,
                scytheDistance,
                giantScytheHitTrigger,
                giantScytheDamage,
                giantScytheRadius);
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public readonly struct RangedModifierSnapshot
    {
        public int AppliedPassiveCount { get; }
        public int ArrowDirections { get; }
        public int ArrowVolleys { get; }
        public int ArrowPierceBonus { get; }
        public float ArrowPenetrationDamageStep { get; }
        public int BombCount { get; }
        public float BombFuseMultiplier { get; }
        public int BombFragmentCount { get; }
        public float BombCenterBonus { get; }
        public float ScytheWidthMultiplier { get; }
        public float ScytheReturnSpeedMultiplier { get; }
        public int ScytheDirections { get; }
        public float ScytheReturnDamageMultiplier { get; }

        internal RangedModifierSnapshot(bool[] passives)
        {
            int count = 0;
            for (int index = 0; index < passives.Length; index++)
            {
                if (passives[index])
                    count++;
            }
            AppliedPassiveCount = count;
            ArrowDirections = passives[(int)RangedPassiveId.ArcherMultiShot] ? 3 : 1;
            ArrowVolleys = passives[(int)RangedPassiveId.ArcherDoubleVolley] ? 2 : 1;
            ArrowPierceBonus = passives[(int)RangedPassiveId.ArcherPiercingArrow] ? 2 : 0;
            ArrowPenetrationDamageStep = passives[(int)RangedPassiveId.ArcherPenetrationAcceleration] ? 0.20f : 0.0f;
            BombCount = passives[(int)RangedPassiveId.BombDoubleThrow] ? 2 : 1;
            BombFuseMultiplier = passives[(int)RangedPassiveId.BombShortFuse] ? 0.65f : 1.0f;
            BombFragmentCount = passives[(int)RangedPassiveId.BombFragments] ? 6 : 0;
            BombCenterBonus = passives[(int)RangedPassiveId.BombCompressedPowder] ? 0.50f : 0.0f;
            ScytheWidthMultiplier = passives[(int)RangedPassiveId.ScytheGiantBlade] ? 1.30f : 1.0f;
            ScytheReturnSpeedMultiplier = passives[(int)RangedPassiveId.ScytheSwiftReturn] ? 1.30f : 1.0f;
            ScytheDirections = passives[(int)RangedPassiveId.ScytheDoubleDirection] ? 2 : 1;
            ScytheReturnDamageMultiplier = passives[(int)RangedPassiveId.ScytheRoundTripHarvest] ? 1.35f : 1.0f;
        }
    }

    public readonly struct RangedMemberSnapshot
    {
        public RangedLineage Lineage { get; }
        public int MemberIndex { get; }
        public bool IsActive { get; }
        public RangedActionPhase Phase { get; }
        public RunPoint Slot { get; }
        public RunPoint CurrentPosition { get; }
        public RunPoint ActionOrigin { get; }
        public RunPoint LockedPoint { get; }
        public RunPoint Direction { get; }
        public bool IsClusterBomb { get; }
        public int PendingBombCount { get; }
        public float CooldownRemaining { get; }
        public float ReturnSpeedMultiplier { get; }

        internal RangedMemberSnapshot(RangedMemberState state, bool active)
        {
            Lineage = state.Lineage;
            MemberIndex = state.MemberIndex;
            IsActive = active;
            Phase = state.Phase;
            Slot = state.Slot;
            CurrentPosition = state.CurrentPosition;
            ActionOrigin = state.ActionOrigin;
            LockedPoint = state.LockedPoint;
            Direction = state.Direction;
            IsClusterBomb = state.IsClusterBomb;
            PendingBombCount = state.PendingBombCount;
            CooldownRemaining = state.CooldownRemaining;
            ReturnSpeedMultiplier = state.ReturnSpeedMultiplier;
        }
    }

    public readonly struct RangedLegionSnapshot
    {
        private readonly RangedMemberSnapshot[] _members;
        private readonly int[] _progressions;

        public bool IsEnabled { get; }
        public bool FalconReady { get; }
        public bool FalconActive { get; }
        public int FalconTargetId { get; }
        public int FalconCastCount { get; }
        public bool ClusterBombReady { get; }
        public int ClusterBombCastCount { get; }
        public bool GiantScytheReady { get; }
        public bool GiantScytheActive { get; }
        public int GiantScytheCastCount { get; }
        public RunPoint GiantScytheLastCenter { get; }
        public RangedModifierSnapshot Modifiers { get; }
        internal ulong StateDigest { get; }

        internal RangedLegionSnapshot(
            RangedMemberSnapshot[] members,
            int[] progressions,
            bool falconReady,
            bool falconActive,
            int falconTargetId,
            int falconCastCount,
            bool clusterBombReady,
            int clusterBombCastCount,
            bool giantScytheReady,
            bool giantScytheActive,
            int giantScytheCastCount,
            RunPoint giantScytheLastCenter,
            RangedModifierSnapshot modifiers,
            ulong stateDigest)
        {
            IsEnabled = true;
            _members = members;
            _progressions = progressions;
            FalconReady = falconReady;
            FalconActive = falconActive;
            FalconTargetId = falconTargetId;
            FalconCastCount = falconCastCount;
            ClusterBombReady = clusterBombReady;
            ClusterBombCastCount = clusterBombCastCount;
            GiantScytheReady = giantScytheReady;
            GiantScytheActive = giantScytheActive;
            GiantScytheCastCount = giantScytheCastCount;
            GiantScytheLastCenter = giantScytheLastCenter;
            Modifiers = modifiers;
            StateDigest = stateDigest;
        }

        public RangedMemberSnapshot GetMember(RangedLineage lineage, int memberIndex)
        {
            if (_members == null || memberIndex < 1 || memberIndex > 2)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return _members[(int)lineage * 2 + memberIndex - 1];
        }

        public int GetProgression(RangedLineage lineage)
        {
            if (_progressions == null || (int)lineage < 0 || (int)lineage >= 3)
                return 0;
            return _progressions[(int)lineage];
        }
    }

    public sealed class RangedLegionRuntime
    {
        private readonly RangedLegionDefinition _definition;
        private readonly CombatResolver _resolver;
        private readonly RangedMemberState[] _members = new RangedMemberState[6];
        private readonly int[] _progressions = new int[3];
        private int[] _snapshotProgressions = new int[3];
        private readonly bool[] _passives = new bool[12];
        private readonly int[] _hitIds = new int[64];
        private readonly float[] _hitForwards = new float[64];
        private RangedMemberSnapshot[] _snapshotMembers = Array.Empty<RangedMemberSnapshot>();
        private int _stateVersion;
        private int _snapshotVersion = -1;

        private int _falconProgress;
        private bool _falconReady;
        private bool _falconActive;
        private int _falconTargetId;
        private int _falconCastCount;
        private int _clusterProgress;
        private bool _clusterReady;
        private int _clusterCastCount;
        private int _giantScytheHitProgress;
        private bool _giantScytheReady;
        private bool _giantScytheActive;
        private int _giantScytheCastCount;
        private RunPoint _giantScytheLastCenter;

        internal bool IsEnabled => _definition.IsEnabled;

        public RangedLegionRuntime(RangedLegionDefinition definition, CombatResolver resolver)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            for (int lineage = 0; lineage < 3; lineage++)
            {
                for (int member = 1; member <= 2; member++)
                    _members[lineage * 2 + member - 1] = new RangedMemberState((RangedLineage)lineage, member);
            }
        }

        public void SetProgression(RangedLineage lineage, int progression)
        {
            if (IsEnabled == false)
                return;
            ValidateLineage(lineage);
            if (progression < _progressions[(int)lineage] || progression > 3)
                throw new ArgumentOutOfRangeException(nameof(progression));
            if (_progressions[(int)lineage] == progression)
                return;
            _progressions[(int)lineage] = progression;
            int[] progressions = new int[3];
            Array.Copy(_progressions, progressions, 3);
            _snapshotProgressions = progressions;
            _stateVersion++;
        }

        internal bool TryApplyGrowth(string baseUnitId, int progression)
        {
            RangedLineage lineage;
            if (string.Equals(baseUnitId, "falcon_archer", StringComparison.Ordinal))
                lineage = RangedLineage.Archer;
            else if (string.Equals(baseUnitId, "bombardier", StringComparison.Ordinal))
                lineage = RangedLineage.Bombardier;
            else if (string.Equals(baseUnitId, "skeleton_bomber", StringComparison.Ordinal))
                lineage = RangedLineage.Scythe;
            else
                return false;
            SetProgression(lineage, progression);
            return true;
        }

        public void SetSlot(RangedLineage lineage, int memberIndex, RunPoint slot)
        {
            if (IsEnabled == false)
                return;
            RangedMemberState member = GetMember(lineage, memberIndex);
            member.Slot = slot;
            if (member.Phase == RangedActionPhase.Idle)
                member.CurrentPosition = slot;
            _stateVersion++;
        }

        public void ApplyPassive(RangedPassiveId passive)
        {
            if (IsEnabled == false)
                return;
            int index = (int)passive;
            if (index < 0 || index >= _passives.Length)
                throw new ArgumentOutOfRangeException(nameof(passive));
            if (_passives[index] == false)
            {
                _passives[index] = true;
                _stateVersion++;
            }
        }

        public bool TryBeginBaseAttack(RangedLineage lineage, int memberIndex)
        {
            if (IsEnabled == false)
                return false;
            RangedMemberState member = GetMember(lineage, memberIndex);
            if (IsMemberActive(lineage, memberIndex) == false ||
                member.Phase != RangedActionPhase.Idle || member.CooldownRemaining > 0.0f)
            {
                return false;
            }
            if (TrySelectClosestEnemy(out CombatEntitySnapshot target) == false)
                return false;

            member.ActionOrigin = member.CurrentPosition;
            member.LockedPoint = target.Position;
            member.Direction = Normalize(target.Position.X - member.ActionOrigin.X, target.Position.Y - member.ActionOrigin.Y);
            member.CooldownRemaining = _definition.BaseAttackPeriod;
            member.WasPromotedAtStart = _progressions[(int)lineage] >= 3;
            member.IsClusterBomb = false;
            member.PendingBombCount = 0;
            member.ReturnSpeedMultiplier = 1.0f;
            if (lineage == RangedLineage.Archer)
                member.Phase = RangedActionPhase.ProjectileOutbound;
            else if (lineage == RangedLineage.Bombardier)
            {
                member.LockedPoint = SelectDensestPoint();
                member.IsClusterBomb = member.WasPromotedAtStart && _clusterReady;
                if (member.IsClusterBomb)
                {
                    _clusterReady = false;
                    _clusterCastCount++;
                }
                member.PendingBombCount = CurrentModifiers().BombCount;
                member.Phase = RangedActionPhase.ProjectileOutbound;
            }
            else
            {
                member.Direction = SelectBestLineDirection(member.ActionOrigin, _definition.ScytheDistance, _definition.ScytheWidth);
                member.ReturnSpeedMultiplier = CurrentModifiers().ScytheReturnSpeedMultiplier;
                member.Phase = RangedActionPhase.ProjectileOutbound;
            }
            _stateVersion++;
            return true;
        }

        public void ResolveArcherFlight(int memberIndex)
        {
            RangedMemberState member = GetMember(RangedLineage.Archer, memberIndex);
            if (member.Phase != RangedActionPhase.ProjectileOutbound)
                return;
            RangedModifierSnapshot modifiers = CurrentModifiers();
            for (int volley = 0; volley < modifiers.ArrowVolleys; volley++)
            {
                for (int directionIndex = 0; directionIndex < modifiers.ArrowDirections; directionIndex++)
                {
                    RunPoint direction = ResolveSpreadDirection(member.Direction, directionIndex, modifiers.ArrowDirections);
                    ApplyPiercingLine(
                        SourceId(RangedLineage.Archer, memberIndex),
                        member.ActionOrigin,
                        direction,
                        _definition.TargetRange + DistanceToCommander(member.ActionOrigin),
                        _definition.ArrowWidth,
                        _definition.ArrowPierceCount + modifiers.ArrowPierceBonus,
                        _definition.ArcherDamage,
                        modifiers.ArrowPenetrationDamageStep,
                        "archer-arrow",
                        false);
                }
            }
            if (member.WasPromotedAtStart)
            {
                _falconProgress++;
                if (_falconProgress >= _definition.FalconTrigger)
                {
                    _falconProgress -= _definition.FalconTrigger;
                    _falconReady = true;
                }
            }
            member.Phase = RangedActionPhase.Idle;
            _stateVersion++;
        }

        public bool TryLaunchFalcon()
        {
            if (_falconReady == false || _falconActive || TrySelectFalconTarget(out CombatEntitySnapshot target) == false)
                return false;
            _falconReady = false;
            _falconActive = true;
            _falconTargetId = target.EntityId;
            _falconCastCount++;
            _stateVersion++;
            return true;
        }

        public void CompleteFalcon(bool didHit)
        {
            if (_falconActive == false)
                return;
            if (didHit && IsEnemyAlive(_falconTargetId))
            {
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(RangedLineage.Archer, 3),
                    _falconTargetId,
                    _definition.FalconDamage,
                    CombatDamageKind.Direct,
                    attackId: "falcon-strike"));
            }
            _falconActive = false;
            _falconTargetId = 0;
            _stateVersion++;
        }

        public void ReportBombLanded(int memberIndex)
        {
            RangedMemberState member = GetMember(RangedLineage.Bombardier, memberIndex);
            if (member.Phase != RangedActionPhase.ProjectileOutbound)
                return;
            member.FuseRemaining = _definition.BombFuseSeconds * CurrentModifiers().BombFuseMultiplier;
            member.Phase = RangedActionPhase.Fuse;
            _stateVersion++;
        }

        public void ResolveScytheOutbound(int memberIndex)
        {
            RangedMemberState member = GetMember(RangedLineage.Scythe, memberIndex);
            if (member.Phase != RangedActionPhase.ProjectileOutbound)
                return;
            RangedModifierSnapshot modifiers = CurrentModifiers();
            int hits = 0;
            for (int directionIndex = 0; directionIndex < modifiers.ScytheDirections; directionIndex++)
            {
                RunPoint direction = directionIndex == 0
                    ? member.Direction
                    : new RunPoint(-member.Direction.X, -member.Direction.Y);
                hits += ApplyPiercingLine(
                    SourceId(RangedLineage.Scythe, memberIndex),
                    member.ActionOrigin,
                    direction,
                    _definition.ScytheDistance,
                    _definition.ScytheWidth * modifiers.ScytheWidthMultiplier,
                    int.MaxValue,
                    _definition.ScytheDamage,
                    0.0f,
                    "scythe-outbound",
                    false);
            }
            CountGiantScytheHits(member, hits);
            member.LockedPoint = new RunPoint(
                member.ActionOrigin.X + member.Direction.X * _definition.ScytheDistance,
                member.ActionOrigin.Y + member.Direction.Y * _definition.ScytheDistance);
            member.Phase = RangedActionPhase.ProjectileReturning;
            _stateVersion++;
        }

        public void ResolveScytheReturn(int memberIndex)
        {
            RangedMemberState member = GetMember(RangedLineage.Scythe, memberIndex);
            if (member.Phase != RangedActionPhase.ProjectileReturning)
                return;
            RangedModifierSnapshot modifiers = CurrentModifiers();
            RunPoint destination = member.Slot;
            RunPoint direction = Normalize(
                destination.X - member.LockedPoint.X,
                destination.Y - member.LockedPoint.Y);
            float distance = (float)Math.Sqrt(RunPoint.DistanceSquared(member.LockedPoint, destination));
            int hits = ApplyPiercingLine(
                SourceId(RangedLineage.Scythe, memberIndex),
                member.LockedPoint,
                direction,
                distance,
                _definition.ScytheWidth * modifiers.ScytheWidthMultiplier,
                int.MaxValue,
                _definition.ScytheDamage,
                0.0f,
                "scythe-return",
                true,
                modifiers.ScytheReturnDamageMultiplier);
            CountGiantScytheHits(member, hits);
            member.CurrentPosition = destination;
            member.Phase = RangedActionPhase.Idle;
            _stateVersion++;
        }

        public bool TryLaunchGiantScythe()
        {
            if (_giantScytheReady == false || _giantScytheActive)
                return false;
            _giantScytheReady = false;
            _giantScytheActive = true;
            _giantScytheCastCount++;
            _stateVersion++;
            return true;
        }

        public void ResolveGiantScytheOrbit()
        {
            if (_giantScytheActive == false)
                return;
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            _giantScytheLastCenter = commander.Position;
            float radiusSquared = _definition.GiantScytheRadius * _definition.GiantScytheRadius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    RunPoint.DistanceSquared(enemy.Position, commander.Position) > radiusSquared)
                {
                    continue;
                }
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(RangedLineage.Scythe, 3),
                    enemy.EntityId,
                    _definition.GiantScytheDamage,
                    CombatDamageKind.Area,
                    attackId: "giant-scythe"));
            }
            _giantScytheActive = false;
            _stateVersion++;
        }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (IsEnabled == false || deltaSeconds == 0.0f)
                return;
            bool changed = false;
            for (int index = 0; index < _members.Length; index++)
            {
                RangedMemberState member = _members[index];
                if (member.CooldownRemaining > 0.0f)
                {
                    member.CooldownRemaining = Math.Max(0.0f, member.CooldownRemaining - deltaSeconds);
                    changed = true;
                }
                if (member.Phase != RangedActionPhase.Fuse)
                    continue;
                member.FuseRemaining -= deltaSeconds;
                changed = true;
                if (member.FuseRemaining <= 0.0001f)
                    ExplodeBomb(member);
            }
            if (changed)
                _stateVersion++;
        }

        public RangedLegionSnapshot CreateSnapshot()
        {
            if (IsEnabled == false)
                return default;
            if (_snapshotVersion != _stateVersion)
            {
                RangedMemberSnapshot[] members = new RangedMemberSnapshot[6];
                for (int index = 0; index < _members.Length; index++)
                {
                    RangedMemberState member = _members[index];
                    members[index] = new RangedMemberSnapshot(
                        member,
                        IsMemberActive(member.Lineage, member.MemberIndex));
                }
                _snapshotMembers = members;
                _snapshotVersion = _stateVersion;
            }
            return new RangedLegionSnapshot(
                _snapshotMembers,
                _snapshotProgressions,
                _falconReady,
                _falconActive,
                _falconTargetId,
                _falconCastCount,
                _clusterReady,
                _clusterCastCount,
                _giantScytheReady,
                _giantScytheActive,
                _giantScytheCastCount,
                _giantScytheLastCenter,
                CurrentModifiers(),
                CalculateDigest());
        }

        private void ExplodeBomb(RangedMemberState member)
        {
            RangedModifierSnapshot modifiers = CurrentModifiers();
            ApplyExplosion(
                SourceId(RangedLineage.Bombardier, member.MemberIndex),
                member.LockedPoint,
                _definition.BombRadius,
                _definition.BombDamage,
                modifiers.BombCenterBonus,
                "bomb-explosion");
            if (member.IsClusterBomb)
            {
                for (int mini = 0; mini < _definition.ClusterMiniCount; mini++)
                {
                    double angle = Math.PI * 2.0 * mini / _definition.ClusterMiniCount;
                    RunPoint center = new RunPoint(
                        member.LockedPoint.X + (float)Math.Cos(angle) * _definition.ClusterMiniRadius * 0.5f,
                        member.LockedPoint.Y + (float)Math.Sin(angle) * _definition.ClusterMiniRadius * 0.5f);
                    ApplyExplosion(
                        SourceId(RangedLineage.Bombardier, 3),
                        center,
                        _definition.ClusterMiniRadius,
                        _definition.ClusterMiniDamage,
                        0.0f,
                        "cluster-mini");
                }
            }
            ApplyBombFragments(member, modifiers.BombFragmentCount);
            member.PendingBombCount--;
            if (member.PendingBombCount > 0)
            {
                member.FuseRemaining = _definition.DoubleBombDelay;
                return;
            }
            if (member.WasPromotedAtStart && member.IsClusterBomb == false)
            {
                _clusterProgress++;
                if (_clusterProgress >= _definition.ClusterTrigger)
                {
                    _clusterProgress -= _definition.ClusterTrigger;
                    _clusterReady = true;
                }
            }
            member.Phase = RangedActionPhase.Idle;
            member.IsClusterBomb = false;
        }

        private void ApplyExplosion(
            int sourceId,
            RunPoint center,
            float radius,
            int damage,
            float centerBonus,
            string attackId)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float radiusSquared = radius * radius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                float distanceSquared = RunPoint.DistanceSquared(enemy.Position, center);
                if (IsEnemy(enemy) == false || enemy.IsDead || distanceSquared > radiusSquared)
                    continue;
                float distance = (float)Math.Sqrt(distanceSquared);
                float multiplier = 1.0f + centerBonus * Math.Max(0.0f, 1.0f - distance / radius);
                _resolver.ApplyDamage(new DamageRequest(
                    sourceId,
                    enemy.EntityId,
                    damage,
                    CombatDamageKind.Area,
                    attackerDamageMultiplier: multiplier,
                    attackId: attackId));
            }
        }

        private void ApplyBombFragments(RangedMemberState member, int fragmentCount)
        {
            if (fragmentCount <= 0)
                return;
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float radius = _definition.BombRadius * 1.5f;
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
                    SourceId(RangedLineage.Bombardier, member.MemberIndex),
                    enemy.EntityId,
                    Math.Max(1.0f, _definition.BombDamage * 0.25f),
                    CombatDamageKind.Projectile,
                    attackId: "bomb-fragments"));
            }
        }

        private int ApplyPiercingLine(
            int sourceId,
            RunPoint origin,
            RunPoint direction,
            float distance,
            float halfWidth,
            int pierceCount,
            int damage,
            float penetrationStep,
            string attackId,
            bool returning,
            float damageMultiplier = 1.0f)
        {
            int count = CollectLineTargets(origin, direction, distance, halfWidth);
            int appliedCount = Math.Min(count, pierceCount);
            for (int index = 0; index < appliedCount; index++)
            {
                float multiplier = damageMultiplier * (1.0f + Math.Min(3, index) * penetrationStep);
                _resolver.ApplyDamage(new DamageRequest(
                    sourceId,
                    _hitIds[index],
                    damage,
                    CombatDamageKind.Projectile,
                    attackerDamageMultiplier: multiplier,
                    attackId: attackId));
            }
            return appliedCount;
        }

        private int CollectLineTargets(RunPoint origin, RunPoint direction, float distance, float halfWidth)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            int count = 0;
            for (int index = 0; index < combat.EntityCount && count < _hitIds.Length; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead)
                    continue;
                float x = enemy.Position.X - origin.X;
                float y = enemy.Position.Y - origin.Y;
                float forward = x * direction.X + y * direction.Y;
                float perpendicular = Math.Abs(x * direction.Y - y * direction.X);
                if (forward < 0.0f || forward > distance || perpendicular > halfWidth)
                    continue;
                int insert = count;
                while (insert > 0 && (_hitForwards[insert - 1] > forward ||
                    (_hitForwards[insert - 1] == forward && _hitIds[insert - 1] > enemy.EntityId)))
                {
                    _hitForwards[insert] = _hitForwards[insert - 1];
                    _hitIds[insert] = _hitIds[insert - 1];
                    insert--;
                }
                _hitForwards[insert] = forward;
                _hitIds[insert] = enemy.EntityId;
                count++;
            }
            return count;
        }

        private RunPoint SelectDensestPoint()
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            int bestCount = 0;
            int bestId = int.MaxValue;
            RunPoint best = default;
            float radiusSquared = _definition.BombRadius * _definition.BombRadius;
            for (int candidateIndex = 0; candidateIndex < combat.EntityCount; candidateIndex++)
            {
                CombatEntitySnapshot candidate = combat.GetEntityAt(candidateIndex);
                if (IsEnemy(candidate) == false || candidate.IsDead)
                    continue;
                int count = 0;
                for (int targetIndex = 0; targetIndex < combat.EntityCount; targetIndex++)
                {
                    CombatEntitySnapshot target = combat.GetEntityAt(targetIndex);
                    if (IsEnemy(target) && target.IsDead == false &&
                        RunPoint.DistanceSquared(target.Position, candidate.Position) <= radiusSquared)
                    {
                        count++;
                    }
                }
                if (count > bestCount || (count == bestCount && candidate.EntityId < bestId))
                {
                    bestCount = count;
                    bestId = candidate.EntityId;
                    best = candidate.Position;
                }
            }
            return best;
        }

        private RunPoint SelectBestLineDirection(RunPoint origin, float distance, float halfWidth)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            int bestCount = 0;
            int bestId = int.MaxValue;
            RunPoint best = new RunPoint(1.0f, 0.0f);
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot candidate = combat.GetEntityAt(index);
                if (IsEnemy(candidate) == false || candidate.IsDead)
                    continue;
                RunPoint direction = Normalize(candidate.Position.X - origin.X, candidate.Position.Y - origin.Y);
                int count = CollectLineTargets(origin, direction, distance, halfWidth);
                if (count > bestCount || (count == bestCount && candidate.EntityId < bestId))
                {
                    bestCount = count;
                    bestId = candidate.EntityId;
                    best = direction;
                }
            }
            return best;
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

        private bool TrySelectFalconTarget(out CombatEntitySnapshot selected)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            bool found = false;
            selected = default;
            int bestRank = -1;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead)
                    continue;
                int rank = EnemyRank(enemy.Kind);
                if (found && (rank < bestRank ||
                    (rank == bestRank && enemy.Health < selected.Health) ||
                    (rank == bestRank && enemy.Health == selected.Health && enemy.EntityId >= selected.EntityId)))
                {
                    continue;
                }
                found = true;
                bestRank = rank;
                selected = enemy;
            }
            return found;
        }

        private void CountGiantScytheHits(RangedMemberState member, int hits)
        {
            if (member.WasPromotedAtStart == false || hits <= 0)
                return;
            _giantScytheHitProgress += hits;
            if (_giantScytheHitProgress >= _definition.GiantScytheHitTrigger)
            {
                _giantScytheHitProgress -= _definition.GiantScytheHitTrigger;
                _giantScytheReady = true;
            }
        }

        private float DistanceToCommander(RunPoint point)
        {
            CombatEntitySnapshot commander = _resolver.CreateSnapshot().GetEntity(_definition.CommanderEntityId);
            return (float)Math.Sqrt(RunPoint.DistanceSquared(point, commander.Position));
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

        private RangedModifierSnapshot CurrentModifiers()
        {
            return new RangedModifierSnapshot(_passives);
        }

        private bool IsMemberActive(RangedLineage lineage, int memberIndex)
        {
            int progression = _progressions[(int)lineage];
            return memberIndex == 1 ? progression >= 1 : progression >= 2;
        }

        private RangedMemberState GetMember(RangedLineage lineage, int memberIndex)
        {
            ValidateLineage(lineage);
            if (memberIndex < 1 || memberIndex > 2)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return _members[(int)lineage * 2 + memberIndex - 1];
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
                RangedMemberState member = _members[index];
                AddDigest(ref digest, (ulong)(uint)member.Phase);
                AddPointDigest(ref digest, member.Slot);
                AddPointDigest(ref digest, member.CurrentPosition);
                AddPointDigest(ref digest, member.ActionOrigin);
                AddPointDigest(ref digest, member.LockedPoint);
                AddPointDigest(ref digest, member.Direction);
                AddFloatDigest(ref digest, member.CooldownRemaining);
                AddFloatDigest(ref digest, member.FuseRemaining);
                AddDigest(ref digest, (ulong)(uint)member.PendingBombCount);
                AddDigest(ref digest, member.IsClusterBomb ? 1UL : 0UL);
                AddDigest(ref digest, member.WasPromotedAtStart ? 1UL : 0UL);
            }
            AddDigest(ref digest, (ulong)(uint)_falconProgress);
            AddDigest(ref digest, _falconReady ? 1UL : 0UL);
            AddDigest(ref digest, _falconActive ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_falconTargetId);
            AddDigest(ref digest, (ulong)(uint)_falconCastCount);
            AddDigest(ref digest, (ulong)(uint)_clusterProgress);
            AddDigest(ref digest, _clusterReady ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_clusterCastCount);
            AddDigest(ref digest, (ulong)(uint)_giantScytheHitProgress);
            AddDigest(ref digest, _giantScytheReady ? 1UL : 0UL);
            AddDigest(ref digest, _giantScytheActive ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_giantScytheCastCount);
            AddPointDigest(ref digest, _giantScytheLastCenter);
            return digest;
        }

        private static RunPoint ResolveSpreadDirection(RunPoint baseDirection, int index, int count)
        {
            if (count <= 1 || index == 0)
                return baseDirection;
            float angle = index == 1 ? 0.20f : -0.20f;
            float cosine = (float)Math.Cos(angle);
            float sine = (float)Math.Sin(angle);
            return new RunPoint(
                baseDirection.X * cosine - baseDirection.Y * sine,
                baseDirection.X * sine + baseDirection.Y * cosine);
        }

        private static RunPoint Normalize(float x, float y)
        {
            float length = (float)Math.Sqrt(x * x + y * y);
            return length <= 0.0001f ? new RunPoint(1.0f, 0.0f) : new RunPoint(x / length, y / length);
        }

        private static bool IsEnemy(CombatEntitySnapshot entity)
        {
            return entity.Kind == CombatEntityKind.NormalEnemy ||
                entity.Kind == CombatEntityKind.EliteEnemy ||
                entity.Kind == CombatEntityKind.BossEnemy;
        }

        private static int EnemyRank(CombatEntityKind kind)
        {
            if (kind == CombatEntityKind.BossEnemy)
                return 3;
            if (kind == CombatEntityKind.EliteEnemy)
                return 2;
            return kind == CombatEntityKind.NormalEnemy ? 1 : 0;
        }

        private static int SourceId(RangedLineage lineage, int memberIndex)
        {
            return 11000 + (int)lineage * 10 + memberIndex;
        }

        private static void ValidateLineage(RangedLineage lineage)
        {
            if ((int)lineage < 0 || (int)lineage >= 3)
                throw new ArgumentOutOfRangeException(nameof(lineage));
        }

        private static void AddPointDigest(ref ulong digest, RunPoint point)
        {
            AddFloatDigest(ref digest, point.X);
            AddFloatDigest(ref digest, point.Y);
        }

        private static void AddFloatDigest(ref ulong digest, float part)
        {
            AddDigest(ref digest, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(part)));
        }

        private static void AddDigest(ref ulong digest, ulong part)
        {
            const ulong prime = 1099511628211UL;
            for (int shift = 0; shift < 64; shift += 8)
            {
                digest ^= (byte)(part >> shift);
                digest *= prime;
            }
        }
    }

    internal sealed class RangedMemberState
    {
        internal RangedLineage Lineage { get; }
        internal int MemberIndex { get; }
        internal RangedActionPhase Phase { get; set; }
        internal RunPoint Slot { get; set; }
        internal RunPoint CurrentPosition { get; set; }
        internal RunPoint ActionOrigin { get; set; }
        internal RunPoint LockedPoint { get; set; }
        internal RunPoint Direction { get; set; }
        internal float CooldownRemaining { get; set; }
        internal float FuseRemaining { get; set; }
        internal int PendingBombCount { get; set; }
        internal bool IsClusterBomb { get; set; }
        internal bool WasPromotedAtStart { get; set; }
        internal float ReturnSpeedMultiplier { get; set; } = 1.0f;

        internal RangedMemberState(RangedLineage lineage, int memberIndex)
        {
            Lineage = lineage;
            MemberIndex = memberIndex;
        }
    }
}
