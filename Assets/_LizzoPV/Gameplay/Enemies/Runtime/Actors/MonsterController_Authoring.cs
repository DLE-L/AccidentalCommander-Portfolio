using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.Combat;

public partial class MonsterController
{
	public bool TryCancelChargeAndApplyStun(float stunDuration, out ChargeCancellationResult result)
	{
		EnsureRuntimeComponents();
		IChargeCancelable chargeCancelable = _redCharger;
		if (chargeCancelable == null)
		{
			result = new ChargeCancellationResult(false, false);
			return false;
		}

		result = chargeCancelable.CancelChargeAndApplyStun(stunDuration);
		return result.ChargeCancelled;
	}

	public Collider2D CombatCollider
	{
		get
		{
			EnsureColliderRefs();
			return _colliderRefs == null ? null : _colliderRefs.CombatCollider;
		}
	}

	public Collider2D BodyCollider
	{
		get
		{
			EnsureColliderRefs();
			return _colliderRefs == null ? null : _colliderRefs.BodyCollider;
		}
	}

	void EnsureRuntimeComponents()
	{
		if (_animator == null)
			_animator = GetComponentInChildren<Animator>(true);

		if (_unitVisual == null)
			_unitVisual = GetComponent<UnitVisualRole>();
		if (_patternVisual == null)
			_patternVisual = GetComponent<PatternEnemyVisual>();
		if (_unitVisual == null)
			Debug.LogError($"Enemy prefab is missing required UnitVisualRole: {gameObject.name}", this);
		if (_fallbackSpriteRenderer == null)
			_fallbackSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
		if (_runtimeStats == null)
			_runtimeStats = GetComponent<EnemyRuntimeStats>();
		if (_hitFlash == null)
			_hitFlash = GetComponent<HitFlash>();
		if (_healthBar == null)
			_healthBar = GetComponent<EnemyHealthBar>();
		if (_wolfDash == null)
			_wolfDash = GetComponent<WolfDashBehaviour>();
		if (_hungryGiant == null)
			_hungryGiant = GetComponent<HungryGiantBehaviour>();
		if (_redCharger == null)
			_redCharger = GetComponent<RedChargerBehaviour>();

		if (_body == null)
			_body = GetComponent<Rigidbody2D>();
		if (_body == null)
		{
			Debug.LogError($"Enemy prefab is missing required Rigidbody2D: {gameObject.name}", this);
			return;
		}

		_body.gravityScale = 0.0f;
		_body.freezeRotation = true;
		EnsureColliderRefs();
		CacheRuntimeColliders();
		CacheResettableSpriteRenderers();
		ValidateColliderRefs();
		ValidateBodyCollider();
		ValidateCombatCollider();
	}

	void EnsureColliderRefs()
	{
		if (_colliderRefs != null)
			return;

		_colliderRefs = GetComponent<UnitColliderRefs>();
	}

	void ValidateColliderRefs()
	{
		if (_colliderRefs == null)
			Debug.LogError($"Enemy prefab is missing required UnitColliderRefs: {gameObject.name}", this);
	}

	void ValidateBodyCollider()
	{
		EnsureColliderRefs();

		Collider2D bodyCollider = BodyCollider;
		if (bodyCollider == null)
		{
			Debug.LogError($"Enemy prefab is missing required BodyCollider reference: {gameObject.name}", this);
			return;
		}

		if (bodyCollider.isTrigger)
			Debug.LogError($"Enemy BodyCollider must not be trigger: {gameObject.name}", this);
	}

	void ValidateCombatCollider()
	{
		EnsureColliderRefs();

		Collider2D combatCollider = CombatCollider;
		if (combatCollider == null)
		{
			Debug.LogError($"Enemy prefab is missing required CombatCollider reference: {gameObject.name}", this);
			return;
		}

		if (combatCollider.isTrigger == false)
			Debug.LogError($"Enemy CombatCollider must be trigger: {gameObject.name}", this);
	}

	void SetCollidersEnabled(bool enabled)
	{
		CacheRuntimeColliders();
		for (int i = 0; i < _allColliders.Length; i++)
		{
			if (_allColliders[i] != null)
				_allColliders[i].enabled = enabled;
		}

		ValidateBodyCollider();
		ValidateCombatCollider();
	}

	void CacheRuntimeColliders()
	{
		if (_allColliders != null)
			return;

		_allColliders = GetComponentsInChildren<Collider2D>(true);
	}

	void CacheResettableSpriteRenderers()
	{
		if (_resettableSpriteRenderers != null)
			return;

		SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
		int count = 0;
		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] != null && IsRuntimeOverlayRenderer(renderers[i].transform) == false)
				count++;
		}

		_resettableSpriteRenderers = new SpriteRenderer[count];
		int writeIndex = 0;
		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] == null || IsRuntimeOverlayRenderer(renderers[i].transform))
				continue;

			_resettableSpriteRenderers[writeIndex] = renderers[i];
			writeIndex++;
		}
	}

	static bool IsRuntimeOverlayRenderer(Transform target)
	{
		while (target != null)
		{
			if (target.name == "P0_HPBar")
				return true;

			target = target.parent;
		}

		return false;
	}

	static Collider2D ResolveCombatCollider(Transform owner)
	{
		if (owner == null)
			return null;

		PlayerController player = owner.GetComponent<PlayerController>();
		if (player != null)
			return player.CombatCollider;

		CompanionRuntime companion = owner.GetComponent<CompanionRuntime>();
		if (companion != null)
			return companion.CombatCollider;

		MonsterController monster = owner.GetComponent<MonsterController>();
		if (monster != null)
			return monster.CombatCollider;

		return null;
	}

}
