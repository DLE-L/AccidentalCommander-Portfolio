using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class WorldFeedbackPresenterTests
    {
        [Test]
        public void CombatImpactPresenter_ResolvesTypedBindingAndPreservesEventContext()
        {
            CombatImpactFeedbackProfileSO profile = ScriptableObject.CreateInstance<CombatImpactFeedbackProfileSO>();
            var sink = new RecordingCombatImpactSink();
            try
            {
                var presenter = new CombatImpactPresenter(
                    new[]
                    {
                        new CombatImpactFeedbackBinding(new CombatImpactKind("commander.hit.normal"), profile),
                    },
                    sink);
                var presentation = new CombatImpactPresentation(
                    new CombatImpactKind("commander.hit.normal"),
                    new Vector3(1.0f, 2.0f, 0.0f),
                    Vector3.left,
                    17);

                Assert.That(presenter.TryPresent(in presentation), Is.True);
                Assert.That(sink.Count, Is.EqualTo(1));
                Assert.That(sink.Profile, Is.SameAs(profile));
                Assert.That(sink.Last.TargetInstanceId, Is.EqualTo(17));
                Assert.That(sink.Last.Position, Is.EqualTo(new Vector3(1.0f, 2.0f, 0.0f)));
                Assert.That(sink.Last.Direction, Is.EqualTo(Vector3.left));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CombatImpactPresenter_MissingBindingDoesNotDispatch()
        {
            var sink = new RecordingCombatImpactSink();
            var presenter = new CombatImpactPresenter(System.Array.Empty<CombatImpactFeedbackBinding>(), sink);
            var presentation = new CombatImpactPresentation(
                new CombatImpactKind("enemy.hit.normal"),
                Vector3.zero,
                Vector3.zero,
                1);

            Assert.That(presenter.TryPresent(in presentation), Is.False);
            Assert.That(sink.Count, Is.Zero);
        }

        [Test]
        public void StatusFeedbackPresenter_DispatchesConfiguredApplyAndReactionOnly()
        {
            StatusFeedbackProfileSO profile = ScriptableObject.CreateInstance<StatusFeedbackProfileSO>();
            var sink = new RecordingStatusSink();
            try
            {
                profile.SetForEditor(
                    new VfxAssetId(1),
                    new VfxAssetId(2),
                    new VfxAssetId(3),
                    new SpriteAssetId(4),
                    new AudioAssetId(5),
                    new AudioAssetId(6),
                    new AudioAssetId(7),
                    new[]
                    {
                        new StatusReactionFeedback(
                            StatusReactionKind.Consumed,
                            new VfxAssetId(8),
                            new AudioAssetId(9)),
                    });
                var presenter = new StatusFeedbackPresenter(
                    new[] { new StatusFeedbackBinding(new StatusId("shock"), profile) },
                    sink);
                var applied = new StatusFeedbackPresentation(
                    new StatusId("shock"),
                    StatusFeedbackEventKind.Applied,
                    Vector3.one,
                    22);
                var consumed = new StatusFeedbackPresentation(
                    new StatusId("shock"),
                    StatusFeedbackEventKind.Reaction,
                    Vector3.one,
                    22,
                    StatusReactionKind.Consumed);
                var targetDeath = new StatusFeedbackPresentation(
                    new StatusId("shock"),
                    StatusFeedbackEventKind.Reaction,
                    Vector3.one,
                    22,
                    StatusReactionKind.TargetDeath);

                Assert.That(presenter.TryPresent(in applied), Is.True);
                Assert.That(presenter.TryPresent(in consumed), Is.True);
                Assert.That(presenter.TryPresent(in targetDeath), Is.False);
                Assert.That(sink.Count, Is.EqualTo(2));
                Assert.That(sink.Profile, Is.SameAs(profile));
                Assert.That(sink.Last.ReactionKind, Is.EqualTo(StatusReactionKind.Consumed));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        private sealed class RecordingCombatImpactSink : ICombatImpactFeedbackSink
        {
            public int Count { get; private set; }
            public CombatImpactPresentation Last { get; private set; }
            public CombatImpactFeedbackProfileSO Profile { get; private set; }

            public void Present(
                in CombatImpactPresentation presentation,
                CombatImpactFeedbackProfileSO profile)
            {
                Count++;
                Last = presentation;
                Profile = profile;
            }
        }

        private sealed class RecordingStatusSink : IStatusFeedbackSink
        {
            public int Count { get; private set; }
            public StatusFeedbackPresentation Last { get; private set; }
            public StatusFeedbackProfileSO Profile { get; private set; }

            public void Present(
                in StatusFeedbackPresentation presentation,
                StatusFeedbackProfileSO profile)
            {
                Count++;
                Last = presentation;
                Profile = profile;
            }
        }
    }
}
