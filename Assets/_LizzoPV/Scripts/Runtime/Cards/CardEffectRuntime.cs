using System.Globalization;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static class CardEffectRuntime
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

        public sealed class PassiveProgression
        {
                        private readonly List<CardKind> _acquisitionOrder = new List<CardKind>(MaxDistinctPassiveTypes);
private readonly Dictionary<string, int> _acquisitionCounts = new Dictionary<string, int>(MaxDistinctPassiveTypes);

            public int DistinctCount => _acquisitionCounts.Count;

            public int GetCount(string passiveId)
            {
                return string.IsNullOrWhiteSpace(passiveId) == false
                    && _acquisitionCounts.TryGetValue(passiveId, out int count)
                    ? count
                    : 0;
            }

            public bool IsEligible(string passiveId)
            {
                if (string.IsNullOrWhiteSpace(passiveId))
                    return false;

                int currentCount = GetCount(passiveId);
                return currentCount < MaxPassiveAcquisitions
                    && (currentCount > 0 || _acquisitionCounts.Count < MaxDistinctPassiveTypes);
            }

            public bool TryRecordSuccess(string passiveId, CardKind kind)
            {
                if (IsEligible(passiveId) == false)
                    return false;

                int currentCount = GetCount(passiveId);
                if (currentCount == 0)
                    _acquisitionOrder.Add(kind);

                _acquisitionCounts[passiveId] = currentCount + 1;
                return true;
            }

            public bool TryRecordSuccess(string passiveId)
            {
                return TryRecordSuccess(passiveId, default);
            }


            public int FillDistinctKinds(CardKind[] kinds)
            {
                if (kinds == null)
                    return 0;

                int count = Mathf.Min(kinds.Length, _acquisitionOrder.Count);
                for (int i = 0; i < count; i++)
                    kinds[i] = _acquisitionOrder[i];

                return count;
            }


            public void Reset()
            {
                _acquisitionCounts.Clear();
                _acquisitionOrder.Clear();
            }
        }

        public static int GetPassiveAcquisitionCount(CardKind kind)
        {
            return IsPassiveCard(kind) ? _passiveProgression.GetCount(ResolvePassiveId(kind)) : 0;
        }

        public static int FillAcquiredPassiveKinds(CardKind[] kinds)
        {
            return _passiveProgression.FillDistinctKinds(kinds);
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

        public static int FillPassiveSlotLabels(string[] labels)
        {
            if (labels == null)
                return 0;

            for (int i = 0; i < labels.Length; i++)
                labels[i] = string.Empty;

            int count = 0;
            AddPassiveSlotLabel(labels, ref count, "공", _commanderAttackLevel);
            AddPassiveSlotLabel(labels, ref count, "속", _commanderMoveLevel);
            AddPassiveSlotLabel(labels, ref count, "깃", _legionBannerLevel);
            AddPassiveSlotLabel(labels, ref count, "충", _guardShockwaveCrestLevel);
            return count;
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

        public static string BuildCorePassiveSummary()
        {
            string summary = string.Empty;
            summary = AppendPassiveSummary(summary, ResolvePassiveSummaryLabel(CardKind.BasicAttackUp, "전투 지휘"), _commanderAttackLevel);
            summary = AppendPassiveSummary(summary, ResolvePassiveSummaryLabel(CardKind.MoveSpeedUp, "행군 속도"), _commanderMoveLevel);
            summary = AppendPassiveSummary(summary, ResolvePassiveSummaryLabel(CardKind.LegionBanner, "군단 깃발"), _legionBannerLevel);
            summary = AppendPassiveSummary(summary, ResolvePassiveSummaryLabel(CardKind.GuardShockwaveCrest, "방패 충격문장"), _guardShockwaveCrestLevel);
            return string.IsNullOrEmpty(summary) ? "없음" : summary;
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

        private static bool ApplySmallHeal(int amount, string cardId)
        {
            PlayerController player = _registry?.Player;
            int commanderHeal = 0;

            if (player != null && player.Hp > 0)
            {
                int beforeHp = player.Hp;
                player.Hp = Mathf.Min(player.MaxHp, player.Hp + amount);
                commanderHeal = player.Hp - beforeHp;
                if (commanderHeal > 0)
                {
                    FloatingDamageText.ShowHeal(player.transform.position, commanderHeal);
                    AttackVisual.SpawnAttached(player.transform, AttackVisualKind.HealPulse, new Vector3(0.0f, 0.32f, 0.0f));
                }
            }

            int healedCompanions = _party.ApplySmallHealToCompanions(amount);
            LastEffectSummary = $"Changed: Heal +{amount}, allies {healedCompanions}";
            P0Telemetry.Log(
                P0Telemetry.HealCast,
                $"source={cardId}",
                $"commander_heal={commanderHeal}",
                $"companion_targets={healedCompanions}");
            return true;
        }

        private static bool ApplyCommanderAttackUp(int amount, string cardId)
        {
            PlayerController player = _registry?.Player;
            CommanderAttack attack = player == null ? null : player.GetComponent<CommanderAttack>();
            if (attack == null)
            {
                LastEffectSummary = "Changed: attack up failed";
                return false;
            }

            int newDamage = attack.AddDamageBonus(amount);
            AttackVisual.SpawnAttached(player.transform, AttackVisualKind.BuffPulse, new Vector3(0.0f, 0.32f, 0.0f));
            LastEffectSummary = $"Changed: Commander ATK +{amount} => {newDamage}";
            _commanderAttackLevel++;
            P0Telemetry.Log(
                P0Telemetry.UpgradeCommander,
                "stat=attack",
                $"delta={amount}",
                $"value={newDamage}");
            LogPassiveApplied(cardId, "commander_attack", _commanderAttackLevel, $"+{amount}", $"value={newDamage}");
            return true;
        }

        private static bool ApplyCommanderMoveSpeedUp(float amount, string cardId)
        {
            PlayerController player = _registry?.Player;
            if (player == null)
            {
                LastEffectSummary = "Changed: move speed up failed";
                return false;
            }

            float newSpeed = player.MoveSpeed + amount;
            player.SetMoveSpeed(newSpeed);
            AttackVisual.SpawnAttached(player.transform, AttackVisualKind.BuffPulse, new Vector3(0.0f, 0.32f, 0.0f));
            LastEffectSummary = $"Changed: Commander Move +{FormatNumber(amount)} => {FormatNumber(newSpeed)}";
            _commanderMoveLevel++;
            P0Telemetry.Log(
                P0Telemetry.UpgradeCommander,
                "stat=move_speed",
                $"delta={FormatNumber(amount)}",
                $"value={FormatNumber(newSpeed)}");
            LogPassiveApplied(cardId, "commander_move_speed", _commanderMoveLevel, $"+{FormatNumber(amount)}", $"value={FormatNumber(newSpeed)}");
            return true;
        }

        private static bool ApplyLegionBanner(float ratio, string cardId)
        {
            if (_party == null)
            {
                LastEffectSummary = "Changed: ally attack up failed";
                return false;
            }

            float multiplier = _party.AddAllyAttackBonus(ratio);
            PlayerController player = _registry?.Player;
            if (player != null)
                AttackVisual.SpawnAttached(player.transform, AttackVisualKind.BuffPulse, new Vector3(0.0f, 0.32f, 0.0f));

            string percent = FormatPercent(ratio);
            _legionBannerLevel++;
            LastEffectSummary = $"Changed: Ally ATK +{percent}% => x{FormatNumber(multiplier)}";
            P0Telemetry.Log(
                P0Telemetry.CardEffectApply,
                $"card_id={cardId}",
                "effect=ally_attack_multiplier",
                $"delta_percent={percent}",
                $"value_multiplier={FormatNumber(multiplier)}",
                $"active_companions={_party.ActiveCompanionCount}");
            LogPassiveApplied(cardId, "ally_attack_multiplier", _legionBannerLevel, $"+{percent}%", $"value_multiplier={FormatNumber(multiplier)}");
            return true;
        }

        private static bool ApplyGuardShockwaveCrest(float ratio, string cardId)
        {
            if (_party == null)
            {
                LastEffectSummary = "Changed: guard shockwave up failed";
                return false;
            }

            float multiplier = _party.AddGuardWallBonus(ratio);
            PlayerController player = _registry?.Player;
            if (player != null)
                AttackVisual.SpawnAttached(player.transform, AttackVisualKind.BuffPulse, new Vector3(0.0f, 0.32f, 0.0f));

            string percent = FormatPercent(ratio);
            _guardShockwaveCrestLevel++;
            LastEffectSummary = $"Changed: Guard Shockwave +{percent}% => x{FormatNumber(multiplier)}";
            P0Telemetry.Log(
                P0Telemetry.CardEffectApply,
                $"card_id={cardId}",
                "effect=guard_shockwave_radius_duration",
                $"delta_percent={percent}",
                $"value_multiplier={FormatNumber(multiplier)}",
                $"guard_active={_party.IsGuardSquadActivated.ToString().ToLowerInvariant()}");
            LogPassiveApplied(cardId, "guard_shockwave_radius_duration", _guardShockwaveCrestLevel, $"+{percent}%", $"value_multiplier={FormatNumber(multiplier)}");
            return true;
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

        private static void AddPassiveSlotLabel(string[] labels, ref int count, string shortLabel, int level)
        {
            if (level <= 0 || count >= labels.Length || count >= PassiveSlotCap)
                return;

            labels[count] = level > 1 ? $"{shortLabel}{level}" : shortLabel;
            count++;
        }

        private static string AppendPassiveSummary(string summary, string label, int level)
        {
            if (level <= 0)
                return summary;

            string entry = $"{label} Lv.{level}";
            return string.IsNullOrEmpty(summary) ? entry : $"{summary} / {entry}";
        }

        private static string ResolvePassiveSummaryLabel(CardKind kind, string fallback)
        {
            return CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry)
                && string.IsNullOrWhiteSpace(entry.PassiveSummaryLabel) == false
                    ? entry.PassiveSummaryLabel
                    : fallback;
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
