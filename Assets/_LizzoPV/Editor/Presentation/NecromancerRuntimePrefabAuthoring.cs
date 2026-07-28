using System;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class NecromancerRuntimePrefabAuthoring
    {
        private static readonly string[] Paths =
        {
            "Assets/_LizzoPV/Prefabs/Characters/Companions/Necromancer.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/DarkRitualist.prefab",
        };

        public static void CreateOrUpdate()
        {
            for (int i = 0; i < Paths.Length; i++)
                Compose(Paths[i]);
            AssetDatabase.SaveAssets();
        }

        private static void Compose(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                UnitColliderRefs refs = root.GetComponent<UnitColliderRefs>();
                if (refs == null || refs.BodyCollider == null || refs.CombatCollider == null)
                    throw new InvalidOperationException($"Missing authored collider refs: {prefabPath}");

                AddIfMissing<AllyCombat>(root);
                AddIfMissing<AllyFollower>(root);
                CompanionRuntime runtime = AddIfMissing<CompanionRuntime>(root);
                AddIfMissing<CompanionHealthBar>(root);
                SetReference(runtime, "_bodyCollider", refs.BodyCollider);
                SetReference(runtime, "_combatCollider", refs.CombatCollider);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static T AddIfMissing<T>(GameObject root) where T : Component => root.GetComponent<T>() ?? root.AddComponent<T>();

        private static void SetReference(Component component, string propertyPath, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
                throw new InvalidOperationException($"Serialized reference is missing: {component.GetType().Name}.{propertyPath}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
