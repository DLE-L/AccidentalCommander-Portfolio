using System.Collections.Generic;
using System.IO;
using Lizzo.PV.Flow;
using NUnit.Framework;
using Lizzo.PV.EditorTools;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class EditorBuildAndFtueTests
    {
        [Test]
        public void ExternalProcessWaitsAreBounded()
        {
            string sourcePath = Path.Combine(Application.dataPath, "_LizzoPV", "Editor", "Build", "InternalAndroidBuildDeviceProcess.cs");
            string source = File.ReadAllText(sourcePath);

            Assert.That(InternalAndroidBuildUtility.ProcessTimeoutMilliseconds, Is.InRange(1, 5000));
            Assert.That(source, Does.Contain("WaitForExit(timeoutMilliseconds)"));
            Assert.That(source, Does.Contain("WaitForExit(ProcessTerminationTimeoutMilliseconds)"));
            Assert.That(source, Does.Not.Contain("WaitForExit();"));
            Assert.That(source, Does.Not.Contain("StandardOutput.ReadToEnd();"));
            Assert.That(source, Does.Not.Contain("StandardError.ReadToEnd();"));
        }

        [Test]
        public void InternalBuildRestoresPerformanceTestPackageGeneratedResources()
        {
            string utilitySourcePath = Path.Combine(
                Application.dataPath,
                "_LizzoPV",
                "Editor",
                "Build",
                "InternalAndroidBuildUtility.cs");
            string snapshotSourcePath = Path.Combine(
                Application.dataPath,
                "_LizzoPV",
                "Editor",
                "Build",
                "InternalAndroidBuildSourceState.cs");
            string utilitySource = File.ReadAllText(utilitySourcePath);
            string snapshotSource = File.ReadAllText(snapshotSourcePath);

            Assert.That(utilitySource, Does.Contain("PerformanceTestRunInfo.json"));
            Assert.That(utilitySource, Does.Contain("PerformanceTestRunSettings.json"));
            Assert.That(snapshotSource, Does.Contain("GeneratedFileSnapshot.Capture(PerformanceTestRunInfoAssetPath)"));
            Assert.That(snapshotSource, Does.Contain("GeneratedFileSnapshot.Capture(PerformanceTestRunInfoMetaAssetPath)"));
            Assert.That(snapshotSource, Does.Contain("GeneratedFileSnapshot.Capture(PerformanceTestRunSettingsAssetPath)"));
            Assert.That(snapshotSource, Does.Contain("GeneratedFileSnapshot.Capture(PerformanceTestRunSettingsMetaAssetPath)"));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void AndroidBuildInventory_AllowsZeroOrOneSymbolsZip(int symbolsCount)
        {
            string outputRoot = CreateTemporaryOutputRoot();
            string apkPath = Path.Combine(outputRoot, "AccidentalCommander.apk");
            try
            {
                File.WriteAllText(apkPath, string.Empty);
                File.WriteAllText(Path.Combine(outputRoot, "build_info.json"), "{}");
                CreateSymbolsZips(outputRoot, apkPath, symbolsCount);

                Assert.IsTrue(
                    InternalAndroidBuildUtility.TryValidateAllowedBuildInventory(outputRoot, apkPath, out string failure),
                    failure);
            }
            finally
            {
                DeleteTemporaryOutputRoot(outputRoot);
            }
        }

        [Test]
        public void AndroidBuildInventory_RejectsMoreThanOneSymbolsZip()
        {
            string outputRoot = CreateTemporaryOutputRoot();
            string apkPath = Path.Combine(outputRoot, "AccidentalCommander.apk");
            try
            {
                File.WriteAllText(apkPath, string.Empty);
                CreateSymbolsZips(outputRoot, apkPath, 2);

                Assert.IsFalse(
                    InternalAndroidBuildUtility.TryValidateAllowedBuildInventory(outputRoot, apkPath, out string failure));
                Assert.That(failure, Does.Contain("at most one IL2CPP symbols ZIP"));
            }
            finally
            {
                DeleteTemporaryOutputRoot(outputRoot);
            }
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
        public void FreshResetClearsTutorialCheckpointAndAccountProgressWithoutTouchingForeignPrefs()
        {
            const string sentinelKey = "lizzo.ftue.test.sentinel";
            bool hadCompletion = PlayerPrefs.HasKey(FtueHomeTestActions.TutorialCompletionKey);
            int previousCompletion = PlayerPrefs.GetInt(FtueHomeTestActions.TutorialCompletionKey, 0);
            bool hadSentinel = PlayerPrefs.HasKey(sentinelKey);
            int previousSentinel = PlayerPrefs.GetInt(sentinelKey, 0);
            TutorialCheckpointId previousCheckpoint = TutorialCheckpointProgress.Current;
            CompanionUnlockProgress progress = new CompanionUnlockProgress(
                new MemoryCompanionUnlockProgressStore(),
                false,
                () => true);

            try
            {
                progress.RecordResultCreated();
                Assert.That(progress.TryMarkStage1FirstClear(), Is.True);
                TutorialCheckpointProgress.Reset();
                Assert.That(TutorialCheckpointProgress.TryAdvance(135.0f), Is.True);
                PlayerPrefs.SetInt(sentinelKey, 77);
                FtueHomeTestActions.SetReturningState();
                Assert.IsTrue(FtueHomeTestActions.IsTutorialCompleted);

                FtueHomeTestActions.ResetFirstRunState(progress);
                Assert.IsFalse(FtueHomeTestActions.IsTutorialCompleted);
                Assert.That(TutorialCheckpointProgress.Current, Is.EqualTo(TutorialCheckpointId.Start));
                Assert.That(progress.CompletedResultCount, Is.Zero);
                Assert.That(progress.HasStage1FirstClear, Is.False);
                Assert.AreEqual(77, PlayerPrefs.GetInt(sentinelKey));
            }
            finally
            {
                Restore(FtueHomeTestActions.TutorialCompletionKey, hadCompletion, previousCompletion);
                Restore(sentinelKey, hadSentinel, previousSentinel);
                RestoreCheckpoint(previousCheckpoint);
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

        private static void CreateSymbolsZips(string outputRoot, string apkPath, int count)
        {
            string prefix = Path.GetFileNameWithoutExtension(apkPath);
            for (int index = 0; index < count; index++)
            {
                File.WriteAllText(
                    Path.Combine(outputRoot, $"{prefix}-{index}-IL2CPP.symbols.zip"),
                    string.Empty);
            }
        }

        static void Restore(string key, bool existed, int value)
        {
            if (existed)
                PlayerPrefs.SetInt(key, value);
            else
                PlayerPrefs.DeleteKey(key);
        }

        static void RestoreCheckpoint(TutorialCheckpointId checkpoint)
        {
            TutorialCheckpointProgress.Reset();
            switch (checkpoint)
            {
                case TutorialCheckpointId.RangedExpansion:
                    TutorialCheckpointProgress.TryAdvance(TutorialRunTimeline.RangedExpansionStartSeconds);
                    break;
                case TutorialCheckpointId.FinalAssembly:
                    TutorialCheckpointProgress.TryAdvance(TutorialRunTimeline.FinalAssemblyStartSeconds);
                    break;
                case TutorialCheckpointId.BossReady:
                    TutorialCheckpointProgress.TryAdvance(TutorialRunTimeline.ShowcaseStartSeconds);
                    break;
            }
        }

        sealed class MemoryCompanionUnlockProgressStore : ICompanionUnlockProgressStore
        {
            readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int GetInt(string key, int defaultValue)
            {
                return _values.TryGetValue(key, out int value) ? value : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _values[key] = value;
            }

            public void Save()
            {
            }
        }
    }
}
