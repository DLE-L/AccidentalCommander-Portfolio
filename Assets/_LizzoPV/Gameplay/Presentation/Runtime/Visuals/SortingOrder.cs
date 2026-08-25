using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public static class SortingOrder
    {
        public const int Map = -100;
        public const int GroundEffect = 12;
        public const int Unit = 20;
        public const int HitEffect = 34;
        public const int WorldBarBack = 40;
        public const int WorldBarFill = 41;
        public const int WorldText = 44;
        public const int FloatingText = 46;

        public static void ApplyToRenderers(GameObject root, int sortingOrder)
        {
            if (root == null)
            {
                Debug.LogError("[SortingOrder] Cannot apply sorting order because root is null.");
                return;
            }

            RendererSortingCache cache = root.GetComponent<RendererSortingCache>();
            if (cache == null)
            {
                Debug.LogError($"[SortingOrder] Missing authored RendererSortingCache on '{root.name}'. Add it to the prefab or scene root.");
                return;
            }

            cache.Apply(sortingOrder);
        }
    }
}
