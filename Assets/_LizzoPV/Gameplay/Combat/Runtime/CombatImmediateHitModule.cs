using Lizzo.PV.Flow;

namespace Lizzo.PV.Combat
{
    public sealed class CombatImmediateHitModule : ICombatImmediateHitModule
    {
        public event System.Action<CombatImmediateHitRequest> Applied;

        public bool TryApply(in CombatImmediateHitRequest request)
        {
            if (RunPauseController.IsResultGameplayLocked || request.IsValid == false)
                return false;

            if (request.Target.IsAlive == false)
                return false;

            if (request.Mode == CombatImmediateHitMode.AllyDirectTarget)
            {
                if (request.Faction != CombatImmediateHitFaction.Ally
                    || request.Target.Faction != CombatImmediateHitFaction.Enemy)
                    return false;
            }
            else if (request.Mode == CombatImmediateHitMode.EnemyContact)
            {
                if (request.Faction != CombatImmediateHitFaction.Enemy
                    || request.Target.Faction != CombatImmediateHitFaction.Ally)
                    return false;
            }
            else
            {
                return false;
            }

            request.Target.ReceiveImmediateHit(request);
            Applied?.Invoke(request);
            return true;
        }
    }
}
