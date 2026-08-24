using System;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public static class GeneratedProjectileVisualCatalog
    {
        private const string ResourcePath = "Generated/recording_projectiles_v3";
        private const int Rows = 2;
        private const int Columns = 3;
        private const float PixelsPerUnit = 64.0f;

        private const int RapidArrowFrame = 0;
        private const int PiercingSpearFrame = 1;
        private const int BlastStaffFrame = 2;
        private const int ClericBoltFrame = 3;
        private const int BombardierFrame = 4;
        private const int FireMageFrame = 5;

        public static bool TryResolveCatalogVisual(
            string presentationId,
            out Sprite sprite,
            out float scaleMultiplier)
        {
            sprite = null;
            scaleMultiplier = 1.0f;

            int frameIndex;
            switch (presentationId)
            {
                case "commander_basic":
                case "commander_rapid_crossbow":
                    frameIndex = RapidArrowFrame;
                    scaleMultiplier = 0.70f;
                    break;
                case "commander_piercing_spear":
                    frameIndex = PiercingSpearFrame;
                    scaleMultiplier = 0.70f;
                    break;
                case "commander_blast_staff":
                    frameIndex = BlastStaffFrame;
                    scaleMultiplier = 0.72f;
                    break;
                case "dmg_cleric_bolt_v1":
                    frameIndex = ClericBoltFrame;
                    scaleMultiplier = 0.52f;
                    break;
                default:
                    return false;
            }

            return TryGetFrame(frameIndex, out sprite);
        }

        public static bool TryGetBombardierPayload(out Sprite sprite)
        {
            return TryGetFrame(BombardierFrame, out sprite);
        }

        public static bool TryGetFireMagePayload(out Sprite sprite)
        {
            return TryGetFrame(FireMageFrame, out sprite);
        }

        private static bool TryGetFrame(int frameIndex, out Sprite sprite)
        {
            sprite = null;
            if (!RuntimeSpriteSheet.TryGetFrames(
                    ResourcePath,
                    Rows,
                    Columns,
                    PixelsPerUnit,
                    out Sprite[] frames)
                || frames == null
                || frameIndex < 0
                || frameIndex >= frames.Length)
            {
                return false;
            }

            sprite = frames[frameIndex];
            return sprite != null;
        }
    }
}
