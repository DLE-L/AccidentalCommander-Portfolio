using System.IO;
using NUnit.Framework;
using Lizzo.PV.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class EditorPlayModeBackgroundPolicyTests
    {
        [Test]
        public void EnteredPlayModeEnablesBackgroundRun()
        {
            Assert.IsTrue(EditorPlayModeBackgroundPolicy.ShouldEnableRunInBackground(PlayModeStateChange.EnteredPlayMode));
        }

        [TestCase(PlayModeStateChange.EnteredEditMode)]
        [TestCase(PlayModeStateChange.ExitingEditMode)]
        [TestCase(PlayModeStateChange.ExitingPlayMode)]
        public void OtherPlayModeTransitionsDoNotChangeBackgroundRun(PlayModeStateChange change)
        {
            Assert.IsFalse(EditorPlayModeBackgroundPolicy.ShouldEnableRunInBackground(change));
        }

        [Test]
        public void ExternalProcessWaitsAreBounded()
        {
            string sourcePath = Path.Combine(Application.dataPath, "Scripts", "Editor", "InternalAndroidBuildUtility.cs");
            string source = File.ReadAllText(sourcePath);

            Assert.That(InternalAndroidBuildUtility.ProcessTimeoutMilliseconds, Is.InRange(1, 5000));
            Assert.That(source, Does.Contain("WaitForExit(timeoutMilliseconds)"));
            Assert.That(source, Does.Contain("WaitForExit(ProcessTerminationTimeoutMilliseconds)"));
            Assert.That(source, Does.Not.Contain("WaitForExit();"));
            Assert.That(source, Does.Not.Contain("StandardOutput.ReadToEnd();"));
            Assert.That(source, Does.Not.Contain("StandardError.ReadToEnd();"));
        }

        [Test]
        public void FirstRunResetChangesOnlyTheTutorialCompletionKey()
        {
            const string sentinelKey = "lizzo.ftue.test.sentinel";
            bool hadCompletion = PlayerPrefs.HasKey(FtueHomeTestActions.TutorialCompletionKey);
            int previousCompletion = PlayerPrefs.GetInt(FtueHomeTestActions.TutorialCompletionKey, 0);
            bool hadSentinel = PlayerPrefs.HasKey(sentinelKey);
            int previousSentinel = PlayerPrefs.GetInt(sentinelKey, 0);

            try
            {
                PlayerPrefs.SetInt(sentinelKey, 77);
                FtueHomeTestActions.SetReturningState();
                Assert.IsTrue(FtueHomeTestActions.IsTutorialCompleted);

                FtueHomeTestActions.ResetFirstRunState();
                Assert.IsFalse(FtueHomeTestActions.IsTutorialCompleted);
                Assert.AreEqual(77, PlayerPrefs.GetInt(sentinelKey));
            }
            finally
            {
                Restore(FtueHomeTestActions.TutorialCompletionKey, hadCompletion, previousCompletion);
                Restore(sentinelKey, hadSentinel, previousSentinel);
                PlayerPrefs.Save();
            }
        }

        static void Restore(string key, bool existed, int value)
        {
            if (existed)
                PlayerPrefs.SetInt(key, value);
            else
                PlayerPrefs.DeleteKey(key);
        }
    }
}
