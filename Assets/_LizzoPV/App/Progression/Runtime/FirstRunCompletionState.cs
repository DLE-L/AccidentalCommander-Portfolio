using UnityEngine;

namespace Lizzo.PV.Flow
{
    public enum FirstRunEntryRoute
    {
        Tutorial,
        Home,
        NormalGameplay,
    }

    public interface IFirstRunProgressStore
    {
        bool GetBool(string key, bool defaultValue);
        void SetBool(string key, bool value);
        void Save();
    }

    public sealed class FirstRunCompletionState
    {
        internal const string TutorialCompletedKey = "lizzo.ftue.tutorial_completed.v1";

        readonly IFirstRunProgressStore _store;

        public FirstRunCompletionState(IFirstRunProgressStore store)
        {
            _store = store ?? throw new System.ArgumentNullException(nameof(store));
        }

        public bool IsTutorialCompleted => _store.GetBool(TutorialCompletedKey, false);

        public FirstRunEntryRoute ResolveEntryRoute(bool startNormalGameplay)
        {
            if (startNormalGameplay)
                return FirstRunEntryRoute.NormalGameplay;

            return IsTutorialCompleted
                ? FirstRunEntryRoute.Home
                : FirstRunEntryRoute.Tutorial;
        }

        public RunMode ResolveNextBattleMode(bool forceNormal)
        {
            return forceNormal || IsTutorialCompleted
                ? RunMode.Normal
                : RunMode.Tutorial;
        }

        public bool TryCommitTutorialClear()
        {
            if (IsTutorialCompleted)
                return false;

            _store.SetBool(TutorialCompletedKey, true);
            _store.Save();
            return true;
        }
    }

    public static class FirstRunProgress
    {
        static readonly FirstRunCompletionState State = new FirstRunCompletionState(new PlayerPrefsFirstRunProgressStore());

        public static bool IsTutorialCompleted => State.IsTutorialCompleted;

        public static FirstRunEntryRoute ResolveEntryRoute(bool startNormalGameplay)
        {
            return State.ResolveEntryRoute(startNormalGameplay);
        }

        public static RunMode ResolveNextBattleMode(bool forceNormal)
        {
            return State.ResolveNextBattleMode(forceNormal);
        }

        public static bool TryCommitTutorialClear()
        {
            return State.TryCommitTutorialClear();
        }
    }

    sealed class PlayerPrefsFirstRunProgressStore : IFirstRunProgressStore
    {
        public bool GetBool(string key, bool defaultValue)
        {
            int defaultValueAsInt = defaultValue ? 1 : 0;
            return PlayerPrefs.GetInt(key, defaultValueAsInt) != 0;
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
