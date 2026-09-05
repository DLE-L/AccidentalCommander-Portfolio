using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public enum CasterLineage
    {
        Herbalist,
        Fire,
        Lightning,
    }

    public enum CasterPassiveId
    {
        HerbalistWideFlask,
        HerbalistConcentratedMixture,
        HerbalistLongReaction,
        HerbalistReactiveCompound,
        FireWideField,
        FireLongBurn,
        FireRapidCombustion,
        FireAdditionalField,
        LightningAdditionalChains,
        LightningConductiveArc,
        LightningLongShock,
        LightningWideOverload,
    }

    public sealed class HerbalistActionDefinition
    {
        public int Damage { get; }
        public float Radius { get; }
        public float VulnerabilityMagnitude { get; }
        public float VulnerabilityDuration { get; }
        public float SpreadRadius { get; }
        public int SpreadMaxTargets { get; }
        public int SpreadMaxDepth { get; }

        private HerbalistActionDefinition(
            int damage,
            float radius,
            float vulnerabilityMagnitude,
            float vulnerabilityDuration,
            float spreadRadius,
            int spreadMaxTargets,
            int spreadMaxDepth)
        {
            Damage = damage;
            Radius = radius;
            VulnerabilityMagnitude = vulnerabilityMagnitude;
            VulnerabilityDuration = vulnerabilityDuration;
            SpreadRadius = spreadRadius;
            SpreadMaxTargets = spreadMaxTargets;
            SpreadMaxDepth = spreadMaxDepth;
        }

        public static HerbalistActionDefinition Create(
            int damage,
            float radius,
            float vulnerabilityMagnitude,
            float vulnerabilityDuration,
            float spreadRadius,
            int spreadMaxTargets,
            int spreadMaxDepth)
        {
            ValidatePositive(damage, nameof(damage));
            ValidatePositive(radius, nameof(radius));
            ValidatePositive(vulnerabilityMagnitude, nameof(vulnerabilityMagnitude));
            ValidatePositive(vulnerabilityDuration, nameof(vulnerabilityDuration));
            ValidatePositive(spreadRadius, nameof(spreadRadius));
            ValidatePositive(spreadMaxTargets, nameof(spreadMaxTargets));
            ValidatePositive(spreadMaxDepth, nameof(spreadMaxDepth));
            return new HerbalistActionDefinition(
                damage,
                radius,
                vulnerabilityMagnitude,
                vulnerabilityDuration,
                spreadRadius,
                spreadMaxTargets,
                spreadMaxDepth);
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

    public sealed class FireFieldActionDefinition
    {
        public int TickDamage { get; }
        public float Radius { get; }
        public float TickInterval { get; }
        public float Duration { get; }
        public int MaxActiveFields { get; }
        public int IgnitionTrigger { get; }
        public int IgnitionDamage { get; }
        public float IgnitionDurationExtension { get; }

        private FireFieldActionDefinition(
            int tickDamage,
            float radius,
            float tickInterval,
            float duration,
            int maxActiveFields,
            int ignitionTrigger,
            int ignitionDamage,
            float ignitionDurationExtension)
        {
            TickDamage = tickDamage;
            Radius = radius;
            TickInterval = tickInterval;
            Duration = duration;
            MaxActiveFields = maxActiveFields;
            IgnitionTrigger = ignitionTrigger;
            IgnitionDamage = ignitionDamage;
            IgnitionDurationExtension = ignitionDurationExtension;
        }

        public static FireFieldActionDefinition Create(
            int tickDamage,
            float radius,
            float tickInterval,
            float duration,
            int maxActiveFields,
            int ignitionTrigger,
            int ignitionDamage,
            float ignitionDurationExtension)
        {
            ValidatePositive(tickDamage, nameof(tickDamage));
            ValidatePositive(radius, nameof(radius));
            ValidatePositive(tickInterval, nameof(tickInterval));
            ValidatePositive(duration, nameof(duration));
            ValidatePositive(maxActiveFields, nameof(maxActiveFields));
            ValidatePositive(ignitionTrigger, nameof(ignitionTrigger));
            ValidatePositive(ignitionDamage, nameof(ignitionDamage));
            ValidatePositive(ignitionDurationExtension, nameof(ignitionDurationExtension));
            return new FireFieldActionDefinition(
                tickDamage,
                radius,
                tickInterval,
                duration,
                maxActiveFields,
                ignitionTrigger,
                ignitionDamage,
                ignitionDurationExtension);
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

    public sealed class LightningActionDefinition
    {
        public int Damage { get; }
        public float ChainDistance { get; }
        public int MaxTargets { get; }
        public float ShockMagnitude { get; }
        public float ShockDuration { get; }
        public int OverloadTrigger { get; }
        public int OverloadDamage { get; }
        public float OverloadRadius { get; }
        public int OverloadMaxAnchors { get; }

        private LightningActionDefinition(
            int damage,
            float chainDistance,
            int maxTargets,
            float shockMagnitude,
            float shockDuration,
            int overloadTrigger,
            int overloadDamage,
            float overloadRadius,
            int overloadMaxAnchors)
        {
            Damage = damage;
            ChainDistance = chainDistance;
            MaxTargets = maxTargets;
            ShockMagnitude = shockMagnitude;
            ShockDuration = shockDuration;
            OverloadTrigger = overloadTrigger;
            OverloadDamage = overloadDamage;
            OverloadRadius = overloadRadius;
            OverloadMaxAnchors = overloadMaxAnchors;
        }

        public static LightningActionDefinition Create(
            int damage,
            float chainDistance,
            int maxTargets,
            float shockMagnitude,
            float shockDuration,
            int overloadTrigger,
            int overloadDamage,
            float overloadRadius,
            int overloadMaxAnchors)
        {
            ValidatePositive(damage, nameof(damage));
            ValidatePositive(chainDistance, nameof(chainDistance));
            ValidatePositive(maxTargets, nameof(maxTargets));
            ValidatePositive(shockMagnitude, nameof(shockMagnitude));
            ValidatePositive(shockDuration, nameof(shockDuration));
            ValidatePositive(overloadTrigger, nameof(overloadTrigger));
            ValidatePositive(overloadDamage, nameof(overloadDamage));
            ValidatePositive(overloadRadius, nameof(overloadRadius));
            ValidatePositive(overloadMaxAnchors, nameof(overloadMaxAnchors));
            return new LightningActionDefinition(
                damage,
                chainDistance,
                maxTargets,
                shockMagnitude,
                shockDuration,
                overloadTrigger,
                overloadDamage,
                overloadRadius,
                overloadMaxAnchors);
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

    public sealed class CasterLegionDefinition
    {
        public static CasterLegionDefinition Disabled { get; } = new CasterLegionDefinition();

        internal bool IsEnabled { get; }
        public int CommanderEntityId { get; }
        public float TargetRange { get; }
        public float BaseAttackPeriod { get; }
        public HerbalistActionDefinition Herbalist { get; }
        public FireFieldActionDefinition Fire { get; }
        public LightningActionDefinition Lightning { get; }

        private CasterLegionDefinition()
        {
        }

        private CasterLegionDefinition(
            int commanderEntityId,
            float targetRange,
            float baseAttackPeriod,
            HerbalistActionDefinition herbalist,
            FireFieldActionDefinition fire,
            LightningActionDefinition lightning)
        {
            IsEnabled = true;
            CommanderEntityId = commanderEntityId;
            TargetRange = targetRange;
            BaseAttackPeriod = baseAttackPeriod;
            Herbalist = herbalist;
            Fire = fire;
            Lightning = lightning;
        }

        public static CasterLegionDefinition Create(
            int commanderEntityId,
            float targetRange,
            float baseAttackPeriod,
            HerbalistActionDefinition herbalist,
            FireFieldActionDefinition fire,
            LightningActionDefinition lightning)
        {
            if (commanderEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(commanderEntityId));
            if (float.IsNaN(targetRange) || float.IsInfinity(targetRange) || targetRange <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(targetRange));
            if (float.IsNaN(baseAttackPeriod) || float.IsInfinity(baseAttackPeriod) || baseAttackPeriod < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(baseAttackPeriod));
            return new CasterLegionDefinition(
                commanderEntityId,
                targetRange,
                baseAttackPeriod,
                herbalist ?? throw new ArgumentNullException(nameof(herbalist)),
                fire ?? throw new ArgumentNullException(nameof(fire)),
                lightning ?? throw new ArgumentNullException(nameof(lightning)));
        }
    }

    public readonly struct CasterModifierSnapshot
    {
        public int AppliedPassiveCount { get; }
        public float FlaskRadiusMultiplier { get; }
        public float VulnerabilityMagnitudeBonus { get; }
        public float VulnerabilityDurationMultiplier { get; }
        public int SpreadTargetBonus { get; }
        public float FieldRadiusMultiplier { get; }
        public float FieldDurationMultiplier { get; }
        public float FieldTickIntervalMultiplier { get; }
        public int FieldCapacityBonus { get; }
        public int ChainTargetBonus { get; }
        public float ChainDamageRetention { get; }
        public float ShockDurationMultiplier { get; }
        public float OverloadRadiusMultiplier { get; }

        internal CasterModifierSnapshot(bool[] passives)
        {
            int count = 0;
            for (int index = 0; index < passives.Length; index++)
            {
                if (passives[index])
                    count++;
            }
            AppliedPassiveCount = count;
            FlaskRadiusMultiplier = passives[(int)CasterPassiveId.HerbalistWideFlask] ? 1.3f : 1.0f;
            VulnerabilityMagnitudeBonus = passives[(int)CasterPassiveId.HerbalistConcentratedMixture] ? 0.15f : 0.0f;
            VulnerabilityDurationMultiplier = passives[(int)CasterPassiveId.HerbalistLongReaction] ? 1.5f : 1.0f;
            SpreadTargetBonus = passives[(int)CasterPassiveId.HerbalistReactiveCompound] ? 2 : 0;
            FieldRadiusMultiplier = passives[(int)CasterPassiveId.FireWideField] ? 1.3f : 1.0f;
            FieldDurationMultiplier = passives[(int)CasterPassiveId.FireLongBurn] ? 1.5f : 1.0f;
            FieldTickIntervalMultiplier = passives[(int)CasterPassiveId.FireRapidCombustion] ? 0.7f : 1.0f;
            FieldCapacityBonus = passives[(int)CasterPassiveId.FireAdditionalField] ? 1 : 0;
            ChainTargetBonus = passives[(int)CasterPassiveId.LightningAdditionalChains] ? 2 : 0;
            ChainDamageRetention = passives[(int)CasterPassiveId.LightningConductiveArc] ? 0.10f : 0.0f;
            ShockDurationMultiplier = passives[(int)CasterPassiveId.LightningLongShock] ? 1.5f : 1.0f;
            OverloadRadiusMultiplier = passives[(int)CasterPassiveId.LightningWideOverload] ? 1.35f : 1.0f;
        }
    }

    public readonly struct CasterMemberSnapshot
    {
        public CasterLineage Lineage { get; }
        public int MemberIndex { get; }
        public bool IsActive { get; }
        public RunPoint Slot { get; }
        public float CooldownRemaining { get; }

        internal CasterMemberSnapshot(CasterMemberState state, bool isActive)
        {
            Lineage = state.Lineage;
            MemberIndex = state.MemberIndex;
            IsActive = isActive;
            Slot = state.Slot;
            CooldownRemaining = state.CooldownRemaining;
        }
    }

    public readonly struct FireFieldSnapshot
    {
        public int Sequence { get; }
        public RunPoint Center { get; }
        public float Radius { get; }
        public float RemainingSeconds { get; }

        internal FireFieldSnapshot(FireFieldState state)
        {
            Sequence = state.Sequence;
            Center = state.Center;
            Radius = state.Radius;
            RemainingSeconds = state.RemainingSeconds;
        }
    }

    public readonly struct CasterLegionSnapshot
    {
        private readonly CasterMemberSnapshot[] _members;
        private readonly int[] _progressions;
        private readonly FireFieldSnapshot[] _fields;

        public bool IsEnabled { get; }
        public int ActiveFieldCount => _fields == null ? 0 : _fields.Length;
        public bool IgnitionReady { get; }
        public int IgnitionCastCount { get; }
        public bool OverloadReady { get; }
        public int OverloadCastCount { get; }
        public int VulnerabilitySpreadCount { get; }
        public CasterModifierSnapshot Modifiers { get; }
        internal ulong StateDigest { get; }

        internal CasterLegionSnapshot(
            CasterMemberSnapshot[] members,
            int[] progressions,
            FireFieldSnapshot[] fields,
            bool ignitionReady,
            int ignitionCastCount,
            bool overloadReady,
            int overloadCastCount,
            int vulnerabilitySpreadCount,
            CasterModifierSnapshot modifiers,
            ulong stateDigest)
        {
            IsEnabled = true;
            _members = members;
            _progressions = progressions;
            _fields = fields;
            IgnitionReady = ignitionReady;
            IgnitionCastCount = ignitionCastCount;
            OverloadReady = overloadReady;
            OverloadCastCount = overloadCastCount;
            VulnerabilitySpreadCount = vulnerabilitySpreadCount;
            Modifiers = modifiers;
            StateDigest = stateDigest;
        }

        public CasterMemberSnapshot GetMember(CasterLineage lineage, int memberIndex)
        {
            if (_members == null || memberIndex < 1 || memberIndex > 2)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return _members[(int)lineage * 2 + memberIndex - 1];
        }

        public int GetProgression(CasterLineage lineage)
        {
            if (_progressions == null || (int)lineage < 0 || (int)lineage >= 3)
                return 0;
            return _progressions[(int)lineage];
        }

        public FireFieldSnapshot GetField(int index)
        {
            if (_fields == null || index < 0 || index >= _fields.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _fields[index];
        }
    }

    public sealed class CasterLegionRuntime
    {
        private const float TimeEpsilon = 0.0001f;

        private readonly CasterLegionDefinition _definition;
        private readonly CombatResolver _resolver;
        private readonly CasterMemberState[] _members = new CasterMemberState[6];
        private readonly int[] _progressions = new int[3];
        private readonly bool[] _passives = new bool[12];
        private readonly List<FireFieldState> _fields = new List<FireFieldState>(4);
        private int[] _snapshotProgressions = new int[3];
        private CasterMemberSnapshot[] _snapshotMembers = Array.Empty<CasterMemberSnapshot>();
        private FireFieldSnapshot[] _snapshotFields = Array.Empty<FireFieldSnapshot>();
        private int[] _targetIds = new int[16];
        private int _fieldSequence;
        private int _ignitionProgress;
        private bool _ignitionReady;
        private int _ignitionCastCount;
        private int _overloadProgress;
        private bool _overloadReady;
        private int _overloadCastCount;
        private int _vulnerabilitySpreadCount;
        private int _stateVersion;
        private int _snapshotVersion = -1;

        internal bool IsEnabled => _definition.IsEnabled;

        public CasterLegionRuntime(CasterLegionDefinition definition, CombatResolver resolver)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            for (int lineage = 0; lineage < 3; lineage++)
            {
                for (int memberIndex = 1; memberIndex <= 2; memberIndex++)
                    _members[lineage * 2 + memberIndex - 1] = new CasterMemberState((CasterLineage)lineage, memberIndex);
            }
        }

        public void SetProgression(CasterLineage lineage, int progression)
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
            CasterLineage lineage;
            if (string.Equals(baseUnitId, "field_herbalist", StringComparison.Ordinal))
                lineage = CasterLineage.Herbalist;
            else if (string.Equals(baseUnitId, "fire_mage", StringComparison.Ordinal))
                lineage = CasterLineage.Fire;
            else if (string.Equals(baseUnitId, "lightning_mage", StringComparison.Ordinal))
                lineage = CasterLineage.Lightning;
            else
                return false;
            SetProgression(lineage, progression);
            return true;
        }

        public void SetSlot(CasterLineage lineage, int memberIndex, RunPoint slot)
        {
            if (IsEnabled == false)
                return;
            CasterMemberState member = GetMember(lineage, memberIndex);
            if (member.Slot.Equals(slot))
                return;
            member.Slot = slot;
            MarkChanged();
        }

        public void ApplyPassive(CasterPassiveId passive)
        {
            if (IsEnabled == false)
                return;
            int index = (int)passive;
            if (index < 0 || index >= _passives.Length)
                throw new ArgumentOutOfRangeException(nameof(passive));
            if (_passives[index])
                return;
            _passives[index] = true;
            TrimOldestFieldsToCapacity();
            MarkChanged();
        }

        public bool TryCastBase(CasterLineage lineage, int memberIndex)
        {
            if (IsEnabled == false)
                return false;
            CasterMemberState member = GetMember(lineage, memberIndex);
            if (IsMemberActive(lineage, memberIndex) == false || member.CooldownRemaining > TimeEpsilon)
                return false;
            if (TrySelectClosestEnemy(out CombatEntitySnapshot firstTarget) == false)
                return false;

            member.CooldownRemaining = _definition.BaseAttackPeriod;
            bool promotedAtStart = _progressions[(int)lineage] >= 3;
            if (lineage == CasterLineage.Herbalist)
                ResolveHerbalistFlask(memberIndex, SelectDensestPoint(_definition.Herbalist.Radius));
            else if (lineage == CasterLineage.Fire)
                CreateFireField(memberIndex, SelectDensestPoint(_definition.Fire.Radius), promotedAtStart);
            else
                ResolveChainLightning(memberIndex, firstTarget, promotedAtStart);
            MarkChanged();
            return true;
        }

        public bool TrySpreadVulnerability(int deadEnemyId, int reactionDepth)
        {
            if (IsEnabled == false || _progressions[(int)CasterLineage.Herbalist] < 3 ||
                reactionDepth < 0 || reactionDepth >= _definition.Herbalist.SpreadMaxDepth)
            {
                return false;
            }

            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot dead = combat.GetEntity(deadEnemyId);
            if (dead.IsDead == false || HasHerbalistVulnerability(dead) == false)
                return false;

            CasterModifierSnapshot modifiers = CurrentModifiers();
            int maxTargets = _definition.Herbalist.SpreadMaxTargets + modifiers.SpreadTargetBonus;
            float radiusSquared = _definition.Herbalist.SpreadRadius * _definition.Herbalist.SpreadRadius;
            int count = CollectNearestEnemies(dead.Position, radiusSquared, deadEnemyId, statusRequired: false);
            int applied = 0;
            for (int index = 0; index < count && applied < maxTargets; index++)
            {
                CombatEntitySnapshot target = combat.GetEntity(_targetIds[index]);
                if (target.IsDead)
                    continue;
                _resolver.ApplyStatus(new StatusRequest(
                    SourceId(CasterLineage.Herbalist, 3),
                    target.EntityId,
                    CombatStatusKind.Vulnerable,
                    _definition.Herbalist.VulnerabilityMagnitude + modifiers.VulnerabilityMagnitudeBonus,
                    _definition.Herbalist.VulnerabilityDuration * modifiers.VulnerabilityDurationMultiplier));
                applied++;
            }
            if (applied == 0)
                return false;
            _vulnerabilitySpreadCount++;
            MarkChanged();
            return true;
        }

        public bool TryIgniteFields()
        {
            if (IsEnabled == false || _ignitionReady == false || _fields.Count == 0)
                return false;
            if (HasEnemyInsideAnyField() == false)
                return false;

            for (int fieldIndex = 0; fieldIndex < _fields.Count; fieldIndex++)
            {
                FireFieldState field = _fields[fieldIndex];
                ApplyAreaDamage(
                    SourceId(CasterLineage.Fire, 3),
                    field.Center,
                    field.Radius,
                    _definition.Fire.IgnitionDamage,
                    "fire-ignition");
                field.RemainingSeconds += _definition.Fire.IgnitionDurationExtension;
            }
            _ignitionReady = false;
            _ignitionCastCount++;
            MarkChanged();
            return true;
        }

        public bool TryResolveShockOverload()
        {
            if (IsEnabled == false || _overloadReady == false)
                return false;
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            float rangeSquared = _definition.TargetRange * _definition.TargetRange;
            int count = CollectNearestEnemies(commander.Position, rangeSquared, 0, statusRequired: true);
            if (count == 0)
                return false;

            CasterModifierSnapshot modifiers = CurrentModifiers();
            int anchors = Math.Min(count, _definition.Lightning.OverloadMaxAnchors);
            for (int index = 0; index < anchors; index++)
            {
                int targetId = _targetIds[index];
                CombatEntitySnapshot anchor = combat.GetEntity(targetId);
                ApplyAreaDamage(
                    SourceId(CasterLineage.Lightning, 3),
                    anchor.Position,
                    _definition.Lightning.OverloadRadius * modifiers.OverloadRadiusMultiplier,
                    _definition.Lightning.OverloadDamage,
                    "shock-overload");
                _resolver.RemoveStatuses(targetId, CombatStatusKind.Shock);
            }
            _overloadReady = false;
            _overloadCastCount++;
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
                CasterMemberState member = _members[index];
                if (member.CooldownRemaining <= 0.0f)
                    continue;
                member.CooldownRemaining = Math.Max(0.0f, member.CooldownRemaining - deltaSeconds);
                changed = true;
            }

            for (int index = _fields.Count - 1; index >= 0; index--)
            {
                FireFieldState field = _fields[index];
                float activeDelta = Math.Min(deltaSeconds, field.RemainingSeconds);
                field.RemainingSeconds -= deltaSeconds;
                field.TickRemaining -= activeDelta;
                while (field.TickRemaining <= TimeEpsilon && activeDelta > 0.0f)
                {
                    ApplyAreaDamage(
                        SourceId(CasterLineage.Fire, field.SourceMemberIndex),
                        field.Center,
                        field.Radius,
                        _definition.Fire.TickDamage,
                        "fire-field-tick");
                    field.TickRemaining += field.TickInterval;
                }
                if (field.RemainingSeconds <= TimeEpsilon)
                    _fields.RemoveAt(index);
                changed = true;
            }
            if (changed)
                MarkChanged();
        }

        public CasterLegionSnapshot CreateSnapshot()
        {
            if (IsEnabled == false)
                return default;
            if (_snapshotVersion != _stateVersion)
            {
                CasterMemberSnapshot[] members = new CasterMemberSnapshot[6];
                for (int index = 0; index < _members.Length; index++)
                {
                    CasterMemberState member = _members[index];
                    members[index] = new CasterMemberSnapshot(
                        member,
                        IsMemberActive(member.Lineage, member.MemberIndex));
                }
                FireFieldSnapshot[] fields = new FireFieldSnapshot[_fields.Count];
                for (int index = 0; index < _fields.Count; index++)
                    fields[index] = new FireFieldSnapshot(_fields[index]);
                _snapshotMembers = members;
                _snapshotFields = fields;
                _snapshotVersion = _stateVersion;
            }
            return new CasterLegionSnapshot(
                _snapshotMembers,
                _snapshotProgressions,
                _snapshotFields,
                _ignitionReady,
                _ignitionCastCount,
                _overloadReady,
                _overloadCastCount,
                _vulnerabilitySpreadCount,
                CurrentModifiers(),
                CalculateDigest());
        }

        private void ResolveHerbalistFlask(int memberIndex, RunPoint center)
        {
            CasterModifierSnapshot modifiers = CurrentModifiers();
            float radius = _definition.Herbalist.Radius * modifiers.FlaskRadiusMultiplier;
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
                    SourceId(CasterLineage.Herbalist, memberIndex),
                    enemy.EntityId,
                    _definition.Herbalist.Damage,
                    CombatDamageKind.Area,
                    attackId: "herbalist-flask"));
                if (_resolver.CreateSnapshot().GetEntity(enemy.EntityId).IsDead == false)
                {
                    _resolver.ApplyStatus(new StatusRequest(
                        SourceId(CasterLineage.Herbalist, memberIndex),
                        enemy.EntityId,
                        CombatStatusKind.Vulnerable,
                        _definition.Herbalist.VulnerabilityMagnitude + modifiers.VulnerabilityMagnitudeBonus,
                        _definition.Herbalist.VulnerabilityDuration * modifiers.VulnerabilityDurationMultiplier));
                }
            }
        }

        private void CreateFireField(int memberIndex, RunPoint center, bool promotedAtStart)
        {
            CasterModifierSnapshot modifiers = CurrentModifiers();
            int capacity = _definition.Fire.MaxActiveFields + modifiers.FieldCapacityBonus;
            while (_fields.Count >= capacity)
                RemoveOldestField();
            _fields.Add(new FireFieldState(
                ++_fieldSequence,
                memberIndex,
                center,
                _definition.Fire.Radius * modifiers.FieldRadiusMultiplier,
                _definition.Fire.TickInterval * modifiers.FieldTickIntervalMultiplier,
                _definition.Fire.Duration * modifiers.FieldDurationMultiplier));
            if (promotedAtStart && _ignitionReady == false)
            {
                _ignitionProgress++;
                if (_ignitionProgress >= _definition.Fire.IgnitionTrigger)
                {
                    _ignitionProgress -= _definition.Fire.IgnitionTrigger;
                    _ignitionReady = true;
                }
            }
        }

        private void ResolveChainLightning(
            int memberIndex,
            CombatEntitySnapshot firstTarget,
            bool promotedAtStart)
        {
            CasterModifierSnapshot modifiers = CurrentModifiers();
            int maxTargets = _definition.Lightning.MaxTargets + modifiers.ChainTargetBonus;
            EnsureTargetCapacity(maxTargets);
            int count = 0;
            CombatEntitySnapshot current = firstTarget;
            while (count < maxTargets)
            {
                _targetIds[count] = current.EntityId;
                float multiplier = 1.0f + count * modifiers.ChainDamageRetention;
                _resolver.ApplyDamage(new DamageRequest(
                    SourceId(CasterLineage.Lightning, memberIndex),
                    current.EntityId,
                    _definition.Lightning.Damage,
                    CombatDamageKind.Direct,
                    attackerDamageMultiplier: multiplier,
                    attackId: "chain-lightning"));
                if (count == 0 && _resolver.CreateSnapshot().GetEntity(current.EntityId).IsDead == false)
                {
                    _resolver.ApplyStatus(new StatusRequest(
                        SourceId(CasterLineage.Lightning, memberIndex),
                        current.EntityId,
                        CombatStatusKind.Shock,
                        _definition.Lightning.ShockMagnitude,
                        _definition.Lightning.ShockDuration * modifiers.ShockDurationMultiplier));
                }
                count++;
                if (TrySelectNextChainTarget(current.Position, count, out current) == false)
                    break;
            }
            if (promotedAtStart && _overloadReady == false)
            {
                _overloadProgress++;
                if (_overloadProgress >= _definition.Lightning.OverloadTrigger)
                {
                    _overloadProgress -= _definition.Lightning.OverloadTrigger;
                    _overloadReady = true;
                }
            }
        }

        private bool TrySelectNextChainTarget(
            RunPoint origin,
            int selectedCount,
            out CombatEntitySnapshot selected)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            bool found = false;
            selected = default;
            float bestDistance = _definition.Lightning.ChainDistance * _definition.Lightning.ChainDistance;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead || ContainsTarget(enemy.EntityId, selectedCount))
                    continue;
                float distance = RunPoint.DistanceSquared(enemy.Position, origin);
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

        private RunPoint SelectDensestPoint(float radius)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            CombatEntitySnapshot commander = combat.GetEntity(_definition.CommanderEntityId);
            float rangeSquared = _definition.TargetRange * _definition.TargetRange;
            float radiusSquared = radius * radius;
            int bestCount = -1;
            int bestId = int.MaxValue;
            RunPoint best = default;
            for (int candidateIndex = 0; candidateIndex < combat.EntityCount; candidateIndex++)
            {
                CombatEntitySnapshot candidate = combat.GetEntityAt(candidateIndex);
                if (IsEnemy(candidate) == false || candidate.IsDead ||
                    RunPoint.DistanceSquared(candidate.Position, commander.Position) > rangeSquared)
                {
                    continue;
                }
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

        private int CollectNearestEnemies(
            RunPoint origin,
            float radiusSquared,
            int excludedId,
            bool statusRequired)
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            EnsureTargetCapacity(combat.EntityCount);
            int count = 0;
            for (int index = 0; index < combat.EntityCount; index++)
            {
                CombatEntitySnapshot enemy = combat.GetEntityAt(index);
                if (IsEnemy(enemy) == false || enemy.IsDead || enemy.EntityId == excludedId ||
                    (statusRequired && enemy.GetStatusCount(CombatStatusKind.Shock) == 0))
                {
                    continue;
                }
                float distance = RunPoint.DistanceSquared(enemy.Position, origin);
                if (distance > radiusSquared)
                    continue;
                int insert = count;
                while (insert > 0)
                {
                    CombatEntitySnapshot previous = combat.GetEntity(_targetIds[insert - 1]);
                    float previousDistance = RunPoint.DistanceSquared(previous.Position, origin);
                    if (previousDistance < distance ||
                        (previousDistance == distance && previous.EntityId < enemy.EntityId))
                    {
                        break;
                    }
                    _targetIds[insert] = _targetIds[insert - 1];
                    insert--;
                }
                _targetIds[insert] = enemy.EntityId;
                count++;
            }
            return count;
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

        private bool HasEnemyInsideAnyField()
        {
            CombatEffectsSnapshot combat = _resolver.CreateSnapshot();
            for (int fieldIndex = 0; fieldIndex < _fields.Count; fieldIndex++)
            {
                FireFieldState field = _fields[fieldIndex];
                float radiusSquared = field.Radius * field.Radius;
                for (int entityIndex = 0; entityIndex < combat.EntityCount; entityIndex++)
                {
                    CombatEntitySnapshot enemy = combat.GetEntityAt(entityIndex);
                    if (IsEnemy(enemy) && enemy.IsDead == false &&
                        RunPoint.DistanceSquared(enemy.Position, field.Center) <= radiusSquared)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool HasHerbalistVulnerability(CombatEntitySnapshot entity)
        {
            for (int memberIndex = 1; memberIndex <= 3; memberIndex++)
            {
                if (entity.GetStatusRemaining(
                    CombatStatusKind.Vulnerable,
                    SourceId(CasterLineage.Herbalist, memberIndex)) > 0.0f)
                {
                    return true;
                }
            }
            return false;
        }

        private void TrimOldestFieldsToCapacity()
        {
            int capacity = _definition.Fire.MaxActiveFields + CurrentModifiers().FieldCapacityBonus;
            while (_fields.Count > capacity)
                RemoveOldestField();
        }

        private void RemoveOldestField()
        {
            int oldestIndex = 0;
            for (int index = 1; index < _fields.Count; index++)
            {
                if (_fields[index].Sequence < _fields[oldestIndex].Sequence)
                    oldestIndex = index;
            }
            _fields.RemoveAt(oldestIndex);
        }

        private bool ContainsTarget(int entityId, int count)
        {
            for (int index = 0; index < count; index++)
            {
                if (_targetIds[index] == entityId)
                    return true;
            }
            return false;
        }

        private void EnsureTargetCapacity(int required)
        {
            if (required <= _targetIds.Length)
                return;
            int capacity = _targetIds.Length;
            while (capacity < required)
                capacity *= 2;
            _targetIds = new int[capacity];
        }

        private CasterModifierSnapshot CurrentModifiers()
        {
            return new CasterModifierSnapshot(_passives);
        }

        private bool IsMemberActive(CasterLineage lineage, int memberIndex)
        {
            int progression = _progressions[(int)lineage];
            return memberIndex == 1 ? progression >= 1 : progression >= 2;
        }

        private CasterMemberState GetMember(CasterLineage lineage, int memberIndex)
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
                AddPointDigest(ref digest, _members[index].Slot);
                AddFloatDigest(ref digest, _members[index].CooldownRemaining);
            }
            for (int index = 0; index < _fields.Count; index++)
            {
                FireFieldState field = _fields[index];
                AddDigest(ref digest, (ulong)(uint)field.Sequence);
                AddDigest(ref digest, (ulong)(uint)field.SourceMemberIndex);
                AddPointDigest(ref digest, field.Center);
                AddFloatDigest(ref digest, field.Radius);
                AddFloatDigest(ref digest, field.TickInterval);
                AddFloatDigest(ref digest, field.TickRemaining);
                AddFloatDigest(ref digest, field.RemainingSeconds);
            }
            AddDigest(ref digest, (ulong)(uint)_ignitionProgress);
            AddDigest(ref digest, _ignitionReady ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_ignitionCastCount);
            AddDigest(ref digest, (ulong)(uint)_overloadProgress);
            AddDigest(ref digest, _overloadReady ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_overloadCastCount);
            AddDigest(ref digest, (ulong)(uint)_vulnerabilitySpreadCount);
            return digest;
        }

        private static bool IsEnemy(CombatEntitySnapshot entity)
        {
            return entity.Kind == CombatEntityKind.NormalEnemy ||
                entity.Kind == CombatEntityKind.EliteEnemy ||
                entity.Kind == CombatEntityKind.BossEnemy;
        }

        private static int SourceId(CasterLineage lineage, int memberIndex)
        {
            return 12000 + (int)lineage * 10 + memberIndex;
        }

        private static void ValidateLineage(CasterLineage lineage)
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

    internal sealed class CasterMemberState
    {
        internal CasterLineage Lineage { get; }
        internal int MemberIndex { get; }
        internal RunPoint Slot { get; set; }
        internal float CooldownRemaining { get; set; }

        internal CasterMemberState(CasterLineage lineage, int memberIndex)
        {
            Lineage = lineage;
            MemberIndex = memberIndex;
        }
    }

    internal sealed class FireFieldState
    {
        internal int Sequence { get; }
        internal int SourceMemberIndex { get; }
        internal RunPoint Center { get; }
        internal float Radius { get; }
        internal float TickInterval { get; }
        internal float TickRemaining { get; set; }
        internal float RemainingSeconds { get; set; }

        internal FireFieldState(
            int sequence,
            int sourceMemberIndex,
            RunPoint center,
            float radius,
            float tickInterval,
            float duration)
        {
            Sequence = sequence;
            SourceMemberIndex = sourceMemberIndex;
            Center = center;
            Radius = radius;
            TickInterval = tickInterval;
            TickRemaining = tickInterval;
            RemainingSeconds = duration;
        }
    }
}
