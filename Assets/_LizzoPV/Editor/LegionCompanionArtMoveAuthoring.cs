using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class LegionCompanionArtMoveAuthoring
    {
        private const string SelfPath =
            "Assets/_LizzoPV/Editor/LegionCompanionArtMoveAuthoring.cs";

        private const string SourceFolder =
            "Assets/_LizzoPV/Art/Characters/Companions";

        private const string LegionArtFolder =
            "Assets/_LizzoPV/Gameplay/Legion/Art";

        private const string LegionCharacterArtFolder =
            "Assets/_LizzoPV/Gameplay/Legion/Art/Characters";

        private const string DestinationFolder =
            "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions";

        private const string OldPrefix =
            "Assets/_LizzoPV/Art/Characters/Companions";

        private const string NewPrefix =
            "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions";

        private const string ManifestFileName = "companions_sprite_sheet_manifest.json";

        public static void Apply()
        {
            try
            {
                Dictionary<string, string> expectedGuids = CaptureSourceGuids();
                EnsureDestinationParents();

                string error = AssetDatabase.MoveAsset(SourceFolder, DestinationFolder);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException(
                        $"Failed to move Companion art folder from {SourceFolder} to {DestinationFolder}: {error}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                ValidateMovedAssets(expectedGuids);
                ValidateManifestPaths();

                Debug.Log(
                    "[Legion Companion Art Move] PASS: moved 49 Companion art files with folder and asset GUIDs " +
                    "preserved; manifest contains 48 destination paths and no legacy paths.");

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
            if (!AssetDatabase.IsValidFolder(SourceFolder) || AssetDatabase.IsValidFolder(DestinationFolder))
            {
                throw new InvalidOperationException(
                    "Companion art source/destination folder state differs from the approved move.");
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
                    throw new InvalidOperationException($"Missing Companion art asset or GUID: {path}");
                }

                expectedGuids.Add(relativePath, guid);
                assetCount++;
            }

            if (assetCount != 49 || string.IsNullOrEmpty(expectedGuids[string.Empty]))
            {
                throw new InvalidOperationException(
                    $"Expected 49 Companion art files and a folder GUID; found {assetCount} files.");
            }

            return expectedGuids;
        }

        private static void EnsureDestinationParents()
        {
            if (!AssetDatabase.IsValidFolder(LegionArtFolder))
            {
                string guid = AssetDatabase.CreateFolder("Assets/_LizzoPV/Gameplay/Legion", "Art");
                if (string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException($"Failed to create destination parent: {LegionArtFolder}");
                }
            }

            if (!AssetDatabase.IsValidFolder(LegionCharacterArtFolder))
            {
                string guid = AssetDatabase.CreateFolder(LegionArtFolder, "Characters");
                if (string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException(
                        $"Failed to create destination parent: {LegionCharacterArtFolder}");
                }
            }
        }

        private static void ValidateMovedAssets(IReadOnlyDictionary<string, string> expectedGuids)
        {
            if (AssetDatabase.IsValidFolder(SourceFolder) || !AssetDatabase.IsValidFolder(DestinationFolder))
            {
                throw new InvalidOperationException("Companion art folder move did not reach the expected final state.");
            }

            string folderGuid = AssetDatabase.AssetPathToGUID(DestinationFolder);
            if (!string.Equals(expectedGuids[string.Empty], folderGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Companion art folder GUID changed. Expected {expectedGuids[string.Empty]}, got {folderGuid}.");
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
                        $"Moved Companion art asset failed load or GUID validation: {destinationPath}");
                }

                assetCount++;
            }

            if (assetCount != 49)
            {
                throw new InvalidOperationException($"Expected 49 moved Companion art files; found {assetCount}.");
            }
        }

        private static void ValidateManifestPaths()
        {
            string manifestPath = $"{DestinationFolder}/{ManifestFileName}";
            string text = File.ReadAllText(manifestPath);
            int oldCount = CountOccurrences(text, OldPrefix);
            int newCount = CountOccurrences(text, NewPrefix);
            if (oldCount != 0 || newCount != 48)
            {
                throw new InvalidOperationException(
                    $"Companion manifest path counts differ from the approved replacement. " +
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
