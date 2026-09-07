using UnityEngine;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private void SeedFallbackEnemies()
        {
            AddEnemy(Define.GOBLIN_ID, "small_goblin", "작은 고블린", "Units/Enemies/SmallGoblin", "normal", 7, 5, 1.0f, 0.8f, 0.9f, 0.0f, 0.0f, 0.0f, 0.0f, 0.8f, 0.0f, new Color(0.58f, 1.0f, 0.35f, 1.0f), 200);
            AddEnemy(Define.SNAKE_ID, "hungry_wolf", "굶주린 늑대", "Units/Enemies/HungryWolf", "normal", 5, 7, 0.9f, 0.8f, 1.25f, 45.0f, 0.0f, 0.0f, 0.0f, 0.8f, 0.0f, new Color(0.85f, 0.85f, 0.95f, 1.0f), 201, chargeAttack: 10);
            AddEnemy(Define.ORC_ID, "shield_orc", "방패 오크", "Units/Enemies/ShieldOrc", "normal", 25, 6, 1.5f, 1.0f, 0.65f, 90.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, new Color(0.35f, 0.7f, 0.45f, 1.0f), 202);
            AddEnemy(Define.RED_CHARGER_ID, "elite_red_charger", "붉은 돌격수", "Units/Enemies/RedCharger", "elite", 240, 16, 1.0f, 1.0f, 1.35f, 150.0f, 5.0f, 0.9f, 0.0f, 0.0f, 1.4f, new Color(1.0f, 0.05f, 0.05f, 1.0f), 210, chargeAttack: 24);
            AddEnemy(Define.BOSS_ID, "boss_hungry_giant", "굶주린 거인", "Units/Enemies/HungryGiant", "boss", 2500, 25, 1.0f, 2.2f, 0.55f, 300.0f, 7.0f, 0.0f, 4.0f, 2.2f, 1.6f, new Color(0.45f, 0.08f, 0.08f, 1.0f), 220);
        }

        private void AddEnemy(
            int templateId,
            string id,
            string displayName,
            string prefab,
            string type,
            int hp,
            int attack,
            float attackCooldown,
            float contactRange,
            float moveSpeed,
            float spawnSeconds,
            float chargeCooldown,
            float chargeDuration,
            float patternCooldown,
            float range,
            float width,
            Color color,
            int sortingOrder,
            int chargeAttack = 0)
        {
            EnemyData data = new EnemyData
            {
                TemplateId = templateId,
                Id = id,
                DisplayName = displayName,
                Prefab = prefab,
                Type = type,
                Hp = hp,
                Attack = attack,
                ChargeAttack = chargeAttack > 0 ? chargeAttack : attack,
                AttackCooldown = attackCooldown,
                ContactRange = contactRange,
                MoveSpeed = moveSpeed,
                SpawnSeconds = spawnSeconds,
                ChargeCooldown = chargeCooldown,
                ChargeDuration = chargeDuration,
                PatternCooldown = patternCooldown,
                Range = range,
                Width = width,
                Color = color,
                SortingOrder = sortingOrder,
            };

            Enemies[id] = data;
            EnemiesByTemplateId[templateId] = data;
        }
    }
}
