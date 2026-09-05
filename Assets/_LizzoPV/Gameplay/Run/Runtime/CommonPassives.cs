using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public enum CommonPassiveId
    {
        StandardBearer,
        WarDrum,
        ScoutingBanner,
        WideFormation,
        MarchingBoots,
        ReinforcedArmor,
        SupplyPouch,
        EliteDoctrine,
        HeavyFormation,
        LingeringTactics,
        SustainedSummons,
        VeteranCommand,
        Count,
    }

    public enum CommonPassiveEffect
    {
        LegionDamageMultiplier,
        ActionIntervalMultiplier,
        AcquisitionRangeMultiplier,
        AreaRadiusMultiplier,
        CommanderMoveSpeedMultiplier,
        CommanderMaxHealthMultiplier,
        ExperienceGainMultiplier,
        EliteRosterDamageMultiplier,
        ForcedMovementDistanceMultiplier,
        StatusDurationMultiplier,
        OwnedEffectDurationMultiplier,
        PromotedActionIntervalMultiplier,
    }

    public sealed class CommonPassiveCardDefinition
    {
        public CommonPassiveId Id { get; }
        public string CardId { get; }
        public CommonPassiveEffect Effect { get; }
        public float Weight { get; }
        public float Level1Value { get; }
        public float Level2Value { get; }
        public float Level3Value { get; }

        public CommonPassiveCardDefinition(
            CommonPassiveId id,
            string cardId,
            CommonPassiveEffect effect,
            float weight,
            float level1Value,
            float level2Value,
            float level3Value)
        {
            if ((int)id < 0 || id >= CommonPassiveId.Count)
                throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(cardId))
                throw new ArgumentException("Card id is required.", nameof(cardId));
            ValidatePositive(weight, nameof(weight));
            ValidatePositive(level1Value, nameof(level1Value));
            ValidatePositive(level2Value, nameof(level2Value));
            ValidatePositive(level3Value, nameof(level3Value));

            Id = id;
            CardId = cardId;
            Effect = effect;
            Weight = weight;
            Level1Value = level1Value;
            Level2Value = level2Value;
            Level3Value = level3Value;
        }

        public float GetValue(int level)
        {
            return level switch
            {
                1 => Level1Value,
                2 => Level2Value,
                3 => Level3Value,
                _ => throw new ArgumentOutOfRangeException(nameof(level)),
            };
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class CommonPassiveDefinition
    {
        public static CommonPassiveDefinition Disabled { get; } = new CommonPassiveDefinition();

        private readonly CommonPassiveCardDefinition[] _cards;

        internal bool IsEnabled { get; }
        public IReadOnlyList<CommonPassiveCardDefinition> Cards => _cards;

        private CommonPassiveDefinition()
        {
            _cards = Array.Empty<CommonPassiveCardDefinition>();
        }

        public CommonPassiveDefinition(CommonPassiveCardDefinition[] cards)
        {
            if (cards == null || cards.Length != (int)CommonPassiveId.Count)
                throw new ArgumentException("Exactly twelve common passive cards are required.", nameof(cards));

            _cards = new CommonPassiveCardDefinition[cards.Length];
            bool[] ids = new bool[(int)CommonPassiveId.Count];
            bool[] effects = new bool[Enum.GetValues(typeof(CommonPassiveEffect)).Length];
            HashSet<string> cardIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < cards.Length; index++)
            {
                CommonPassiveCardDefinition card = cards[index]
                    ?? throw new ArgumentException("Common passive card cannot be null.", nameof(cards));
                int id = (int)card.Id;
                int effect = (int)card.Effect;
                if (ids[id])
                    throw new ArgumentException($"Duplicate common passive id: {card.Id}", nameof(cards));
                if (effects[effect])
                    throw new ArgumentException($"Duplicate common passive effect: {card.Effect}", nameof(cards));
                if (cardIds.Add(card.CardId) == false)
                    throw new ArgumentException($"Duplicate common passive card id: {card.CardId}", nameof(cards));
                ids[id] = true;
                effects[effect] = true;
                _cards[index] = card;
            }

            IsEnabled = true;
        }

        internal CommonPassiveCardDefinition Find(CommonPassiveId id)
        {
            for (int index = 0; index < _cards.Length; index++)
            {
                if (_cards[index].Id == id)
                    return _cards[index];
            }
            return null;
        }
    }

    public readonly struct CommonModifierSnapshot
    {
        public float LegionDamageMultiplier { get; }
        public float ActionIntervalMultiplier { get; }
        public float AcquisitionRangeMultiplier { get; }
        public float AreaRadiusMultiplier { get; }
        public float CommanderMoveSpeedMultiplier { get; }
        public float CommanderMaxHealthMultiplier { get; }
        public float ExperienceGainMultiplier { get; }
        public float EliteRosterDamageMultiplier { get; }
        public float ForcedMovementDistanceMultiplier { get; }
        public float StatusDurationMultiplier { get; }
        public float OwnedEffectDurationMultiplier { get; }
        public float PromotedActionIntervalMultiplier { get; }

        internal CommonModifierSnapshot(float[] values)
        {
            LegionDamageMultiplier = values[(int)CommonPassiveEffect.LegionDamageMultiplier];
            ActionIntervalMultiplier = values[(int)CommonPassiveEffect.ActionIntervalMultiplier];
            AcquisitionRangeMultiplier = values[(int)CommonPassiveEffect.AcquisitionRangeMultiplier];
            AreaRadiusMultiplier = values[(int)CommonPassiveEffect.AreaRadiusMultiplier];
            CommanderMoveSpeedMultiplier = values[(int)CommonPassiveEffect.CommanderMoveSpeedMultiplier];
            CommanderMaxHealthMultiplier = values[(int)CommonPassiveEffect.CommanderMaxHealthMultiplier];
            ExperienceGainMultiplier = values[(int)CommonPassiveEffect.ExperienceGainMultiplier];
            EliteRosterDamageMultiplier = values[(int)CommonPassiveEffect.EliteRosterDamageMultiplier];
            ForcedMovementDistanceMultiplier = values[(int)CommonPassiveEffect.ForcedMovementDistanceMultiplier];
            StatusDurationMultiplier = values[(int)CommonPassiveEffect.StatusDurationMultiplier];
            OwnedEffectDurationMultiplier = values[(int)CommonPassiveEffect.OwnedEffectDurationMultiplier];
            PromotedActionIntervalMultiplier = values[(int)CommonPassiveEffect.PromotedActionIntervalMultiplier];
        }

        public float ResolveLegionDamageMultiplier(int activeLegionCount)
        {
            if (activeLegionCount < 0)
                throw new ArgumentOutOfRangeException(nameof(activeLegionCount));
            return LegionDamageMultiplier * (activeLegionCount <= 3 ? EliteRosterDamageMultiplier : 1.0f);
        }

        internal static CommonModifierSnapshot Identity
        {
            get
            {
                float[] values = new float[Enum.GetValues(typeof(CommonPassiveEffect)).Length];
                for (int index = 0; index < values.Length; index++)
                    values[index] = 1.0f;
                return new CommonModifierSnapshot(values);
            }
        }
    }

    public readonly struct CommonPassiveSnapshot
    {
        private readonly int[] _levels;

        public bool IsEnabled { get; }
        public int AppliedLevelCount { get; }
        public int CacheRevision { get; }
        public CommonModifierSnapshot Modifiers { get; }
        internal ulong StateDigest { get; }

        internal CommonPassiveSnapshot(
            bool isEnabled,
            int appliedLevelCount,
            int cacheRevision,
            CommonModifierSnapshot modifiers,
            int[] levels,
            ulong stateDigest)
        {
            IsEnabled = isEnabled;
            AppliedLevelCount = appliedLevelCount;
            CacheRevision = cacheRevision;
            Modifiers = modifiers;
            _levels = levels;
            StateDigest = stateDigest;
        }

        public int GetLevel(CommonPassiveId id)
        {
            if ((int)id < 0 || id >= CommonPassiveId.Count || _levels == null)
                return 0;
            return _levels[(int)id];
        }
    }

    public sealed class CommonPassiveRuntime
    {
        private readonly CommonPassiveDefinition _definition;
        private readonly int[] _levels;
        private int[] _snapshotLevels;
        private CommonModifierSnapshot _modifiers;
        private int _appliedLevelCount;
        private int _cacheRevision;
        private ulong _stateDigest;

        internal CommonModifierSnapshot CurrentModifiers => _modifiers;

        public CommonPassiveRuntime(CommonPassiveDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _levels = new int[(int)CommonPassiveId.Count];
            _snapshotLevels = new int[_levels.Length];
            _modifiers = CommonModifierSnapshot.Identity;
            RecalculateDigest();
        }

        public bool Apply(CommonPassiveId id)
        {
            if (_definition.IsEnabled == false || (int)id < 0 || id >= CommonPassiveId.Count)
                return false;
            int index = (int)id;
            if (_levels[index] >= 3)
                return false;

            _levels[index]++;
            _appliedLevelCount++;
            _cacheRevision++;
            RebuildCache();
            return true;
        }

        public CommonPassiveSnapshot CreateSnapshot()
        {
            return new CommonPassiveSnapshot(
                _definition.IsEnabled,
                _appliedLevelCount,
                _cacheRevision,
                _modifiers,
                _snapshotLevels,
                _stateDigest);
        }

        private void RebuildCache()
        {
            float[] values = new float[Enum.GetValues(typeof(CommonPassiveEffect)).Length];
            for (int index = 0; index < values.Length; index++)
                values[index] = 1.0f;

            for (int index = 0; index < _definition.Cards.Count; index++)
            {
                CommonPassiveCardDefinition card = _definition.Cards[index];
                int level = _levels[(int)card.Id];
                if (level > 0)
                    values[(int)card.Effect] = card.GetValue(level);
            }

            int[] levels = new int[_levels.Length];
            Array.Copy(_levels, levels, levels.Length);
            _snapshotLevels = levels;
            _modifiers = new CommonModifierSnapshot(values);
            RecalculateDigest();
        }

        private void RecalculateDigest()
        {
            ulong digest = 14695981039346656037UL;
            AddDigest(ref digest, _definition.IsEnabled ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_cacheRevision);
            for (int index = 0; index < _levels.Length; index++)
                AddDigest(ref digest, (ulong)(uint)_levels[index]);
            _stateDigest = digest;
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
    }
}
