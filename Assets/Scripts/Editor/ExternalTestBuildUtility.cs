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

namespace Lizzo.PV.EditorTools
{
    internal static class ExternalTestBuildUtility
    {
        private const string ProductName = "Accidental Commander";
        private const string CompanyName = "Lizzo";
        private const string AndroidBundleId = "com.Lizzo.accidentalcommander";
        private const string Version = "0.1.0";
        private const int VersionCode = 1;

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

            string buildId = CreateBuildId();
            string buildRoot = Path.Combine(ProjectRoot, "Builds", "ExternalTest", $"Build_{DateTime.UtcNow:yyyyMMdd}_{buildId}");
            string binariesDirectory = Path.Combine(buildRoot, "01_Binaries");
            string apkPath = Path.Combine(binariesDirectory, $"AccidentalCommander_{Version}_{buildId}.apk");
            Directory.CreateDirectory(binariesDirectory);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            WriteArtifactPackage(buildRoot, buildId, report);
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

        private static void WriteArtifactPackage(string buildRoot, string buildId, BuildReport report)
        {
            string metadataDirectory = Path.Combine(buildRoot, "02_BuildMetadata");
            Directory.CreateDirectory(metadataDirectory);
            Directory.CreateDirectory(Path.Combine(buildRoot, "03_TestEvidence"));
            Directory.CreateDirectory(Path.Combine(buildRoot, "04_Captures"));
            Directory.CreateDirectory(Path.Combine(buildRoot, "05_StoreCompliance"));
            Directory.CreateDirectory(Path.Combine(buildRoot, "06_KnownIssues"));

            string apkPath = report.summary.outputPath;
            BuildManifest manifest = new BuildManifest
            {
                buildId = buildId,
                productName = PlayerSettings.productName,
                packageId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                versionName = PlayerSettings.bundleVersion,
                versionCode = PlayerSettings.Android.bundleVersionCode,
                buildDateUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                revision = GetRevision(),
                buildResult = report.summary.result.ToString(),
                apkRelativePath = ToRelativePath(buildRoot, apkPath),
                apkSha256 = File.Exists(apkPath) ? ComputeSha256(apkPath) : "UNVERIFIED",
            };

            File.WriteAllText(Path.Combine(metadataDirectory, "build_manifest.json"), JsonUtility.ToJson(manifest, true), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(metadataDirectory, "Unity Build Report.txt"), CreateBuildReportText(report), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(metadataDirectory, "release_notes.md"), CreateReleaseNotes(manifest), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(metadataDirectory, "checksums_sha256.txt"), $"{manifest.apkSha256}  {manifest.apkRelativePath}{Environment.NewLine}", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(buildRoot, "06_KnownIssues", "known_issues.md"), "# Known Issues\n\n- Android icon asset is not assigned yet. External distribution GO is blocked until it is applied and verified on device.\n", new UTF8Encoding(false));

            UnityEngine.Debug.Log($"[ExternalTestBuild] Artifact package written: {buildRoot}");
        }

        private static string CreateBuildReportText(BuildReport report)
        {
            BuildSummary summary = report.summary;
            return $"Build result: {summary.result}\nPlatform: {summary.platform}\nOutput: {summary.outputPath}\nStarted: {summary.buildStartedAt:O}\nEnded: {summary.buildEndedAt:O}\nDuration: {summary.totalTime}\nSize: {summary.totalSize}\nErrors: {summary.totalErrors}\nWarnings: {summary.totalWarnings}\n";
        }

        private static string CreateReleaseNotes(BuildManifest manifest)
        {
            return $"# {manifest.productName} {manifest.versionName}\n\n- Build ID: `{manifest.buildId}`\n- Android package: `{manifest.packageId}`\n- Revision: `{manifest.revision}`\n- Scope: first external-test candidate; real-device evidence pending.\n";
        }

        private static string CreateBuildId()
        {
            return $"{DateTime.UtcNow:HHmmss}_{GetRevision()}";
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

        private static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();

        [Serializable]
        private sealed class BuildManifest
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
            public string apkRelativePath;
            public string apkSha256;
        }
    }
}
#endif
