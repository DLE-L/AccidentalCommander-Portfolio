using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Editor.Presentation
{
    [CustomEditor(typeof(FeedbackPresentationSet))]
    public sealed class FeedbackPresentationSetEditor : UnityEditor.Editor
    {
        private static readonly GUIContent PrefabLabel = new GUIContent("Prefab");
        private static readonly GUIContent ScaleLabel = new GUIContent("Scale");
        private static readonly GUIContent LifetimeLabel = new GUIContent("Lifetime");

        private SerializedProperty _entries;

        private void OnEnable()
        {
            _entries = serializedObject.FindProperty("_entries");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FeedbackPresentationSet presentationSet = (FeedbackPresentationSet)target;
            if (presentationSet.TryValidate(out string issue) == false)
                EditorGUILayout.HelpBox(issue, MessageType.Error);

            Dictionary<int, int> kindCounts = CountKinds();
            DrawMissingKinds(kindCounts);

            for (int i = 0; i < _entries.arraySize; i++)
                DrawEntry(_entries.GetArrayElementAtIndex(i), i, kindCounts);

            serializedObject.ApplyModifiedProperties();
        }

        private Dictionary<int, int> CountKinds()
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int i = 0; i < _entries.arraySize; i++)
            {
                SerializedProperty kind = _entries.GetArrayElementAtIndex(i).FindPropertyRelative("_kind");
                int value = kind.intValue;
                counts.TryGetValue(value, out int count);
                counts[value] = count + 1;
            }

            return counts;
        }

        private static void DrawMissingKinds(Dictionary<int, int> kindCounts)
        {
            Array values = Enum.GetValues(typeof(RetroVfxKind));
            for (int i = 0; i < values.Length; i++)
            {
                RetroVfxKind kind = (RetroVfxKind)values.GetValue(i);
                if (kindCounts.ContainsKey((int)kind) == false)
                    EditorGUILayout.HelpBox($"Missing fixed slot: {kind} / {FeedbackPresentationSet.GetExpectedSlotId(kind)}", MessageType.Error);
            }
        }

        private static void DrawEntry(SerializedProperty entry, int index, Dictionary<int, int> kindCounts)
        {
            SerializedProperty kindProperty = entry.FindPropertyRelative("_kind");
            SerializedProperty slotIdProperty = entry.FindPropertyRelative("_slotId");
            SerializedProperty prefabProperty = entry.FindPropertyRelative("_prefab");
            RetroVfxKind kind = (RetroVfxKind)kindProperty.intValue;
            string rowIssue = GetRowIssue(kind, slotIdProperty.stringValue, prefabProperty.objectReferenceValue, kindCounts);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (string.IsNullOrEmpty(rowIssue) == false)
                EditorGUILayout.HelpBox($"Entry {index}: {rowIssue}", MessageType.Error);

            EditorGUILayout.LabelField($"{kind} / {slotIdProperty.stringValue}", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prefabProperty, PrefabLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_scale"), ScaleLabel, GUILayout.MinWidth(120.0f));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_lifetime"), LifetimeLabel, GUILayout.MinWidth(120.0f));
            EditorGUILayout.EndHorizontal();

            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, "Advanced", true);
            if (entry.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(kindProperty);
                        EditorGUILayout.PropertyField(slotIdProperty);
                    }

                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_forwardOffset"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_upOffset"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_alignToDirection"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_angleOffset"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_sfx"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_sfxVolumeScale"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_minScale"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_maxScale"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_isScaleException"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_isHitFeedback"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_hasRewardCue"));
                }
            }

            EditorGUILayout.EndVertical();
        }

        private static string GetRowIssue(
            RetroVfxKind kind,
            string slotId,
            UnityEngine.Object prefab,
            Dictionary<int, int> kindCounts)
        {
            if (Enum.IsDefined(typeof(RetroVfxKind), kind) == false)
                return $"undefined kind value {(int)kind}";

            if (kindCounts.TryGetValue((int)kind, out int count) && count > 1)
                return $"duplicate kind {kind}";

            string expectedSlotId = FeedbackPresentationSet.GetExpectedSlotId(kind);
            if (string.Equals(slotId, expectedSlotId, StringComparison.Ordinal) == false)
                return $"slot ID must remain '{expectedSlotId}'";

            if (prefab == null)
                return "prefab is missing";

            return string.Empty;
        }
    }
}
