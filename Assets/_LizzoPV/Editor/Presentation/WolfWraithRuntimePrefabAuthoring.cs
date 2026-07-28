using System;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Units;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class WolfWraithRuntimePrefabAuthoring
    {
        private static readonly string[] WolfPaths =
        {
            "Assets/_LizzoPV/Prefabs/Characters/Companions/WolfTamer.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/BeastCommander.prefab",
        };

        private static readonly string[] WraithPaths =
        {
            "Assets/_LizzoPV/Prefabs/Characters/Companions/WraithKnight.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/WraithGuardian.prefab",
        };

        public static void CreateOrUpdate()
        {
            for (int i = 0; i < WolfPaths.Length; i++) Compose(WolfPaths[i], true);
            for (int i = 0; i < WraithPaths.Length; i++) Compose(WraithPaths[i], false);
            AssetDatabase.SaveAssets();
        }

        private static void Compose(string prefabPath, bool requiresWolfPresenter)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                UnitColliderRefs refs = root.GetComponent<UnitColliderRefs>();
                if (refs == null || refs.BodyCollider == null || refs.CombatCollider == null)
                    throw new InvalidOperationException($"Missing authored collider references: {prefabPath}");

                AddIfMissing<AllyCombat>(root);
                AddIfMissing<AllyFollower>(root);
                CompanionRuntime runtime = AddIfMissing<CompanionRuntime>(root);
                AddIfMissing<CompanionHealthBar>(root);
                SetReference(runtime, "_bodyCollider", refs.BodyCollider);
                SetReference(runtime, "_combatCollider", refs.CombatCollider);

                if (requiresWolfPresenter)
                {
                    OwnerBoundSupportPresenterBehaviour presenter = AddIfMissing<OwnerBoundSupportPresenterBehaviour>(root);
                    SetReference(presenter, "_combat", root.GetComponent<AllyCombat>());
                    SetReference(presenter, "_owner", root.transform);
                    SetString(presenter, "_supportId", "grey_wolf_support");
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static T AddIfMissing<T>(GameObject root) where T : Component => root.GetComponent<T>() ?? root.AddComponent<T>();

        private static void SetReference(Component component, string propertyPath, UnityEngine.Object value)
        {
            SerializedProperty property = new SerializedObject(component).FindProperty(propertyPath);
            if (property == null) throw new InvalidOperationException($"Serialized reference is missing: {component.GetType().Name}.{propertyPath}");
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Component component, string propertyPath, string value)
        {
            SerializedProperty property = new SerializedObject(component).FindProperty(propertyPath);
            if (property == null) throw new InvalidOperationException($"Serialized string is missing: {component.GetType().Name}.{propertyPath}");
            property.stringValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
