using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion
{
    public static class UnitAccentView
    {

        private const string FRIENDLY_BASE_NAME = "P0_FriendlyBaseRing";
        private const string FRIENDLY_ROLE_ACCENT_NAME = "P0_FriendlyRoleAccent";
        private const string ENEMY_SHADOW_NAME = "P0_EnemyShadow";
        private const string ENEMY_ACCENT_NAME = "P0_EnemyAccent";
        private const float FRIENDLY_BASE_Y = -0.03f;
        private const float ENEMY_SHADOW_Y = -0.03f;

public static void AttachCommander(GameObject commanderObject, int sortingOrder)
        {
            if (commanderObject == null)
                return;

            ConfigureAccent(
                commanderObject.transform,
                FRIENDLY_BASE_NAME,
                new Color(0.1f, 0.55f, 1.0f, 0.38f),
                new Vector3(0.0f, FRIENDLY_BASE_Y, 0.0f),
                new Vector3(0.72f, 0.2f, 1.0f),
                SortingOrder.UnitShadow);
        }

public static void AttachAlly(GameObject allyObject, string unitId, int sortingOrder)
        {
            if (allyObject == null)
                return;

            bool isShieldCaptain = unitId == "shield_captain";
            bool isShieldSoldier = unitId == "shield_guard";
            float width = isShieldCaptain ? 0.62f : isShieldSoldier ? 0.52f : 0.46f;
            float height = isShieldCaptain ? 0.17f : isShieldSoldier ? 0.14f : 0.12f;
            Color color = isShieldCaptain
                ? new Color(0.0f, 0.78f, 1.0f, 0.34f)
                : new Color(0.12f, 0.48f, 1.0f, 0.28f);

            ConfigureAccent(
                allyObject.transform,
                FRIENDLY_BASE_NAME,
                color,
                new Vector3(0.0f, FRIENDLY_BASE_Y, 0.0f),
                new Vector3(width, height, 1.0f),
                SortingOrder.UnitShadow);

            AttachAllyRoleAccent(allyObject.transform, unitId, SortingOrder.UnitAccent);
        }

private static void AttachAllyRoleAccent(Transform parent, string unitId, int sortingOrder)
        {
            switch (unitId)
            {
                case "shield_guard":
                    ConfigureAccent(parent, FRIENDLY_ROLE_ACCENT_NAME, new Color(0.1f, 0.8f, 1.0f, 0.95f), new Vector3(0.16f, 0.28f, 0.0f), new Vector3(0.16f, 0.16f, 1.0f), sortingOrder);
                    break;
                case "shield_captain":
                    ConfigureAccent(parent, FRIENDLY_ROLE_ACCENT_NAME, new Color(1.0f, 0.82f, 0.18f, 0.95f), new Vector3(0.2f, 0.34f, 0.0f), new Vector3(0.22f, 0.22f, 1.0f), sortingOrder);
                    break;
                case "sword_soldier":
                    ConfigureAccent(parent, FRIENDLY_ROLE_ACCENT_NAME, new Color(1.0f, 0.46f, 0.22f, 0.95f), new Vector3(0.18f, 0.28f, 0.0f), new Vector3(0.14f, 0.14f, 1.0f), sortingOrder);
                    break;
                case "cleric":
                    ConfigureAccent(parent, FRIENDLY_ROLE_ACCENT_NAME, new Color(0.55f, 1.0f, 0.58f, 0.95f), new Vector3(0.18f, 0.3f, 0.0f), new Vector3(0.12f, 0.12f, 1.0f), sortingOrder);
                    break;
                case "archer":
                    ConfigureAccent(parent, FRIENDLY_ROLE_ACCENT_NAME, new Color(1.0f, 0.92f, 0.18f, 0.95f), new Vector3(0.18f, 0.28f, 0.0f), new Vector3(0.14f, 0.14f, 1.0f), sortingOrder);
                    break;
                default:
                    Debug.LogError($"[UnitAccentView] Unsupported companion unit id '{unitId}'.", parent);
                    break;
            }
        }

private static void ConfigureAccent(Transform parent, string name, Color color, Vector3 localPosition, Vector3 localScale, int sortingOrder)
        {
            Transform child = parent.Find(name);
            SpriteRenderer renderer = child?.GetComponent<SpriteRenderer>();
            if (child == null || renderer == null || renderer.sprite == null)
            {
                Debug.LogError($"[UnitAccentView] Authored accent '{name}' is missing on '{parent.name}'.", parent);
                return;
            }

            child.localPosition = localPosition;
            child.localScale = localScale;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = true;
        }


public static void AttachEnemy(GameObject enemyObject, EnemyData data)
        {
            if (enemyObject == null || data == null)
                return;

            float shadowWidth = data.Type == "boss" ? 1.35f : data.Type == "elite" ? 0.78f : 0.55f;
            float shadowHeight = data.Type == "boss" ? 0.32f : data.Type == "elite" ? 0.2f : 0.14f;
            ConfigureAccent(
                enemyObject.transform,
                ENEMY_SHADOW_NAME,
                ResolveShadowColor(data),
                new Vector3(0.0f, ENEMY_SHADOW_Y, 0.0f),
                new Vector3(shadowWidth, shadowHeight, 1.0f),
                SortingOrder.UnitShadow);

            if (data.Id == CombatIds.HungryWolf)
                ConfigureAccent(enemyObject.transform, ENEMY_ACCENT_NAME, new Color(0.95f, 0.18f, 0.28f, 1.0f), new Vector3(0.2f, 0.34f, 0.0f), new Vector3(0.18f, 0.18f, 1.0f), SortingOrder.UnitAccent);
            else if (data.Id == CombatIds.ShieldOrc)
                ConfigureAccent(enemyObject.transform, ENEMY_ACCENT_NAME, new Color(0.95f, 0.55f, 0.18f, 1.0f), new Vector3(0.24f, 0.25f, 0.0f), new Vector3(0.24f, 0.24f, 1.0f), SortingOrder.UnitAccent);
            else if (data.Type == "elite")
                ConfigureAccent(enemyObject.transform, ENEMY_ACCENT_NAME, new Color(1.0f, 0.12f, 0.05f, 1.0f), new Vector3(0.0f, 0.48f, 0.0f), new Vector3(0.26f, 0.26f, 1.0f), SortingOrder.UnitAccent);
            else if (data.Type == "boss")
                ConfigureAccent(enemyObject.transform, ENEMY_ACCENT_NAME, new Color(1.0f, 0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.7f, 0.0f), new Vector3(0.42f, 0.42f, 1.0f), SortingOrder.UnitAccent);
            else
                ConfigureAccent(enemyObject.transform, ENEMY_ACCENT_NAME, new Color(1.0f, 0.25f, 0.12f, 1.0f), new Vector3(0.18f, 0.25f, 0.0f), new Vector3(0.12f, 0.12f, 1.0f), SortingOrder.UnitAccent);
        }

        private static Color ResolveShadowColor(EnemyData data)
        {
            if (data.Id == CombatIds.HungryWolf)
                return new Color(0.9f, 0.05f, 0.1f, 0.32f);

            if (data.Id == CombatIds.ShieldOrc)
                return new Color(0.95f, 0.45f, 0.12f, 0.34f);

            if (data.Type == "boss")
                return new Color(1.0f, 0.0f, 0.0f, 0.45f);

            if (data.Type == "elite")
                return new Color(1.0f, 0.02f, 0.0f, 0.4f);

            return new Color(1.0f, 0.05f, 0.05f, 0.3f);
        }
    }
}
