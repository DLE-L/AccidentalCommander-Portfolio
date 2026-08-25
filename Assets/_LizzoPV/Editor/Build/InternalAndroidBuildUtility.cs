#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Android.Types;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Lizzo.PV.Build;

namespace Lizzo.PV.EditorTools
{
    public static partial class InternalAndroidBuildUtility
    {
        private const string ProductName = "Accidental Commander";
        private const string CompanyName = "Lizzo Studio";
        private const string AndroidBundleId = "com.lizzostudio.accidentalcommander";
        private const string Version = "0.1.0";
        private const int VersionCode = 2;
        private const string BuildIdArgument = "-internalBuildId";
        private const string BuildDateUtcArgument = "-internalBuildDateUtc";
        private const string CliLogPrefix = "[InternalAndroidBuild][CLI]";
        private const string BuildInfoMetaAssetPath = "Assets/Resources/InternalTest/BuildInfo.json.meta";
        private const string ResourcesMetaAssetPath = "Assets/Resources.meta";
        private const string InternalTestResourcesMetaAssetPath = "Assets/Resources/InternalTest.meta";
        private const string AddressablesLinkAssetPath = "Assets/AddressableAssetsData/link.xml";
        private const string AddressablesLinkMetaAssetPath = "Assets/AddressableAssetsData/link.xml.meta";
        internal const int ProcessTimeoutMilliseconds = 3000;
        private const int ProcessTerminationTimeoutMilliseconds = 1000;
        private static readonly Regex BuildIdPattern = new Regex("^(?<time>\\d{6})_(?<revision>[0-9a-f]{7,40})$", RegexOptions.CultureInvariant);
        private static EditorBuildRunCoordinator.Ownership _editorBuildRunOwnership;
        private static readonly EditorBuildRunCoordinator.ScheduleGate EditorBuildRunSchedule = new EditorBuildRunCoordinator.ScheduleGate();

        [MenuItem("Lizzo/Internal Test/Apply Android Settings")]
        private static void ApplyAndroidSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidBundleId);
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            UnityEditor.Android.UserBuildSettings.DebugSymbols.level = DebugSymbolLevel.SymbolTable;
            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log($"[InternalAndroidBuild] Android settings applied. package={AndroidBundleId}, version={Version} ({VersionCode})");
        }

        [MenuItem("Lizzo/Internal Test/Build Android & Run")]
        private static void BuildAndroidAndRun()
        {
            string operationId = $"editor-{Guid.NewGuid():N}";
            if (EditorBuildRunCoordinator.TryAcquire(BuildOutputRoot, operationId, out EditorBuildRunCoordinator.Ownership ownership, out string failure) == false)
            {
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] {failure}");
                return;
            }

            _editorBuildRunOwnership = ownership;
            if (TryWriteEditorBuildStatus(EditorBuildRunCoordinator.PendingState, string.Empty, string.Empty, string.Empty, string.Empty) == false)
            {
                ownership.Dispose();
                _editorBuildRunOwnership = null;
                return;
            }

            if (EditorBuildRunSchedule.TrySchedule() == false)
            {
                TryWriteEditorBuildStatus(EditorBuildRunCoordinator.FailedState, string.Empty, "SCHEDULE_REJECTED", string.Empty, "An editor build callback is already scheduled.");
                ownership.Dispose();
                _editorBuildRunOwnership = null;
                UnityEngine.Debug.LogError("[InternalAndroidBuild] SCHEDULE_REJECTED: an editor build callback is already scheduled.");
                return;
            }

            EditorApplication.delayCall += RunScheduledEditorBuild;
            UnityEngine.Debug.Log($"[InternalAndroidBuild][PENDING] Build & Run scheduled. operation_id={operationId}. Invoke once; status={BuildStatusPath}.");
        }

        private static void RunScheduledEditorBuild()
        {
            EditorApplication.delayCall -= RunScheduledEditorBuild;
            EditorBuildRunCoordinator.Ownership ownership = _editorBuildRunOwnership;
            if (ownership == null)
            {
                EditorBuildRunSchedule.Complete();
                return;
            }

            string buildId = string.Empty;
            try
            {
                if (TryWriteEditorBuildStatus(EditorBuildRunCoordinator.RunningState, string.Empty, string.Empty, string.Empty, string.Empty) == false)
                    return;

                if (TryCreateEditorBuildRequest(out BuildRequest request, out string failure) == false)
                {
                    TryWriteEditorBuildStatus(EditorBuildRunCoordinator.FailedState, string.Empty, "FAILED", string.Empty, failure);
                    UnityEngine.Debug.LogError($"[InternalAndroidBuild] {failure}");
                    return;
                }

                buildId = request.BuildId;
                TryWriteEditorBuildStatus(EditorBuildRunCoordinator.RunningState, buildId, string.Empty, string.Empty, string.Empty);
                if (TryBuildAndroidApk(request, true, out string result) == false)
                {
                    TryWriteEditorBuildStatus(EditorBuildRunCoordinator.FailedState, buildId, result, string.Empty, result);
                    UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                    return;
                }

                TryWriteEditorBuildStatus(EditorBuildRunCoordinator.SucceededState, buildId, result, Path.Combine(BuildOutputRoot, buildId), string.Empty);
                UnityEngine.Debug.Log($"[InternalAndroidBuild] {result}. build_id={buildId}, Build & Run requested for authorized LG V50.");
            }
            catch (Exception exception)
            {
                TryWriteEditorBuildStatus(EditorBuildRunCoordinator.FailedState, buildId, "EXCEPTION", string.Empty, exception.Message);
                UnityEngine.Debug.LogException(exception);
            }
            finally
            {
                EditorBuildRunSchedule.Complete();
                ownership.Dispose();
                _editorBuildRunOwnership = null;
            }
        }

        public static void BuildApkFromCommandLine()
        {
            string operationId = $"cli-{Guid.NewGuid():N}";
            if (EditorBuildRunCoordinator.TryAcquire(BuildOutputRoot, operationId, out EditorBuildRunCoordinator.Ownership ownership, out string acquisitionFailure) == false)
            {
                CompleteCli(1, "FAILED", acquisitionFailure);
                return;
            }

            try
            {
                if (TryCreateCliBuildRequest(Environment.GetCommandLineArgs(), out BuildRequest request, out string failure) == false)
                {
                    EditorBuildRunCoordinator.WriteStatus(BuildOutputRoot, CreateStatus(ownership.OperationId, EditorBuildRunCoordinator.FailedState, string.Empty, "FAILED", string.Empty, failure));
                    CompleteCli(1, "FAILED", failure);
                    return;
                }

                EditorBuildRunCoordinator.WriteStatus(BuildOutputRoot, CreateStatus(ownership.OperationId, EditorBuildRunCoordinator.RunningState, request.BuildId, string.Empty, string.Empty, string.Empty));
                if (TryBuildAndroidApk(request, false, out string result) == false)
                {
                    EditorBuildRunCoordinator.WriteStatus(BuildOutputRoot, CreateStatus(ownership.OperationId, EditorBuildRunCoordinator.FailedState, request.BuildId, result, string.Empty, result));
                    CompleteCli(1, "FAILED", result);
                    return;
                }

                EditorBuildRunCoordinator.WriteStatus(BuildOutputRoot, CreateStatus(ownership.OperationId, EditorBuildRunCoordinator.SucceededState, request.BuildId, result, Path.Combine(BuildOutputRoot, request.BuildId), string.Empty));
                CompleteCli(0, result, $"build_id={request.BuildId}");
            }
            finally
            {
                ownership.Dispose();
            }
        }

        private static bool TryBuildAndroidApk(BuildRequest request, bool runAfterBuild, out string result)
        {
            result = "FAILED";
            if (BuildPipeline.isBuildingPlayer)
            {
                result = $"BuildPipeline is already building. build_id={request.BuildId}";
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                return false;
            }

            InternalBuildInfo buildInfo = InternalBuildInfo.Create(
                request.BuildId,
                PlayerSettings.productName,
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                PlayerSettings.bundleVersion,
                PlayerSettings.Android.bundleVersionCode,
                request.BuildDateUtc,
                Application.unityVersion,
                request.Revision);

            string internalTestRoot = Path.Combine(ProjectRoot, "Builds", "InternalTest");
            string buildRoot = Path.Combine(internalTestRoot, request.BuildId);
            string stagingRoot = Path.Combine(internalTestRoot, $".staging_{request.BuildId}");
            string stagingApkPath = Path.Combine(stagingRoot, $"AccidentalCommander_{buildInfo.versionName}_{request.BuildId}.apk");
            string finalApkPath = Path.Combine(buildRoot, Path.GetFileName(stagingApkPath));

            if (Directory.Exists(stagingRoot))
            {
                result = $"CONCURRENT_OR_INCOMPLETE staging directory exists. build_id={request.BuildId}";
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                return false;
            }

            if (Directory.Exists(buildRoot))
            {
                string integrityResult = string.Empty;
                if (TryValidateCompletedBuild(buildRoot, finalApkPath, buildInfo, out integrityResult))
                {
                    result = "ALREADY_COMPLETED";
                    UnityEngine.Debug.Log($"[InternalAndroidBuild] {result}. build_id={request.BuildId}, package={buildRoot}");
                    return true;
                }

                result = $"EXISTING_FINAL_INVALID build_id={request.BuildId}: {integrityResult}";
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                return false;
            }

            if (TryValidateCliPreflight(out string preflightFailure) == false)
            {
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] {preflightFailure}");
                result = preflightFailure;
                return false;
            }

            BuildSourceStateSnapshot sourceSnapshot = null;
            bool sourceSnapshotCaptured = false;
            bool sourceRestored = false;
            bool finalized = false;
            try
            {
                sourceSnapshot = BuildSourceStateSnapshot.Capture();
                sourceSnapshotCaptured = true;
                Directory.CreateDirectory(stagingRoot);
                WriteRuntimePayload(buildInfo);
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = GetEnabledScenes(),
                    locationPathName = stagingApkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport |
                        (runAfterBuild ? BuildOptions.AutoRunPlayer : BuildOptions.None),
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded || File.Exists(stagingApkPath) == false)
                {
                    result = $"APK build was not finalized. result={report.summary.result}, build_id={request.BuildId}";
                    UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                    return false;
                }

                if (TryRestoreBuildSourceState(sourceSnapshot, out string restoreFailure) == false)
                {
                    result = $"SOURCE_RESTORE_FAILED build_id={request.BuildId}: {restoreFailure}";
                    UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                    return false;
                }

                sourceRestored = true;
                if (IsFinalizationAllowed(sourceRestored, out string finalizationFailure) == false)
                {
                    result = $"FINALIZATION_BLOCKED build_id={request.BuildId}: {finalizationFailure}";
                    UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                    return false;
                }

                WriteBuildInfo(stagingRoot, buildInfo, report, stagingApkPath);
                if (TryApplyFinalOutputHygiene(stagingRoot, stagingApkPath, out string hygieneFailure) == false)
                {
                    result = $"STAGING_OUTPUT_HYGIENE_FAILED build_id={request.BuildId}: {hygieneFailure}";
                    UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                    return false;
                }

                if (TryValidateCompletedBuild(stagingRoot, stagingApkPath, buildInfo, out string integrityFailure) == false)
                {
                    result = $"STAGING_PACKAGE_INTEGRITY_FAILED build_id={request.BuildId}: {integrityFailure}";
                    UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                    return false;
                }

                Directory.Move(stagingRoot, buildRoot);
                finalized = true;
                result = "SUCCEEDED";
                UnityEngine.Debug.Log($"[InternalAndroidBuild] APK finalized: {buildRoot}");
                return true;
            }
            catch (Exception exception)
            {
                result = $"EXCEPTION build_id={request.BuildId}: {exception.Message}";
                UnityEngine.Debug.LogException(exception);
                return false;
            }
            finally
            {
                if (finalized == false)
                {
                    if (sourceSnapshotCaptured && sourceRestored == false && TryRestoreBuildSourceState(sourceSnapshot, out string restoreFailure) == false)
                    {
                        result = $"SOURCE_RESTORE_FAILED build_id={request.BuildId}: {restoreFailure}";
                        UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                    }

                    DeleteStagingDirectory(stagingRoot);
                }
            }
        }

        private static void CompleteCli(int exitCode, string state, string details)
        {
            string message = $"{CliLogPrefix}[{state}] exit_code={exitCode} {details}";
            if (exitCode == 0)
                UnityEngine.Debug.Log(message);
            else
                UnityEngine.Debug.LogError(message);

            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }

        [MenuItem("Lizzo/Internal Test/Build Android Addressables")]
        private static void BuildAndroidAddressables()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                UnityEngine.Debug.LogError("[InternalAndroidBuild] Switch the active build target to Android before building Addressables.");
                return;
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (string.IsNullOrEmpty(result.Error) == false)
            {
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] Android Addressables build failed: {result.Error}");
                return;
            }

            UnityEngine.Debug.Log($"[InternalAndroidBuild] Android Addressables built. locations={result.LocationCount}, output={result.OutputPath}");
        }

        private static string[] GetEnabledScenes()
        {
            EditorBuildSettingsScene[] enabledScenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
            string[] scenePaths = new string[enabledScenes.Length];
            for (int i = 0; i < enabledScenes.Length; i++)
            {
                scenePaths[i] = enabledScenes[i].path;
            }

            return scenePaths;
        }

        private static string GetRevision()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("git", "rev-parse --short HEAD")
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                if (TryRunProcess(startInfo, ProcessTimeoutMilliseconds, out int exitCode, out string output, out _, out _) == false || exitCode != 0)
                    return "UNVERIFIED";

                string revision = output.Trim();
                return string.IsNullOrWhiteSpace(revision) ? "UNVERIFIED" : revision;
            }
            catch (Exception)
            {
                return "UNVERIFIED";
            }
        }

        private static string BuildOutputRoot => Path.Combine(ProjectRoot, "Builds", "InternalTest");
        private static string BuildStatusPath => Path.Combine(BuildOutputRoot, EditorBuildRunCoordinator.StatusFileName);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
