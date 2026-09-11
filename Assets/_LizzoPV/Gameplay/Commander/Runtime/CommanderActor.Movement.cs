using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.World;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
public partial class CommanderActor
{
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

    public void BindArenaBounds(ArenaBounds arenaBounds)
    {
        _arenaBounds = arenaBounds ?? throw new System.ArgumentNullException(nameof(arenaBounds));
        EnsureMovementMotor();
    }

}

}
