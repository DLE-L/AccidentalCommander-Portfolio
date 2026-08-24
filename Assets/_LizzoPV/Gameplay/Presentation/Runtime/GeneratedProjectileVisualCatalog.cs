using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public static class GeneratedProjectileVisualCatalog
    {
        private const string ResourcePath = "Generated/recording_projectiles_v3";
        private const int Rows = 2;
        private const int Columns = 3;
        private const float PixelsPerUnit = 64.0f;

        private const int BombardierFrame = 4;

        public static bool TryGetBombardierPayload(out Sprite sprite)
        {
            return TryGetFrame(BombardierFrame, out sprite);
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
