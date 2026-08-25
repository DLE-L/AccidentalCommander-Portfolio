#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Lizzo.PV.Build;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Lizzo.PV.EditorTools
{
    public static partial class InternalAndroidBuildUtility
    {
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

    }
}
#endif
