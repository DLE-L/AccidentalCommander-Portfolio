using System.Globalization;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static partial class CardEffectRuntime
    {
        static RuntimeObjectRegistry _registry;
        static PartyService _party;

        public static void Configure(RuntimeObjectRegistry registry, PartyService party)
        {
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
            _party = party ?? throw new System.ArgumentNullException(nameof(party));
        }

        private const int SMALL_HEAL_AMOUNT = 30;
        private const int COMMANDER_ATTACK_BONUS = 4;
        private const float COMMANDER_MOVE_SPEED_BONUS = 0.25f;
        private const float ALLY_ATTACK_BONUS_RATIO = 0.08f;
        private const float GUARD_SHOCKWAVE_BONUS_RATIO = 0.10f;
        public const int PassiveSlotCap = 5;

        private static int _commanderAttackLevel;
        private static int _commanderMoveLevel;
        private static int _legionBannerLevel;
        private static int _guardShockwaveCrestLevel;
        private static readonly PassiveProgression _passiveProgression = new PassiveProgression();

        public const int MaxDistinctPassiveTypes = 5;
        public const int MaxPassiveAcquisitions = 3;

        public static int GetPassiveAcquisitionCount(CardKind kind)
        {
            return IsPassiveCard(kind) ? _passiveProgression.GetCount(ResolvePassiveId(kind)) : 0;
        }

        public static bool CanAcquirePassive(CardKind kind)
        {
            return IsPassiveCard(kind) && _passiveProgression.IsEligible(ResolvePassiveId(kind));
        }

        public static bool IsPassiveCard(CardKind kind)
        {
            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry))
            {
                return entry.HasCompanionKind == false
                    && entry.EffectKind != CardEffectKind.None
                    && entry.EffectKind != CardEffectKind.SmallHeal;
            }

            return kind == CardKind.BasicAttackUp
                || kind == CardKind.MoveSpeedUp
                || kind == CardKind.LegionBanner
                || kind == CardKind.GuardShockwaveCrest;
        }
        public static int PassiveSlotStateHash
        {
            get
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + _commanderAttackLevel;
                    hash = hash * 31 + _commanderMoveLevel;
                    hash = hash * 31 + _legionBannerLevel;
                    hash = hash * 31 + _guardShockwaveCrestLevel;
                    return hash;
                }
            }
        }

        public static string LastEffectSummary { get; private set; } = "Changed: no runtime effect yet";

        public static void ResetRunState()
        {
            LastEffectSummary = "Changed: no runtime effect yet";
            _commanderAttackLevel = 0;
            _commanderMoveLevel = 0;
            _legionBannerLevel = 0;
            _guardShockwaveCrestLevel = 0;
            _passiveProgression.Reset();
        }

        public static void ClearServices()
        {
            _registry = null;
            _party = null;
        }

        public static void Apply(CardKind kind)
        {
            TryApply(kind);
        }

        public static bool TryApply(CardKind kind)
        {
            if (IsPassiveCard(kind) && CanAcquirePassive(kind) == false)
            {
                LastEffectSummary = "Changed: passive unavailable";
                return false;
            }

            CardDefinitionSet.Entry definition = CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry) ? entry : null;
            CardEffectKind effectKind = definition != null ? definition.EffectKind : ResolveFallbackEffectKind(kind);
            string cardId = ResolveCardId(kind, definition);
            bool applied;

            switch (effectKind)
            {
                case CardEffectKind.SmallHeal:
                    applied = ApplySmallHeal(ResolveIntValue(definition, SMALL_HEAL_AMOUNT), cardId);
                    break;
                case CardEffectKind.CommanderAttackBonus:
                    applied = ApplyCommanderAttackUp(ResolveIntValue(definition, COMMANDER_ATTACK_BONUS), cardId);
                    break;
                case CardEffectKind.CommanderMoveSpeedBonus:
                    applied = ApplyCommanderMoveSpeedUp(ResolveFloatValue(definition, COMMANDER_MOVE_SPEED_BONUS), cardId);
                    break;
                case CardEffectKind.AllyAttackBonusRatio:
                    applied = ApplyLegionBanner(ResolveFloatValue(definition, ALLY_ATTACK_BONUS_RATIO), cardId);
                    break;
                case CardEffectKind.GuardShockwaveBonusRatio:
                    applied = ApplyGuardShockwaveCrest(ResolveFloatValue(definition, GUARD_SHOCKWAVE_BONUS_RATIO), cardId);
                    break;
                default:
                    if (definition != null && ResolveFallbackEffectKind(kind) != CardEffectKind.None)
                        Debug.LogError($"P0 card '{kind}' is effect-capable but its Card Definition effect kind is None.");
                    LastEffectSummary = "Changed: no runtime effect yet";
                    applied = false;
                    break;
            }

            if (applied && IsPassiveCard(kind))
                _passiveProgression.TryRecordSuccess(ResolvePassiveId(kind), kind);

            return applied;
        }

        private static void LogPassiveApplied(string cardId, string effect, int level, string delta, string valueParameter)
        {
            P0Telemetry.Log(
                P0Telemetry.CardEffectApplyPassive,
                $"card_id={cardId}",
                $"effect={effect}",
                $"level={level}",
                $"delta={delta}",
                valueParameter);
            LogPassiveSlotState($"apply_{cardId}");
        }

        private static void LogPassiveSlotState(string reason)
        {
            P0Telemetry.Log(
                P0Telemetry.PassiveSlotStateUpdate,
                $"reason={reason}",
                "slot_model=prototype_effect_levels",
                $"slot_cap={PassiveSlotCap}",
                $"used_slots={ResolveUsedPassiveSlots()}",
                $"basic_attack_level={_commanderAttackLevel}",
                $"move_speed_level={_commanderMoveLevel}",
                $"legion_banner_level={_legionBannerLevel}",
                $"guard_shockwave_crest_level={_guardShockwaveCrestLevel}");
        }

        private static int ResolveUsedPassiveSlots()
        {
            int count = 0;
            if (_commanderAttackLevel > 0)
                count++;
            if (_commanderMoveLevel > 0)
                count++;
            if (_legionBannerLevel > 0)
                count++;
            if (_guardShockwaveCrestLevel > 0)
                count++;
            return count;
        }

        private static CardEffectKind ResolveFallbackEffectKind(CardKind kind)
        {
            return kind switch
            {
                CardKind.SmallHeal => CardEffectKind.SmallHeal,
                CardKind.BasicAttackUp => CardEffectKind.CommanderAttackBonus,
                CardKind.MoveSpeedUp => CardEffectKind.CommanderMoveSpeedBonus,
                CardKind.LegionBanner => CardEffectKind.AllyAttackBonusRatio,
                CardKind.GuardShockwaveCrest => CardEffectKind.GuardShockwaveBonusRatio,
                _ => CardEffectKind.None,
            };
        }

        private static int ResolveIntValue(CardDefinitionSet.Entry definition, int fallback)
        {
            return definition == null ? fallback : definition.IntValue;
        }

        private static float ResolveFloatValue(CardDefinitionSet.Entry definition, float fallback)
        {
            return definition == null ? fallback : definition.FloatValue;
        }

        private static string ResolveCardId(CardKind kind, CardDefinitionSet.Entry definition)
        {
            if (definition != null && string.IsNullOrWhiteSpace(definition.Id) == false)
                return definition.Id;

            return kind switch
            {
                CardKind.SmallHeal => "small_heal",
                CardKind.BasicAttackUp => "basic_attack_up",
                CardKind.MoveSpeedUp => "move_speed_up",
                CardKind.LegionBanner => "legion_banner",
                CardKind.GuardShockwaveCrest => "guard_shockwave_crest",
                CardKind.AddShieldSoldier => "shield_soldier",
                CardKind.RecruitArcher => "archer",
                CardKind.RecruitSwordsman => "swordsman",
                CardKind.RecruitCleric => "cleric",
                _ => kind.ToString(),
            };
        }

        private static string ResolvePassiveId(CardKind kind)
        {
            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry)
                && string.IsNullOrWhiteSpace(entry.Id) == false)
            {
                return entry.Id;
            }

            return kind.ToString();
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatPercent(float ratio)
        {
            return (ratio * 100.0f).ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
