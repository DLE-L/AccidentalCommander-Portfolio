using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using System.Collections.Generic;
using Lizzo.PV.Data;

public class PlayerController : CreatureController
{
    Vector2 _moveDir = Vector2.zero;

    float EnvCollectDist { get; set; } = 1.0f;
    bool _loggedLowHp30;
    bool _loggedLowHp10;
    float _invulnerableUntil;
    readonly Dictionary<string, float> _nextDamageTimeBySource = new Dictionary<string, float>();
    Rigidbody2D _body;
    GridController _gridController;
    CommanderAttack _commanderAttack;
    CommanderHealthBar _commanderHealthBar;
    CommanderAllyVisual _commanderVisual;
    HitFlash _hitFlash;
    readonly List<GemController> _collectBuffer = new List<GemController>(64);
    [SerializeField]
    CircleCollider2D _bodyCollider;
    [SerializeField]
    CircleCollider2D _combatCollider;

	[SerializeField]
	Transform _indicator;
    [SerializeField]
    Transform _fireSocket;

    public Transform Indicator {  get { return _indicator; } }
    public Collider2D BodyCollider => _bodyCollider;
    public Collider2D CombatCollider => _combatCollider;
    public Vector3 FireSocket { get { return _fireSocket == null ? transform.position : _fireSocket.position; } }
    public Vector3 ShootDir
    {
        get
        {
            if (_fireSocket == null || _indicator == null)
                return Vector3.up;

            return (_fireSocket.position - _indicator.position).normalized;
        }
    }

    public Vector2 MoveDirection => _moveDir;

	public override bool Init()
	{
        bool initialized = base.Init();
        if (initialized)
        {
            EnsureRuntimeComponents();

        }
		return true;
	}

    public override void ResetForSpawn()
    {
        EnsureRuntimeComponents();
        ObjectType = Define.ObjectType.Player;
        UnitData commanderData = Services.App.Data.GetUnit("commander_01");
        if (commanderData != null)
        {
            MaxHp = commanderData.Hp;
            Hp = commanderData.Hp;
            _speed = commanderData.MoveSpeed;
            EnvCollectDist = commanderData.AbsorbRange;
}
        else
        {
            _speed = 5.0f;
        }

        _loggedLowHp30 = false;
        _loggedLowHp10 = false;
        _invulnerableUntil = 0.0f;
        _nextDamageTimeBySource.Clear();
        _moveDir = Vector2.zero;
        if (_body != null)
        {
            _body.simulated = true;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0.0f;
        }

        EnsureCommanderAttack();
        EnsureCommanderHealthBar();
        PixelFantasyVisualBridge.ApplyCommanderVisual(gameObject);
        ValidateCommanderHurtbox();
    }

    void CollectEnv()
    {
        if (_gridController == null)
            return;

        float sqrCollectDist = EnvCollectDist * EnvCollectDist;
        _gridController.GatherGems(transform.position, EnvCollectDist + 0.5f, _collectBuffer);

        foreach (GemController gem in _collectBuffer)
        {
            if (gem == null || gem.IsValid() == false)
                continue;

            if (gem.CanPickup == false)
                continue;

            Vector3 dir = gem.transform.position - transform.position;
            if (dir.sqrMagnitude <= sqrCollectDist)
            {
                float absorbScale = gem.SourceEnemyId == CombatIds.EliteRedCharger ? 1.45f : 1.0f;
                RetroVfx.Spawn(RetroVfxKind.XpAbsorb, gem.transform.position, Vector3.zero, absorbScale);
                Services.State.AddExperience(1);
                P0Telemetry.Log(
                    P0Telemetry.ExpOrbAbsorb,
                    $"enemy_id={gem.SourceEnemyId}",
                    "actual_exp_reward=1",
                    $"source_reward_total={gem.SourceRewardTotal}",
                    "visual_only=false");

                Services.Registry.ReleaseGem(gem);
            }
        }
    }

    void EnsureRuntimeComponents()
    {
        _body = GetComponent<Rigidbody2D>();
        if (_body == null)
            Debug.LogError("Commander prefab is missing required Rigidbody2D.", this);

        _hitFlash = GetComponent<HitFlash>();

        if (_commanderVisual == null)
            _commanderVisual = GetComponent<CommanderAllyVisual>();
        if (_commanderVisual == null)
            Debug.LogError("Commander prefab is missing required CommanderAllyVisual.", this);

        if (_body != null)
        {
            _body.gravityScale = 0.0f;
            _body.freezeRotation = true;
        }

        EnsureCommanderHurtbox();
        ValidateCommanderBodyCollider();

        if (_indicator == null)
            Debug.LogError("Commander prefab is missing required child: @Indicator", this);

        if (_fireSocket == null)
            Debug.LogError("Commander prefab is missing required child: @FireSocket", this);

        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].sortingOrder = SortingOrder.Unit;
        }
    }

    public void BindGrid(GridController gridController)
    {
        _gridController = gridController;
    }

    void EnsureCommanderAttack()
    {
        if (_commanderAttack == null)
            _commanderAttack = GetComponent<CommanderAttack>();

        if (_commanderAttack == null)
        {
            Debug.LogError("Commander prefab is missing required CommanderAttack.", this);
            return;
        }

        _commanderAttack.Setup(this);
    }

    void EnsureCommanderHealthBar()
    {
        if (_commanderHealthBar == null)
            _commanderHealthBar = GetComponent<CommanderHealthBar>();

        if (_commanderHealthBar == null)
        {
            Debug.LogError("Commander prefab is missing required CommanderHealthBar.", this);
            return;
        }

        _commanderHealthBar.Refresh(this);
    }

    void RefreshCommanderHealthBar()
    {
        if (_commanderHealthBar == null)
            EnsureCommanderHealthBar();

        _commanderHealthBar.Refresh(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        MonsterController target = collision.gameObject.GetComponentInParent<MonsterController>();
        if (target == null)
            return;
    }

    public override void OnDamaged(BaseController attacker, int damage)
    {
        TryApplyDamage(attacker as MonsterController, damage);
    }

    public bool TryApplyBossPatternDamage(MonsterController attacker, int damage)
    {
        return TryApplyDamage(attacker, damage);
    }

    public bool TryApplyEnemyPatternDamage(MonsterController attacker, int damage, string patternId)
    {
        return TryApplyDamage(attacker, damage, patternId);
    }

    bool TryApplyDamage(MonsterController monster, int damage, string overridePatternId = null)
    {
        if (damage <= 0)
            return false;

        string enemyId = monster == null ? CombatIds.Unknown : monster.GetDamageEnemyId();
        string patternId = string.IsNullOrEmpty(overridePatternId)
            ? monster == null ? CombatIds.Unknown : monster.GetDamagePatternId()
            : overridePatternId;
        string sourceKey = ResolveDamageSourceKey(monster, patternId);

        if (monster != null)
        {
            P0PlaytestDiagnostics.RecordCommanderHurtboxContact(enemyId, patternId);
            P0Telemetry.Log(P0Telemetry.HurtboxContact, "target=commander", $"enemy_id={enemyId}", $"pattern_id={patternId}");
        }

        if (_nextDamageTimeBySource.TryGetValue(sourceKey, out float nextSourceDamageTime)
            && Time.time < nextSourceDamageTime)
        {
            if (CombatIds.IsBossPattern(patternId))
            {
                P0Telemetry.Log(
                    P0Telemetry.BossPatternRepeatBlock,
                    "target=commander",
                    $"enemy_id={enemyId}",
                    $"pattern_id={patternId}",
                    $"remaining={nextSourceDamageTime - Time.time:0.##}");
            }

            P0Telemetry.Log(
                P0Telemetry.DamageBlockedCooldown,
                "target=commander",
                $"enemy_id={enemyId}",
                $"pattern_id={patternId}",
                $"remaining={nextSourceDamageTime - Time.time:0.##}");
            return false;
        }

        if (Time.time < _invulnerableUntil)
        {
            P0Telemetry.Log(
                P0Telemetry.DamageBlockedInvulnerable,
                "target=commander",
                $"enemy_id={enemyId}",
                $"pattern_id={patternId}",
                $"remaining={_invulnerableUntil - Time.time:0.##}");
            return false;
        }

        if (monster != null)
            P0DeathReasonTracker.RecordEnemyDamage(monster, patternId);

        base.OnDamaged(monster, damage);
        FloatingDamageText.ShowFriendlyDamage(transform.position, damage);
        P0PlaytestDiagnostics.RecordCommanderDamage(enemyId, patternId, damage, GetHpPercent());
        P0Telemetry.Log(P0Telemetry.CommanderDamage, $"damage={damage}", $"enemy_id={enemyId}", $"pattern_id={patternId}", $"hp_percent={GetHpPercent()}");
        P0Telemetry.Log(P0Telemetry.DamageApply, "target=commander", $"damage={damage}", $"enemy_id={enemyId}", $"pattern_id={patternId}");
        if (enemyId == CombatIds.EliteRedCharger && patternId == CombatIds.RedChargerImpactGrace)
            P0Telemetry.Log(P0Telemetry.RedChargerImpactGraceHit, $"damage={damage}", $"hp_percent={GetHpPercent()}");
        else if (enemyId == CombatIds.EliteRedCharger && patternId == CombatIds.RedChargerDash)
            P0Telemetry.Log(P0Telemetry.RedChargerImpactHit, $"damage={damage}", $"pattern_id={patternId}", $"hp_percent={GetHpPercent()}");
        if (CombatIds.IsBossPattern(patternId))
            P0Telemetry.Log(P0Telemetry.BossPatternHit, "target=commander", $"pattern_id={patternId}", $"damage={damage}");
        if (monster != null)
            P0PlaytestDiagnostics.RecordEnemyContactDamage(monster);

        HitFlash flash = _hitFlash;
        if (flash == null)
        {
            Debug.LogError("Commander prefab is missing required HitFlash.", this);
            return true;
        }
        flash.Play();

        if (monster != null)
        {
            _invulnerableUntil = Time.time + RemoteConfig.CommanderPostHitInvuln;
            _nextDamageTimeBySource[sourceKey] = Time.time + RemoteConfig.ContactDamageSourceCooldown;
            P0Telemetry.Log(P0Telemetry.CommanderInvulnStart, $"duration={RemoteConfig.CommanderPostHitInvuln:0.##}");
        }

        LogCommanderLowHp();
        return true;
    }

    string ResolveDamageSourceKey(MonsterController monster, string patternId = null)
    {
        if (monster == null)
            return CombatIds.Unknown;

        if (string.IsNullOrEmpty(patternId))
            return monster.GetDamageSourceKey();

        return CombatIds.DamageCooldownKey(monster.GetDamageEnemyId(), monster.GetInstanceID(), patternId);
    }

    int GetHpPercent()
    {
        if (MaxHp <= 0)
            return 0;

        return Mathf.Clamp(Mathf.RoundToInt((float)Hp / MaxHp * 100.0f), 0, 100);
    }

    void LogCommanderLowHp()
    {
        if (MaxHp <= 0 || Hp <= 0)
            return;

        int hpPercent = Mathf.CeilToInt((float)Hp / MaxHp * 100.0f);
        if (_loggedLowHp30 == false && hpPercent <= 30)
        {
            _loggedLowHp30 = true;
            P0Telemetry.Log(P0Telemetry.CommanderLowHp, "threshold=30", $"hp_percent={hpPercent}");
        }

        if (_loggedLowHp10 == false && hpPercent <= 10)
        {
            _loggedLowHp10 = true;
            P0Telemetry.Log(P0Telemetry.CommanderLowHp, "threshold=10", $"hp_percent={hpPercent}");
        }
    }

    protected override void OnDead()
    {
        FindFirstObjectByType<GameScene>()?.ShowFailureResult(HungryGiantBehaviour.GetCurrentHpPercent());
    }

    void EnsureCommanderHurtbox()
    {
        if (_combatCollider == null)
        {
            Debug.LogError("Commander prefab is missing required CombatCollider reference.", this);
            return;
        }

        if (_combatCollider.isTrigger == false)
            Debug.LogError("Commander CombatCollider must be trigger.", this);
    }

    void ValidateCommanderBodyCollider()
    {
        if (_bodyCollider == null)
        {
            Debug.LogError("Commander prefab is missing required BodyCollider reference.", this);
            return;
        }

        if (_bodyCollider.isTrigger)
            Debug.LogError("Commander BodyCollider must not be trigger.", this);
    }

    void ValidateCommanderHurtbox()
    {
        if (_combatCollider == null)
            EnsureCommanderHurtbox();

        if (_combatCollider == null)
            return;

        if (_combatCollider.enabled == false)
            Debug.LogError("Commander CombatCollider is disabled.", this);
    }

    public bool IsHurtboxOverlappingCircle(Vector2 circleCenter, float circleRadius)
    {
        if (_combatCollider == null)
            EnsureCommanderHurtbox();

        if (_combatCollider == null || _combatCollider.enabled == false)
            return false;

        Vector2 hurtboxCenter = _combatCollider.transform.TransformPoint(_combatCollider.offset);
        float maxScale = Mathf.Max(
            Mathf.Abs(_combatCollider.transform.lossyScale.x),
            Mathf.Abs(_combatCollider.transform.lossyScale.y));
        float hurtboxRadius = _combatCollider.radius * maxScale;
        float overlapDistance = Mathf.Max(0.0f, circleRadius) + hurtboxRadius;
        return (hurtboxCenter - circleCenter).sqrMagnitude <= overlapDistance * overlapDistance;
    }

    public bool IsHurtboxOverlappingCapsule(Vector2 segmentStart, Vector2 segmentEnd, float radius)
    {
        if (_combatCollider == null)
            EnsureCommanderHurtbox();

        if (_combatCollider == null || _combatCollider.enabled == false)
            return false;

        Vector2 hurtboxCenter = _combatCollider.transform.TransformPoint(_combatCollider.offset);
        float maxScale = Mathf.Max(
            Mathf.Abs(_combatCollider.transform.lossyScale.x),
            Mathf.Abs(_combatCollider.transform.lossyScale.y));
        float hurtboxRadius = _combatCollider.radius * maxScale;
        float overlapDistance = Mathf.Max(0.0f, radius) + hurtboxRadius;
        Vector2 closestPoint = GetClosestPointOnSegment(segmentStart, segmentEnd, hurtboxCenter);
        return (hurtboxCenter - closestPoint).sqrMagnitude <= overlapDistance * overlapDistance;
    }

    static Vector2 GetClosestPointOnSegment(Vector2 segmentStart, Vector2 segmentEnd, Vector2 point)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float lengthSqr = segment.sqrMagnitude;
        if (lengthSqr <= 0.000001f)
            return segmentStart;

        float t = Vector2.Dot(point - segmentStart, segment) / lengthSqr;
        return segmentStart + segment * Mathf.Clamp01(t);
    }

    void OnDrawGizmos()
    {
        CircleCollider2D collider = _combatCollider;
        P0CombatDebugSettings.DrawCollider2D(collider, new Color(0.1f, 0.75f, 1.0f, 1.0f));
    }

    public void SetMoveDirection(Vector2 direction)
    {
        _moveDir = direction.normalized;
    }

    void Update()
    {
        CollectEnv();
        UpdateCommanderRunAnimator();
        RefreshCommanderHealthBar();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        Vector2 movement = _moveDir * _speed * Time.fixedDeltaTime;
        if (_body != null)
        {
            _body.MovePosition(_body.position + movement);
            _body.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position += new Vector3(movement.x, movement.y, 0.0f);
        }

        if (_moveDir != Vector2.zero && _indicator != null)
            _indicator.eulerAngles = new Vector3(0, 0, Mathf.Atan2(-movement.x, movement.y) * 180 / Mathf.PI);
    }

    void UpdateCommanderRunAnimator()
    {
        if (_commanderVisual == null)
            _commanderVisual = GetComponent<CommanderAllyVisual>();

        _commanderVisual?.SetMoveInput(_moveDir);
    }

    public void PlayAttackPose(Vector3 worldDirection, float holdSeconds = 0.28f)
    {
        Vector2 direction = new Vector2(worldDirection.x, worldDirection.y);
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float duration = Mathf.Max(0.05f, holdSeconds);
        if (_commanderVisual == null)
            _commanderVisual = GetComponent<CommanderAllyVisual>();

        _commanderVisual?.PlayAttack(direction, duration);
    }

}
