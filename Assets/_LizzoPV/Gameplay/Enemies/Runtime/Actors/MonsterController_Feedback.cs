using Lizzo.PV.P0.Combat;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

public partial class MonsterController
{
	protected override void OnDead()
	{
		if (_isDead)
			return;

		base.OnDead();

		_isDead = true;
		CreatureState = Define.CreatureState.Dead;
		Services.Registry.MarkEnemyInactive(this);
		P0PlaytestDiagnostics.RegisterEnemyDeath(this);
		Services.State.RegisterKill();
		Services.State.RegisterCountableKill(_lethalKillAttribution.WithLethalContext(_spawnSequence, transform.position, Time.frameCount));
		_lethalKillAttribution = default;
		if (_companionEnemyStatuses.TryCaptureDeath(Time.time, out CompanionEnemyDeathStatusSnapshot statusSnapshot))
			Services.Party?.ReportCompanionEnemyDeathStatus(statusSnapshot, transform.position);

		EnemyRuntimeStats stats = _runtimeStats;
		string enemyId = stats?.Data?.Id ?? GetDamageEnemyId();
		bool isShieldOrc = enemyId == CombatIds.ShieldOrc;
		bool isBoss = IsBoss;
		bool isElite = stats?.Data?.Type == "elite";

		if (isElite)
			Services.RunTraitOffers?.ReportEliteDefeated();

		if (isBoss)
		{
			Build1RuntimeDiagnostics.Log(
				"boss_defeated",
				Build1RuntimeDiagnostics.Text("boss_id", enemyId),
				Build1RuntimeDiagnostics.Text("time_since_spawn", "unavailable"));
			HitStop.Request(0.15f, "boss_defeated");
		}

		int expReward = stats?.Data == null ? 1 : stats.Data.ExpReward;
		if (expReward > 0)
		{
			P0Telemetry.Log(
				P0Telemetry.EnemyRewardDrop,
				$"enemy_id={enemyId}",
				$"actual_exp_reward={expReward}",
				$"orb_count={expReward}",
				"visual_only=false");
		}

		int orbCount = Services.Definition.CollapseExperienceDrops && expReward > 0 ? 1 : expReward;
		for (int i = 0; i < orbCount; i++)
		{
			Vector2 offset = Random.insideUnitCircle * (isShieldOrc ? 0.38f : 0.25f);
			GemController gem = Services.Spawner.SpawnGem(transform.position + new Vector3(offset.x, offset.y, 0.0f));
			gem?.SetRewardSource(enemyId, expReward);
		}
		PlayDeathFeedback(enemyId, expReward);

		if (isShieldOrc)
		{
			FloatingDamageText.ShowLabel(transform.position + Vector3.up * 0.55f, "EXP!", new Color(0.28f, 1.0f, 0.35f, 1.0f), large: true, lifeTime: 0.85f);
			P0PlaytestDiagnostics.LogShieldOrcFeedbackCheck("death", this);
		}

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

	void PlayDeathFeedback(string enemyId, int expReward)
	{
		if (enemyId == ELITE_RED_CHARGER_ID)
		{
			EnemyDeathFeedback.ShowRedChargerDefeatFeedback(transform.position, expReward);
			return;
		}

		if (enemyId == SMALL_GOBLIN_ID || enemyId == HUNGRY_WOLF_ID)
			EnemyDeathFeedback.RecordNormalDeathFeedback(enemyId, expReward);
	}

	void RefreshHealthBar()
	{
		EnemyRuntimeStats stats = _runtimeStats;
		if (stats?.Data == null || stats.Data.Type == "boss")
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

		bool alwaysVisible = stats.Data.Id == CombatIds.ShieldOrc || stats.Data.Id == CombatIds.EliteRedCharger;
		healthBar.Refresh(this, alwaysVisible, EnemyHealthBar.HIT_REVEAL_SECONDS);
	}

	bool ShouldShowLargeDamageText(int damage)
	{
		if (IsShieldOrc())
			return true;

		EnemyRuntimeStats stats = _runtimeStats;
		return stats != null && stats.Data != null && stats.Data.Type != "normal" && damage >= 10;
	}

	void PlayShieldOrcHitFeedback()
	{
		if (IsShieldOrc() == false || MaxHp <= 0)
			return;

		HitFlash flash = _hitFlash;
		if (flash == null)
		{
			Debug.LogError($"Enemy prefab is missing required HitFlash: {gameObject.name}", this);
			return;
		}
		flash.PlayShake();

		if (_shieldOrcHitFeedbackLogged == false)
		{
			_shieldOrcHitFeedbackLogged = true;
			P0PlaytestDiagnostics.LogShieldOrcFeedbackCheck("hit", this);
		}

		if (_shieldOrcBreakFeedbackShown || Hp > MaxHp * 0.5f)
			return;

		_shieldOrcBreakFeedbackShown = true;
		FloatingDamageText.ShowLabel(transform.position + Vector3.up * 0.35f, "방패 균열!", new Color(1.0f, 0.82f, 0.18f, 1.0f), large: true);
		RetroVfx.Spawn(RetroVfxKind.ShieldOrcCrack, transform.position, Vector3.zero, 1.0f);
		P0PlaytestDiagnostics.LogShieldOrcFeedbackCheck("crack", this);
	}
}
