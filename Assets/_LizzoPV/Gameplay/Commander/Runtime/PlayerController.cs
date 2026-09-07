using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Gameplay.CardOffer;

public partial class PlayerController : CreatureController, ICombatImmediateHitTarget
{
    Rigidbody2D _body;
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

        EnsureCommanderHealthBar();
        UnitVisualAuthoringValidator.ValidateCommanderVisual(gameObject);
        ValidateCommanderHurtbox();
    }

}
