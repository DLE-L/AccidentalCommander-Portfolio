using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using UnityEngine;

namespace Lizzo.PV.UI
{
    internal static class GeneratedCardIconCatalog
    {
        private const string ResourcePath = "Generated/card_icons_sheet_v3";
        private const int Rows = 3;
        private const int Columns = 4;
        private const float PixelsPerUnit = 64.0f;

        public static Sprite Resolve(CardKind kind)
        {
            int index = ResolveIndex(kind);
            if (index < 0
                || !RuntimeSpriteSheet.TryGetFrames(
                    ResourcePath,
                    Rows,
                    Columns,
                    PixelsPerUnit,
                    out Sprite[] frames)
                || index >= frames.Length)
            {
                return null;
            }

            return frames[index];
        }

        private static int ResolveIndex(CardKind kind)
        {
            return kind switch
            {
                CardKind.SmallHeal => 2,
                CardKind.BasicAttackUp => 5,
                CardKind.MoveSpeedUp => 9,
                CardKind.LegionBanner => 8,
                CardKind.GuardShockwaveCrest => 0,
                CardKind.PassiveMeleeTraining => 1,
                CardKind.PassiveFrontlineTempo => 5,
                CardKind.PassiveRangedTraining or
                CardKind.PassiveProjectileSpeed or
                CardKind.PassiveLongRange => 6,
                CardKind.PassiveHealingPrayer or
                CardKind.PassiveSwiftPrayer => 2,
                CardKind.PassiveBlueShieldCrest or
                CardKind.PassiveHoldFormation => 0,
                CardKind.PassiveBattleCommand => 5,
                CardKind.PassiveMarchSpeed => 9,
                CardKind.PassiveCommandRadius => 7,
                CardKind.PassiveSurvivalInstinct => 10,
                CardKind.PassiveOldFlag => 8,
                CardKind.PassiveWarDrum => 5,
                CardKind.PassiveSupplyPouch => 7,
                _ => -1,
            };
        }
    }
}
