using UnityEngine;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        public void SetInfo(AllyAttackStyle attackStyle, int damage, float period, float range, float knockback, float angle = 60.0f)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = attackStyle;
            _damage = damage;
            _period = period;
            _range = Mathf.Max(range, MIN_ATTACK_RANGE);
            _knockback = knockback;
            _angle = angle;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = NO_TARGET_RETRY_DELAY;
            _sourceIdOverride = null;
            _projectileSpeedMultiplier = 1.0f;
            _targetRule = CombatTargetRule.Nearest;
            _nextAttackTime = Time.time + Random.Range(0.1f, 0.35f);
        }

        public void ApplyGrowthScale(CompanionGrowthScale scale)
        {
            if (_chainAbilitySchedule != null)
            {
                _chainSetup = _chainSetup.WithGrowthScale(scale);
                _damage = _chainSetup.Damage;
                _period = _chainSetup.Period;
                _chainAbilitySchedule.ApplyIntervalMultiplier(scale.IntervalMultiplier);
                return;
            }

            if (_persistentFieldAbilitySchedule != null)
            {
                _persistentFieldSetup = _persistentFieldSetup.WithGrowthScale(scale);
                _damage = _persistentFieldSetup.Damage;
                _period = _persistentFieldSetup.Period;
                _persistentFieldAbilitySchedule.ApplyIntervalMultiplier(scale.IntervalMultiplier);
                return;
            }

            _damage = Mathf.Max(1, Mathf.RoundToInt(_damage * scale.EffectMultiplier));
            _period = Mathf.Max(0.01f, _period * scale.IntervalMultiplier);
            _secondaryHealAmount = _secondaryHealAmount > 0
                ? Mathf.Max(1, Mathf.RoundToInt(_secondaryHealAmount * scale.EffectMultiplier))
                : 0;
            if (_secondaryHealPeriodScalesWithGrowth)
            {
                _secondaryHealPeriod = Mathf.Max(0.01f, _secondaryHealPeriod * scale.IntervalMultiplier);
                _secondaryAbilitySchedule?.ApplyIntervalMultiplier(scale.IntervalMultiplier);
            }
        }

        private void ClearCanonicalAbilitySchedules()
        {
            _primaryAbilitySchedule = null;
            _secondaryAbilitySchedule = null;
            _targetAreaCastState = null;
            _persistentFieldAbilitySchedule = null;
            _chainAbilitySchedule = null;
            _persistentFieldSetup = default;
            _chainSetup = default;
            _ownedProxyCounter = null;
            _ownedProxySetup = default;
            _secondaryHealAmount = 0;
            _secondaryHealPeriod = 0.0f;
            _secondaryHealRange = 0.0f;
            _secondaryHealMaxTargets = 0;
            _secondaryHealSecondTargetRatio = 0.0f;
            _secondaryHealPeriodScalesWithGrowth = true;
            _supportHealTargets.Clear();
            _targetAreaRadius = 0.0f;
            _targetAreaMaxTargets = 0;
            _targetAreaNormalPush = 0.0f;
            _targetAreaEliteBossPush = 0.0f;
            _targetRule = CombatTargetRule.Invalid;
            _promotedMultiHitSequence = null;
            _promotedProjectileBurst = null;
            _promotedProjectileBounce = default;
        }

    }
}
