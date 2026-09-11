using Lizzo.PV.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Units;

namespace Lizzo.PV.Gameplay.Units
{
public partial class CommanderActor
{
    public event System.Action<int> DamageApplied;
    public event System.Action<int> HealingResolved;
    internal void NotifyDamageApplied(int damage) => DamageApplied?.Invoke(damage);
    internal void NotifyHealingResolved(int amount) => HealingResolved?.Invoke(amount);

    public void OnDamaged(UnityEngine.Component attacker, int damage)
    {
        TryApplyDamage(attacker as EnemyActor, damage);
    }

    CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => CombatImmediateHitFaction.Ally;
    bool ICombatImmediateHitTarget.IsAlive => this != null && isActiveAndEnabled && Hp > 0;

    public bool TryReceiveImmediateHit(in CombatImmediateHitRequest request)
    {
        if (request.Mode != CombatImmediateHitMode.EnemyContact)
            return false;

        return TryApplyDamage(request.Source as EnemyActor, request.Damage, request.EnemyPatternId);
    }

    public bool TryApplyBossPatternDamage(EnemyActor attacker, int damage)
    {
        return TryApplyDamage(attacker, damage);
    }

    public bool TryApplyEnemyPatternDamage(EnemyActor attacker, int damage, string patternId)
    {
        return TryApplyDamage(attacker, damage, patternId);
    }

    bool TryApplyDamage(EnemyActor monster, int damage, string overridePatternId = null)
    {
        if (RunPauseController.IsResultGameplayLocked)
            return false;

        if (_synergies?.TryInterceptCommanderDamage(monster) == true)
            return false;

        EnsureDamageReceiver();
        return _damageReceiver.TryApply(monster, damage, overridePatternId);
    }

    internal void ApplyDamageFromReceiver(EnemyActor monster, int damage)
    {
        if (_health.ApplyDamageAndCheckDeath(damage))
            OnDead();
    }

#if UNITY_EDITOR
    public void SetEditorAutomationInfiniteHp(bool enabled)
    {
        EnsureDamageReceiver();
        _damageReceiver.SetEditorAutomationInfiniteHp(enabled);
    }

    public bool EditorAutomationInfiniteHpEnabled => _damageReceiver != null && _damageReceiver.EditorAutomationInfiniteHpEnabled;
#endif

    private void OnDead()
    {
        FindFirstObjectByType<GameScene>()?.ShowFailureResult();
    }

}

}
