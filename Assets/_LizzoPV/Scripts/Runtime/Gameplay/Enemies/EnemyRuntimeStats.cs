using Lizzo.PV.P0.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class EnemyRuntimeStats : MonoBehaviour
    {
        public EnemyData Data { get; private set; }
        public int AttackDamage => Data == null ? 2 : Data.Attack;
        public float AttackCooldown => Data == null ? 0.1f : Mathf.Max(0.05f, Data.AttackCooldown);
        public static EnemyRuntimeStats ApplyTo(MonsterController monster, EnemyData data)
        {
            if (monster == null || data == null)
                return null;

            EnemyRuntimeStats stats = monster.RuntimeStats;
            if (stats == null)
            {
                Debug.LogError($"Enemy prefab is missing required EnemyRuntimeStats: {monster.gameObject.name}", monster);
                return null;
            }

            stats.Apply(monster, data);
            monster.RefreshRuntimeStatsCache(stats);
            return stats;
        }

        private void Apply(MonsterController monster, EnemyData data)
        {
            Data = data;
            gameObject.name = $"P0_{data.Id}";

            monster.MaxHp = data.Hp;
            monster.Hp = data.Hp;
            monster.SetMoveSpeed(data.MoveSpeed);

            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = SortingOrder.Unit;

            PixelFantasyVisualBridge.ApplyEnemyVisual(gameObject, data);
            EnemyDamageHitbox.ApplyTo(monster, data);
EnemyHealthBar healthBar = monster.HealthBar;
            if (healthBar == null)
            {
                Debug.LogError($"Enemy prefab is missing required EnemyHealthBar: {gameObject.name}", this);
                return;
            }

            bool alwaysVisible = data.Id == CombatIds.ShieldOrc || data.Id == CombatIds.EliteRedCharger;
            healthBar.Refresh(monster, alwaysVisible, visibleSeconds: 0.0f);
        }

        private void OnDisable()
        {
            Data = null;
        }
    }
}
