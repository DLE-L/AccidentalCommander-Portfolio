using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lizzo.PV.Flow
{
    public readonly struct RunSnapshotRosterEntry
    {
        public string BaseUnitId { get; }
        public int Progression { get; }

        internal RunSnapshotRosterEntry(string baseUnitId, int progression)
        {
            BaseUnitId = baseUnitId;
            Progression = progression;
        }
    }

    public sealed class RunSnapshot
    {
        readonly ReadOnlyCollection<RunSnapshotRosterEntry> _roster;

        public string SnapshotId { get; }
        public float ElapsedSeconds { get; }
        public IReadOnlyList<RunSnapshotRosterEntry> Roster => _roster;
        public int ActiveSquadCount => _roster.Count;
        public int ActiveCompanionCount { get; }
        public int CompletedCardCount { get; }

        internal RunSnapshot(
            string snapshotId,
            float elapsedSeconds,
            params RunSnapshotRosterEntry[] roster)
        {
            if (string.IsNullOrWhiteSpace(snapshotId))
                throw new ArgumentException("Snapshot id is required.", nameof(snapshotId));

            SnapshotId = snapshotId;
            ElapsedSeconds = elapsedSeconds;
            _roster = Array.AsReadOnly(roster ?? Array.Empty<RunSnapshotRosterEntry>());

            int activeCompanionCount = 0;
            for (int index = 0; index < _roster.Count; index++)
                activeCompanionCount += _roster[index].Progression;
            ActiveCompanionCount = activeCompanionCount;
            CompletedCardCount = activeCompanionCount;
        }

        public int GetProgression(string baseUnitId)
        {
            if (string.IsNullOrEmpty(baseUnitId))
                return 0;

            for (int index = 0; index < _roster.Count; index++)
                if (string.Equals(_roster[index].BaseUnitId, baseUnitId, StringComparison.Ordinal))
                    return _roster[index].Progression;

            return 0;
        }
    }

    public static class TutorialCheckpointRecovery
    {
        static readonly RunSnapshot Start = Create(
            TutorialCheckpointId.Start,
            0.0f);

        static readonly RunSnapshot RangedExpansion = Create(
            TutorialCheckpointId.RangedExpansion,
            TutorialRunTimeline.RangedExpansionStartSeconds,
            Entry("shield_guard", 3),
            Entry("sword_soldier", 1),
            Entry("cleric", 1));

        static readonly RunSnapshot FinalAssembly = Create(
            TutorialCheckpointId.FinalAssembly,
            TutorialRunTimeline.FinalAssemblyStartSeconds,
            Entry("shield_guard", 3),
            Entry("sword_soldier", 1),
            Entry("cleric", 1),
            Entry("falcon_archer", 3),
            Entry("bombardier", 3),
            Entry("skeleton_bomber", 3));

        static readonly RunSnapshot BossReady = Create(
            TutorialCheckpointId.BossReady,
            TutorialRunTimeline.ShowcaseStartSeconds,
            Entry("shield_guard", 3),
            Entry("sword_soldier", 3),
            Entry("cleric", 3),
            Entry("falcon_archer", 3),
            Entry("bombardier", 3),
            Entry("skeleton_bomber", 3),
            Entry("wolf_tamer", 3));

        public static RunSnapshot Resolve(TutorialCheckpointId checkpointId)
        {
            return checkpointId switch
            {
                TutorialCheckpointId.RangedExpansion => RangedExpansion,
                TutorialCheckpointId.FinalAssembly => FinalAssembly,
                TutorialCheckpointId.BossReady => BossReady,
                _ => Start,
            };
        }

        static RunSnapshot Create(
            TutorialCheckpointId checkpointId,
            float elapsedSeconds,
            params RunSnapshotRosterEntry[] roster)
        {
            return new RunSnapshot(ToSnapshotId(checkpointId), elapsedSeconds, roster);
        }

        static RunSnapshotRosterEntry Entry(string baseUnitId, int progression)
        {
            return new RunSnapshotRosterEntry(baseUnitId, progression);
        }

        static string ToSnapshotId(TutorialCheckpointId checkpointId)
        {
            return checkpointId switch
            {
                TutorialCheckpointId.RangedExpansion => "tutorial:phase_30",
                TutorialCheckpointId.FinalAssembly => "tutorial:phase_90",
                TutorialCheckpointId.BossReady => "tutorial:boss_ready_135",
                _ => "tutorial:start",
            };
        }
    }
}
