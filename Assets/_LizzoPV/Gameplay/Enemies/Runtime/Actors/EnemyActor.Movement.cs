using Lizzo.PV.Gameplay.Units;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;

namespace Lizzo.PV.Gameplay.Units
{
public partial class EnemyActor
{
	bool _hasExternalPhysicsOwner;
    bool _pullToPointActive;
    Vector3 _pullDestination;
    internal bool IsPullingToPoint => _pullToPointActive && IsForcedMovementActive;

	void FixedUpdate()
    {
		if (RunPauseController.IsResultGameplayLocked)
			return;

		if (CreatureState == Define.CreatureState.Dead)
			return;
		if (_hasExternalPhysicsOwner)
			return;

		bool pulling = _pullToPointActive && IsForcedMovementActive;
		Vector3 knockbackDelta = ConsumeSmoothKnockbackDelta(Time.fixedDeltaTime);
        if (pulling)
        {
            MoveByDelta(knockbackDelta);
            return;
        }
		if (CreatureState != Define.CreatureState.Moving)
		{
			MoveByDelta(knockbackDelta);
			return;
		}

		CommanderActor pc = Registry.Player;
		if (pc == null)
		{
			MoveByDelta(knockbackDelta);
			return;
		}

		if (_usesExternalMovement)
		{
			MoveByDelta(knockbackDelta);
			return;
		}

		Vector3 dir = pc.transform.position - transform.position;
		if (TryEnterContactAttack())
		{
			MoveByDelta(knockbackDelta);
			return;
		}

		UpdateFacing(dir);
		Vector3 newPos = transform.position + knockbackDelta + dir.normalized * Time.fixedDeltaTime * _speed * CurrentSlowMultiplier;
		if (_body != null)
			_body.MovePosition(newPos);
		else
			transform.position = newPos;


    }

	public void SetExternalMovement(bool usesExternalMovement)
	{
		if (usesExternalMovement == false)
		{
			_hasExternalPhysicsOwner = false;
			ClearExternalAttackRecovery();
			SetExternalAttackPreparationLocked(false);
		}

		_usesExternalMovement = usesExternalMovement;
	}

	internal bool IsExternalAttackRecovering => _externalAttackRecoveryRemaining > 0.0f;
	internal float ExternalAttackRecoverySeconds => _externalAttackRecoverySeconds;
	internal void SetExternalPhysicsOwner(bool active) => _hasExternalPhysicsOwner = active;
	internal Vector3 ConsumeExternalForcedMovement(float deltaSeconds) => ConsumeSmoothKnockbackDelta(deltaSeconds);

	internal EnemyActionInput ReadExternalActionInput()
	{
		CommanderActor player = Registry?.Player;
		if (player == null || player.Hp <= 0) return default;
		bool touching = IsTouchingPlayer(player);
		return new EnemyActionInput(transform.position, player.transform.position, true,
			touching && ContactAttack.IsReady, touching);
	}

	internal void BeginExternalAttackRecovery()
	{
		if (!_usesExternalMovement || _isDead || IsForcedMovementActive || _externalAttackRecoverySeconds <= 0.0f)
			return;
		_externalAttackRecoveryRemaining = _externalAttackRecoverySeconds;
		SetExternalAttackPreparationLocked(true);
	}

	internal bool AdvanceExternalAttackRecovery(float deltaTime)
	{
		if (!IsExternalAttackRecovering)
			return false;
		_externalAttackRecoveryRemaining = Mathf.Max(0.0f, _externalAttackRecoveryRemaining - deltaTime);
		if (!IsExternalAttackRecovering)
			SetExternalAttackPreparationLocked(false);
		return true;
	}

	internal void ClearExternalAttackRecovery()
	{
		if (!IsExternalAttackRecovering)
			return;
		_externalAttackRecoveryRemaining = 0.0f;
		SetExternalAttackPreparationLocked(false);
	}

	internal void SetExternalAttackPreparationLocked(bool locked)
	{
		if (_body == null)
			_body = GetComponent<Rigidbody2D>();

		if (_body == null)
			return;

		if (locked)
		{
			if (_externalAttackPreparationLocked == false)
				_externalAttackPreparationBaseConstraints = _body.constraints;

			_externalAttackPreparationLocked = true;
			_body.linearVelocity = Vector2.zero;
			_body.angularVelocity = 0.0f;
			_body.constraints = _externalAttackPreparationBaseConstraints
				| RigidbodyConstraints2D.FreezePositionX
				| RigidbodyConstraints2D.FreezePositionY;
			return;
		}

		if (_externalAttackPreparationLocked == false)
			return;

		_body.linearVelocity = Vector2.zero;
		_body.angularVelocity = 0.0f;
		_body.constraints = _externalAttackPreparationBaseConstraints;
		_externalAttackPreparationLocked = false;
	}

	public bool TryEnterContactAttack(float windupSeconds = 0f) => ContactAttack.TryEnterContactAttack(windupSeconds);

	public bool TryApplyContactDamageNow(bool recoverAfterAttack = false) => ContactAttack.TryApplyContactDamageNow(recoverAfterAttack);

	public void UpdateExternalMoveFacing(Vector3 direction)
	{
		UpdateFacing(direction);
	}

	void ResetSpriteRenderers()
	{
		CacheResettableSpriteRenderers();
		for (int i = 0; i < _resettableSpriteRenderers.Length; i++)
		{
			if (_resettableSpriteRenderers[i] == null)
				continue;

			_resettableSpriteRenderers[i].color = Color.white;
			_resettableSpriteRenderers[i].flipX = false;
		}
	}

	void UpdateFacing(Vector3 direction)
	{
		if (direction.sqrMagnitude <= 0.001f)
			return;

		_lastFacingVector = direction.normalized;

	}



	Vector3 ResolveInitialFacingVector()
	{
		CommanderActor player = Registry.Player;
		if (player == null)
			return Vector3.down;

		Vector3 direction = player.transform.position - transform.position;
		if (direction.sqrMagnitude <= 0.001f)
			return Vector3.down;

		return direction.normalized;
	}
    public void ApplySmoothKnockback(Vector3 direction, float distance, float duration = 0.16f)
    {
        if (_isDead || IsBoss || distance <= 0.0f || direction.sqrMagnitude <= 0.0001f)
            return;

        if (IsForcedMovementActive)
            return;

        Rigidbody2D resistanceBody = _body ?? GetComponent<Rigidbody2D>();
        float resistanceMass = resistanceBody == null ? 1.0f : Mathf.Max(1.0f, resistanceBody.mass);
        BeginSmoothDisplacement(direction, distance / resistanceMass, duration);
        _pullToPointActive = false;
    }

    public void ApplySmoothPullToPoint(Vector3 destination, float duration = 0.16f)
    {
        if (_isDead || IsBoss || IsForcedMovementActive) return;
        Vector3 position = _body != null ? (Vector3)_body.position : transform.position;
        destination.z = position.z;
        Vector3 direction = destination - position;
        if (direction.sqrMagnitude <= 0.0001f) return;
        BeginSmoothDisplacement(direction, direction.magnitude, duration);
        _pullDestination = destination;
        _pullToPointActive = true;
    }

    void BeginSmoothDisplacement(Vector3 direction, float distance, float duration)
    {
        ClearExternalAttackRecovery();
        if (_hasExternalPhysicsOwner)
            SetExternalAttackPreparationLocked(false);
        _smoothKnockbackDirection = direction.normalized;
        _smoothKnockbackRemainingDistance = distance;

        _smoothKnockbackDuration = Mathf.Max(MIN_SMOOTH_KNOCKBACK_DURATION, duration);
        _smoothKnockbackElapsed = 0.0f;
    }

    Vector3 ConsumeSmoothKnockbackDelta(float deltaTime)
    {
        if (_smoothKnockbackRemainingDistance <= 0.0f || _smoothKnockbackDirection.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        float remainingDuration = Mathf.Max(0.001f, _smoothKnockbackDuration - _smoothKnockbackElapsed);
        if (_pullToPointActive)
        {
            Vector3 position = _body != null ? (Vector3)_body.position : transform.position;
            Vector3 toDestination = _pullDestination - position;
            _smoothKnockbackDirection = toDestination.normalized;
            _smoothKnockbackRemainingDistance = toDestination.magnitude;
        }
        float stepRatio = Mathf.Clamp01(deltaTime / remainingDuration);
        float moveDistance = _smoothKnockbackRemainingDistance * stepRatio;
        Vector3 delta = _smoothKnockbackDirection * moveDistance;
        _smoothKnockbackElapsed += deltaTime;
        _smoothKnockbackRemainingDistance = Mathf.Max(0.0f, _smoothKnockbackRemainingDistance - moveDistance);

        if (_smoothKnockbackRemainingDistance <= 0.001f
            || (_pullToPointActive && _smoothKnockbackElapsed >= _smoothKnockbackDuration))
        {
            _pullToPointActive = false;
            _smoothKnockbackDirection = Vector3.zero;
            _smoothKnockbackRemainingDistance = 0.0f;
            _smoothKnockbackDuration = 0.0f;
            _smoothKnockbackElapsed = 0.0f;
        }

        SafeKnockbackWorld safeKnockbackWorld = _safeKnockback;
        Collider2D bodyCollider = BodyCollider;
        if (safeKnockbackWorld != null && bodyCollider != null)
        {
            Vector2 safeDelta = safeKnockbackWorld.ResolveDisplacement(bodyCollider, new Vector2(delta.x, delta.y));
            delta.x = safeDelta.x;
            delta.y = safeDelta.y;
        }

        return delta;
    }

    void MoveByDelta(Vector3 delta)
    {
        if (delta.sqrMagnitude <= 0.000001f)
            return;

        if (_body != null && _body.simulated)
            _body.MovePosition(_body.position + new Vector2(delta.x, delta.y));
        else
            transform.position += delta;
    }

}

}
