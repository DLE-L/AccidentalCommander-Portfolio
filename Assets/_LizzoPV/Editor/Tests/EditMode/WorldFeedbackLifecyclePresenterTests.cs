using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class WorldFeedbackLifecyclePresenterTests
    {
        [Test]
        public void SpawnDeathPresenter_UsesIndependentProfilesForSameEnemyId()
        {
            EnemySpawnFeedbackProfileSO spawnProfile = ScriptableObject.CreateInstance<EnemySpawnFeedbackProfileSO>();
            EnemyDeathFeedbackProfileSO deathProfile = ScriptableObject.CreateInstance<EnemyDeathFeedbackProfileSO>();
            var sink = new SpawnDeathSink();
            try
            {
                EnemyId enemyId = new EnemyId("small_goblin");
                var presenter = new SpawnDeathFeedbackPresenter(
                    new[] { new EnemySpawnFeedbackBinding(enemyId, spawnProfile) },
                    new[] { new EnemyDeathFeedbackBinding(enemyId, deathProfile) },
                    sink);
                var spawn = new EnemySpawnPresentation(enemyId, Vector3.left, 10);
                var death = new EnemyDeathPresentation(enemyId, Vector3.right, 10, false, false);

                Assert.That(presenter.TryPresentSpawn(in spawn), Is.True);
                Assert.That(presenter.TryPresentDeath(in death), Is.True);
                Assert.That(sink.SpawnCount, Is.EqualTo(1));
                Assert.That(sink.DeathCount, Is.EqualTo(1));
                Assert.That(sink.SpawnProfile, Is.SameAs(spawnProfile));
                Assert.That(sink.DeathProfile, Is.SameAs(deathProfile));
                Assert.That(sink.LastDeath.EnemyInstanceId, Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(spawnProfile);
                Object.DestroyImmediate(deathProfile);
            }
        }

        [Test]
        public void SpawnDeathPresenter_RejectsMissingEnemyBindingWithoutFallback()
        {
            var sink = new SpawnDeathSink();
            var presenter = new SpawnDeathFeedbackPresenter(
                System.Array.Empty<EnemySpawnFeedbackBinding>(),
                System.Array.Empty<EnemyDeathFeedbackBinding>(),
                sink);
            var spawn = new EnemySpawnPresentation(new EnemyId("unknown"), Vector3.zero, 1);

            Assert.That(presenter.TryPresentSpawn(in spawn), Is.False);
            Assert.That(sink.SpawnCount, Is.Zero);
        }

        [Test]
        public void ExperiencePresenter_SelectsVisualTierWithoutRecomputingRewardValue()
        {
            ExperienceOrbFeedbackProfileSO small = ScriptableObject.CreateInstance<ExperienceOrbFeedbackProfileSO>();
            ExperienceOrbFeedbackProfileSO large = ScriptableObject.CreateInstance<ExperienceOrbFeedbackProfileSO>();
            var sink = new ExperienceSink();
            try
            {
                var presenter = new ExperienceFeedbackPresenter(
                    new[]
                    {
                        new ExperienceOrbFeedbackBinding(OrbVisualTier.Small, small),
                        new ExperienceOrbFeedbackBinding(OrbVisualTier.Large, large),
                    },
                    sink);
                var presentation = new ExperienceFeedbackPresentation(
                    OrbVisualTier.Large,
                    ExperienceFeedbackEventKind.AbsorbComplete,
                    Vector3.one,
                    55,
                    12);

                Assert.That(presenter.TryPresent(in presentation), Is.True);
                Assert.That(sink.Count, Is.EqualTo(1));
                Assert.That(sink.Profile, Is.SameAs(large));
                Assert.That(sink.Last.RewardValue, Is.EqualTo(12));
                Assert.That(sink.Last.VisualTier, Is.EqualTo(OrbVisualTier.Large));
            }
            finally
            {
                Object.DestroyImmediate(small);
                Object.DestroyImmediate(large);
            }
        }

        [Test]
        public void ExperiencePresenter_RejectsNonPositiveRewardAndMissingTier()
        {
            ExperienceOrbFeedbackProfileSO small = ScriptableObject.CreateInstance<ExperienceOrbFeedbackProfileSO>();
            var sink = new ExperienceSink();
            try
            {
                var presenter = new ExperienceFeedbackPresenter(
                    new[] { new ExperienceOrbFeedbackBinding(OrbVisualTier.Small, small) },
                    sink);
                var zeroReward = new ExperienceFeedbackPresentation(
                    OrbVisualTier.Small,
                    ExperienceFeedbackEventKind.Spawn,
                    Vector3.zero,
                    1,
                    0);
                var missingTier = new ExperienceFeedbackPresentation(
                    OrbVisualTier.Medium,
                    ExperienceFeedbackEventKind.Spawn,
                    Vector3.zero,
                    2,
                    1);

                Assert.That(presenter.TryPresent(in zeroReward), Is.False);
                Assert.That(presenter.TryPresent(in missingTier), Is.False);
                Assert.That(sink.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(small);
            }
        }

        private sealed class SpawnDeathSink : ISpawnDeathFeedbackSink
        {
            public int SpawnCount { get; private set; }
            public int DeathCount { get; private set; }
            public EnemySpawnFeedbackProfileSO SpawnProfile { get; private set; }
            public EnemyDeathFeedbackProfileSO DeathProfile { get; private set; }
            public EnemyDeathPresentation LastDeath { get; private set; }

            public void PresentSpawn(in EnemySpawnPresentation presentation, EnemySpawnFeedbackProfileSO profile)
            {
                SpawnCount++;
                SpawnProfile = profile;
            }

            public void PresentDeath(in EnemyDeathPresentation presentation, EnemyDeathFeedbackProfileSO profile)
            {
                DeathCount++;
                LastDeath = presentation;
                DeathProfile = profile;
            }
        }

        private sealed class ExperienceSink : IExperienceFeedbackSink
        {
            public int Count { get; private set; }
            public ExperienceFeedbackPresentation Last { get; private set; }
            public ExperienceOrbFeedbackProfileSO Profile { get; private set; }

            public void Present(
                in ExperienceFeedbackPresentation presentation,
                ExperienceOrbFeedbackProfileSO profile)
            {
                Count++;
                Last = presentation;
                Profile = profile;
            }
        }
    }
}
