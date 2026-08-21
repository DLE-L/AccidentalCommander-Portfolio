using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class LegionSharedCompatibilityAnimationAuthoring
    {
        private const string SelfPath =
            "Assets/_LizzoPV/Editor/LegionSharedCompatibilityAnimationAuthoring.cs";

        private const string SourceRoot =
            "Assets/_LizzoPV/Animations/Characters/Companions";

        private const string DestinationRoot =
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared";

        private const string PrefabRoot =
            "Assets/_LizzoPV/Gameplay/Legion/Prefabs";

        private const string ControllerFileName = "CompanionSpriteShared.controller";

        private static readonly string[] AssetFileNames =
        {
            "CompanionSpriteShared_Attack.anim",
            "CompanionSpriteShared_Death.anim",
            "CompanionSpriteShared_Idle.anim",
            "CompanionSpriteShared_Run.anim",
            ControllerFileName,
        };

        private static readonly HashSet<string> ExpectedClipNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "CompanionSpriteShared_Attack",
            "CompanionSpriteShared_Death",
            "CompanionSpriteShared_Idle",
            "CompanionSpriteShared_Run",
        };

        public static void Apply()
        {
            try
            {
                EnsureDestinationFolder();

                Dictionary<string, string> sourceGuids = CaptureSourceGuids();
                MoveAssets();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                ValidateMovedGuids(sourceGuids);
                ValidateControllerClips();
                ValidatePrefabBindings();

                Debug.Log(
                    "[Legion Shared Compatibility Animations] PASS: moved 5 shared animation assets with GUIDs preserved; " +
                    "controller retains 4 clips; 38 Legion Prefab bindings resolve the moved controller.");

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

        private static void EnsureDestinationFolder()
        {
            if (AssetDatabase.IsValidFolder(DestinationRoot))
            {
                return;
            }

            string parent = "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility";
            string createdGuid = AssetDatabase.CreateFolder(parent, "Shared");
            if (string.IsNullOrEmpty(createdGuid))
            {
                throw new InvalidOperationException($"Failed to create destination folder: {DestinationRoot}");
            }
        }

        private static Dictionary<string, string> CaptureSourceGuids()
        {
            var sourceGuids = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string fileName in AssetFileNames)
            {
                string sourcePath = $"{SourceRoot}/{fileName}";
                string destinationPath = $"{DestinationRoot}/{fileName}";

                if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
                {
                    throw new InvalidOperationException($"Missing source asset: {sourcePath}");
                }

                if (AssetDatabase.LoadMainAssetAtPath(destinationPath) != null)
                {
                    throw new InvalidOperationException($"Destination already contains asset: {destinationPath}");
                }

                string guid = AssetDatabase.AssetPathToGUID(sourcePath);
                if (string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException($"Source asset has no GUID: {sourcePath}");
                }

                sourceGuids.Add(fileName, guid);
            }

            return sourceGuids;
        }

        private static void MoveAssets()
        {
            foreach (string fileName in AssetFileNames)
            {
                string sourcePath = $"{SourceRoot}/{fileName}";
                string destinationPath = $"{DestinationRoot}/{fileName}";
                string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException(
                        $"Failed to move shared animation asset from {sourcePath} to {destinationPath}: {error}");
                }
            }
        }

        private static void ValidateMovedGuids(IReadOnlyDictionary<string, string> sourceGuids)
        {
            foreach (string fileName in AssetFileNames)
            {
                string destinationPath = $"{DestinationRoot}/{fileName}";
                string actualGuid = AssetDatabase.AssetPathToGUID(destinationPath);
                if (!string.Equals(sourceGuids[fileName], actualGuid, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"GUID changed for {fileName}. Expected {sourceGuids[fileName]}, got {actualGuid}.");
                }
            }
        }

        private static void ValidateControllerClips()
        {
            string controllerPath = $"{DestinationRoot}/{ControllerFileName}";
            RuntimeAnimatorController controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Failed to load moved controller: {controllerPath}");
            }

            AnimationClip[] clips = controller.animationClips;
            if (clips.Length != ExpectedClipNames.Count)
            {
                throw new InvalidOperationException(
                    $"Expected {ExpectedClipNames.Count} controller clips, found {clips.Length}.");
            }

            var actualClipNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (AnimationClip clip in clips)
            {
                if (clip == null || !actualClipNames.Add(clip.name))
                {
                    throw new InvalidOperationException("Controller contains a null or duplicate animation clip.");
                }
            }

            if (!actualClipNames.SetEquals(ExpectedClipNames))
            {
                throw new InvalidOperationException("Moved controller clip membership differs from the expected four clips.");
            }
        }

        private static void ValidatePrefabBindings()
        {
            string controllerPath = $"{DestinationRoot}/{ControllerFileName}";
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            int dependencyCount = 0;
            int resolvedBindingCount = 0;

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);
                bool dependsOnController = Array.IndexOf(dependencies, controllerPath) >= 0;
                if (!dependsOnController)
                {
                    continue;
                }

                dependencyCount++;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Failed to load dependent Prefab: {prefabPath}");
                }

                bool resolvesController = false;
                Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);
                foreach (Animator animator in animators)
                {
                    RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
                    if (runtimeController != null &&
                        string.Equals(
                            AssetDatabase.GetAssetPath(runtimeController),
                            controllerPath,
                            StringComparison.Ordinal))
                    {
                        resolvesController = true;
                        break;
                    }
                }

                if (!resolvesController)
                {
                    throw new InvalidOperationException(
                        $"Dependent Prefab does not resolve the moved controller through an Animator: {prefabPath}");
                }

                resolvedBindingCount++;
            }

            if (dependencyCount != 38 || resolvedBindingCount != 38)
            {
                throw new InvalidOperationException(
                    $"Expected 38 Legion Prefab bindings; found {dependencyCount} dependencies and " +
                    $"{resolvedBindingCount} resolved Animator bindings.");
            }
        }
    }
}
