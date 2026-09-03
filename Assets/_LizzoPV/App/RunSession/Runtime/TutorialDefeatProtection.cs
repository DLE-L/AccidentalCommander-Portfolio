using System;

namespace Lizzo.PV.Flow
{
    public readonly struct TutorialDamageResolution
    {
        public TutorialDamageResolution(
            int appliedDamage,
            int recoveryHp,
            bool preventedDefeat)
        {
            AppliedDamage = appliedDamage;
            RecoveryHp = recoveryHp;
            PreventedDefeat = preventedDefeat;
        }

        public int AppliedDamage { get; }
        public int RecoveryHp { get; }
        public bool PreventedDefeat { get; }
    }

    public static class TutorialDefeatProtection
    {
        public static TutorialDamageResolution Resolve(
            RunContext context,
            int currentHp,
            int maxHp,
            int incomingDamage)
        {
            if (currentHp <= 0 || incomingDamage <= 0)
                return default;

            if (context.IsTutorial == false || incomingDamage < currentHp)
            {
                return new TutorialDamageResolution(
                    incomingDamage,
                    0,
                    preventedDefeat: false);
            }

            int appliedDamage = Math.Max(0, currentHp - 1);
            int recoveryHp = Math.Max(1, (Math.Max(1, maxHp) + 1) / 2);
            return new TutorialDamageResolution(
                appliedDamage,
                recoveryHp,
                preventedDefeat: true);
        }
    }
}
