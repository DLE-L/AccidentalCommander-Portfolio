using System.Collections.Generic;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        internal void UpdateCanonicalReturningAttack(float currentTime)
        {
            if (_returningPassPending && currentTime >= _returningPassDueTime)
            {
                _returningPassPending = false;
                ResolveReturningAttackPass(ReturningAttackPass.Return);
            }

            if (_returningPassPending || _returningAttackSchedule.IsDue(currentTime) == false)
                return;

            bool resolved = TryBeginReturningAttack(currentTime);
            _returningAttackSchedule.RecordResolution(
                currentTime,
                resolved,
                resolved ? ResolveAttackIntervalDivisor() : 1.0f);
            if (resolved)
                _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);
        }

        private bool TryBeginReturningAttack(float currentTime)
        {
            MonsterController target = this.FindNearestMonster(_returningAttackSetup.Range);
            if (target == null)
                return false;

            Vector3 direction = target.transform.position - transform.position;
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            _returningAttackStart = transform.position;
            _returningAttackEnd = _returningAttackStart + direction.normalized * _returningAttackSetup.Range;
            _returningAttackHitLedger.Reset();
            this.FaceDirection(direction);
            P0BossDpsTracker.RecordAttackCast(GetSourceId(), target);
            if (ResolveReturningAttackPass(ReturningAttackPass.Outbound) == false)
                return false;

            this.SpawnCanonicalCompanionAttack(_returningAttackStart, direction);
            _returningPassPending = true;
            _returningPassDueTime = currentTime + _returningAttackSetup.TravelDuration;
            return true;
        }

        private bool ResolveReturningAttackPass(ReturningAttackPass pass)
        {
            List<TargetAreaImpactCandidate> candidates = _returningAttackCandidates;
            candidates.Clear();
            foreach (MonsterController target in _party.Registry.Enemies)
            {
                if (target == null || target.IsValid() == false)
                    continue;

                candidates.Add(new TargetAreaImpactCandidate(
                    target,
                    AllyTargeting.ResolveTargetPoint(target, _returningAttackStart),
                    target.GetInstanceID()));
            }

            CompanionReturningAttackTargetSelector.Collect(
                candidates,
                _returningAttackStart,
                _returningAttackEnd,
                _returningAttackSetup.Width,
                _returningAttackSetup.MaxTargetsPerPass,
                pass,
                _returningAttackTargets);
            bool resolved = false;
            for (int index = 0; index < _returningAttackTargets.Count; index += 1)
            {
                TargetAreaImpactCandidate candidate = _returningAttackTargets[index];
                if (candidate.Target == null
                    || candidate.Target.IsValid() == false
                    || _returningAttackHitLedger.TryRecord(candidate.InstanceId, pass) == false)
                {
                    continue;
                }

                this.TryDamageTarget(
                    candidate.Target,
                    _returningAttackSetup.Damage,
                    AttackVisualKind.SingleHit,
                    spawnHitVisual: false);
                resolved = true;
            }

            if (pass == ReturningAttackPass.Return)
                this.SpawnCanonicalCompanionAttack(_returningAttackEnd, _returningAttackStart - _returningAttackEnd);
            return resolved;
        }

        public void SetCanonicalReturningAttackInfo(CompanionReturningAttackCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _returningAttackSetup = setup;
            _returningAttackSchedule = new CombatAbilitySchedule();
            _returningAttackSchedule.Configure(
                setup.Period,
                setup.NoTargetRetrySeconds,
                Time.time,
                UnityEngine.Random.Range(0.1f, 0.35f));
            _attackStyle = AllyAttackStyle.TargetedProjectile;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = setup.Range;
            _sourceIdOverride = setup.SourceId;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds;
            _returningPassPending = false;
            _nextAttackTime = float.PositiveInfinity;
        }
    }
}
