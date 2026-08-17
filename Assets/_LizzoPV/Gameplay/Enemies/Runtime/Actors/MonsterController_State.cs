using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;
using Lizzo.PV.Flow;

public partial class MonsterController
{
	public virtual Define.CreatureState CreatureState
	{
		get { return _creatureState; }
		set
		{
			_creatureState = value;
			UpdateAnimation();
		}
	}

	public virtual void UpdateAnimation()
	{
		if (_unitVisual != null)
			return;

		if (_animator == null)
			return;

		string directionalStateName;
		string simpleStateName;
		bool shouldPlayExternalAttackPose = CreatureState != Define.CreatureState.Dead
			&& Time.time < _externalAttackPoseUntil;

		switch (CreatureState)
		{
			case Define.CreatureState.Skill:
				directionalStateName = DirectionalAnimator.ResolveAttack3State(_lastFacingDirection);
				simpleStateName = "Attack";
				break;
			case Define.CreatureState.Dead:
				directionalStateName = DirectionalAnimator.ResolveDieState(_lastFacingDirection);
				simpleStateName = "Die";
				break;
			default:
				if (shouldPlayExternalAttackPose)
				{
					directionalStateName = DirectionalAnimator.ResolveAttack3State(_lastFacingDirection);
					simpleStateName = "Attack";
				}
				else
				{
					directionalStateName = DirectionalAnimator.ResolveRunState(_lastFacingDirection);
					simpleStateName = "Run";
				}
				break;
		}

		DirectionalAnimator.PlayAny(_animator, ref _lastAnimatorState, directionalStateName, simpleStateName);
	}

	public void PlayExternalAttackPose(Vector3 direction, float holdSeconds = 0.35f)
	{
		if (CreatureState == Define.CreatureState.Dead)
			return;

		float duration = Mathf.Max(0.05f, holdSeconds);
		if (direction.sqrMagnitude <= 0.001f)
			direction = _lastFacingVector;

		if (direction.sqrMagnitude > 0.001f)
		{
			_externalAttackPoseDirection = direction.normalized;
			_lastFacingVector = _externalAttackPoseDirection;
			_lastFacingDirection = DirectionalAnimator.ResolveDirectionName(new Vector2(direction.x, direction.y));
		}

		_externalAttackPoseUntil = Time.time + duration;
		if (_patternVisual == null)
			_patternVisual = GetComponent<PatternEnemyVisual>();

		if (_patternVisual != null)
		{
			_patternVisual.PlayAttack(direction, duration);
			return;
		}

		if (_unitVisual != null)
			_unitVisual.FaceDirection(direction);

		if (_unitVisual == null)
			UpdateAnimation();
	}

	public void CancelExternalAttackPose()
	{
		_externalAttackPoseUntil = 0.0f;
		if (_patternVisual == null)
			_patternVisual = GetComponent<PatternEnemyVisual>();

		_patternVisual?.CancelAttack();
		if (_patternVisual == null)
			_unitVisual?.CancelAttack();

		if (_unitVisual == null)
			UpdateAnimation();
	}

	public override void UpdateController()
	{
		if (RunPauseController.IsResultGameplayLocked)
			return;

		base.UpdateController();

		switch (CreatureState)
		{
			case Define.CreatureState.Idle:
				UpdateIdle();
				break;
			case Define.CreatureState.Skill:
				UpdateSkill();
				break;
			case Define.CreatureState.Moving:
				UpdateMoving();
				break;
			case Define.CreatureState.Dead:
				UpdateDead();
				break;
		}
	}

	protected virtual void UpdateIdle() { }

	protected virtual void UpdateSkill()
	{
		PlayerController player = Services.Registry.Player;
		if (player == null || player.Hp <= 0)
			return;

		Vector3 dir = player.transform.position - transform.position;
		if (IsTouchingPlayer(player) == false)
		{
			CreatureState = Define.CreatureState.Moving;
			return;
		}

		UpdateFacing(dir);
		if (Time.time < _nextAttackTime)
			return;

		ApplyContactDamage(player, dir);
	}

	protected virtual void UpdateMoving() { }

	protected virtual void UpdateDead()
	{
		if (_shouldShowBossClearResult && _bossClearResultShown == false && Time.time >= _bossClearResultAt)
		{
			_bossClearResultShown = true;
			Build1RuntimeDiagnostics.Log(
				"boss_result_transition",
				Build1RuntimeDiagnostics.Text("boss_id", EnemyId),
				Build1RuntimeDiagnostics.Float("delay_seconds", DIE_DESPAWN_DELAY));
			FindFirstObjectByType<GameScene>()?.ShowClearResult();
		}

		if (Time.time < _despawnAt)
			return;

		Services.Registry.ReleaseEnemy(this);
	}
}
