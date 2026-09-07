using Lizzo.PV.Gameplay.Commander;
using UnityEngine;

public partial class PlayerController
{
    void EnsureCommanderHurtbox()
    {
        ResolveCommanderHurtbox().ValidateRequired();
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
        ResolveCommanderHurtbox().ValidateEnabled();
    }

    public bool IsHurtboxOverlappingCircle(Vector2 circleCenter, float circleRadius)
    {
        return ResolveCommanderHurtbox().OverlapsCircle(circleCenter, circleRadius);
    }

    public bool IsHurtboxOverlappingCapsule(Vector2 segmentStart, Vector2 segmentEnd, float radius)
    {
        return ResolveCommanderHurtbox().OverlapsCapsule(segmentStart, segmentEnd, radius);
    }

    CommanderHurtbox ResolveCommanderHurtbox()
    {
        if (_hurtbox == null || ReferenceEquals(_hurtbox.Collider, _combatCollider) == false)
            _hurtbox = new CommanderHurtbox(this, _combatCollider);

        return _hurtbox;
    }

}
