using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    internal static class UnusedMonsterPhysicsCleanupAuthoring
    {
        private const string SelfPath =
            "Assets/_LizzoPV/Editor/UnusedMonsterPhysicsCleanupAuthoring.cs";

        private const string PhysicsRoot = "Assets/_LizzoPV/Physics";

        private const string MaterialPath =
            "Assets/_LizzoPV/Physics/MonsterPhysics.physicsMaterial2D";

        private const string ExpectedGuid = "3bb5c416fb5534c498c5b8380af74b01";

        public static void Apply()
        {
            try
            {
                ValidateRemovalTarget();

                if (!AssetDatabase.DeleteAsset(PhysicsRoot))
                {
                    throw new InvalidOperationException($"Failed to delete unused physics root: {PhysicsRoot}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                if (AssetDatabase.IsValidFolder(PhysicsRoot) ||
                    Directory.Exists(PhysicsRoot) ||
                    File.Exists(PhysicsRoot + ".meta") ||
                    AssetDatabase.LoadMainAssetAtPath(MaterialPath) != null ||
                    File.Exists(MaterialPath) ||
                    File.Exists(MaterialPath + ".meta"))
                {
                    throw new InvalidOperationException("Unused MonsterPhysics removal left target files behind.");
                }

                Debug.Log(
                    "[Unused MonsterPhysics Cleanup] PASS: removed the unreferenced Physics root, " +
                    "MonsterPhysics.physicsMaterial2D, and its meta.");

                if (!AssetDatabase.DeleteAsset(SelfPath))
                {
                    throw new InvalidOperationException($"Failed to remove temporary authoring helper: {SelfPath}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void ValidateRemovalTarget()
        {
            if (!AssetDatabase.IsValidFolder(PhysicsRoot) ||
                !Directory.Exists(PhysicsRoot) ||
                !File.Exists(PhysicsRoot + ".meta"))
            {
                throw new InvalidOperationException($"Missing Physics root or folder meta: {PhysicsRoot}");
            }

            string[] files = Directory.GetFiles(PhysicsRoot, "*", SearchOption.AllDirectories);
            if (files.Length != 2)
            {
                throw new InvalidOperationException(
                    $"Expected only MonsterPhysics and its meta under {PhysicsRoot}; found {files.Length} files.");
            }

            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
            if (material == null ||
                !string.Equals(material.name, "MonsterPhysics", StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.AssetPathToGUID(MaterialPath), ExpectedGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("MonsterPhysics identity or GUID differs from the approved target.");
            }

            if (!Mathf.Approximately(material.friction, 0.4f) ||
                !Mathf.Approximately(material.bounciness, 0.33f))
            {
                throw new InvalidOperationException(
                    $"MonsterPhysics values changed before removal. " +
                    $"Friction: {material.friction}, bounciness: {material.bounciness}.");
            }
        }
    }
}
