using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class LegacyAnimationFolderCleanupAuthoring
    {
        private const string SelfPath =
            "Assets/_LizzoPV/Editor/LegacyAnimationFolderCleanupAuthoring.cs";

        private const string LegacyRoot = "Assets/_LizzoPV/Animations";

        private static readonly HashSet<string> ExpectedInnerMetaPaths =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Assets/_LizzoPV/Animations/Characters.meta",
                "Assets/_LizzoPV/Animations/Characters/Companions.meta",
                "Assets/_LizzoPV/Animations/Units.meta",
            };

        public static void Apply()
        {
            try
            {
                ValidateLegacyTreeIsEmpty();
                ValidatePreservedAnimationAssets();

                if (!AssetDatabase.DeleteAsset(LegacyRoot))
                {
                    throw new InvalidOperationException($"Failed to delete legacy animation folder: {LegacyRoot}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                if (AssetDatabase.IsValidFolder(LegacyRoot) ||
                    Directory.Exists(LegacyRoot) ||
                    File.Exists(LegacyRoot + ".meta"))
                {
                    throw new InvalidOperationException($"Legacy animation folder still exists: {LegacyRoot}");
                }

                ValidatePreservedAnimationAssets();

                Debug.Log(
                    "[Legacy Animation Folder Cleanup] PASS: removed the empty legacy Animations tree and four folder metas; " +
                    "60 compatibility animation assets remain loadable in their feature-owned roots.");

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
            if (!AssetDatabase.IsValidFolder(LegacyRoot) || !Directory.Exists(LegacyRoot))
            {
                throw new InvalidOperationException($"Missing legacy animation folder: {LegacyRoot}");
            }

            if (!File.Exists(LegacyRoot + ".meta"))
            {
                throw new InvalidOperationException($"Missing legacy animation root meta: {LegacyRoot}.meta");
            }

            var actualInnerMetaPaths = new HashSet<string>(StringComparer.Ordinal);
            string[] files = Directory.GetFiles(LegacyRoot, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string path = file.Replace('\\', '/');
                if (!path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Legacy animation tree still contains an asset: {path}");
                }

                actualInnerMetaPaths.Add(path);
            }

            if (!actualInnerMetaPaths.SetEquals(ExpectedInnerMetaPaths))
            {
                throw new InvalidOperationException(
                    $"Legacy animation tree folder-meta set differs from the expected three inner metas. " +
                    $"Found {actualInnerMetaPaths.Count}.");
            }
        }

        private static void ValidatePreservedAnimationAssets()
        {
            ValidateAnimationRoot(
                "Assets/_LizzoPV/Gameplay/Commander/Animations",
                expectedClipCount: 4,
                expectedControllerCount: 1);
            ValidateAnimationRoot(
                "Assets/_LizzoPV/Gameplay/Enemies/Animations",
                expectedClipCount: 20,
                expectedControllerCount: 5);
            ValidateAnimationRoot(
                "Assets/_LizzoPV/Gameplay/Legion/Animations",
                expectedClipCount: 24,
                expectedControllerCount: 6);
        }

        private static void ValidateAnimationRoot(
            string root,
            int expectedClipCount,
            int expectedControllerCount)
        {
            if (!AssetDatabase.IsValidFolder(root))
            {
                throw new InvalidOperationException($"Missing preserved animation root: {root}");
            }

            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { root });
            string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { root });
            if (clipGuids.Length != expectedClipCount || controllerGuids.Length != expectedControllerCount)
            {
                throw new InvalidOperationException(
                    $"Unexpected animation inventory under {root}. " +
                    $"Expected {expectedClipCount} clips and {expectedControllerCount} controllers; " +
                    $"found {clipGuids.Length} clips and {controllerGuids.Length} controllers.");
            }

            ValidateAssets(root, clipGuids);
            ValidateAssets(root, controllerGuids);
        }

        private static void ValidateAssets(string root, IReadOnlyList<string> guids)
        {
            for (int index = 0; index < guids.Count; index += 1)
            {
                string guid = guids[index];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(guid) ||
                    !path.StartsWith(root + "/", StringComparison.Ordinal) ||
                    AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    throw new InvalidOperationException(
                        $"Preserved animation asset failed GUID/path/load validation under {root}: {path}");
                }
            }
        }
    }
}
