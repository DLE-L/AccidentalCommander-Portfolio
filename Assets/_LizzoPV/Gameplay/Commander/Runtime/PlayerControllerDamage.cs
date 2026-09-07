using Lizzo.PV.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Units;

public partial class PlayerController
{
    public override void OnDamaged(BaseController attacker, int damage)
    {
        TryApplyDamage(attacker as MonsterController, damage);
    }

    CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => CombatImmediateHitFaction.Ally;
    bool ICombatImmediateHitTarget.IsAlive => this != null && isActiveAndEnabled && Hp > 0;

    public bool TryReceiveImmediateHit(in CombatImmediateHitRequest request)
    {
        if (request.Mode != CombatImmediateHitMode.EnemyContact)
            return false;

        return TryApplyDamage(request.EnemySource, request.Damage, request.EnemyPatternId);
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

        if (Services?.ProductionSynergies?.TryInterceptCommanderDamage(monster) == true)
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
        FindFirstObjectByType<GameScene>()?.ShowFailureResult();
    }

}
