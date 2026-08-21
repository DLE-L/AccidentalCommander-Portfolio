#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class CommanderCompatibilityAnimationAuthoring
    {
        private const string SelfPath = "Assets/_LizzoPV/Editor/CommanderCompatibilityAnimationAuthoring.cs";
        private const string SourceRoot = "Assets/_LizzoPV/Animations/Units";
        private const string DestinationRoot = "Assets/_LizzoPV/Gameplay/Commander/Animations/Compatibility";
        private const string CommanderPrefabPath = "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab";

        public static void Apply()
        {
            string[] assetNames =
            {
                "Commander_Attack.anim",
                "Commander_Death.anim",
                "Commander_Idle.anim",
                "Commander_Run.anim",
                "Commander.controller"
            };

            bool succeeded = false;
            try
            {
                EnsureFolder(DestinationRoot);

                var sourceGuids = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string assetName in assetNames)
                {
                    string sourcePath = SourceRoot + "/" + assetName;
                    string destinationPath = DestinationRoot + "/" + assetName;
                    string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
                    if (string.IsNullOrEmpty(sourceGuid))
                        throw new InvalidOperationException($"Commander compatibility animation is missing: {sourcePath}");

                    sourceGuids.Add(destinationPath, sourceGuid);
                    string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
                    if (string.IsNullOrEmpty(error) == false)
                        throw new InvalidOperationException($"Move failed: {sourcePath} -> {destinationPath}: {error}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                foreach (KeyValuePair<string, string> entry in sourceGuids)
                {
                    string destinationGuid = AssetDatabase.AssetPathToGUID(entry.Key);
                    if (string.Equals(entry.Value, destinationGuid, StringComparison.Ordinal) == false)
                        throw new InvalidOperationException($"GUID changed during move: {entry.Key}");
                }

                ValidateControllerAndClips();
                ValidateCommanderPrefabController();

                succeeded = true;
                Debug.Log("[Commander Compatibility Animations] PASS: 5 assets moved with GUIDs, clips, and Commander Prefab binding preserved.");
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

        private static void ValidateControllerAndClips()
        {
            string controllerPath = DestinationRoot + "/Commander.controller";
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller == null)
                throw new InvalidOperationException($"Commander controller is not importable: {controllerPath}");

            var expectedNames = new HashSet<string>(StringComparer.Ordinal)
            {
                "Commander_Attack",
                "Commander_Death",
                "Commander_Idle",
                "Commander_Run"
            };

            AnimationClip[] clips = controller.animationClips;
            if (clips == null || clips.Length != expectedNames.Count)
                throw new InvalidOperationException($"Commander controller clip count changed: expected={expectedNames.Count}, actual={clips?.Length ?? 0}");

            foreach (AnimationClip clip in clips)
            {
                if (clip == null || expectedNames.Remove(clip.name) == false)
                    throw new InvalidOperationException($"Commander controller has an unexpected clip: {clip?.name ?? "<null>"}");
            }

            if (expectedNames.Count != 0)
                throw new InvalidOperationException("Commander controller is missing one or more expected clips.");
        }

        private static void ValidateCommanderPrefabController()
        {
            RuntimeAnimatorController expectedController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                DestinationRoot + "/Commander.controller");
            GameObject commanderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommanderPrefabPath);
            if (commanderPrefab == null)
                throw new InvalidOperationException($"Commander Prefab is not importable: {CommanderPrefabPath}");

            Animator[] animators = commanderPrefab.GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                if (animator.runtimeAnimatorController == expectedController)
                    return;
            }

            throw new InvalidOperationException("Commander Prefab no longer resolves the compatibility Animator Controller.");
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
    }
}
#endif
