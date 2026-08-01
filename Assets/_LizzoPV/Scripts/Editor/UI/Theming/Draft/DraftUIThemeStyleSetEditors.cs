using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Lizzo.PV.UI.Theming.Draft;

namespace Lizzo.PV.EditorTools.UI.Theming.Draft
{
    internal abstract class DraftUIThemeStyleSetEditor : Editor
    {
        protected abstract string SurfaceId { get; }
        protected abstract DraftUIThemeTargetKind ExpectedTargetKind { get; }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawAuthoringTarget();
        }

        void DrawAuthoringTarget()
        {
            EditorGUILayout.LabelField("Authoring Target", EditorStyles.boldLabel);
            DraftUIThemeTargetResolution resolution = DraftUIThemeTargetResolver.Resolve(
                target as ScriptableObject,
                SurfaceId,
                ExpectedTargetKind);

            if (resolution.IsValid)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        ExpectedTargetKind == DraftUIThemeTargetKind.Scene ? "Scene" : "Prefab",
                        resolution.Target,
                        ExpectedTargetKind == DraftUIThemeTargetKind.Scene ? typeof(SceneAsset) : typeof(GameObject),
                        false);
                    EditorGUILayout.LabelField("Path", resolution.TargetPath);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(resolution.Message, MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(resolution.IsValid == false))
            {
                if (GUILayout.Button("Ping"))
                {
                    EditorGUIUtility.PingObject(resolution.Target);
                }

                if (GUILayout.Button("Open"))
                {
                    AssetDatabase.OpenAsset(resolution.Target);
                }
            }
        }
    }

    [CustomEditor(typeof(SkillSelectStyleSet))]
    internal sealed class SkillSelectStyleSetEditor : DraftUIThemeStyleSetEditor
    {
        protected override string SurfaceId => "SkillSelectPopup";
        protected override DraftUIThemeTargetKind ExpectedTargetKind => DraftUIThemeTargetKind.Prefab;
    }

    [CustomEditor(typeof(GameplayHudStyleSet))]
    internal sealed class GameplayHudStyleSetEditor : DraftUIThemeStyleSetEditor
    {
        protected override string SurfaceId => "HUD";
        protected override DraftUIThemeTargetKind ExpectedTargetKind => DraftUIThemeTargetKind.Prefab;
    }

    [CustomEditor(typeof(PauseOverlayStyleSet))]
    internal sealed class PauseOverlayStyleSetEditor : DraftUIThemeStyleSetEditor
    {
        protected override string SurfaceId => "HUD";
        protected override DraftUIThemeTargetKind ExpectedTargetKind => DraftUIThemeTargetKind.Prefab;
    }

    [CustomEditor(typeof(LobbyStyleSet))]
    internal sealed class LobbyStyleSetEditor : DraftUIThemeStyleSetEditor
    {
        protected override string SurfaceId => "Lobby";
        protected override DraftUIThemeTargetKind ExpectedTargetKind => DraftUIThemeTargetKind.Scene;
    }

    internal sealed class DraftUIThemeTargetResolution
    {
        public bool IsValid;
        public string Message;
        public UnityEngine.Object Target;
        public string TargetPath;
    }

    internal static class DraftUIThemeTargetResolver
    {
        public static DraftUIThemeTargetResolution Resolve(
            ScriptableObject styleSet,
            string surfaceId,
            DraftUIThemeTargetKind expectedTargetKind)
        {
            DraftUIThemeTargetResolution resolution = new DraftUIThemeTargetResolution();
            DraftUIThemeIndex index = AssetDatabase.LoadAssetAtPath<DraftUIThemeIndex>(DraftUIThemeDefinitions.IndexPath);
            if (index == null)
            {
                resolution.Message = "No authoring target: DraftUIThemeIndex is missing at " + DraftUIThemeDefinitions.IndexPath + ".";
                return resolution;
            }

            List<DraftUIThemeIndexEntry> matchingEntries = new List<DraftUIThemeIndexEntry>();
            List<DraftUIThemeIndexEntry> allReferences = new List<DraftUIThemeIndexEntry>();
            IReadOnlyList<DraftUIThemeIndexEntry> entries = index.Entries;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    DraftUIThemeIndexEntry entry = entries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    int referenceCount = CountReferences(entry.StyleSetAssets, styleSet);
                    for (int referenceIndex = 0; referenceIndex < referenceCount; referenceIndex++)
                    {
                        allReferences.Add(entry);
                        if (string.Equals(entry.SurfaceId, surfaceId, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingEntries.Add(entry);
                        }
                    }
                }
            }

            if (allReferences.Count > 1)
            {
                resolution.Message = "Ambiguous authoring target: DraftUIThemeIndex has multiple mappings for this StyleSet.";
                return resolution;
            }

            if (matchingEntries.Count == 0)
            {
                resolution.Message = allReferences.Count == 0
                    ? "No authoring target mapping exists in DraftUIThemeIndex for this StyleSet."
                    : "No authoring target mapping exists for surface '" + surfaceId + "'; the StyleSet is registered under another surface.";
                return resolution;
            }

            if (matchingEntries.Count > 1)
            {
                resolution.Message = "Ambiguous authoring target: DraftUIThemeIndex has multiple '" + surfaceId + "' mappings for this StyleSet.";
                return resolution;
            }

            DraftUIThemeIndexEntry match = matchingEntries[0];
            if (match.AuthoringTarget == null)
            {
                resolution.Message = "Missing authoring target: the '" + surfaceId + "' index mapping has no target asset.";
                return resolution;
            }

            string targetPath = AssetDatabase.GetAssetPath(match.AuthoringTarget);
            if (string.IsNullOrEmpty(targetPath))
            {
                resolution.Message = "Missing authoring target: the '" + surfaceId + "' index target has no asset path.";
                return resolution;
            }

            if (match.TargetKind != expectedTargetKind
                || IsTargetKindMatch(match.AuthoringTarget, targetPath, expectedTargetKind) == false)
            {
                string expectedKind = expectedTargetKind == DraftUIThemeTargetKind.Scene ? "scene" : "prefab";
                resolution.Message = "Authoring target kind mismatch: '" + surfaceId + "' must map to a " + expectedKind + ", but resolves to " + targetPath + ".";
                return resolution;
            }

            resolution.IsValid = true;
            resolution.Target = match.AuthoringTarget;
            resolution.TargetPath = targetPath;
            return resolution;
        }

        static int CountReferences(UnityEngine.Object[] objects, UnityEngine.Object target)
        {
            if (objects == null || target == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == target)
                {
                    count++;
                }
            }

            return count;
        }

        static bool IsTargetKindMatch(
            UnityEngine.Object target,
            string targetPath,
            DraftUIThemeTargetKind expectedTargetKind)
        {
            if (expectedTargetKind == DraftUIThemeTargetKind.Prefab)
            {
                return target is GameObject
                    && targetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                    && AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null;
            }

            if (expectedTargetKind == DraftUIThemeTargetKind.Scene)
            {
                return target is SceneAsset
                    && targetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                    && AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) != null;
            }

            return false;
        }
    }
}
