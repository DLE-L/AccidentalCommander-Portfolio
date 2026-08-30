using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Combat;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        public float ResolveNextAttackDelay(bool didAttack)
        {
            return didAttack ? _period / ResolveAttackIntervalDivisor() : _noTargetRetrySeconds;
        }

        internal float ResolveAttackIntervalDivisor()
        {
            float divisor = _party == null ? 1.0f : _party.ResolveCompanionAttackIntervalDivisor(GetRuntime());
            return divisor > 0.0f ? divisor : 1.0f;
        }

        private void Update()
        {
            this.AdvanceCanonicalCombat(Time.time);
        }

#if UNITY_EDITOR
        public void TryAdvanceCanonicalCastForTests(float currentTime)
#else
        internal void TryAdvanceCanonicalCastForTests(float currentTime)
#endif
        {
            this.AdvanceCanonicalCombat(currentTime);
        }

        internal void AdvanceCanonicalCombat(float currentTime)
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            if (_isDown || IsRuntimeDown())
                return;

            bool movementAllowsAction = this.UpdateCanonicalMeleeMovement(currentTime);

            if (_primaryReturnHealPending && currentTime >= _primaryReturnHealDueTime)
            {
                _primaryReturnHealPending = false;
                ResolveReturningLightHeal();
            }

            if (_personalMitigation != null)
            {
                _personalMitigation.Advance(currentTime);
                GetRuntime().IncomingDamageMultiplier = _personalMitigation.IncomingDamageMultiplier;
            }

            if (_wolfState != null)
            {
                this.UpdateCanonicalWolfOwnedProxy(currentTime);
                return;
            }

            if (_returningAttackSchedule != null)
            {
                this.UpdateCanonicalReturningAttack(currentTime);
                return;
            }

            if (_targetAreaCastState != null)
            {
                this.UpdateCanonicalTargetArea(currentTime);
                return;
            }

            if (_persistentFieldAbilitySchedule != null)
            {
                if (_persistentFieldAbilitySchedule.IsDue(currentTime))
                {
                    bool resolved = this.SpawnCanonicalPersistentField(currentTime);
                    _persistentFieldAbilitySchedule.RecordResolution(
                        currentTime,
                        resolved,
                        resolved ? ResolveAttackIntervalDivisor() : 1.0f);
                    if (resolved)
                        _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);
                }

                return;
            }

            if (_chainAbilitySchedule != null)
            {
                if (_chainAbilitySchedule.IsDue(currentTime))
                {
                    bool resolved = this.AttackCanonicalChain();
                    _chainAbilitySchedule.RecordResolution(
                        currentTime,
                        resolved,
                        resolved ? ResolveAttackIntervalDivisor() : 1.0f);
                    if (resolved)
                        _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);
                }

                return;
            }

            if (_primaryAbilitySchedule != null)
            {
                if (_primaryAbilitySchedule.IsDue(currentTime))
                {
                    bool resolved = this.AttackTargetedProjectile(currentTime);
                    _primaryAbilitySchedule.RecordResolution(
                        currentTime,
                        resolved,
                        resolved ? ResolveAttackIntervalDivisor() : 1.0f);
                    if (resolved)
                        _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);
                }

                if (_secondaryAbilitySchedule != null && _secondaryAbilitySchedule.IsDue(currentTime))
                {
                    bool resolved = this.AttackCanonicalRangedSupportHeal();
                    _secondaryAbilitySchedule.RecordResolution(
                        currentTime,
                        resolved,
                        resolved ? ResolveAttackIntervalDivisor() : 1.0f);
                    if (resolved)
                        _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.ActiveSkill);
                }

                return;
            }

            if (currentTime < _nextAttackTime)
                return;

            if (_meleeMovement.IsConfigured && movementAllowsAction == false)
                return;

            bool didAttack = _attackStyle switch
            {
                AllyAttackStyle.SingleTarget => this.AttackNearest(),
                AllyAttackStyle.FarthestTarget => this.AttackFarthest(),
                AllyAttackStyle.TargetedProjectile => this.AttackTargetedProjectile(),
                AllyAttackStyle.ForwardSlash => this.AttackForwardSlash(),
                AllyAttackStyle.ForwardPush => this.AttackForwardPush(),
                AllyAttackStyle.AreaPulse => this.AttackArea(),
                AllyAttackStyle.HealCommander => this.HealCommander(),
                _ => false,
            };

            _nextAttackTime = currentTime + ResolveNextAttackDelay(didAttack);
            if (didAttack)
            {
                BeginMeleeReturn();
                _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);
            }
        }

    }
}
