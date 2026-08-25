using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using UnityEngine;

public partial class PlayerController
{
    void OnDestroy()
    {
        if (_passiveRoster != null)
            _passiveRoster.Changed -= RefreshPassiveEffects;
    }

    void EnsureGemCollector()
    {
        if (_gemCollector != null || Services == null)
            return;

        _gemCollector = new CommanderGemCollector(Services.State, Services.Registry, Services.RunTraitEffects);
    }

    void BindPassiveEffects()
    {
        PassiveRosterState roster = Services == null ? null : Services.PassiveRoster;
        if (ReferenceEquals(_passiveRoster, roster))
            return;

        if (_passiveRoster != null)
            _passiveRoster.Changed -= RefreshPassiveEffects;
        _passiveRoster = roster;
        _passiveResolver = roster == null || Services == null ? null : Services.PassiveEffects;
        if (_passiveRoster != null)
            _passiveRoster.Changed += RefreshPassiveEffects;
    }

    public void RefreshPassiveEffects()
    {
        if (Services == null)
            return;

        UnitData commanderData = Services.App.Data.GetUnit("commander_01");
        if (commanderData == null)
            return;

        _passiveModifiers = _passiveResolver == null
            ? CommanderPassiveModifiers.Identity
            : _passiveResolver.ResolveCommander();
        int nextMaxHp = Mathf.Max(1, commanderData.Hp + _passiveModifiers.MaxHpBonus);
        Hp = CommanderPassiveHealth.ResolveCurrentHp(Hp, MaxHp, nextMaxHp);
        MaxHp = nextMaxHp;
        _speed = commanderData.MoveSpeed + _passiveModifiers.MoveSpeedBonus;
        EnsureGemCollector();
        _gemCollector.SetCollectDistance(commanderData.AbsorbRange + _passiveModifiers.AbsorbRadiusBonus);
        _gemCollector.SetExperienceMultiplier(_passiveModifiers.ExperienceMultiplier);
        CacheCommanderAttack()?.SetPassiveDamageBonus(_passiveModifiers.BasicDamageBonus);
        RefreshCommanderHealthBar();
    }

    public void BindGrid(GridController gridController)
    {
        EnsureGemCollector();
        _gemCollector?.BindGrid(gridController);
    }

}
