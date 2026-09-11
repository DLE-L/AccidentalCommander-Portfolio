using System;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public sealed partial class UnitVisualDriver
    {
        private PersonalSummonRuntime _summon;

        private void BindSummon()
        {
            UnbindSummon();
            if (_summon == null) _summon = GetComponentInParent<PersonalSummonRuntime>();
            if (_summon == null) return;
            _summon.FacingChanged += OnSummonFacing;
            _summon.AttackExecuted += OnSummonAttack;
            _summon.MovingChanged += OnSummonMoving;
        }

        private void OnDisable() => UnbindSummon();

        private void UnbindSummon()
        {
            if (_summon == null) return;
            _summon.FacingChanged -= OnSummonFacing;
            _summon.AttackExecuted -= OnSummonAttack;
            _summon.MovingChanged -= OnSummonMoving;
        }

        private void OnSummonFacing(Vector3 direction)
        {
            try { FaceDirection(direction); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
        private void OnSummonAttack(Vector3 direction)
        {
            try { PlayAttack(direction); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
        private void OnSummonMoving(bool moving)
        {
            try { SetMoving(moving); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }
}
