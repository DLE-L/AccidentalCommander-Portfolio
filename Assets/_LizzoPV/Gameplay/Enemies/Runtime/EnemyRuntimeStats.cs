using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    public sealed class EnemyRuntimeStats : MonoBehaviour
    {
        public EnemyData Data { get; private set; }
        public int AttackDamage => Data == null ? 2 : Data.Attack;
        public int ChargeDamage => Data == null || Data.ChargeAttack <= 0 ? AttackDamage : Data.ChargeAttack;
        public float AttackCooldown => Data == null ? 0.1f : Mathf.Max(0.05f, Data.AttackCooldown);
        public static EnemyRuntimeStats ApplyTo(EnemyActor monster, EnemyData data)
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

        private void Apply(EnemyActor monster, EnemyData data)
        {
            Data = data;
            gameObject.name = data.Id;

            monster.ResetHealth(data.Hp);
            monster.SetMoveSpeed(data.MoveSpeed);

            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = SortingOrder.Unit;

            UnitVisualAuthoringValidator.ValidateEnemyVisual(gameObject, data);
            Collider2D combatCollider = monster.CombatCollider;
            if (combatCollider == null)
            {
                Debug.LogError($"Enemy prefab is missing required CombatCollider reference. enemy_id={data.Id}", monster);
            }
            else
            {
                if (combatCollider.isTrigger == false)
                    Debug.LogError($"Enemy CombatCollider must be trigger. enemy_id={data.Id}", monster);
                combatCollider.enabled = true;
            }

            EnemyHealthBar healthBar = monster.HealthBar;
            if (healthBar == null)
            {
                Debug.LogError($"Enemy prefab is missing required EnemyHealthBar: {gameObject.name}", this);
                return;
            }

            if (monster.IsBoss)
            {
                EnemyHealthBar.RemoveFrom(monster.transform);
            }
            else
            {
                bool alwaysVisible = monster.IsElite || data.Id == CombatIds.ShieldOrc;
                healthBar.Refresh(monster, alwaysVisible, visibleSeconds: 0.0f);
            }
        }

        private void OnDisable()
        {
            Data = null;
        }
    }
}
