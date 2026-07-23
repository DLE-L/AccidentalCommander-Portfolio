using UnityEngine;
using Lizzo.PV.Flow;

namespace Lizzo.PV.Legion
{
    [DefaultExecutionOrder(10)]
    public sealed class AllyFollower : MonoBehaviour
    {
        private const float ARRIVAL_STOP_DISTANCE = 0.035f;
        private const float FORMATION_REASSIGN_GRACE_SECONDS = 0.25f;
        private const float FORMATION_REASSIGN_JUMP_DISTANCE = 0.75f;
        private const float FORMATION_REASSIGN_SPEED_MULTIPLIER = 0.75f;
        private const float FORMATION_REASSIGN_MAX_STEP = 0.16f;
        private const float COMMANDER_CLEAR_RADIUS = 0.72f;
        private const float COMMANDER_CLEAR_SPEED_MULTIPLIER = 1.75f;

        private Transform _target;
        private Vector3 _offset;
        private float _followSpeed = 8.0f;
        private bool _useDirectionalOffset;
        private string _slotId = string.Empty;
        private Rigidbody2D _body;
        private Vector3 _lastResolvedTargetPosition;
        private float _formationReassignSlowUntil = -999.0f;
        private bool _hasResolvedTargetPosition;
        private PartyService _party;

        public void BindParty(PartyService party)
        {
            _party = party ?? throw new System.ArgumentNullException(nameof(party));
        }

        public string SlotId => _slotId;

        private void Awake()
        {
            CacheRequiredBody();
        }

        private void OnEnable()
        {
            CacheRequiredBody();
        }

        public void SetTarget(Transform target, Vector3 offset)
        {
            SetTarget(target, offset, _followSpeed);
        }

        public void SetTarget(Transform target, Vector3 offset, float followSpeed)
        {
            _target = target;
            _offset = offset;
            _followSpeed = followSpeed;
            _useDirectionalOffset = false;
        }

        public void SetDirectionalTarget(Transform target, Vector3 localOffset, float followSpeed)
        {
            SetDirectionalTarget(target, localOffset, followSpeed, string.Empty);
        }

        public void SetDirectionalTarget(Transform target, Vector3 localOffset, float followSpeed, string slotId)
        {
            bool slotChanged = string.IsNullOrEmpty(_slotId) == false && _slotId != slotId;
            if (_target == target && _useDirectionalOffset && slotChanged)
                BeginFormationReassignGrace();

            _target = target;
            _offset = localOffset;
            _followSpeed = followSpeed;
            _useDirectionalOffset = true;
            _slotId = slotId;
        }

        private void FixedUpdate()
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            if (_target == null)
                return;

            if (_body == null)
            {
                CacheRequiredBody();
                return;
            }

            Vector3 targetPosition = _useDirectionalOffset
                ? _target.position + _party.Formation.ResolveWorldOffset(_offset, _slotId)
                : _target.position + _offset;
            if (_hasResolvedTargetPosition
                && (targetPosition - _lastResolvedTargetPosition).sqrMagnitude >= FORMATION_REASSIGN_JUMP_DISTANCE * FORMATION_REASSIGN_JUMP_DISTANCE)
            {
                BeginFormationReassignGrace();
            }

            _lastResolvedTargetPosition = targetPosition;
            _hasResolvedTargetPosition = true;

            targetPosition = _party.Formation.ApplyReadabilityGuards(this, targetPosition);

            Vector2 currentPosition = _body.position;
            Vector2 targetPosition2D = targetPosition;
            Vector2 delta = targetPosition2D - currentPosition;
            if (delta.sqrMagnitude <= ARRIVAL_STOP_DISTANCE * ARRIVAL_STOP_DISTANCE)
            {
                _body.linearVelocity = Vector2.zero;
                _body.angularVelocity = 0.0f;
                return;
            }

            bool reassignGraceActive = IsFormationReassignGraceActive();
            bool commanderClearPriority = ShouldPrioritizeCommanderClear(currentPosition, targetPosition2D);
            float effectiveFollowSpeed = reassignGraceActive && commanderClearPriority == false
                ? _followSpeed * FORMATION_REASSIGN_SPEED_MULTIPLIER
                : _followSpeed;
            if (commanderClearPriority)
                effectiveFollowSpeed *= COMMANDER_CLEAR_SPEED_MULTIPLIER;

            Vector2 nextPosition = Vector2.Lerp(currentPosition, targetPosition2D, Mathf.Clamp01(effectiveFollowSpeed * Time.fixedDeltaTime));
            if (reassignGraceActive && commanderClearPriority == false)
            {
                Vector2 step = nextPosition - currentPosition;
                float maxStepSqr = FORMATION_REASSIGN_MAX_STEP * FORMATION_REASSIGN_MAX_STEP;
                if (step.sqrMagnitude > maxStepSqr)
                    nextPosition = currentPosition + step.normalized * FORMATION_REASSIGN_MAX_STEP;
            }

            _body.MovePosition(nextPosition);
        }

        private bool ShouldPrioritizeCommanderClear(Vector2 currentPosition, Vector2 targetPosition)
        {
            PlayerController player = _party.Registry?.Player;
            if (player == null)
                return false;

            Vector2 commanderPosition = player.transform.position;
            Vector2 currentDelta = currentPosition - commanderPosition;
            float clearRadiusSqr = COMMANDER_CLEAR_RADIUS * COMMANDER_CLEAR_RADIUS;
            if (currentDelta.sqrMagnitude >= clearRadiusSqr)
                return false;

            Vector2 targetDelta = targetPosition - commanderPosition;
            return targetDelta.sqrMagnitude > currentDelta.sqrMagnitude;
        }

        private void BeginFormationReassignGrace()
        {
            _formationReassignSlowUntil = Mathf.Max(_formationReassignSlowUntil, Time.time + FORMATION_REASSIGN_GRACE_SECONDS);
        }

        private bool IsFormationReassignGraceActive()
        {
            return Time.time < _formationReassignSlowUntil;
        }

        private void CacheRequiredBody()
        {
            if (_body != null)
                return;

            _body = GetComponent<Rigidbody2D>();
            if (_body == null)
            {
                Debug.LogError($"Companion prefab is missing required Rigidbody2D: {gameObject.name}", this);
                return;
            }
        }
    }
}
