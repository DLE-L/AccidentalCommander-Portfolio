using Lizzo.PV.P0.Units;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;

public partial class MonsterController
{
	void FixedUpdate()
    {
		if (RunPauseController.IsResultGameplayLocked)
			return;

		if (CreatureState == Define.CreatureState.Dead)
			return;

		Vector3 knockbackDelta = ConsumeSmoothKnockbackDelta(Time.fixedDeltaTime);
		if (CreatureState != Define.CreatureState.Moving)
		{
			MoveByDelta(knockbackDelta);
			return;
		}

		PlayerController pc = Services.Registry.Player;
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

		if (_animator == null && _unitVisual == null)
		{
			SpriteRenderer spriteRenderer = _fallbackSpriteRenderer;
			if (spriteRenderer != null)
				spriteRenderer.flipX = dir.x > 0;
		}
    }

	private void OnCollisionEnter2D(Collision2D collision)
	{
	}

	public void OnCollisionExit2D(Collision2D collision)
	{
	}

	public void SetExternalMovement(bool usesExternalMovement)
	{
		_usesExternalMovement = usesExternalMovement;
	}

	public bool TryEnterContactAttack()
	{
		if (RunPauseController.IsResultGameplayLocked)
			return false;

		PlayerController player = Services.Registry.Player;
		if (player == null)
			return false;

		if (CanUseContactAttack() == false)
			return false;

		if (IsTouchingPlayer(player) == false)
			return false;

		if (_body != null)
			_body.linearVelocity = Vector2.zero;

		CreatureState = Define.CreatureState.Skill;
		return true;
	}

	public bool TryApplyContactDamageNow()
	{
		if (RunPauseController.IsResultGameplayLocked)
			return false;

		PlayerController player = Services.Registry.Player;
		if (player == null || player.Hp <= 0)
			return false;

		if (CanUseContactAttack() == false)
			return false;

		if (IsTouchingPlayer(player) == false)
			return false;

		if (_body != null)
			_body.linearVelocity = Vector2.zero;

		Vector3 dir = player.transform.position - transform.position;
		UpdateFacing(dir);

		if (Time.time >= _nextAttackTime)
			ApplyContactDamage(player, dir);

		return true;
	}

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
		_lastFacingDirection = DirectionalAnimator.ResolveDirectionName(direction);
		UpdateAnimation();
	}

	string ResolveInitialFacingDirection()
	{
		PlayerController player = Services.Registry.Player;
		if (player == null)
			return "S";

		Vector3 direction = player.transform.position - transform.position;
		if (direction.sqrMagnitude <= 0.001f)
			return "S";

		return DirectionalAnimator.ResolveDirectionName(direction);
	}

	Vector3 ResolveInitialFacingVector()
	{
		PlayerController player = Services.Registry.Player;
		if (player == null)
			return Vector3.down;

		Vector3 direction = player.transform.position - transform.position;
		if (direction.sqrMagnitude <= 0.001f)
			return Vector3.down;

		return direction.normalized;
	}
    public void ApplySmoothKnockback(Vector3 direction, float distance, float duration = 0.16f)
    {
        if (_isDead || distance <= 0.0f || direction.sqrMagnitude <= 0.0001f)
            return;

        Vector3 normalizedDirection = direction.normalized;
        if (_smoothKnockbackRemainingDistance > 0.0f && _smoothKnockbackDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 combined = _smoothKnockbackDirection * _smoothKnockbackRemainingDistance + normalizedDirection * distance;
            _smoothKnockbackDirection = combined.sqrMagnitude <= 0.0001f ? normalizedDirection : combined.normalized;
            _smoothKnockbackRemainingDistance += distance;
        }
        else
        {
            _smoothKnockbackDirection = normalizedDirection;
            _smoothKnockbackRemainingDistance = distance;
        }

        _smoothKnockbackDuration = Mathf.Max(MIN_SMOOTH_KNOCKBACK_DURATION, duration);
        _smoothKnockbackElapsed = 0.0f;
    }

    Vector3 ConsumeSmoothKnockbackDelta(float deltaTime)
    {
        if (_smoothKnockbackRemainingDistance <= 0.0f || _smoothKnockbackDirection.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        float remainingDuration = Mathf.Max(0.001f, _smoothKnockbackDuration - _smoothKnockbackElapsed);
        float stepRatio = Mathf.Clamp01(deltaTime / remainingDuration);
        float moveDistance = _smoothKnockbackRemainingDistance * stepRatio;
        Vector3 delta = _smoothKnockbackDirection * moveDistance;
        _smoothKnockbackElapsed += deltaTime;
        _smoothKnockbackRemainingDistance = Mathf.Max(0.0f, _smoothKnockbackRemainingDistance - moveDistance);

        if (_smoothKnockbackRemainingDistance <= 0.001f)
        {
            _smoothKnockbackDirection = Vector3.zero;
            _smoothKnockbackRemainingDistance = 0.0f;
            _smoothKnockbackDuration = 0.0f;
            _smoothKnockbackElapsed = 0.0f;
        }

        SafeKnockbackWorld safeKnockbackWorld = Services == null ? null : Services.SafeKnockbackWorld;
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
