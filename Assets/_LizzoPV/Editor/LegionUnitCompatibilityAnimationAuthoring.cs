#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class LegionUnitCompatibilityAnimationAuthoring
    {
        private const string SelfPath = "Assets/_LizzoPV/Editor/LegionUnitCompatibilityAnimationAuthoring.cs";
        private const string SourceRoot = "Assets/_LizzoPV/Animations/Units";
        private const string DestinationRoot = "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Units";
        private const string UnitPrefabRoot = "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Units";

        private static readonly string[] UnitNames =
        {
            "Archer",
            "Cleric",
            "ShieldCaptain",
            "ShieldSoldier",
            "Swordsman"
        };

        private static readonly string[] AssetSuffixes =
        {
            "_Attack.anim",
            "_Death.anim",
            "_Idle.anim",
            "_Run.anim",
            ".controller"
        };

        public static void Apply()
        {
            bool succeeded = false;
            try
            {
                EnsureFolder(DestinationRoot);

                var sourceGuids = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string unitName in UnitNames)
                {
                    foreach (string suffix in AssetSuffixes)
                    {
                        string assetName = unitName + suffix;
                        string sourcePath = SourceRoot + "/" + assetName;
                        string destinationPath = DestinationRoot + "/" + assetName;
                        string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
                        if (string.IsNullOrEmpty(sourceGuid))
                            throw new InvalidOperationException($"Legion Unit compatibility animation is missing: {sourcePath}");

                        sourceGuids.Add(destinationPath, sourceGuid);
                        string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
                        if (string.IsNullOrEmpty(error) == false)
                            throw new InvalidOperationException($"Move failed: {sourcePath} -> {destinationPath}: {error}");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                foreach (KeyValuePair<string, string> entry in sourceGuids)
                {
                    string destinationGuid = AssetDatabase.AssetPathToGUID(entry.Key);
                    if (string.Equals(entry.Value, destinationGuid, StringComparison.Ordinal) == false)
                        throw new InvalidOperationException($"GUID changed during move: {entry.Key}");
                }

                foreach (string unitName in UnitNames)
                {
                    ValidateControllerAndClips(unitName);
                    ValidateUnitPrefabController(unitName);
                }

                succeeded = true;
                Debug.Log("[Legion Unit Compatibility Animations] PASS: 25 assets moved with GUIDs, clips, and 5 Unit Prefab bindings preserved.");
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

        private static void ValidateControllerAndClips(string unitName)
        {
            string controllerPath = DestinationRoot + "/" + unitName + ".controller";
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller == null)
                throw new InvalidOperationException($"Legion Unit controller is not importable: {controllerPath}");

            var expectedNames = new HashSet<string>(StringComparer.Ordinal)
            {
                unitName + "_Attack",
                unitName + "_Death",
                unitName + "_Idle",
                unitName + "_Run"
            };

            AnimationClip[] clips = controller.animationClips;
            if (clips == null || clips.Length != expectedNames.Count)
                throw new InvalidOperationException($"{unitName} controller clip count changed: expected={expectedNames.Count}, actual={clips?.Length ?? 0}");

            foreach (AnimationClip clip in clips)
            {
                if (clip == null || expectedNames.Remove(clip.name) == false)
                    throw new InvalidOperationException($"{unitName} controller has an unexpected clip: {clip?.name ?? "<null>"}");
            }

            if (expectedNames.Count != 0)
                throw new InvalidOperationException($"{unitName} controller is missing one or more expected clips.");
        }

        private static void ValidateUnitPrefabController(string unitName)
        {
            RuntimeAnimatorController expectedController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                DestinationRoot + "/" + unitName + ".controller");
            string prefabPath = UnitPrefabRoot + "/" + unitName + ".prefab";
            GameObject unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (unitPrefab == null)
                throw new InvalidOperationException($"Legion Unit Prefab is not importable: {prefabPath}");

            Animator[] animators = unitPrefab.GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                if (animator.runtimeAnimatorController == expectedController)
                    return;
            }

            throw new InvalidOperationException($"{unitName} Prefab no longer resolves its compatibility Animator Controller.");
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
