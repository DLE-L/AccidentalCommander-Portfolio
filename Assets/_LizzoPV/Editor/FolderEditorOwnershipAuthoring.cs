#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class FolderEditorOwnershipAuthoring
    {
        private const string SelfPath = "Assets/_LizzoPV/Editor/FolderEditorOwnershipAuthoring.cs";
        private const string OldScriptsRoot = "Assets/_LizzoPV/Scripts";

        public static void Apply()
        {
            var moves = new[]
            {
                new AssetMove(
                    "Assets/_LizzoPV/Scripts/Editor/EditorBuildRunCoordinator.cs",
                    "Assets/_LizzoPV/Editor/Build/EditorBuildRunCoordinator.cs"),
                new AssetMove(
                    "Assets/_LizzoPV/Scripts/Editor/InternalAndroidBuildUtility.cs",
                    "Assets/_LizzoPV/Editor/Build/InternalAndroidBuildUtility.cs"),
                new AssetMove(
                    "Assets/_LizzoPV/Scripts/Editor/P0PresentationSetEditor.cs",
                    "Assets/_LizzoPV/Editor/Presentation/Compatibility/P0PresentationSetEditor.cs"),
                new AssetMove(
                    "Assets/_LizzoPV/Scripts/Editor/UI/Catalog",
                    "Assets/_LizzoPV/Editor/UI/Catalog")
            };

            bool succeeded = false;
            try
            {
                EnsureFolder("Assets/_LizzoPV/Editor/Build");
                EnsureFolder("Assets/_LizzoPV/Editor/Presentation/Compatibility");
                EnsureFolder("Assets/_LizzoPV/Editor/UI");

                var sourceGuids = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (AssetMove move in moves)
                {
                    string guid = AssetDatabase.AssetPathToGUID(move.SourcePath);
                    if (string.IsNullOrEmpty(guid))
                        throw new InvalidOperationException($"Source asset is missing: {move.SourcePath}");

                    sourceGuids.Add(move.DestinationPath, guid);
                    string error = AssetDatabase.MoveAsset(move.SourcePath, move.DestinationPath);
                    if (string.IsNullOrEmpty(error) == false)
                        throw new InvalidOperationException($"Move failed: {move.SourcePath} -> {move.DestinationPath}: {error}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                foreach (AssetMove move in moves)
                {
                    string destinationGuid = AssetDatabase.AssetPathToGUID(move.DestinationPath);
                    if (string.Equals(sourceGuids[move.DestinationPath], destinationGuid, StringComparison.Ordinal) == false)
                        throw new InvalidOperationException($"GUID changed during move: {move.DestinationPath}");
                }

                AssertScriptExists("Assets/_LizzoPV/Editor/Build/EditorBuildRunCoordinator.cs");
                AssertScriptExists("Assets/_LizzoPV/Editor/Build/InternalAndroidBuildUtility.cs");
                AssertScriptExists("Assets/_LizzoPV/Editor/Presentation/Compatibility/P0PresentationSetEditor.cs");
                AssertScriptExists("Assets/_LizzoPV/Editor/UI/Catalog/GUIProBlueCatalogGenerator.cs");
                AssertOnlyFolderMetadataRemains(OldScriptsRoot);

                if (AssetDatabase.DeleteAsset(OldScriptsRoot) == false)
                    throw new InvalidOperationException($"Could not delete empty legacy root: {OldScriptsRoot}");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                if (AssetDatabase.IsValidFolder(OldScriptsRoot))
                    throw new InvalidOperationException($"Legacy root still exists: {OldScriptsRoot}");

                succeeded = true;
                Debug.Log("[Folder Editor Ownership] PASS: editor scripts moved with GUIDs preserved; empty legacy Scripts tree removed.");
            }
            finally
            {
                if (succeeded)
                {
                    AssetDatabase.DeleteAsset(SelfPath);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            int separatorIndex = assetPath.LastIndexOf('/');
            if (separatorIndex <= 0 || separatorIndex >= assetPath.Length - 1)
                throw new InvalidOperationException($"Invalid asset folder path: {assetPath}");

            string parentPath = assetPath.Substring(0, separatorIndex);
            string folderName = assetPath.Substring(separatorIndex + 1);
            EnsureFolder(parentPath);
            string guid = AssetDatabase.CreateFolder(parentPath, folderName);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Could not create asset folder: {assetPath}");
        }

        private static void AssertScriptExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath) == null)
                throw new InvalidOperationException($"Moved script is not importable: {assetPath}");
        }

        private static void AssertOnlyFolderMetadataRemains(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            foreach (string filePath in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetExtension(filePath), ".meta", StringComparison.OrdinalIgnoreCase) == false)
                    throw new InvalidOperationException($"Legacy root still contains an asset: {filePath}");
            }
        }

        private readonly struct AssetMove
        {
            public AssetMove(string sourcePath, string destinationPath)
            {
                SourcePath = sourcePath;
                DestinationPath = destinationPath;
            }

            public string SourcePath { get; }
            public string DestinationPath { get; }
        }
    }
}
#endif
