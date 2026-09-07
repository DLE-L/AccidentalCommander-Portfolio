using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.World;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class BossArena : MonoBehaviour
    {
        private const float BossOffsetFromCommander = 4.2f;
        private const float ArenaWidth = 8.4f;
        private const float ArenaHeight = 12.0f;
        private const string RootName = "P0_BossArena";

        private static BossArena _current;
        private Vector3 _bossSpawnPosition;

        public Vector3 BossSpawnPosition => _bossSpawnPosition;

        public static BossArena Create(Vector3 commanderPosition, IPrefabFactory factory, ArenaBounds arenaBounds)
        {
            Clear();

            GameObject root = factory.Spawn("BossArenaAuthoring.prefab");
            if (root == null)
            {
                Debug.LogError("[BossArena] Missing authored BossArenaAuthoring.prefab.");
                return null;
            }

            root.name = RootName;
            BossArena arena = root.GetComponent<BossArena>();
            if (arena == null)
            {
                Debug.LogError("[BossArena] Authored prefab is missing BossArena component.", root);
                Object.Destroy(root);
                return null;
            }

            Vector2 resolvedCenter = arenaBounds.ResolveBossArenaCenter(commanderPosition, ArenaWidth, ArenaHeight, BossOffsetFromCommander);
            Vector2 resolvedSpawn = arenaBounds.ResolveBossSpawnPosition(resolvedCenter, BossOffsetFromCommander);
            Vector3 center = new Vector3(resolvedCenter.x, resolvedCenter.y, commanderPosition.z);
            arena.transform.position = center;
            arena._bossSpawnPosition = new Vector3(resolvedSpawn.x, resolvedSpawn.y, commanderPosition.z);
            _current = arena;

            RunTelemetry.Log(
                RunTelemetry.BossArenaCreate,
                RunTelemetry.RunTimeSecondsParameter,
                $"center={center.x:0.##}:{center.y:0.##}",
                $"boss_spawn={arena._bossSpawnPosition.x:0.##}:{arena._bossSpawnPosition.y:0.##}",
                $"width={ArenaWidth:0.##}",
                $"height={ArenaHeight:0.##}");

            return arena;
        }

        public static void Clear()
        {
            if (_current == null)
                return;

            Object.Destroy(_current.gameObject);
            _current = null;
        }

        private void OnDestroy()
        {
            if (_current == this)
                _current = null;
        }
    }
}
