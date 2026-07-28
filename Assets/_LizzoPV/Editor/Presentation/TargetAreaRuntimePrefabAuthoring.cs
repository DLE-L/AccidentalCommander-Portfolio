using System;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class TargetAreaRuntimePrefabAuthoring
    {
        private static readonly string[] PrefabPaths =
        {
            "Assets/_LizzoPV/Prefabs/Characters/Companions/Bombardier.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/PowderCaptain.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/SkeletonBomber.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/BoneArtillery.prefab",
        };

        public static void CreateOrUpdate()
        {
            for (int i = 0; i < PrefabPaths.Length; i++)
                Compose(PrefabPaths[i]);

            AssetDatabase.SaveAssets();
        }

        private static void Compose(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                CircleCollider2D body = RequireCollider(root, "BodyCollider");
                CircleCollider2D combat = RequireCollider(root, "CombatCollider");
                AddIfMissing<AllyCombat>(root);
                AddIfMissing<AllyFollower>(root);
                CompanionRuntime runtime = AddIfMissing<CompanionRuntime>(root);
                AddIfMissing<CompanionHealthBar>(root);
                UnitColliderRefs colliderRefs = root.GetComponent<UnitColliderRefs>();
                if (colliderRefs == null)
                    throw new InvalidOperationException($"Canonical companion prefab is missing UnitColliderRefs: {prefabPath}");

                SetReference(runtime, "_bodyCollider", body);
                SetReference(runtime, "_combatCollider", combat);
                SetReference(colliderRefs, "_bodyCollider", body);
                SetReference(colliderRefs, "_combatCollider", combat);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T AddIfMissing<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component == null ? root.AddComponent<T>() : component;
        }

        private static CircleCollider2D RequireCollider(GameObject root, string path)
        {
            CircleCollider2D collider = root.transform.Find(path)?.GetComponent<CircleCollider2D>();
            if (collider == null)
                throw new InvalidOperationException($"Canonical companion prefab is missing required {path}: {root.name}");

            return collider;
        }

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
