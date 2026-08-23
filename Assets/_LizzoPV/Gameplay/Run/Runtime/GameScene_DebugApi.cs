using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

public partial class GameScene
{
    public void DebugAddExperience(int amount)
    {
        if (IsRunLoaded)
            _runState.AddExperience(amount);
    }

    public void DebugSetRunLevel(int level)
    {
        if (!IsRunLoaded)
            return;

        int safeLevel = Mathf.Max(1, level);
        _runState.SetLevelForDebug(safeLevel, Mathf.Max(1, _services.App.Data.GetLevelExp(safeLevel)));
    }

    public void DebugForceLevelUp()
    {
        if (IsRunLoaded)
            _levelProgression.HandleExperienceChanged(_runState.RequiredExperience, _runState.RequiredExperience);
    }

    public void DebugSetRunElapsedSeconds(float seconds)
    {
        if (IsRunLoaded)
            _runState.SetElapsedSecondsForDebug(seconds);
    }

    public void DebugSetSpawnStopped(bool stopped)
    {
        if (_stageSpawner != null)
            _stageSpawner.Stopped = stopped;
    }

    public void DebugClearEnemies()
    {
        _services?.Registry?.ReleaseAllEnemies();
    }

    public bool DebugSpawnEnemy(int templateId)
    {
        PlayerController player = _services?.Registry?.Player;
        if (!IsRunLoaded || player == null)
            return false;

        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        MonsterController monster = _services.Spawner.SpawnEnemy(
            player.transform.position + new Vector3(direction.x, direction.y, 0.0f) * 7.0f,
            templateId);
        if (monster == null)
            return false;

        if (templateId == Define.RED_CHARGER_ID)
        {
            RedChargerBehaviour redCharger = monster.GetComponent<RedChargerBehaviour>();
            if (redCharger == null)
                return false;

            redCharger.Setup(monster);
            _uiController?.ShowThreatDirection(monster.transform, "ELITE", new Color(1.0f, 0.2f, 0.08f, 1.0f));
            return true;
        }

        if (templateId == Define.BOSS_ID)
        {
            StageType = Define.StageType.Boss;
            HungryGiantBehaviour hungryGiant = monster.GetComponent<HungryGiantBehaviour>();
            if (hungryGiant == null)
                return false;

            hungryGiant.Setup(monster);
            P0BossDpsTracker.BeginBossFight(monster);
            _uiController?.ShowThreatDirection(monster.transform, "BOSS", new Color(1.0f, 0.72f, 0.12f, 1.0f));
        }

        return true;
    }

    public bool DebugSpawnGem()
    {
        PlayerController player = _services?.Registry?.Player;
        if (!IsRunLoaded || player == null)
            return false;

        return _services.Spawner.SpawnGem(player.transform.position + Vector3.up * 1.5f) != null;
    }

    public bool DebugSetCommanderHp(int hp)
    {
        PlayerController player = _services?.Registry?.Player;
        if (!IsRunLoaded || player == null)
            return false;

        player.MaxHp = Mathf.Max(player.MaxHp, hp);
        player.Hp = Mathf.Clamp(hp, 1, player.MaxHp);
        return true;
    }

    public bool DebugRestoreCommanderHp()
    {
        PlayerController player = _services?.Registry?.Player;
        if (!IsRunLoaded || player == null)
            return false;

        player.Hp = player.MaxHp;
        return true;
    }

    public bool DebugRecruit(CompanionKind kind)
    {
        if (!IsRunLoaded || _services?.Party == null)
            return false;

        _services.Party.Recruit(kind);
        return true;
    }

    public bool DebugStartBossVisibilityFixture()
    {
        if (!IsRunLoaded || _services?.Party == null || _bossSpawnController == null)
            return false;

        DebugRecruit(CompanionKind.ShieldSoldier);
        DebugRecruit(CompanionKind.ShieldSoldier);
        DebugRecruit(CompanionKind.ShieldSoldier);
        DebugRecruit(CompanionKind.Cleric);
        DebugRecruit(CompanionKind.Swordsman);
        DebugRecruit(CompanionKind.Archer);
        DebugRecruit(CompanionKind.Archer);
        DebugSetSpawnStopped(true);
        _bossSpawnController.DebugJumpToHungryGiantPrelude();
        return true;
    }
}
