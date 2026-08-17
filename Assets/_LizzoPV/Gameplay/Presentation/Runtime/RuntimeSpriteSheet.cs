using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public static class RuntimeSpriteSheet
    {
        private static readonly Dictionary<string, Sprite[]> FramesByKey =
            new Dictionary<string, Sprite[]>(StringComparer.Ordinal);

        public static bool TryGetFrames(
            string resourcePath,
            int rows,
            int columns,
            float pixelsPerUnit,
            out Sprite[] frames)
        {
            frames = null;
            if (string.IsNullOrWhiteSpace(resourcePath)
                || rows <= 0
                || columns <= 0
                || pixelsPerUnit <= 0.0f)
            {
                return false;
            }

            string key = resourcePath + "|" + rows + "|" + columns + "|" + pixelsPerUnit;
            if (FramesByKey.TryGetValue(key, out frames))
            {
                if (frames != null
                    && frames.Length == rows * columns
                    && AreFramesAlive(frames))
                {
                    return true;
                }

                FramesByKey.Remove(key);
                frames = null;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null || texture.width < columns || texture.height < rows)
                return false;

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            frames = new Sprite[rows * columns];
            for (int rowFromTop = 0; rowFromTop < rows; rowFromTop += 1)
            {
                int sourceTop = Mathf.RoundToInt(
                    texture.height - (rowFromTop * texture.height / (float)rows));
                int sourceBottom = Mathf.RoundToInt(
                    texture.height - ((rowFromTop + 1) * texture.height / (float)rows));
                for (int column = 0; column < columns; column += 1)
                {
                    int sourceLeft = Mathf.RoundToInt(column * texture.width / (float)columns);
                    int sourceRight = Mathf.RoundToInt((column + 1) * texture.width / (float)columns);
                    int index = (rowFromTop * columns) + column;
                    frames[index] = Sprite.Create(
                        texture,
                        new Rect(
                            sourceLeft,
                            sourceBottom,
                            sourceRight - sourceLeft,
                            sourceTop - sourceBottom),
                        new Vector2(0.5f, 0.5f),
                        pixelsPerUnit,
                        0u,
                        SpriteMeshType.FullRect);
                    frames[index].name = resourcePath.Replace('/', '_') + "_" + index.ToString("00");
                }
            }

            FramesByKey[key] = frames;
            return true;
        }

        private static bool AreFramesAlive(Sprite[] frames)
        {
            for (int i = 0; i < frames.Length; i += 1)
            {
                if (frames[i] == null)
                    return false;
            }

            return true;
        }
    }
}
