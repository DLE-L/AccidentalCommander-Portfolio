using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.P0.Cards;

public partial class PlayerController : CreatureController, ICombatImmediateHitTarget
{
    Rigidbody2D _body;
    CommanderAttack _commanderAttack;
    CommanderHealthBar _commanderHealthBar;
    CommanderAllyVisual _commanderVisual;
    HitFlash _hitFlash;
    CommanderDamageReceiver _damageReceiver;
    CommanderHurtbox _hurtbox;
    CommanderMovementMotor _movementMotor;
    ArenaBounds _arenaBounds;
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
        UnitVisualAuthoringValidator.ValidateCommanderVisual(gameObject);
        ValidateCommanderHurtbox();
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

        _movementMotor.BindArenaBounds(_arenaBounds);
        _movementMotor.ConfigureRigidbody();
    }

    void LogMissingCommanderHealthBar()
    {
        if (_commanderHealthBarMissingLogged)
            return;

        _commanderHealthBarMissingLogged = true;
        Debug.LogError("Commander prefab is missing required CommanderHealthBar.", this);
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

    public void BindArenaBounds(ArenaBounds arenaBounds)
    {
        _arenaBounds = arenaBounds ?? throw new System.ArgumentNullException(nameof(arenaBounds));
        EnsureMovementMotor();
    }

}
