using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.Combat.Attacks
{
    public static class ClericHealAttack
    {
        public static bool TryResolve(PartyService party, int healAmount)
        {
            PlayerController player = party.Registry?.Player;
            if (player != null && player.Hp > 0 && player.Hp < player.MaxHp && GetHpPercent(player.Hp, player.MaxHp) <= 30)
            {
                int beforeHp = player.Hp;
                player.Hp = Mathf.Min(player.MaxHp, player.Hp + healAmount);
                int actualHeal = player.Hp - beforeHp;
                FloatingDamageText.ShowHeal(player.transform.position, actualHeal);
                AttackVisual.SpawnAttached(player.transform, AttackVisualKind.HealPulse, new Vector3(0.0f, 0.32f, 0.0f));
                P0Telemetry.Log(
                    P0Telemetry.HealCast,
                    "target=commander",
                    "priority_reason=commander_low_hp",
                    $"heal_amount={actualHeal}",
                    $"hp_percent={GetHpPercent(player.Hp, player.MaxHp)}");
                LogHealSaveEvent(party, "commander", "commander_low_hp", actualHeal, GetHpPercent(player.Hp, player.MaxHp));
                return true;
            }

            CompanionRuntime target = FindDownedCompanion(party);
            if (target != null && target.ApplyHeal(healAmount, "downed_companion"))
            {
                LogCompanionHeal(party, target, healAmount, "downed_companion");
                return true;
            }

            target = FindDamagedCompanion(party, PartyService.SHIELD_FAMILY_TAG);
            if (target != null && target.ApplyHeal(healAmount, "shield_family"))
            {
                LogCompanionHeal(party, target, healAmount, "shield_family");
                return true;
            }

            target = FindDamagedCompanion(party, string.Empty);
            if (target != null && target.ApplyHeal(healAmount, "lowest_companion_hp"))
            {
                LogCompanionHeal(party, target, healAmount, "lowest_companion_hp");
                return true;
            }

            return false;
        }

        private static CompanionRuntime FindDownedCompanion(PartyService party)
        {
            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                if (companion != null && companion.IsDown)
                    return companion;
            }

            return null;
        }

        private static CompanionRuntime FindDamagedCompanion(PartyService party, string familyTag)
        {
            CompanionRuntime target = null;
            int lowestHpPercent = 101;

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                if (companion == null || companion.IsDown || companion.IsDamaged == false)
                    continue;

                if (string.IsNullOrEmpty(familyTag) == false && companion.IsFamily(familyTag) == false)
                    continue;

                int hpPercent = GetHpPercent(companion.Hp, companion.MaxHp);
                if (hpPercent >= lowestHpPercent)
                    continue;

                lowestHpPercent = hpPercent;
                target = companion;
            }

            return target;
        }

        private static void LogCompanionHeal(PartyService party, CompanionRuntime target, int healAmount, string priorityReason)
        {
            int actualHeal = target.LastAppliedHealAmount > 0 ? target.LastAppliedHealAmount : healAmount;
            P0Telemetry.Log(
                P0Telemetry.HealCast,
                $"target={target.UnitId}",
                $"priority_reason={priorityReason}",
                $"heal_amount={actualHeal}",
                $"hp_percent={GetHpPercent(target.Hp, target.MaxHp)}");
            LogHealSaveEvent(party, target.UnitId, priorityReason, actualHeal, GetHpPercent(target.Hp, target.MaxHp));
        }

        private static void LogHealSaveEvent(PartyService party, string targetId, string priorityReason, int healAmount, int hpPercent)
        {
            if (healAmount <= 0)
                return;

            P0Telemetry.Log(
                P0Telemetry.HealSaveEvent,
                $"target={targetId}",
                $"priority_reason={priorityReason}",
                $"heal_amount={healAmount}",
                $"hp_percent={hpPercent}",
                $"has_cleric={(party.ClericCount > 0).ToString().ToLowerInvariant()}");
        }

        private static int GetHpPercent(int hp, int maxHp)
        {
            if (maxHp <= 0)
                return 0;

            return Mathf.Clamp(Mathf.RoundToInt((float)hp / maxHp * 100.0f), 0, 100);
        }
    }
}
