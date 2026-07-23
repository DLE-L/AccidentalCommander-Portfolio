using Lizzo.PV.EditorTools;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class FtueHomeTestAutomationTests
    {
        [Test]
        public void AutoCardSelectionCyclesVisibleChoicesWithoutLeavingBounds()
        {
            Assert.AreEqual(0, FtueHomeTestAutomation.NextVisibleChoiceIndex(0, 3));
            Assert.AreEqual(1, FtueHomeTestAutomation.NextVisibleChoiceIndex(1, 3));
            Assert.AreEqual(2, FtueHomeTestAutomation.NextVisibleChoiceIndex(2, 3));
            Assert.AreEqual(0, FtueHomeTestAutomation.NextVisibleChoiceIndex(3, 3));
            Assert.AreEqual(-1, FtueHomeTestAutomation.NextVisibleChoiceIndex(0, 0));
        }

        [Test]
        public void AutoCardSelectionRequiresLoadedTutorialOrGameplayRun()
        {
            Assert.IsTrue(FtueHomeTestAutomation.CanAutomate(true, "Assets/_LizzoPV/Scenes/Tutorial.unity"));
            Assert.IsTrue(FtueHomeTestAutomation.CanAutomate(true, "Assets/_LizzoPV/Scenes/Gameplay.unity"));
            Assert.IsFalse(FtueHomeTestAutomation.CanAutomate(false, "Assets/_LizzoPV/Scenes/Gameplay.unity"));
            Assert.IsFalse(FtueHomeTestAutomation.CanAutomate(true, "Assets/_LizzoPV/Scenes/Lobby.unity"));
        }

        [Test]
        public void TimeScalePresetsRemainLimitedToOneTwoOrFive()
        {
            Assert.AreEqual(1.0f, FtueHomeTestAutomation.NormalizeRequestedTimeScale(1.0f));
            Assert.AreEqual(2.0f, FtueHomeTestAutomation.NormalizeRequestedTimeScale(2.0f));
            Assert.AreEqual(5.0f, FtueHomeTestAutomation.NormalizeRequestedTimeScale(5.0f));
            Assert.AreEqual(1.0f, FtueHomeTestAutomation.NormalizeRequestedTimeScale(3.0f));
        }
    }
}
