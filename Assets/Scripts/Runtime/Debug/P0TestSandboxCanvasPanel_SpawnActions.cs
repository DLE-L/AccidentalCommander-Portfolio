using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Debugging
{
    public sealed partial class P0TestSandboxCanvasPanel
    {
        private void SpawnEnemy(string buttonId, int templateId, PlayerController player)
        {
            LogDevButtonAction(buttonId, "spawn_enemy", $"template_id={templateId}", $"enemy_key={ResolveEnemyButtonKey(templateId)}");
            ResolveGameScene()?.DebugSpawnEnemy(templateId);
        }

        private void SpawnGem(PlayerController player)
        {
            LogDevButtonAction("SpawnGem", "spawn_gem");
            ResolveGameScene()?.DebugSpawnGem();
        }

        private static string ResolveEnemyButtonKey(int templateId)
        {
            if (templateId == Define.GOBLIN_ID)
                return "small_goblin";
            if (templateId == Define.SNAKE_ID)
                return "hungry_wolf";
            if (templateId == Define.ORC_ID)
                return "shield_orc";
            if (templateId == Define.RED_CHARGER_ID)
                return "red_charger";
            if (templateId == Define.BOSS_ID)
                return "hungry_giant";

            return "unknown";
        }
    }
}
