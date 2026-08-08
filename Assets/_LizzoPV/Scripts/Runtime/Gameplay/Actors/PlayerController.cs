using Lizzo.PV.P0.Debugging;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.P0.Cards;

public class PlayerController : CreatureController, ICombatImmediateHitTarget
{
    Rigidbody2D _body;
    CommanderAttack _commanderAttack;
    CommanderHealthBar _commanderHealthBar;
    CommanderAllyVisual _commanderVisual;
    HitFlash _hitFlash;
    CommanderDamageReceiver _damageReceiver;
    CommanderMovementMotor _movementMotor;
    CommanderGemCollector _gemCollector;
    PassiveRosterState _passiveRoster;
    CompanionPassiveCombatResolver _passiveResolver;
    CommanderPassiveModifiers _passiveModifiers;
    bool _bodyCacheResolved;
    bool _hitFlashCacheResolved;
    bool _commanderAttackCacheResolved;
    bool _commanderHealthBarCacheResolved;
    bool _commanderVisualCacheResolved;
    bool _commanderHealthBarMissingLogged;
    [SerializeField]
    CircleCollider2D _bodyCollider;
    [SerializeField]
    CircleCollider2D _combatCollider;

	[SerializeField]
	Transform _indicator;
    [SerializeField]
    Transform _fireSocket;

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

    public Vector2 MoveDirection => _movementMotor == null ? Vector2.zero : _movementMotor.Direction;
    public CommanderPassiveModifiers PassiveModifiers => _passiveModifiers;

	public override bool Init()
	{
        bool initialized = base.Init();
        if (initialized)
            EnsureRuntimeComponents();

		return initialized;
	}

    public override void ResetForSpawn()
    {
        EnsureRuntimeComponents();
        ObjectType = Define.ObjectType.Player;
        UnitData commanderData = Services.App.Data.GetUnit("commander_01");
        EnsureGemCollector();
        _gemCollector.ResetExperienceBonusRemainder();
        if (commanderData != null)
        {
            MaxHp = commanderData.Hp;
            Hp = commanderData.Hp;
            _speed = commanderData.MoveSpeed;
            _gemCollector.SetCollectDistance(commanderData.AbsorbRange);
}
        else
        {
            _speed = 5.0f;
        }

        BindPassiveEffects();
        RefreshPassiveEffects();

        EnsureDamageReceiver();
        _damageReceiver.ResetForSpawn();
        EnsureMovementMotor();
        _movementMotor.ResetForSpawn();

        EnsureCommanderAttack();
        EnsureCommanderHealthBar();
        PixelFantasyVisualBridge.ApplyCommanderVisual(gameObject);
        ValidateCommanderHurtbox();
    }

    void OnDestroy()
    {
        if (_passiveRoster != null)
            _passiveRoster.Changed -= RefreshPassiveEffects;
    }

    public bool RestoreFullHealth()
    {
        if (MaxHp <= 0)
            return false;

        Hp = MaxHp;
        EnsureDamageReceiver();
        _damageReceiver.ResetLowHpWarnings();
        RefreshCommanderHealthBar();
        return true;
    }


    void EnsureRuntimeComponents()
    {
        Rigidbody2D body = CacheBody();
        if (body == null)
            Debug.LogError("Commander prefab is missing required Rigidbody2D.", this);

        CacheHitFlash();
        EnsureDamageReceiver();

        CommanderAllyVisual commanderVisual = CacheCommanderVisual();
        if (commanderVisual == null)
            Debug.LogError("Commander prefab is missing required CommanderAllyVisual.", this);

        EnsureMovementMotor(body);

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

        CacheCommanderHealthBar()?.ApplyVisualOrdering();
    }

    Rigidbody2D CacheBody()
    {
        if (_bodyCacheResolved == false)
        {
            _body = GetComponent<Rigidbody2D>();
            _bodyCacheResolved = true;
        }

        return _body;
    }

    HitFlash CacheHitFlash()
    {
        if (_hitFlashCacheResolved == false)
        {
            _hitFlash = GetComponent<HitFlash>();
            _hitFlashCacheResolved = true;
        }

        return _hitFlash;
    }

    void EnsureDamageReceiver()
    {
        _damageReceiver ??= new CommanderDamageReceiver(this, CacheHitFlash());
    }

    CommanderAllyVisual CacheCommanderVisual()
    {
        if (_commanderVisualCacheResolved == false)
        {
            _commanderVisual = GetComponent<CommanderAllyVisual>();
            _commanderVisualCacheResolved = true;
        }

        return _commanderVisual;
    }

    CommanderAttack CacheCommanderAttack()
    {
        if (_commanderAttackCacheResolved == false)
        {
            _commanderAttack = GetComponent<CommanderAttack>();
            _commanderAttackCacheResolved = true;
        }

        return _commanderAttack;
    }

    CommanderHealthBar CacheCommanderHealthBar()
    {
        if (_commanderHealthBarCacheResolved == false)
        {
            _commanderHealthBar = GetComponent<CommanderHealthBar>();
            _commanderHealthBarCacheResolved = true;
        }

        return _commanderHealthBar;
    }

    void EnsureMovementMotor()
    {
        EnsureMovementMotor(CacheBody());
    }

    void EnsureMovementMotor(Rigidbody2D body)
    {
        if (_movementMotor == null)
            _movementMotor = new CommanderMovementMotor(transform, body, _indicator);

        _movementMotor.ConfigureRigidbody();
    }

    void EnsureGemCollector()
    {
        if (_gemCollector != null || Services == null)
            return;

        _gemCollector = new CommanderGemCollector(Services.State, Services.Registry, Services.RunTraitEffects);
    }

    void BindPassiveEffects()
    {
        PassiveRosterState roster = Services == null ? null : Services.PassiveRoster;
        if (ReferenceEquals(_passiveRoster, roster))
            return;

        if (_passiveRoster != null)
            _passiveRoster.Changed -= RefreshPassiveEffects;
        _passiveRoster = roster;
        _passiveResolver = roster == null || Services == null ? null : Services.PassiveEffects;
        if (_passiveRoster != null)
            _passiveRoster.Changed += RefreshPassiveEffects;
    }

    public void RefreshPassiveEffects()
    {
        if (Services == null)
            return;

        UnitData commanderData = Services.App.Data.GetUnit("commander_01");
        if (commanderData == null)
            return;

        _passiveModifiers = _passiveResolver == null
            ? CommanderPassiveModifiers.Identity
            : _passiveResolver.ResolveCommander();
        int nextMaxHp = Mathf.Max(1, commanderData.Hp + _passiveModifiers.MaxHpBonus);
        Hp = CommanderPassiveHealth.ResolveCurrentHp(Hp, MaxHp, nextMaxHp);
        MaxHp = nextMaxHp;
        _speed = commanderData.MoveSpeed + _passiveModifiers.MoveSpeedBonus;
        EnsureGemCollector();
        _gemCollector.SetCollectDistance(commanderData.AbsorbRange + _passiveModifiers.AbsorbRadiusBonus);
        _gemCollector.SetExperienceMultiplier(_passiveModifiers.ExperienceMultiplier);
        CacheCommanderAttack()?.SetPassiveDamageBonus(_passiveModifiers.BasicDamageBonus);
        RefreshCommanderHealthBar();
    }

    void LogMissingCommanderHealthBar()
    {
        if (_commanderHealthBarMissingLogged)
            return;

        _commanderHealthBarMissingLogged = true;
        Debug.LogError("Commander prefab is missing required CommanderHealthBar.", this);
    }

    public void BindGrid(GridController gridController)
    {
        EnsureGemCollector();
        _gemCollector?.BindGrid(gridController);
    }

    void EnsureCommanderAttack()
    {
        CommanderAttack commanderAttack = CacheCommanderAttack();
        if (commanderAttack == null)
        {
            Debug.LogError("Commander prefab is missing required CommanderAttack.", this);
            return;
        }

        commanderAttack.Setup(this);
    }

    void EnsureCommanderHealthBar()
    {
        CommanderHealthBar commanderHealthBar = CacheCommanderHealthBar();
        if (commanderHealthBar == null)
        {
            LogMissingCommanderHealthBar();
            return;
        }

        commanderHealthBar.Refresh(this);
    }

    void RefreshCommanderHealthBar()
    {
        CommanderHealthBar commanderHealthBar = CacheCommanderHealthBar();
        if (commanderHealthBar == null)
        {
            LogMissingCommanderHealthBar();
            return;
        }

        commanderHealthBar.Refresh(this);
    }

    public override void OnDamaged(BaseController attacker, int damage)
    {
        TryApplyDamage(attacker as MonsterController, damage);
    }

    CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => CombatImmediateHitFaction.Ally;
    bool ICombatImmediateHitTarget.IsAlive => this != null && isActiveAndEnabled && Hp > 0;

    public void ReceiveImmediateHit(in CombatImmediateHitRequest request)
    {
        if (request.Mode != CombatImmediateHitMode.EnemyContact)
            return;

        TryApplyDamage(request.EnemySource, request.Damage, request.EnemyPatternId);
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
        if (RunPauseController.IsResultGameplayLocked)
            return false;

        EnsureDamageReceiver();
        return _damageReceiver.TryApply(monster, damage, overridePatternId);
    }

    internal void ApplyDamageFromReceiver(MonsterController monster, int damage)
    {
        base.OnDamaged(monster, damage);
    }

#if UNITY_EDITOR
    public void SetEditorAutomationInfiniteHp(bool enabled)
    {
        EnsureDamageReceiver();
        _damageReceiver.SetEditorAutomationInfiniteHp(enabled);
    }

    public bool EditorAutomationInfiniteHpEnabled => _damageReceiver != null && _damageReceiver.EditorAutomationInfiniteHpEnabled;
#endif

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
        if (RunPauseController.IsResultGameplayLocked)
        {
            _movementMotor?.SetDirection(Vector2.zero);
            return;
        }

        EnsureMovementMotor();
        _movementMotor.SetDirection(direction);
    }

    void Update()
    {
        if (RunPauseController.IsResultGameplayLocked)
            return;

        _gemCollector?.Collect(transform.position);
        UpdateCommanderRunAnimator();
        RefreshCommanderHealthBar();
    }

    void FixedUpdate()
    {
        if (RunPauseController.IsResultGameplayLocked)
            return;

        EnsureMovementMotor();
        _movementMotor.Advance(_speed, Time.fixedDeltaTime);
    }

    void UpdateCommanderRunAnimator()
    {
        CacheCommanderVisual()?.SetMoveInput(MoveDirection);
    }

    public void PlayAttackPose(Vector3 worldDirection, float holdSeconds = 0.28f)
    {
        Vector2 direction = new Vector2(worldDirection.x, worldDirection.y);
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float duration = Mathf.Max(0.05f, holdSeconds);
        CacheCommanderVisual()?.PlayAttack(direction, duration);
    }

}
