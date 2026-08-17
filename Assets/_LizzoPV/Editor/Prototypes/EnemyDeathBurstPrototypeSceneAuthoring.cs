using System;
using System.IO;
using Lizzo.PV.Prototypes.Vfx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Editor.Prototypes
{
    public static class EnemyDeathBurstPrototypeSceneAuthoring
    {
        public const string ScenePath = "Assets/_LizzoPV/Prototypes/VFX/EnemyDeathBurst/EnemyDeathBurstPrototype.unity";

        private const string SkullBurstPath = "Assets/Retro Arsenal/Prefabs/Interactive/Symbols/Skull/SkullBurst.prefab";
        private const string SmallGoblinPath = "Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units/SmallGoblin.prefab";

        [MenuItem("Lizzo/Prototype/Rebuild Enemy Death Burst Scene")]
        public static void RebuildScene()
        {
            EnsureEditorIsSafeForAuthoring();

            GameObject skullBurstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkullBurstPath);
            GameObject smallGoblinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallGoblinPath);
            if (skullBurstPrefab == null)
                throw new InvalidOperationException($"Missing required VFX donor: {SkullBurstPath}");
            if (smallGoblinPrefab == null)
                throw new InvalidOperationException($"Missing required target reference: {SmallGoblinPath}");

            SpriteRenderer sourceRenderer = smallGoblinPrefab.GetComponentInChildren<SpriteRenderer>(true);
            if (sourceRenderer == null || sourceRenderer.sprite == null)
                throw new InvalidOperationException($"SmallGoblin has no readable SpriteRenderer source: {SmallGoblinPath}");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "EnemyDeathBurstPrototype";

            CreateCamera();

            GameObject root = new GameObject("PROTOTYPE_EnemyDeathBurst");
            GameObject target = new GameObject("PrototypeTarget_SmallGoblin");
            target.transform.SetParent(root.transform, false);
            target.transform.localPosition = Vector3.zero;
            target.transform.localScale = sourceRenderer.transform.lossyScale;

            SpriteRenderer targetRenderer = target.AddComponent<SpriteRenderer>();
            targetRenderer.sprite = sourceRenderer.sprite;
            targetRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            targetRenderer.color = sourceRenderer.color;
            targetRenderer.flipX = sourceRenderer.flipX;
            targetRenderer.flipY = sourceRenderer.flipY;
            targetRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            targetRenderer.sortingOrder = 0;

            EnemyDeathBurstPrototypeController controller = root.AddComponent<EnemyDeathBurstPrototypeController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_burstPrefab").objectReferenceValue = skullBurstPrefab;
            serializedController.FindProperty("_targetRenderer").objectReferenceValue = targetRenderer;
            serializedController.FindProperty("_initialDelaySeconds").floatValue = 0.8f;
            serializedController.FindProperty("_repeatIntervalSeconds").floatValue = 2.4f;
            serializedController.FindProperty("_targetHiddenSeconds").floatValue = 1.1f;
            serializedController.FindProperty("_burstScale").floatValue = 1.0f;
            serializedController.FindProperty("_burstOffset").vector3Value = Vector3.zero;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            string directory = Path.GetDirectoryName(ScenePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException($"Could not resolve Scene directory: {ScenePath}");

            Directory.CreateDirectory(directory);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"Failed to save prototype Scene: {ScenePath}");

            AssetDatabase.SaveAssets();
            ValidateOpenScene(scene, skullBurstPrefab, targetRenderer, controller);
            Debug.Log($"[EnemyDeathBurstPrototype] PASS scene={ScenePath} donor={SkullBurstPath} target={SmallGoblinPath}");
        }

        private static void EnsureEditorIsSafeForAuthoring()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode is active or changing; prototype Scene authoring stopped.");

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty && !string.IsNullOrEmpty(activeScene.path))
                throw new InvalidOperationException($"Active Scene has unsaved changes: {activeScene.path}");
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0.0f, 0.5f, -10.0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 1.0f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100.0f;
        }

        private static void ValidateOpenScene(
            Scene scene,
            GameObject expectedDonor,
            SpriteRenderer expectedRenderer,
            EnemyDeathBurstPrototypeController expectedController)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException($"Prototype Scene path mismatch: {scene.path}");
            if (Camera.main == null || !Camera.main.orthographic)
                throw new InvalidOperationException("Prototype Scene is missing its orthographic Main Camera.");
            if (expectedRenderer == null || expectedRenderer.sprite == null)
                throw new InvalidOperationException("Prototype target SpriteRenderer was not authored correctly.");
            if (expectedController == null)
                throw new InvalidOperationException("Prototype controller was not authored correctly.");

            SerializedObject serializedController = new SerializedObject(expectedController);
            if (serializedController.FindProperty("_burstPrefab").objectReferenceValue != expectedDonor)
                throw new InvalidOperationException("Prototype controller donor reference mismatch.");
            if (serializedController.FindProperty("_targetRenderer").objectReferenceValue != expectedRenderer)
                throw new InvalidOperationException("Prototype controller target reference mismatch.");
        }
    }
}
