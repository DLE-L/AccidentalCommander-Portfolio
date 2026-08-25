using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public sealed partial class UnitVisualDriver
    {
        private bool _isForwardingLinkedState;

        private void ForwardMoving(bool isMoving)
        {
            if (_isForwardingLinkedState || _linkedDrivers == null)
                return;

            _isForwardingLinkedState = true;
            try
            {
                for (int i = 0; i < _linkedDrivers.Length; i++)
                {
                    UnitVisualDriver linked = _linkedDrivers[i];
                    if (linked != null && linked != this)
                        linked.SetMoving(isMoving);
                }
            }
            finally
            {
                _isForwardingLinkedState = false;
            }
        }

        private void ForwardDead(bool isDead)
        {
            if (_isForwardingLinkedState || _linkedDrivers == null)
                return;

            _isForwardingLinkedState = true;
            try
            {
                for (int i = 0; i < _linkedDrivers.Length; i++)
                {
                    UnitVisualDriver linked = _linkedDrivers[i];
                    if (linked != null && linked != this)
                        linked.SetDead(isDead);
                }
            }
            finally
            {
                _isForwardingLinkedState = false;
            }
        }

        private void ForwardFacing(Vector3 worldDirection)
        {
            if (_isForwardingLinkedState || _linkedDrivers == null)
                return;

            _isForwardingLinkedState = true;
            try
            {
                for (int i = 0; i < _linkedDrivers.Length; i++)
                {
                    UnitVisualDriver linked = _linkedDrivers[i];
                    if (linked != null && linked != this)
                        linked.FaceDirection(worldDirection);
                }
            }
            finally
            {
                _isForwardingLinkedState = false;
            }
        }

        private void ForwardAttack(Vector3 worldDirection, float holdSeconds)
        {
            if (_isForwardingLinkedState || _linkedDrivers == null)
                return;

            _isForwardingLinkedState = true;
            try
            {
                for (int i = 0; i < _linkedDrivers.Length; i++)
                {
                    UnitVisualDriver linked = _linkedDrivers[i];
                    if (linked != null && linked != this)
                        linked.PlayAttack(worldDirection, holdSeconds);
                }
            }
            finally
            {
                _isForwardingLinkedState = false;
            }
        }

    }
}
