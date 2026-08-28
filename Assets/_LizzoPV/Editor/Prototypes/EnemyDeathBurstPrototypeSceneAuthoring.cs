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
        private const string SmokeBurstPath = "Assets/Retro Arsenal/Prefabs/Environment/Smoke/Grey/SmokeRadialGrey.prefab";
        private const string ArrivalBurstPath = "Assets/Retro Arsenal/Prefabs/Environment/Sparkle/Mini/SparkleMiniGreenBurst.prefab";
        private const string SmallGoblinPath = "Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units/SmallGoblin.prefab";
        private const string CommanderPath = "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab";
        private const string ExpGemPath = "Assets/_LizzoPV/Gameplay/Combat/Prefabs/Pickups/Visuals/Retro_XpGem_RupeeGreen.prefab";

        [MenuItem("Lizzo/Prototype/Rebuild Enemy Death Burst Scene")]
        public static void RebuildScene()
        {
            EnsureEditorIsSafeForAuthoring();

            GameObject skullBurstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkullBurstPath);
            GameObject smokeBurstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokeBurstPath);
            GameObject arrivalBurstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrivalBurstPath);
            GameObject smallGoblinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallGoblinPath);
            GameObject commanderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommanderPath);
            GameObject expGemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExpGemPath);
            if (skullBurstPrefab == null)
                throw new InvalidOperationException($"Missing required VFX donor: {SkullBurstPath}");
            if (smokeBurstPrefab == null)
                throw new InvalidOperationException($"Missing required VFX donor: {SmokeBurstPath}");
            if (arrivalBurstPrefab == null)
                throw new InvalidOperationException($"Missing required VFX donor: {ArrivalBurstPath}");
            if (smallGoblinPrefab == null || commanderPrefab == null || expGemPrefab == null)
                throw new InvalidOperationException("A required prototype reference prefab is missing.");

            SpriteReferenceData sourceRenderer = LoadSpriteReference(SmallGoblinPath);
            SpriteReferenceData commanderSourceRenderer = LoadSpriteReference(CommanderPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "EnemyDeathBurstPrototype";

            CreateCamera();

            GameObject root = new GameObject("PROTOTYPE_EnemyDeathBurst");
            SpriteRenderer targetRenderer = CreateSpriteReference(
                root.transform,
                "PrototypeTarget_SmallGoblin",
                sourceRenderer,
                new Vector3(0.0f, 0.85f, 0.0f),
                0);

            SpriteRenderer commanderRenderer = CreateSpriteReference(
                root.transform,
                "PrototypeTarget_Commander",
                commanderSourceRenderer,
                new Vector3(0.0f, -1.3f, 0.0f),
                0);
            GameObject commanderRoot = commanderRenderer.transform.parent.gameObject;
            CircleCollider2D absorbCollider = commanderRoot.AddComponent<CircleCollider2D>();
            absorbCollider.isTrigger = true;
            absorbCollider.offset = commanderSourceRenderer.OpaqueCenter;
            absorbCollider.radius = 0.72f;
            GameObject soulVisual = PrefabUtility.InstantiatePrefab(expGemPrefab) as GameObject;
            if (soulVisual == null)
                throw new InvalidOperationException($"Failed to instantiate EXP visual: {ExpGemPath}");
            soulVisual.name = "PrototypeSoul_ExpGem";
            soulVisual.transform.SetParent(root.transform, false);
            soulVisual.transform.position = targetRenderer.bounds.center;
            soulVisual.transform.localScale = Vector3.one * 0.44f;
            soulVisual.SetActive(false);

            EnemyDeathBurstPrototypeController controller = root.AddComponent<EnemyDeathBurstPrototypeController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_burstPrefab").objectReferenceValue = skullBurstPrefab;
            serializedController.FindProperty("_smokePrefab").objectReferenceValue = smokeBurstPrefab;
            serializedController.FindProperty("_arrivalPrefab").objectReferenceValue = arrivalBurstPrefab;
            serializedController.FindProperty("_targetRenderer").objectReferenceValue = targetRenderer;
            serializedController.FindProperty("_commanderRenderer").objectReferenceValue = commanderRenderer;
            serializedController.FindProperty("_absorbCollider").objectReferenceValue = absorbCollider;
            serializedController.FindProperty("_soulVisual").objectReferenceValue = soulVisual;
            serializedController.FindProperty("_initialDelaySeconds").floatValue = 0.8f;
            serializedController.FindProperty("_repeatIntervalSeconds").floatValue = 2.4f;
            serializedController.FindProperty("_targetHiddenSeconds").floatValue = 1.1f;
            serializedController.FindProperty("_burstScale").floatValue = 0.72f;
            serializedController.FindProperty("_smokeScale").floatValue = 0.36f;
            serializedController.FindProperty("_popDurationSeconds").floatValue = 0.1f;
            serializedController.FindProperty("_soulTravelSeconds").floatValue = 0.72f;
            serializedController.FindProperty("_soulCount").intValue = 24;
            serializedController.FindProperty("_soulStaggerSeconds").floatValue = 0.018f;
            serializedController.FindProperty("_soulOriginSpread").vector2Value = new Vector2(1.0f, 0.62f);
            serializedController.FindProperty("_soulCurveOffset").vector3Value = new Vector3(0.52f, 0.34f, 0.0f);
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
            Debug.Log($"[EnemyDeathBurstPrototype] PASS scene={ScenePath} flow=death_burst_to_commander_xp target={SmallGoblinPath}");
        }

        [MenuItem("Lizzo/Prototype/Rebuild Enemy XP Soul Scene")]
        public static void RebuildXpSoulScene()
        {
            RebuildScene();
        }

        private static SpriteRenderer CreateSpriteReference(
            Transform parent,
            string name,
            SpriteReferenceData source,
            Vector3 localPosition,
            int sortingOrder)
        {
            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(target.transform, false);
            visual.transform.localPosition = source.LocalPosition;
            visual.transform.localRotation = source.LocalRotation;
            visual.transform.localScale = source.Scale;
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            ApplySpriteReference(renderer, source, sortingOrder);
            return renderer;
        }

        private static SpriteReferenceData LoadSpriteReference(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
                SpriteRenderer best = null;
                float bestArea = -1.0f;
                for (int i = 0; i < renderers.Length; i++)
                {
                    SpriteRenderer candidate = renderers[i];
                    if (candidate == null || candidate.sprite == null)
                        continue;
                    Vector2 size = candidate.sprite.bounds.size;
                    float area = size.x * size.y;
                    if (area <= bestArea)
                        continue;
                    best = candidate;
                    bestArea = area;
                }

                if (best == null)
                    throw new InvalidOperationException($"Prefab has no readable SpriteRenderer source: {path}");
                return new SpriteReferenceData(
                    best.sprite,
                    best.sharedMaterial,
                    best.color,
                    best.flipX,
                    best.flipY,
                    best.sortingLayerID,
                    root.transform.InverseTransformPoint(best.transform.position),
                    Quaternion.Inverse(root.transform.rotation) * best.transform.rotation,
                    best.transform.lossyScale,
                    CalculateOpaqueCenter(root.transform, best));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Vector3 CalculateOpaqueCenter(Transform prefabRoot, SpriteRenderer renderer)
        {
            Sprite sprite = renderer.sprite;
            Rect textureRect = sprite.textureRect;
            int width = Mathf.Max(1, Mathf.RoundToInt(textureRect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(textureRect.height));
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                sprite.texture.width,
                sprite.texture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            RenderTexture previous = RenderTexture.active;
            Texture2D readable = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            try
            {
                Graphics.Blit(sprite.texture, renderTexture);
                RenderTexture.active = renderTexture;
                readable.ReadPixels(
                    new Rect(textureRect.x, textureRect.y, textureRect.width, textureRect.height),
                    0,
                    0,
                    false);
                readable.Apply(false, false);

                Color32[] pixels = readable.GetPixels32();
                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[row + x].a <= 12)
                            continue;
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }

                if (maxX < minX || maxY < minY)
                    return prefabRoot.InverseTransformPoint(renderer.bounds.center);

                float centerX = sprite.textureRectOffset.x + (minX + maxX + 1) * 0.5f;
                float centerY = sprite.textureRectOffset.y + (minY + maxY + 1) * 0.5f;
                Vector3 spriteLocal = new Vector3(
                    (centerX - sprite.pivot.x) / sprite.pixelsPerUnit,
                    (centerY - sprite.pivot.y) / sprite.pixelsPerUnit,
                    0.0f);
                if (renderer.flipX)
                    spriteLocal.x = -spriteLocal.x;
                if (renderer.flipY)
                    spriteLocal.y = -spriteLocal.y;
                return prefabRoot.InverseTransformPoint(renderer.transform.TransformPoint(spriteLocal));
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        private static void ApplySpriteReference(
            SpriteRenderer renderer,
            SpriteReferenceData source,
            int sortingOrder)
        {
            renderer.sprite = source.Sprite;
            renderer.sharedMaterial = source.Material;
            renderer.color = source.Color;
            renderer.flipX = source.FlipX;
            renderer.flipY = source.FlipY;
            renderer.sortingLayerID = source.SortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }

        private readonly struct SpriteReferenceData
        {
            public SpriteReferenceData(
                Sprite sprite,
                Material material,
                Color color,
                bool flipX,
                bool flipY,
                int sortingLayerId,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 scale,
                Vector3 opaqueCenter)
            {
                Sprite = sprite;
                Material = material;
                Color = color;
                FlipX = flipX;
                FlipY = flipY;
                SortingLayerId = sortingLayerId;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                Scale = scale;
                OpaqueCenter = opaqueCenter;
            }

            public Sprite Sprite { get; }
            public Material Material { get; }
            public Color Color { get; }
            public bool FlipX { get; }
            public bool FlipY { get; }
            public int SortingLayerId { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 Scale { get; }
            public Vector3 OpaqueCenter { get; }
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
            camera.orthographicSize = 3.0f;
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
