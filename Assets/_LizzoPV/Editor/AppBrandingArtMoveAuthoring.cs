using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class AppBrandingArtMoveAuthoring
    {
        private const string SelfPath =
            "Assets/_LizzoPV/Editor/AppBrandingArtMoveAuthoring.cs";

        private const string SourceFolder = "Assets/_LizzoPV/Art/AppIcon";
        private const string BrandingFolder = "Assets/_LizzoPV/App/Branding";
        private const string BrandingArtFolder = "Assets/_LizzoPV/App/Branding/Art";
        private const string DestinationFolder = "Assets/_LizzoPV/App/Branding/Art/AppIcon";

        private static readonly string[] AssetFileNames =
        {
            "CommanderIcon_AdaptiveBackground.png",
            "CommanderIcon_AdaptiveForeground.png",
            "CommanderIcon_Master_RoundLegacy.png",
        };

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
                        $"Failed to move AppIcon folder from {SourceFolder} to {DestinationFolder}: {error}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                ValidateMovedAssets(expectedGuids);

                Debug.Log(
                    "[App Branding Art Move] PASS: moved the AppIcon folder and three textures with folder, asset, " +
                    "and importer GUIDs preserved.");

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
                throw new InvalidOperationException("AppIcon source/destination folder state differs from the approved move.");
            }

            var expectedGuids = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { string.Empty, AssetDatabase.AssetPathToGUID(SourceFolder) },
            };

            foreach (string fileName in AssetFileNames)
            {
                string sourcePath = $"{SourceFolder}/{fileName}";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                string guid = AssetDatabase.AssetPathToGUID(sourcePath);
                if (texture == null || string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException($"Missing AppIcon texture or GUID: {sourcePath}");
                }

                expectedGuids.Add(fileName, guid);
            }

            if (string.IsNullOrEmpty(expectedGuids[string.Empty]))
            {
                throw new InvalidOperationException($"Missing source folder GUID: {SourceFolder}");
            }

            return expectedGuids;
        }

        private static void EnsureDestinationParents()
        {
            if (!AssetDatabase.IsValidFolder(BrandingFolder))
            {
                string guid = AssetDatabase.CreateFolder("Assets/_LizzoPV/App", "Branding");
                if (string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException($"Failed to create destination parent: {BrandingFolder}");
                }
            }

            if (!AssetDatabase.IsValidFolder(BrandingArtFolder))
            {
                string guid = AssetDatabase.CreateFolder(BrandingFolder, "Art");
                if (string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException($"Failed to create destination parent: {BrandingArtFolder}");
                }
            }
        }

        private static void ValidateMovedAssets(IReadOnlyDictionary<string, string> expectedGuids)
        {
            if (AssetDatabase.IsValidFolder(SourceFolder) || !AssetDatabase.IsValidFolder(DestinationFolder))
            {
                throw new InvalidOperationException("AppIcon folder move did not reach the expected final state.");
            }

            string folderGuid = AssetDatabase.AssetPathToGUID(DestinationFolder);
            if (!string.Equals(expectedGuids[string.Empty], folderGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"AppIcon folder GUID changed. Expected {expectedGuids[string.Empty]}, got {folderGuid}.");
            }

            foreach (string fileName in AssetFileNames)
            {
                string destinationPath = $"{DestinationFolder}/{fileName}";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(destinationPath);
                string guid = AssetDatabase.AssetPathToGUID(destinationPath);
                if (texture == null ||
                    !string.Equals(expectedGuids[fileName], guid, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Moved AppIcon texture failed load or GUID validation: {destinationPath}");
                }
            }
        }
    }
}
