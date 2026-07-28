using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class MagicRuntimePrefabAuthoring
    {
        private static readonly string[] Paths =
        {
            "Assets/_LizzoPV/Prefabs/Characters/Companions/FireMage.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/FireSage.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/LightningMage.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/StormMage.prefab",
        };

        public static void CreateOrUpdate()
        {
            foreach (string path in Paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    UnitColliderRefs refs = root.GetComponent<UnitColliderRefs>();
                    if (refs == null || refs.BodyCollider == null || refs.CombatCollider == null)
                        throw new System.InvalidOperationException($"Missing authored collider refs: {path}");
                    if (root.GetComponent<AllyCombat>() == null) root.AddComponent<AllyCombat>();
                    if (root.GetComponent<AllyFollower>() == null) root.AddComponent<AllyFollower>();
                    CompanionRuntime runtime = root.GetComponent<CompanionRuntime>() ?? root.AddComponent<CompanionRuntime>();
                    if (root.GetComponent<CompanionHealthBar>() == null) root.AddComponent<CompanionHealthBar>();
                    SetReference(runtime, "_bodyCollider", refs.BodyCollider);
                    SetReference(runtime, "_combatCollider", refs.CombatCollider);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }

        private static void SetReference(Component component, string propertyPath, Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
                throw new System.InvalidOperationException($"Serialized reference is missing: {component.GetType().Name}.{propertyPath}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
