using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Prototypes.CommanderArtSwap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Editor.Prototypes
{
    public static class CommanderArtSwapStartHoldStopPrototypeAuthoring
    {
        public const string ScenePath = "Assets/_LizzoPV/Prototypes/Art/CommanderArtSwapStartHoldStop/CommanderArtSwapStartHoldStopPrototype.unity";

        private const string AssetRoot = "Assets/_LizzoPV/Prototypes/Art/CommanderArtSwapStartHoldStop";
        private const string SpriteRoot = AssetRoot + "/Sprites";
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private const string CommanderPrefabPath = "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab";
        private const string PackageRelativeRoot = "GenArt/Commander_ArtSwap_Package_v1";
        private const string ExpectedMoveManifestSha256 = "2177852054f2c310d7a6db9300e0d15b88c43919de555b5dbc3017e7b0b5673c";
        private const string ExpectedLockedStaticSha256 = "011fb406cd51d2b6d30f522f8ca6acaebf7e19fdfef593fdaab979d0df3ba6d1";
        private const float FeetPivotFromBottomPixels = 20.0f;

        [MenuItem("Lizzo/Prototype/Build Commander Art Swap Start-Hold-Stop Scene")]
        public static void BuildPrototypeScene()
        {
            EnsureEditorIsSafeForAuthoring();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string packageRoot = Path.Combine(projectRoot, PackageRelativeRoot);
            ValidateSourceHashes(packageRoot);

            SpriteReferenceData reference = LoadCommanderSpriteReference();
            CopyPrototypeSprites(packageRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigurePrototypeSpriteImports(reference);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException($"Prototype Scene already exists; refusing to overwrite owned evidence: {ScenePath}");

            EnsureAssetDirectory(ScenePath);
            if (!AssetDatabase.CopyAsset(GameplayScenePath, ScenePath))
                throw new InvalidOperationException($"Failed to copy Gameplay Scene to prototype path: {ScenePath}");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = new GameObject("PROTOTYPE_CommanderArtSwapStartHoldStop");
            CommanderArtSwapStartHoldStopPrototypeController controller = root.AddComponent<CommanderArtSwapStartHoldStopPrototypeController>();
            ConfigureController(controller, reference);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Failed to save prototype Scene: {ScenePath}");

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            Debug.Log(
                $"[CommanderArtSwapPrototypeAuthoring] PASS scene={ScenePath} sourceSprite={reference.Sprite.name} "
                + $"sourcePPU={reference.Sprite.pixelsPerUnit:F2} sourceVisibleHeight={reference.Alpha.VisibleHeightLocal:F5} "
                + $"sourceVisibleBottom={reference.Alpha.VisibleBottomLocal:F5} filter={reference.FilterMode}");
        }

        private static void ConfigureController(
            CommanderArtSwapStartHoldStopPrototypeController controller,
            SpriteReferenceData reference)
        {
            Sprite[] idle = LoadNumberedSprites(SpriteRoot + "/Idle/Commander_Idle_", 12);
            Sprite[] attack = LoadNumberedSprites(SpriteRoot + "/Attack/Commander_Attack_", 6);
            Sprite moveIdle = LoadSprite(SpriteRoot + "/Movement/Idle.png");
            Sprite[] moveStart =
            {
                LoadSprite(SpriteRoot + "/Movement/Start_01.png"),
                LoadSprite(SpriteRoot + "/Movement/Start_02.png"),
            };
            Sprite moveHold = LoadSprite(SpriteRoot + "/Movement/Hold.png");
            Sprite[] moveStop =
            {
                LoadSprite(SpriteRoot + "/Movement/Stop_01.png"),
                LoadSprite(SpriteRoot + "/Movement/Stop_02.png"),
            };
            Sprite moveSettled = LoadSprite(SpriteRoot + "/Movement/Settled.png");
            AlphaMetrics prototypeAlpha = ReadAlphaMetrics(
                ProjectAbsolutePath(SpriteRoot + "/Idle/Commander_Idle_00.png"),
                reference.Sprite.pixelsPerUnit,
                FeetPivotFromBottomPixels);

            SerializedObject serialized = new SerializedObject(controller);
            SetSpriteArray(serialized, "_idleSprites", idle);
            SetSpriteArray(serialized, "_attackSprites", attack);
            SetReference(serialized, "_moveIdleSprite", moveIdle);
            SetSpriteArray(serialized, "_moveStartSprites", moveStart);
            SetReference(serialized, "_moveHoldSprite", moveHold);
            SetSpriteArray(serialized, "_moveStopSprites", moveStop);
            SetReference(serialized, "_moveSettledSprite", moveSettled);
            serialized.FindProperty("_referenceVisibleHeightLocal").floatValue = reference.Alpha.VisibleHeightLocal;
            serialized.FindProperty("_referenceVisibleBottomLocal").floatValue = reference.Alpha.VisibleBottomLocal;
            serialized.FindProperty("_prototypeVisibleHeightLocal").floatValue = prototypeAlpha.VisibleHeightLocal;
            serialized.FindProperty("_prototypeVisibleBottomLocal").floatValue = prototypeAlpha.VisibleBottomLocal;
            serialized.FindProperty("_runAutomaticFixture").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpriteReferenceData LoadCommanderSpriteReference()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CommanderPrefabPath);
            try
            {
                CommanderAllyVisual role = root.GetComponentInChildren<CommanderAllyVisual>(true);
                UnitVisualDriver driver = role == null
                    ? root.GetComponentInChildren<UnitVisualDriver>(true)
                    : role.GetComponentInChildren<UnitVisualDriver>(true);
                SpriteRenderer renderer = driver == null ? null : driver.SpriteRenderer;
                if (renderer == null || renderer.sprite == null)
                    throw new InvalidOperationException($"Commander Prefab has no primary UnitVisualDriver sprite: {CommanderPrefabPath}");

                string texturePath = AssetDatabase.GetAssetPath(renderer.sprite.texture);
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException($"Could not read Commander source TextureImporter: {texturePath}");

                AlphaMetrics alpha = ReadSpriteAlphaMetrics(renderer.sprite);
                return new SpriteReferenceData(renderer.sprite, importer.filterMode, importer.textureCompression, alpha);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CopyPrototypeSprites(string packageRoot)
        {
            for (int i = 0; i < 12; i++)
            {
                string name = $"Commander_Idle_{i:00}.png";
                CopyOwnedPrototypeFile(Path.Combine(packageRoot, "Sprites", "Idle", name), SpriteRoot + "/Idle/" + name);
            }

            for (int i = 0; i < 6; i++)
            {
                string name = $"Commander_Attack_{i:00}.png";
                CopyOwnedPrototypeFile(Path.Combine(packageRoot, "Sprites", "Attack", name), SpriteRoot + "/Attack/" + name);
            }

            string movementSource = Path.Combine(packageRoot, "Prototypes", "Commander_MoveStartHoldStop_v001", "states");
            string[] movementNames = { "Idle.png", "Start_01.png", "Start_02.png", "Hold.png", "Stop_01.png", "Stop_02.png", "Settled.png" };
            for (int i = 0; i < movementNames.Length; i++)
                CopyOwnedPrototypeFile(Path.Combine(movementSource, movementNames[i]), SpriteRoot + "/Movement/" + movementNames[i]);
        }

        private static void CopyOwnedPrototypeFile(string sourceAbsolutePath, string destinationAssetPath)
        {
            if (!File.Exists(sourceAbsolutePath))
                throw new FileNotFoundException("Missing Commander prototype source.", sourceAbsolutePath);
            string destinationAbsolutePath = ProjectAbsolutePath(destinationAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationAbsolutePath) ?? throw new InvalidOperationException(destinationAssetPath));
            if (File.Exists(destinationAbsolutePath))
                throw new InvalidOperationException($"Prototype sprite already exists; refusing to overwrite: {destinationAssetPath}");
            File.Copy(sourceAbsolutePath, destinationAbsolutePath, false);
        }

        private static void ConfigurePrototypeSpriteImports(SpriteReferenceData reference)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteRoot });
            if (guids.Length != 25)
                throw new InvalidOperationException($"Expected 25 prototype textures, found {guids.Length} under {SpriteRoot}");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException($"Missing TextureImporter: {path}");

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = reference.Sprite.pixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.filterMode = reference.FilterMode;
                importer.textureCompression = reference.TextureCompression;
                importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 512);
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, FeetPivotFromBottomPixels / 512.0f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static AlphaMetrics ReadSpriteAlphaMetrics(Sprite sprite)
        {
            string texturePath = AssetDatabase.GetAssetPath(sprite.texture);
            string absolutePath = ProjectAbsolutePath(texturePath);
            byte[] bytes = File.ReadAllBytes(absolutePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false))
                    throw new InvalidOperationException($"Could not decode sprite texture: {texturePath}");
                Rect rect = sprite.rect;
                return ReadAlphaMetrics(texture, rect, sprite.pixelsPerUnit, sprite.pivot.y);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static AlphaMetrics ReadAlphaMetrics(string absolutePath, float pixelsPerUnit, float pivotFromBottomPixels)
        {
            byte[] bytes = File.ReadAllBytes(absolutePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false))
                    throw new InvalidOperationException($"Could not decode prototype texture: {absolutePath}");
                return ReadAlphaMetrics(
                    texture,
                    new Rect(0.0f, 0.0f, texture.width, texture.height),
                    pixelsPerUnit,
                    pivotFromBottomPixels);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static AlphaMetrics ReadAlphaMetrics(Texture2D texture, Rect rect, float pixelsPerUnit, float pivotY)
        {
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            int startX = Mathf.RoundToInt(rect.x);
            int startY = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (texture.GetPixel(startX + x, startY + y).a <= 0.01f)
                        continue;
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (minY == int.MaxValue || maxY == int.MinValue)
                throw new InvalidOperationException("Sprite alpha bounds are empty.");

            return new AlphaMetrics(
                (maxY - minY + 1) / pixelsPerUnit,
                (minY - pivotY) / pixelsPerUnit);
        }

        private static Sprite[] LoadNumberedSprites(string prefix, int count)
        {
            Sprite[] sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
                sprites[i] = LoadSprite($"{prefix}{i:00}.png");
            return sprites;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Missing imported prototype Sprite: {path}");
            return sprite;
        }

        private static void ValidateSourceHashes(string packageRoot)
        {
            string moveManifest = Path.Combine(packageRoot, "Prototypes", "Commander_MoveStartHoldStop_v001", "prototype-manifest.json");
            string lockedStatic = Path.Combine(packageRoot, "Sprites", "Idle", "Commander_Idle_00.png");
            ValidateSha256(moveManifest, ExpectedMoveManifestSha256);
            ValidateSha256(lockedStatic, ExpectedLockedStaticSha256);
        }

        private static void ValidateSha256(string path, string expected)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Required Commander prototype source is missing.", path);
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException($"Source hash mismatch: {path} expected={expected} actual={actual}");
        }

        private static void SetSpriteArray(SerializedObject serializedObject, string fieldName, IReadOnlyList<Sprite> sprites)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized field: {fieldName}");
            property.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static void SetReference(SerializedObject serializedObject, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized field: {fieldName}");
            property.objectReferenceValue = value;
        }

        private static string ProjectAbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            string directory = Path.GetDirectoryName(ProjectAbsolutePath(assetPath));
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException($"Could not resolve asset directory: {assetPath}");
            Directory.CreateDirectory(directory);
        }

        private static void EnsureEditorIsSafeForAuthoring()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode is active or changing; prototype authoring stopped.");
            if (EditorApplication.isCompiling)
                throw new InvalidOperationException("Compilation is active; prototype authoring stopped.");

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
                throw new InvalidOperationException($"Active Scene has unsaved changes: {activeScene.path}");
        }

        private readonly struct SpriteReferenceData
        {
            public SpriteReferenceData(
                Sprite sprite,
                FilterMode filterMode,
                TextureImporterCompression textureCompression,
                AlphaMetrics alpha)
            {
                Sprite = sprite;
                FilterMode = filterMode;
                TextureCompression = textureCompression;
                Alpha = alpha;
            }

            public Sprite Sprite { get; }
            public FilterMode FilterMode { get; }
            public TextureImporterCompression TextureCompression { get; }
            public AlphaMetrics Alpha { get; }
        }

        private readonly struct AlphaMetrics
        {
            public AlphaMetrics(float visibleHeightLocal, float visibleBottomLocal)
            {
                VisibleHeightLocal = visibleHeightLocal;
                VisibleBottomLocal = visibleBottomLocal;
            }

            public float VisibleHeightLocal { get; }
            public float VisibleBottomLocal { get; }
        }
    }
}
