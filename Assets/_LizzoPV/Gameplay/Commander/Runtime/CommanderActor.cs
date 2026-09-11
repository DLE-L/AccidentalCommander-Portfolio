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

namespace Lizzo.PV.Gameplay.Units
{
public partial class CommanderActor : MonoBehaviour, ICombatImmediateHitTarget
{
    public Define.ObjectType ObjectType { get; private set; }
    private bool _initialized;
    private void Awake() => Init();
    private readonly Lizzo.PV.Combat.CombatHealthState _health = new Lizzo.PV.Combat.CombatHealthState();
    private float _speed = 1f;
    public int Hp => _health.Current;
    public int MaxHp => _health.Maximum;
    public bool IsPostHitInvulnerable(float time) => _damageReceiver != null && _damageReceiver.IsInvulnerable(time);
    public void RestoreHealth(int current, int? maximum = null) => _health.Restore(current, maximum ?? MaxHp);
    public void ResetHealth(int maximum) => _health.Reset(maximum);
    public int Heal(int amount) => _health.Heal(amount);
    public float MoveSpeed => _speed;
    public void SetMoveSpeed(float speed) => _speed = speed;
    Rigidbody2D _body;
    CommanderHealthBar _commanderHealthBar;
    CommanderAllyVisual _commanderVisual;
    CommanderDamageReceiver _damageReceiver;
    CommanderHurtbox _hurtbox;
    CommanderMovementMotor _movementMotor;
    ArenaBounds _arenaBounds;
    CommanderGemCollector _gemCollector;
    PassiveRosterState _passiveRoster;
    CompanionPassiveCombatResolver _passiveResolver;
    CommanderPassiveModifiers _passiveModifiers;
    bool _bodyCacheResolved;
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
	public bool Init()
	{
        bool initialized = !_initialized;
        _initialized = true;
        if (initialized)
            EnsureRuntimeComponents();

		return initialized;
	}

    public void ResetForSpawn()
    {
        EnsureRuntimeComponents();
        ObjectType = Define.ObjectType.Player;
        UnitData commanderData = _data.GetUnit("commander_01");
        _gemCollector.ResetExperienceBonusRemainder();
        if (commanderData != null)
        {
            ResetHealth(commanderData.Hp);
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

        RefreshCommanderHealthBar();
        UnitVisualAuthoringValidator.ValidateCommanderVisual(gameObject);
        ValidateCommanderHurtbox();
    }

}

}
