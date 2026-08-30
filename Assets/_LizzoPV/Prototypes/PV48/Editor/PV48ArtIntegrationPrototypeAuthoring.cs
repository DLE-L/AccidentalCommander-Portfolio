using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Prototypes.PV48.Runtime;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Prototypes.PV48.Editor
{
    public static class PV48ArtIntegrationPrototypeAuthoring
    {
        private const string PrototypeRoot = "Assets/_LizzoPV/Prototypes/PV48";
        private const string ReportPath = "Artifacts/Tasks/pv-48-art-integration-prototype__01a05337/Reports/PV48ArtIntegrationValidation.json";
        private const string SharedCompanionControllerPath =
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared/CompanionSpriteShared.controller";
        private const string CommanderControllerPath =
            "Assets/_LizzoPV/Gameplay/Commander/Animations/Compatibility/Commander.controller";
        private const string MapPrefabPath = "Assets/_LizzoPV/Gameplay/World/Prefabs/@Map.prefab";
        private const string ArenaGroundPrefabPath = "Assets/_LizzoPV/Gameplay/World/Prefabs/ArenaGround.prefab";
        private static readonly string[] Categories = { "Idle" };
        private static readonly string[] RetiredAnimationClipPaths =
        {
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared/CompanionSpriteShared_Run.anim",
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared/CompanionSpriteShared_Attack.anim",
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared/CompanionSpriteShared_Death.anim",
            "Assets/_LizzoPV/Gameplay/Commander/Animations/Compatibility/Commander_Run.anim",
            "Assets/_LizzoPV/Gameplay/Commander/Animations/Compatibility/Commander_Attack.anim",
            "Assets/_LizzoPV/Gameplay/Commander/Animations/Compatibility/Commander_Death.anim",
        };

        private static readonly Definition[] Definitions =
        {
            new(
                "Commander",
                PrototypeRoot + "/Art/Commander.png",
                PrototypeRoot + "/Art/Commander_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab",
                "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Bonus/Character/Thief/SpriteSheet.png",
                "B7428CA20F98EB468CDA3E88AB479BFFC5A45A61110227DFDACA1CF634691A63",
                112.0f,
                new Vector2(66.5f / 128.0f, 3.0f / 128.0f),
                0.5f,
                1.0f,
                119,
                17),
            new(
                "SwordSoldier",
                PrototypeRoot + "/Art/SwordSoldier.png",
                PrototypeRoot + "/Art/SwordSoldier_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/sword_soldier_SpriteSheet.png",
                "2FD0C9F80FFCC048B4C42190BBC006146F4B658788076E464BA6F281A4C2BD3F",
                110.0f * 16.0f / 18.0f,
                new Vector2(67.5f / 128.0f, 4.0f / 128.0f),
                0.6857143f,
                0.6f,
                110,
                18),
            new(
                "FalconArcher",
                PrototypeRoot + "/Art/FalconArcher.png",
                PrototypeRoot + "/Art/FalconArcher_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/falcon_archer_SpriteSheet.png",
                "FF3C7625ECF437AF0D28F4A3FC5276EC8E17CD6358F538061FDC13B8537AADA4",
                110.0f * 16.0f / 17.0f,
                new Vector2(67.5f / 128.0f, 4.0f / 128.0f),
                0.6857143f,
                0.6f,
                110,
                17),
            new(
                "Cleric",
                PrototypeRoot + "/Art/Cleric.png",
                PrototypeRoot + "/Art/Cleric_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/cleric_SpriteSheet.png",
                "915743C3792B8BEF63B813D840972946A02C0A741C368B1922BE1BE6F7351DB9",
                108.0f * 16.0f / 17.0f,
                new Vector2(67.0f / 128.0f, 4.0f / 128.0f),
                0.6857143f,
                0.6f,
                108,
                17),
        };

        private static readonly AdditionalBinding[] AdditionalBindings =
        {
            new(
                "SwordSoldierLegacyRuntime",
                PrototypeRoot + "/Art/SwordSoldier.png",
                PrototypeRoot + "/Art/SwordSoldier_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Companions/SwordSoldier.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/sword_soldier_SpriteSheet.png",
                0.44f,
                0.6f,
                110,
                18,
                110.0f * 16.0f / 18.0f),
            new(
                "FalconArcherLegacyRuntime",
                PrototypeRoot + "/Art/FalconArcher.png",
                PrototypeRoot + "/Art/FalconArcher_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Companions/FalconArcher.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/falcon_archer_SpriteSheet.png",
                0.44f,
                0.6f,
                110,
                17,
                110.0f * 16.0f / 17.0f),
            new(
                "ClericLegacyRuntime",
                PrototypeRoot + "/Art/Cleric.png",
                PrototypeRoot + "/Art/Cleric_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Companions/Cleric.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/cleric_SpriteSheet.png",
                0.44f,
                0.6f,
                108,
                17,
                108.0f * 16.0f / 17.0f),
        };

        private static readonly ScaleBinding[] FixedScaleBindings =
        {
            new("ShieldGuard", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardBaseMemberView.prefab", 0.6857143f, 0.6f),
            new("Bombardier", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierBaseMemberView.prefab", 0.6857143f, 0.6f),
            new("FireMage", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageBaseMemberView.prefab", 0.6857143f, 0.6f),
            new("SkeletonBomber", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonBomberBaseMemberView.prefab", 0.6857143f, 0.6f),
            new("WolfTamer", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerBaseMemberView.prefab", 0.6857143f, 0.6f),
            new("ShieldGuardPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardPromotedMemberView.prefab", 0.8571429f, 0.8f),
            new("SwordCaptain", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordCaptainMemberView.prefab", 0.8571429f, 0.8f),
            new("ClericPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericPromotedMemberView.prefab", 0.8571429f, 0.8f),
            new("BombardierPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierPromotedMemberView.prefab", 0.8571429f, 0.8f),
            new("FireMagePromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMagePromotedMemberView.prefab", 0.8571429f, 0.8f),
            new("FalconArcherPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherPromotedMemberView.prefab", 0.8571429f, 0.8f),
            new("SkeletonBomberPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonBomberPromotedMemberView.prefab", 0.8571429f, 0.8f),
            new("WolfTamerPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerPromotedMemberView.prefab", 0.8571429f, 0.8f),
        };

        [MenuItem("Lizzo/Prototypes/PV-48/Build Art Integration Prototype")]
        [CliCommand(
            "lizzo_pv48_build_art_integration",
            "Build and validate the bounded PV-48 character body integration prototype.",
            MainThreadRequired = true)]
        public static void Build()
        {
            EnsureDirectories();
            foreach (Definition definition in Definitions)
            {
                ValidateSourceHash(definition);
                ConfigureSpriteSheet(definition);
                CreateOrUpdateSpriteLibrary(definition);
            }

            foreach (Definition definition in Definitions)
                BindPrefab(definition);
            foreach (AdditionalBinding binding in AdditionalBindings)
                BindPrefab(binding);
            foreach (ScaleBinding binding in FixedScaleBindings)
                BindFixedScale(binding);
            ApplyIdleOnlyAnimationContract();
            ConfigureCompactArena();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("PV48_ART_INTEGRATION_BUILD_PASS");
        }

        [MenuItem("Lizzo/Prototypes/PV-48/Validate Art Integration Prototype")]
        public static void Validate()
        {
            var results = new List<RoleValidation>(Definitions.Length);
            foreach (Definition definition in Definitions)
                results.Add(ValidateDefinition(definition));
            foreach (AdditionalBinding binding in AdditionalBindings)
                ValidateAdditionalBinding(binding);
            foreach (ScaleBinding binding in FixedScaleBindings)
                ValidateFixedScale(binding);

            var report = new ValidationReport
            {
                result = "PASS",
                utc = DateTime.UtcNow.ToString("O"),
                frameWidth = 128,
                frameHeight = 128,
                frameCount = 8,
                categories = Categories,
                roles = results.ToArray(),
            };

            string absoluteReportPath = ToAbsolutePath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteReportPath) ?? throw new InvalidOperationException("Report directory is missing."));
            File.WriteAllText(absoluteReportPath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            Debug.Log("PV48_ART_INTEGRATION_VALIDATE_PASS " + absoluteReportPath);
        }

        [CliCommand(
            "lizzo_pv48_launch_from_loading",
            "Open Loading and enter Play Mode without changing persistent player progress.",
            MainThreadRequired = true)]
        public static RuntimeFixtureResponse LaunchFromLoading()
        {
            if (EditorApplication.isPlaying)
                return RuntimeFixtureResponse.Current("Play Mode is already active.");

            EditorSceneManager.OpenScene(GameFlowRoutes.LoadingScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
            return RuntimeFixtureResponse.Current("Opening Loading and entering Play Mode with current progress.");
        }

        [CliCommand(
            "lizzo_pv48_prepare_representative_battle",
            "Prepare Commander, SwordSoldier, FalconArcher, Cleric, and live enemies in the current Gameplay run.",
            MainThreadRequired = true)]
        public static RuntimeFixtureResponse PrepareRepresentativeBattle()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("PV-48 runtime fixture requires Play Mode.");

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == GameFlowRoutes.LobbyScenePath)
            {
                GameFlowRoutes.LoadGameplay();
                return RuntimeFixtureResponse.Current("Routing from Lobby to Gameplay.");
            }

            GameScene gameScene = UnityEngine.Object.FindFirstObjectByType<GameScene>();
            if (scene.path != GameFlowRoutes.GameplayScenePath || gameScene == null || !gameScene.IsRunLoaded)
                return RuntimeFixtureResponse.Current("Waiting for Gameplay run load.");

            PartyService party = gameScene.Services?.Party ?? throw new InvalidOperationException("Gameplay PartyService is unavailable.");
            EnsureRepresentativeRecruit(gameScene, party, "falcon_archer", CompanionKind.Archer);
            EnsureRepresentativeRecruit(gameScene, party, "cleric", CompanionKind.Cleric);
            gameScene.DebugSetCommanderHp(9999);
            if (gameScene.Services.Registry.EnemyResidualCount == 0)
            {
                gameScene.DebugSpawnEnemy(Define.GOBLIN_ID);
                gameScene.DebugSpawnEnemy(Define.GOBLIN_ID);
                gameScene.DebugSpawnEnemy(Define.GOBLIN_ID);
            }
            gameScene.DebugSetSpawnStopped(true);

            return RuntimeFixtureResponse.Current("Representative battle is ready.");
        }

        [CliCommand(
            "lizzo_pv48_inspect_runtime_visuals",
            "Report live UnitVisualDriver sprite and SpriteLibrary provenance in the current Scene.",
            MainThreadRequired = true)]
        public static RuntimeVisualInspection InspectRuntimeVisuals()
        {
            UnitVisualDriver[] drivers = UnityEngine.Object.FindObjectsByType<UnitVisualDriver>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var visuals = new RuntimeVisualEntry[drivers.Length];
            for (int index = 0; index < drivers.Length; index++)
            {
                UnitVisualDriver driver = drivers[index];
                SpriteRenderer renderer = driver.GetComponent<SpriteRenderer>();
                SpriteLibrary spriteLibrary = driver.GetComponent<SpriteLibrary>();
                string libraryPath = string.Empty;
                if (spriteLibrary)
                {
                    var serializedLibrary = new SerializedObject(spriteLibrary);
                    libraryPath = AssetDatabase.GetAssetPath(
                        serializedLibrary.FindProperty("m_SpriteLibraryAsset")?.objectReferenceValue);
                }

                visuals[index] = new RuntimeVisualEntry
                {
                    rootName = driver.transform.root.name,
                    objectName = driver.gameObject.name,
                    spriteName = renderer && renderer.sprite ? renderer.sprite.name : string.Empty,
                    spritePath = renderer && renderer.sprite ? AssetDatabase.GetAssetPath(renderer.sprite) : string.Empty,
                    libraryPath = libraryPath,
                    scaleX = driver.transform.localScale.x,
                    scaleY = driver.transform.localScale.y,
                };
            }

            return new RuntimeVisualInspection
            {
                scenePath = SceneManager.GetActiveScene().path,
                count = visuals.Length,
                visuals = visuals,
            };
        }

        private static void EnsureRepresentativeRecruit(GameScene gameScene, PartyService party, string baseUnitId, CompanionKind kind)
        {
            if (party.TryGetCompanionProgress(kind, out int ownedCount, out _) && ownedCount > 0)
                return;

            PartyRosterChangeResult preview = party.PreviewCanonicalRecruit(baseUnitId);
            if (preview == PartyRosterChangeResult.Reinforce ||
                preview == PartyRosterChangeResult.Promote ||
                preview == PartyRosterChangeResult.RejectedMaxed)
            {
                return;
            }
            if (preview != PartyRosterChangeResult.Recruit)
                throw new InvalidOperationException($"Cannot recruit {baseUnitId}: {preview}.");
            if (party.FreeCompanionSlots <= 0)
                throw new InvalidOperationException("No free companion slot remains for " + baseUnitId + ".");
            if (!gameScene.DebugRecruit(kind))
                throw new InvalidOperationException("Could not recruit " + baseUnitId + ".");
        }

        private static void EnsureDirectories()
        {
            EnsureAssetFolder("Assets/_LizzoPV/Prototypes");
            EnsureAssetFolder(PrototypeRoot);
            EnsureAssetFolder(PrototypeRoot + "/Art");
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Invalid asset folder: " + path);

            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void ValidateSourceHash(Definition definition)
        {
            string actual = ComputeSha256(ToAbsolutePath(definition.SheetPath));
            if (!string.Equals(actual, definition.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{definition.Role} source hash mismatch. Expected {definition.ExpectedSha256}, got {actual}.");
        }

        private static void ConfigureSpriteSheet(Definition definition)
        {
            AssetDatabase.ImportAsset(definition.SheetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(definition.SheetPath) as TextureImporter;
            if (!importer)
                throw new InvalidOperationException("TextureImporter is missing: " + definition.SheetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = definition.PixelsPerUnit;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = new SpriteRect[8];
            for (int frame = 0; frame < rects.Length; frame++)
            {
                rects[frame] = new SpriteRect
                {
                    name = "Frame_" + frame,
                    rect = new Rect(frame * 128, 0, 128, 128),
                    pivot = definition.Pivot,
                    alignment = SpriteAlignment.Custom,
                };
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void CreateOrUpdateSpriteLibrary(Definition definition)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(definition.SheetPath).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
            if (sprites.Length != 8)
                throw new InvalidOperationException($"{definition.Role} sprite count must be 8, got {sprites.Length}.");

            SpriteLibraryAsset library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(definition.LibraryPath);
            if (!library)
            {
                library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
                AssetDatabase.CreateAsset(library, definition.LibraryPath);
            }

            foreach (string category in Categories)
            {
                for (int frame = 0; frame < sprites.Length; frame++)
                {
                    Sprite sprite = sprites.Single(candidate => candidate.name == "Frame_" + frame);
                    library.AddCategoryLabel(sprite, category, frame.ToString());
                }
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private static void BindPrefab(Definition definition)
        {
            BindPrefab(
                definition.Role,
                definition.SheetPath,
                definition.LibraryPath,
                definition.PrefabPath,
                definition.OriginalSpritePath,
                definition.OriginalScale,
                definition.PrototypeScale);
        }

        private static void BindFixedScale(ScaleBinding binding)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(binding.PrefabPath);
            try
            {
                UnitVisualDriver[] drivers = root.GetComponentsInChildren<UnitVisualDriver>(true);
                if (drivers.Length != 1)
                    throw new InvalidOperationException($"{binding.Role} must have exactly one UnitVisualDriver, got {drivers.Length}.");

                Transform visual = drivers[0].transform;
                bool hasOriginalScale = Approximately(visual.localScale.x, binding.OriginalScale) &&
                    Approximately(visual.localScale.y, binding.OriginalScale);
                bool hasFixedScale = Approximately(visual.localScale.x, binding.FixedScale) &&
                    Approximately(visual.localScale.y, binding.FixedScale);
                if (!hasOriginalScale && !hasFixedScale)
                    throw new InvalidOperationException($"{binding.Role} Visual scale changed before authoring: {visual.localScale}.");

                visual.localScale = new Vector3(binding.FixedScale, binding.FixedScale, visual.localScale.z);
                EditorUtility.SetDirty(visual);
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                PrefabUtility.SaveAsPrefabAsset(root, binding.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BindPrefab(AdditionalBinding binding)
        {
            BindPrefab(
                binding.Role,
                binding.SheetPath,
                binding.LibraryPath,
                binding.PrefabPath,
                binding.OriginalSpritePath,
                binding.OriginalScale,
                binding.PrototypeScale);
        }

        private static void BindPrefab(
            string role,
            string sheetPath,
            string libraryPath,
            string prefabPath,
            string originalSpritePath,
            float originalScale,
            float prototypeScale)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                UnitVisualDriver[] drivers = root.GetComponentsInChildren<UnitVisualDriver>(true);
                if (drivers.Length != 1)
                    throw new InvalidOperationException($"{role} must have exactly one UnitVisualDriver, got {drivers.Length}.");

                UnitVisualDriver driver = drivers[0];
                GameObject visual = driver.gameObject;
                if (!string.Equals(visual.name, "Visual", StringComparison.Ordinal))
                    throw new InvalidOperationException($"{role} driver must remain on Visual, got {visual.name}.");
                bool hasOriginalScale = Approximately(visual.transform.localScale.x, originalScale) &&
                    Approximately(visual.transform.localScale.y, originalScale);
                bool hasPrototypeScale = Approximately(visual.transform.localScale.x, prototypeScale) &&
                    Approximately(visual.transform.localScale.y, prototypeScale);
                if (!hasOriginalScale && !hasPrototypeScale)
                {
                    throw new InvalidOperationException($"{role} Visual scale changed before authoring: {visual.transform.localScale}.");
                }

                var renderer = visual.GetComponent<SpriteRenderer>();
                var animator = visual.GetComponent<Animator>();
                if (!renderer || !animator || !animator.runtimeAnimatorController)
                    throw new InvalidOperationException(role + " Visual is missing its existing renderer or Animator contract.");

                string currentSpritePath = renderer.sprite ? AssetDatabase.GetAssetPath(renderer.sprite) : string.Empty;
                if (!string.Equals(currentSpritePath, originalSpritePath, StringComparison.Ordinal) &&
                    !string.Equals(currentSpritePath, sheetPath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"{role} current body art is unexpected: {currentSpritePath}.");
                }

                SpriteLibrary spriteLibrary = visual.GetComponent<SpriteLibrary>() ?? visual.AddComponent<SpriteLibrary>();
                SpriteResolver resolver = visual.GetComponent<SpriteResolver>() ?? visual.AddComponent<SpriteResolver>();
                SpriteLibraryAsset libraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
                SetObjectReference(spriteLibrary, "m_SpriteLibraryAsset", libraryAsset);

                if (string.Equals(role, "Commander", StringComparison.Ordinal))
                {
                    PV48CommanderVisualOverride visualOverride =
                        visual.GetComponent<PV48CommanderVisualOverride>() ?? visual.AddComponent<PV48CommanderVisualOverride>();
                    var overrideObject = new SerializedObject(visualOverride);
                    SetObjectReference(overrideObject, "_spriteRenderer", renderer);
                    SetObjectReference(overrideObject, "_spriteLibrary", libraryAsset);
                    SetObjectReference(overrideObject, "_visualDriver", driver);
                    overrideObject.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(visualOverride);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(visualOverride);
                }

                var driverObject = new SerializedObject(driver);
                SetObjectReference(driverObject, "_spriteRenderer", renderer);
                SetObjectReference(driverObject, "_animator", animator);
                SetObjectReference(driverObject, "_spriteResolver", resolver);
                SetString(driverObject, "_idleCategory", "Idle");
                SetInteger(driverObject, "_idleFrameCount", 8);
                driverObject.ApplyModifiedPropertiesWithoutUndo();

                resolver.SetCategoryAndLabel("Idle", "0");
                renderer.sprite = libraryAsset.GetSprite("Idle", "0");
                visual.transform.localScale = new Vector3(prototypeScale, prototypeScale, visual.transform.localScale.z);
                EditorUtility.SetDirty(spriteLibrary);
                EditorUtility.SetDirty(resolver);
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(driver);
                EditorUtility.SetDirty(visual.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(spriteLibrary);
                PrefabUtility.RecordPrefabInstancePropertyModifications(resolver);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(driver);
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyIdleOnlyAnimationContract()
        {
            KeepOnlyIdleState(SharedCompanionControllerPath);
            KeepOnlyIdleState(CommanderControllerPath);

            string[] libraryGuids = AssetDatabase.FindAssets(
                "t:SpriteLibraryAsset",
                new[]
                {
                    "Assets/_LizzoPV/Gameplay/Legion/Art/Characters",
                    PrototypeRoot + "/Art",
                });
            foreach (string guid in libraryGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SpriteLibraryAsset library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);
                if (library)
                    KeepOnlyIdleCategory(library);
            }

            AssetDatabase.SaveAssets();
            foreach (string clipPath in RetiredAnimationClipPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) && !AssetDatabase.DeleteAsset(clipPath))
                    throw new InvalidOperationException("Could not remove retired animation clip: " + clipPath);
            }
        }

        private static void KeepOnlyIdleState(string controllerPath)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller)
                throw new InvalidOperationException("AnimatorController is missing: " + controllerPath);

            AnimatorControllerLayer[] layers = controller.layers;
            if (layers.Length != 1)
                throw new InvalidOperationException("Idle-only controller must have exactly one layer: " + controllerPath);

            AnimatorStateMachine stateMachine = layers[0].stateMachine;
            ChildAnimatorState[] states = stateMachine.states;
            AnimatorState idle = states
                .Select(child => child.state)
                .SingleOrDefault(state => string.Equals(state.name, "Idle", StringComparison.Ordinal));
            if (!idle)
                throw new InvalidOperationException("Idle state is missing: " + controllerPath);

            foreach (ChildAnimatorState child in states)
            {
                if (child.state != idle)
                    stateMachine.RemoveState(child.state);
            }

            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
        }

        private static void KeepOnlyIdleCategory(SpriteLibraryAsset library)
        {
            string[] retiredCategories = library.GetCategoryNames()
                .Where(category => !string.Equals(category, "Idle", StringComparison.Ordinal))
                .ToArray();
            foreach (string category in retiredCategories)
            {
                string[] labels = library.GetCategoryLabelNames(category).ToArray();
                foreach (string label in labels)
                    library.RemoveCategoryLabel(category, label, true);
            }

            EditorUtility.SetDirty(library);
        }

        private static void ConfigureCompactArena()
        {
            Vector2 compactSize = Vector2.one * 35.0f;
            GameObject mapRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
            try
            {
                ArenaBounds arenaBounds = mapRoot.GetComponent<ArenaBounds>();
                if (!arenaBounds)
                    throw new InvalidOperationException("@Map prefab is missing ArenaBounds.");

                arenaBounds.Configure(compactSize);
                EditorUtility.SetDirty(arenaBounds);
                PrefabUtility.SaveAsPrefabAsset(mapRoot, MapPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(mapRoot);
            }

            GameObject groundRoot = PrefabUtility.LoadPrefabContents(ArenaGroundPrefabPath);
            try
            {
                Transform backgroundTransform = groundRoot.transform.Find("Background");
                SpriteRenderer background = backgroundTransform
                    ? backgroundTransform.GetComponent<SpriteRenderer>()
                    : null;
                if (!background)
                    throw new InvalidOperationException("ArenaGround prefab is missing Background SpriteRenderer.");

                background.size = compactSize;
                EditorUtility.SetDirty(background);
                PrefabUtility.SaveAsPrefabAsset(groundRoot, ArenaGroundPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(groundRoot);
            }
        }

        private static RoleValidation ValidateDefinition(Definition definition)
        {
            ValidateSourceHash(definition);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.SheetPath);
            if (!texture || texture.width != 1024 || texture.height != 128)
                throw new InvalidOperationException($"{definition.Role} sheet dimensions must be 1024x128.");

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(definition.SheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length != 8 || sprites.Any(sprite => sprite.rect.width != 128 || sprite.rect.height != 128))
                throw new InvalidOperationException($"{definition.Role} must contain eight 128x128 sprites.");

            SpriteLibraryAsset library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(definition.LibraryPath);
            if (!library)
                throw new InvalidOperationException(definition.Role + " SpriteLibraryAsset is missing.");
            foreach (string category in Categories)
            {
                string[] labels = library.GetCategoryLabelNames(category).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                if (!labels.SequenceEqual(Enumerable.Range(0, 8).Select(frame => frame.ToString())))
                    throw new InvalidOperationException($"{definition.Role} {category} labels are not exactly 0..7.");
                if (labels.Any(label => !library.GetSprite(category, label)))
                    throw new InvalidOperationException($"{definition.Role} {category} contains an empty sprite.");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(definition.PrefabPath);
            try
            {
                UnitVisualDriver[] drivers = root.GetComponentsInChildren<UnitVisualDriver>(true);
                if (drivers.Length != 1)
                    throw new InvalidOperationException($"{definition.Role} driver count changed: {drivers.Length}.");
                UnitVisualDriver driver = drivers[0];
                GameObject visual = driver.gameObject;
                var renderer = visual.GetComponent<SpriteRenderer>();
                var animator = visual.GetComponent<Animator>();
                var spriteLibrary = visual.GetComponent<SpriteLibrary>();
                var resolver = visual.GetComponent<SpriteResolver>();
                if (!renderer || !animator || !spriteLibrary || !resolver)
                    throw new InvalidOperationException(definition.Role + " visual binding is incomplete.");
                if (driver.IdleFrameCount != 8)
                    throw new InvalidOperationException(definition.Role + " Idle frame-count contract is not 8.");
                if (!Approximately(visual.transform.localScale.x, definition.PrototypeScale) ||
                    !Approximately(visual.transform.localScale.y, definition.PrototypeScale))
                {
                    throw new InvalidOperationException(definition.Role + " fixed Visual scale is incorrect.");
                }

                var spriteLibraryObject = new SerializedObject(spriteLibrary);
                if (spriteLibraryObject.FindProperty("m_SpriteLibraryAsset")?.objectReferenceValue != library)
                    throw new InvalidOperationException(definition.Role + " SpriteLibrary binding is incorrect.");
                if (renderer.sprite != library.GetSprite("Idle", "0"))
                    throw new InvalidOperationException(definition.Role + " initial body sprite is incorrect.");
                if (string.Equals(definition.Role, "Commander", StringComparison.Ordinal))
                {
                    PV48CommanderVisualOverride visualOverride = visual.GetComponent<PV48CommanderVisualOverride>();
                    if (!visualOverride || visualOverride.SpriteLibrary != library)
                        throw new InvalidOperationException("Commander final-render override binding is incomplete.");
                }

                float previousVisibleHeight = definition.TargetAlphaHeight / 16.0f * definition.OriginalScale;
                float prototypeVisibleHeight = definition.CandidateAlphaHeight / definition.PixelsPerUnit * definition.PrototypeScale;

                return new RoleValidation
                {
                    role = definition.Role,
                    sheet = definition.SheetPath,
                    prefab = definition.PrefabPath,
                    pixelsPerUnit = definition.PixelsPerUnit,
                    pivot = definition.Pivot,
                    visualScale = visual.transform.localScale.x,
                    previousVisibleHeight = previousVisibleHeight,
                    prototypeVisibleHeight = prototypeVisibleHeight,
                    categories = Categories.Length,
                    labelsPerCategory = 8,
                    animatorController = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),
                };
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateAdditionalBinding(AdditionalBinding binding)
        {
            SpriteLibraryAsset library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(binding.LibraryPath);
            if (!library)
                throw new InvalidOperationException(binding.Role + " SpriteLibraryAsset is missing.");

            GameObject root = PrefabUtility.LoadPrefabContents(binding.PrefabPath);
            try
            {
                UnitVisualDriver[] drivers = root.GetComponentsInChildren<UnitVisualDriver>(true);
                if (drivers.Length != 1)
                    throw new InvalidOperationException($"{binding.Role} driver count changed: {drivers.Length}.");
                UnitVisualDriver driver = drivers[0];
                GameObject visual = driver.gameObject;
                var renderer = visual.GetComponent<SpriteRenderer>();
                var animator = visual.GetComponent<Animator>();
                var spriteLibrary = visual.GetComponent<SpriteLibrary>();
                var resolver = visual.GetComponent<SpriteResolver>();
                if (!renderer || !animator || !spriteLibrary || !resolver)
                    throw new InvalidOperationException(binding.Role + " visual binding is incomplete.");
                if (driver.IdleFrameCount != 8)
                    throw new InvalidOperationException(binding.Role + " Idle frame-count contract is not 8.");
                if (!Approximately(visual.transform.localScale.x, binding.PrototypeScale) ||
                    !Approximately(visual.transform.localScale.y, binding.PrototypeScale))
                {
                    throw new InvalidOperationException(binding.Role + " prototype scale is incorrect.");
                }

                var spriteLibraryObject = new SerializedObject(spriteLibrary);
                if (spriteLibraryObject.FindProperty("m_SpriteLibraryAsset")?.objectReferenceValue != library)
                    throw new InvalidOperationException(binding.Role + " SpriteLibrary binding is incorrect.");
                if (renderer.sprite != library.GetSprite("Idle", "0"))
                    throw new InvalidOperationException(binding.Role + " initial body sprite is incorrect.");

            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateFixedScale(ScaleBinding binding)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(binding.PrefabPath);
            try
            {
                UnitVisualDriver[] drivers = root.GetComponentsInChildren<UnitVisualDriver>(true);
                if (drivers.Length != 1)
                    throw new InvalidOperationException($"{binding.Role} driver count changed: {drivers.Length}.");

                Vector3 scale = drivers[0].transform.localScale;
                if (!Approximately(scale.x, binding.FixedScale) || !Approximately(scale.y, binding.FixedScale))
                    throw new InvalidOperationException($"{binding.Role} fixed Visual scale is incorrect: {scale}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            SetObjectReference(serializedObject, propertyName, value);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName) ??
                throw new InvalidOperationException($"{serializedObject.targetObject.GetType().Name}.{propertyName} is missing.");
            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName) ??
                throw new InvalidOperationException($"{serializedObject.targetObject.GetType().Name}.{propertyName} is missing.");
            property.stringValue = value;
        }

        private static void SetInteger(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName) ??
                throw new InvalidOperationException($"{serializedObject.targetObject.GetType().Name}.{propertyName} is missing.");
            property.intValue = value;
        }

        private static bool Approximately(float left, float right) => Mathf.Abs(left - right) <= 0.0001f;

        private static string ComputeSha256(string path)
        {
            using var sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private sealed class Definition
        {
            public Definition(
                string role,
                string sheetPath,
                string libraryPath,
                string prefabPath,
                string originalSpritePath,
                string expectedSha256,
                float pixelsPerUnit,
                Vector2 pivot,
                float originalScale,
                float prototypeScale,
                int candidateAlphaHeight,
                int targetAlphaHeight)
            {
                Role = role;
                SheetPath = sheetPath;
                LibraryPath = libraryPath;
                PrefabPath = prefabPath;
                OriginalSpritePath = originalSpritePath;
                ExpectedSha256 = expectedSha256;
                PixelsPerUnit = pixelsPerUnit;
                Pivot = pivot;
                OriginalScale = originalScale;
                PrototypeScale = prototypeScale;
                CandidateAlphaHeight = candidateAlphaHeight;
                TargetAlphaHeight = targetAlphaHeight;
            }

            public string Role { get; }
            public string SheetPath { get; }
            public string LibraryPath { get; }
            public string PrefabPath { get; }
            public string OriginalSpritePath { get; }
            public string ExpectedSha256 { get; }
            public float PixelsPerUnit { get; }
            public Vector2 Pivot { get; }
            public float OriginalScale { get; }
            public float PrototypeScale { get; }
            public int CandidateAlphaHeight { get; }
            public int TargetAlphaHeight { get; }
        }

        private sealed class ScaleBinding
        {
            public ScaleBinding(string role, string prefabPath, float originalScale, float fixedScale)
            {
                Role = role;
                PrefabPath = prefabPath;
                OriginalScale = originalScale;
                FixedScale = fixedScale;
            }

            public string Role { get; }
            public string PrefabPath { get; }
            public float OriginalScale { get; }
            public float FixedScale { get; }
        }

        private sealed class AdditionalBinding
        {
            public AdditionalBinding(
                string role,
                string sheetPath,
                string libraryPath,
                string prefabPath,
                string originalSpritePath,
                float originalScale,
                float prototypeScale,
                int candidateAlphaHeight,
                int targetAlphaHeight,
                float pixelsPerUnit)
            {
                Role = role;
                SheetPath = sheetPath;
                LibraryPath = libraryPath;
                PrefabPath = prefabPath;
                OriginalSpritePath = originalSpritePath;
                OriginalScale = originalScale;
                PrototypeScale = prototypeScale;
                CandidateAlphaHeight = candidateAlphaHeight;
                TargetAlphaHeight = targetAlphaHeight;
                PixelsPerUnit = pixelsPerUnit;
            }

            public string Role { get; }
            public string SheetPath { get; }
            public string LibraryPath { get; }
            public string PrefabPath { get; }
            public string OriginalSpritePath { get; }
            public float OriginalScale { get; }
            public float PrototypeScale { get; }
            public int CandidateAlphaHeight { get; }
            public int TargetAlphaHeight { get; }
            public float PixelsPerUnit { get; }
        }

        [Serializable]
        private sealed class ValidationReport
        {
            public string result;
            public string utc;
            public int frameWidth;
            public int frameHeight;
            public int frameCount;
            public string[] categories;
            public RoleValidation[] roles;
        }

        [Serializable]
        private sealed class RoleValidation
        {
            public string role;
            public string sheet;
            public string prefab;
            public float pixelsPerUnit;
            public Vector2 pivot;
            public float visualScale;
            public float previousVisibleHeight;
            public float prototypeVisibleHeight;
            public int categories;
            public int labelsPerCategory;
            public string animatorController;
        }

        [Serializable]
        public sealed class RuntimeFixtureResponse
        {
            public bool playing;
            public string scenePath;
            public bool runLoaded;
            public int activeCompanionSlots;
            public int freeCompanionSlots;
            public int enemyCount;
            public float runSeconds;
            public string message;

            public static RuntimeFixtureResponse Current(string message)
            {
                GameScene gameScene = UnityEngine.Object.FindFirstObjectByType<GameScene>();
                PartyService party = gameScene?.Services?.Party;
                return new RuntimeFixtureResponse
                {
                    playing = EditorApplication.isPlaying,
                    scenePath = SceneManager.GetActiveScene().path,
                    runLoaded = gameScene != null && gameScene.IsRunLoaded,
                    activeCompanionSlots = party?.ActiveCompanionSlotCount ?? 0,
                    freeCompanionSlots = party?.FreeCompanionSlots ?? 0,
                    enemyCount = gameScene?.Services?.Registry?.EnemyResidualCount ?? 0,
                    runSeconds = gameScene?.TestRunElapsedSeconds ?? 0.0f,
                    message = message,
                };
            }
        }

        [Serializable]
        public sealed class RuntimeVisualInspection
        {
            public string scenePath;
            public int count;
            public RuntimeVisualEntry[] visuals;
        }

        [Serializable]
        public sealed class RuntimeVisualEntry
        {
            public string rootName;
            public string objectName;
            public string spriteName;
            public string spritePath;
            public string libraryPath;
            public float scaleX;
            public float scaleY;
        }
    }
}
