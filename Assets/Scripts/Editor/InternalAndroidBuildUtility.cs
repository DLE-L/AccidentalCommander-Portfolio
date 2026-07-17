#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    public static class InternalAndroidBuildUtility
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
        private static readonly Regex BuildIdPattern = new Regex("^(?<time>\\d{6})_(?<revision>[0-9a-f]{7,40})$", RegexOptions.CultureInvariant);

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
            if (TryCreateEditorBuildRequest(out BuildRequest request, out string failure) == false)
            {
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] {failure}");
                return;
            }

            if (TryBuildAndroidApk(request, true, out string result) == false)
            {
                UnityEngine.Debug.LogError($"[InternalAndroidBuild] {result}");
                return;
            }

            UnityEngine.Debug.Log($"[InternalAndroidBuild] {result}. build_id={request.BuildId}, Build & Run requested for authorized LG V50.");
        }

        public static void BuildApkFromCommandLine()
        {
            if (TryCreateCliBuildRequest(Environment.GetCommandLineArgs(), out BuildRequest request, out string failure) == false)
            {
                CompleteCli(1, "FAILED", failure);
                return;
            }

            if (TryBuildAndroidApk(request, false, out string result) == false)
            {
                CompleteCli(1, "FAILED", result);
                return;
            }

            CompleteCli(0, result, $"build_id={request.BuildId}");
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

        private static bool TryValidateAuthorizedAndroidDevice(out string failure)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(ResolveAdbPath(), "devices -l")
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using Process process = Process.Start(startInfo);
                if (process == null)
                {
                    failure = "PREFLIGHT_ANDROID_ADB_UNAVAILABLE";
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    failure = $"PREFLIGHT_ANDROID_ADB_FAILED: {error.Trim()}";
                    return false;
                }

                string authorizedModel = string.Empty;
                int authorizedDeviceCount = 0;
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int index = 1; index < lines.Length; index++)
                {
                    string[] fields = lines[index].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length < 2 || string.Equals(fields[1], "device", StringComparison.Ordinal) == false)
                        continue;

                    authorizedDeviceCount++;
                    for (int fieldIndex = 2; fieldIndex < fields.Length; fieldIndex++)
                    {
                        if (fields[fieldIndex].StartsWith("model:", StringComparison.Ordinal))
                        {
                            authorizedModel = fields[fieldIndex].Substring("model:".Length);
                            break;
                        }
                    }
                }

                if (authorizedDeviceCount != 1 || authorizedModel.IndexOf("V500", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failure = $"PREFLIGHT_AUTHORIZED_LG_V50_REQUIRED devices={authorizedDeviceCount}, model={authorizedModel}";
                    return false;
                }
            }
            catch (Exception exception)
            {
                failure = $"PREFLIGHT_ANDROID_ADB_UNAVAILABLE: {exception.Message}";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static string ResolveAdbPath()
        {
            string unitySdkAdbPath = Path.Combine(
                EditorApplication.applicationContentsPath,
                "PlaybackEngines",
                "AndroidPlayer",
                "SDK",
                "platform-tools",
                "adb.exe");
            if (File.Exists(unitySdkAdbPath))
                return unitySdkAdbPath;

            string sdkRoot = Environment.GetEnvironmentVariable("ANDROID_HOME");
            if (string.IsNullOrWhiteSpace(sdkRoot))
                sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

            if (string.IsNullOrWhiteSpace(sdkRoot) == false)
            {
                string sdkAdbPath = Path.Combine(sdkRoot, "platform-tools", "adb.exe");
                if (File.Exists(sdkAdbPath))
                    return sdkAdbPath;
            }

            return "adb";
        }

        private static bool IsSourceWorktreeClean(out string failure)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("git", "status --porcelain --untracked-files=all")
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using Process process = Process.Start(startInfo);
                string output = process?.StandardOutput.ReadToEnd();
                process?.WaitForExit();
                if (process == null || process.ExitCode != 0)
                {
                    failure = "PREFLIGHT_GIT_STATUS_UNAVAILABLE";
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

        private static bool TryValidateCompletedBuild(string buildRoot, string expectedApkPath, InternalBuildInfo expectedBuildInfo, out string result)
        {
            if (TryValidateAllowedBuildInventory(buildRoot, expectedApkPath, out string inventoryFailure) == false)
            {
                result = inventoryFailure;
                return false;
            }

            string buildInfoPath = Path.Combine(buildRoot, "build_info.json");
            if (File.Exists(expectedApkPath) == false || File.Exists(buildInfoPath) == false)
            {
                result = "required APK or build_info artifact is missing";
                return false;
            }

            CompletedBuildInfo completedBuildInfo = JsonUtility.FromJson<CompletedBuildInfo>(File.ReadAllText(buildInfoPath));
            string actualSha256 = ComputeSha256(expectedApkPath);
            if (completedBuildInfo == null ||
                completedBuildInfo.buildId != expectedBuildInfo.buildId ||
                completedBuildInfo.productName != expectedBuildInfo.productName ||
                completedBuildInfo.packageId != expectedBuildInfo.packageId ||
                completedBuildInfo.versionName != expectedBuildInfo.versionName ||
                completedBuildInfo.versionCode != expectedBuildInfo.versionCode ||
                completedBuildInfo.buildDateUtc != expectedBuildInfo.buildDateUtc ||
                completedBuildInfo.unityVersion != expectedBuildInfo.unityVersion ||
                completedBuildInfo.revision != expectedBuildInfo.revision ||
                completedBuildInfo.buildResult != BuildResult.Succeeded.ToString() ||
                completedBuildInfo.apkFileName != Path.GetFileName(expectedApkPath) ||
                completedBuildInfo.apkSha256 != actualSha256)
            {
                result = "build_info identity, result, APK path, or checksum does not match expected package";
                return false;
            }

            result = "integrity verified";
            return true;
        }

        private static bool TryApplyFinalOutputHygiene(string buildRoot, string apkPath, out string failure)
        {
            if (TryRemoveExpectedAuxiliaryDirectory(
                    buildRoot,
                    "Accidental Commander_BurstDebugInformation_DoNotShip",
                    out failure) == false ||
                TryRemoveExpectedAuxiliaryDirectory(
                    buildRoot,
                    $"{Path.GetFileNameWithoutExtension(apkPath)}_BackUpThisFolder_ButDontShipItWithYourGame",
                    out failure) == false)
            {
                return false;
            }

            return TryValidateAllowedBuildInventory(buildRoot, apkPath, out failure);
        }

        private static bool TryRemoveExpectedAuxiliaryDirectory(string buildRoot, string directoryName, out string failure)
        {
            if (TryGetDirectChildPath(buildRoot, directoryName, out string directoryPath, out failure) == false)
                return false;

            if (File.Exists(directoryPath))
            {
                failure = $"expected auxiliary directory path is a file: {directoryName}";
                return false;
            }

            if (Directory.Exists(directoryPath) == false)
            {
                failure = string.Empty;
                return true;
            }

            if (TryVerifyNoReparsePoints(directoryPath, out failure) == false)
                return false;

            Directory.Delete(directoryPath, true);
            failure = string.Empty;
            return true;
        }

        private static bool TryValidateAllowedBuildInventory(string buildRoot, string expectedApkPath, out string failure)
        {
            if (Directory.Exists(buildRoot) == false)
            {
                failure = "build output directory is missing";
                return false;
            }

            if (TryVerifyNoReparsePoints(buildRoot, out failure) == false)
                return false;

            string expectedApkName = Path.GetFileName(expectedApkPath);
            string expectedSymbolsPrefix = $"{Path.GetFileNameWithoutExtension(expectedApkPath)}-";
            const string SymbolsSuffix = "-IL2CPP.symbols.zip";
            int symbolsCount = 0;

            foreach (FileSystemInfo entry in new DirectoryInfo(buildRoot).EnumerateFileSystemInfos())
            {
                if (entry is DirectoryInfo)
                {
                    failure = $"unexpected output directory remains: {entry.Name}";
                    return false;
                }

                if (entry.Name == expectedApkName || entry.Name == "build_info.json")
                    continue;

                if (entry.Name.StartsWith(expectedSymbolsPrefix, StringComparison.Ordinal) &&
                    entry.Name.EndsWith(SymbolsSuffix, StringComparison.Ordinal))
                {
                    symbolsCount++;
                    continue;
                }

                failure = $"unexpected output file remains: {entry.Name}";
                return false;
            }

            if (symbolsCount != 1)
            {
                failure = $"expected exactly one IL2CPP symbols ZIP, found {symbolsCount}";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static bool TryGetDirectChildPath(string buildRoot, string childName, out string childPath, out string failure)
        {
            childPath = string.Empty;
            if (string.IsNullOrWhiteSpace(childName) || Path.GetFileName(childName) != childName)
            {
                failure = "output child name is invalid";
                return false;
            }

            string fullBuildRoot = Path.GetFullPath(buildRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            childPath = Path.GetFullPath(Path.Combine(fullBuildRoot, childName));
            if (string.Equals(Path.GetDirectoryName(childPath), fullBuildRoot, StringComparison.OrdinalIgnoreCase) == false)
            {
                failure = $"output child is not a direct child of the build root: {childName}";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static bool TryVerifyNoReparsePoints(string directoryPath, out string failure)
        {
            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                failure = $"reparse point is not allowed in build output: {directory.Name}";
                return false;
            }

            Stack<DirectoryInfo> pendingDirectories = new Stack<DirectoryInfo>();
            pendingDirectories.Push(directory);
            while (pendingDirectories.Count > 0)
            {
                DirectoryInfo currentDirectory = pendingDirectories.Pop();
                foreach (FileSystemInfo entry in currentDirectory.EnumerateFileSystemInfos())
                {
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        failure = $"reparse point is not allowed in build output: {entry.FullName}";
                        return false;
                    }

                    if (entry is DirectoryInfo childDirectory)
                        pendingDirectories.Push(childDirectory);
                }
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
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using Process process = Process.Start(startInfo);
                string revision = process?.StandardOutput.ReadToEnd().Trim();
                process?.WaitForExit();
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

        [Serializable]
        private sealed class CompletedBuildInfo
        {
            public string buildId;
            public string productName;
            public string packageId;
            public string versionName;
            public int versionCode;
            public string buildDateUtc;
            public string unityVersion;
            public string revision;
            public string buildResult;
            public string apkFileName;
            public string apkSha256;
        }

        private sealed class BuildSourceStateSnapshot
        {
            private readonly GeneratedFileSnapshot[] _fileSnapshots;
            private readonly UnityEngine.Object[] _preloadedAssets;
            private readonly bool _preloadedAssetsWereNull;
            private readonly bool _resourcesDirectoryExisted;
            private readonly bool _internalTestDirectoryExisted;

            private BuildSourceStateSnapshot(
                GeneratedFileSnapshot[] fileSnapshots,
                UnityEngine.Object[] preloadedAssets,
                bool preloadedAssetsWereNull,
                bool resourcesDirectoryExisted,
                bool internalTestDirectoryExisted)
            {
                _fileSnapshots = fileSnapshots;
                _preloadedAssets = preloadedAssets;
                _preloadedAssetsWereNull = preloadedAssetsWereNull;
                _resourcesDirectoryExisted = resourcesDirectoryExisted;
                _internalTestDirectoryExisted = internalTestDirectoryExisted;
            }

            public static BuildSourceStateSnapshot Capture()
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                string resourcesDirectory = ToFullPath("Assets/Resources");
                string internalTestDirectory = ToFullPath("Assets/Resources/InternalTest");
                GeneratedFileSnapshot[] fileSnapshots =
                {
                    GeneratedFileSnapshot.Capture(InternalBuildInfo.RuntimePayloadAssetPath),
                    GeneratedFileSnapshot.Capture(BuildInfoMetaAssetPath),
                    GeneratedFileSnapshot.Capture(ResourcesMetaAssetPath),
                    GeneratedFileSnapshot.Capture(InternalTestResourcesMetaAssetPath),
                    GeneratedFileSnapshot.Capture(AddressablesLinkAssetPath),
                    GeneratedFileSnapshot.Capture(AddressablesLinkMetaAssetPath),
                };

                UnityEngine.Object[] originalPreloadedAssets = PlayerSettings.GetPreloadedAssets();
                UnityEngine.Object[] preloadedAssets = originalPreloadedAssets ?? Array.Empty<UnityEngine.Object>();
                UnityEngine.Object[] preloadedAssetsCopy = new UnityEngine.Object[preloadedAssets.Length];
                Array.Copy(preloadedAssets, preloadedAssetsCopy, preloadedAssets.Length);
                return new BuildSourceStateSnapshot(
                    fileSnapshots,
                    preloadedAssetsCopy,
                    originalPreloadedAssets == null,
                    Directory.Exists(resourcesDirectory),
                    Directory.Exists(internalTestDirectory));
            }

            public bool TryRestoreAndVerify(out string failure)
            {
                try
                {
                    for (int index = 0; index < _fileSnapshots.Length; index++)
                        _fileSnapshots[index].RestoreToSnapshot();

                    RestoreAbsentGeneratedDirectory("Assets/Resources/InternalTest", _internalTestDirectoryExisted);
                    RestoreAbsentGeneratedDirectory("Assets/Resources", _resourcesDirectoryExisted);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    PlayerSettings.SetPreloadedAssets(_preloadedAssetsWereNull ? null : _preloadedAssets);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    return IsRestored(out failure);
                }
                catch (Exception exception)
                {
                    failure = exception.Message;
                    return false;
                }
            }

            private bool IsRestored(out string failure)
            {
                for (int index = 0; index < _fileSnapshots.Length; index++)
                {
                    if (_fileSnapshots[index].MatchesSnapshot() == false)
                    {
                        failure = $"generated source mismatch: {_fileSnapshots[index].AssetPath}";
                        return false;
                    }
                }

                if (Directory.Exists(ToFullPath("Assets/Resources")) != _resourcesDirectoryExisted ||
                    Directory.Exists(ToFullPath("Assets/Resources/InternalTest")) != _internalTestDirectoryExisted)
                {
                    failure = "generated Resources directory existence mismatch";
                    return false;
                }

                UnityEngine.Object[] restoredPreloadedAssets = PlayerSettings.GetPreloadedAssets();
                if ((_preloadedAssetsWereNull && restoredPreloadedAssets != null) ||
                    (_preloadedAssetsWereNull == false && AreSameAssets(_preloadedAssets, restoredPreloadedAssets) == false))
                {
                    failure = "preloaded assets mismatch";
                    return false;
                }

                failure = string.Empty;
                return true;
            }

            private static void RestoreAbsentGeneratedDirectory(string assetPath, bool existedBeforeBuild)
            {
                if (existedBeforeBuild)
                    return;

                string directoryPath = ToFullPath(assetPath);
                if (Directory.Exists(directoryPath) == false)
                    return;

                if (Directory.GetFileSystemEntries(directoryPath).Length != 0)
                    throw new InvalidOperationException($"Expected generated directory to be empty before removal: {assetPath}");

                Directory.Delete(directoryPath, false);
            }

            private static bool AreSameAssets(UnityEngine.Object[] expected, UnityEngine.Object[] actual)
            {
                expected = expected ?? Array.Empty<UnityEngine.Object>();
                actual = actual ?? Array.Empty<UnityEngine.Object>();
                if (expected.Length != actual.Length)
                    return false;

                for (int index = 0; index < expected.Length; index++)
                {
                    if (expected[index] != actual[index])
                        return false;
                }

                return true;
            }
        }

        private sealed class GeneratedFileSnapshot
        {
            public string AssetPath { get; }
            private readonly bool _existed;
            private readonly byte[] _contents;

            private GeneratedFileSnapshot(string assetPath, bool existed, byte[] contents)
            {
                AssetPath = assetPath;
                _existed = existed;
                _contents = contents;
            }

            public static GeneratedFileSnapshot Capture(string assetPath)
            {
                string fullPath = ToFullPath(assetPath);
                return new GeneratedFileSnapshot(assetPath, File.Exists(fullPath), File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : Array.Empty<byte>());
            }

            public void RestoreToSnapshot()
            {
                string fullPath = ToFullPath(AssetPath);
                if (_existed)
                {
                    string directory = Path.GetDirectoryName(fullPath);
                    if (string.IsNullOrEmpty(directory) == false)
                        Directory.CreateDirectory(directory);
                    File.WriteAllBytes(fullPath, _contents);
                    return;
                }

                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }

            public bool MatchesSnapshot()
            {
                string fullPath = ToFullPath(AssetPath);
                if (File.Exists(fullPath) != _existed)
                    return false;

                if (_existed == false)
                    return true;

                byte[] currentContents = File.ReadAllBytes(fullPath);
                if (currentContents.Length != _contents.Length)
                    return false;

                for (int index = 0; index < currentContents.Length; index++)
                {
                    if (currentContents[index] != _contents[index])
                        return false;
                }

                return true;
            }
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
