using System;

namespace Lizzo.PV.Flow
{
    public interface ITutorialRecoveryApplicationTarget
    {
        int GetProgression(string baseUnitId);
        bool TryAdvanceCompanion(string baseUnitId);
        bool TryRestoreElapsedSeconds(float elapsedSeconds);
    }

    public static class TutorialRecoveryApplication
    {
        public static bool TryApply(
            TutorialRecoverySnapshot snapshot,
            ITutorialRecoveryApplicationTarget target)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            for (int index = 0; index < snapshot.Roster.Count; index++)
            {
                TutorialRecoveryRosterEntry entry = snapshot.Roster[index];
                int current = target.GetProgression(entry.BaseUnitId);
                if (current < 0 || current > entry.Progression)
                    return false;

                while (current < entry.Progression)
                {
                    if (target.TryAdvanceCompanion(entry.BaseUnitId) == false)
                        return false;

                    int next = target.GetProgression(entry.BaseUnitId);
                    if (next != current + 1)
                        return false;
                    current = next;
                }
            }

            return target.TryRestoreElapsedSeconds(snapshot.ElapsedSeconds);
        }
    }
}
