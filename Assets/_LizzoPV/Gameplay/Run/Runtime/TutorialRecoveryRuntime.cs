using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    internal static class TutorialRecoveryRuntime
    {
        internal static bool TryRestore(RunServices services)
        {
            if (services == null)
                return false;
            if (services.Context.IsTutorial == false)
                return true;

            TutorialRecoverySnapshot snapshot = TutorialCheckpointRecovery.Resolve(
                TutorialCheckpointProgress.Current);
            bool restored = TutorialRecoveryApplication.TryApply(
                snapshot,
                new RunServicesTutorialRecoveryTarget(services));
            if (restored == false)
            {
                Debug.LogError(
                    $"[TutorialRecovery] Checkpoint restore failed: {snapshot.CheckpointId}.");
            }

            return restored;
        }
    }

    internal sealed class RunServicesTutorialRecoveryTarget : ITutorialRecoveryApplicationTarget
    {
        readonly RunServices _services;

        internal RunServicesTutorialRecoveryTarget(RunServices services)
        {
            _services = services;
        }

        public int GetProgression(string baseUnitId)
        {
            return _services.Party.TryGetCanonicalCompanionProgress(
                baseUnitId,
                out int ownedCount,
                out _)
                ? ownedCount
                : 0;
        }

        public bool TryAdvanceCompanion(string baseUnitId)
        {
            switch (baseUnitId)
            {
                case "shield_guard":
                    _services.Party.Recruit(CompanionKind.ShieldSoldier);
                    return true;
                case "sword_soldier":
                    _services.Party.Recruit(CompanionKind.Swordsman);
                    return true;
                case "cleric":
                    _services.Party.Recruit(CompanionKind.Cleric);
                    return true;
                case "falcon_archer":
                    _services.Party.Recruit(CompanionKind.Archer);
                    return true;
                default:
                    return _services.Party.RecruitCanonical(baseUnitId);
            }
        }

        public bool TryRestoreElapsedSeconds(float elapsedSeconds)
        {
            return _services.State.TryRestoreElapsedSeconds(elapsedSeconds);
        }
    }
}
