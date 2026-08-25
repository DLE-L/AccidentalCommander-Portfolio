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


    public void PlayAttackPose(Vector3 worldDirection, float holdSeconds = 0.28f)
    {
        Vector2 direction = new Vector2(worldDirection.x, worldDirection.y);
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float duration = Mathf.Max(0.05f, holdSeconds);
        CacheCommanderVisual()?.PlayAttack(direction, duration);
    }

}
