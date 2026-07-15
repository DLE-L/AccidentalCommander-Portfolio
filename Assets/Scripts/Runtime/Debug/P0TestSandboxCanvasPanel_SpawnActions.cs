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
    Vector3 origin = player == null ? Vector3.zero : player.transform.position;
    Vector2 direction = Random.insideUnitCircle.normalized;
    if (direction.sqrMagnitude <= 0.0001f) direction = Vector2.right;
    Vector3 spawnPosition = origin + new Vector3(direction.x, direction.y, 0.0f) * 7.0f;
    MonsterController monster = player?.Services?.Spawner.SpawnEnemy(spawnPosition, templateId);
    if (monster == null) return;
    if (templateId == Define.RED_CHARGER_ID)
    {
        RedChargerBehaviour redCharger = monster.GetComponent<RedChargerBehaviour>();
        if (redCharger == null) { Debug.LogError("Red Charger prefab is missing required RedChargerBehaviour.", monster); return; }
        redCharger.Setup(monster);
        UnityEngine.Object.FindFirstObjectByType<Lizzo.PV.UI.GameplayUIController>()?.Hud?.ShowThreatDirection(monster.transform, "ELITE", new Color(1.0f, 0.2f, 0.08f, 1.0f));
        return;
    }
    if (templateId == Define.BOSS_ID)
    {
        GameScene scene = ResolveGameScene();
        if (scene != null) scene.StageType = Define.StageType.Boss;
        HungryGiantBehaviour hungryGiant = monster.GetComponent<HungryGiantBehaviour>();
        if (hungryGiant == null) { Debug.LogError("Hungry Giant prefab is missing required HungryGiantBehaviour.", monster); return; }
        hungryGiant.Setup(monster);
        P0BossDpsTracker.BeginBossFight(monster);
        UnityEngine.Object.FindFirstObjectByType<Lizzo.PV.UI.GameplayUIController>()?.Hud?.ShowThreatDirection(monster.transform, "BOSS", new Color(1.0f, 0.72f, 0.12f, 1.0f));
    }
}

        private void SpawnGem(PlayerController player)
        {
            LogDevButtonAction("SpawnGem", "spawn_gem");
            Vector3 origin = player == null ? Vector3.zero : player.transform.position;
            player?.Services?.Spawner.SpawnGem(origin + Vector3.up * 1.5f);
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
