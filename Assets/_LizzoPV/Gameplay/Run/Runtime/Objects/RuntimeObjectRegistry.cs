using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Combat.Projectiles;
using UnityEngine;

public sealed class RuntimeObjectRegistry
{
    readonly IPrefabFactory _factory;
    readonly HashSet<EnemyActor> _enemies = new HashSet<EnemyActor>();
    readonly HashSet<EnemyActor> _inactiveEnemies = new HashSet<EnemyActor>();
    readonly HashSet<CombatProjectileController> _projectiles = new HashSet<CombatProjectileController>();
    readonly HashSet<GemController> _gems = new HashSet<GemController>();
    long _nextEnemySpawnSequence = 1;
    public RuntimeObjectRegistry(IPrefabFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public CommanderActor Player { get; private set; }
    public IReadOnlyCollection<EnemyActor> Enemies => _enemies;
    public IReadOnlyCollection<CombatProjectileController> Projectiles => _projectiles;
    public IReadOnlyCollection<GemController> Gems => _gems;
    public int EnemyResidualCount => _enemies.Count + _inactiveEnemies.Count;
    public int ExpResidualCount => _gems.Count;
    public int DebugGemSpawnRequests { get; private set; }
    public int DebugGemSpawnSuccesses { get; private set; }
    public int DebugGemSpawnFailures { get; private set; }

    public void RegisterPlayer(CommanderActor player)
    {
        if (player == null) { Debug.LogError("[RuntimeObjectRegistry] Cannot register a null Player."); return; }
        if (Player != null && Player != player) Debug.LogError("[RuntimeObjectRegistry] Duplicate Player registration.", player);
        Player = player;
    }

    public void RegisterEnemy(EnemyActor enemy)
    {
        if (enemy == null)
        {
            Debug.LogError("[RuntimeObjectRegistry] Duplicate or null Enemy registration.", enemy);
            return;
        }
        if (_enemies.Contains(enemy))
        {
            Debug.LogError("[RuntimeObjectRegistry] Duplicate or null Enemy registration.", enemy);
            return;
        }
        if (_nextEnemySpawnSequence == long.MaxValue)
        {
            Debug.LogError("[RuntimeObjectRegistry] Enemy spawn sequence overflow.", enemy);
            return;
        }
        _enemies.Add(enemy);
        enemy.AssignSpawnSequence(_nextEnemySpawnSequence);
        _nextEnemySpawnSequence++;
    }

    public void RegisterProjectile(CombatProjectileController projectile)
    {
        if (projectile == null || !_projectiles.Add(projectile)) Debug.LogError("[RuntimeObjectRegistry] Duplicate or null Projectile registration.", projectile);
    }

    public void RegisterGem(GemController gem)
    {
        if (gem == null || !_gems.Add(gem)) Debug.LogError("[RuntimeObjectRegistry] Duplicate or null Gem registration.", gem);
    }

    public void MarkEnemyInactive(EnemyActor enemy)
    {
        if (enemy == null || !_enemies.Remove(enemy) || !_inactiveEnemies.Add(enemy))
            Debug.LogError("[RuntimeObjectRegistry] Unknown Enemy inactive transition.", enemy);
    }

    public bool ReleaseEnemy(EnemyActor enemy)
    {
        if (enemy == null) { Debug.LogError("[RuntimeObjectRegistry] Unknown Enemy release.", enemy); return false; }
        bool owned = _enemies.Remove(enemy) || _inactiveEnemies.Remove(enemy);
        if (!owned) { Debug.LogError("[RuntimeObjectRegistry] Unknown Enemy release.", enemy); return false; }
        enemy.ClearSpawnSequence();
        return Release(enemy.gameObject);
    }

    public bool ReleaseGem(GemController gem)
    {
        if (gem == null || !_gems.Remove(gem)) { Debug.LogError("[RuntimeObjectRegistry] Unknown Gem release.", gem); return false; }
        return Release(gem.gameObject);
    }

    public bool ReleaseProjectile(CombatProjectileController projectile)
    {
        if (projectile == null || !_projectiles.Remove(projectile)) { Debug.LogError("[RuntimeObjectRegistry] Unknown Projectile release.", projectile); return false; }
        return Release(projectile.gameObject);
    }

    public void ReleaseProjectilesBySourceId(string sourceId)
    {
        if (string.IsNullOrEmpty(sourceId))
            return;

        List<CombatProjectileController> snapshot = new List<CombatProjectileController>();
        foreach (CombatProjectileController projectile in _projectiles)
        {
            if (projectile != null && projectile.Request.SourceId == sourceId)
                snapshot.Add(projectile);
        }

        for (int index = 0; index < snapshot.Count; index++)
            snapshot[index].Release();
    }

    public void ReleaseAllEnemies()
    {
        List<EnemyActor> snapshot = new List<EnemyActor>(_enemies);
        snapshot.AddRange(_inactiveEnemies);
        foreach (EnemyActor enemy in snapshot) ReleaseEnemy(enemy);
    }

    public void Clear()
    {
        if (Player != null) ReleaseIfAlive(Player);
        foreach (EnemyActor enemy in new List<EnemyActor>(_enemies)) { if (enemy != null) enemy.ClearSpawnSequence(); ReleaseIfAlive(enemy); }
        foreach (EnemyActor enemy in new List<EnemyActor>(_inactiveEnemies)) { if (enemy != null) enemy.ClearSpawnSequence(); ReleaseIfAlive(enemy); }
        foreach (CombatProjectileController projectile in new List<CombatProjectileController>(_projectiles))
            if (projectile != null) projectile.Release();
        foreach (GemController gem in new List<GemController>(_gems))
        {
            if (gem != null)
                ReleaseIfAlive(gem);
        }
        Player = null;
        _enemies.Clear();
        _inactiveEnemies.Clear();
        _projectiles.Clear();
        _gems.Clear();
        _nextEnemySpawnSequence = 1;
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
