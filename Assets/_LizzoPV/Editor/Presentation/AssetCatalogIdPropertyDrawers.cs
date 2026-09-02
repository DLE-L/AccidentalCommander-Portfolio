using Lizzo.PV.Presentation;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Editor.Presentation
{
    internal static class AssetCatalogIdPropertyDrawerGUI
    {
        private const float ButtonWidth = 52f;

        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("_value");
            if (valueProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "Missing serialized _value");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            Rect valueRect = position;
            valueRect.width -= ButtonWidth + 4f;
            Rect buttonRect = position;
            buttonRect.xMin = valueRect.xMax + 4f;

            string labelText = string.IsNullOrEmpty(label.text) || label.text == "Id" ? "Asset ID" : label.text;
            valueProperty.intValue = EditorGUI.IntField(valueRect, new GUIContent(labelText, "Project-wide int Asset ID. Editable at any time."), valueProperty.intValue);

            var nextContent = new GUIContent("Next", "Assign the next project-wide Asset ID. The value remains freely editable.");
            if (GUI.Button(buttonRect, nextContent))
            {
                AssetCatalogIdReport report = AssetCatalogIdIndex.BuildFromProject();
                if (report.NextAvailableId <= 0)
                {
                    Debug.LogError("[Asset Catalog] A next Asset ID could not be allocated because the positive int range is exhausted.");
                }
                else
                {
                    valueProperty.intValue = report.NextAvailableId;
                }
            }

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(SpriteAssetId))]
    internal sealed class SpriteAssetIdPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AssetCatalogIdPropertyDrawerGUI.Draw(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(AudioAssetId))]
    internal sealed class AudioAssetIdPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AssetCatalogIdPropertyDrawerGUI.Draw(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(VfxAssetId))]
    internal sealed class VfxAssetIdPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AssetCatalogIdPropertyDrawerGUI.Draw(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(MotionAssetId))]
    internal sealed class MotionAssetIdPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AssetCatalogIdPropertyDrawerGUI.Draw(position, property, label);
        }
    }
}
