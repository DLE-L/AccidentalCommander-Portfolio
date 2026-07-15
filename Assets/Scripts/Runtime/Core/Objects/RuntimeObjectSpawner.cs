using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

/// <summary>Run-scoped explicit gameplay spawn entry points.</summary>
public sealed class RuntimeObjectSpawner
{
    const string COMMANDER_PREFAB = "P0/Units/Commander/Commander.prefab";
    const string COMMANDER_PROJECTILE_PREFAB = "CommanderProjectile.prefab";
    readonly RunServices _services;

    public RuntimeObjectSpawner(RunServices services)
    {
        _services = services ?? throw new System.ArgumentNullException(nameof(services));
    }

    public PlayerController SpawnPlayer(Vector3 position)
    {
        GameObject go = _services.Factory.Spawn(COMMANDER_PREFAB, pooled: true);
        if (go == null) return null;
        go.name = "Player";
        go.transform.position = position;
        PlayerController player = go.GetComponent<PlayerController>();
        if (player == null)
        {
            Debug.LogError($"Commander prefab is missing required PlayerController: {COMMANDER_PREFAB}", go);
            _services.Factory.Release(go);
            return null;
        }
        player.Initialize(_services);
        player.ResetForSpawn();
        player.BindGrid(_services.Registry.Grid);
        _services.Registry.RegisterPlayer(player);
        return player;
    }

    public MonsterController SpawnEnemy(Vector3 position, int templateId)
    {
        EnemyData enemyData = _services.App.Data.GetEnemyByTemplateId(templateId);
        string prefab = string.IsNullOrEmpty(enemyData?.Prefab) ? "Sweeper" : enemyData.Prefab;
        GameObject go = _services.Factory.Spawn(prefab + ".prefab", pooled: true);
        if (go == null) return null;
        go.transform.position = position;
        MonsterController monster = go.GetComponent<MonsterController>();
        if (monster == null)
        {
            Debug.LogError($"Enemy prefab is missing required MonsterController: {prefab}.prefab", go);
            _services.Factory.Release(go);
            return null;
        }
        monster.Initialize(_services);
        monster.ResetForSpawn();
        EnemyRuntimeStats.ApplyTo(monster, enemyData);
        SetupEnemyBehaviour(monster, templateId);
        _services.Party.IgnoreFriendlyBodyCollisionsWithEnemy(monster);
        _services.Registry.RegisterEnemy(monster);
        P0PlaytestDiagnostics.RegisterEnemySpawn(monster);
        return monster;
    }

    public GemController SpawnGem(Vector3 position)
    {
        _services.Registry.RecordGemRequest();
        GameObject go = _services.Factory.Spawn(Define.EXP_GEM_PREFAB, pooled: true);
        if (go == null) { _services.Registry.RecordGemFailure(); return null; }
        go.transform.position = position;
        GemController gem = go.GetComponent<GemController>();
        if (gem == null)
        {
            Debug.LogError($"EXP gem prefab is missing required GemController: {Define.EXP_GEM_PREFAB}", go);
            _services.Registry.RecordGemFailure();
            _services.Factory.Release(go);
            return null;
        }
        gem.ResetForSpawn();
        _services.Registry.RegisterGem(gem);
        _services.Registry.RecordGemSuccess();
        return gem;
    }

    public ProjectileController SpawnCommanderProjectile(Vector3 position)
    {
        GameObject go = _services.Factory.Spawn(COMMANDER_PROJECTILE_PREFAB, pooled: true);
        if (go == null) return null;
        go.transform.position = position;
        ProjectileController projectile = go.GetComponent<ProjectileController>();
        if (projectile == null)
        {
            Debug.LogError($"Projectile prefab is missing required ProjectileController: {COMMANDER_PROJECTILE_PREFAB}", go);
            _services.Factory.Release(go);
            return null;
        }
        projectile.Initialize(_services);
        projectile.ResetForSpawn();
        _services.Registry.RegisterProjectile(projectile);
        return projectile;
    }

    static void SetupEnemyBehaviour(MonsterController monster, int templateId)
    {
        if (templateId != Define.SNAKE_ID) return;
        WolfDashBehaviour wolfDash = monster.WolfDash;
        if (wolfDash == null)
        {
            Debug.LogError("Hungry Wolf prefab is missing required WolfDashBehaviour.", monster);
            return;
        }
        wolfDash.Setup(monster);
    }
}