using Lizzo.PV.P0.Cards;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.P0.Editor
{
    public abstract class P0NamedCardDataEditor : UnityEditor.Editor
    {
        protected void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }
        }

        protected void DrawArray(string propertyName, string label)
        {
            SerializedProperty array = serializedObject.FindProperty(propertyName);
            if (array == null || array.isArray == false)
                return;

            int newSize = Mathf.Max(0, EditorGUILayout.DelayedIntField(label, array.arraySize));
            if (newSize != array.arraySize)
                array.arraySize = newSize;

            EditorGUILayout.Space(4.0f);
            for (int i = 0; i < array.arraySize; i++)
                DrawElement(array.GetArrayElementAtIndex(i), i);
        }

        protected static void DrawElement(SerializedProperty element, int index)
        {
            string label = ResolveElementLabel(element, index);
            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, label, toggleOnLabelClick: true);
            if (element.isExpanded == false)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty child = element.Copy();
                SerializedProperty end = child.GetEndProperty();
                bool enterChildren = true;
                while (child.NextVisible(enterChildren) && SerializedProperty.EqualContents(child, end) == false)
                {
                    EditorGUILayout.PropertyField(child, includeChildren: true);
                    enterChildren = false;
                }
            }
        }

        private static string ResolveElementLabel(SerializedProperty element, int index)
        {
            SerializedProperty level = element.FindPropertyRelative("_levelUpIndex");
            if (level != null)
                return $"Level {Mathf.Max(1, level.intValue)}";

            SerializedProperty id = element.FindPropertyRelative("_id");
            if (id != null && string.IsNullOrWhiteSpace(id.stringValue) == false)
                return id.stringValue;

            SerializedProperty kind = element.FindPropertyRelative("_kind");
            if (kind != null && kind.enumValueIndex >= 0 && kind.enumValueIndex < kind.enumDisplayNames.Length)
                return kind.enumDisplayNames[kind.enumValueIndex];

            SerializedProperty highlight = element.FindPropertyRelative("_highlight");
            if (highlight != null && highlight.enumValueIndex >= 0 && highlight.enumValueIndex < highlight.enumDisplayNames.Length)
                return highlight.enumDisplayNames[highlight.enumValueIndex];

            return $"Element {index}";
        }
    }

    [CustomEditor(typeof(CardDefinitionSet))]
    public sealed class P0CardDefinitionSetEditor : P0NamedCardDataEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawScriptField();
            EditorGUILayout.Space(4.0f);
            DrawArray("_entries", "Entries");
            EditorGUILayout.Space(8.0f);
            DrawArray("_highlightEntries", "Highlight Entries");
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(CardPoolDefinition))]
    public sealed class P0CardPoolDefinitionEditor : P0NamedCardDataEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawScriptField();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_cardOptionCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_fillGuardLimit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_fullSlotPressureStartOffset"));
            EditorGUILayout.Space(8.0f);
            DrawArray("_fixedOffers", "Fixed Offers");
            EditorGUILayout.Space(8.0f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_levelFivePlusRandomPool"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_fallbackKinds"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_squadBucket"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_utilityBucket"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_passiveBucketDefault"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_passiveBucketAfterShield"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
