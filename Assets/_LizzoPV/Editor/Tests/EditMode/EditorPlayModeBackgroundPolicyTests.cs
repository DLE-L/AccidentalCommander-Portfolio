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
            string sourcePath = Path.Combine(Application.dataPath, "_LizzoPV", "Scripts", "Editor", "InternalAndroidBuildUtility.cs");
            string source = File.ReadAllText(sourcePath);

            Assert.That(InternalAndroidBuildUtility.ProcessTimeoutMilliseconds, Is.InRange(1, 5000));
            Assert.That(source, Does.Contain("WaitForExit(timeoutMilliseconds)"));
            Assert.That(source, Does.Contain("WaitForExit(ProcessTerminationTimeoutMilliseconds)"));
            Assert.That(source, Does.Not.Contain("WaitForExit();"));
            Assert.That(source, Does.Not.Contain("StandardOutput.ReadToEnd();"));
            Assert.That(source, Does.Not.Contain("StandardError.ReadToEnd();"));
        }

        [Test]
        public void DoubleInvocationUsesOneLiveBuildOwner()
        {
            string outputRoot = CreateTemporaryOutputRoot();
            try
            {
                Assert.IsTrue(EditorBuildRunCoordinator.TryAcquire(outputRoot, "first-operation", out EditorBuildRunCoordinator.Ownership first,
                    out string firstFailure), firstFailure);
                using (first)
                {
                    EditorBuildRunCoordinator.WriteStatus(
                        outputRoot,
                        new EditorBuildRunCoordinator.EditorBuildRunStatus
                        {
                            state = EditorBuildRunCoordinator.PendingState,
                            operationId = "first-operation",
                            buildId = string.Empty,
                        }
                        );

                    Assert.IsFalse(
                        EditorBuildRunCoordinator.TryAcquire(
                            outputRoot,
                            "second-operation",
                            out EditorBuildRunCoordinator.Ownership second,
                            out string secondFailure));
                    Assert.IsNull(second);
                    Assert.That(secondFailure, Does.Contain("ALREADY_IN_PROGRESS"));
                    Assert.That(secondFailure, Does.Contain("first-operation"));
                }
            }
            finally
            {
                DeleteTemporaryOutputRoot(outputRoot);
            }
        }

        [Test]
        public void StaleOwnershipIsReplacedOnlyAfterExclusiveAcquire()
        {
            string outputRoot = CreateTemporaryOutputRoot();
            string lockPath = Path.Combine(outputRoot, EditorBuildRunCoordinator.LockFileName);
            try
            {
                File.WriteAllText(lockPath, "stale-owner");
                Assert.IsTrue(EditorBuildRunCoordinator.TryAcquire(outputRoot, "fresh-operation", out EditorBuildRunCoordinator.Ownership ownership,
                    out string failure), failure);
                ownership.Dispose();

                Assert.That(File.ReadAllText(lockPath), Does.Contain("fresh-operation"));
                Assert.That(File.ReadAllText(lockPath), Does.Not.Contain("stale-owner"));
            }
            finally
            {
                DeleteTemporaryOutputRoot(outputRoot);
            }
        }

        [Test]
        public void RepeatedSchedulingIsRejectedUntilTheCallbackCompletes()
        {
            var gate = new EditorBuildRunCoordinator.ScheduleGate();

            Assert.IsTrue(gate.TrySchedule());
            Assert.IsFalse(gate.TrySchedule());

            gate.Complete();
            Assert.IsTrue(gate.TrySchedule());
        }

        [Test]
        public void StatusTransitionsRemainDurableAndTerminal()
        {
            string outputRoot = CreateTemporaryOutputRoot();
            try
            {
                string[] states =
                {
                    EditorBuildRunCoordinator.PendingState,
                    EditorBuildRunCoordinator.RunningState,
                    EditorBuildRunCoordinator.SucceededState,
                }
                ;

                for (int index = 0;
                index < states.Length;
                index++)
                {
                    EditorBuildRunCoordinator.WriteStatus(
                        outputRoot,
                        new EditorBuildRunCoordinator.EditorBuildRunStatus
                        {
                            state = states[index],
                            operationId = "durable-operation",
                            buildId = "260718_abcdef1",
                            result = index == states.Length - 1 ? "SUCCEEDED" : string.Empty,
                        }
                        );

                    Assert.IsTrue(EditorBuildRunCoordinator.TryReadStatus(outputRoot, out EditorBuildRunCoordinator.EditorBuildRunStatus status));
                    Assert.AreEqual(states[index], status.state);
                    Assert.AreEqual("durable-operation", status.operationId);
                }

                Assert.IsTrue(EditorBuildRunCoordinator.IsTerminalState(EditorBuildRunCoordinator.SucceededState));
                Assert.IsTrue(EditorBuildRunCoordinator.IsTerminalState(EditorBuildRunCoordinator.FailedState));
                Assert.IsFalse(EditorBuildRunCoordinator.IsTerminalState(EditorBuildRunCoordinator.RunningState));
            }
            finally
            {
                DeleteTemporaryOutputRoot(outputRoot);
            }
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

        private static string CreateTemporaryOutputRoot()
        {
            string outputRoot = Path.Combine(Path.GetTempPath(), $"LizzoPV_BuildRun_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(outputRoot);
            return outputRoot;
        }

        private static void DeleteTemporaryOutputRoot(string outputRoot)
        {
            if (Directory.Exists(outputRoot))
                Directory.Delete(outputRoot, true);
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
