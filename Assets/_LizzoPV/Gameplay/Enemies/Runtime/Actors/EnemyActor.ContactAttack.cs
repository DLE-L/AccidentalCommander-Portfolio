using UnityEngine;
using Lizzo.PV.Flow;
namespace Lizzo.PV.Gameplay.Units
{
    public partial class EnemyActor
    {
        private EnemyContactAttack _contactAttack;
        private EnemyContactAttack ContactAttack => _contactAttack ??= new EnemyContactAttack(this);

        // Shared contact preparation, cooldown and resolution for ordinary and special-attack actors.
        // The actor owns identity, collision geometry, damage routing and presentation.
        private sealed class EnemyContactAttack
        {
            private readonly EnemyActor _actor;
            private float _nextAttackTime;
            private float _contactAttackReadyAt;
            internal EnemyContactAttack(EnemyActor actor) { _actor = actor; }
            internal bool IsReady => Time.time >= _nextAttackTime;
            internal void ScheduleNext(float cooldown) => _nextAttackTime = Time.time + cooldown;
            internal void Reset() { _nextAttackTime = 0f; _contactAttackReadyAt = 0f; }
public bool TryEnterContactAttack(float windupSeconds = 0.0f)
	{
		if (RunPauseController.IsResultGameplayLocked || _actor.IsExternalAttackRecovering)
			return false;

		CommanderActor player = _actor.Registry.Player;
		if (player == null)
			return false;

		if (_actor.CanUseContactAttack() == false)
			return false;

		if (_actor._usesExternalMovement && Time.time < _nextAttackTime)
			return false;

		if (_actor.IsTouchingPlayer(player) == false)
			return false;

		if (_actor._body != null)
			_actor._body.linearVelocity = Vector2.zero;

		_contactAttackReadyAt = Time.time + Mathf.Max(0.0f, windupSeconds);
		_actor.CreatureState = Define.CreatureState.Skill;
		if (_actor._usesExternalMovement)
			_actor.SetExternalAttackPreparationLocked(true);
		return true;
	}
public bool TryApplyContactDamageNow(bool recoverAfterAttack = false)
	{
		if (RunPauseController.IsResultGameplayLocked || _actor.IsExternalAttackRecovering)
			return false;

		CommanderActor player = _actor.Registry.Player;
		if (player == null || player.Hp <= 0)
			return false;

		if (_actor.CanUseContactAttack() == false)
			return false;

		if (_actor.IsTouchingPlayer(player) == false)
			return false;

		if (_actor._body != null)
			_actor._body.linearVelocity = Vector2.zero;

		Vector3 dir = player.transform.position - _actor.transform.position;
		_actor.UpdateFacing(dir);

		if (Time.time >= _nextAttackTime)
		{
			_actor.ApplyContactDamage(player, dir);
			if (recoverAfterAttack)
				_actor.BeginExternalAttackRecovery();
		}

		return true;
	}
public void Advance()
	{
		CommanderActor player = _actor.Registry.Player;
		if (player == null || player.Hp <= 0)
		{
			if (_actor._usesExternalMovement)
				_actor.SetExternalAttackPreparationLocked(false);
			return;
		}

		Vector3 dir = player.transform.position - _actor.transform.position;
		if (_actor.IsTouchingPlayer(player) == false)
		{
			if (_actor._usesExternalMovement)
				_actor.SetExternalAttackPreparationLocked(false);
			_actor.CreatureState = Define.CreatureState.Moving;
			return;
		}

		_actor.UpdateFacing(dir);
		if (Time.time < _contactAttackReadyAt || Time.time < _nextAttackTime)
			return;

		_actor.ApplyContactDamage(player, dir);
		if (_actor._usesExternalMovement)
		{
			_actor.SetExternalAttackPreparationLocked(false);
			_actor.CreatureState = Define.CreatureState.Moving;
			_actor.BeginExternalAttackRecovery();
		}
	}
        }
    }
}