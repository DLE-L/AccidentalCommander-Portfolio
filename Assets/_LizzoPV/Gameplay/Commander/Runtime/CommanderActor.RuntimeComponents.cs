using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
public partial class CommanderActor
{
    void EnsureRuntimeComponents()
    {
        Rigidbody2D body = CacheBody();
        if (body == null)
            Debug.LogError("Commander prefab is missing required Rigidbody2D.", this);
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

    void EnsureDamageReceiver()
    {
        _damageReceiver ??= new CommanderDamageReceiver(this);
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

}

}
