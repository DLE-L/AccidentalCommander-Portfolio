using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    [DefaultExecutionOrder(20)]
    public abstract class UnitVisualRole : MonoBehaviour
    {
        private const float MOVE_DELTA_THRESHOLD = 0.01f;

        [SerializeField]
        private UnitVisualDriver _driver;

        private Vector3 _lastPosition;
        private bool _hasLastPosition;

        protected UnitVisualDriver Driver
        {
            get
            {
                EnsureDriver();
                return _driver;
            }
        }

        protected virtual void Awake()
        {
            EnsureDriver();
        }

        protected virtual void OnEnable()
        {
            EnsureDriver();
            _lastPosition = transform.position;
            _hasLastPosition = true;
        }

        protected bool TryConsumeMoveDirection(out Vector3 direction)
        {
            Vector3 currentPosition = transform.position;
            Vector3 delta = _hasLastPosition ? currentPosition - _lastPosition : Vector3.zero;
            _lastPosition = currentPosition;
            _hasLastPosition = true;

            if (delta.sqrMagnitude <= MOVE_DELTA_THRESHOLD * MOVE_DELTA_THRESHOLD)
            {
                direction = Vector3.zero;
                return false;
            }

            direction = delta.normalized;
            return true;
        }

        protected void ApplyMovingState(bool isMoving, Vector3 moveDirection, bool allowRunState)
        {
            UnitVisualDriver driver = Driver;
            if (driver == null)
                return;

            driver.SetMoving(allowRunState && isMoving);
            if (isMoving)
                driver.FaceDirection(moveDirection);
        }

        protected void SetDead(bool isDead)
        {
            Driver?.SetDead(isDead);
        }

        public virtual void FaceDirection(Vector3 worldDirection)
        {
            Driver?.FaceDirection(worldDirection);
        }

        public virtual void PlayAttack(Vector3 worldDirection, float holdSeconds = -1.0f)
        {
            Driver?.PlayAttack(worldDirection, holdSeconds);
        }

        public virtual void CancelAttack()
        {
            Driver?.CancelAttack();
        }

        private void EnsureDriver()
        {
            if (_driver != null)
                return;

            _driver = GetComponentInChildren<UnitVisualDriver>(true);
            if (_driver == null)
                Debug.LogError($"P0 unit prefab is missing required UnitVisualDriver: {gameObject.name}", this);
        }
    }
}
