using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        public void SetDown(bool isDown)
        {
            _isDown = isDown;

            if (isDown)
            {
                _ownedProxyCounter?.Reset();
                _wolfState?.Reset();
                _personalMitigation?.ResetForOwnerDown(Time.time);
                if (_runtime != null)
                    _runtime.IncomingDamageMultiplier = 1.0f;
                _nextAttackTime = float.PositiveInfinity;
                return;
            }

            if (_targetAreaCastState != null)
            {
                _targetAreaCastState.Restart(Time.time, Random.Range(0.15f, 0.35f));
                return;
            }

            if (_persistentFieldAbilitySchedule != null)
            {
                _persistentFieldAbilitySchedule.Restart(Time.time, Random.Range(0.15f, 0.35f));
                return;
            }

            if (_chainAbilitySchedule != null)
            {
                _chainAbilitySchedule.Restart(Time.time, Random.Range(0.15f, 0.35f));
                return;
            }

            if (_primaryAbilitySchedule != null)
            {
                float restartDelay = Random.Range(0.15f, 0.35f);
                _primaryAbilitySchedule.Restart(Time.time, restartDelay);
                _secondaryAbilitySchedule.Restart(Time.time, restartDelay);
                return;
            }

            _nextAttackTime = Time.time + Random.Range(0.15f, 0.35f);
        }

    }
}
