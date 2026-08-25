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

        private static bool TryCreateEditorBuildRequest(out BuildRequest request, out string failure)
        {
            request = default;
            if (Application.isBatchMode)
            {
                failure = "PREFLIGHT_EDITOR_ONLY";
                return false;
            }

            if (TryValidateEditorBuildPreflight(out failure) == false)
                return false;

            string revision = GetRevision();
            if (string.Equals(revision, "UNVERIFIED", StringComparison.Ordinal))
            {
                failure = "PREFLIGHT_UNVERIFIED_REVISION";
                return false;
            }

            DateTime buildDateUtc = DateTime.UtcNow;
            request = new BuildRequest(InternalBuildInfo.CreateBuildId(buildDateUtc, revision), buildDateUtc, revision);
            failure = string.Empty;
            return true;
        }

        private static bool TryCreateCliBuildRequest(string[] commandLineArgs, out BuildRequest request, out string failure)
        {
            request = default;
            if (TryGetSingleCommandLineValue(commandLineArgs, BuildIdArgument, out string buildId, out failure) == false ||
                TryGetSingleCommandLineValue(commandLineArgs, BuildDateUtcArgument, out string buildDateText, out failure) == false)
                return false;

            Match buildIdMatch = BuildIdPattern.Match(buildId);
            if (buildIdMatch.Success == false)
            {
                failure = $"INVALID_BUILD_ID expected HHmmss_revision: '{buildId}'";
                return false;
            }

            if (DateTime.TryParseExact(buildDateText, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime buildDateUtc) == false)
            {
                failure = $"INVALID_BUILD_DATE_UTC expected yyyy-MM-ddTHH:mm:ssZ: '{buildDateText}'";
                return false;
            }

            string revision = GetRevision();
            if (string.Equals(revision, "UNVERIFIED", StringComparison.Ordinal))
            {
                failure = "UNVERIFIED_REVISION current git revision is required for CLI build.";
                return false;
            }

            if (string.Equals(buildIdMatch.Groups["revision"].Value, revision, StringComparison.Ordinal) == false ||
                string.Equals(InternalBuildInfo.CreateBuildId(buildDateUtc, revision), buildId, StringComparison.Ordinal) == false)
            {
                failure = $"BUILD_ID_REVISION_MISMATCH requested={buildId}, current_revision={revision}, requested_date_utc={buildDateText}";
                return false;
            }

            request = new BuildRequest(buildId, buildDateUtc, revision);
            failure = string.Empty;
            return true;
        }

        private static bool TryGetSingleCommandLineValue(string[] commandLineArgs, string argumentName, out string value, out string failure)
        {
            value = string.Empty;
            failure = string.Empty;
            int matchCount = 0;
            for (int index = 0; index < commandLineArgs.Length; index++)
            {
                if (string.Equals(commandLineArgs[index], argumentName, StringComparison.Ordinal) == false)
                    continue;

                if (index + 1 >= commandLineArgs.Length || string.IsNullOrWhiteSpace(commandLineArgs[index + 1]))
                {
                    failure = $"MISSING_ARGUMENT_VALUE {argumentName}";
                    return false;
                }

                value = commandLineArgs[index + 1].Trim();
                matchCount++;
            }

            if (matchCount != 1)
            {
                failure = matchCount == 0 ? $"MISSING_REQUIRED_ARGUMENT {argumentName}" : $"DUPLICATE_ARGUMENT {argumentName}";
                return false;
            }

            return true;
        }

        private static bool TryValidateEditorBuildPreflight(out string failure)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                failure = "PREFLIGHT_PLAY_MODE_ACTIVE_OR_PENDING";
                return false;
            }

            if (EditorApplication.isCompiling)
            {
                failure = "PREFLIGHT_EDITOR_COMPILING";
                return false;
            }

            if (EditorApplication.isUpdating)
            {
                failure = "PREFLIGHT_EDITOR_UPDATING";
                return false;
            }

            if (BuildPipeline.isBuildingPlayer)
            {
                failure = "PREFLIGHT_BUILD_ALREADY_RUNNING";
                return false;
            }

            if (TryValidateCliPreflight(out failure) == false)
                return false;

            if (GetEnabledScenes().Length == 0)
            {
                failure = "PREFLIGHT_ENABLED_SCENES_MISSING";
                return false;
            }

            for (int index = 0; index < EditorBuildSettings.scenes.Length; index++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[index];
                if (scene.enabled && File.Exists(ToFullPath(scene.path)) == false)
                {
                    failure = $"PREFLIGHT_ENABLED_SCENE_MISSING path={scene.path}";
                    return false;
                }
            }

            if (TryValidateAuthorizedAndroidDevice(out failure) == false)
                return false;

            if (TryValidateNoUnsavedScenes(out failure) == false)
                return false;

            failure = string.Empty;
            return true;
        }

        private static bool TryValidateCliPreflight(out string failure)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                failure = "PREFLIGHT_ACTIVE_TARGET_NOT_ANDROID";
                return false;
            }

            if (PlayerSettings.productName != ProductName ||
                PlayerSettings.companyName != CompanyName ||
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != AndroidBundleId ||
                PlayerSettings.bundleVersion != Version ||
                PlayerSettings.Android.bundleVersionCode != VersionCode ||
                PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait ||
                PlayerSettings.allowedAutorotateToPortrait == false ||
                PlayerSettings.allowedAutorotateToPortraitUpsideDown ||
                PlayerSettings.allowedAutorotateToLandscapeLeft ||
                PlayerSettings.allowedAutorotateToLandscapeRight)
            {
                failure = "PREFLIGHT_ANDROID_IDENTITY_OR_ORIENTATION_MISMATCH";
                return false;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrWhiteSpace(settings.activeProfileId))
            {
                failure = "PREFLIGHT_ADDRESSABLES_SETTINGS_OR_PROFILE_MISSING";
                return false;
            }

            string androidOutput = Path.Combine(Addressables.BuildPath, BuildTarget.Android.ToString());
            if (Directory.Exists(androidOutput) == false)
            {
                failure = $"PREFLIGHT_ANDROID_ADDRESSABLES_OUTPUT_MISSING path={androidOutput}";
                return false;
            }

            if (IsSourceWorktreeClean(out string worktreeFailure) == false)
            {
                failure = worktreeFailure;
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static bool TryValidateNoUnsavedScenes(out string failure)
        {
            for (int index = 0; index < EditorSceneManager.sceneCount; index++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(index);
                if (scene.isDirty)
                {
                    failure = $"PREFLIGHT_UNSAVED_SCENE_CHANGES path={scene.path}";
                    return false;
                }
            }

            failure = string.Empty;
            return true;
        }

        private static bool IsSourceWorktreeClean(out string failure)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("git", "status --porcelain --untracked-files=all")
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                if (TryRunProcess(startInfo, ProcessTimeoutMilliseconds, out int exitCode, out string output, out string processError, out string processFailure) == false || exitCode != 0)
                {
                    string detail = string.IsNullOrWhiteSpace(processFailure) ? processError.Trim() : processFailure;
                    failure = string.IsNullOrWhiteSpace(detail)
                        ? "PREFLIGHT_GIT_STATUS_UNAVAILABLE"
                        : $"PREFLIGHT_GIT_STATUS_UNAVAILABLE: {detail}";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(output) == false)
                {
                    failure = "PREFLIGHT_WORKTREE_NOT_CLEAN";
                    return false;
                }
            }
            catch (Exception exception)
            {
                failure = $"PREFLIGHT_GIT_STATUS_UNAVAILABLE: {exception.Message}";
                return false;
            }

            failure = string.Empty;
            return true;
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

        private static void WriteBuildInfo(string buildRoot, InternalBuildInfo buildInfo, BuildReport report, string apkPath)
        {
            if (report.summary.result != BuildResult.Succeeded || File.Exists(apkPath) == false)
                throw new InvalidOperationException("A successful APK output is required before build_info can be written.");

            string apkSha256 = ComputeSha256(apkPath);
            File.WriteAllText(
                Path.Combine(buildRoot, "build_info.json"),
                buildInfo.CreateBuildInfoJson(report.summary.result.ToString(), Path.GetFileName(apkPath), apkSha256),
                new UTF8Encoding(false));
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

        private static string ComputeSha256(string filePath)
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(filePath);
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void WriteRuntimePayload(InternalBuildInfo buildInfo)
        {
            string payloadPath = Path.Combine(ProjectRoot, InternalBuildInfo.RuntimePayloadAssetPath.Replace('/', Path.DirectorySeparatorChar));
            string directory = Path.GetDirectoryName(payloadPath);
            if (string.IsNullOrEmpty(directory) == false)
                Directory.CreateDirectory(directory);

            File.WriteAllText(payloadPath, buildInfo.ToRuntimePayloadJson(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(InternalBuildInfo.RuntimePayloadAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
        }

        private static bool TryRestoreBuildSourceState(BuildSourceStateSnapshot sourceSnapshot, out string failure)
        {
            if (sourceSnapshot == null)
            {
                failure = "source snapshot is unavailable";
                return false;
            }

            return sourceSnapshot.TryRestoreAndVerify(out failure);
        }

        private static bool IsFinalizationAllowed(bool sourceRestored, out string failure)
        {
            if (sourceRestored == false)
            {
                failure = "source restore verification did not succeed";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static void DeleteStagingDirectory(string stagingRoot)
        {
            if (Directory.Exists(stagingRoot) == false)
                return;

            try
            {
                Directory.Delete(stagingRoot, true);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] Failed to clean staging output '{stagingRoot}': {exception.Message}");
            }
        }

        private static bool TryWriteEditorBuildStatus(string state, string buildId, string result, string packagePath, string error)
        {
            if (_editorBuildRunOwnership == null)
                return false;

            try
            {
                EditorBuildRunCoordinator.WriteStatus(
                    BuildOutputRoot,
                    CreateStatus(_editorBuildRunOwnership.OperationId, state, buildId, result, packagePath, error));
                return true;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] Failed to write durable status: {exception.Message}");
                return false;
            }
        }

        private static EditorBuildRunCoordinator.EditorBuildRunStatus CreateStatus(
            string operationId,
            string state,
            string buildId,
            string result,
            string packagePath,
            string error)
        {
            return new EditorBuildRunCoordinator.EditorBuildRunStatus
            {
                state = state,
                operationId = operationId,
                ownerProcessId = Process.GetCurrentProcess().Id,
                startedAtUtc = DateTime.UtcNow.ToString("O"),
                buildId = buildId,
                result = result,
                packagePath = packagePath,
                error = error,
            };
        }

        private static string BuildOutputRoot => Path.Combine(ProjectRoot, "Builds", "InternalTest");
        private static string BuildStatusPath => Path.Combine(BuildOutputRoot, EditorBuildRunCoordinator.StatusFileName);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();

        private readonly struct BuildRequest
        {
            public readonly string BuildId;
            public readonly DateTime BuildDateUtc;
            public readonly string Revision;

            public BuildRequest(string buildId, DateTime buildDateUtc, string revision)
            {
                BuildId = buildId;
                BuildDateUtc = buildDateUtc;
                Revision = revision;
            }
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
