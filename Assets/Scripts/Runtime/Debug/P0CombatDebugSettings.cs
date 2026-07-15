using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Debugging
{
    public static class P0CombatDebugSettings
    {
        public static bool ShowCombatGizmos { get; set; }
        public static bool AutoStartGuardSquadPushTest { get; set; } = false;
        public static bool NoIdleAnimationTestEnabled { get; set; }
        public static bool AttackAnimationTestEnabled { get; set; }
        public static bool ShieldSoldierAreaPushTestEnabled { get; set; }
        public static bool BossAttackMotionTestEnabled { get; set; }

        public static void DrawCollider2D(Collider2D collider, Color color)
        {
            if (ShowCombatGizmos == false || collider == null || collider.enabled == false)
                return;

            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.color = color;
            if (collider is CircleCollider2D circle)
            {
                Vector2 center = circle.transform.TransformPoint(circle.offset);
                float maxScale = Mathf.Max(
                    Mathf.Abs(circle.transform.lossyScale.x),
                    Mathf.Abs(circle.transform.lossyScale.y));
                Gizmos.DrawWireSphere(center, circle.radius * maxScale);
            }
            else if (collider is BoxCollider2D box)
            {
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else
            {
                Bounds bounds = collider.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }

    public static class P0GuardSquadPushTestScenario
    {
        private const int TEST_ENEMY_HP = 999999;

public static void TryStart(PlayerController player, StageSpawner stageSpawner)
{
    if (P0CombatDebugSettings.AutoStartGuardSquadPushTest == false)
        return;

    if (player == null || player.Services == null || player.Services.Registry == null)
        return;

    if (stageSpawner != null)
        stageSpawner.Stopped = true;

    Vector3 origin = player.transform.position;
    Vector3 forward = Vector3.up;
    Vector3 right = Vector3.right;
    Vector3 wallCenter = origin + forward * 1.6f;
    RuntimeObjectSpawner spawner = player.Services.Spawner;

    SpawnTestEnemy(spawner, Define.GOBLIN_ID, wallCenter + right * -0.85f + forward * 0.15f);
    SpawnTestEnemy(spawner, Define.GOBLIN_ID, wallCenter + right * 0.85f + forward * 0.15f);
    SpawnTestEnemy(spawner, Define.SNAKE_ID, wallCenter + right * -0.35f + forward * -0.35f);
    SpawnTestEnemy(spawner, Define.SNAKE_ID, wallCenter + right * 0.35f + forward * -0.35f);
    SpawnTestEnemy(spawner, Define.ORC_ID, wallCenter + right * -0.45f + forward * 0.55f);
    SpawnTestEnemy(spawner, Define.ORC_ID, wallCenter + right * 0.45f + forward * 0.55f);

    player.Services.Party.Recruit(CompanionKind.ShieldSoldier);
    player.Services.Party.Recruit(CompanionKind.Swordsman);
    player.Services.Party.Recruit(CompanionKind.Cleric);

    P0Telemetry.Log(
        "guard_squad_push_test_start",
        "mode=auto_start",
        "enemy_invincible=true",
        "enemy_movement=frozen",
        "spawn_stopped=true",
        "guard_squad=active");
}

        private static void SpawnTestEnemy(RuntimeObjectSpawner spawner, int templateId, Vector3 position)
        {
            MonsterController monster = spawner.SpawnEnemy(position, templateId);
            if (monster == null)
                return;

            monster.gameObject.name = $"P0_PushTest_{monster.gameObject.name}";
            P0GuardSquadPushTestEnemy marker = monster.gameObject.GetComponent<P0GuardSquadPushTestEnemy>();
            if (marker == null)
                marker = monster.gameObject.AddComponent<P0GuardSquadPushTestEnemy>();

            marker.Configure(monster, TEST_ENEMY_HP);
        }
    }

    public sealed class P0GuardSquadPushTestEnemy : MonoBehaviour
    {
        private MonsterController _monster;
        private int _testHp;

        public void Configure(MonsterController monster, int testHp)
        {
            _monster = monster;
            _testHp = Mathf.Max(1, testHp);

            DisableAutonomousMovement();
            _monster.SetExternalMovement(true);
            _monster.MaxHp = _testHp;
            _monster.Hp = _testHp;
        }

        private void LateUpdate()
        {
            if (_monster == null)
                return;

            if (_monster.MaxHp != _testHp)
                _monster.MaxHp = _testHp;

            if (_monster.Hp < _testHp)
                _monster.Hp = _testHp;
        }

        private void DisableAutonomousMovement()
        {
            WolfDashBehaviour wolfDash = GetComponent<WolfDashBehaviour>();
            if (wolfDash != null)
                wolfDash.enabled = false;

            RedChargerBehaviour redCharger = GetComponent<RedChargerBehaviour>();
            if (redCharger != null)
                redCharger.enabled = false;

            HungryGiantBehaviour hungryGiant = GetComponent<HungryGiantBehaviour>();
            if (hungryGiant != null)
                hungryGiant.enabled = false;
        }
    }
}
