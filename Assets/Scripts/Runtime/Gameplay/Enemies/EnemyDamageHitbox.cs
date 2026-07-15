using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public static class EnemyDamageHitbox
    {
        public static void ApplyTo(MonsterController monster, EnemyData data)
        {
            if (monster == null || data == null)
                return;

            Collider2D collider = monster.CombatCollider;
            if (collider == null)
            {
                Debug.LogError($"Enemy prefab is missing required CombatCollider reference. enemy_id={data.Id}", monster);
                return;
            }

            ApplyTo(collider, data, monster);
        }

        public static void ApplyTo(GameObject enemyObject, EnemyData data)
        {
            if (enemyObject == null || data == null)
                return;

            UnitColliderRefs colliderRefs = enemyObject.GetComponent<UnitColliderRefs>();
            if (colliderRefs == null)
            {
                Debug.LogError($"Enemy prefab is missing required UnitColliderRefs. enemy_id={data.Id}", enemyObject);
                return;
            }

            Collider2D collider = colliderRefs.CombatCollider;
            if (collider == null)
            {
                Debug.LogError($"Enemy prefab is missing required CombatCollider reference. enemy_id={data.Id}", enemyObject);
                return;
            }

            ApplyTo(collider, data, enemyObject);
        }

        private static void ApplyTo(Collider2D collider, EnemyData data, Object context)
        {
            if (collider.isTrigger == false)
                Debug.LogError($"Enemy CombatCollider must be trigger. enemy_id={data.Id}", context);

            collider.enabled = true;
        }
    }
}
