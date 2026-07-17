using Lizzo.PV.Flow;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunPauseControllerTests
    {
        [Test]
        public void RunEndKeepsTimeFrozenAfterResultModalCloses()
        {
            GameObject gameObject = new GameObject(nameof(RunPauseControllerTests));
            try
            {
                RunPauseController controller = gameObject.AddComponent<RunPauseController>();
                controller.Initialize();
                controller.SetModalOpen(true);
                controller.MarkRunEnded();
                controller.SetModalOpen(false);

                Assert.IsTrue(controller.IsPaused);
                Assert.AreEqual(0.0f, Time.timeScale);
            }
            finally
            {
                Time.timeScale = 1.0f;
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GameplaySpeedRestoresAfterModalAndResetsAtRunEnd()
        {
            GameObject gameObject = new GameObject(nameof(RunPauseControllerTests));
            try
            {
                RunPauseController controller = gameObject.AddComponent<RunPauseController>();
                controller.Initialize();

                Assert.AreEqual(1.0f, controller.SelectedGameplaySpeed);
                Assert.IsTrue(controller.ToggleGameplaySpeed());
                Assert.AreEqual(5.0f, controller.SelectedGameplaySpeed);
                Assert.AreEqual(5.0f, Time.timeScale);

                controller.SetModalOpen(true);
                Assert.AreEqual(0.0f, Time.timeScale);
                Assert.IsFalse(controller.ToggleGameplaySpeed());

                controller.SetModalOpen(false);
                Assert.AreEqual(5.0f, Time.timeScale);

                controller.MarkRunEnded();
                Assert.AreEqual(1.0f, controller.SelectedGameplaySpeed);
                Assert.AreEqual(0.0f, Time.timeScale);
            }
            finally
            {
                Time.timeScale = 1.0f;
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
