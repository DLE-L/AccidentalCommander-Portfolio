using System;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public enum TutorialCheckpointId
    {
        Start,
        RangedExpansion,
        FinalAssembly,
        BossReady,
    }

    public static class TutorialCheckpointPolicy
    {
        public static TutorialCheckpointId Resolve(float elapsedSeconds)
        {
            if (elapsedSeconds >= TutorialRunTimeline.ShowcaseStartSeconds)
                return TutorialCheckpointId.BossReady;
            if (elapsedSeconds >= TutorialRunTimeline.FinalAssemblyStartSeconds)
                return TutorialCheckpointId.FinalAssembly;
            if (elapsedSeconds >= TutorialRunTimeline.RangedExpansionStartSeconds)
                return TutorialCheckpointId.RangedExpansion;

            return TutorialCheckpointId.Start;
        }
    }

    public interface ITutorialCheckpointStore
    {
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
        void DeleteKey(string key);
        void Save();
    }

    public sealed class TutorialCheckpointState
    {
        internal const string CheckpointKey = "lizzo.ftue.checkpoint.v1";

        readonly ITutorialCheckpointStore _store;

        public TutorialCheckpointId Current { get; private set; }

        public TutorialCheckpointState(ITutorialCheckpointStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Current = FromId(_store.GetString(CheckpointKey, string.Empty));
        }

        public bool TryAdvance(float elapsedSeconds)
        {
            TutorialCheckpointId next = TutorialCheckpointPolicy.Resolve(elapsedSeconds);
            if (next <= Current)
                return false;

            Current = next;
            _store.SetString(CheckpointKey, ToId(next));
            _store.Save();
            return true;
        }

        public void Reset()
        {
            Current = TutorialCheckpointId.Start;
            _store.DeleteKey(CheckpointKey);
            _store.Save();
        }

        static string ToId(TutorialCheckpointId checkpoint)
        {
            return checkpoint switch
            {
                TutorialCheckpointId.RangedExpansion => "phase_30",
                TutorialCheckpointId.FinalAssembly => "phase_90",
                TutorialCheckpointId.BossReady => "boss_ready_135",
                _ => string.Empty,
            };
        }

        static TutorialCheckpointId FromId(string checkpointId)
        {
            return checkpointId switch
            {
                "phase_30" => TutorialCheckpointId.RangedExpansion,
                "phase_90" => TutorialCheckpointId.FinalAssembly,
                "boss_ready_135" => TutorialCheckpointId.BossReady,
                _ => TutorialCheckpointId.Start,
            };
        }
    }

    public static class TutorialCheckpointProgress
    {
        static readonly TutorialCheckpointState State =
            new TutorialCheckpointState(new PlayerPrefsTutorialCheckpointStore());

        public static TutorialCheckpointId Current => State.Current;

        public static bool TryAdvance(float elapsedSeconds)
        {
            return State.TryAdvance(elapsedSeconds);
        }

        public static void Reset()
        {
            State.Reset();
        }
    }

    sealed class PlayerPrefsTutorialCheckpointStore : ITutorialCheckpointStore
    {
        public string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
