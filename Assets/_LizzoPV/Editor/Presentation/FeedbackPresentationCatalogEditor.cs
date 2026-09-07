using Lizzo.PV.Gameplay.Presentation;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Editor.Presentation
{
    [CustomEditor(typeof(FeedbackPresentationCatalog))]
    public sealed class FeedbackPresentationCatalogEditor : UnityEditor.Editor
    {
        private SerializedProperty _cues;

        private void OnEnable()
        {
            _cues = serializedObject.FindProperty("_cues");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            FeedbackPresentationCatalog catalog = (FeedbackPresentationCatalog)target;
            if (catalog.TryValidate(out string issue) == false)
                EditorGUILayout.HelpBox(issue, MessageType.Error);

            EditorGUILayout.LabelField("SFX Cues", EditorStyles.boldLabel);
            for (int i = 0; i < _cues.arraySize; i++)
            {
                SerializedProperty cue = _cues.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(cue.FindPropertyRelative("_presentationId"), new GUIContent("ID"));
                EditorGUILayout.PropertyField(cue.FindPropertyRelative("_sfx"), new GUIContent("SFX"));
                EditorGUILayout.PropertyField(cue.FindPropertyRelative("_volume"), new GUIContent("Volume"));
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
