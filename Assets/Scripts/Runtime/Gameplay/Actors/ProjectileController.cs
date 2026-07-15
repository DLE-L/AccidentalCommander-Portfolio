using Lizzo.PV.Gameplay.World;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

public class ProjectileController : BaseController, IVisibilityCullTarget
{
    const float ProjectileSpriteAngleOffset = -135.0f;

    CreatureController _owner;
    [SerializeField] private Collider2D _hitCollider;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private VisibilityCullProbe _visibilityProbe;
    Vector3 _moveDir;
    float _speed = 10.0f;
    float _lifeTime = 10.0f;
    float _despawnAt;
    int _damage;

	public override bool Init()
	{
		base.Init();
		return true;
	}

    public override void ResetForSpawn()
    {
        ObjectType = Define.ObjectType.Projectile;
        _owner = null;
        _moveDir = Vector3.zero;
        _damage = 0;
        _despawnAt = 0.0f;
    }

	public void Initialize(CreatureController owner, Vector3 moveDir, int damage)
	{
		_owner = owner;
		_moveDir = moveDir.normalized;
		_damage = Mathf.Max(0, damage);
        _despawnAt = Time.time + _lifeTime;
        if (!ValidateAuthoredReferences())
            return;

        _visibilityProbe.Bind(this);
        ConfigureProjectileFacing();
	}

	public override void UpdateController()
	{
		base.UpdateController();

        if (Time.time >= _despawnAt)
        {
            Services.Registry.ReleaseProjectile(this);
            return;
        }

		transform.position += _moveDir * _speed * Time.deltaTime;
	}

    public void OnVisibilityEnter(CameraVisibilityZone zone)
    {
    }

    public void OnVisibilityExit(CameraVisibilityZone zone)
    {
        if (this.IsValid())
            Services.Registry.ReleaseProjectile(this);
    }

	void OnTriggerEnter2D(Collider2D collision)
	{
		MonsterController mc = collision.gameObject.GetComponentInParent<MonsterController>();
		if (mc == null || mc.IsValid() == false)
			return;
		if (this.IsValid() == false)
			return;
		if (_damage <= 0)
		{
			Services.Registry.ReleaseProjectile(this);
			return;
		}

        int beforeHp = mc.Hp;
        if (_owner is PlayerController)
            P0BossDpsTracker.RecordBossDamage(CombatIds.Commander, mc, _damage);
		mc.OnDamagedFromPosition(transform.position, _damage, _owner is PlayerController ? CombatIds.Commander : CombatIds.Projectile);
		RetroVfx.Spawn(RetroVfxKind.ProjectileHit, transform.position, _moveDir, 1.0f);
		if (_owner is PlayerController)
			CommanderAttack.DebugRecordProjectileHit(beforeHp > 0 && mc.Hp <= 0);

		Services.Registry.ReleaseProjectile(this);
	}

    bool ValidateAuthoredReferences()
    {
        if (_hitCollider == null || _visualRoot == null || _visibilityProbe == null)
        {
            Debug.LogError("Commander projectile prefab requires authored hit collider, visual root, and visibility probe references.", this);
            return false;
        }

        if (_hitCollider.isTrigger == false)
        {
            Debug.LogError("Commander projectile hit collider must be trigger.", this);
            return false;
        }

        return true;
    }

    void ConfigureProjectileFacing()
    {
        if (_moveDir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(_moveDir.y, _moveDir.x) * Mathf.Rad2Deg + ProjectileSpriteAngleOffset;
            _visualRoot.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);
        }
    }
}
