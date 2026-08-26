using System;
using System.IO;
using Lizzo.PV.Prototypes.Vfx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Editor.Prototypes
{
    public static class RecruitRevealEpicPrototypeSceneAuthoring
    {
        public const string ScenePath = "Assets/_LizzoPV/Prototypes/VFX/RecruitRevealEpic/RecruitRevealEpicPrototype.unity";
        public const string PrefabPath = "Assets/_LizzoPV/Prototypes/VFX/RecruitRevealEpic/RecruitRevealEpicPrototype.prefab";

        private const string DonorRoot = "Assets/_LizzoPV/Prototypes/VFX/RecruitRevealEpic/Donor";
        private const string CharacterPrefabPath = "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab";

        [MenuItem("Lizzo/Prototype/Rebuild Epic Recruit Reveal Scene")]
        public static void RebuildScene()
        {
            EnsureEditorIsSafeForAuthoring();
            ValidateDonors();

            SpriteReferenceData characterSource = LoadSpriteReference(CharacterPrefabPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RecruitRevealEpicPrototype";
            CreateCamera();

            GameObject prototypeRoot = BuildPrototypeRoot(characterSource);
            EnsureAssetDirectory(PrefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prototypeRoot, PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save prototype Prefab: {PrefabPath}");

            UnityEngine.Object.DestroyImmediate(prototypeRoot);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Failed to instantiate prototype Prefab: {PrefabPath}");
            instance.name = "PROTOTYPE_RecruitRevealEpic";

            EnsureAssetDirectory(ScenePath);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"Failed to save prototype Scene: {ScenePath}");

            AssetDatabase.SaveAssets();
            ValidateOpenScene(scene, instance);
            Selection.activeGameObject = instance;
            Debug.Log($"[RecruitRevealEpicPrototype] PASS scene={ScenePath} prefab={PrefabPath} character={CharacterPrefabPath} reveal=2.70s loop=4.2s");
        }

        private static GameObject BuildPrototypeRoot(SpriteReferenceData characterSource)
        {
            GameObject root = new GameObject("PROTOTYPE_RecruitRevealEpic");
            GameObject stage = CreateTransform(root.transform, "Visual_Stage", Vector3.zero);

            SpriteRenderer rays = CreateDonorRenderer(stage.transform, "Back_Rays", "VFX/FX_Rays.png", new Vector3(0.0f, -0.25f, 0.0f), 4.15f, 0);
            SpriteRenderer glow = CreateDonorRenderer(stage.transform, "Back_GlowCore", "VFX/FX_Glow_Core.png", new Vector3(0.0f, -0.35f, 0.0f), 3.6f, 1);
            SpriteRenderer magicCircle = CreateDonorRenderer(stage.transform, "Ground_MagicCircle", "VFX/FX_MagicCircle.png", new Vector3(0.0f, -0.45f, 0.0f), 2.75f, 3);
            Vector3 magicCircleScale = magicCircle.transform.localScale;
            magicCircle.transform.localScale = new Vector3(magicCircleScale.x, magicCircleScale.y * 0.38f, magicCircleScale.z);

            GameObject characterRoot = CreateTransform(stage.transform, "Character_RoyalSwordsman", new Vector3(0.0f, -1.52f, 0.0f));
            SpriteRenderer character = CreateCharacterRenderer(characterRoot.transform, characterSource, 8.4f, 12);

            GameObject chestRoot = CreateTransform(stage.transform, "Chest_Root", new Vector3(0.0f, -1.05f, 0.0f));
            SpriteRenderer chestClosed = CreateDonorRenderer(chestRoot.transform, "Chest_Closed", "Chest/Chest_Closed_Prototype.png", Vector3.zero, 3.35f, 22);
            SpriteRenderer chestBottom = CreateDonorRenderer(chestRoot.transform, "Chest_Bottom", "Chest/Chest_Bottom.png", Vector3.zero, 3.35f, 20);
            SpriteRenderer chestLid = CreateDonorRenderer(chestRoot.transform, "Chest_Lid", "Chest/Chest_Lid.png", Vector3.zero, 3.35f, 21);

            SpriteRenderer smoke = CreateDonorRenderer(stage.transform, "Burst_Smoke", "VFX/FX_Smoke.png", new Vector3(0.0f, -0.65f, 0.0f), 3.55f, 22);
            SpriteRenderer debris = CreateDonorRenderer(stage.transform, "Burst_Debris", "VFX/FX_Debris.png", new Vector3(0.0f, -0.4f, 0.0f), 3.85f, 23);
            SpriteRenderer explosion = CreateDonorRenderer(stage.transform, "Burst_Explosion", "VFX/FX_Explosion_Master.png", new Vector3(0.0f, -0.45f, 0.0f), 3.3f, 24);
            SpriteRenderer shockwave = CreateDonorRenderer(stage.transform, "Burst_Shockwave", "VFX/FX_Shockwave.png", new Vector3(0.0f, -0.65f, 0.0f), 3.15f, 25);
            SpriteRenderer shockwaveSecondary = CreateDonorRenderer(stage.transform, "Burst_ShockwaveSecondary", "VFX/FX_Shockwave.png", new Vector3(0.0f, -0.58f, 0.0f), 3.15f, 25);
            SpriteRenderer energy = CreateDonorRenderer(stage.transform, "Charge_EnergyStreak", "VFX/FX_EnergyStreak.png", new Vector3(0.0f, -0.35f, 0.0f), 3.2f, 26);
            SpriteRenderer landingDust = CreateDonorRenderer(stage.transform, "Landing_Dust", "VFX/FX_LandingDust.png", new Vector3(0.0f, -1.05f, 0.0f), 2.45f, 27);
            SpriteRenderer sparkLeft = CreateDonorRenderer(stage.transform, "Accent_SparkLeft", "VFX/FX_Spark.png", new Vector3(-1.05f, -0.95f, 0.0f), 0.75f, 28);
            SpriteRenderer sparkRight = CreateDonorRenderer(stage.transform, "Accent_SparkRight", "VFX/FX_Spark.png", new Vector3(1.0f, -0.82f, 0.0f), 0.72f, 28);
            SpriteRenderer starLeft = CreateDonorRenderer(stage.transform, "Accent_StarLeft", "VFX/FX_Star.png", new Vector3(-0.6f, -0.2f, 0.0f), 0.78f, 29);
            SpriteRenderer starRight = CreateDonorRenderer(stage.transform, "Accent_StarRight", "VFX/FX_Star.png", new Vector3(0.65f, -0.15f, 0.0f), 0.72f, 29);

            SpriteRenderer gradeBanner = CreateDonorRenderer(stage.transform, "Result_GradeBanner", "UI/UI_GradeBanner_Epic.png", new Vector3(0.0f, 2.78f, 0.0f), 4.0f, 40);
            SpriteRenderer nameBanner = CreateDonorRenderer(stage.transform, "Result_NameBanner", "UI/UI_NameBanner.png", new Vector3(0.0f, 2.05f, 0.0f), 3.65f, 40);
            SpriteRenderer flash = CreateDonorRenderer(stage.transform, "Foreground_Flash", "VFX/FX_Flash.png", new Vector3(0.0f, -0.15f, 0.0f), 4.65f, 60);

            SpriteRenderer[] initiallyHidden =
            {
                chestBottom,
                chestLid,
                character,
                glow,
                rays,
                magicCircle,
                energy,
                explosion,
                flash,
                shockwave,
                shockwaveSecondary,
                smoke,
                debris,
                landingDust,
                sparkLeft,
                sparkRight,
                starLeft,
                starRight,
                gradeBanner,
                nameBanner,
            };
            for (int i = 0; i < initiallyHidden.Length; i++)
                SetRendererAlpha(initiallyHidden[i], 0.0f);
            SetRendererAlpha(chestClosed, 1.0f);

            RecruitRevealEpicPrototypeController controller = root.AddComponent<RecruitRevealEpicPrototypeController>();
            SerializedObject serialized = new SerializedObject(controller);
            SetReference(serialized, "_stageRoot", stage.transform);
            SetReference(serialized, "_chestRoot", chestRoot.transform);
            SetReference(serialized, "_chestLid", chestLid.transform);
            SetReference(serialized, "_characterRoot", characterRoot.transform);
            SetReference(serialized, "_chestClosedRenderer", chestClosed);
            SetReference(serialized, "_chestBottomRenderer", chestBottom);
            SetReference(serialized, "_chestLidRenderer", chestLid);
            SetReference(serialized, "_characterRenderer", character);
            SetReference(serialized, "_glowRenderer", glow);
            SetReference(serialized, "_raysRenderer", rays);
            SetReference(serialized, "_magicCircleRenderer", magicCircle);
            SetReference(serialized, "_energyStreakRenderer", energy);
            SetReference(serialized, "_explosionRenderer", explosion);
            SetReference(serialized, "_flashRenderer", flash);
            SetReference(serialized, "_shockwaveRenderer", shockwave);
            SetReference(serialized, "_shockwaveSecondaryRenderer", shockwaveSecondary);
            SetReference(serialized, "_smokeRenderer", smoke);
            SetReference(serialized, "_debrisRenderer", debris);
            SetReference(serialized, "_landingDustRenderer", landingDust);
            SetReference(serialized, "_sparkLeftRenderer", sparkLeft);
            SetReference(serialized, "_sparkRightRenderer", sparkRight);
            SetReference(serialized, "_starLeftRenderer", starLeft);
            SetReference(serialized, "_starRightRenderer", starRight);
            SetReference(serialized, "_gradeBannerRenderer", gradeBanner);
            SetReference(serialized, "_nameBannerRenderer", nameBanner);
            serialized.FindProperty("_loopDurationSeconds").floatValue = 4.2f;
            serialized.FindProperty("_autoRepeat").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static SpriteRenderer CreateDonorRenderer(
            Transform parent,
            string name,
            string relativePath,
            Vector3 localPosition,
            float targetWidth,
            int sortingOrder)
        {
            string path = $"{DonorRoot}/{relativePath}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Missing imported Sprite donor: {path}");

            GameObject gameObject = CreateTransform(parent, name, localPosition);
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;
            float sourceWidth = Mathf.Max(0.001f, sprite.bounds.size.x);
            gameObject.transform.localScale = Vector3.one * (targetWidth / sourceWidth);
            return renderer;
        }

        private static SpriteRenderer CreateCharacterRenderer(
            Transform parent,
            SpriteReferenceData source,
            float targetHeight,
            int sortingOrder)
        {
            GameObject visual = CreateTransform(parent, "Visual", Vector3.zero);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = source.Sprite;
            renderer.sharedMaterial = source.Material;
            renderer.color = source.Color;
            renderer.flipX = source.FlipX;
            renderer.flipY = source.FlipY;
            renderer.sortingLayerID = source.SortingLayerId;
            renderer.sortingOrder = sortingOrder;
            float sourceHeight = Mathf.Max(0.001f, source.Sprite.bounds.size.y);
            visual.transform.localScale = Vector3.one * (targetHeight / sourceHeight);
            return renderer;
        }

        private static GameObject CreateTransform(Transform parent, string name, Vector3 localPosition)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            return gameObject;
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
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
                    throw new InvalidOperationException($"Prefab has no readable SpriteRenderer: {path}");
                return new SpriteReferenceData(
                    best.sprite,
                    best.sharedMaterial,
                    best.color,
                    best.flipX,
                    best.flipY,
                    best.sortingLayerID);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0.0f, 0.25f, -10.0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.012f, 0.075f, 1.0f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100.0f;
        }

        private static void ValidateDonors()
        {
            string[] requiredPaths =
            {
                $"{DonorRoot}/Chest/Chest_Bottom.png",
                $"{DonorRoot}/Chest/Chest_Lid.png",
                $"{DonorRoot}/Chest/Chest_Closed_Prototype.png",
                $"{DonorRoot}/VFX/FX_Debris.png",
                $"{DonorRoot}/VFX/FX_EnergyStreak.png",
                $"{DonorRoot}/VFX/FX_Explosion_Master.png",
                $"{DonorRoot}/VFX/FX_Flash.png",
                $"{DonorRoot}/VFX/FX_Glow_Core.png",
                $"{DonorRoot}/VFX/FX_LandingDust.png",
                $"{DonorRoot}/VFX/FX_MagicCircle.png",
                $"{DonorRoot}/VFX/FX_Rays.png",
                $"{DonorRoot}/VFX/FX_Shockwave.png",
                $"{DonorRoot}/VFX/FX_Smoke.png",
                $"{DonorRoot}/VFX/FX_Spark.png",
                $"{DonorRoot}/VFX/FX_Star.png",
                $"{DonorRoot}/UI/UI_GradeBanner_Epic.png",
                $"{DonorRoot}/UI/UI_NameBanner.png",
                CharacterPrefabPath,
            };

            for (int i = 0; i < requiredPaths.Length; i++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(requiredPaths[i]) == null)
                    throw new InvalidOperationException($"Missing required prototype donor: {requiredPaths[i]}");
            }
        }

        private static void ValidateOpenScene(Scene scene, GameObject instance)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException($"Prototype Scene path mismatch: {scene.path}");
            if (Camera.main == null || !Camera.main.orthographic)
                throw new InvalidOperationException("Prototype Scene is missing its orthographic Main Camera.");
            if (instance.GetComponent<RecruitRevealEpicPrototypeController>() == null)
                throw new InvalidOperationException("Prototype controller is missing from the Prefab instance.");
            if (instance.GetComponentsInChildren<SpriteRenderer>(true).Length < 21)
                throw new InvalidOperationException("Prototype visual hierarchy is incomplete.");
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException($"Could not resolve asset directory: {assetPath}");
            Directory.CreateDirectory(directory);
        }

        private static void EnsureEditorIsSafeForAuthoring()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode is active or changing; prototype Scene authoring stopped.");

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty && !string.IsNullOrEmpty(activeScene.path))
                throw new InvalidOperationException($"Active Scene has unsaved changes: {activeScene.path}");
        }

        private static void SetReference(SerializedObject serializedObject, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized field on prototype controller: {fieldName}");
            property.objectReferenceValue = value;
        }

        private readonly struct SpriteReferenceData
        {
            public SpriteReferenceData(Sprite sprite, Material material, Color color, bool flipX, bool flipY, int sortingLayerId)
            {
                Sprite = sprite;
                Material = material;
                Color = color;
                FlipX = flipX;
                FlipY = flipY;
                SortingLayerId = sortingLayerId;
            }

            public Sprite Sprite { get; }
            public Material Material { get; }
            public Color Color { get; }
            public bool FlipX { get; }
            public bool FlipY { get; }
            public int SortingLayerId { get; }
        }
    }
}
