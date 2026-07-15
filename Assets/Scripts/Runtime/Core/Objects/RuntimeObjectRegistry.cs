using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RuntimeObjectRegistry
{
    readonly IPrefabFactory _factory;
    readonly HashSet<MonsterController> _enemies = new HashSet<MonsterController>();
    readonly HashSet<MonsterController> _inactiveEnemies = new HashSet<MonsterController>();
    readonly HashSet<ProjectileController> _projectiles = new HashSet<ProjectileController>();
    readonly HashSet<GemController> _gems = new HashSet<GemController>();
    readonly HashSet<GameObject> _attackVisuals = new HashSet<GameObject>();
    GridController _gridController;

    public RuntimeObjectRegistry(IPrefabFactory factory, GridController gridController = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _gridController = gridController;
    }

    public PlayerController Player { get; private set; }
    public GridController Grid => _gridController;
    public IReadOnlyCollection<MonsterController> Enemies => _enemies;
    public IReadOnlyCollection<ProjectileController> Projectiles => _projectiles;
    public IReadOnlyCollection<GemController> Gems => _gems;
    public int DebugGemSpawnRequests { get; private set; }
    public int DebugGemSpawnSuccesses { get; private set; }
    public int DebugGemSpawnFailures { get; private set; }

    public void BindGrid(GridController gridController) => _gridController = gridController;

    public void RegisterPlayer(PlayerController player)
    {
        if (player == null) { Debug.LogError("[RuntimeObjectRegistry] Cannot register a null Player."); return; }
        if (Player != null && Player != player) Debug.LogError("[RuntimeObjectRegistry] Duplicate Player registration.", player);
        Player = player;
    }

    public void RegisterEnemy(MonsterController enemy)
    {
        if (enemy == null || !_enemies.Add(enemy)) Debug.LogError("[RuntimeObjectRegistry] Duplicate or null Enemy registration.", enemy);
    }

    public void RegisterProjectile(ProjectileController projectile)
    {
        if (projectile == null || !_projectiles.Add(projectile)) Debug.LogError("[RuntimeObjectRegistry] Duplicate or null Projectile registration.", projectile);
    }

    public void RegisterGem(GemController gem)
    {
        if (gem == null || !_gems.Add(gem)) Debug.LogError("[RuntimeObjectRegistry] Duplicate or null Gem registration.", gem);
        else _gridController?.Add(gem);
    }

    public void RegisterAttackVisual(GameObject visual)
    {
        if (visual == null || !_attackVisuals.Add(visual)) Debug.LogError("[RuntimeObjectRegistry] Duplicate or null attack visual registration.", visual);
    }

    public void MarkEnemyInactive(MonsterController enemy)
    {
        if (enemy == null || !_enemies.Remove(enemy) || !_inactiveEnemies.Add(enemy))
            Debug.LogError("[RuntimeObjectRegistry] Unknown Enemy inactive transition.", enemy);
    }

    public bool ReleaseEnemy(MonsterController enemy)
    {
        if (enemy == null) { Debug.LogError("[RuntimeObjectRegistry] Unknown Enemy release.", enemy); return false; }
        bool owned = _enemies.Remove(enemy) || _inactiveEnemies.Remove(enemy);
        if (!owned) { Debug.LogError("[RuntimeObjectRegistry] Unknown Enemy release.", enemy); return false; }
        return Release(enemy.gameObject);
    }

    public bool ReleaseGem(GemController gem)
    {
        if (gem == null || !_gems.Remove(gem)) { Debug.LogError("[RuntimeObjectRegistry] Unknown Gem release.", gem); return false; }
        _gridController?.Remove(gem.gameObject);
        return Release(gem.gameObject);
    }

    public bool ReleaseProjectile(ProjectileController projectile)
    {
        if (projectile == null || !_projectiles.Remove(projectile)) { Debug.LogError("[RuntimeObjectRegistry] Unknown Projectile release.", projectile); return false; }
        return Release(projectile.gameObject);
    }

    public bool ReleaseAttackVisual(GameObject visual)
    {
        if (visual == null || !_attackVisuals.Remove(visual)) { Debug.LogError("[RuntimeObjectRegistry] Unknown attack visual release.", visual); return false; }
        return Release(visual);
    }

    public void ReleaseAllEnemies()
    {
        List<MonsterController> snapshot = new List<MonsterController>(_enemies);
        snapshot.AddRange(_inactiveEnemies);
        foreach (MonsterController enemy in snapshot) ReleaseEnemy(enemy);
    }

    public void Clear()
    {
        if (Player != null) ReleaseIfAlive(Player);
        foreach (MonsterController enemy in new List<MonsterController>(_enemies)) ReleaseIfAlive(enemy);
        foreach (MonsterController enemy in new List<MonsterController>(_inactiveEnemies)) ReleaseIfAlive(enemy);
        foreach (ProjectileController projectile in new List<ProjectileController>(_projectiles)) ReleaseIfAlive(projectile);
        foreach (GemController gem in new List<GemController>(_gems))
        {
            if (gem != null)
            {
                _gridController?.Remove(gem.gameObject);
                ReleaseIfAlive(gem);
            }
        }
        foreach (GameObject visual in new List<GameObject>(_attackVisuals))
            if (visual != null) Release(visual);

        Player = null;
        _enemies.Clear();
        _inactiveEnemies.Clear();
        _projectiles.Clear();
        _gems.Clear();
        _attackVisuals.Clear();
        _gridController?.ClearObjects();
        ResetGemSpawnCounters();
    }

    public void RecordGemRequest() => DebugGemSpawnRequests++;
    public void RecordGemSuccess() => DebugGemSpawnSuccesses++;
    public void RecordGemFailure() => DebugGemSpawnFailures++;
    public void ResetGemSpawnCounters()
    {
        DebugGemSpawnRequests = 0;
        DebugGemSpawnSuccesses = 0;
        DebugGemSpawnFailures = 0;
    }

    void ReleaseIfAlive(Component component)
    {
        if (component != null) Release(component.gameObject);
    }

    bool Release(GameObject instance)
    {
        if (instance == null) return false;
        _factory.Release(instance);
        return true;
    }
}