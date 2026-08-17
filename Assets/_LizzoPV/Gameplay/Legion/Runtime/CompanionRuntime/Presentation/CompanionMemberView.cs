using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CompanionMemberView : MonoBehaviour
    {
        private const float IdleBobAmplitude = 0.012f;
        private const float IdleMotionFrequency = 1.60f;

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
            in CompanionPoint facingDirection,
            bool allowIdleMotion)
        {
            Vector3 proceduralOffset = isMoving || !allowIdleMotion || !Application.isPlaying
                ? Vector3.zero
                : ResolveIdleOffset();
            transform.localPosition = new Vector3(localPosition.X, localPosition.Y, 0.0f) + proceduralOffset;
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

        private Vector3 ResolveIdleOffset()
        {
            float phase = (_memberOrder + 1) * 1.73f;
            float time = Time.time * IdleMotionFrequency;
            float bob = Mathf.Sin(time + phase) * IdleBobAmplitude;
            return new Vector3(0.0f, bob, 0.0f);
        }
    }
}
