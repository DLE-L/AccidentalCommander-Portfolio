using Lizzo.PV.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Units;

public partial class PlayerController
{
    public override void OnDamaged(BaseController attacker, int damage)
    {
        TryApplyDamage(attacker as MonsterController, damage);
    }

    CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => CombatImmediateHitFaction.Ally;
    bool ICombatImmediateHitTarget.IsAlive => this != null && isActiveAndEnabled && Hp > 0;

    public void ReceiveImmediateHit(in CombatImmediateHitRequest request)
    {
        if (request.Mode != CombatImmediateHitMode.EnemyContact)
            return;

        TryApplyDamage(request.EnemySource, request.Damage, request.EnemyPatternId);
    }

    public bool TryApplyBossPatternDamage(MonsterController attacker, int damage)
    {
        return TryApplyDamage(attacker, damage);
    }

    public bool TryApplyEnemyPatternDamage(MonsterController attacker, int damage, string patternId)
    {
        return TryApplyDamage(attacker, damage, patternId);
    }

    bool TryApplyDamage(MonsterController monster, int damage, string overridePatternId = null)
    {
        if (RunPauseController.IsResultGameplayLocked)
            return false;

        EnsureDamageReceiver();
        return _damageReceiver.TryApply(monster, damage, overridePatternId);
    }

    internal void ApplyDamageFromReceiver(MonsterController monster, int damage)
    {
        base.OnDamaged(monster, damage);
    }

#if UNITY_EDITOR
    public void SetEditorAutomationInfiniteHp(bool enabled)
    {
        EnsureDamageReceiver();
        _damageReceiver.SetEditorAutomationInfiniteHp(enabled);
    }

    public bool EditorAutomationInfiniteHpEnabled => _damageReceiver != null && _damageReceiver.EditorAutomationInfiniteHpEnabled;
#endif

    protected override void OnDead()
    {
        FindFirstObjectByType<GameScene>()?.ShowFailureResult(HungryGiantBehaviour.GetCurrentHpPercent());
    }

}
