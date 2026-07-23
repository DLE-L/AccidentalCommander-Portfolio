using System.Collections.Generic;
using Lizzo.PV.Flow;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class FirstRunCompletionStateTests
    {
        [Test]
        public void FreshStateStartsTutorial()
        {
            FirstRunCompletionState state = new FirstRunCompletionState(new MemoryStore());

            Assert.AreEqual(FirstRunEntryRoute.Tutorial, state.ResolveEntryRoute(startNormalGameplay: false));
        }

        [Test]
        public void TutorialClearCommitsOnceAndPersistsAcrossStateInstances()
        {
            MemoryStore store = new MemoryStore();
            FirstRunCompletionState firstState = new FirstRunCompletionState(store);

            Assert.IsTrue(firstState.TryCommitTutorialClear());
            Assert.IsFalse(firstState.TryCommitTutorialClear());
            Assert.AreEqual(1, store.SaveCount);

            FirstRunCompletionState relaunchedState = new FirstRunCompletionState(store);
            Assert.IsTrue(relaunchedState.IsTutorialCompleted);
            Assert.AreEqual(FirstRunEntryRoute.Home, relaunchedState.ResolveEntryRoute(startNormalGameplay: false));
        }

        [Test]
        public void HomeNextBattleRequestsNormalGameplayInsteadOfTutorial()
        {
            MemoryStore store = new MemoryStore();
            FirstRunCompletionState state = new FirstRunCompletionState(store);
            state.TryCommitTutorialClear();

            Assert.AreEqual(FirstRunEntryRoute.NormalGameplay, state.ResolveEntryRoute(startNormalGameplay: true));
        }

        sealed class MemoryStore : IFirstRunProgressStore
        {
            readonly Dictionary<string, bool> _values = new Dictionary<string, bool>();

            public int SaveCount { get; private set; }

            public bool GetBool(string key, bool defaultValue)
            {
                return _values.TryGetValue(key, out bool value) ? value : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _values[key] = value;
            }

            public void Save()
            {
                SaveCount++;
            }
        }
    }
}
