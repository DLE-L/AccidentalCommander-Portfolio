using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Flow;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class SceneTransitionCoordinatorTests
    {
        [Test]
        public void Transition_WaitsForMatchingReadyAndKeepsProgressMonotonic()
        {
            var driver = new FakeDriver();
            var presentation = new FakePresentation();
            var coordinator = new SceneTransitionCoordinator(driver, presentation);
            var request = new SceneTransitionRequest("Target", SceneTransitionKind.Standard, true);

            UniTask<bool> run = coordinator.TryRunAsync(request, "Source", CancellationToken.None);

            Assert.That(coordinator.Phase, Is.EqualTo(SceneTransitionPhase.WaitingForTargetReady));
            Assert.That(coordinator.Progress, Is.EqualTo(0.9f).Within(0.0001f));
            CollectionAssert.AreEqual(new[] { 0f, 0.45f, 0.9f }, presentation.ProgressValues);
            Assert.That(coordinator.ReportTargetReady("Other"), Is.False);
            Assert.That(driver.FinalizeCount, Is.Zero);

            Assert.That(coordinator.ReportTargetReady("Target"), Is.True);
            Assert.That(run.GetAwaiter().GetResult(), Is.True);
            Assert.That(coordinator.Phase, Is.EqualTo(SceneTransitionPhase.Completed));
            Assert.That(coordinator.Progress, Is.EqualTo(1f));
            Assert.That(driver.FinalizeCount, Is.EqualTo(1));
            Assert.That(presentation.HideCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateRequest_DoesNotStartASecondLoad()
        {
            var driver = new FakeDriver();
            var coordinator = new SceneTransitionCoordinator(driver, new FakePresentation());
            var request = new SceneTransitionRequest("Target", SceneTransitionKind.Standard, true);

            UniTask<bool> first = coordinator.TryRunAsync(request, "Source", CancellationToken.None);
            bool duplicate = coordinator.TryRunAsync(request, "Source", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(duplicate, Is.False);
            Assert.That(driver.LoadCount, Is.EqualTo(1));
            coordinator.ReportTargetReady("Target");
            Assert.That(first.GetAwaiter().GetResult(), Is.True);
        }

        [Test]
        public void TargetFailure_CleansPartialTargetAndRetryUsesSameRequest()
        {
            var driver = new FakeDriver();
            var presentation = new FakePresentation();
            var coordinator = new SceneTransitionCoordinator(driver, presentation);
            var request = new SceneTransitionRequest("Target", SceneTransitionKind.Start, false);

            UniTask<bool> first = coordinator.TryRunAsync(request, "Loading", CancellationToken.None);
            Assert.That(coordinator.ReportTargetFailure("Target", "data_not_ready"), Is.True);
            Assert.That(first.GetAwaiter().GetResult(), Is.False);
            Assert.That(coordinator.Phase, Is.EqualTo(SceneTransitionPhase.Failed));
            Assert.That(coordinator.LastFailure, Is.EqualTo("data_not_ready"));
            Assert.That(driver.CleanupCount, Is.EqualTo(1));
            Assert.That(presentation.ErrorCount, Is.EqualTo(1));

            UniTask<bool> retry = coordinator.RetryAsync(CancellationToken.None);
            Assert.That(driver.LoadCount, Is.EqualTo(2));
            Assert.That(coordinator.ReportTargetReady("Target"), Is.True);
            Assert.That(retry.GetAwaiter().GetResult(), Is.True);
            Assert.That(driver.FinalizeCount, Is.EqualTo(1));
        }

        [Test]
        public void InvalidRequest_DoesNotTouchDriverOrPresentation()
        {
            var driver = new FakeDriver();
            var presentation = new FakePresentation();
            var coordinator = new SceneTransitionCoordinator(driver, presentation);

            bool result = coordinator.TryRunAsync(
                    new SceneTransitionRequest(string.Empty, SceneTransitionKind.Standard, true),
                    "Source",
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result, Is.False);
            Assert.That(driver.LoadCount, Is.Zero);
            Assert.That(presentation.ShowCount, Is.Zero);
        }

        sealed class FakeDriver : ISceneTransitionDriver
        {
            public int LoadCount { get; private set; }
            public int FinalizeCount { get; private set; }
            public int CleanupCount { get; private set; }

            public UniTask<bool> LoadTargetAsync(
                string targetScenePath,
                Action<float> reportProgress,
                CancellationToken cancellationToken)
            {
                LoadCount++;
                reportProgress(0.5f);
                reportProgress(0.2f);
                reportProgress(1f);
                return UniTask.FromResult(true);
            }

            public UniTask<bool> ActivateTargetAndUnloadSourceAsync(
                string sourceScenePath,
                string targetScenePath,
                CancellationToken cancellationToken)
            {
                FinalizeCount++;
                return UniTask.FromResult(true);
            }

            public UniTask CleanupTargetAsync(string targetScenePath, CancellationToken cancellationToken)
            {
                CleanupCount++;
                return UniTask.CompletedTask;
            }
        }

        sealed class FakePresentation : ISceneTransitionPresentation
        {
            public readonly List<float> ProgressValues = new List<float>();
            public int ShowCount { get; private set; }
            public int ErrorCount { get; private set; }
            public int HideCount { get; private set; }

            public void Show(SceneTransitionKind kind)
            {
                ShowCount++;
            }

            public void SetProgress(float progress)
            {
                ProgressValues.Add(progress);
            }

            public void ShowError()
            {
                ErrorCount++;
            }

            public void Hide()
            {
                HideCount++;
            }
        }
    }
}
