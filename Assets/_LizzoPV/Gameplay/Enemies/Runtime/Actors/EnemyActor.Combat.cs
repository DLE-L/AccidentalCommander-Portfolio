using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;

namespace Lizzo.PV.Gameplay.Units
{
public partial class EnemyActor
{
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

	public void OnDamaged(UnityEngine.Component attacker, int damage)
	{
		CombatKillSourceCategory sourceCategory = attacker is CommanderActor
			? CombatKillSourceCategory.Commander
			: CombatKillSourceCategory.None;
		ApplyIncomingDamage(attacker == null ? (Vector3?)null : attacker.transform.position, damage,
			attacker == null ? CombatIds.Unknown : ResolveIncomingDamageSource(attacker), sourceCategory, default);
	}

	public void OnDamagedFromPosition(Vector3 sourcePosition, int damage, string sourceId = null, CountableKillAttribution killAttribution = default)
	{
		ApplyIncomingDamage(sourcePosition, damage, CombatIds.Normalize(sourceId), killAttribution.Category, killAttribution);
	}

	private bool ApplyIncomingDamage(Vector3? sourcePosition, int damage, string sourceId,
		CombatKillSourceCategory category, CountableKillAttribution killAttribution,
        CombatStatusPayload statusPayload = default, float executionThreshold = 0f)
	{
		if (RunPauseController.IsResultGameplayLocked || Hp <= 0)
			return false;

        bool execute = executionThreshold > 0f && !IsBoss && MaxHp > 0 && (float)Hp / MaxHp <= executionThreshold;
        damage = execute ? Hp : ResolveIncomingDamage(sourcePosition, damage);
		if (damage <= 0)
			return false;

        ApplyHitStatus(statusPayload, Lizzo.PV.Data.StatusApplicationTiming.BeforeDeathResolution);
		int hpBefore = Hp;
		RecordIncomingDamage(sourceId, damage, category);
		_lethalKillAttribution = Hp > 0 && Hp - damage <= 0 && killAttribution.IsAttributable
			? killAttribution
			: default;
		if (_health.ApplyDamageAndCheckDeath(damage))
        OnDead();
		if (Hp > 0)
            ApplyHitStatus(statusPayload, Lizzo.PV.Data.StatusApplicationTiming.AfterDamageIfAlive);
		int appliedDamage = Mathf.Max(0, hpBefore - Hp);
		if (appliedDamage <= 0)
			return false;

		DamageApplied?.Invoke(appliedDamage);
		return true;
	}

    private void ApplyHitStatus(in CombatStatusPayload payload, Lizzo.PV.Data.StatusApplicationTiming timing)
    {
        if (!payload.IsConfigured || Lizzo.PV.Data.CompanionEnemyStatusRules.GetApplicationTiming(payload.Kind) != timing)
            return;
        ApplyCompanionStatus(payload.Kind, payload.Source, payload.Magnitude, payload.Duration, Time.time);
    }
	public bool TryReceiveImmediateHit(in CombatImmediateHitRequest request)
	{
		if (request.Mode != CombatImmediateHitMode.AllyDirectTarget)
			return false;

		RunBossDpsTracker.RecordBossDamage(request.SourceId, this, request.Damage);
		bool damageApplied = ApplyIncomingDamage(request.Origin, request.Damage,
			CombatIds.Normalize(request.SourceId), request.KillAttribution.Category, request.KillAttribution, request.StatusPayload, request.ExecutionThreshold);
		ImmediateHitReceived?.Invoke(request, damageApplied);
		return true;
	}

	void ApplyContactDamage(CommanderActor player, Vector3 dir)
	{
		int damage = _runtimeStats == null ? 2 : _runtimeStats.AttackDamage;
		float cooldown = _runtimeStats == null ? 0.5f : _runtimeStats.AttackCooldown;
		string patternId = ResolveCurrentDamagePatternId();
		RunDeathReasonTracker.RecordEnemyDamage(this, patternId);
		CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateEnemyContact(
			GetDamageEnemyId(),
			player,
			transform.position,
			dir,
			damage,
			patternId,
            RetroVfxKind.PlayerDamaged, source: this);
		ICombatImmediateHitModule module = ImmediateHits;
		if (module == null)
		{
			Debug.LogError("[EnemyActor] Required CombatImmediateHitModule runtime wiring is missing.", this);
			return;
		}
		module.TryApply(request);
		ContactAttack.ScheduleNext(cooldown);
	}

	void RecordIncomingDamage(string sourceId, int damage, CombatKillSourceCategory sourceCategory)
	{
		if (damage <= 0 || Hp <= 0)
			return;

		int appliedDamage = Mathf.Min(Hp, damage);
		if (appliedDamage <= 0)
			return;

		bool willKill = Hp - appliedDamage <= 0;
		_combatTelemetry?.RecordHit(this, sourceId, sourceCategory, appliedDamage, willKill);
		RunDiagnostics.RecordEnemyDamage(this, sourceId, appliedDamage, willKill);
	}

	string ResolveIncomingDamageSource(UnityEngine.Component attacker)
	{
		if (attacker is CommanderActor)
			return CombatIds.Commander;

		if (attacker == null)
			return CombatIds.Unknown;

		return attacker.gameObject.name;
	}

	bool CanUseContactAttack()
	{
		return true;
	}

	bool IsTouchingPlayer(CommanderActor player)
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
		if (_chargeAttack != null && !string.IsNullOrEmpty(_chargeAttack.ActiveDamagePatternId))
			return _chargeAttack.ActiveDamagePatternId;

		if (_hungryGiant != null && _hungryGiant.IsAoeDamageFrame)
			return EnemyBossController.BossAoePatternId;

		if (_hungryGiant != null && _hungryGiant.IsCharging)
			return CombatIds.BossSlowCharge;


		return CombatIds.ContactAttack;
	}

	internal int ResolveBossStaggerDamage(int damage) =>
		_bossVulnerability == null ? damage : _bossVulnerability.ResolveStaggerIncomingDamage(damage);

	int ResolveIncomingDamage(Vector3? sourcePosition, int damage)
	{
		damage = ResolveBossStaggerDamage(damage);

		if (damage > 0)
		{
			damage = Mathf.Max(1, Mathf.RoundToInt(
				damage * _companionEnemyStatuses.ResolveIncomingDamageMultiplier(Time.time)));
		}

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

}
