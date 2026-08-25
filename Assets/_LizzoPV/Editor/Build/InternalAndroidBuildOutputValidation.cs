#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Lizzo.PV.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lizzo.PV.EditorTools
{
    public static partial class InternalAndroidBuildUtility
    {
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

        internal static bool TryValidateAllowedBuildInventory(string buildRoot, string expectedApkPath, out string failure)
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

            if (symbolsCount > 1)
            {
                failure = $"expected at most one IL2CPP symbols ZIP, found {symbolsCount}";
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

    }
}
#endif
