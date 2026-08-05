using System;
using System.IO;
using NUnit.Framework;

namespace Lizzo.PV.EditorTools.Capture.Tests
{
    public sealed class GameplayOverlayCaptureCommandTests
    {
        [Test]
        public void ValidationRejectsEditModeAndInvalidResolution()
        {
            string projectRoot = Path.Combine(Path.GetTempPath(), "GameplayOverlayCaptureTests");
            string output = Path.Combine(projectRoot, "Temp", "overlay.png");

            Assert.That(
                GameplayOverlayCaptureCommand.ValidateRequest(output, 1080, 2340, 10.0f, projectRoot, false, true, false),
                Does.Contain("Play Mode"));
            Assert.That(
                GameplayOverlayCaptureCommand.ValidateRequest(output, 0, 2340, 10.0f, projectRoot, true, true, false),
                Does.Contain("positive"));
            Assert.That(
                GameplayOverlayCaptureCommand.ValidateRequest(output, 4097, 2340, 10.0f, projectRoot, true, true, false),
                Does.Contain("4096"));
        }

        [Test]
        public void ValidationConfinesOutputToProjectTemp()
        {
            string projectRoot = Path.Combine(Path.GetTempPath(), "GameplayOverlayCaptureTests");
            string outside = Path.Combine(projectRoot, "Assets", "overlay.png");

            Assert.That(
                GameplayOverlayCaptureCommand.ValidateRequest(outside, 1080, 2340, 10.0f, projectRoot, true, true, false),
                Does.Contain("Temp"));
        }

        [Test]
        public void EnsureOutputDirectoryCreatesMissingNestedTempFolder()
        {
            string projectRoot = Path.Combine(Path.GetTempPath(), "GameplayOverlayCaptureTests", Guid.NewGuid().ToString("N"));
            string output = Path.Combine(projectRoot, "Temp", "GameplayOverlayCapture", "overlay.png");
            try
            {
                Assert.That(Directory.Exists(Path.GetDirectoryName(output)), Is.False);
                GameplayOverlayCaptureCommand.EnsureOutputDirectory(output);
                Assert.That(Directory.Exists(Path.GetDirectoryName(output)), Is.True);
            }
            finally
            {
                if (Directory.Exists(projectRoot))
                    Directory.Delete(projectRoot, true);
            }
        }

        [Test]
        public void SingleSessionGuardRejectsSecondReservation()
        {
            GameplayOverlayCaptureCommand.ResetForTests();
            try
            {
                Assert.That(GameplayOverlayCaptureCommand.TryReserveSessionForTests(), Is.True);
                Assert.That(GameplayOverlayCaptureCommand.TryReserveSessionForTests(), Is.False);
            }
            finally
            {
                GameplayOverlayCaptureCommand.ResetForTests();
            }
        }

        [Test]
        public void RestorationRemovesOnlyTemporarySizeAndRestoresPreviousSelection()
        {
            int removedIndex = -1;
            int selectedIndex = -1;
            GameViewRestorePlan plan = new GameViewRestorePlan(7, 19, true);

            plan.Restore(index => selectedIndex = index, index => removedIndex = index);
            plan.Restore(index => selectedIndex = index, index => removedIndex = index);

            Assert.That(removedIndex, Is.EqualTo(19));
            Assert.That(selectedIndex, Is.EqualTo(7));
            Assert.That(plan.IsRestored, Is.True);
        }
    }
}
