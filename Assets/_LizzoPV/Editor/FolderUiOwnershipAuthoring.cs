using System;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class FolderUiOwnershipAuthoring
    {
        private const string OldTypographyRoot = "Assets/_LizzoPV/Fonts";
        private const string NewTypographyRoot = "Assets/_LizzoPV/Shared/UI/Typography";
        private const string OldTypographyToolRoot = "Assets/_LizzoPV/Scripts/Editor/UI/Typography";
        private const string NewTypographyToolRoot = "Assets/_LizzoPV/Editor/UI/Typography";
        private const string SafeAreaLayoutPath = "Assets/_LizzoPV/Shared/UI/Runtime/SafeAreaLayout.cs";
        private const string SelfPath = "Assets/_LizzoPV/Editor/FolderUiOwnershipAuthoring.cs";

        public static void Apply()
        {
            EnsureFolder("Assets/_LizzoPV/Editor/UI");

            string typographyGuid = RequireFolderGuid(OldTypographyRoot);
            string typographyToolGuid = RequireFolderGuid(OldTypographyToolRoot);

            MoveFolder(OldTypographyRoot, NewTypographyRoot);
            MoveFolder(OldTypographyToolRoot, NewTypographyToolRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            RequireSameGuid(NewTypographyRoot, typographyGuid);
            RequireSameGuid(NewTypographyToolRoot, typographyToolGuid);
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(SafeAreaLayoutPath) == null)
                throw new InvalidOperationException($"[Folder UI Ownership] Preserved SafeAreaLayout is missing at '{SafeAreaLayoutPath}'.");

            Debug.Log("[Folder UI Ownership] PASS: shared typography and its Editor tool moved; SafeAreaLayout preserved.");
            if (!AssetDatabase.DeleteAsset(SelfPath))
                throw new InvalidOperationException($"[Folder UI Ownership] Temporary authoring script could not delete itself at '{SelfPath}'.");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            int separator = folderPath.LastIndexOf('/');
            if (separator <= 0)
                throw new InvalidOperationException($"[Folder UI Ownership] Invalid folder path '{folderPath}'.");

            string parent = folderPath.Substring(0, separator);
            string name = folderPath.Substring(separator + 1);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name)))
                throw new InvalidOperationException($"[Folder UI Ownership] Failed to create '{folderPath}'.");
        }

        private static string RequireFolderGuid(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                throw new InvalidOperationException($"[Folder UI Ownership] Required source folder is missing: '{folderPath}'.");

            string guid = AssetDatabase.AssetPathToGUID(folderPath);
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException($"[Folder UI Ownership] Source folder GUID is missing: '{folderPath}'.");

            return guid;
        }

        private static void MoveFolder(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.IsValidFolder(destinationPath))
                throw new InvalidOperationException($"[Folder UI Ownership] Destination already exists: '{destinationPath}'.");

            string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"[Folder UI Ownership] Move failed '{sourcePath}' -> '{destinationPath}': {error}");
        }

        private static void RequireSameGuid(string folderPath, string expectedGuid)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                throw new InvalidOperationException($"[Folder UI Ownership] Destination folder is missing: '{folderPath}'.");

            string actualGuid = AssetDatabase.AssetPathToGUID(folderPath);
            if (!string.Equals(actualGuid, expectedGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[Folder UI Ownership] Folder GUID changed at '{folderPath}'. expected={expectedGuid} actual={actualGuid}");
            }
        }
    }
}
