using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lizzo.PV.Flow
{
    public readonly struct TutorialRecoveryRosterEntry
    {
        public string BaseUnitId { get; }
        public int Progression { get; }

        internal TutorialRecoveryRosterEntry(string baseUnitId, int progression)
        {
            BaseUnitId = baseUnitId;
            Progression = progression;
        }
    }

    public sealed class TutorialRecoverySnapshot
    {
        readonly ReadOnlyCollection<TutorialRecoveryRosterEntry> _roster;

        public TutorialCheckpointId CheckpointId { get; }
        public float ElapsedSeconds { get; }
        public IReadOnlyList<TutorialRecoveryRosterEntry> Roster => _roster;
        public int ActiveSquadCount => _roster.Count;
        public int ActiveCompanionCount { get; }

        internal TutorialRecoverySnapshot(
            TutorialCheckpointId checkpointId,
            float elapsedSeconds,
            params TutorialRecoveryRosterEntry[] roster)
        {
            CheckpointId = checkpointId;
            ElapsedSeconds = elapsedSeconds;
            _roster = Array.AsReadOnly(roster ?? Array.Empty<TutorialRecoveryRosterEntry>());

            int activeCompanionCount = 0;
            for (int index = 0; index < _roster.Count; index++)
                activeCompanionCount += _roster[index].Progression;
            ActiveCompanionCount = activeCompanionCount;
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
        static readonly TutorialRecoverySnapshot Start = Create(
            TutorialCheckpointId.Start,
            0.0f);

        static readonly TutorialRecoverySnapshot RangedExpansion = Create(
            TutorialCheckpointId.RangedExpansion,
            TutorialRunTimeline.RangedExpansionStartSeconds,
            Entry("shield_guard", 3),
            Entry("sword_soldier", 1),
            Entry("cleric", 1));

        static readonly TutorialRecoverySnapshot FinalAssembly = Create(
            TutorialCheckpointId.FinalAssembly,
            TutorialRunTimeline.FinalAssemblyStartSeconds,
            Entry("shield_guard", 3),
            Entry("sword_soldier", 1),
            Entry("cleric", 1),
            Entry("falcon_archer", 3),
            Entry("bombardier", 3),
            Entry("skeleton_bomber", 3));

        static readonly TutorialRecoverySnapshot BossReady = Create(
            TutorialCheckpointId.BossReady,
            TutorialRunTimeline.ShowcaseStartSeconds,
            Entry("shield_guard", 3),
            Entry("sword_soldier", 3),
            Entry("cleric", 3),
            Entry("falcon_archer", 3),
            Entry("bombardier", 3),
            Entry("skeleton_bomber", 3),
            Entry("wolf_tamer", 3));

        public static TutorialRecoverySnapshot Resolve(TutorialCheckpointId checkpointId)
        {
            return checkpointId switch
            {
                TutorialCheckpointId.RangedExpansion => RangedExpansion,
                TutorialCheckpointId.FinalAssembly => FinalAssembly,
                TutorialCheckpointId.BossReady => BossReady,
                _ => Start,
            };
        }

        static TutorialRecoverySnapshot Create(
            TutorialCheckpointId checkpointId,
            float elapsedSeconds,
            params TutorialRecoveryRosterEntry[] roster)
        {
            return new TutorialRecoverySnapshot(checkpointId, elapsedSeconds, roster);
        }

        static TutorialRecoveryRosterEntry Entry(string baseUnitId, int progression)
        {
            return new TutorialRecoveryRosterEntry(baseUnitId, progression);
        }
    }
}
