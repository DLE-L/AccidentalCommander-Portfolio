using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CompanionMemberView : MonoBehaviour
    {
        [SerializeField]
        private int _memberOrder;
        [SerializeField]
        private bool _promotedLeaderVisual;
        [SerializeField]
        private UnitVisualDriver _visualDriver;

        public int MemberOrder => _memberOrder;

        public bool IsPromotedLeaderVisual => _promotedLeaderVisual;

        public UnitVisualDriver VisualDriver => _visualDriver;

        internal void ApplyPose(
            in CompanionPoint localPosition,
            bool isMoving,
            in CompanionPoint facingDirection)
        {
            transform.localPosition = new Vector3(localPosition.X, localPosition.Y, 0.0f);
            if (_visualDriver == null)
            {
                return;
            }

            _visualDriver.SetMoving(isMoving);
            if (facingDirection.X != 0.0f || facingDirection.Y != 0.0f)
            {
                _visualDriver.FaceDirection(new Vector3(facingDirection.X, facingDirection.Y, 0.0f));
            }
        }

        internal void SetPresentationActive(bool isActive)
        {
            if (!isActive && _visualDriver != null)
            {
                _visualDriver.SetMoving(false);
            }

            if (gameObject.activeSelf != isActive)
            {
                gameObject.SetActive(isActive);
            }
        }

        internal void PlayAttack(in CompanionPoint facingDirection, float holdSeconds)
        {
            if (_visualDriver == null)
            {
                return;
            }

            _visualDriver.PlayAttack(
                new Vector3(facingDirection.X, facingDirection.Y, 0.0f),
                holdSeconds);
        }
    }
}
