using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class WorldFeedbackAttackPresenterTests
    {
        [Test]
        public void CompanionImpact_DispatchesAttackProfileAndConfiguredGlobalImpact()
        {
            AttackFeedbackProfileSO attackProfile = ScriptableObject.CreateInstance<AttackFeedbackProfileSO>();
            CombatImpactFeedbackProfileSO impactProfile = ScriptableObject.CreateInstance<CombatImpactFeedbackProfileSO>();
            var attackSink = new CompanionSink();
            var impactSink = new ImpactSink();
            try
            {
                CombatImpactKind impactKind = new CombatImpactKind("enemy.hit.normal");
                attackProfile.SetForEditor(
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    new ImpactFeedback(new VfxAssetId(1), impactKind));
                var impactPresenter = new CombatImpactPresenter(
                    new[] { new CombatImpactFeedbackBinding(impactKind, impactProfile) },
                    impactSink);
                var presenter = new CompanionAttackFeedbackPresenter(
                    new[] { new AttackFeedbackBinding(new AttackId("cleric.basic"), attackProfile) },
                    attackSink,
                    impactPresenter);
                var impact = new CompanionAttackPresentation(
                    new AttackId("cleric.basic"),
                    CompanionAttackFeedbackEventKind.Impact,
                    new Vector3(2.0f, 3.0f, 0.0f),
                    Vector3.right,
                    10,
                    20);

                Assert.That(presenter.TryPresent(in impact), Is.True);
                Assert.That(attackSink.Count, Is.EqualTo(1));
                Assert.That(attackSink.Profile, Is.SameAs(attackProfile));
                Assert.That(impactSink.Count, Is.EqualTo(1));
                Assert.That(impactSink.Last.ImpactKind, Is.EqualTo(impactKind));
                Assert.That(impactSink.Last.TargetInstanceId, Is.EqualTo(20));
            }
            finally
            {
                Object.DestroyImmediate(attackProfile);
                Object.DestroyImmediate(impactProfile);
            }
        }

        [Test]
        public void CompanionCast_DoesNotDispatchImpactAndUnknownAttackIsRejected()
        {
            AttackFeedbackProfileSO profile = ScriptableObject.CreateInstance<AttackFeedbackProfileSO>();
            var attackSink = new CompanionSink();
            var impactSink = new ImpactSink();
            try
            {
                var presenter = new CompanionAttackFeedbackPresenter(
                    new[] { new AttackFeedbackBinding(new AttackId("sword.basic"), profile) },
                    attackSink,
                    new CombatImpactPresenter(System.Array.Empty<CombatImpactFeedbackBinding>(), impactSink));
                var cast = new CompanionAttackPresentation(
                    new AttackId("sword.basic"),
                    CompanionAttackFeedbackEventKind.Cast,
                    Vector3.zero,
                    Vector3.down,
                    11);
                var unknown = new CompanionAttackPresentation(
                    new AttackId("unknown"),
                    CompanionAttackFeedbackEventKind.Cast,
                    Vector3.zero,
                    Vector3.down,
                    11);

                Assert.That(presenter.TryPresent(in cast), Is.True);
                Assert.That(presenter.TryPresent(in unknown), Is.False);
                Assert.That(attackSink.Count, Is.EqualTo(1));
                Assert.That(impactSink.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void EnemyImpact_DispatchesEnemyProfileAndConfiguredGlobalImpact()
        {
            EnemyAttackFeedbackProfileSO attackProfile = ScriptableObject.CreateInstance<EnemyAttackFeedbackProfileSO>();
            CombatImpactFeedbackProfileSO impactProfile = ScriptableObject.CreateInstance<CombatImpactFeedbackProfileSO>();
            var attackSink = new EnemySink();
            var impactSink = new ImpactSink();
            try
            {
                CombatImpactKind impactKind = new CombatImpactKind("commander.hit.normal");
                attackProfile.SetForEditor(
                    default,
                    default,
                    default,
                    new EnemyImpactFeedback(new VfxAssetId(2), impactKind),
                    default);
                var impactPresenter = new CombatImpactPresenter(
                    new[] { new CombatImpactFeedbackBinding(impactKind, impactProfile) },
                    impactSink);
                var presenter = new EnemyAttackFeedbackPresenter(
                    new[] { new EnemyAttackFeedbackBinding(new EnemyAttackId("boss.slam"), attackProfile) },
                    attackSink,
                    impactPresenter);
                var impact = new EnemyAttackPresentation(
                    new EnemyAttackId("boss.slam"),
                    EnemyAttackFeedbackEventKind.Impact,
                    Vector3.one,
                    Vector3.left,
                    30,
                    40);

                Assert.That(presenter.TryPresent(in impact), Is.True);
                Assert.That(attackSink.Count, Is.EqualTo(1));
                Assert.That(attackSink.Profile, Is.SameAs(attackProfile));
                Assert.That(impactSink.Count, Is.EqualTo(1));
                Assert.That(impactSink.Last.ImpactKind, Is.EqualTo(impactKind));
                Assert.That(impactSink.Last.TargetInstanceId, Is.EqualTo(40));
            }
            finally
            {
                Object.DestroyImmediate(attackProfile);
                Object.DestroyImmediate(impactProfile);
            }
        }

        private sealed class CompanionSink : ICompanionAttackFeedbackSink
        {
            public int Count { get; private set; }
            public AttackFeedbackProfileSO Profile { get; private set; }

            public void Present(in CompanionAttackPresentation presentation, AttackFeedbackProfileSO profile)
            {
                Count++;
                Profile = profile;
            }
        }

        private sealed class EnemySink : IEnemyAttackFeedbackSink
        {
            public int Count { get; private set; }
            public EnemyAttackFeedbackProfileSO Profile { get; private set; }

            public void Present(in EnemyAttackPresentation presentation, EnemyAttackFeedbackProfileSO profile)
            {
                Count++;
                Profile = profile;
            }
        }

        private sealed class ImpactSink : ICombatImpactFeedbackSink
        {
            public int Count { get; private set; }
            public CombatImpactPresentation Last { get; private set; }

            public void Present(in CombatImpactPresentation presentation, CombatImpactFeedbackProfileSO profile)
            {
                Count++;
                Last = presentation;
            }
        }
    }
}
