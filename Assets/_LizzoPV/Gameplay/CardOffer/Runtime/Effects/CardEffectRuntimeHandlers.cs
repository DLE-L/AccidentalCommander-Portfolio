using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static partial class CardEffectRuntime
    {
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
                    AttackVisual.SpawnAttached(player.transform, AttackVisualKind.HealingReceived, new Vector3(0.0f, 0.32f, 0.0f));
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
            AttackVisual.SpawnAttached(player.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.32f, 0.0f));
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
            AttackVisual.SpawnAttached(player.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.32f, 0.0f));
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
                AttackVisual.SpawnAttached(player.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.32f, 0.0f));

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
                AttackVisual.SpawnAttached(player.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.32f, 0.0f));

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

    }
}
