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
                MonsterController target = this.FindNearestMonster(setup.SearchRange);
                if (target == null) { _nextAttackTime = currentTime + setup.NoTargetRetrySeconds; return; }
                this.FaceTarget(target);
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
                if (locked != null && locked.IsValid()) this.TryDamageTarget(locked, setup.ResolvePerHitDamage(), AttackVisualKind.SingleHit, false);
            }
            if (_wolfState.IsActive == false) _nextAttackTime = currentTime + setup.Period / ResolveAttackIntervalDivisor();
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
