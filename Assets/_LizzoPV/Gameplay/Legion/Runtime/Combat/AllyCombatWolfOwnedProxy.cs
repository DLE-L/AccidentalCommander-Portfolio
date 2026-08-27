using System.Collections.Generic;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        internal void UpdateCanonicalWolfOwnedProxy(float currentTime)
        {
            CompanionWolfOwnedProxyCombatSetup setup = _wolfSetup;
            if (_wolfState.IsActive == false)
            {
                if (currentTime < _nextAttackTime) return;
                _wolfChainVisitedTargets.Clear();
                MonsterController target = FindLowestHealthWolfTarget(transform.position, setup.SearchRange);
                if (target == null) { _nextAttackTime = currentTime + setup.NoTargetRetrySeconds; return; }
                this.FaceTarget(target);
                _wolfChainVisitedTargets.Add(target.GetInstanceID());
                _wolfState.TryBegin(transform.position, target.transform.position, target.GetInstanceID(), currentTime, setup.Duration, setup.HitCount);
                if (_wolfState.IsActive)
                    this.SpawnCanonicalCompanionAttack(transform.position, target.transform.position - transform.position);
                return;
            }
            _wolfState.Advance(currentTime, out _, out bool consumeHit);
            if (consumeHit)
            {
                MonsterController locked = null;
                foreach (MonsterController target in _party.Registry.Enemies) if (target != null && target.GetInstanceID() == _wolfState.LockedTargetInstanceId) { locked = target; break; }
                if (locked != null && locked.IsValid())
                {
                    Vector3 chainOrigin = locked.transform.position;
                    this.TryDamageTarget(locked, setup.ResolvePerHitDamage(), AttackVisualKind.SingleHit, false);
                    if (locked.IsValid() == false)
                        ResolveWolfExecutionChain(chainOrigin, setup);
                }
            }
            if (_wolfState.IsActive == false) _nextAttackTime = currentTime + setup.Period / ResolveAttackIntervalDivisor();
        }

        private void ResolveWolfExecutionChain(Vector3 chainOrigin, CompanionWolfOwnedProxyCombatSetup setup)
        {
            for (int chainIndex = 1; chainIndex < setup.MaxChainTargets; chainIndex += 1)
            {
                MonsterController target = FindLowestHealthWolfTarget(chainOrigin, setup.ChainRange);
                if (target == null)
                    return;

                int targetId = target.GetInstanceID();
                _wolfChainVisitedTargets.Add(targetId);
                chainOrigin = target.transform.position;
                this.TryDamageTarget(target, setup.ResolvePerHitDamage(), AttackVisualKind.SingleHit, false);
                if (target.IsValid())
                    return;
            }
        }

        private MonsterController FindLowestHealthWolfTarget(Vector3 origin, float range)
        {
            List<TargetAreaImpactCandidate> candidates = _targetAreaCandidates;
            candidates.Clear();
            foreach (MonsterController target in _party.Registry.Enemies)
            {
                if (target.IsValid() == false)
                    continue;

                candidates.Add(new TargetAreaImpactCandidate(
                    target,
                    AllyTargeting.ResolveTargetPoint(target, origin),
                    target.GetInstanceID()));
            }

            return CompanionPrimaryTargetSelector.TrySelectLowestHealth(
                    candidates,
                    origin,
                    range,
                    _wolfChainVisitedTargets,
                    out TargetAreaImpactCandidate selected)
                ? selected.Target
                : null;
        }

        public void SetCanonicalWolfOwnedProxyInfo(CompanionWolfOwnedProxyCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _wolfSetup = setup;
            _wolfState = new WolfOwnedProxyState();
            _attackStyle = AllyAttackStyle.SingleTarget;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = setup.SearchRange;
            _sourceIdOverride = setup.SourceId;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds;
            _nextAttackTime = Time.time;
        }
    }
}
