using System.Collections.Generic;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        internal List<ChainTargetCandidate> CollectCanonicalChainTargets()
        {
            _chainCandidates.Clear();
            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false) continue;
                _chainCandidates.Add(new ChainTargetCandidate(monster, AllyTargeting.ResolveTargetPoint(monster, transform.position), monster.GetInstanceID()));
            }
            CompanionChainCombatSetup setup = _chainSetup;
            ChainTargetSelector.Collect(_chainCandidates, transform.position, setup.InitialRange, setup.ChainDistance, setup.MaxTargets, _chainTargets);
            return _chainTargets;
        }

        internal bool AttackCanonicalChain()
        {
            List<ChainTargetCandidate> targets = this.CollectCanonicalChainTargets();
            if (targets.Count == 0) return false;
            this.FaceTarget(targets[0].Target);
            RunBossDpsTracker.RecordAttackCast(GetSourceId(), targets[0].Target);
            this.SpawnCanonicalCompanionAttack(targets[0].Point, targets[0].Point - transform.position);
            for (int i = 0; i < targets.Count; i++)
                this.DamageTarget(targets[i].Target, AttackVisualKind.SingleHit, spawnHitVisual: false);
            if (targets[0].Target.IsValid())
            {
                CompanionRuntime runtime = GetRuntime();
                int ownerId = runtime == null ? GetInstanceID() : runtime.GetInstanceID();
                targets[0].Target.ApplyCompanionStatus(
                    _chainSetup.FirstTargetStatusKind,
                    new CompanionStatusSource(GetSourceId(), ownerId),
                    _chainSetup.FirstTargetStatusMagnitude,
                    _chainSetup.FirstTargetStatusDuration,
                    Time.time);
            }
            return true;
        }

        public void SetCanonicalChainInfo(CompanionChainCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedChain;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.InitialRange, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds;
            _sourceIdOverride = setup.SourceId;
            _chainSetup = setup;
            _projectileSpeedMultiplier = 1.0f;
            _chainAbilitySchedule = new CombatAbilitySchedule();
            _chainAbilitySchedule.Configure(
                setup.Period,
                setup.NoTargetRetrySeconds,
                Time.time,
                UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }
    }
}
