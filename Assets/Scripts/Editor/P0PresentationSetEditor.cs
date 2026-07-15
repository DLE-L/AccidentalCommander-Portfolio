using Lizzo.PV.P0.Presentation;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.P0.Editor
{
    public abstract class P0PresentationSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }

            SerializedProperty entries = serializedObject.FindProperty("_entries");
            if (entries == null || entries.isArray == false)
            {
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
                return;
            }

            int newSize = Mathf.Max(0, EditorGUILayout.DelayedIntField("Entries", entries.arraySize));
            if (newSize != entries.arraySize)
                entries.arraySize = newSize;

            EditorGUILayout.Space(4.0f);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                DrawEntry(entry, i);
            }

            serializedObject.ApplyModifiedProperties();
        }

        static void DrawEntry(SerializedProperty entry, int index)
        {
            string label = ResolveEntryLabel(entry, index);
            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, label, toggleOnLabelClick: true);
            if (entry.isExpanded == false)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty child = entry.Copy();
                SerializedProperty end = child.GetEndProperty();
                bool enterChildren = true;
                while (child.NextVisible(enterChildren) && SerializedProperty.EqualContents(child, end) == false)
                {
                    EditorGUILayout.PropertyField(child, includeChildren: true);
                    enterChildren = false;
                }
            }
        }

        static string ResolveEntryLabel(SerializedProperty entry, int index)
        {
            SerializedProperty slotId = entry.FindPropertyRelative("_slotId");
            if (slotId != null && string.IsNullOrWhiteSpace(slotId.stringValue) == false)
                return slotId.stringValue;

            SerializedProperty id = entry.FindPropertyRelative("_id");
            if (id != null && string.IsNullOrWhiteSpace(id.stringValue) == false)
                return id.stringValue;

            SerializedProperty kind = entry.FindPropertyRelative("_kind");
            if (kind != null)
                return kind.enumDisplayNames[kind.enumValueIndex];

            return $"Element {index}";
        }
    }

    [CustomEditor(typeof(FeedbackPresentationSet))]
    public sealed class P0FeedbackPresentationSetEditor : P0PresentationSetEditor
    {
    }

    [CustomEditor(typeof(AnnouncementPresentationSet))]
    public sealed class P0AnnouncementPresentationSetEditor : P0PresentationSetEditor
    {
    }

    [CustomEditor(typeof(CardPresentationSet))]
    public sealed class P0CardPresentationSetEditor : P0PresentationSetEditor
    {
    }

    [CustomEditor(typeof(SquadSlotPresentationSet))]
    public sealed class P0SquadSlotPresentationSetEditor : P0PresentationSetEditor
    {
    }

    [CustomEditor(typeof(UnitPresentationSet))]
    public sealed class P0UnitPresentationSetEditor : P0PresentationSetEditor
    {
    }
}
