using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Presentation;
using UnityEngine;

/// <summary>Run-scoped explicit gameplay spawn entry points.</summary>
public sealed class RuntimeObjectSpawner
{
    public event System.Action<EnemyActor> EnemySpawned;
    public event System.Action<GemController> GemSpawned;

    const string COMMANDER_PREFAB = "Units/Commander/Commander.prefab";
    readonly RunServices _services;
    CameraVisibilityZone _visibilityZone;

    public void BindVisibilityZone(CameraVisibilityZone zone) => _visibilityZone = zone;

    public RuntimeObjectSpawner(RunServices services)
    {
        _services = services ?? throw new System.ArgumentNullException(nameof(services));
    }

    public CommanderActor SpawnPlayer(Vector3 position)
    {
        GameObject go = _services.Factory.Spawn(COMMANDER_PREFAB, pooled: true);
        if (go == null) return null;
        go.name = "Player";
        go.transform.position = position;
        CommanderActor player = go.GetComponent<CommanderActor>();
        if (player == null)
        {
            Debug.LogError($"Commander prefab is missing required CommanderActor: {COMMANDER_PREFAB}", go);
            _services.Factory.Release(go);
            return null;
        }
        _services.BindCommander(player);
        player.ResetForSpawn();
        go.GetComponent<Lizzo.PV.Gameplay.Commander.Presentation.CommanderStatusVisualBinding>()?.Bind(_services.Factory, _services.CommanderStatuses);
        _services.Registry.RegisterPlayer(player);
        return player;
    }

    public EnemyActor SpawnEnemy(Vector3 position, int templateId)
    {
        return SpawnEnemy(position, templateId, EnemyEncounterRank.TemplateDefault, 1.0f);
    }

    public EnemyActor SpawnEnemy(
        Vector3 position,
        int templateId,
        EnemyEncounterRank encounterRank,
        float scaleMultiplier)
    {
        EnemyData enemyData = _services.App.Data.GetEnemyByTemplateId(templateId);
        string prefab = string.IsNullOrEmpty(enemyData?.Prefab) ? "Sweeper" : enemyData.Prefab;
        GameObject go = _services.Factory.Spawn(prefab + ".prefab", pooled: true);
        if (go == null) return null;
        go.transform.position = position;
        EnemyActor monster = go.GetComponent<EnemyActor>();
        if (monster == null)
        {
            Debug.LogError($"Enemy prefab is missing required EnemyActor: {prefab}.prefab", go);
            _services.Factory.Release(go);
            return null;
        }
        _services.BindEnemy(monster);
        monster.ResetForSpawn();
        go.GetComponent<Lizzo.PV.Gameplay.Enemies.Presentation.EnemyStatusVisualBinding>()?.Bind(_services.Factory);
        EnemyRuntimeStats.ApplyTo(monster, enemyData);
        monster.ConfigureEncounterRank(encounterRank, scaleMultiplier);
        SetupEnemyBehaviour(monster, templateId);
        IgnoreCommanderBodyCollision(monster);
        _services.Registry.RegisterEnemy(monster);
        RunDiagnostics.RegisterEnemySpawn(monster);
        EnemySpawned?.Invoke(monster);
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
        gem.BindVisibilityZone(_visibilityZone);
        gem.ResetForSpawn();
        _services.Registry.RegisterGem(gem);
        _services.Registry.RecordGemSuccess();
        GemSpawned?.Invoke(gem);
        return gem;
    }

    static void SetupEnemyBehaviour(EnemyActor monster, int templateId)
    {
        if (templateId != Define.SNAKE_ID) return;
        EnemyChargeController wolfDash = monster.ChargeAttack;
        if (wolfDash == null)
        {
            Debug.LogError("Hungry Wolf prefab is missing required EnemyChargeController.", monster);
            return;
        }
        wolfDash.Setup(monster);
    }

    void IgnoreCommanderBodyCollision(EnemyActor monster)
    {
        Collider2D commanderBody = _services.Registry.Player?.BodyCollider;
        Collider2D enemyBody = monster?.BodyCollider;
        if (commanderBody != null && enemyBody != null && commanderBody != enemyBody)
            Physics2D.IgnoreCollision(commanderBody, enemyBody, true);
    }
}
