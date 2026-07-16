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
using UnityEngine;
using UnityEngine.AddressableAssets;
using Lizzo.PV.Build;

namespace Lizzo.PV.EditorTools
{
    public static class ExternalTestBuildUtility
    {
        private const string ProductName = "Accidental Commander";
        private const string CompanyName = "Lizzo Studio";
        private const string AndroidBundleId = "com.lizzostudio.accidentalcommander";
        private const string Version = "0.1.0";
        private const int VersionCode = 2;
        private const string BuildIdArgument = "-externalTestBuildId";
        private const string BuildDateUtcArgument = "-externalTestBuildDateUtc";
        private const string CliLogPrefix = "[ExternalTestBuild][CLI]";
        private static readonly Regex BuildIdPattern = new Regex("^(?<time>\\d{6})_(?<revision>[0-9a-f]{7,40})$", RegexOptions.CultureInvariant);

        [MenuItem("Lizzo/External Test/Apply Android Settings")]
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

            UnityEngine.Debug.Log($"[ExternalTestBuild] Android settings applied. package={AndroidBundleId}, version={Version} ({VersionCode})");
        }

        [MenuItem("Lizzo/External Test/Build Android APK")]
        private static void BuildAndroidApk()
        {
            ApplyAndroidSettings();

            DateTime buildDateUtc = DateTime.UtcNow;
            string revision = GetRevision();
            string buildId = ExternalTestBuildInfo.CreateBuildId(buildDateUtc, revision);
            BuildRequest request = new BuildRequest(buildId, buildDateUtc, revision);
            TryBuildAndroidApk(request, false, out _);
        }

        public static void BuildAndroidApkFromCommandLine()
        {
            if (TryCreateCliBuildRequest(Environment.GetCommandLineArgs(), out BuildRequest request, out string failure) == false)
            {
                CompleteCli(1, "FAILED", failure);
                return;
            }

            if (TryBuildAndroidApk(request, true, out string result) == false)
            {
                CompleteCli(1, "FAILED", result);
                return;
            }

            CompleteCli(0, result, $"build_id={request.BuildId}");
        }

        private static bool TryBuildAndroidApk(BuildRequest request, bool isCli, out string result)
        {
            result = "FAILED";
            if (BuildPipeline.isBuildingPlayer)
            {
                result = $"BuildPipeline is already building. build_id={request.BuildId}";
                UnityEngine.Debug.LogError($"[ExternalTestBuild] {result}");
                return false;
            }

            ExternalTestBuildInfo buildInfo = ExternalTestBuildInfo.Create(
                request.BuildId,
                PlayerSettings.productName,
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                PlayerSettings.bundleVersion,
                PlayerSettings.Android.bundleVersionCode,
                request.BuildDateUtc,
                Application.unityVersion,
                request.Revision);

            string externalTestRoot = Path.Combine(ProjectRoot, "Builds", "ExternalTest");
            string buildRoot = Path.Combine(externalTestRoot, $"Build_{request.BuildDateUtc:yyyyMMdd}_{request.BuildId}");
            string stagingRoot = Path.Combine(externalTestRoot, $".staging_{request.BuildId}");
            string binariesDirectory = Path.Combine(stagingRoot, "01_Binaries");
            string stagingApkPath = Path.Combine(binariesDirectory, $"AccidentalCommander_{buildInfo.versionName}_{request.BuildId}.apk");
            string finalApkPath = Path.Combine(buildRoot, "01_Binaries", Path.GetFileName(stagingApkPath));

            if (Directory.Exists(stagingRoot))
            {
                result = $"CONCURRENT_OR_INCOMPLETE staging directory exists. build_id={request.BuildId}";
                UnityEngine.Debug.LogError($"[ExternalTestBuild] {result}");
                return false;
            }

            if (Directory.Exists(buildRoot))
            {
                string integrityResult = string.Empty;
                if (isCli && TryValidateCompletedArtifactPackage(buildRoot, finalApkPath, request.BuildId, out integrityResult))
                {
                    result = "ALREADY_COMPLETED";
                    UnityEngine.Debug.Log($"[ExternalTestBuild] {result}. build_id={request.BuildId}, package={buildRoot}");
                    return true;
                }

                result = isCli
                    ? $"EXISTING_FINAL_INVALID build_id={request.BuildId}: {integrityResult}"
                    : $"Refusing to overwrite an existing build directory. build_id={request.BuildId}";
                UnityEngine.Debug.LogError($"[ExternalTestBuild] {result}");
                return false;
            }

            if (isCli && TryValidateCliPreflight(out string preflightFailure) == false)
            {
                UnityEngine.Debug.LogError($"[ExternalTestBuild] {preflightFailure}");
                result = preflightFailure;
                return false;
            }

            Directory.CreateDirectory(binariesDirectory);
            RuntimePayloadSnapshot runtimePayload = default;
            bool runtimePayloadPrepared = false;
            bool finalized = false;
            try
            {
                runtimePayload = WriteRuntimePayload(buildInfo);
                runtimePayloadPrepared = true;
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = GetEnabledScenes(),
                    locationPathName = stagingApkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport,
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded || File.Exists(stagingApkPath) == false)
                {
                    result = $"APK build was not finalized. result={report.summary.result}, build_id={request.BuildId}";
                    UnityEngine.Debug.LogError($"[ExternalTestBuild] {result}");
                    return false;
                }

                WriteArtifactPackage(stagingRoot, buildInfo, report, finalApkPath);
                Directory.Move(stagingRoot, buildRoot);
                finalized = true;
                result = "SUCCEEDED";
                UnityEngine.Debug.Log($"[ExternalTestBuild] RC artifact package finalized: {buildRoot}");
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
                    if (runtimePayloadPrepared)
                        runtimePayload.Restore();
                    DeleteStagingDirectory(stagingRoot);
                }
            }
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
                string.Equals(ExternalTestBuildInfo.CreateBuildId(buildDateUtc, revision), buildId, StringComparison.Ordinal) == false)
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

        private static bool TryValidateCompletedArtifactPackage(string buildRoot, string expectedApkPath, string buildId, out string result)
        {
            string metadataDirectory = Path.Combine(buildRoot, "02_BuildMetadata");
            string manifestPath = Path.Combine(metadataDirectory, "build_manifest.json");
            string buildReportPath = Path.Combine(metadataDirectory, "Unity Build Report.txt");
            string releaseNotesPath = Path.Combine(metadataDirectory, "release_notes.md");
            string checksumsPath = Path.Combine(metadataDirectory, "checksums_sha256.txt");
            if (File.Exists(expectedApkPath) == false || File.Exists(manifestPath) == false || File.Exists(buildReportPath) == false || File.Exists(releaseNotesPath) == false || File.Exists(checksumsPath) == false)
            {
                result = "required APK or metadata artifact is missing";
                return false;
            }

            CompletedArtifactManifest manifest = JsonUtility.FromJson<CompletedArtifactManifest>(File.ReadAllText(manifestPath));
            string expectedRelativePath = ToRelativePath(buildRoot, expectedApkPath);
            string actualSha256 = ComputeSha256(expectedApkPath);
            if (manifest == null || manifest.buildId != buildId || manifest.buildResult != BuildResult.Succeeded.ToString() ||
                manifest.apkRelativePath != expectedRelativePath || manifest.apkSha256 != actualSha256)
            {
                result = "manifest identity or checksum does not match expected package";
                return false;
            }

            string checksums = File.ReadAllText(checksumsPath);
            if (checksums.Contains($"{actualSha256}  {expectedRelativePath}") == false ||
                File.ReadAllText(releaseNotesPath).Contains($"Build ID: `{buildId}`") == false ||
                File.ReadAllText(buildReportPath).Contains(Path.GetFileName(expectedApkPath)) == false)
            {
                result = "checksum, release notes, or build report does not match expected package";
                return false;
            }

            result = "integrity verified";
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

        [MenuItem("Lizzo/External Test/Build Android Addressables")]
        private static void BuildAndroidAddressables()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                UnityEngine.Debug.LogError("[ExternalTestBuild] Switch the active build target to Android before building Addressables.");
                return;
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (string.IsNullOrEmpty(result.Error) == false)
            {
                UnityEngine.Debug.LogError($"[ExternalTestBuild] Android Addressables build failed: {result.Error}");
                return;
            }

            UnityEngine.Debug.Log($"[ExternalTestBuild] Android Addressables built. locations={result.LocationCount}, output={result.OutputPath}");
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

        private static void WriteArtifactPackage(string buildRoot, ExternalTestBuildInfo buildInfo, BuildReport report, string finalApkPath)
        {
            string metadataDirectory = Path.Combine(buildRoot, "02_BuildMetadata");
            Directory.CreateDirectory(metadataDirectory);
            Directory.CreateDirectory(Path.Combine(buildRoot, "03_TestEvidence"));
            Directory.CreateDirectory(Path.Combine(buildRoot, "04_Captures"));
            Directory.CreateDirectory(Path.Combine(buildRoot, "05_StoreCompliance"));
            Directory.CreateDirectory(Path.Combine(buildRoot, "06_KnownIssues"));

            string stagingApkPath = report.summary.outputPath;
            if (report.summary.result != BuildResult.Succeeded || File.Exists(stagingApkPath) == false)
                throw new InvalidOperationException("A successful APK output is required before evidence metadata can be written.");

            string apkRelativePath = ToRelativePath(buildRoot, stagingApkPath);
            string apkSha256 = ComputeSha256(stagingApkPath);

            File.WriteAllText(Path.Combine(metadataDirectory, "build_manifest.json"), buildInfo.CreateManifestJson(report.summary.result.ToString(), apkRelativePath, apkSha256), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(metadataDirectory, "Unity Build Report.txt"), CreateBuildReportText(report, finalApkPath), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(metadataDirectory, "release_notes.md"), CreateReleaseNotes(buildInfo), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(metadataDirectory, "checksums_sha256.txt"), $"{apkSha256}  {apkRelativePath}{Environment.NewLine}", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(buildRoot, "06_KnownIssues", "known_issues.md"), CreateKnownIssues(), new UTF8Encoding(false));
        }

        private static string CreateBuildReportText(BuildReport report, string finalApkPath)
        {
            BuildSummary summary = report.summary;
            return $"Build result: {summary.result}\nPlatform: {summary.platform}\nOutput: {finalApkPath}\nStarted: {summary.buildStartedAt:O}\nEnded: {summary.buildEndedAt:O}\nDuration: {summary.totalTime}\nSize: {summary.totalSize}\nErrors: {summary.totalErrors}\nWarnings: {summary.totalWarnings}\n";
        }

        private static string CreateReleaseNotes(ExternalTestBuildInfo buildInfo)
        {
            return $"# {buildInfo.productName} {buildInfo.versionName}\n\n- Build ID: `{buildInfo.buildId}`\n- Version Name: `{buildInfo.versionName}`\n- versionCode: `{buildInfo.versionCode}`\n- Build Date (UTC): `{buildInfo.buildDateUtc}`\n- Unity Version: `{buildInfo.unityVersion}`\n- Android package: `{buildInfo.packageId}`\n- Revision: `{buildInfo.revision}`\n- Scope: first external-test candidate; real-device evidence pending.\n";
        }

        private static string CreateKnownIssues()
        {
            return "# Known Issues\n\n- LG V50 device validation and external-test evidence capture are pending for this build.\n";
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

        private static string ToRelativePath(string rootPath, string path)
        {
            return Path.GetRelativePath(rootPath, path).Replace('\\', '/');
        }

        private static RuntimePayloadSnapshot WriteRuntimePayload(ExternalTestBuildInfo buildInfo)
        {
            string payloadPath = Path.Combine(ProjectRoot, ExternalTestBuildInfo.RuntimePayloadAssetPath.Replace('/', Path.DirectorySeparatorChar));
            bool existed = File.Exists(payloadPath);
            string originalText = existed ? File.ReadAllText(payloadPath) : string.Empty;
            RuntimePayloadSnapshot snapshot = new RuntimePayloadSnapshot(existed, originalText);
            string directory = Path.GetDirectoryName(payloadPath);
            try
            {
                if (string.IsNullOrEmpty(directory) == false)
                    Directory.CreateDirectory(directory);

                File.WriteAllText(payloadPath, buildInfo.ToRuntimePayloadJson(), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(ExternalTestBuildInfo.RuntimePayloadAssetPath, ImportAssetOptions.ForceUpdate);
                return snapshot;
            }
            catch
            {
                snapshot.Restore();
                throw;
            }
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
                UnityEngine.Debug.LogError($"[ExternalTestBuild] Failed to clean staging output '{stagingRoot}': {exception.Message}");
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
        private sealed class CompletedArtifactManifest
        {
            public string buildId;
            public string buildResult;
            public string apkRelativePath;
            public string apkSha256;
        }

        private readonly struct RuntimePayloadSnapshot
        {
            private readonly bool _existed;
            private readonly string _originalText;

            public RuntimePayloadSnapshot(bool existed, string originalText)
            {
                _existed = existed;
                _originalText = originalText;
            }

            public void Restore()
            {
                string payloadPath = Path.Combine(ProjectRoot, ExternalTestBuildInfo.RuntimePayloadAssetPath.Replace('/', Path.DirectorySeparatorChar));
                if (_existed)
                {
                    File.WriteAllText(payloadPath, _originalText, new UTF8Encoding(false));
                    AssetDatabase.ImportAsset(ExternalTestBuildInfo.RuntimePayloadAssetPath, ImportAssetOptions.ForceUpdate);
                    return;
                }

                AssetDatabase.DeleteAsset(ExternalTestBuildInfo.RuntimePayloadAssetPath);
            }
        }
    }
}
#endif
