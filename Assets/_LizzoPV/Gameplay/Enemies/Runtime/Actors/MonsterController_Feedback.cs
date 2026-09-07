using Lizzo.PV.P0.Combat;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
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
		RunDiagnostics.RegisterEnemyDeath(this);
		Services.State.RegisterKill();
		Lizzo.PV.Combat.CountableKillAttribution lethalAttribution = _lethalKillAttribution;
		Services.State.RegisterCountableKill(lethalAttribution.WithLethalContext(_spawnSequence, transform.position, Time.frameCount));
		_lethalKillAttribution = default;
		bool hasCompanionDeathStatus = _companionEnemyStatuses.TryCaptureDeath(
			Time.time,
			out CompanionEnemyDeathStatusSnapshot statusSnapshot);
		Services.ProductionSynergies?.ReportEnemyDeath(
			Lizzo.PV.Gameplay.Run.CompanionSynergyProductionHost.StableEntityId(this),
			transform.position,
			statusSnapshot,
			lethalAttribution);
		if (hasCompanionDeathStatus)
		{
			if (statusSnapshot.WasVulnerable)
			{
				Services.WorldFeedback?.TryPresentStatusReaction(
					Lizzo.PV.Data.CompanionEnemyStatusKind.Vulnerable,
					Lizzo.PV.Presentation.StatusReactionKind.TargetDeath,
					transform.position,
					GetInstanceID());
			}
			if (statusSnapshot.WasCursed)
			{
				Services.WorldFeedback?.TryPresentStatusReaction(
					Lizzo.PV.Data.CompanionEnemyStatusKind.Curse,
					Lizzo.PV.Presentation.StatusReactionKind.TargetDeath,
					transform.position,
					GetInstanceID());
			}
			Services.Party?.ReportCompanionEnemyDeathStatus(statusSnapshot, transform.position);
		}

		EnemyRuntimeStats stats = _runtimeStats;
		string enemyId = stats?.Data?.Id ?? GetDamageEnemyId();
		bool isShieldOrc = enemyId == CombatIds.ShieldOrc;
		bool isBoss = IsBoss;
		bool isElite = IsElite;
		Services.WorldFeedback?.TryPresentEnemyDeath(this, isBoss, isElite);

		if (isBoss)
		{
			CombatRuntimeDiagnostics.Log(
				"boss_defeated",
				CombatRuntimeDiagnostics.Text("boss_id", enemyId),
				CombatRuntimeDiagnostics.Text("time_since_spawn", "unavailable"));
			HitStop.Request(0.15f, "boss_defeated");
		}

		int expReward = EnemyExperienceRewardPolicy.Resolve(
			EncounterRank,
			Services.Context,
			Services.App.Data.RunTuning);
		if (expReward > 0)
		{
			RunTelemetry.Log(
				RunTelemetry.EnemyRewardDrop,
				$"enemy_id={enemyId}",
				$"actual_exp_reward={expReward}",
				"orb_count=1",
				"visual_only=false");
		}

		if (expReward > 0)
		{
			GemController gem = Services.Spawner.SpawnGem(transform.position);
			gem?.SetRewardSource(enemyId, expReward);
		}
		PlayDeathFeedback(enemyId, expReward);

		if (isShieldOrc)
		{
			FloatingDamageText.ShowLabel(transform.position + Vector3.up * 0.55f, "EXP!", new Color(0.28f, 1.0f, 0.35f, 1.0f), large: true, lifeTime: 0.85f);
			RunDiagnostics.LogShieldOrcFeedbackCheck("death", this);
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

	bool ShouldShowLargeDamageText(int damage)
	{
		if (IsShieldOrc())
			return true;

		EnemyRuntimeStats stats = _runtimeStats;
		return stats != null && stats.Data != null && (IsBoss || IsElite) && damage >= 10;
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
			RunDiagnostics.LogShieldOrcFeedbackCheck("hit", this);
		}

		if (_shieldOrcBreakFeedbackShown || Hp > MaxHp * 0.5f)
			return;

		_shieldOrcBreakFeedbackShown = true;
		FloatingDamageText.ShowLabel(transform.position + Vector3.up * 0.35f, "방패 균열!", new Color(1.0f, 0.82f, 0.18f, 1.0f), large: true);
		RetroVfx.Spawn(RetroVfxKind.ShieldOrcCrack, transform.position, Vector3.zero, 1.0f);
		RunDiagnostics.LogShieldOrcFeedbackCheck("crack", this);
	}
}
