using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public static class PixelFantasyVisualBridge
    {
        private const string VisualChildName = "Visual";

        public static void ApplyCommanderVisual(GameObject owner)
        {
            ValidatePrefabVisual(owner, "commander");
        }

        public static void ApplyAllyVisual(GameObject owner, UnitData data, int sortingOrder)
        {
            if (owner == null || data == null)
                return;

            string visualId = data.Id switch
            {
                "shield_guard" => "shield_guard",
                "shield_captain" => "shield_captain",
                "sword_soldier" => "sword_soldier",
                "cleric" => "cleric",
                "archer" => "archer",
                _ => string.Empty,
            };

            if (string.IsNullOrEmpty(visualId))
                return;

            ValidatePrefabVisual(owner, visualId);
        }

        public static void ApplyEnemyVisual(GameObject owner, EnemyData data)
        {
            if (owner == null || data == null)
                return;

            switch (data.Id)
            {
                case "small_goblin":
                    ValidatePrefabVisual(owner, "small_goblin");
                    break;
                case "shield_orc":
                    ValidatePrefabVisual(owner, "shield_orc");
                    break;
                case "hungry_wolf":
                    ValidatePrefabVisual(owner, "hungry_wolf");
                    break;
                case "elite_red_charger":
                    ValidatePrefabVisual(owner, "elite_red_charger");
                    break;
                case "boss_hungry_giant":
                    ValidatePrefabVisual(owner, "boss_hungry_giant");
                    break;
            }
        }

        private static bool ValidatePrefabVisual(GameObject owner, string visualId)
        {
            if (owner == null)
                return false;

            Transform visual = owner.transform.Find(VisualChildName);
            if (visual == null)
            {
                Debug.LogError($"P0 unit prefab is missing required child: {VisualChildName}. visual_id={visualId}", owner);
                return false;
            }

            SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"P0 unit {VisualChildName} is missing required SpriteRenderer. visual_id={visualId}", owner);
                return false;
            }

            if (spriteRenderer.sprite == null)
                Debug.LogError($"P0 unit {VisualChildName} SpriteRenderer has no sprite. visual_id={visualId}", owner);

            UnitVisualDriver visualDriver = visual.GetComponent<UnitVisualDriver>();
            if (visualDriver == null)
            {
                Debug.LogError($"P0 unit {VisualChildName} is missing required UnitVisualDriver. visual_id={visualId}", owner);
                return false;
            }

            spriteRenderer.enabled = true;
            return true;
        }
    }
}
