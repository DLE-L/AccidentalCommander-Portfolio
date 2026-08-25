#if UNITY_EDITOR
using System;
using System.IO;
using Lizzo.PV.Build;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools
{
    public static partial class InternalAndroidBuildUtility
    {
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
    }
}
#endif
