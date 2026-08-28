using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    internal static class RunResumeRuntime
    {
        internal static bool TryRestore(RunServices services)
        {
            if (services == null)
                return false;
            RunStartRequest request = services.StartRequest;
            if (request.StartMode == RunStartMode.Fresh)
                return true;

            RunSnapshot snapshot = request.Snapshot;
            bool restored = RunSnapshotApplication.TryApply(
                snapshot,
                new RunServicesSnapshotApplicationTarget(services));
            if (restored == false)
            {
                Debug.LogError(
                    $"[RunResume] Snapshot restore failed: {snapshot.SnapshotId}.");
            }

            else
            {
                int completedCardCount = snapshot.CompletedCardCount;
                int nextCardNumber = completedCardCount + 1;
                restored = services.State.TryRestoreProgression(
                    completedCardCount,
                    services.App.Data.GetLevelExp(nextCardNumber),
                    services.Definition.TargetCardCount);
                if (restored)
                    FixedCardPool.RestoreProgression(completedCardCount);
            }

            return restored;
        }
    }

    internal sealed class RunServicesSnapshotApplicationTarget : IRunSnapshotApplicationTarget
    {
        readonly RunServices _services;

        internal RunServicesSnapshotApplicationTarget(RunServices services)
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
            if (_services.RecordingCompanions != null)
                return FixedCardPool.TryApplyCanonicalCompanion(baseUnitId);

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
