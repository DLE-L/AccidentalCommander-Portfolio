using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.CardOffer;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
public partial class CommanderActor
{
    void OnDestroy()
    {
        if (_passiveRoster != null)
            _passiveRoster.Changed -= RefreshPassiveEffects;
    }

    void BindPassiveEffects()
    {
        PassiveRosterState roster = _sourcePassiveRoster;
        if (ReferenceEquals(_passiveRoster, roster))
            return;

        if (_passiveRoster != null)
            _passiveRoster.Changed -= RefreshPassiveEffects;
        _passiveRoster = roster;
        _passiveResolver = roster == null ? null : _sourcePassiveResolver;
        if (_passiveRoster != null)
            _passiveRoster.Changed += RefreshPassiveEffects;
    }

    public void RefreshPassiveEffects()
    {
        if (_data == null)
            return;

        UnitData commanderData = _data.GetUnit("commander_01");
        if (commanderData == null)
            return;

        _passiveModifiers = _passiveResolver == null
            ? CommanderPassiveModifiers.Identity
            : _passiveResolver.ResolveCommander();
        int nextMaxHp = Mathf.Max(1, Mathf.RoundToInt(
            commanderData.Hp * _passiveModifiers.MaxHpMultiplier));
        RestoreHealth(CommanderPassiveHealth.ResolveCurrentHp(Hp, MaxHp, nextMaxHp), nextMaxHp);
        _speed = commanderData.MoveSpeed * _passiveModifiers.MoveSpeedMultiplier;
        _gemCollector.SetExperienceMultiplier(_passiveModifiers.ExperienceMultiplier);
        RefreshCommanderHealthBar();
    }

}

}
