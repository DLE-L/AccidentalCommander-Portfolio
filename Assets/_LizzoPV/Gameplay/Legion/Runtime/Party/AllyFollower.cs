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
        private bool _synergyExternalMovement;
        private CompanionRuntime _companion;
        private bool _combatDestinationActive;
        private Vector2 _combatDestination;
        private float _combatMoveSpeed;
        private float _returnMoveSpeedOverride;
        private Vector2 _resolvedCombatDestination;
        private bool _hasResolvedCombatDestination;

        public void BindParty(PartyService party)
        {
            _party = party ?? throw new System.ArgumentNullException(nameof(party));
        }

        public string SlotId => _slotId;
        public Vector3 FormationLocalOffset => _offset;
        public bool HasCombatDestination => _combatDestinationActive;

        internal Vector3 ResolveFormationTargetPosition()
        {
            if (_target == null)
                return transform.position;

            return _useDirectionalOffset
                ? _target.position + _party.Formation.ResolveWorldOffset(_offset, _slotId)
                : _target.position + _offset;
        }

        internal bool IsAtFormationTarget(float tolerance = 0.10f)
        {
            Vector2 delta = (Vector2)ResolveFormationTargetPosition() - ResolveCurrentPosition();
            float safeTolerance = Mathf.Max(0.0f, tolerance);
            return delta.sqrMagnitude <= safeTolerance * safeTolerance;
        }

        internal bool IsAtCombatDestination(float tolerance = 0.10f)
        {
            if (_combatDestinationActive == false || _hasResolvedCombatDestination == false)
                return false;

            Vector2 delta = _resolvedCombatDestination - ResolveCurrentPosition();
            float safeTolerance = Mathf.Max(0.0f, tolerance);
            return delta.sqrMagnitude <= safeTolerance * safeTolerance;
        }

        internal void SetCombatDestination(Vector2 targetPosition, float moveSpeed)
        {
            if (_combatDestinationActive == false
                || (_combatDestination - targetPosition).sqrMagnitude > 0.0001f)
            {
                _hasResolvedCombatDestination = false;
            }

            _combatDestination = targetPosition;
            _combatMoveSpeed = Mathf.Max(0.0f, moveSpeed);
            _combatDestinationActive = true;
            _returnMoveSpeedOverride = 0.0f;
        }

        internal void ClearCombatDestination(float returnMoveSpeed)
        {
            if (_combatDestinationActive)
                _returnMoveSpeedOverride = Mathf.Max(0.0f, returnMoveSpeed);
            _combatDestinationActive = false;
            _combatMoveSpeed = 0.0f;
            _hasResolvedCombatDestination = false;
        }

        internal void SetSynergyExternalMovement(bool active)
        {
            _synergyExternalMovement = active;
            if (active && _body != null) _body.linearVelocity = Vector2.zero;
        }

        internal bool TryMoveSynergyExternal(Vector2 targetPosition)
        {
            if (_body == null) CacheRequiredBody();
            if (_body == null) return false;
            _body.MovePosition(_party.Formation.ClampFriendlyActor(targetPosition));
            return true;
        }

        private void Awake()
        {
            CacheRequiredBody();
            CacheCompanion();
        }

        private void OnEnable()
        {
            CacheRequiredBody();
            CacheCompanion();
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

            if (_synergyExternalMovement)
                return;

            if (_target == null)
                return;

            if (_body == null)
            {
                CacheRequiredBody();
                return;
            }

            bool combatDestinationActive = _combatDestinationActive;
            Vector3 targetPosition = combatDestinationActive
                ? (Vector3)_combatDestination
                : ResolveFormationTargetPosition();
            if (combatDestinationActive == false
                && _hasResolvedTargetPosition
                && (targetPosition - _lastResolvedTargetPosition).sqrMagnitude >= FORMATION_REASSIGN_JUMP_DISTANCE * FORMATION_REASSIGN_JUMP_DISTANCE)
            {
                BeginFormationReassignGrace();
            }

            _lastResolvedTargetPosition = targetPosition;
            _hasResolvedTargetPosition = true;

            targetPosition = _party.Formation.ApplyReadabilityGuards(this, targetPosition);
            targetPosition = _party.Formation.ClampFriendlyActor(targetPosition);
            if (combatDestinationActive)
            {
                _resolvedCombatDestination = targetPosition;
                _hasResolvedCombatDestination = true;
            }

            Vector2 currentPosition = _body.position;
            Vector2 targetPosition2D = targetPosition;
            Vector2 delta = targetPosition2D - currentPosition;
            if (delta.sqrMagnitude <= ARRIVAL_STOP_DISTANCE * ARRIVAL_STOP_DISTANCE)
            {
                _body.linearVelocity = Vector2.zero;
                _body.angularVelocity = 0.0f;
                if (combatDestinationActive == false)
                    _returnMoveSpeedOverride = 0.0f;
                return;
            }

            bool reassignGraceActive = combatDestinationActive == false && IsFormationReassignGraceActive();
            bool commanderClearPriority = ShouldPrioritizeCommanderClear(currentPosition, targetPosition2D);
            float requestedSpeed = combatDestinationActive
                ? _combatMoveSpeed
                : _returnMoveSpeedOverride > 0.0f
                    ? _returnMoveSpeedOverride
                    : _followSpeed;
            float effectiveFollowSpeed = reassignGraceActive && commanderClearPriority == false
                ? requestedSpeed * FORMATION_REASSIGN_SPEED_MULTIPLIER
                : requestedSpeed;
            if (commanderClearPriority)
                effectiveFollowSpeed *= COMMANDER_CLEAR_SPEED_MULTIPLIER;
            effectiveFollowSpeed *= _party.ResolveCompanionMoveSpeedMultiplier(_companion);

            Vector2 nextPosition = ResolveFollowStep(
                currentPosition,
                targetPosition2D,
                effectiveFollowSpeed,
                Time.fixedDeltaTime);
            if (reassignGraceActive && commanderClearPriority == false)
            {
                Vector2 step = nextPosition - currentPosition;
                float maxStepSqr = FORMATION_REASSIGN_MAX_STEP * FORMATION_REASSIGN_MAX_STEP;
                if (step.sqrMagnitude > maxStepSqr)
                    nextPosition = currentPosition + step.normalized * FORMATION_REASSIGN_MAX_STEP;
            }

            nextPosition = _party.Formation.ClampFriendlyActor(nextPosition);
            _body.MovePosition(nextPosition);
        }

        internal static Vector2 ResolveFollowStep(
            Vector2 currentPosition,
            Vector2 targetPosition,
            float followSpeed,
            float fixedDeltaTime)
        {
            float maxDistanceDelta = Mathf.Max(0.0f, followSpeed) * Mathf.Max(0.0f, fixedDeltaTime);
            return Vector2.MoveTowards(currentPosition, targetPosition, maxDistanceDelta);
        }

        public static Vector2 ClampExcursionDestination(
            Vector2 formationPosition,
            Vector2 desiredPosition,
            float maxExcursionDistance)
        {
            float safeDistance = Mathf.Max(0.0f, maxExcursionDistance);
            return formationPosition + Vector2.ClampMagnitude(desiredPosition - formationPosition, safeDistance);
        }

        private Vector2 ResolveCurrentPosition()
        {
            return _body == null ? (Vector2)transform.position : _body.position;
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

        private void CacheCompanion()
        {
            if (_companion == null)
                _companion = GetComponent<CompanionRuntime>();
        }
    }
}
