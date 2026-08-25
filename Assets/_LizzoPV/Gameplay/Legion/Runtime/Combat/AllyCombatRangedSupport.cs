using Lizzo.PV.Legion.Combat.Attacks;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        internal bool HealCommander()
        {
            return ClericHealAttack.TryResolve(_party, _damage);
        }

        internal bool AttackCanonicalRangedSupportHeal()
        {
            return ClericHealAttack.TryResolveNoRevive(
                _party,
                transform.position,
                SecondaryHealAmount,
                SecondaryHealRange,
                SecondaryHealMaxTargets,
                SecondaryHealSecondTargetRatio,
                _supportHealTargets);
        }

        public void SetCanonicalRangedSupportInfo(CompanionRangedSupportCombatSetup setup)
        {
            _promotedProjectileBounce = default;
            _attackStyle = setup.Primary.AttackStyle;
            _damage = setup.Primary.Damage;
            _period = setup.Primary.Period;
            _range = Mathf.Max(setup.Primary.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = Mathf.Max(1, setup.Primary.MaxTargets);
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.Primary.NoTargetRetrySeconds);
            _sourceIdOverride = setup.Primary.SourceId;
            _projectileSpeedMultiplier = setup.Primary.ProjectileSpeedMultiplier;
            _secondaryHealAmount = setup.SecondaryHealAmount;
            _secondaryHealPeriod = setup.SecondaryPeriod;
            _secondaryHealRange = Mathf.Max(setup.SecondaryRange, MIN_ATTACK_RANGE);
            _secondaryHealMaxTargets = Mathf.Max(1, setup.SecondaryMaxTargets);
            _secondaryHealSecondTargetRatio = Mathf.Clamp01(setup.SecondarySecondTargetRatio);
            _secondaryHealPeriodScalesWithGrowth = setup.SecondaryPeriodScalesWithGrowth;
            float now = Time.time;
            _primaryAbilitySchedule = new CombatAbilitySchedule();
            _secondaryAbilitySchedule = new CombatAbilitySchedule();
            _primaryAbilitySchedule.Configure(_period, _noTargetRetrySeconds, now, UnityEngine.Random.Range(0.1f, 0.35f));
            _secondaryAbilitySchedule.Configure(
                setup.SecondaryPeriod,
                setup.SecondaryNoTargetRetrySeconds,
                now,
                UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }
    }
}
