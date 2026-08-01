using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTools.UI.Theming.Draft
{
    public sealed class DraftUIThemeValidationResult
    {
        public readonly List<string> Messages = new List<string>();
        public readonly List<DraftUIThemeIndexEntry> ValidEntries = new List<DraftUIThemeIndexEntry>();
        public bool HasErrors { get; private set; }

        public void AddError(string message)
        {
            HasErrors = true;
            Messages.Add("ERROR: " + message);
        }

        public void AddInfo(string message)
        {
            Messages.Add("INFO: " + message);
        }

        public string ToLog(string operation)
        {
            string status = HasErrors ? "BLOCKED" : "PASS";
            string output = "[Draft UI Theme] " + operation + " " + status;
            for (int i = 0; i < Messages.Count; i++)
            {
                output += "\n- " + Messages[i];
            }

            return output;
        }
    }

    internal sealed class DraftUIThemeSpriteBinding
    {
        public readonly string BinderStyleField;
        public readonly string ImageField;
        public readonly string StyleSpriteProperty;
        public readonly bool RequiredSprite;

        public DraftUIThemeSpriteBinding(
            string binderStyleField,
            string imageField,
            string styleSpriteProperty,
            bool requiredSprite)
        {
            BinderStyleField = binderStyleField;
            ImageField = imageField;
            StyleSpriteProperty = styleSpriteProperty;
            RequiredSprite = requiredSprite;
        }
    }

    internal sealed class DraftUIThemeTargetInspection
    {
        public string AssetPath;
        public GameObject Root;
        public Scene Scene;
        public bool SceneOpenedByTool;
        public Component Binder;
    }

    internal static class DraftUIThemeDefinitions
    {
        public const string IndexPath = "Assets/_LizzoPV/Data/UI/DraftTheme/DraftUIThemeIndex.asset";
        public const string MenuRoot = "Lizzo/UI/Draft Theme";
        public const string SkillSelectBinder = "Lizzo.PV.UI.Theming.Draft.SkillSelectDraftThemeBinder";
        public const string GameplayHudBinder = "Lizzo.PV.UI.Theming.Draft.GameplayHudDraftThemeBinder";
        public const string LobbyBinder = "Lizzo.PV.UI.Theming.Draft.LobbyDraftThemeBinder";

        public static bool IsKnownBinder(string binderTypeName)
        {
            return binderTypeName == SkillSelectBinder
                || binderTypeName == GameplayHudBinder
                || binderTypeName == LobbyBinder;
        }

        public static DraftUIThemeSpriteBinding[] GetBindings(string binderTypeName)
        {
            if (binderTypeName == SkillSelectBinder)
            {
                return new[]
                {
                    new DraftUIThemeSpriteBinding("_styleSet", "_headerImage", "_header._sprite", true),
                    new DraftUIThemeSpriteBinding("_styleSet", "_refreshActionImage", "_refreshAction._sprite", true)
                };
            }

            if (binderTypeName == GameplayHudBinder)
            {
                return new[]
                {
                    new DraftUIThemeSpriteBinding("_gameplayHudStyleSet", "_pauseButtonImage", "_pauseButton._sprite", true),
                    new DraftUIThemeSpriteBinding("_gameplayHudStyleSet", "_pauseImage", "_pauseImage._sprite", false),
                    new DraftUIThemeSpriteBinding("_gameplayHudStyleSet", "_speedToggleButtonImage", "_speedToggleButton._sprite", true),
                    new DraftUIThemeSpriteBinding("_gameplayHudStyleSet", "_speedIconImage", "_speedIcon._sprite", true),
                    new DraftUIThemeSpriteBinding("_gameplayHudStyleSet", "_topMenuImage", "_topMenu._sprite", true),
                    new DraftUIThemeSpriteBinding("_pauseOverlayStyleSet", "_pauseOverlayPanelImage", "_panel._sprite", true),
                    new DraftUIThemeSpriteBinding("_pauseOverlayStyleSet", "_continueButtonImage", "_continueButton._sprite", true)
                };
            }

            if (binderTypeName == LobbyBinder)
            {
                return new[]
                {
                    new DraftUIThemeSpriteBinding("_styleSet", "_persistentHeaderImage", "_persistentHeader._sprite", true),
                    new DraftUIThemeSpriteBinding("_styleSet", "_bottomNavigationImage", "_bottomNavigation._sprite", true),
                    new DraftUIThemeSpriteBinding("_styleSet", "_startBattleButtonImage", "_startBattleButton._sprite", true),
                    new DraftUIThemeSpriteBinding("_styleSet", "_backButtonImage", "_backButton._sprite", true)
                };
            }

            return Array.Empty<DraftUIThemeSpriteBinding>();
        }
    }

    internal static class DraftUIThemeValidator
    {
        public static DraftUIThemeValidationResult Validate()
        {
            DraftUIThemeValidationResult result = new DraftUIThemeValidationResult();
            DraftUIThemeIndex index = AssetDatabase.LoadAssetAtPath<DraftUIThemeIndex>(DraftUIThemeDefinitions.IndexPath);
            if (index == null)
            {
                result.AddError("Missing index asset: " + DraftUIThemeDefinitions.IndexPath);
                return result;
            }

            IReadOnlyList<DraftUIThemeIndexEntry> entries = index.Entries;
            if (entries == null || entries.Count == 0)
            {
                result.AddError("Missing index entries: " + DraftUIThemeDefinitions.IndexPath);
                return result;
            }

            HashSet<string> targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> surfaceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < entries.Count; i++)
            {
                DraftUIThemeIndexEntry entry = entries[i];
                if (entry == null)
                {
                    result.AddError("Missing index entry at index " + i + ".");
                    continue;
                }

                bool entryValid = true;
                if (string.IsNullOrWhiteSpace(entry.SurfaceId))
                {
                    result.AddError("Missing surface ID at index " + i + ".");
                    entryValid = false;
                }
                else if (surfaceIds.Add(entry.SurfaceId) == false)
                {
                    result.AddError("Duplicate surface ID: " + entry.SurfaceId);
                    entryValid = false;
                }

                if (entry.AuthoringTarget == null)
                {
                    result.AddError("Missing authoring target for " + entry.SurfaceId + ".");
                    entryValid = false;
                    continue;
                }

                string targetPath = AssetDatabase.GetAssetPath(entry.AuthoringTarget);
                if (string.IsNullOrEmpty(targetPath))
                {
                    result.AddError("Missing target asset path for " + entry.SurfaceId + ".");
                    entryValid = false;
                    continue;
                }

                if (targetPaths.Add(targetPath) == false)
                {
                    result.AddError("Duplicate target asset: " + targetPath);
                    entryValid = false;
                }

                if (entry.TargetKind == DraftUIThemeTargetKind.Prefab)
                {
                    if (Path.GetExtension(targetPath).Equals(".prefab", StringComparison.OrdinalIgnoreCase) == false
                        || AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) == null)
                    {
                        result.AddError("Wrong target kind for " + entry.SurfaceId + ": expected prefab asset, got " + targetPath);
                        entryValid = false;
                    }
                }
                else if (entry.TargetKind == DraftUIThemeTargetKind.Scene)
                {
                    if (Path.GetExtension(targetPath).Equals(".unity", StringComparison.OrdinalIgnoreCase) == false
                        || AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) == null)
                    {
                        result.AddError("Wrong target kind for " + entry.SurfaceId + ": expected scene asset, got " + targetPath);
                        entryValid = false;
                    }
                }

                UnityEngine.Object[] styleSetAssets = entry.StyleSetAssets;
                if (styleSetAssets == null || styleSetAssets.Length == 0)
                {
                    result.AddError("Missing StyleSet assets for " + entry.SurfaceId + ".");
                    entryValid = false;
                }
                else
                {
                    HashSet<string> stylePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int styleIndex = 0; styleIndex < styleSetAssets.Length; styleIndex++)
                    {
                        UnityEngine.Object styleSetAsset = styleSetAssets[styleIndex];
                        if (styleSetAsset == null)
                        {
                            result.AddError("Missing StyleSet asset at " + entry.SurfaceId + "[" + styleIndex + "].");
                            entryValid = false;
                            continue;
                        }

                        string stylePath = AssetDatabase.GetAssetPath(styleSetAsset);
                        if (string.IsNullOrEmpty(stylePath))
                        {
                            result.AddError("Missing StyleSet asset path at " + entry.SurfaceId + "[" + styleIndex + "].");
                            entryValid = false;
                            continue;
                        }

                        if (stylePaths.Add(stylePath) == false)
                        {
                            result.AddError("Duplicate StyleSet asset in " + entry.SurfaceId + ": " + stylePath);
                            entryValid = false;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(entry.ExpectedBinderTypeName)
                    || DraftUIThemeDefinitions.IsKnownBinder(entry.ExpectedBinderTypeName) == false)
                {
                    result.AddError("Missing or unknown expected binder for " + entry.SurfaceId + ": " + entry.ExpectedBinderTypeName);
                    entryValid = false;
                }

                if (entryValid == false)
                {
                    continue;
                }

                DraftUIThemeTargetInspection inspection = OpenTargetForInspection(entry, targetPath, result);
                if (inspection == null)
                {
                    continue;
                }

                try
                {
                    Component[] binders = FindComponentsByType(inspection.Root, entry.ExpectedBinderTypeName);
                    if (binders.Length == 0)
                    {
                        result.AddError("Missing expected binder " + entry.ExpectedBinderTypeName + " on " + targetPath);
                        continue;
                    }

                    if (binders.Length != 1)
                    {
                        result.AddError("Multiple expected binders " + entry.ExpectedBinderTypeName + " on " + targetPath + ": " + binders.Length);
                        continue;
                    }

                    inspection.Binder = binders[0];
                    ValidateBinder(entry, inspection.Binder, targetPath, result);
                    if (result.HasErrors == false || HasNoEntrySpecificErrors(result, entry.SurfaceId))
                    {
                        result.ValidEntries.Add(entry);
                    }
                    result.AddInfo(entry.SurfaceId + " registered: target=" + targetPath + ", binder=" + entry.ExpectedBinderTypeName);
                }
                finally
                {
                    CloseTargetAfterInspection(inspection);
                }
            }

            result.AddInfo("Loading excluded: no index entry.");
            return result;
        }

        static bool HasNoEntrySpecificErrors(DraftUIThemeValidationResult result, string surfaceId)
        {
            for (int i = result.Messages.Count - 1; i >= 0; i--)
            {
                string message = result.Messages[i];
                if (message.IndexOf("ERROR:", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                if (message.IndexOf(surfaceId, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }

                if (message.StartsWith("ERROR: Missing expected binder", StringComparison.Ordinal)
                    || message.StartsWith("ERROR: Multiple expected binders", StringComparison.Ordinal)
                    || message.StartsWith("ERROR: Missing serialized Image binding", StringComparison.Ordinal)
                    || message.StartsWith("ERROR: Missing required Sprite", StringComparison.Ordinal)
                    || message.StartsWith("ERROR: Semantic ambiguity", StringComparison.Ordinal))
                {
                    return false;
                }

                break;
            }

            return true;
        }

        static void ValidateBinder(
            DraftUIThemeIndexEntry entry,
            Component binder,
            string targetPath,
            DraftUIThemeValidationResult result)
        {
            SerializedObject binderObject = new SerializedObject(binder);
            DraftUIThemeSpriteBinding[] bindings = DraftUIThemeDefinitions.GetBindings(entry.ExpectedBinderTypeName);
            for (int i = 0; i < bindings.Length; i++)
            {
                DraftUIThemeSpriteBinding binding = bindings[i];
                SerializedProperty imageProperty = binderObject.FindProperty(binding.ImageField);
                if (imageProperty == null || imageProperty.objectReferenceValue == null)
                {
                    result.AddError("Missing serialized Image binding " + binding.ImageField + " on " + targetPath);
                    continue;
                }

                SerializedProperty styleProperty = binderObject.FindProperty(binding.BinderStyleField);
                if (styleProperty == null || styleProperty.objectReferenceValue == null)
                {
                    result.AddError("Missing serialized StyleSet binding " + binding.BinderStyleField + " on " + targetPath);
                    continue;
                }

                UnityEngine.Object styleSet = styleProperty.objectReferenceValue;
                if (ContainsObject(entry.StyleSetAssets, styleSet) == false)
                {
                    result.AddError("Semantic ambiguity: binder StyleSet " + AssetDatabase.GetAssetPath(styleSet) + " is not registered on " + entry.SurfaceId);
                    continue;
                }

                SerializedObject styleObject = new SerializedObject(styleSet);
                SerializedProperty spriteProperty = styleObject.FindProperty(binding.StyleSpriteProperty);
                if (spriteProperty == null)
                {
                    result.AddError("Semantic ambiguity: missing StyleSet Sprite slot " + binding.StyleSpriteProperty + " on " + AssetDatabase.GetAssetPath(styleSet));
                    continue;
                }

                if (binding.RequiredSprite && spriteProperty.objectReferenceValue == null)
                {
                    result.AddError("Missing required Sprite " + binding.StyleSpriteProperty + " on " + AssetDatabase.GetAssetPath(styleSet));
                }
                else if (binding.RequiredSprite == false && spriteProperty.objectReferenceValue == null)
                {
                    result.AddInfo("HUD PauseImage-null contract preserved: " + AssetDatabase.GetAssetPath(styleSet) + "." + binding.StyleSpriteProperty);
                }
            }
        }

        static bool ContainsObject(UnityEngine.Object[] objects, UnityEngine.Object target)
        {
            if (objects == null)
            {
                return false;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        internal static Component[] FindComponentsByType(GameObject root, string fullTypeName)
        {
            List<Component> matches = new List<Component>();
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null && component.GetType().FullName == fullTypeName)
                {
                    matches.Add(component);
                }
            }

            return matches.ToArray();
        }

        internal static DraftUIThemeTargetInspection OpenTargetForInspection(
            DraftUIThemeIndexEntry entry,
            string targetPath,
            DraftUIThemeValidationResult result)
        {
            if (entry.TargetKind == DraftUIThemeTargetKind.Prefab)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
                if (root == null)
                {
                    result.AddError("Missing prefab target asset: " + targetPath);
                    return null;
                }

                return new DraftUIThemeTargetInspection
                {
                    AssetPath = targetPath,
                    Root = root
                };
            }

            Scene scene = SceneManager.GetSceneByPath(targetPath);
            bool openedByTool = false;
            if (scene.IsValid() == false || scene.isLoaded == false)
            {
                scene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Additive);
                openedByTool = true;
            }

            if (scene.IsValid() == false || scene.isLoaded == false)
            {
                result.AddError("Unable to load scene target: " + targetPath);
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            if (roots.Length == 0)
            {
                result.AddError("Missing scene root objects: " + targetPath);
                if (openedByTool && scene.isDirty == false)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                return null;
            }

                        GameObject binderRoot = roots[0];
            for (int i = 0; i < roots.Length; i++)
            {
                if (FindComponentsByType(roots[i], entry.ExpectedBinderTypeName).Length > 0)
                {
                    binderRoot = roots[i];
                    break;
                }
            }

            return new DraftUIThemeTargetInspection
            {
                AssetPath = targetPath,
                Root = binderRoot,
                Scene = scene,
                SceneOpenedByTool = openedByTool
            };
        }

        internal static void CloseTargetAfterInspection(DraftUIThemeTargetInspection inspection)
        {
            if (inspection.SceneOpenedByTool
                && inspection.Scene.IsValid()
                && inspection.Scene.isLoaded
                && inspection.Scene.isDirty == false)
            {
                EditorSceneManager.CloseScene(inspection.Scene, true);
            }
        }
    }

    internal static class DraftUIThemeTool
    {
        public static void ValidateCommand()
        {
            DraftUIThemeValidationResult result = DraftUIThemeValidator.Validate();
            Debug.Log(result.ToLog("Validate (read-only)"));
        }

        public static void PreviewCommand()
        {
            DraftUIThemeValidationResult result = DraftUIThemeValidator.Validate();
            if (result.HasErrors)
            {
                Debug.Log(result.ToLog("Preview Finalize/Bake (read-only)"));
                return;
            }

            AddPreviewPlan(result);
            Debug.Log(result.ToLog("Preview Finalize/Bake (read-only)"));
        }

        public static void BakeCommand()
        {
            DraftUIThemeValidationResult validation = DraftUIThemeValidator.Validate();
            if (validation.HasErrors)
            {
                Debug.Log(validation.ToLog("Bake/Finalize (guarded)"));
                return;
            }

            List<string> guardFailures = CollectBakeGuardFailures(validation);
            if (guardFailures.Count > 0)
            {
                for (int i = 0; i < guardFailures.Count; i++)
                {
                    validation.AddError(guardFailures[i]);
                }

                Debug.Log(validation.ToLog("Bake/Finalize (guarded)"));
                return;
            }

            if (EditorUtility.DisplayDialog(
                    "Draft UI Theme Bake/Finalize",
                    "This writes only the indexed prefab/scene targets, applies current StyleSet Sprites, and removes only draft binders. Continue?",
                    "Bake/Finalize",
                    "Cancel") == false)
            {
                Debug.Log("[Draft UI Theme] Bake/Finalize (guarded) cancelled before writes.");
                return;
            }

            DraftUIThemeValidationResult finalValidation = DraftUIThemeValidator.Validate();
            if (finalValidation.HasErrors)
            {
                Debug.Log(finalValidation.ToLog("Bake/Finalize (guarded)"));
                return;
            }

            List<string> finalGuardFailures = CollectBakeGuardFailures(finalValidation);
            if (finalGuardFailures.Count > 0)
            {
                for (int i = 0; i < finalGuardFailures.Count; i++)
                {
                    finalValidation.AddError(finalGuardFailures[i]);
                }

                Debug.Log(finalValidation.ToLog("Bake/Finalize (guarded)"));
                return;
            }

            for (int i = 0; i < finalValidation.ValidEntries.Count; i++)
            {
                ApplyEntry(finalValidation.ValidEntries[i]);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Draft UI Theme] Bake/Finalize (guarded) PASS. Indexed authoring targets were written; StyleSet assets and scripts were not deleted.");
        }

        static void AddPreviewPlan(DraftUIThemeValidationResult result)
        {
            for (int i = 0; i < result.ValidEntries.Count; i++)
            {
                DraftUIThemeIndexEntry entry = result.ValidEntries[i];
                string targetPath = AssetDatabase.GetAssetPath(entry.AuthoringTarget);
                result.AddInfo("Future bake target: " + targetPath);
                result.AddInfo("Future binder removal: " + entry.ExpectedBinderTypeName + " on " + targetPath);

                DraftUIThemeTargetInspection inspection = DraftUIThemeValidator.OpenTargetForInspection(entry, targetPath, result);
                if (inspection == null)
                {
                    continue;
                }

                try
                {
                    Component[] binders = DraftUIThemeValidator.FindComponentsByType(inspection.Root, entry.ExpectedBinderTypeName);
                    if (binders.Length != 1)
                    {
                        result.AddError("Preview binder ambiguity on " + targetPath + ": expected exactly one " + entry.ExpectedBinderTypeName + ".");
                        continue;
                    }

                    SerializedObject binderObject = new SerializedObject(binders[0]);
                    DraftUIThemeSpriteBinding[] bindings = DraftUIThemeDefinitions.GetBindings(entry.ExpectedBinderTypeName);
                    for (int bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
                    {
                        DraftUIThemeSpriteBinding binding = bindings[bindingIndex];
                        SerializedProperty styleProperty = binderObject.FindProperty(binding.BinderStyleField);
                        UnityEngine.Object styleSet = styleProperty == null ? null : styleProperty.objectReferenceValue;
                        if (styleSet == null)
                        {
                            result.AddError("Preview missing serialized StyleSet binding " + binding.BinderStyleField + " on " + targetPath);
                            continue;
                        }

                        SerializedObject styleObject = new SerializedObject(styleSet);
                        SerializedProperty spriteProperty = styleObject.FindProperty(binding.StyleSpriteProperty);
                        UnityEngine.Object sprite = spriteProperty == null ? null : spriteProperty.objectReferenceValue;
                        string spriteDescription = "<missing>";
                        if (sprite != null)
                        {
                            spriteDescription = AssetDatabase.GetAssetPath(sprite) + " [" + sprite.name + "]";
                        }
                        else if (binding.RequiredSprite == false)
                        {
                            spriteDescription = "<null; HUD PauseImage contract>";
                        }

                        result.AddInfo("Future Sprite assignment: " + targetPath + " " + binding.ImageField + " <= " + spriteDescription);
                    }
                }
                finally
                {
                    DraftUIThemeValidator.CloseTargetAfterInspection(inspection);
                }
            }
        }



        static List<string> CollectBakeGuardFailures(DraftUIThemeValidationResult validation)
        {
            List<string> failures = new List<string>();
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                failures.Add("Bake guard: an unsafe Prefab Stage is open (" + prefabStage.assetPath + ").");
            }

            for (int i = 0; i < validation.ValidEntries.Count; i++)
            {
                DraftUIThemeIndexEntry entry = validation.ValidEntries[i];
                string targetPath = AssetDatabase.GetAssetPath(entry.AuthoringTarget);
                if (entry.TargetKind == DraftUIThemeTargetKind.Prefab)
                {
                    if (EditorUtility.IsDirty(entry.AuthoringTarget))
                    {
                        failures.Add("Bake guard: dirty target prefab " + targetPath);
                    }
                }
                else
                {
                    Scene scene = SceneManager.GetSceneByPath(targetPath);
                    if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                    {
                        failures.Add("Bake guard: loaded dirty target scene " + targetPath);
                    }
                }
            }

            return failures;
        }

        static void ApplyEntry(DraftUIThemeIndexEntry entry)
        {
            string targetPath = AssetDatabase.GetAssetPath(entry.AuthoringTarget);
            if (entry.TargetKind == DraftUIThemeTargetKind.Prefab)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(targetPath);
                try
                {
                    Component binder = FindSingleBinder(root, entry.ExpectedBinderTypeName);
                    ApplySpritesAndRemoveBinder(entry, binder);
                    if (PrefabUtility.SaveAsPrefabAsset(root, targetPath) == false)
                    {
                        throw new InvalidOperationException("Prefab save failed: " + targetPath);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                return;
            }

            Scene scene = SceneManager.GetSceneByPath(targetPath);
            bool openedByTool = false;
            if (scene.IsValid() == false || scene.isLoaded == false)
            {
                scene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Additive);
                openedByTool = true;
            }

            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                Component binder = FindSingleBinder(roots, entry.ExpectedBinderTypeName);
                ApplySpritesAndRemoveBinder(entry, binder);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedByTool && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static Component FindSingleBinder(GameObject root, string binderTypeName)
        {
            Component[] binders = FindComponentsByType(root, binderTypeName);
            if (binders.Length != 1)
            {
                throw new InvalidOperationException("Binder count changed before write: " + binderTypeName + " on " + root.name);
            }

            return binders[0];
        }

        static Component FindSingleBinder(GameObject[] roots, string binderTypeName)
        {
            List<Component> binders = new List<Component>();
            for (int i = 0; i < roots.Length; i++)
            {
                Component[] rootBinders = FindComponentsByType(roots[i], binderTypeName);
                binders.AddRange(rootBinders);
            }

            if (binders.Count != 1)
            {
                throw new InvalidOperationException("Binder count changed before write: " + binderTypeName);
            }

            return binders[0];
        }

        static Component[] FindComponentsByType(GameObject root, string fullTypeName)
        {
            List<Component> matches = new List<Component>();
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null && component.GetType().FullName == fullTypeName)
                {
                    matches.Add(component);
                }
            }

            return matches.ToArray();
        }

        static void ApplySpritesAndRemoveBinder(DraftUIThemeIndexEntry entry, Component binder)
        {
            SerializedObject binderObject = new SerializedObject(binder);
            DraftUIThemeSpriteBinding[] bindings = DraftUIThemeDefinitions.GetBindings(entry.ExpectedBinderTypeName);
            for (int i = 0; i < bindings.Length; i++)
            {
                DraftUIThemeSpriteBinding binding = bindings[i];
                SerializedProperty styleProperty = binderObject.FindProperty(binding.BinderStyleField);
                SerializedProperty imageProperty = binderObject.FindProperty(binding.ImageField);
                UnityEngine.Object styleSet = styleProperty.objectReferenceValue;
                Image image = imageProperty.objectReferenceValue as Image;
                if (styleSet == null || image == null)
                {
                    throw new InvalidOperationException("Serialized binding became ambiguous before write: " + binding.ImageField);
                }

                SerializedObject styleObject = new SerializedObject(styleSet);
                SerializedProperty spriteProperty = styleObject.FindProperty(binding.StyleSpriteProperty);
                if (spriteProperty == null)
                {
                    throw new InvalidOperationException("StyleSet Sprite slot missing before write: " + binding.StyleSpriteProperty);
                }

                SerializedObject imageObject = new SerializedObject(image);
                SerializedProperty imageSpriteProperty = imageObject.FindProperty("m_Sprite");
                if (imageSpriteProperty == null)
                {
                    throw new InvalidOperationException("Image Sprite property missing before write: " + binding.ImageField);
                }

                imageSpriteProperty.objectReferenceValue = spriteProperty.objectReferenceValue;
                imageObject.ApplyModifiedPropertiesWithoutUndo();
            }

            UnityEngine.Object.DestroyImmediate(binder, true);
        }
    }

    public sealed class DraftUIThemeWindow : EditorWindow
    {
        [MenuItem(DraftUIThemeDefinitions.MenuRoot)]
        public static void Open()
        {
            GetWindow<DraftUIThemeWindow>("Draft UI Theme");
        }

        [MenuItem(DraftUIThemeDefinitions.MenuRoot + "/Validate")]
        static void ValidateMenu()
        {
            DraftUIThemeTool.ValidateCommand();
        }

        [MenuItem(DraftUIThemeDefinitions.MenuRoot + "/Preview Finalize-Bake")]
        static void PreviewMenu()
        {
            DraftUIThemeTool.PreviewCommand();
        }

        [MenuItem(DraftUIThemeDefinitions.MenuRoot + "/Bake-Finalize (Guarded)")]
        static void BakeMenu()
        {
            DraftUIThemeTool.BakeCommand();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Temporary Draft UI Theme", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The index is the only registration surface. Validate and Preview are read-only. Bake/Finalize is guarded and intentionally not invoked by this slice.",
                MessageType.Info);

            if (GUILayout.Button("Validate (Read-only)"))
            {
                DraftUIThemeTool.ValidateCommand();
            }

            if (GUILayout.Button("Preview Finalize/Bake (Read-only)"))
            {
                DraftUIThemeTool.PreviewCommand();
            }

            using (new EditorGUI.DisabledScope(true))
            {
                GUILayout.Button("Bake/Finalize (Guarded; owner-operated)");
            }
        }
    }
}
