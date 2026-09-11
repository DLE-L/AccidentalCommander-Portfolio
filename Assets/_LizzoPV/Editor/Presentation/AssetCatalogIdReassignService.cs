using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lizzo.PV.Editor.Presentation
{
    public readonly struct AssetIdReassignResult
    {
        public AssetIdReassignResult(int changedAssetCount, int changedPropertyCount)
        {
            ChangedAssetCount = changedAssetCount;
            ChangedPropertyCount = changedPropertyCount;
        }

        public int ChangedAssetCount { get; }
        public int ChangedPropertyCount { get; }
    }

    public static class AssetCatalogIdReassignService
    {
        private static readonly string[] TypedIdNames =
        {
            "SpriteAssetId",
            "AudioAssetId",
            "VfxAssetId",
            "MotionAssetId",
        };

        public static bool TryReassignProject(
            AssetCatalogKind kind,
            int oldId,
            int newId,
            out AssetIdReassignResult result,
            out string issue)
        {
            AssetCatalogIdReport catalogReport = AssetCatalogIdIndex.BuildFromProject();
            if (!catalogReport.IsValid)
            {
                result = default;
                issue = "Project Asset Catalog IDs must be valid before reassignment.";
                return false;
            }

            AssetCatalogIdRecord? sourceRecord = null;
            for (int index = 0; index < catalogReport.Records.Count; index++)
            {
                AssetCatalogIdRecord record = catalogReport.Records[index];
                if (record.Id == oldId)
                {
                    sourceRecord = record;
                    break;
                }
            }

            if (!sourceRecord.HasValue)
            {
                result = default;
                issue = $"Asset ID {oldId} is not registered in a Catalog.";
                return false;
            }

            if (sourceRecord.Value.Kind != kind)
            {
                result = default;
                issue = $"Asset ID {oldId} belongs to {sourceRecord.Value.Kind}, not {kind}.";
                return false;
            }

            string referencedSceneOrPrefab = FindSceneOrPrefabReference(GetTypedIdName(kind), oldId);
            if (referencedSceneOrPrefab != null)
            {
                result = default;
                issue = $"Asset ID {oldId} is referenced by '{referencedSceneOrPrefab}'. This tool updates ScriptableObjects only; keep the ID and replace its resource, or perform an explicit scene/prefab migration.";
                return false;
            }

            IReadOnlyList<Object> projectAssets = LoadProjectScriptableObjects();
            if (!TryReassignObjects(kind, oldId, newId, projectAssets, out result, out issue))
            {
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetCatalogIdReport verification = AssetCatalogIdIndex.BuildFromProject();
            if (!verification.IsValid)
            {
                issue = "Asset ID reassignment completed, but Catalog verification failed.";
                return false;
            }

            for (int index = 0; index < verification.Records.Count; index++)
            {
                AssetCatalogIdRecord record = verification.Records[index];
                if (record.Id == oldId)
                {
                    issue = $"Asset ID reassignment left Catalog reference {oldId} behind.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        public static bool TryReassignObjects(
            AssetCatalogKind kind,
            int oldId,
            int newId,
            IReadOnlyList<Object> assets,
            out AssetIdReassignResult result,
            out string issue)
        {
            result = default;
            if (oldId <= 0 || newId <= 0)
            {
                issue = "Old and new Asset IDs must be positive.";
                return false;
            }

            if (oldId == newId)
            {
                issue = "Old and new Asset IDs must be different.";
                return false;
            }

            if (assets == null)
            {
                issue = "Asset collection is null.";
                return false;
            }

            string targetTypeName = GetTypedIdName(kind);
            var targets = new List<AssetPropertyTarget>();
            for (int assetIndex = 0; assetIndex < assets.Count; assetIndex++)
            {
                Object asset = assets[assetIndex];
                if (asset == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(asset);
                SerializedProperty iterator = serializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.Next(enterChildren))
                {
                    enterChildren = true;
                    if (!IsTypedAssetId(iterator.type))
                    {
                        continue;
                    }

                    SerializedProperty valueProperty = iterator.FindPropertyRelative("_value");
                    if (valueProperty == null || valueProperty.propertyType != SerializedPropertyType.Integer)
                    {
                        continue;
                    }

                    if (valueProperty.intValue == newId)
                    {
                        issue = $"New Asset ID {newId} is already referenced by {asset.name}:{iterator.propertyPath}.";
                        return false;
                    }

                    if (iterator.type == targetTypeName && valueProperty.intValue == oldId)
                    {
                        targets.Add(new AssetPropertyTarget(asset, iterator.propertyPath));
                    }
                }
            }

            if (targets.Count == 0)
            {
                issue = $"No {targetTypeName} reference uses Asset ID {oldId}.";
                return false;
            }

            var changedAssets = new HashSet<Object>();
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                changedAssets.Add(targets[targetIndex].Asset);
            }

            Object[] undoTargets = new Object[changedAssets.Count];
            changedAssets.CopyTo(undoTargets);
            Undo.RecordObjects(undoTargets, $"Reassign {targetTypeName} {oldId} to {newId}");

            foreach (Object asset in changedAssets)
            {
                var serializedObject = new SerializedObject(asset);
                bool changed = false;
                for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    AssetPropertyTarget target = targets[targetIndex];
                    if (target.Asset != asset)
                    {
                        continue;
                    }

                    SerializedProperty idProperty = serializedObject.FindProperty(target.PropertyPath);
                    SerializedProperty valueProperty = idProperty?.FindPropertyRelative("_value");
                    if (valueProperty == null || valueProperty.intValue != oldId)
                    {
                        issue = $"Asset ID target changed before apply: {asset.name}:{target.PropertyPath}.";
                        return false;
                    }

                    valueProperty.intValue = newId;
                    changed = true;
                }

                if (changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                }
            }

            if (CountReferences(targetTypeName, oldId, assets) != 0)
            {
                issue = $"Asset ID reassignment left {targetTypeName} reference {oldId} behind.";
                return false;
            }

            result = new AssetIdReassignResult(changedAssets.Count, targets.Count);
            issue = string.Empty;
            return true;
        }

        private static string FindSceneOrPrefabReference(string typeName, int id)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_LizzoPV" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && CountReferences(typeName, id, prefab.GetComponentsInChildren<MonoBehaviour>(true)) > 0)
                    return path;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_LizzoPV" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                        if (CountReferences(typeName, id, root.GetComponentsInChildren<MonoBehaviour>(true)) > 0)
                            return path;
                }
                finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
            }
            return null;
        }

        private static IReadOnlyList<Object> LoadProjectScriptableObjects()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_LizzoPV" });
            Array.Sort(guids, StringComparer.Ordinal);
            var assets = new List<Object>(guids.Length);
            var seen = new HashSet<Object>();

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int assetIndex = 0; assetIndex < loaded.Length; assetIndex++)
                {
                    Object asset = loaded[assetIndex];
                    if (asset is ScriptableObject && seen.Add(asset))
                    {
                        assets.Add(asset);
                    }
                }
            }

            return assets;
        }

        private static int CountReferences(string targetTypeName, int id, IReadOnlyList<Object> assets)
        {
            int count = 0;
            for (int assetIndex = 0; assetIndex < assets.Count; assetIndex++)
            {
                Object asset = assets[assetIndex];
                if (asset == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(asset);
                SerializedProperty iterator = serializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.Next(enterChildren))
                {
                    enterChildren = true;
                    if (iterator.type != targetTypeName)
                    {
                        continue;
                    }

                    SerializedProperty valueProperty = iterator.FindPropertyRelative("_value");
                    if (valueProperty != null && valueProperty.intValue == id)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static string GetTypedIdName(AssetCatalogKind kind)
        {
            return kind switch
            {
                AssetCatalogKind.Sprite => "SpriteAssetId",
                AssetCatalogKind.Audio => "AudioAssetId",
                AssetCatalogKind.Vfx => "VfxAssetId",
                AssetCatalogKind.Motion => "MotionAssetId",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        private static bool IsTypedAssetId(string typeName)
        {
            for (int index = 0; index < TypedIdNames.Length; index++)
            {
                if (typeName == TypedIdNames[index])
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct AssetPropertyTarget
        {
            public AssetPropertyTarget(Object asset, string propertyPath)
            {
                Asset = asset;
                PropertyPath = propertyPath;
            }

            public Object Asset { get; }
            public string PropertyPath { get; }
        }
    }
}
