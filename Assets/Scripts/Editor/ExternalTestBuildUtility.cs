#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Android.Types;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Lizzo.PV.Build;

namespace Lizzo.PV.EditorTools
{
    internal static class ExternalTestBuildUtility
    {
        private const string ProductName = "Accidental Commander";
        private const string CompanyName = "Lizzo Studio";
        private const string AndroidBundleId = "com.lizzostudio.accidentalcommander";
        private const string Version = "0.1.0";
        private const int VersionCode = 2;

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
            ExternalTestBuildInfo buildInfo = ExternalTestBuildInfo.Create(
                buildId,
                PlayerSettings.productName,
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                PlayerSettings.bundleVersion,
                PlayerSettings.Android.bundleVersionCode,
                buildDateUtc,
                Application.unityVersion,
                revision);

            string externalTestRoot = Path.Combine(ProjectRoot, "Builds", "ExternalTest");
            string buildRoot = Path.Combine(externalTestRoot, $"Build_{buildDateUtc:yyyyMMdd}_{buildId}");
            string stagingRoot = Path.Combine(externalTestRoot, $".staging_{buildId}");
            string binariesDirectory = Path.Combine(stagingRoot, "01_Binaries");
            string stagingApkPath = Path.Combine(binariesDirectory, $"AccidentalCommander_{buildInfo.versionName}_{buildId}.apk");
            string finalApkPath = Path.Combine(buildRoot, "01_Binaries", Path.GetFileName(stagingApkPath));
            if (Directory.Exists(buildRoot) || Directory.Exists(stagingRoot))
            {
                UnityEngine.Debug.LogError($"[ExternalTestBuild] Refusing to overwrite an existing build directory. build_id={buildId}");
                return;
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
                    UnityEngine.Debug.LogError($"[ExternalTestBuild] APK build was not finalized. result={report.summary.result}, build_id={buildId}");
                    return;
                }

                WriteArtifactPackage(stagingRoot, buildInfo, report, finalApkPath);
                Directory.Move(stagingRoot, buildRoot);
                finalized = true;
                UnityEngine.Debug.Log($"[ExternalTestBuild] RC artifact package finalized: {buildRoot}");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
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
