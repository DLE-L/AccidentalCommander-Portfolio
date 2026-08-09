using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.Combat.Attacks
{
    public static class ClericHealAttack
    {
        public readonly struct SupportHealTarget
        {
            public readonly PlayerController Commander;
            public readonly CompanionRuntime Companion;
            public readonly int Hp;
            public readonly int MaxHp;
            public readonly int InstanceId;

            public SupportHealTarget(PlayerController commander)
            {
                Commander = commander;
                Companion = null;
                Hp = commander.Hp;
                MaxHp = commander.MaxHp;
                InstanceId = commander.GetInstanceID();
            }

            public SupportHealTarget(CompanionRuntime companion)
            {
                Commander = null;
                Companion = companion;
                Hp = companion.Hp;
                MaxHp = companion.MaxHp;
                InstanceId = companion.GetInstanceID();
            }
        }

        public static bool TryResolve(PartyService party, int healAmount)
        {
            CompanionRuntime downedTarget = FindDownedCompanion(party);
            if (downedTarget != null && downedTarget.ApplyHeal(healAmount, "downed_companion"))
            {
                LogCompanionHeal(party, downedTarget, healAmount, "downed_companion");
                return true;
            }

            PlayerController player = party.Registry?.Player;
            CompanionRuntime companionTarget = FindDamagedCompanion(party, string.Empty);
            if (IsDamaged(player) && IsLowerHp(player.Hp, player.MaxHp, companionTarget))
            {
                if (TryHealCommander(party, player, healAmount, "lowest_hp"))
                    return true;
            }

            if (companionTarget != null && companionTarget.ApplyHeal(healAmount, "lowest_companion_hp"))
            {
                LogCompanionHeal(party, companionTarget, healAmount, "lowest_companion_hp");
                ReportCompanionHealingBond(party, companionTarget, healAmount);
                return true;
            }

            return false;
        }

        public static bool TryResolveNoRevive(PartyService party, Vector3 casterPosition, int healAmount, float range)
        {
            List<SupportHealTarget> targets = new List<SupportHealTarget>(1);
            return TryResolveNoRevive(party, casterPosition, healAmount, range, 1, 1.0f, targets);
        }

        public static bool TryResolveNoRevive(
            PartyService party,
            Vector3 casterPosition,
            int healAmount,
            float range,
            int maxTargets,
            float secondTargetRatio,
            List<SupportHealTarget> targets)
        {
            if (party == null || healAmount <= 0 || range < 0.0f || maxTargets <= 0 || targets == null)
                return false;

            float sqrRange = range * range;
            CollectNoReviveTargets(party, casterPosition, sqrRange, maxTargets, targets);
            bool resolved = false;
            for (int i = 0; i < targets.Count; i++)
            {
                SupportHealTarget target = targets[i];
                int targetHeal = i == 0
                    ? healAmount
                    : Mathf.Max(1, Mathf.RoundToInt(healAmount * secondTargetRatio));
                if (target.Commander != null)
                    resolved |= TryHealCommander(party, target.Commander, targetHeal, "lowest_hp_no_revive");
                else if (target.Companion != null && target.Companion.ApplyHeal(targetHeal, "lowest_hp_no_revive"))
                {
                    LogCompanionHeal(party, target.Companion, targetHeal, "lowest_hp_no_revive");
                    ReportCompanionHealingBond(party, target.Companion, targetHeal);
                    resolved = true;
                }
            }

            return resolved;
        }

        private static void CollectNoReviveTargets(
            PartyService party,
            Vector3 casterPosition,
            float sqrRange,
            int maxTargets,
            List<SupportHealTarget> targets)
        {
            targets.Clear();
            PlayerController player = party.Registry?.Player;
            if (IsDamagedInRange(player, casterPosition, sqrRange))
                AddSortedTarget(new SupportHealTarget(player), maxTargets, targets);

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                if (companion == null || companion.IsDown || companion.Hp <= 0 || companion.IsDamaged == false)
                    continue;
                if ((companion.transform.position - casterPosition).sqrMagnitude > sqrRange)
                    continue;

                AddSortedTarget(new SupportHealTarget(companion), maxTargets, targets);
            }
        }

        private static void AddSortedTarget(SupportHealTarget candidate, int maxTargets, List<SupportHealTarget> targets)
        {
            int insertIndex = 0;
            while (insertIndex < targets.Count && CompareTargets(targets[insertIndex], candidate) <= 0)
                insertIndex++;

            if (insertIndex >= maxTargets)
                return;

            targets.Insert(insertIndex, candidate);
            if (targets.Count > maxTargets)
                targets.RemoveAt(maxTargets);
        }

        private static int CompareTargets(SupportHealTarget left, SupportHealTarget right)
        {
            long leftRatio = (long)left.Hp * right.MaxHp;
            long rightRatio = (long)right.Hp * left.MaxHp;
            if (leftRatio != rightRatio)
                return leftRatio < rightRatio ? -1 : 1;

            return left.InstanceId.CompareTo(right.InstanceId);
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

        private static CompanionRuntime FindDamagedCompanionInRange(PartyService party, Vector3 casterPosition, float sqrRange)
        {
            CompanionRuntime target = null;
            int lowestHpPercent = 101;

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                if (companion == null || companion.IsDown || companion.IsDamaged == false)
                    continue;

                if ((companion.transform.position - casterPosition).sqrMagnitude > sqrRange)
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

        private static void ReportCompanionHealingBond(PartyService party, CompanionRuntime target, int requestedHealAmount)
        {
            int effectiveHealAmount = target.LastAppliedHealAmount;
            if (effectiveHealAmount < 1)
                return;

            party.ReportHealingBond(target, new SynergyHealingEvent(
                true,
                effectiveHealAmount,
                effectiveHealAmount < requestedHealAmount,
                false,
                false));
        }

        private static void ReportCommanderHealingBond(PartyService party, PlayerController player, int effectiveHealAmount, int requestedHealAmount)
        {
            if (effectiveHealAmount < 1)
                return;

            party.ReportHealingBond(player, new SynergyHealingEvent(
                true,
                effectiveHealAmount,
                effectiveHealAmount < requestedHealAmount,
                false,
                false));
        }

        private static int GetHpPercent(int hp, int maxHp)
        {
            if (maxHp <= 0)
                return 0;

            return Mathf.Clamp(Mathf.RoundToInt((float)hp / maxHp * 100.0f), 0, 100);
        }
    

        private static bool IsDamaged(PlayerController player)
        {
            return player != null && player.Hp > 0 && player.MaxHp > 0 && player.Hp < player.MaxHp;
        }

        private static bool IsDamagedInRange(PlayerController player, Vector3 casterPosition, float sqrRange)
        {
            return IsDamaged(player) && (player.transform.position - casterPosition).sqrMagnitude <= sqrRange;
        }

        private static bool IsLowerHp(int hp, int maxHp, CompanionRuntime target)
        {
            return target == null || GetHpPercent(hp, maxHp) <= GetHpPercent(target.Hp, target.MaxHp);
        }


private static bool TryHealCommander(PartyService party, PlayerController player, int healAmount, string priorityReason)
        {
            int beforeHp = player.Hp;
            player.Hp = Mathf.Min(player.MaxHp, player.Hp + healAmount);
            int actualHeal = player.Hp - beforeHp;
            FloatingDamageText.ShowHeal(player.transform.position, actualHeal);
            AttackVisual.SpawnAttached(player.transform, AttackVisualKind.HealPulse, new Vector3(0.0f, 0.32f, 0.0f));
            P0Telemetry.Log(
                P0Telemetry.HealCast,
                "target=commander",
                $"priority_reason={priorityReason}",
                $"heal_amount={actualHeal}",
                $"hp_percent={GetHpPercent(player.Hp, player.MaxHp)}");
            LogHealSaveEvent(party, "commander", priorityReason, actualHeal, GetHpPercent(player.Hp, player.MaxHp));
            ReportCommanderHealingBond(party, player, actualHeal, healAmount);
            return true;
        }


}
}
