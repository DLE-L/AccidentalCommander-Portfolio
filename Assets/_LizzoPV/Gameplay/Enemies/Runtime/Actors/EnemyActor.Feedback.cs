using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
public partial class EnemyActor
{
	private void OnDead()
	{
		if (_isDead)
			return;



		_isDead = true;
		ClearExternalAttackRecovery();
		CreatureState = Define.CreatureState.Dead;
		Lizzo.PV.Combat.CountableKillAttribution lethalAttribution = _lethalKillAttribution;
		_lethalKillAttribution = default;
		_deathResolver.Resolve(this, lethalAttribution, _companionEnemyStatuses);
		bool isBoss = IsBoss;

		if (_body != null)
		{
			_body.linearVelocity = Vector2.zero;
			_body.simulated = false;
		}

		SetCollidersEnabled(false);

		_bossClearResultAt = Time.time + DIE_DESPAWN_DELAY;
		_shouldShowBossClearResult = isBoss;
		_despawnAt = isBoss ? _bossClearResultAt + 0.1f : Time.time + DIE_DESPAWN_DELAY;
	}

	void RefreshHealthBar()
	{
		EnemyRuntimeStats stats = _runtimeStats;
		if (stats?.Data == null || IsBoss)
		{
			EnemyHealthBar.RemoveFrom(transform);
			return;
		}

		EnemyHealthBar healthBar = _healthBar;
		if (healthBar == null)
		{
			Debug.LogError($"Enemy prefab is missing required EnemyHealthBar: {gameObject.name}", this);
			return;
		}

		bool alwaysVisible = IsElite || stats.Data.Id == CombatIds.ShieldOrc;
		healthBar.Refresh(this, alwaysVisible, EnemyHealthBar.HIT_REVEAL_SECONDS);
	}

}

}
