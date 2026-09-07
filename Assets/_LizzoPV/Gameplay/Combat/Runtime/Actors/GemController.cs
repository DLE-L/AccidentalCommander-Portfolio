using System.Collections.Generic;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Gameplay.Combat;
using UnityEngine;

public class GemController : BaseController, IVisibilityCullTarget
{
	const float RED_CHARGER_REWARD_SCALE = 1.35f;

	bool _visible = true;
	Renderer[] _renderers;
	Collider2D[] _gameplayColliders;
	[SerializeField] CircleCollider2D _visibilityProbeCollider;
	[SerializeField] VisibilityCullProbe _visibilityProbe;
	[SerializeField, Min(0.01f)] float _homingSpeed = 12.0f;
	[SerializeField, Min(0.01f)] float _absorbDistance = 0.12f;
	Vector3 _baseScale = Vector3.one;
	bool _hasBaseScale;

	public string SourceEnemyId { get; private set; } = "unknown";
	public int SourceRewardTotal { get; private set; }
	public int RewardAmount { get; private set; } = 1;

	public override bool Init()
	{
		bool initialized = base.Init();
		if (initialized)
			ObjectType = Define.ObjectType.Env;

		if (_hasBaseScale == false)
		{
			_baseScale = transform.localScale;
			_hasBaseScale = true;
		}

		ResetForSpawn();
		return true;
	}

	public override void ResetForSpawn()
	{
		ObjectType = Define.ObjectType.Env;
		SourceEnemyId = "unknown";
		SourceRewardTotal = 0;
		RewardAmount = 1;
		transform.localScale = _baseScale;
		if (!ResolveVisibilityComponents())
			return;

		_visibilityProbe.Bind(this);
		CameraVisibilityZone zone = CameraVisibilityZone.Current;
		bool visible = zone == null || zone.ContainsWorldPosition(transform.position);
		SetVisible(visible, force: true);
	}

	public void SetRewardSource(string enemyId, int rewardAmount)
	{
		SourceEnemyId = string.IsNullOrEmpty(enemyId) ? "unknown" : enemyId;
		RewardAmount = Mathf.Max(1, rewardAmount);
		SourceRewardTotal = RewardAmount;
		if (SourceEnemyId == CombatIds.EliteRedCharger)
			transform.localScale = _baseScale * RED_CHARGER_REWARD_SCALE;
	}

	public bool AdvanceToward(Vector3 destination, float deltaTime)
	{
		if ((transform.position - destination).sqrMagnitude <= _absorbDistance * _absorbDistance)
			return true;

		transform.position = Vector3.MoveTowards(
			transform.position,
			destination,
			_homingSpeed * Mathf.Max(0.0f, deltaTime));
		return (transform.position - destination).sqrMagnitude <= _absorbDistance * _absorbDistance;
	}

	public void OnVisibilityEnter(CameraVisibilityZone zone)
	{
		SetVisible(true);
	}

	public void OnVisibilityExit(CameraVisibilityZone zone)
	{
		SetVisible(false);
	}

	void OnDisable()
	{
		CameraVisibilityZone.Current?.Forget(this);
	}

	bool ResolveVisibilityComponents()
	{
		if (_visibilityProbeCollider == null || _visibilityProbe == null)
		{
			Debug.LogError("[GemController] Authored visibility probe collider and VisibilityCullProbe references are required.", this);
			return false;
		}

		_renderers ??= GetComponentsInChildren<Renderer>(true);
		if (_gameplayColliders != null)
			return true;

		Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(true);
		int gameplayColliderCount = 0;
		for (int i = 0; i < allColliders.Length; i++)
		{
			if (allColliders[i] != null && allColliders[i] != _visibilityProbeCollider)
				gameplayColliderCount++;
		}

		_gameplayColliders = new Collider2D[gameplayColliderCount];
		int writeIndex = 0;
		for (int i = 0; i < allColliders.Length; i++)
		{
			if (allColliders[i] == null || allColliders[i] == _visibilityProbeCollider)
				continue;

			_gameplayColliders[writeIndex] = allColliders[i];
			writeIndex++;
		}

		return true;
	}

	void SetVisible(bool visible, bool force = false)
	{
		if (_visible == visible && force == false)
			return;

		_visible = visible;
		if (!ResolveVisibilityComponents())
			return;

		if (_visibilityProbeCollider != null)
			_visibilityProbeCollider.enabled = true;

		for (int i = 0; i < _renderers.Length; i++)
		{
			if (_renderers[i] != null)
				_renderers[i].enabled = visible;
		}

		for (int i = 0; i < _gameplayColliders.Length; i++)
		{
			if (_gameplayColliders[i] != null)
				_gameplayColliders[i].enabled = visible;
		}
	}
}
