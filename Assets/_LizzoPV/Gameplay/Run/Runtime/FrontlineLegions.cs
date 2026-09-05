using System;

namespace Lizzo.PV.Gameplay.Run
{
    public enum FrontlineLineage
    {
        Sword,
        Shield,
        Cleric,
    }

    public enum FrontlineActionPhase
    {
        Idle,
        Approaching,
        Returning,
        ProjectileOutbound,
        ProjectileReturning,
    }

    public enum FrontlinePassiveId
    {
        SwordGreatsword,
        SwordFocusedStrike,
        SwordAfterimage,
        SwordFootwork,
        ShieldWideStrike,
        ShieldStrongPush,
        ShieldReturnTrail,
        ShieldCloseIntercept,
        ClericPiercingLight,
        ClericSplitLight,
        ClericSwiftReturn,
        ClericFullPrayer,
    }

    public sealed class FrontlineLegionDefinition
    {
        public static FrontlineLegionDefinition Disabled { get; } = new FrontlineLegionDefinition();

        internal bool IsEnabled { get; }
        public int CommanderEntityId { get; }
        public float TargetRange { get; }
        public float MovementSpeed { get; }
        public float BaseAttackPeriod { get; }
        public int SwordDamage { get; }
        public float SwordRadius { get; }
        public int SwordPromotionTrigger { get; }
        public int SwordCrescentDamage { get; }
        public int ShieldDamage { get; }
        public float ShieldRadius { get; }
        public float ShieldPushDistance { get; }
        public float ShieldSpecialPeriod { get; }
        public int ShieldSpecialDamage { get; }
        public float ShieldSpecialRadius { get; }
        public int ClericDamage { get; }
        public int ClericHealing { get; }
        public int ClericPromotionTrigger { get; }
        public float SanctuaryRadius { get; }
        public float SanctuaryAttackSpeedMultiplier { get; }

        private FrontlineLegionDefinition()
        {
        }

        private FrontlineLegionDefinition(
            int commanderEntityId,
            float targetRange,
            float movementSpeed,
            float baseAttackPeriod,
            int swordDamage,
            float swordRadius,
            int swordPromotionTrigger,
            int swordCrescentDamage,
            int shieldDamage,
            float shieldRadius,
            float shieldPushDistance,
            float shieldSpecialPeriod,
            int shieldSpecialDamage,
            float shieldSpecialRadius,
            int clericDamage,
            int clericHealing,
            int clericPromotionTrigger,
            float sanctuaryRadius,
            float sanctuaryAttackSpeedMultiplier)
        {
            IsEnabled = true;
            CommanderEntityId = commanderEntityId;
            TargetRange = targetRange;
            MovementSpeed = movementSpeed;
            BaseAttackPeriod = baseAttackPeriod;
            SwordDamage = swordDamage;
            SwordRadius = swordRadius;
            SwordPromotionTrigger = swordPromotionTrigger;
            SwordCrescentDamage = swordCrescentDamage;
            ShieldDamage = shieldDamage;
            ShieldRadius = shieldRadius;
            ShieldPushDistance = shieldPushDistance;
            ShieldSpecialPeriod = shieldSpecialPeriod;
            ShieldSpecialDamage = shieldSpecialDamage;
            ShieldSpecialRadius = shieldSpecialRadius;
            ClericDamage = clericDamage;
            ClericHealing = clericHealing;
            ClericPromotionTrigger = clericPromotionTrigger;
            SanctuaryRadius = sanctuaryRadius;
            SanctuaryAttackSpeedMultiplier = sanctuaryAttackSpeedMultiplier;
        }

        public static FrontlineLegionDefinition Create(
            int commanderEntityId,
            float targetRange,
            float movementSpeed,
            float baseAttackPeriod,
            int swordDamage,
            float swordRadius,
            int swordPromotionTrigger,
            int swordCrescentDamage,
            int shieldDamage,
            float shieldRadius,
            float shieldPushDistance,
            float shieldSpecialPeriod,
            int shieldSpecialDamage,
            float shieldSpecialRadius,
            int clericDamage,
            int clericHealing,
            int clericPromotionTrigger,
            float sanctuaryRadius,
            float sanctuaryAttackSpeedMultiplier)
        {
            if (commanderEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(commanderEntityId));
            ValidatePositive(targetRange, nameof(targetRange));
            ValidatePositive(movementSpeed, nameof(movementSpeed));
            if (float.IsNaN(baseAttackPeriod) || float.IsInfinity(baseAttackPeriod) || baseAttackPeriod < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(baseAttackPeriod));
            ValidatePositive(swordDamage, nameof(swordDamage));
            ValidatePositive(swordRadius, nameof(swordRadius));
            ValidatePositive(swordPromotionTrigger, nameof(swordPromotionTrigger));
            ValidatePositive(swordCrescentDamage, nameof(swordCrescentDamage));
            ValidatePositive(shieldDamage, nameof(shieldDamage));
            ValidatePositive(shieldRadius, nameof(shieldRadius));
            ValidatePositive(shieldPushDistance, nameof(shieldPushDistance));
            ValidatePositive(shieldSpecialPeriod, nameof(shieldSpecialPeriod));
            ValidatePositive(shieldSpecialDamage, nameof(shieldSpecialDamage));
            ValidatePositive(shieldSpecialRadius, nameof(shieldSpecialRadius));
            ValidatePositive(clericDamage, nameof(clericDamage));
            ValidatePositive(clericHealing, nameof(clericHealing));
            ValidatePositive(clericPromotionTrigger, nameof(clericPromotionTrigger));
            ValidatePositive(sanctuaryRadius, nameof(sanctuaryRadius));
            if (float.IsNaN(sanctuaryAttackSpeedMultiplier) ||
                float.IsInfinity(sanctuaryAttackSpeedMultiplier) ||
                sanctuaryAttackSpeedMultiplier <= 1.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sanctuaryAttackSpeedMultiplier));
            }

            return new FrontlineLegionDefinition(
                commanderEntityId,
                targetRange,
                movementSpeed,
                baseAttackPeriod,
                swordDamage,
                swordRadius,
                swordPromotionTrigger,
                swordCrescentDamage,
                shieldDamage,
                shieldRadius,
                shieldPushDistance,
                shieldSpecialPeriod,
                shieldSpecialDamage,
                shieldSpecialRadius,
                clericDamage,
                clericHealing,
                clericPromotionTrigger,
                sanctuaryRadius,
                sanctuaryAttackSpeedMultiplier);
        }

        private static void ValidatePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(parameterName);
        }

        private static void ValidatePositive(int value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public readonly struct FrontlineMemberSnapshot
    {
        public FrontlineLineage Lineage { get; }
        public int MemberIndex { get; }
        public bool IsActive { get; }
        public FrontlineActionPhase Phase { get; }
        public RunPoint Slot { get; }
        public RunPoint CurrentPosition { get; }
        public RunPoint ActionOrigin { get; }
        public RunPoint ActionPoint { get; }
        public int LockedTargetId { get; }
        public RunPoint LockedTargetPoint { get; }
        public float MovementSpeed { get; }
        public float StartedAttackSpeedMultiplier { get; }
        public int ProjectileCount { get; }
        public int PierceCount { get; }
        public float CooldownRemaining { get; }

        internal FrontlineMemberSnapshot(FrontlineMemberState state, bool active)
        {
            Lineage = state.Lineage;
            MemberIndex = state.MemberIndex;
            IsActive = active;
            Phase = state.Phase;
            Slot = state.Slot;
            CurrentPosition = state.CurrentPosition;
            ActionOrigin = state.ActionOrigin;
            ActionPoint = state.ActionPoint;
            LockedTargetId = state.LockedTargetId;
            LockedTargetPoint = state.LockedTargetPoint;
            MovementSpeed = state.MovementSpeed;
            StartedAttackSpeedMultiplier = state.StartedAttackSpeedMultiplier;
            ProjectileCount = state.ProjectileCount;
            PierceCount = state.PierceCount;
            CooldownRemaining = state.CooldownRemaining;
        }
    }

    public readonly struct FrontlineModifierSnapshot
    {
        public int AppliedPassiveCount { get; }
        public float SwordRadiusMultiplier { get; }
        public float SwordFocusMultiplier { get; }
        public int SwordAfterimageCount { get; }
        public float SwordMovementMultiplier { get; }
        public float ShieldRadiusMultiplier { get; }
        public float ShieldPushMultiplier { get; }
        public int ShieldReturnTrailCount { get; }
        public float ShieldCloseDamageMultiplier { get; }
        public int ClericPierceBonus { get; }
        public int ClericProjectileCount { get; }
        public float ClericReturnSpeedMultiplier { get; }
        public float ClericHealingMultiplier { get; }

        internal FrontlineModifierSnapshot(bool[] passives)
        {
            int count = 0;
            for (int index = 0; index < passives.Length; index++)
            {
                if (passives[index])
                    count++;
            }
            AppliedPassiveCount = count;
            SwordRadiusMultiplier = passives[(int)FrontlinePassiveId.SwordGreatsword] ? 1.25f : 1.0f;
            SwordFocusMultiplier = passives[(int)FrontlinePassiveId.SwordFocusedStrike] ? 1.25f : 1.0f;
            SwordAfterimageCount = passives[(int)FrontlinePassiveId.SwordAfterimage] ? 1 : 0;
            SwordMovementMultiplier = passives[(int)FrontlinePassiveId.SwordFootwork] ? 1.25f : 1.0f;
            ShieldRadiusMultiplier = passives[(int)FrontlinePassiveId.ShieldWideStrike] ? 1.25f : 1.0f;
            ShieldPushMultiplier = passives[(int)FrontlinePassiveId.ShieldStrongPush] ? 1.25f : 1.0f;
            ShieldReturnTrailCount = passives[(int)FrontlinePassiveId.ShieldReturnTrail] ? 1 : 0;
            ShieldCloseDamageMultiplier = passives[(int)FrontlinePassiveId.ShieldCloseIntercept] ? 1.25f : 1.0f;
            ClericPierceBonus = passives[(int)FrontlinePassiveId.ClericPiercingLight] ? 1 : 0;
            ClericProjectileCount = passives[(int)FrontlinePassiveId.ClericSplitLight] ? 2 : 1;
            ClericReturnSpeedMultiplier = passives[(int)FrontlinePassiveId.ClericSwiftReturn] ? 1.25f : 1.0f;
            ClericHealingMultiplier = passives[(int)FrontlinePassiveId.ClericFullPrayer] ? 1.25f : 1.0f;
        }
    }

    public readonly struct FrontlineLegionSnapshot
    {
        private readonly FrontlineMemberSnapshot[] _members;
        private readonly int[] _progressions;

        public bool IsEnabled { get; }
        public int SwordPromotionProgress { get; }
        public int SwordPendingCrescentCount { get; }
        public int SwordCrescentCastCount { get; }
        public bool ShieldSpecialReady { get; }
        public int ShieldShockwaveCastCount { get; }
        public int ClericPromotionProgress { get; }
        public int SanctuaryRevision { get; }
        public RunPoint SanctuaryPosition { get; }
        public bool HasSanctuary => SanctuaryRevision > 0;
        public FrontlineModifierSnapshot Modifiers { get; }
        internal ulong StateDigest { get; }

        internal FrontlineLegionSnapshot(
            bool isEnabled,
            FrontlineMemberSnapshot[] members,
            int[] progressions,
            int swordPromotionProgress,
            int swordPendingCrescentCount,
            int swordCrescentCastCount,
            bool shieldSpecialReady,
            int shieldShockwaveCastCount,
            int clericPromotionProgress,
            int sanctuaryRevision,
            RunPoint sanctuaryPosition,
            FrontlineModifierSnapshot modifiers,
            ulong stateDigest)
        {
            IsEnabled = isEnabled;
            _members = members;
            _progressions = progressions;
            SwordPromotionProgress = swordPromotionProgress;
            SwordPendingCrescentCount = swordPendingCrescentCount;
            SwordCrescentCastCount = swordCrescentCastCount;
            ShieldSpecialReady = shieldSpecialReady;
            ShieldShockwaveCastCount = shieldShockwaveCastCount;
            ClericPromotionProgress = clericPromotionProgress;
            SanctuaryRevision = sanctuaryRevision;
            SanctuaryPosition = sanctuaryPosition;
            Modifiers = modifiers;
            StateDigest = stateDigest;
        }

        public FrontlineMemberSnapshot GetMember(FrontlineLineage lineage, int memberIndex)
        {
            if (memberIndex < 1 || memberIndex > 2 || _members == null)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return _members[(int)lineage * 2 + memberIndex - 1];
        }

        public int GetProgression(FrontlineLineage lineage)
        {
            if ((int)lineage < 0 || (int)lineage >= 3 || _progressions == null)
                return 0;
            return _progressions[(int)lineage];
        }
    }

    public sealed class FrontlineLegionRuntime
    {
        private readonly FrontlineLegionDefinition _definition;
        private readonly CombatResolver _resolver;
        private readonly int[] _progressions = new int[3];
        private int[] _snapshotProgressions = new int[3];
        private readonly FrontlineMemberState[] _members = new FrontlineMemberState[6];
        private readonly bool[] _passives = new bool[12];

        private int _swordPromotionProgress;
        private int _swordPendingCrescentCount;
        private int _swordCrescentCastCount;
        private float _shieldSpecialElapsed;
        private int _shieldShockwaveCastCount;
        private int _clericPromotionProgress;
        private int _sanctuaryRevision;
        private RunPoint _sanctuaryPosition;
        private FrontlineMemberSnapshot[] _snapshotMembers = Array.Empty<FrontlineMemberSnapshot>();
        private int _stateVersion;
        private int _snapshotVersion = -1;

        internal bool IsEnabled => _definition.IsEnabled;

        public FrontlineLegionRuntime(
            FrontlineLegionDefinition definition,
            CombatResolver resolver)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            for (int lineage = 0; lineage < 3; lineage++)
            {
                for (int member = 1; member <= 2; member++)
                {
                    int index = lineage * 2 + member - 1;
                    _members[index] = new FrontlineMemberState((FrontlineLineage)lineage, member);
                }
            }
        }

        public void SetProgression(FrontlineLineage lineage, int progression)
        {
            if (IsEnabled == false)
                return;
            ValidateLineage(lineage);
            if (progression < 0 || progression > 3)
                throw new ArgumentOutOfRangeException(nameof(progression));
            if (progression < _progressions[(int)lineage])
                throw new InvalidOperationException("Frontline progression cannot move backwards.");
            if (_progressions[(int)lineage] != progression)
            {
                _progressions[(int)lineage] = progression;
                int[] progressions = new int[_progressions.Length];
                Array.Copy(_progressions, progressions, progressions.Length);
                _snapshotProgressions = progressions;
                _stateVersion++;
            }
        }

        internal bool TryApplyGrowth(string baseUnitId, int progression)
        {
            FrontlineLineage lineage;
            if (string.Equals(baseUnitId, "sword_soldier", StringComparison.Ordinal))
                lineage = FrontlineLineage.Sword;
            else if (string.Equals(baseUnitId, "shield_guard", StringComparison.Ordinal))
                lineage = FrontlineLineage.Shield;
            else if (string.Equals(baseUnitId, "cleric", StringComparison.Ordinal))
                lineage = FrontlineLineage.Cleric;
            else
                return false;

            SetProgression(lineage, progression);
            return true;
        }

        public void SetSlot(
            FrontlineLineage lineage,
            int memberIndex,
            RunPoint slot)
        {
            if (IsEnabled == false)
                return;
            FrontlineMemberState member = GetMember(lineage, memberIndex);
            member.Slot = slot;
            if (member.Phase == FrontlineActionPhase.Idle)
                member.CurrentPosition = slot;
            _stateVersion++;
        }

        public void ApplyPassive(FrontlinePassiveId passive)
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

        public bool TryBeginBaseAttack(FrontlineLineage lineage, int memberIndex)
        {
            if (IsEnabled == false)
                return false;
            FrontlineMemberState member = GetMember(lineage, memberIndex);
            if (IsMemberActive(lineage, memberIndex) == false ||
                member.Phase != FrontlineActionPhase.Idle ||
                member.CooldownRemaining > 0.0f)
                return false;
            if (TrySelectClosestEnemy(out CombatEntitySnapshot target) == false)
                return false;

            CombatEntitySnapshot commander = _resolver.CreateSnapshot().GetEntity(_definition.CommanderEntityId);
            member.ActionOrigin = member.CurrentPosition;
            member.LockedTargetId = target.EntityId;
            member.LockedTargetPoint = target.Position;
            member.WasPromotedAtStart = _progressions[(int)lineage] >= 3;
            member.StartedAttackSpeedMultiplier = ResolveSanctuaryAttackSpeed(member.CurrentPosition);
            member.CooldownRemaining = _definition.BaseAttackPeriod / member.StartedAttackSpeedMultiplier;
            member.MovementSpeed = _definition.MovementSpeed;
            member.ProjectileCount = 1;
            member.PierceCount = 0;
            member.ClericDidHit = false;

            switch (lineage)
            {
                case FrontlineLineage.Sword:
                    member.ActionPoint = target.Position;
                    member.MovementSpeed *= CurrentModifiers().SwordMovementMultiplier;
                    member.Phase = FrontlineActionPhase.Approaching;
                    break;
                case FrontlineLineage.Shield:
                    member.ActionPoint = new RunPoint(
                        (commander.Position.X + target.Position.X) * 0.5f,
                        (commander.Position.Y + target.Position.Y) * 0.5f);
                    member.Phase = FrontlineActionPhase.Approaching;
                    break;
                case FrontlineLineage.Cleric:
                    FrontlineModifierSnapshot modifiers = CurrentModifiers();
                    member.ActionPoint = target.Position;
                    member.ProjectileCount = modifiers.ClericProjectileCount;
                    member.PierceCount = modifiers.ClericPierceBonus;
                    member.MovementSpeed *= modifiers.ClericReturnSpeedMultiplier;
                    member.Phase = FrontlineActionPhase.ProjectileOutbound;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lineage));
            }
            _stateVersion++;
            return true;
        }

        public void ResolveBaseImpact(FrontlineLineage lineage, int memberIndex)
        {
            if (lineage == FrontlineLineage.Cleric)
                throw new InvalidOperationException("Cleric light resolves through ResolveClericImpact.");
            FrontlineMemberState member = GetMember(lineage, memberIndex);
            if (member.Phase != FrontlineActionPhase.Approaching)
                return;

            member.CurrentPosition = member.ActionPoint;
            if (lineage == FrontlineLineage.Sword)
                ResolveSwordImpact(member);
            else
                ResolveShieldImpact(member);
            member.Phase = FrontlineActionPhase.Returning;
            _stateVersion++;
        }

        public void CompleteReturn(FrontlineLineage lineage, int memberIndex)
        {
            if (lineage == FrontlineLineage.Cleric)
                throw new InvalidOperationException("Cleric light returns through CompleteClericReturn.");
            FrontlineMemberState member = GetMember(lineage, memberIndex);
            if (member.Phase != FrontlineActionPhase.Returning)
                return;

            if (lineage == FrontlineLineage.Shield &&
                CurrentModifiers().ShieldReturnTrailCount > 0)
            {
                ApplyShieldReturnTrail(member);
            }
            member.CurrentPosition = member.Slot;
            member.Phase = FrontlineActionPhase.Idle;
            member.LockedTargetId = 0;
            if (lineage == FrontlineLineage.Sword)
                TryCastPendingSwordCrescent(member);
            _stateVersion++;
        }

        public void ResolveClericImpact(int memberIndex, bool didHit)
        {
            FrontlineMemberState member = GetMember(FrontlineLineage.Cleric, memberIndex);
            if (member.Phase != FrontlineActionPhase.ProjectileOutbound)
                return;

            member.ClericDidHit = didHit && IsEnemyAlive(member.LockedTargetId);
            if (member.ClericDidHit)
            {
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(FrontlineLineage.Cleric, memberIndex),
                    member.LockedTargetId,
                    _definition.ClericDamage,
                    CombatDamageKind.Projectile,
                    attackId: "cleric-light"));
                ApplyAdditionalClericHits(member);
            }
            member.Phase = FrontlineActionPhase.ProjectileReturning;
            _stateVersion++;
        }

        public void CompleteClericReturn(int memberIndex)
        {
            FrontlineMemberState member = GetMember(FrontlineLineage.Cleric, memberIndex);
            if (member.Phase != FrontlineActionPhase.ProjectileReturning)
                return;

            if (member.ClericDidHit)
            {
                _resolver.ApplyHealing(new HealingRequest(
                    SourceId(FrontlineLineage.Cleric, memberIndex),
                    _definition.CommanderEntityId,
                    _definition.ClericHealing,
                    CurrentModifiers().ClericHealingMultiplier));
                if (member.WasPromotedAtStart)
                {
                    _clericPromotionProgress++;
                    if (_clericPromotionProgress >= _definition.ClericPromotionTrigger)
                    {
                        _clericPromotionProgress -= _definition.ClericPromotionTrigger;
                        CombatEntitySnapshot commander = _resolver.CreateSnapshot().GetEntity(
                            _definition.CommanderEntityId);
                        _sanctuaryPosition = commander.Position;
                        _sanctuaryRevision++;
                    }
                }
            }

            member.CurrentPosition = member.Slot;
            member.Phase = FrontlineActionPhase.Idle;
            member.LockedTargetId = 0;
            member.ClericDidHit = false;
            _stateVersion++;
        }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            bool changed = false;
            for (int index = 0; index < _members.Length; index++)
            {
                FrontlineMemberState member = _members[index];
                if (member.CooldownRemaining <= 0.0f)
                    continue;
                member.CooldownRemaining = Math.Max(0.0f, member.CooldownRemaining - deltaSeconds);
                changed |= deltaSeconds > 0.0f;
            }
            if (_progressions[(int)FrontlineLineage.Shield] >= 3)
            {
                _shieldSpecialElapsed += deltaSeconds;
                changed |= deltaSeconds > 0.0f;
            }
            if (changed)
                _stateVersion++;
        }

        public bool TryCastShieldShockwave()
        {
            if (_progressions[(int)FrontlineLineage.Shield] < 3 ||
                _shieldSpecialElapsed < _definition.ShieldSpecialPeriod ||
                HasActiveBaseAction(FrontlineLineage.Shield))
            {
                return false;
            }
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            bool found = false;
            float radiusSquared = _definition.ShieldSpecialRadius * _definition.ShieldSpecialRadius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    RunPoint.DistanceSquared(enemy.Position, commander.Position) > radiusSquared)
                {
                    continue;
                }
                found = true;
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(FrontlineLineage.Shield, 3),
                    enemy.EntityId,
                    _definition.ShieldSpecialDamage,
                    CombatDamageKind.Area,
                    attackId: "shield-shockwave"));
                TryPushAwayFromCommander(
                    SourceId(FrontlineLineage.Shield, 3),
                    enemy,
                    commander.Position,
                    _definition.ShieldPushDistance);
            }
            if (found == false)
                return false;

            _shieldSpecialElapsed = 0.0f;
            _shieldShockwaveCastCount++;
            _stateVersion++;
            return true;
        }

        public FrontlineLegionSnapshot CreateSnapshot()
        {
            if (IsEnabled == false)
                return default;
            if (_snapshotVersion != _stateVersion)
            {
                FrontlineMemberSnapshot[] members = new FrontlineMemberSnapshot[_members.Length];
                for (int index = 0; index < _members.Length; index++)
                {
                    FrontlineMemberState member = _members[index];
                    members[index] = new FrontlineMemberSnapshot(
                        member,
                        IsMemberActive(member.Lineage, member.MemberIndex));
                }
                _snapshotMembers = members;
                _snapshotVersion = _stateVersion;
            }
            FrontlineModifierSnapshot modifiers = CurrentModifiers();
            ulong digest = CalculateDigest();
            return new FrontlineLegionSnapshot(
                true,
                _snapshotMembers,
                _snapshotProgressions,
                _swordPromotionProgress,
                _swordPendingCrescentCount,
                _swordCrescentCastCount,
                _progressions[(int)FrontlineLineage.Shield] >= 3 &&
                    _shieldSpecialElapsed >= _definition.ShieldSpecialPeriod,
                _shieldShockwaveCastCount,
                _clericPromotionProgress,
                _sanctuaryRevision,
                _sanctuaryPosition,
                modifiers,
                digest);
        }

        private void ApplyAdditionalClericHits(FrontlineMemberState member)
        {
            int remaining = Math.Max(0, member.ProjectileCount - 1) + member.PierceCount;
            int excludedA = member.LockedTargetId;
            int excludedB = 0;
            while (remaining > 0 && TrySelectClosestEnemyExcept(excludedA, excludedB, out CombatEntitySnapshot target))
            {
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(FrontlineLineage.Cleric, member.MemberIndex),
                    target.EntityId,
                    _definition.ClericDamage,
                    CombatDamageKind.Projectile,
                    attackId: "cleric-light-extra"));
                if (excludedB == 0)
                    excludedB = target.EntityId;
                else
                    excludedA = target.EntityId;
                remaining--;
            }
        }

        private void ApplyShieldReturnTrail(FrontlineMemberState member)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float width = _definition.ShieldRadius * 0.5f;
            float widthSquared = width * width;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    DistanceSquaredToSegment(enemy.Position, member.ActionPoint, member.Slot) > widthSquared)
                {
                    continue;
                }
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(FrontlineLineage.Shield, member.MemberIndex),
                    enemy.EntityId,
                    _definition.ShieldDamage * 0.5f,
                    CombatDamageKind.Area,
                    attackId: "shield-return-trail"));
            }
        }

        private void ResolveSwordImpact(FrontlineMemberState member)
        {
            FrontlineModifierSnapshot modifiers = CurrentModifiers();
            ApplySwordArea(member, _definition.SwordDamage, modifiers);
            for (int index = 0; index < modifiers.SwordAfterimageCount; index++)
                ApplySwordArea(member, _definition.SwordDamage, modifiers);

            if (member.WasPromotedAtStart)
            {
                _swordPromotionProgress++;
                if (_swordPromotionProgress >= _definition.SwordPromotionTrigger)
                {
                    _swordPromotionProgress -= _definition.SwordPromotionTrigger;
                    _swordPendingCrescentCount++;
                }
            }
        }

        private void ApplySwordArea(
            FrontlineMemberState member,
            int baseDamage,
            FrontlineModifierSnapshot modifiers)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float radius = _definition.SwordRadius * modifiers.SwordRadiusMultiplier;
            float radiusSquared = radius * radius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    RunPoint.DistanceSquared(enemy.Position, member.LockedTargetPoint) > radiusSquared)
                {
                    continue;
                }
                float focusMultiplier = enemy.EntityId == member.LockedTargetId
                    ? modifiers.SwordFocusMultiplier
                    : 1.0f;
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(FrontlineLineage.Sword, member.MemberIndex),
                    enemy.EntityId,
                    baseDamage,
                    CombatDamageKind.Area,
                    attackerDamageMultiplier: focusMultiplier,
                    attackId: "sword-slash"));
            }
        }

        private void ResolveShieldImpact(FrontlineMemberState member)
        {
            FrontlineModifierSnapshot modifiers = CurrentModifiers();
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            float radius = _definition.ShieldRadius * modifiers.ShieldRadiusMultiplier;
            float radiusSquared = radius * radius;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead ||
                    RunPoint.DistanceSquared(enemy.Position, member.ActionPoint) > radiusSquared)
                {
                    continue;
                }
                float targetDistance = (float)Math.Sqrt(
                    RunPoint.DistanceSquared(enemy.Position, commander.Position));
                float closeMultiplier = targetDistance <= _definition.ShieldSpecialRadius * 0.5f
                    ? modifiers.ShieldCloseDamageMultiplier
                    : 1.0f;
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(FrontlineLineage.Shield, member.MemberIndex),
                    enemy.EntityId,
                    _definition.ShieldDamage,
                    CombatDamageKind.Area,
                    attackerDamageMultiplier: closeMultiplier,
                    attackId: "shield-strike"));
                TryPushAwayFromCommander(
                    SourceId(FrontlineLineage.Shield, member.MemberIndex),
                    enemy,
                    commander.Position,
                    _definition.ShieldPushDistance * modifiers.ShieldPushMultiplier);
            }
        }

        private void TryCastPendingSwordCrescent(FrontlineMemberState member)
        {
            if (_swordPendingCrescentCount <= 0 ||
                TryResolveBestSwordLine(member.CurrentPosition, out RunPoint direction) == false)
            {
                return;
            }
            _swordPendingCrescentCount--;
            _swordCrescentCastCount++;
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float reach = ResolveProjectileReach(member.CurrentPosition, combat);
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot target = combat.GetEntityAt(index);
                if (IsEnemy(target) == false || target.IsDead ||
                    IsWithinLine(target.Position, member.CurrentPosition, direction, reach, _definition.SwordRadius) == false)
                {
                    continue;
                }
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(FrontlineLineage.Sword, 3),
                    target.EntityId,
                    _definition.SwordCrescentDamage,
                    CombatDamageKind.Projectile,
                    attackId: "sword-crescent"));
            }
        }

        private bool TryResolveBestSwordLine(RunPoint origin, out RunPoint selectedDirection)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            float reach = ResolveProjectileReach(origin, combat);
            int bestCount = 0;
            int bestTargetId = int.MaxValue;
            selectedDirection = default;
            for (int candidateIndex = 0; candidateIndex < combat.EntityCount; candidateIndex++)
            {
                CombatEntitySnapshot candidate = combat.GetEntityAt(candidateIndex);
                if (IsEnemy(candidate) == false || candidate.IsDead)
                    continue;
                float x = candidate.Position.X - origin.X;
                float y = candidate.Position.Y - origin.Y;
                float length = (float)Math.Sqrt(x * x + y * y);
                if (length <= 0.0001f || length > reach)
                    continue;
                RunPoint direction = new RunPoint(x / length, y / length);
                int count = 0;
                for (int targetIndex = 0; targetIndex < combat.EntityCount; targetIndex++)
                {
                    CombatEntitySnapshot target = combat.GetEntityAt(targetIndex);
                    if (IsEnemy(target) && target.IsDead == false &&
                        IsWithinLine(target.Position, origin, direction, reach, _definition.SwordRadius))
                    {
                        count++;
                    }
                }
                if (count > bestCount || (count == bestCount && candidate.EntityId < bestTargetId))
                {
                    bestCount = count;
                    bestTargetId = candidate.EntityId;
                    selectedDirection = direction;
                }
            }
            return bestCount > 0;
        }

        private float ResolveProjectileReach(RunPoint origin, CombatEffectsSnapshot combat)
        {
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            float slotOffset = (float)Math.Sqrt(RunPoint.DistanceSquared(origin, commander.Position));
            return _definition.TargetRange + slotOffset;
        }

        private static bool IsWithinLine(
            RunPoint point,
            RunPoint origin,
            RunPoint direction,
            float reach,
            float halfWidth)
        {
            float x = point.X - origin.X;
            float y = point.Y - origin.Y;
            float forward = x * direction.X + y * direction.Y;
            if (forward < 0.0f || forward > reach)
                return false;
            float perpendicular = Math.Abs(x * direction.Y - y * direction.X);
            return perpendicular <= halfWidth;
        }

        private void TryPushAwayFromCommander(
            int sourceId,
            CombatEntitySnapshot enemy,
            RunPoint commanderPosition,
            float distance)
        {
            RunPoint direction = new RunPoint(
                enemy.Position.X - commanderPosition.X,
                enemy.Position.Y - commanderPosition.Y);
            if (direction.X == 0.0f && direction.Y == 0.0f)
                direction = new RunPoint(0.0f, -1.0f);
            _resolver.ResolveForcedMovement(new[]
            {
                new ForcedMovementRequest(
                    sourceId,
                    enemy.EntityId,
                    ForcedMovementKind.Push,
                    direction,
                    distance,
                    0,
                    distance,
                    1.0f),
            });
        }

        private bool TrySelectClosestEnemy(out CombatEntitySnapshot selected)
        {
            return TrySelectClosestEnemyExcept(0, 0, out selected);
        }

        private bool TrySelectClosestEnemyExcept(
            int excludedA,
            int excludedB,
            out CombatEntitySnapshot selected)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            selected = default;
            bool found = false;
            float bestDistance = _definition.TargetRange * _definition.TargetRange;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot candidate = combat.GetEntityAt(index);
                if (IsEnemy(candidate) == false || candidate.IsDead ||
                    candidate.EntityId == excludedA || candidate.EntityId == excludedB)
                    continue;
                float distance = RunPoint.DistanceSquared(candidate.Position, commander.Position);
                if (distance > bestDistance)
                    continue;
                if (found && (distance > bestDistance ||
                    (distance == bestDistance && candidate.EntityId >= selected.EntityId)))
                {
                    continue;
                }
                selected = candidate;
                bestDistance = distance;
                found = true;
            }
            return found;
        }

        private ulong CalculateDigest()
        {
            ulong digest = 14695981039346656037UL;
            for (int index = 0; index < _progressions.Length; index++)
                AddDigest(ref digest, (ulong)(uint)_progressions[index]);
            for (int index = 0; index < _members.Length; index++)
            {
                FrontlineMemberState member = _members[index];
                AddDigest(ref digest, (ulong)(uint)member.Lineage);
                AddDigest(ref digest, (ulong)(uint)member.MemberIndex);
                AddDigest(ref digest, (ulong)(uint)member.Phase);
                AddPointDigest(ref digest, member.Slot);
                AddPointDigest(ref digest, member.CurrentPosition);
                AddPointDigest(ref digest, member.ActionOrigin);
                AddPointDigest(ref digest, member.ActionPoint);
                AddDigest(ref digest, (ulong)(uint)member.LockedTargetId);
                AddPointDigest(ref digest, member.LockedTargetPoint);
                AddFloatDigest(ref digest, member.MovementSpeed);
                AddFloatDigest(ref digest, member.StartedAttackSpeedMultiplier);
                AddDigest(ref digest, (ulong)(uint)member.ProjectileCount);
                AddDigest(ref digest, (ulong)(uint)member.PierceCount);
                AddFloatDigest(ref digest, member.CooldownRemaining);
                AddDigest(ref digest, member.WasPromotedAtStart ? 1UL : 0UL);
                AddDigest(ref digest, member.ClericDidHit ? 1UL : 0UL);
            }
            for (int index = 0; index < _passives.Length; index++)
                AddDigest(ref digest, _passives[index] ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_swordPromotionProgress);
            AddDigest(ref digest, (ulong)(uint)_swordPendingCrescentCount);
            AddDigest(ref digest, (ulong)(uint)_swordCrescentCastCount);
            AddFloatDigest(ref digest, _shieldSpecialElapsed);
            AddDigest(ref digest, (ulong)(uint)_shieldShockwaveCastCount);
            AddDigest(ref digest, (ulong)(uint)_clericPromotionProgress);
            AddDigest(ref digest, (ulong)(uint)_sanctuaryRevision);
            AddPointDigest(ref digest, _sanctuaryPosition);
            return digest;
        }

        private static float DistanceSquaredToSegment(RunPoint point, RunPoint start, RunPoint end)
        {
            float x = end.X - start.X;
            float y = end.Y - start.Y;
            float lengthSquared = x * x + y * y;
            if (lengthSquared <= 0.000001f)
                return RunPoint.DistanceSquared(point, start);
            float projection = ((point.X - start.X) * x + (point.Y - start.Y) * y) / lengthSquared;
            projection = Math.Max(0.0f, Math.Min(1.0f, projection));
            RunPoint closest = new RunPoint(start.X + x * projection, start.Y + y * projection);
            return RunPoint.DistanceSquared(point, closest);
        }

        private static void AddPointDigest(ref ulong value, RunPoint point)
        {
            AddFloatDigest(ref value, point.X);
            AddFloatDigest(ref value, point.Y);
        }

        private static void AddFloatDigest(ref ulong value, float part)
        {
            AddDigest(ref value, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(part)));
        }

        private static void AddDigest(ref ulong value, ulong part)
        {
            const ulong prime = 1099511628211UL;
            for (int shift = 0; shift < 64; shift += 8)
            {
                value ^= (byte)(part >> shift);
                value *= prime;
            }
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

        private float ResolveSanctuaryAttackSpeed(RunPoint memberPosition)
        {
            if (_sanctuaryRevision <= 0)
                return 1.0f;
            float radiusSquared = _definition.SanctuaryRadius * _definition.SanctuaryRadius;
            return RunPoint.DistanceSquared(memberPosition, _sanctuaryPosition) <= radiusSquared
                ? _definition.SanctuaryAttackSpeedMultiplier
                : 1.0f;
        }

        private bool HasActiveBaseAction(FrontlineLineage lineage)
        {
            for (int member = 1; member <= 2; member++)
            {
                if (GetMember(lineage, member).Phase != FrontlineActionPhase.Idle)
                    return true;
            }
            return false;
        }

        private bool IsMemberActive(FrontlineLineage lineage, int memberIndex)
        {
            int progression = _progressions[(int)lineage];
            return memberIndex == 1 ? progression >= 1 : progression >= 2;
        }

        private FrontlineMemberState GetMember(FrontlineLineage lineage, int memberIndex)
        {
            ValidateLineage(lineage);
            if (memberIndex < 1 || memberIndex > 2)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return _members[(int)lineage * 2 + memberIndex - 1];
        }

        private FrontlineModifierSnapshot CurrentModifiers()
        {
            return new FrontlineModifierSnapshot(_passives);
        }

        private static bool IsEnemy(CombatEntitySnapshot entity)
        {
            return entity.Kind == CombatEntityKind.NormalEnemy ||
                entity.Kind == CombatEntityKind.EliteEnemy ||
                entity.Kind == CombatEntityKind.BossEnemy;
        }

        private static int SourceId(FrontlineLineage lineage, int memberIndex)
        {
            return 10000 + (int)lineage * 10 + memberIndex;
        }

        private static void ValidateLineage(FrontlineLineage lineage)
        {
            if ((int)lineage < 0 || (int)lineage > 2)
                throw new ArgumentOutOfRangeException(nameof(lineage));
        }
    }

    internal sealed class FrontlineMemberState
    {
        internal FrontlineLineage Lineage { get; }
        internal int MemberIndex { get; }
        internal FrontlineActionPhase Phase { get; set; }
        internal RunPoint Slot { get; set; }
        internal RunPoint CurrentPosition { get; set; }
        internal RunPoint ActionOrigin { get; set; }
        internal RunPoint ActionPoint { get; set; }
        internal int LockedTargetId { get; set; }
        internal RunPoint LockedTargetPoint { get; set; }
        internal float MovementSpeed { get; set; }
        internal float StartedAttackSpeedMultiplier { get; set; } = 1.0f;
        internal int ProjectileCount { get; set; } = 1;
        internal int PierceCount { get; set; }
        internal float CooldownRemaining { get; set; }
        internal bool WasPromotedAtStart { get; set; }
        internal bool ClericDidHit { get; set; }

        internal FrontlineMemberState(FrontlineLineage lineage, int memberIndex)
        {
            Lineage = lineage;
            MemberIndex = memberIndex;
        }
    }
}
