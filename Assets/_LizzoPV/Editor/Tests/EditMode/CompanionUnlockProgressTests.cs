using System.Collections.Generic;
using Lizzo.PV.Flow;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionUnlockProgressTests
    {
        [Test]
        public void DefaultAndPhaseTransitions_ExposeExactCanonicalMembership()
        {
            MemoryStore store = new MemoryStore();
            CompanionUnlockProgress progress = new CompanionUnlockProgress(store, false);

            Assert.AreEqual(CompanionUnlockPhase.Unlock00, progress.CurrentPhase);
            CollectionAssert.AreEquivalent(
                new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier" },
                progress.UnlockedBaseUnitIds);
            Assert.IsFalse(progress.IsUnlocked("field_herbalist"));

            Assert.IsTrue(progress.TryMarkStage1FirstClear());
            Assert.AreEqual(CompanionUnlockPhase.Unlock01, progress.CurrentPhase);
            Assert.IsTrue(progress.IsUnlocked("field_herbalist"));
            Assert.IsTrue(progress.IsUnlocked("fire_mage"));

            Assert.IsTrue(progress.TryMarkRedChargerBlockSuccess());
            Assert.AreEqual(CompanionUnlockPhase.Unlock02, progress.CurrentPhase);
            Assert.IsTrue(progress.IsUnlocked("lightning_mage"));
            Assert.IsTrue(progress.IsUnlocked("wolf_tamer"));

            Assert.IsTrue(progress.TryMarkStage2BossSeen());
            Assert.AreEqual(CompanionUnlockPhase.Unlock03, progress.CurrentPhase);
            Assert.IsTrue(progress.IsUnlocked("necromancer"));

            Assert.IsTrue(progress.TryMarkStage3Enter());
            Assert.AreEqual(CompanionUnlockPhase.Unlock04, progress.CurrentPhase);
            Assert.IsTrue(progress.IsUnlocked("wraith_knight"));

            Assert.IsTrue(progress.TryMarkStage3FirstClear());
            Assert.AreEqual(CompanionUnlockPhase.Unlock05, progress.CurrentPhase);
            Assert.IsTrue(progress.IsUnlocked("skeleton_bomber"));
            Assert.AreEqual(12, progress.UnlockedBaseUnitIds.Count);
            Assert.IsTrue(progress.TryMarkStage3BossSeen());
        }

        [Test]
        public void TestRuntimePolicy_ExposesAllCanonicalBaseCompanionsInOrderOnFreshStore()
        {
            Assert.That(CompanionUnlockProgress.IsTestRuntime, Is.True);
            CompanionUnlockProgress progress = new CompanionUnlockProgress(new MemoryStore());

            CollectionAssert.AreEqual(
                new[]
                {
                    "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier",
                    "field_herbalist", "fire_mage", "lightning_mage", "wolf_tamer", "necromancer",
                    "wraith_knight", "skeleton_bomber",
                },
                progress.UnlockedBaseUnitIds);
        }

        [Test]
        public void PersistenceAndResultCreatedWindow_AreMonotonicAndExpireAfterExactlyThreeResults()
        {
            MemoryStore store = new MemoryStore();
            CompanionUnlockProgress progress = new CompanionUnlockProgress(store, false);
            progress.TryMarkStage1FirstClear();

            Assert.IsTrue(progress.IsNewUnlockBoostEligible("field_herbalist"));
            Assert.IsFalse(progress.IsNewUnlockBoostEligible("shield_guard"));

            using (RunState run = new RunState())
            using (CompanionUnlockProgressRunBinder binder = new CompanionUnlockProgressRunBinder(progress, run))
            {
                EndRun(run);
                Assert.IsTrue(progress.IsNewUnlockBoostEligible("field_herbalist"));
                EndRun(run);
                Assert.IsTrue(progress.IsNewUnlockBoostEligible("field_herbalist"));
                EndRun(run);
                Assert.IsFalse(progress.IsNewUnlockBoostEligible("field_herbalist"));

                int resultCount = progress.CompletedResultCount;
                Assert.IsFalse(run.TryEnd(RunOutcome.Failure, 0));
                Assert.AreEqual(resultCount, progress.CompletedResultCount);
            }

            CompanionUnlockProgress reloaded = new CompanionUnlockProgress(store, false);
            Assert.AreEqual(CompanionUnlockPhase.Unlock01, reloaded.CurrentPhase);
            Assert.AreEqual(3, reloaded.CompletedResultCount);
            Assert.IsTrue(reloaded.IsUnlocked("field_herbalist"));
        }

        [Test]
        public void OrTriggersAndRunReset_PreserveAccountProgressWithoutRelocking()
        {
            CompanionUnlockProgress progress = new CompanionUnlockProgress(new MemoryStore(), false);
            Assert.IsTrue(progress.TryMarkStage2Enter());
            Assert.AreEqual(CompanionUnlockPhase.Unlock02, progress.CurrentPhase);
            Assert.IsTrue(progress.TryMarkStage3BossSeen());
            Assert.AreEqual(CompanionUnlockPhase.Unlock05, progress.CurrentPhase);

            using (RunState run = new RunState())
            {
                run.Reset(1);
                run.MarkLoaded();
                run.Reset(1);
            }

            Assert.AreEqual(CompanionUnlockPhase.Unlock05, progress.CurrentPhase);
            Assert.AreEqual(12, progress.UnlockedBaseUnitIds.Count);
        }

        static void EndRun(RunState run)
        {
            run.Reset(1);
            run.MarkLoaded();
            Assert.IsTrue(run.TryEnd(RunOutcome.Failure, 100));
        }

        sealed class MemoryStore : ICompanionUnlockProgressStore
        {
            readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int GetInt(string key, int defaultValue) => _values.TryGetValue(key, out int value) ? value : defaultValue;
            public void SetInt(string key, int value) => _values[key] = value;
            public void Save() { }
        }
    }
}
