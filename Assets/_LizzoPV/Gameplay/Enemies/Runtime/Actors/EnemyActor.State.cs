using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;
using Lizzo.PV.Flow;

namespace Lizzo.PV.Gameplay.Units
{
public partial class EnemyActor
{
	public virtual Define.CreatureState CreatureState
	{
		get { return _creatureState; }
		set
		{
			_creatureState = value;

		}
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

			_lastFacingVector = direction.normalized;
		}

		AttackPoseRequested?.Invoke(direction, duration);
	}

	public void UpdateController()
	{
		if (RunPauseController.IsResultGameplayLocked)
			return;



		switch (CreatureState)
		{
			case Define.CreatureState.Skill:
				ContactAttack.Advance();
				break;
			case Define.CreatureState.Dead:
				UpdateDead();
				break;
		}
	}

	protected virtual void UpdateDead()
	{
		if (_shouldShowBossClearResult && _bossClearResultShown == false && Time.time >= _bossClearResultAt)
		{
			_bossClearResultShown = true;
			CombatRuntimeDiagnostics.Log(
				"boss_result_transition",
				CombatRuntimeDiagnostics.Text("boss_id", EnemyId),
				CombatRuntimeDiagnostics.Float("delay_seconds", DIE_DESPAWN_DELAY));
			FindFirstObjectByType<GameScene>()?.ShowClearResult();
		}

		if (Time.time < _despawnAt)
			return;

		Registry.ReleaseEnemy(this);
	}
}

}
