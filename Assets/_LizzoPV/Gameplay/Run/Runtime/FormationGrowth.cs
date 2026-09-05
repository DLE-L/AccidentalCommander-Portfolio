using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class LegionGrowthDefinition
    {
        public string BaseUnitId { get; }
        public string PromotedUnitId { get; }
        public float Weight { get; }

        public LegionGrowthDefinition(string baseUnitId, string promotedUnitId, float weight)
        {
            if (string.IsNullOrWhiteSpace(baseUnitId))
                throw new ArgumentException("Base unit id is required.", nameof(baseUnitId));
            if (string.IsNullOrWhiteSpace(promotedUnitId))
                throw new ArgumentException("Promoted unit id is required.", nameof(promotedUnitId));
            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(weight));

            BaseUnitId = baseUnitId;
            PromotedUnitId = promotedUnitId;
            Weight = weight;
        }
    }

    public sealed class FormationGrowthDefinition
    {
        public static FormationGrowthDefinition Disabled { get; } = new FormationGrowthDefinition();

        private readonly LegionGrowthDefinition[] _legions;

        internal bool IsEnabled { get; }
        public int ExperiencePerLevel { get; }
        public float GroupRadius { get; }
        public float FrontDepth { get; }
        public float RearDepth { get; }
        public float HalfWidth { get; }
        public IReadOnlyList<LegionGrowthDefinition> Legions => _legions;

        private FormationGrowthDefinition()
        {
            _legions = Array.Empty<LegionGrowthDefinition>();
        }

        public FormationGrowthDefinition(
            int experiencePerLevel,
            float groupRadius,
            float frontDepth,
            float rearDepth,
            float halfWidth,
            LegionGrowthDefinition[] legions)
        {
            if (experiencePerLevel <= 0)
                throw new ArgumentOutOfRangeException(nameof(experiencePerLevel));
            if (groupRadius <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(groupRadius));
            if (frontDepth <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(frontDepth));
            if (rearDepth <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(rearDepth));
            if (halfWidth <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(halfWidth));
            if (legions == null || legions.Length == 0 || legions.Length > 7)
                throw new ArgumentOutOfRangeException(nameof(legions));

            _legions = new LegionGrowthDefinition[legions.Length];
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < legions.Length; index++)
            {
                LegionGrowthDefinition legion = legions[index]
                    ?? throw new ArgumentException("Legion definition cannot be null.", nameof(legions));
                if (ids.Add(legion.BaseUnitId) == false)
                    throw new ArgumentException($"Duplicate legion id: {legion.BaseUnitId}", nameof(legions));
                _legions[index] = legion;
            }

            IsEnabled = true;
            ExperiencePerLevel = experiencePerLevel;
            GroupRadius = groupRadius;
            FrontDepth = frontDepth;
            RearDepth = rearDepth;
            HalfWidth = halfWidth;
        }
    }

    public readonly struct GrowthOfferSnapshot
    {
        private readonly string[] _cardIds;
        private readonly float[] _weights;

        public int OfferIndex { get; }
        public int Count => _cardIds == null ? 0 : _cardIds.Length;

        internal GrowthOfferSnapshot(int offerIndex, string[] cardIds, float[] weights)
        {
            OfferIndex = offerIndex;
            _cardIds = cardIds;
            _weights = weights;
        }

        public string GetCardId(int index)
        {
            if (_cardIds == null || index < 0 || index >= _cardIds.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _cardIds[index];
        }

        public float GetWeight(int index)
        {
            if (_weights == null || index < 0 || index >= _weights.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _weights[index];
        }
    }

    public readonly struct FormationSlotSnapshot
    {
        public string SlotId { get; }
        public bool IsGroupActive { get; }
        public bool HasOccupant => string.IsNullOrEmpty(OccupantUnitId) == false;
        public string BaseUnitId { get; }
        public string OccupantUnitId { get; }
        public bool IsPromoted { get; }
        public RunPoint GroupCenter { get; }
        public RunPoint LocalOffset { get; }

        internal FormationSlotSnapshot(
            string slotId,
            bool isGroupActive,
            string baseUnitId,
            string occupantUnitId,
            bool isPromoted,
            RunPoint groupCenter,
            RunPoint localOffset)
        {
            SlotId = slotId ?? string.Empty;
            IsGroupActive = isGroupActive;
            BaseUnitId = baseUnitId ?? string.Empty;
            OccupantUnitId = occupantUnitId ?? string.Empty;
            IsPromoted = isPromoted;
            GroupCenter = groupCenter;
            LocalOffset = localOffset;
        }
    }

    public readonly struct FormationGrowthSnapshot
    {
        private readonly string[] _legionIds;
        private readonly int[] _progressions;
        private readonly FormationSlotSnapshot[] _slots;

        public bool IsEnabled { get; }
        public int ActiveLegionCount { get; }
        public int LayoutRevision { get; }
        public int PendingLevelCount { get; }
        public int AccumulatedExperience { get; }
        public int EarnedLevelCount { get; }
        public int CommittedSelectionCount { get; }
        public bool IsGrowthComplete { get; }
        public GrowthOfferSnapshot ActiveOffer { get; }
        internal ulong StateDigest { get; }

        internal FormationGrowthSnapshot(
            bool isEnabled,
            int activeLegionCount,
            int layoutRevision,
            int pendingLevelCount,
            int accumulatedExperience,
            int earnedLevelCount,
            int committedSelectionCount,
            bool isGrowthComplete,
            GrowthOfferSnapshot activeOffer,
            string[] legionIds,
            int[] progressions,
            FormationSlotSnapshot[] slots,
            ulong stateDigest)
        {
            IsEnabled = isEnabled;
            ActiveLegionCount = activeLegionCount;
            LayoutRevision = layoutRevision;
            PendingLevelCount = pendingLevelCount;
            AccumulatedExperience = accumulatedExperience;
            EarnedLevelCount = earnedLevelCount;
            CommittedSelectionCount = committedSelectionCount;
            IsGrowthComplete = isGrowthComplete;
            ActiveOffer = activeOffer;
            _legionIds = legionIds;
            _progressions = progressions;
            _slots = slots;
            StateDigest = stateDigest;
        }

        public int GetProgression(string baseUnitId)
        {
            if (_legionIds == null || string.IsNullOrEmpty(baseUnitId))
                return 0;
            for (int index = 0; index < _legionIds.Length; index++)
            {
                if (string.Equals(_legionIds[index], baseUnitId, StringComparison.Ordinal))
                    return _progressions[index];
            }
            return 0;
        }

        public FormationSlotSnapshot GetSlot(string slotId)
        {
            if (_slots != null)
            {
                for (int index = 0; index < _slots.Length; index++)
                {
                    if (string.Equals(_slots[index].SlotId, slotId, StringComparison.Ordinal))
                        return _slots[index];
                }
            }
            throw new ArgumentOutOfRangeException(nameof(slotId));
        }
    }

    public static class FormationLayout
    {
        public const int GroupCapacity = 7;
        public const int MemberCapacity = 3;
        public const int SlotCapacity = GroupCapacity * MemberCapacity;

        public static string CreateSlotId(int groupIndex, int memberIndex)
        {
            if (groupIndex < 0 || groupIndex >= GroupCapacity)
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            if (memberIndex < 1 || memberIndex > MemberCapacity)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return string.Concat((char)('A' + groupIndex), memberIndex.ToString());
        }

        public static RunPoint ResolveGroupCenter(int activeGroupCount, int groupIndex, float radius)
        {
            if (activeGroupCount < 1 || activeGroupCount > GroupCapacity)
                throw new ArgumentOutOfRangeException(nameof(activeGroupCount));
            if (groupIndex < 0 || groupIndex >= activeGroupCount)
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            if (radius <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(radius));

            double step = Math.PI * 2.0 / activeGroupCount;
            double angle;
            if ((activeGroupCount & 1) != 0)
            {
                if (groupIndex == 0)
                {
                    angle = -Math.PI * 0.5;
                }
                else
                {
                    int pair = (groupIndex + 1) / 2;
                    int sign = (groupIndex & 1) != 0 ? 1 : -1;
                    angle = -Math.PI * 0.5 + sign * pair * step;
                }
            }
            else
            {
                int pair = groupIndex / 2;
                int sign = (groupIndex & 1) == 0 ? -1 : 1;
                angle = -Math.PI * 0.5 + sign * (pair + 0.5) * step;
            }

            return new RunPoint(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius));
        }

        internal static RunPoint ResolveSlotOffset(
            RunPoint groupCenter,
            int memberIndex,
            float frontDepth,
            float rearDepth,
            float halfWidth)
        {
            float length = (float)Math.Sqrt(groupCenter.X * groupCenter.X + groupCenter.Y * groupCenter.Y);
            float outwardX = length <= 0.0001f ? 0.0f : groupCenter.X / length;
            float outwardY = length <= 0.0001f ? -1.0f : groupCenter.Y / length;
            float rightX = -outwardY;
            float rightY = outwardX;
            if (memberIndex == 1)
            {
                return new RunPoint(
                    groupCenter.X + outwardX * frontDepth - rightX * halfWidth,
                    groupCenter.Y + outwardY * frontDepth - rightY * halfWidth);
            }
            if (memberIndex == 2)
            {
                return new RunPoint(
                    groupCenter.X + outwardX * frontDepth + rightX * halfWidth,
                    groupCenter.Y + outwardY * frontDepth + rightY * halfWidth);
            }
            return new RunPoint(
                groupCenter.X - outwardX * rearDepth,
                groupCenter.Y - outwardY * rearDepth);
        }
    }

    internal readonly struct GrowthApplication
    {
        internal string BaseUnitId { get; }
        internal int Progression { get; }
        internal string ActiveMemberSlotId { get; }
        internal CommonPassiveId CommonPassiveId { get; }
        internal bool IsLegion => string.IsNullOrEmpty(BaseUnitId) == false;

        private GrowthApplication(
            string baseUnitId,
            int progression,
            string activeMemberSlotId,
            CommonPassiveId commonPassiveId)
        {
            BaseUnitId = baseUnitId;
            Progression = progression;
            ActiveMemberSlotId = activeMemberSlotId;
            CommonPassiveId = commonPassiveId;
        }

        internal static GrowthApplication Legion(
            string baseUnitId,
            int progression,
            string activeMemberSlotId)
        {
            return new GrowthApplication(
                baseUnitId,
                progression,
                activeMemberSlotId,
                default);
        }

        internal static GrowthApplication CommonPassive(CommonPassiveId commonPassiveId)
        {
            return new GrowthApplication(string.Empty, 0, string.Empty, commonPassiveId);
        }
    }

    internal sealed class FormationGrowthRuntime
    {
        private readonly FormationGrowthDefinition _definition;
        private readonly CommonPassiveDefinition _commonPassives;
        private readonly LegionState[] _states;
        private readonly int[] _passiveLevels;
        private readonly string[] _snapshotLegionIds;
        private int[] _snapshotProgressions;
        private readonly List<int> _eligible = new List<int>(24);

        private FormationSlotSnapshot[] _snapshotSlots = Array.Empty<FormationSlotSnapshot>();
        private GrowthOfferSnapshot _activeOffer;
        private int _activeLegionCount;
        private int _layoutRevision;
        private int _pendingLevelCount;
        private int _accumulatedExperience;
        private int _earnedLevelCount;
        private int _nextExperienceThreshold;
        private int _committedSelectionCount;
        private int _nextOfferIndex = 1;
        private int _seed;
        private int _experienceGainPermille = 1000;
        private int _experienceRemainderPermille;
        private bool _activeOfferIsInitial;

        internal bool IsEnabled => _definition.IsEnabled;
        internal bool HasActiveOffer => _activeOffer.Count > 0;
        internal bool ActiveOfferIsInitial => _activeOfferIsInitial;

        internal FormationGrowthRuntime(
            FormationGrowthDefinition definition,
            CommonPassiveDefinition commonPassives)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _commonPassives = commonPassives ?? throw new ArgumentNullException(nameof(commonPassives));
            _states = new LegionState[definition.Legions.Count];
            _passiveLevels = new int[commonPassives.Cards.Count];
            _snapshotLegionIds = new string[definition.Legions.Count];
            _snapshotProgressions = new int[definition.Legions.Count];
            for (int index = 0; index < definition.Legions.Count; index++)
            {
                LegionGrowthDefinition legion = definition.Legions[index];
                for (int passiveIndex = 0; passiveIndex < commonPassives.Cards.Count; passiveIndex++)
                {
                    if (string.Equals(
                            legion.BaseUnitId,
                            commonPassives.Cards[passiveIndex].CardId,
                            StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            "Legion and common passive card ids must be unique across the growth catalog.");
                    }
                }
                _states[index] = new LegionState(legion);
                _snapshotLegionIds[index] = legion.BaseUnitId;
            }
            _nextExperienceThreshold = definition.ExperiencePerLevel;
            RebuildSlots();
        }

        internal void BeginInitialOffer(int seed)
        {
            if (IsEnabled == false || HasActiveOffer)
                return;
            _seed = seed;
            GenerateOffer(true);
        }

        internal void AddExperience(int amount)
        {
            if (IsEnabled == false || amount <= 0)
                return;

            long scaledExperience = (long)amount * _experienceGainPermille + _experienceRemainderPermille;
            int awardedExperience = (int)(scaledExperience / 1000L);
            _experienceRemainderPermille = (int)(scaledExperience % 1000L);
            if (awardedExperience <= 0)
                return;

            _accumulatedExperience += awardedExperience;
            while (_accumulatedExperience >= _nextExperienceThreshold)
            {
                _earnedLevelCount++;
                _pendingLevelCount++;
                _nextExperienceThreshold += _definition.ExperiencePerLevel;
            }

            if (IsGrowthComplete())
            {
                _pendingLevelCount = 0;
                _activeOffer = default;
                _activeOfferIsInitial = false;
                return;
            }

            if (_pendingLevelCount > 0 && HasActiveOffer == false)
                GenerateOffer(false);
        }

        internal void SetExperienceGainMultiplier(float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            _experienceGainPermille = Math.Max(
                1,
                (int)Math.Round(multiplier * 1000.0f, MidpointRounding.AwayFromZero));
        }

        internal void SyncCommonPassiveLevel(CommonPassiveId id, int level)
        {
            if (level < 0 || level > 3)
                throw new ArgumentOutOfRangeException(nameof(level));
            for (int index = 0; index < _commonPassives.Cards.Count; index++)
            {
                if (_commonPassives.Cards[index].Id != id)
                    continue;
                _passiveLevels[index] = level;
                return;
            }
        }

        internal bool TryChoose(int slotIndex, out GrowthApplication application)
        {
            application = default;
            if (HasActiveOffer == false || slotIndex < 0 || slotIndex >= _activeOffer.Count)
                return false;

            string selectedId = _activeOffer.GetCardId(slotIndex);
            int stateIndex = FindState(selectedId);
            if (stateIndex < 0)
            {
                int passiveIndex = FindPassive(selectedId);
                if (_activeOfferIsInitial || passiveIndex < 0 || _passiveLevels[passiveIndex] >= 3)
                    return false;
                _passiveLevels[passiveIndex]++;
                _committedSelectionCount++;
                _activeOffer = default;
                _activeOfferIsInitial = false;
                if (_pendingLevelCount > 0)
                    _pendingLevelCount--;
                application = GrowthApplication.CommonPassive(_commonPassives.Cards[passiveIndex].Id);
                if (_pendingLevelCount > 0 && IsGrowthComplete() == false)
                    GenerateOffer(false);
                else if (IsGrowthComplete())
                    _pendingLevelCount = 0;
                return true;
            }
            if (_states[stateIndex].Progression >= 3)
                return false;

            bool wasInitial = _activeOfferIsInitial;
            LegionState state = _states[stateIndex];
            if (state.Progression == 0)
            {
                state.GroupIndex = _activeLegionCount;
                _activeLegionCount++;
                _layoutRevision++;
            }
            state.Progression++;
            _states[stateIndex] = state;
            int[] progressions = new int[_snapshotProgressions.Length];
            Array.Copy(_snapshotProgressions, progressions, progressions.Length);
            progressions[stateIndex] = state.Progression;
            _snapshotProgressions = progressions;
            _committedSelectionCount++;
            _activeOffer = default;
            _activeOfferIsInitial = false;
            if (wasInitial == false && _pendingLevelCount > 0)
                _pendingLevelCount--;

            RebuildSlots();
            string activeSlotId = FormationLayout.CreateSlotId(
                state.GroupIndex,
                state.Progression == 1 ? 3 : 1);
            application = GrowthApplication.Legion(
                state.Definition.BaseUnitId,
                state.Progression,
                activeSlotId);

            if (_pendingLevelCount > 0 && IsGrowthComplete() == false)
                GenerateOffer(false);
            else if (IsGrowthComplete())
                _pendingLevelCount = 0;
            return true;
        }

        internal bool TryGetPrimarySlot(string baseUnitId, out RunPoint slotPosition)
        {
            return TryGetMemberSlot(baseUnitId, 1, out slotPosition);
        }

        internal bool TryGetMemberSlot(
            string baseUnitId,
            int memberIndex,
            out RunPoint slotPosition)
        {
            int stateIndex = FindState(baseUnitId);
            if (memberIndex < 1 || memberIndex > 2 ||
                stateIndex < 0 ||
                _states[stateIndex].Progression < memberIndex)
            {
                slotPosition = default;
                return false;
            }

            LegionState state = _states[stateIndex];
            int formationMemberIndex = state.Progression == 1 ? 3 : memberIndex;
            int slotIndex = state.GroupIndex * FormationLayout.MemberCapacity + formationMemberIndex - 1;
            slotPosition = _snapshotSlots[slotIndex].LocalOffset;
            return true;
        }

        internal FormationGrowthSnapshot CreateSnapshot()
        {
            ulong digest = 14695981039346656037UL;
            AddDigest(ref digest, IsEnabled ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_activeLegionCount);
            AddDigest(ref digest, (ulong)(uint)_layoutRevision);
            AddDigest(ref digest, (ulong)(uint)_pendingLevelCount);
            AddDigest(ref digest, (ulong)(uint)_accumulatedExperience);
            AddDigest(ref digest, (ulong)(uint)_earnedLevelCount);
            AddDigest(ref digest, (ulong)(uint)_committedSelectionCount);
            AddDigest(ref digest, (ulong)(uint)_experienceGainPermille);
            AddDigest(ref digest, (ulong)(uint)_experienceRemainderPermille);
            AddDigest(ref digest, (ulong)(uint)_activeOffer.OfferIndex);
            for (int index = 0; index < _states.Length; index++)
            {
                AddStringDigest(ref digest, _states[index].Definition.BaseUnitId);
                AddDigest(ref digest, (ulong)(uint)_states[index].Progression);
                AddDigest(ref digest, unchecked((ulong)(uint)_states[index].GroupIndex));
            }
            for (int index = 0; index < _passiveLevels.Length; index++)
                AddDigest(ref digest, (ulong)(uint)_passiveLevels[index]);
            for (int index = 0; index < _activeOffer.Count; index++)
            {
                AddStringDigest(ref digest, _activeOffer.GetCardId(index));
                AddDigest(ref digest, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(_activeOffer.GetWeight(index))));
            }

            return new FormationGrowthSnapshot(
                IsEnabled,
                _activeLegionCount,
                _layoutRevision,
                _pendingLevelCount,
                _accumulatedExperience,
                _earnedLevelCount,
                _committedSelectionCount,
                IsGrowthComplete(),
                _activeOffer,
                _snapshotLegionIds,
                _snapshotProgressions,
                _snapshotSlots,
                digest);
        }

        private void GenerateOffer(bool initial)
        {
            _eligible.Clear();
            for (int index = 0; index < _states.Length; index++)
            {
                if (_states[index].Progression < 3)
                    _eligible.Add(index);
            }
            if (initial == false)
            {
                for (int index = 0; index < _passiveLevels.Length; index++)
                {
                    if (_passiveLevels[index] < 3)
                        _eligible.Add(_states.Length + index);
                }
            }
            _eligible.Sort(CompareStateIds);
            if (_eligible.Count == 0)
            {
                _activeOffer = default;
                _activeOfferIsInitial = false;
                if (initial == false)
                    _pendingLevelCount = 0;
                return;
            }

            int count = Math.Min(3, _eligible.Count);
            string[] ids = new string[count];
            float[] weights = new float[count];
            WeightedRandom random = new WeightedRandom(DeriveOfferSeed(_seed, _nextOfferIndex));
            for (int slot = 0; slot < count; slot++)
            {
                int eligibleIndex = DrawEligibleIndex(ref random);
                int candidateIndex = _eligible[eligibleIndex];
                ids[slot] = GetCandidateId(candidateIndex);
                weights[slot] = GetCandidateWeight(candidateIndex);
                _eligible.RemoveAt(eligibleIndex);
            }

            _activeOffer = new GrowthOfferSnapshot(_nextOfferIndex, ids, weights);
            _nextOfferIndex++;
            _activeOfferIsInitial = initial;
        }

        private int DrawEligibleIndex(ref WeightedRandom random)
        {
            double total = 0.0;
            for (int index = 0; index < _eligible.Count; index++)
                total += GetCandidateWeight(_eligible[index]);
            double roll = random.NextUnit() * total;
            for (int index = 0; index < _eligible.Count; index++)
            {
                roll -= GetCandidateWeight(_eligible[index]);
                if (roll <= 0.0)
                    return index;
            }
            return _eligible.Count - 1;
        }

        private int CompareStateIds(int left, int right)
        {
            return string.CompareOrdinal(GetCandidateId(left), GetCandidateId(right));
        }

        private string GetCandidateId(int candidateIndex)
        {
            if (candidateIndex < _states.Length)
                return _states[candidateIndex].Definition.BaseUnitId;
            return _commonPassives.Cards[candidateIndex - _states.Length].CardId;
        }

        private float GetCandidateWeight(int candidateIndex)
        {
            if (candidateIndex < _states.Length)
                return _states[candidateIndex].Definition.Weight;
            return _commonPassives.Cards[candidateIndex - _states.Length].Weight;
        }

        private void RebuildSlots()
        {
            FormationSlotSnapshot[] slots = new FormationSlotSnapshot[FormationLayout.SlotCapacity];
            for (int group = 0; group < FormationLayout.GroupCapacity; group++)
            {
                int stateIndex = FindStateByGroup(group);
                bool active = stateIndex >= 0;
                LegionState state = active ? _states[stateIndex] : default;
                RunPoint center = active
                    ? FormationLayout.ResolveGroupCenter(_activeLegionCount, group, _definition.GroupRadius)
                    : default;
                for (int member = 1; member <= FormationLayout.MemberCapacity; member++)
                {
                    string occupant = string.Empty;
                    bool promoted = false;
                    if (active)
                    {
                        if (state.Progression == 1 && member == 3)
                            occupant = state.Definition.BaseUnitId;
                        else if (state.Progression >= 2 && member <= 2)
                            occupant = state.Definition.BaseUnitId;
                        else if (state.Progression >= 3 && member == 3)
                        {
                            occupant = state.Definition.PromotedUnitId;
                            promoted = true;
                        }
                    }

                    int slotIndex = group * FormationLayout.MemberCapacity + member - 1;
                    slots[slotIndex] = new FormationSlotSnapshot(
                        FormationLayout.CreateSlotId(group, member),
                        active,
                        active ? state.Definition.BaseUnitId : string.Empty,
                        occupant,
                        promoted,
                        center,
                        active
                            ? FormationLayout.ResolveSlotOffset(
                                center,
                                member,
                                _definition.FrontDepth,
                                _definition.RearDepth,
                                _definition.HalfWidth)
                            : default);
                }
            }
            _snapshotSlots = slots;
        }

        private int FindState(string baseUnitId)
        {
            for (int index = 0; index < _states.Length; index++)
            {
                if (string.Equals(_states[index].Definition.BaseUnitId, baseUnitId, StringComparison.Ordinal))
                    return index;
            }
            return -1;
        }

        private int FindPassive(string cardId)
        {
            for (int index = 0; index < _commonPassives.Cards.Count; index++)
            {
                if (string.Equals(_commonPassives.Cards[index].CardId, cardId, StringComparison.Ordinal))
                    return index;
            }
            return -1;
        }

        private int FindStateByGroup(int groupIndex)
        {
            for (int index = 0; index < _states.Length; index++)
            {
                if (_states[index].Progression > 0 && _states[index].GroupIndex == groupIndex)
                    return index;
            }
            return -1;
        }

        private bool IsGrowthComplete()
        {
            if (IsEnabled == false)
                return false;
            for (int index = 0; index < _states.Length; index++)
            {
                if (_states[index].Progression < 3)
                    return false;
            }
            for (int index = 0; index < _passiveLevels.Length; index++)
            {
                if (_passiveLevels[index] < 3)
                    return false;
            }
            return true;
        }

        private static ulong DeriveOfferSeed(int seed, int offerIndex)
        {
            unchecked
            {
                return ((ulong)(uint)seed << 32)
                    ^ (uint)offerIndex
                    ^ 0x9E3779B97F4A7C15UL;
            }
        }

        private static void AddStringDigest(ref ulong value, string part)
        {
            if (part == null)
            {
                AddDigest(ref value, 0UL);
                return;
            }
            for (int index = 0; index < part.Length; index++)
                AddDigest(ref value, part[index]);
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

        private struct WeightedRandom
        {
            private ulong _state;

            internal WeightedRandom(ulong seed)
            {
                _state = seed == 0UL ? 0x9E3779B97F4A7C15UL : seed;
            }

            internal double NextUnit()
            {
                _state ^= _state >> 12;
                _state ^= _state << 25;
                _state ^= _state >> 27;
                ulong value = _state * 2685821657736338717UL;
                return (value >> 11) * (1.0 / 9007199254740992.0);
            }
        }

        private struct LegionState
        {
            internal LegionGrowthDefinition Definition { get; }
            internal int Progression { get; set; }
            internal int GroupIndex { get; set; }

            internal LegionState(LegionGrowthDefinition definition)
            {
                Definition = definition;
                Progression = 0;
                GroupIndex = -1;
            }
        }
    }
}
