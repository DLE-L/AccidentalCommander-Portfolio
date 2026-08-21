using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class LegionSummonArtMoveAuthoring
    {
        private const string SelfPath =
            "Assets/_LizzoPV/Editor/LegionSummonArtMoveAuthoring.cs";

        private const string SourceFolder =
            "Assets/_LizzoPV/Art/Characters/Summons";

        private const string DestinationParent =
            "Assets/_LizzoPV/Gameplay/Legion/Art/Characters";

        private const string DestinationFolder =
            "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Summons";

        private const string OldPrefix =
            "Assets/_LizzoPV/Art/Characters/Summons";

        private const string NewPrefix =
            "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Summons";

        private const string ManifestFileName = "summons_sprite_sheet_manifest.json";

        public static void Apply()
        {
            try
            {
                Dictionary<string, string> expectedGuids = CaptureSourceGuids();

                string error = AssetDatabase.MoveAsset(SourceFolder, DestinationFolder);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException(
                        $"Failed to move Summon art folder from {SourceFolder} to {DestinationFolder}: {error}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                ValidateMovedAssets(expectedGuids);
                ValidateManifestPaths();

                Debug.Log(
                    "[Legion Summon Art Move] PASS: moved 7 Summon art files with folder and asset GUIDs " +
                    "preserved; manifest contains 6 destination paths and no legacy paths.");

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

        private static Dictionary<string, string> CaptureSourceGuids()
        {
            if (!AssetDatabase.IsValidFolder(SourceFolder) ||
                !AssetDatabase.IsValidFolder(DestinationParent) ||
                AssetDatabase.IsValidFolder(DestinationFolder))
            {
                throw new InvalidOperationException(
                    "Summon art source, destination parent, or destination state differs from the approved move.");
            }

            var expectedGuids = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { string.Empty, AssetDatabase.AssetPathToGUID(SourceFolder) },
            };

            string[] files = Directory.GetFiles(SourceFolder, "*", SearchOption.AllDirectories);
            int assetCount = 0;
            foreach (string rawPath in files)
            {
                string path = rawPath.Replace('\\', '/');
                if (path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    continue;
                }

                string relativePath = path.Substring(SourceFolder.Length + 1);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (AssetDatabase.LoadMainAssetAtPath(path) == null || string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException($"Missing Summon art asset or GUID: {path}");
                }

                expectedGuids.Add(relativePath, guid);
                assetCount++;
            }

            if (assetCount != 7 || string.IsNullOrEmpty(expectedGuids[string.Empty]))
            {
                throw new InvalidOperationException(
                    $"Expected 7 Summon art files and a folder GUID; found {assetCount} files.");
            }

            return expectedGuids;
        }

        private static void ValidateMovedAssets(IReadOnlyDictionary<string, string> expectedGuids)
        {
            if (AssetDatabase.IsValidFolder(SourceFolder) || !AssetDatabase.IsValidFolder(DestinationFolder))
            {
                throw new InvalidOperationException("Summon art folder move did not reach the expected final state.");
            }

            string folderGuid = AssetDatabase.AssetPathToGUID(DestinationFolder);
            if (!string.Equals(expectedGuids[string.Empty], folderGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Summon art folder GUID changed. Expected {expectedGuids[string.Empty]}, got {folderGuid}.");
            }

            int assetCount = 0;
            foreach (KeyValuePair<string, string> entry in expectedGuids)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                string destinationPath = $"{DestinationFolder}/{entry.Key}";
                string guid = AssetDatabase.AssetPathToGUID(destinationPath);
                if (AssetDatabase.LoadMainAssetAtPath(destinationPath) == null ||
                    !string.Equals(entry.Value, guid, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Moved Summon art asset failed load or GUID validation: {destinationPath}");
                }

                assetCount++;
            }

            if (assetCount != 7)
            {
                throw new InvalidOperationException($"Expected 7 moved Summon art files; found {assetCount}.");
            }
        }

        private static void ValidateManifestPaths()
        {
            string manifestPath = $"{DestinationFolder}/{ManifestFileName}";
            string text = File.ReadAllText(manifestPath);
            int oldCount = CountOccurrences(text, OldPrefix);
            int newCount = CountOccurrences(text, NewPrefix);
            if (oldCount != 0 || newCount != 6)
            {
                throw new InvalidOperationException(
                    $"Summon manifest path counts differ from the approved replacement. " +
                    $"Old: {oldCount}, new: {newCount}.");
            }
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
