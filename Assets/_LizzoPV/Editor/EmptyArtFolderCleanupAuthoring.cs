using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class EmptyArtFolderCleanupAuthoring
    {
        private const string SelfPath =
            "Assets/_LizzoPV/Editor/EmptyArtFolderCleanupAuthoring.cs";

        private const string LegacyArtRoot = "Assets/_LizzoPV/Art";

        public static void Apply()
        {
            try
            {
                ValidateLegacyTreeIsEmpty();
                ValidateMovedArtAssets();

                if (!AssetDatabase.DeleteAsset(LegacyArtRoot))
                {
                    throw new InvalidOperationException($"Failed to delete empty legacy Art root: {LegacyArtRoot}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                if (AssetDatabase.IsValidFolder(LegacyArtRoot) ||
                    Directory.Exists(LegacyArtRoot) ||
                    File.Exists(LegacyArtRoot + ".meta"))
                {
                    throw new InvalidOperationException($"Legacy Art root still exists: {LegacyArtRoot}");
                }

                ValidateMovedArtAssets();

                Debug.Log(
                    "[Empty Art Folder Cleanup] PASS: removed the empty legacy Art root and two folder metas; " +
                    "59 moved art files remain loadable in App and Legion ownership.");

                if (!AssetDatabase.DeleteAsset(SelfPath))
                {
                    throw new InvalidOperationException($"Failed to remove temporary authoring helper: {SelfPath}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void ValidateLegacyTreeIsEmpty()
        {
            if (!AssetDatabase.IsValidFolder(LegacyArtRoot) ||
                !Directory.Exists(LegacyArtRoot) ||
                !File.Exists(LegacyArtRoot + ".meta"))
            {
                throw new InvalidOperationException($"Missing legacy Art root or folder meta: {LegacyArtRoot}");
            }

            string[] files = Directory.GetFiles(LegacyArtRoot, "*", SearchOption.AllDirectories);
            if (files.Length != 1 ||
                !string.Equals(
                    files[0].Replace('\\', '/'),
                    "Assets/_LizzoPV/Art/Characters.meta",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Legacy Art root contains files beyond the expected Characters folder meta. Found {files.Length}.");
            }
        }

        private static void ValidateMovedArtAssets()
        {
            ValidateArtRoot("Assets/_LizzoPV/App/Branding/Art/AppIcon", 3);
            ValidateArtRoot("Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions", 49);
            ValidateArtRoot("Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Summons", 7);
        }

        private static void ValidateArtRoot(string root, int expectedAssetCount)
        {
            if (!AssetDatabase.IsValidFolder(root))
            {
                throw new InvalidOperationException($"Missing moved art root: {root}");
            }

            string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            int assetCount = 0;
            foreach (string rawPath in files)
            {
                string path = rawPath.Replace('\\', '/');
                if (path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid) || AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    throw new InvalidOperationException($"Moved art asset failed GUID or load validation: {path}");
                }

                assetCount++;
            }

            if (assetCount != expectedAssetCount)
            {
                throw new InvalidOperationException(
                    $"Expected {expectedAssetCount} moved art files under {root}; found {assetCount}.");
            }
        }
    }
}
