using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Legion;
using Lizzo.PV.Presentation;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    // Run-level consequences of one death. Actor lifecycle and the once-only guard stay on EnemyActor.
    internal sealed class EnemyDeathResolver
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly RunState _state;
        private readonly RuntimeObjectSpawner _spawner;
        private readonly IDataProvider _data;
        private readonly RunContext _context;
        internal event System.Action<EnemyActor, int, CompanionEnemyDeathStatusSnapshot> Resolved;
        private float _normalHitStopUntil;
        private readonly CompanionSynergyProductionHost _synergies;
        private readonly CompanionEnemyDeathCombatEffects _deathEffects;

        internal EnemyDeathResolver(RuntimeObjectRegistry registry, RunState state,
            RuntimeObjectSpawner spawner, IDataProvider data, RunContext context,
            CompanionSynergyProductionHost synergies,
            CompanionEnemyDeathCombatEffects deathEffects)
        {
            _registry = registry;
            _state = state;
            _spawner = spawner;
            _data = data;
            _context = context;
            _synergies = synergies;
            _deathEffects = deathEffects;
        }

        internal void Resolve(EnemyActor enemy, CountableKillAttribution attribution,
            CompanionEnemyStatusState statuses)
        {
            _registry.MarkEnemyInactive(enemy);
            RunDiagnostics.RegisterEnemyDeath(enemy);
            _state.RegisterKill();
            _state.RegisterCountableKill(attribution.WithLethalContext(
                enemy.SpawnSequence, enemy.transform.position, Time.frameCount));
            bool hasStatus = statuses.TryCaptureDeath(Time.time, out CompanionEnemyDeathStatusSnapshot snapshot);
            _synergies?.ReportEnemyDeath(CompanionSynergyProductionHost.StableEntityId(enemy),
                enemy.transform.position, snapshot, attribution);
            if (hasStatus)
            {
                _deathEffects?.ReportCompanionEnemyDeathStatus(snapshot, enemy.transform.position);
            }

            string enemyId = enemy.RuntimeStats?.Data?.Id ?? enemy.GetDamageEnemyId();
            if (enemy.IsBoss)
            {
                RunTelemetry.LogOnce(RunTelemetry.FirstBossKill, RunTelemetry.RunTimeSecondsParameter,
                    $"boss={enemyId}");
                CombatRuntimeDiagnostics.Log("boss_defeated",
                    CombatRuntimeDiagnostics.Text("boss_id", enemyId),
                    CombatRuntimeDiagnostics.Text("time_since_spawn", "unavailable"));
                RunPauseController.RequestHitStop(0.15f, "boss_defeated");
            }

            int reward = EnemyExperienceRewardPolicy.Resolve(enemy.EncounterRank, _context, _data.RunTuning);
            if (reward > 0)
            {
                RunTelemetry.Log(RunTelemetry.EnemyRewardDrop, $"enemy_id={enemyId}",
                    $"actual_exp_reward={reward}", "orb_count=1", "visual_only=false");
                GemController gem = _spawner.SpawnGem(enemy.transform.position);
                gem?.SetRewardSource(enemyId, reward);
            }
            if (enemyId == CombatIds.RedCharger)
                RunPauseController.RequestHitStop(.08f, "red_charger_defeated");
            else if ((enemyId == CombatIds.SmallGoblin || enemyId == CombatIds.HungryWolf) && Time.time >= _normalHitStopUntil)
            {
                _normalHitStopUntil = Time.time + .18f;
            }
            Resolved?.Invoke(enemy, reward, snapshot);
        }
    }
}