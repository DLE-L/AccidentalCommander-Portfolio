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
    }
}
