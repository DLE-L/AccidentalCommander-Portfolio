using System.Collections;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;

public partial class MonsterController
{
	Coroutine _coDotDamage;

	public string GetDamageSourceKey()
	{
		return CombatIds.DamageCooldownKey(GetDamageEnemyId(), GetInstanceID(), ResolveCurrentDamagePatternId());
	}

	public string GetDamageEnemyId()
	{
		return EnemyId;
	}

	public string GetDamagePatternId()
	{
		return ResolveCurrentDamagePatternId();
	}

	public override void OnDamaged(BaseController attacker, int damage)
	{
		if (RunPauseController.IsResultGameplayLocked)
			return;

		damage = ResolveIncomingDamage(attacker == null ? (Vector3?)null : attacker.transform.position, damage);
		RecordIncomingDamage(attacker == null ? CombatIds.Unknown : ResolveIncomingDamageSource(attacker), damage);
		base.OnDamaged(attacker, damage);
		FloatingDamageText.ShowEnemyDamage(transform.position, damage, ShouldShowLargeDamageText(damage));
		PlayShieldOrcHitFeedback();
		RefreshHealthBar();
	}

	public void OnDamagedFromPosition(Vector3 sourcePosition, int damage, string sourceId = null, CountableKillAttribution killAttribution = default)
	{
		if (RunPauseController.IsResultGameplayLocked)
			return;

		damage = ResolveIncomingDamage(sourcePosition, damage);
		RecordIncomingDamage(CombatIds.Normalize(sourceId), damage);
		_lethalKillAttribution = Hp > 0 && Hp - damage <= 0 && killAttribution.IsAttributable
			? killAttribution
			: default;
		base.OnDamaged(null, damage);
		FloatingDamageText.ShowEnemyDamage(transform.position, damage, ShouldShowLargeDamageText(damage));
		PlayShieldOrcHitFeedback();
		RefreshHealthBar();
	}

	public void ReceiveImmediateHit(in CombatImmediateHitRequest request)
	{
		if (request.Mode != CombatImmediateHitMode.AllyDirectTarget)
			return;

		P0BossDpsTracker.RecordBossDamage(request.SourceId, this, request.Damage);
		OnDamagedFromPosition(request.Origin, request.Damage, CombatIds.Normalize(request.SourceId), request.KillAttribution);
		if (request.SpawnAllyFeedback)
			AttackVisual.Spawn(request.FeedbackPosition, request.AllyFeedback);

		if (this == null || isActiveAndEnabled == false || Hp <= 0)
			return;

		HitFlash flash = HitFlash;
		if (flash == null)
		{
			Debug.LogError($"Enemy prefab is missing required HitFlash: {gameObject.name}", this);
			return;
		}
		flash.Play();

		EnemyRuntimeStats stats = RuntimeStats;
		if (stats?.Data == null || stats.Data.Type == "boss")
		{
			EnemyHealthBar.RemoveFrom(transform);
			return;
		}

		EnemyHealthBar healthBar = HealthBar;
		if (healthBar == null)
		{
			Debug.LogError($"Enemy prefab is missing required EnemyHealthBar: {gameObject.name}", this);
			return;
		}

		bool alwaysVisible = stats.Data.Id == CombatIds.ShieldOrc || stats.Data.Id == CombatIds.EliteRedCharger;
		healthBar.Refresh(this, alwaysVisible, EnemyHealthBar.HIT_REVEAL_SECONDS);
	}

	public IEnumerator CoStartDotDamage(PlayerController target)
	{
		while (true)
		{
			int damage = _runtimeStats == null ? 2 : _runtimeStats.AttackDamage;
			float cooldown = _runtimeStats == null ? 0.1f : _runtimeStats.AttackCooldown;
			target.OnDamaged(this, damage);

			yield return new WaitForSeconds(cooldown);
		}
	}

	void StopDotDamage()
	{
		if (_coDotDamage == null)
			return;

		StopCoroutine(_coDotDamage);
		_coDotDamage = null;
	}

	void ApplyContactDamage(PlayerController player, Vector3 dir)
	{
		int damage = _runtimeStats == null ? 2 : _runtimeStats.AttackDamage;
		float cooldown = _runtimeStats == null ? 0.5f : _runtimeStats.AttackCooldown;
		string patternId = ResolveCurrentDamagePatternId();
		P0DeathReasonTracker.RecordEnemyDamage(this, patternId);
		CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateEnemyContact(
			this,
			player,
			transform.position,
			dir,
			damage,
			patternId,
			RetroVfxKind.EnemyContactHit);
		ICombatImmediateHitModule module = Services?.ImmediateHitModule;
		if (module == null)
		{
			Debug.LogError("[MonsterController] Required CombatImmediateHitModule runtime wiring is missing.", this);
			return;
		}
		if (module.TryApply(request))
		{
			RetroVfx.Spawn(RetroVfxKind.EnemyContactHit, player.transform.position, dir, 1.0f);
		}
		_nextAttackTime = Time.time + cooldown;
	}

	void RecordIncomingDamage(string sourceId, int damage)
	{
		if (damage <= 0 || Hp <= 0)
			return;

		int appliedDamage = Mathf.Min(Hp, damage);
		if (appliedDamage <= 0)
			return;

		bool willKill = Hp - appliedDamage <= 0;
		float attackIntervalDivisor = Services?.Party?.ResolveCompanionAttackIntervalDivisorForSource(sourceId) ?? 1.0f;
		Services?.DamageContributions?.RecordAppliedDamage(sourceId, appliedDamage, attackIntervalDivisor);
		P0PlaytestDiagnostics.RecordEnemyDamage(this, sourceId, appliedDamage, willKill);
	}

	string ResolveIncomingDamageSource(BaseController attacker)
	{
		if (attacker is PlayerController)
			return CombatIds.Commander;

		if (attacker == null)
			return CombatIds.Unknown;

		return attacker.gameObject.name;
	}

	bool CanUseContactAttack()
	{
		return true;
	}

	bool IsTouchingPlayer(PlayerController player)
	{
		if (player == null || player.Hp <= 0)
			return false;

		Collider2D combatCollider = CombatCollider;
		if (combatCollider == null)
		{
			ValidateCombatCollider();
			combatCollider = CombatCollider;
		}

		if (combatCollider == null || combatCollider.enabled == false)
			return false;

		if (_cachedContactPlayer != player || _cachedPlayerCombatCollider == null)
		{
			_cachedContactPlayer = player;
			_cachedPlayerCombatCollider = ResolveCombatCollider(player.transform);
		}

		if (_cachedPlayerCombatCollider == null || _cachedPlayerCombatCollider.enabled == false)
			return false;

		ColliderDistance2D distance = combatCollider.Distance(_cachedPlayerCombatCollider);
		return distance.isOverlapped || distance.distance <= DEFAULT_CONTACT_PADDING;
	}

	string ResolveCurrentDamagePatternId()
	{
		if (_wolfDash != null && _wolfDash.IsDashing)
			return "wolf_short_dash";

		if (_hungryGiant != null && _hungryGiant.IsAoeDamageFrame)
			return HungryGiantBehaviour.BossAoePatternId;

		if (_hungryGiant != null && _hungryGiant.IsCharging)
			return CombatIds.BossSlowCharge;

		if (_redCharger != null && _redCharger.IsImpactGrace)
			return CombatIds.RedChargerImpactGrace;

		if (_redCharger != null && _redCharger.IsCharging)
			return CombatIds.RedChargerDash;

		return CombatIds.ContactAttack;
	}

	int ResolveIncomingDamage(Vector3? sourcePosition, int damage)
	{
		if (_hungryGiant != null)
			damage = _hungryGiant.ResolveStaggerIncomingDamage(damage);

		if (damage <= 0 || IsShieldOrc() == false || sourcePosition.HasValue == false)
			return damage;

		Vector3 sourceDirection = sourcePosition.Value - transform.position;
		if (sourceDirection.sqrMagnitude <= 0.001f || _lastFacingVector.sqrMagnitude <= 0.001f)
			return damage;

		float dot = Vector3.Dot(sourceDirection.normalized, _lastFacingVector.normalized);
		if (dot < SHIELD_ORC_FRONT_DOT_THRESHOLD)
			return damage;

		return Mathf.Max(1, Mathf.RoundToInt(damage * SHIELD_ORC_FRONT_DAMAGE_MULTIPLIER));
	}

	bool IsShieldOrc()
	{
		return IsShieldOrcEnemy;
	}
}
