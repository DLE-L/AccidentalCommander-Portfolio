using System;
using System.IO;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.P0.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Editor.CompanionRuntime
{
    public static class CompanionPresentationTracerAuthoring
    {
        public const string BaseMemberPrefabPath =
            "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab";
        public const string PromotedMemberPrefabPath =
            "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordCaptainMemberView.prefab";
        public const string SquadRootPrefabPath =
            "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSquadRoot.prefab";

        private const string BaseLibraryPath =
            "Assets/_LizzoPV/Art/Characters/Companions/sword_soldier_SpriteLibrary.asset";
        private const string PromotedLibraryPath =
            "Assets/_LizzoPV/Art/Characters/Companions/sword_captain_SpriteLibrary.asset";
        private const string SharedControllerPath =
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared/CompanionSpriteShared.controller";
        private const float BaseVisualScale = 0.44f;
        private const float PromotedVisualScale = 0.55f;
        private const int SortingOrder = 20;
        private const int CaptureSize = 720;
        private const float CaptureOrthographicSize = 1.5f;
        private const string RecordingPresentationSetPath =
            "Assets/_LizzoPV/Gameplay/Presentation/Data/CompanionRuntimePresentationSet.asset";
        private const string PresentationCatalogPath =
            "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset";

        private static readonly LineageAuthoring[] Lineages =
        {
            new LineageAuthoring(
                "shield_guard",
                "ShieldGuard",
                "Assets/_LizzoPV/Art/Characters/Companions/shield_guard_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/shield_captain_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardSquadRoot.prefab"),
            new LineageAuthoring(
                "sword_soldier",
                "Sword",
                BaseLibraryPath,
                PromotedLibraryPath,
                BaseMemberPrefabPath,
                PromotedMemberPrefabPath,
                SquadRootPrefabPath),
            new LineageAuthoring(
                "cleric",
                "Cleric",
                "Assets/_LizzoPV/Art/Characters/Companions/cleric_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/light_guide_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericSquadRoot.prefab"),
            new LineageAuthoring(
                "bombardier",
                "Bombardier",
                "Assets/_LizzoPV/Art/Characters/Companions/bombardier_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/powder_captain_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierSquadRoot.prefab"),
            new LineageAuthoring(
                "fire_mage",
                "FireMage",
                "Assets/_LizzoPV/Art/Characters/Companions/fire_mage_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/fire_sage_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMagePromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageSquadRoot.prefab")
        };

        [MenuItem("Lizzo/Companion Runtime/Rebuild Five-Lineage Thin Presentation")]
        public static void RebuildFiveLineageThinPresentation()
        {
            RuntimeAnimatorController controller = LoadRequired<RuntimeAnimatorController>(SharedControllerPath);
            AnimationClip attackClip = FindRequiredClip(controller, "CompanionSpriteShared_Attack");

            for (int index = 0; index < Lineages.Length; index += 1)
            {
                LineageAuthoring lineage = Lineages[index];
                SpriteLibraryAsset baseLibrary = LoadRequired<SpriteLibraryAsset>(lineage.BaseLibraryPath);
                SpriteLibraryAsset promotedLibrary = LoadRequired<SpriteLibraryAsset>(lineage.PromotedLibraryPath);
                RequireSprite(baseLibrary, lineage.BaseLibraryPath, "Idle", "0");
                RequireSprite(promotedLibrary, lineage.PromotedLibraryPath, "Idle", "0");

                CreateMemberPrefab(
                    lineage.BasePrefabPath,
                    lineage.NamePrefix + "BaseMemberView",
                    baseLibrary,
                    controller,
                    attackClip.length,
                    BaseVisualScale,
                    0,
                    false);
                CreateMemberPrefab(
                    lineage.PromotedPrefabPath,
                    lineage.NamePrefix + "PromotedMemberView",
                    promotedLibrary,
                    controller,
                    attackClip.length,
                    PromotedVisualScale,
                    2,
                    true);
                CreateSquadRootPrefab(in lineage);
            }

            Debug.Log("[CompanionPresentationTracerAuthoring] Five-lineage thin presentation rebuilt.");
        }

        public static void RebuildS5SwordPresentationTracer()
        {
            RebuildFiveLineageThinPresentation();
        }

        [MenuItem("Lizzo/Companion Runtime/Author Recording Presentation Catalog")]
        public static void AuthorRecordingPresentationCatalog()
        {
            CompanionRuntimePresentationSet presentationSet =
                AssetDatabase.LoadAssetAtPath<CompanionRuntimePresentationSet>(RecordingPresentationSetPath);
            if (presentationSet == null)
            {
                presentationSet = ScriptableObject.CreateInstance<CompanionRuntimePresentationSet>();
                AssetDatabase.CreateAsset(presentationSet, RecordingPresentationSetPath);
            }

            CompanionRuntimePresentationSet.Entry[] entries =
                new CompanionRuntimePresentationSet.Entry[Lineages.Length];
            for (int index = 0; index < Lineages.Length; index += 1)
            {
                LineageAuthoring lineage = Lineages[index];
                GameObject prefab = LoadRequired<GameObject>(lineage.SquadPrefabPath);
                CompanionSquadRoot squadRoot = prefab.GetComponent<CompanionSquadRoot>();
                if (squadRoot == null
                    || !string.Equals(squadRoot.CompanionId, lineage.CompanionId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Invalid Recording squad presentation Prefab: " + lineage.SquadPrefabPath);
                }

                entries[index] = new CompanionRuntimePresentationSet.Entry(
                    lineage.CompanionId,
                    squadRoot);
            }

            presentationSet.SetEntriesForEditor(entries);
            EditorUtility.SetDirty(presentationSet);
            AssetDatabase.SaveAssetIfDirty(presentationSet);

            PresentationCatalog catalog = LoadRequired<PresentationCatalog>(PresentationCatalogPath);
            SerializedObject catalogObject = new SerializedObject(catalog);
            SerializedProperty property = catalogObject.FindProperty("_companionRuntime");
            if (property == null)
                throw new InvalidOperationException("PresentationCatalog companion runtime field is missing.");
            property.objectReferenceValue = presentationSet;
            catalogObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("[CompanionPresentationTracerAuthoring] Recording presentation catalog authored.");
        }

        [MenuItem("Lizzo/Companion Runtime/Capture S5 Sword Presentation Tracer")]
        public static void CaptureS5SwordPresentationTracer()
        {
            string outputDirectory = Path.Combine(
                Path.GetTempPath(),
                "Lizzo_PV",
                "CompanionPresentationTracer",
                "S5Sword");
            Directory.CreateDirectory(outputDirectory);

            CaptureSnapshot(
                outputDirectory,
                "s5-sword-1-member.png",
                CreateSnapshot(
                    false,
                    new CompanionMemberSnapshot(0, false, CompanionPoint.Zero)));
            CaptureSnapshot(
                outputDirectory,
                "s5-sword-2-members.png",
                CreateSnapshot(
                    false,
                    new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, 0.0f)),
                    new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, 0.0f))));
            CaptureSnapshot(
                outputDirectory,
                "s5-sword-3-promoted.png",
                CreateSnapshot(
                    true,
                    new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, -0.14f)),
                    new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, -0.14f)),
                    new CompanionMemberSnapshot(2, true, new CompanionPoint(0.0f, 0.22f))));

            Debug.Log("[CompanionPresentationTracerAuthoring] S5 captures written: " + outputDirectory);
        }

        private static void CreateMemberPrefab(
            string prefabPath,
            string rootName,
            SpriteLibraryAsset libraryAsset,
            RuntimeAnimatorController controller,
            float attackClipLength,
            float visualScale,
            int memberOrder,
            bool promotedLeaderVisual)
        {
            GameObject root = new GameObject(rootName);
            try
            {
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(visualScale, visualScale, 1.0f);

                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = SortingOrder;
                renderer.sprite = libraryAsset.GetSprite("Idle", "0");

                Animator animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;

                SpriteLibrary library = visual.AddComponent<SpriteLibrary>();
                library.spriteLibraryAsset = libraryAsset;
                SpriteResolver resolver = visual.AddComponent<SpriteResolver>();
                resolver.SetCategoryAndLabel("Idle", "0");
                resolver.ResolveSpriteToSpriteRenderer();

                UnitVisualDriver driver = visual.AddComponent<UnitVisualDriver>();
                SerializedObject driverObject = new SerializedObject(driver);
                driverObject.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
                driverObject.FindProperty("_animator").objectReferenceValue = animator;
                driverObject.FindProperty("_spriteResolver").objectReferenceValue = resolver;
                driverObject.FindProperty("_attackHoldSeconds").floatValue = 0.28f;
                driverObject.FindProperty("_attackClipLength").floatValue = attackClipLength;
                driverObject.FindProperty("_idleFrameCount").intValue = 2;
                driverObject.FindProperty("_runFrameCount").intValue = 4;
                driverObject.FindProperty("_attackFrameCount").intValue = 4;
                driverObject.FindProperty("_deathFrameCount").intValue = 3;
                driverObject.ApplyModifiedPropertiesWithoutUndo();

                CompanionMemberView memberView = root.AddComponent<CompanionMemberView>();
                ConfigureMemberView(memberView, memberOrder, promotedLeaderVisual, driver);
                SavePrefab(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateSquadRootPrefab(in LineageAuthoring lineage)
        {
            GameObject basePrefab = LoadRequired<GameObject>(lineage.BasePrefabPath);
            GameObject promotedPrefab = LoadRequired<GameObject>(lineage.PromotedPrefabPath);
            GameObject root = new GameObject(lineage.NamePrefix + "SquadRoot");
            try
            {
                CompanionSquadRoot squadRoot = root.AddComponent<CompanionSquadRoot>();
                CompanionMemberView[] views = new CompanionMemberView[3];
                views[0] = InstantiateMember(basePrefab, root.transform, "BaseMember0", 0, false);
                views[1] = InstantiateMember(basePrefab, root.transform, "BaseMember1", 1, false);
                views[2] = InstantiateMember(promotedPrefab, root.transform, "PromotedMember2", 2, true);
                views[1].gameObject.SetActive(false);
                views[2].gameObject.SetActive(false);

                SerializedObject rootObject = new SerializedObject(squadRoot);
                rootObject.FindProperty("_companionId").stringValue = lineage.CompanionId;
                SerializedProperty members = rootObject.FindProperty("_memberViews");
                members.arraySize = views.Length;
                for (int index = 0; index < views.Length; index += 1)
                {
                    members.GetArrayElementAtIndex(index).objectReferenceValue = views[index];
                }

                rootObject.ApplyModifiedPropertiesWithoutUndo();
                SavePrefab(root, lineage.SquadPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static CompanionMemberView InstantiateMember(
            GameObject sourcePrefab,
            Transform parent,
            string name,
            int memberOrder,
            bool promotedLeaderVisual)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab, parent) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Failed to instantiate member Prefab: " + sourcePrefab.name);
            }

            instance.name = name;
            CompanionMemberView view = instance.GetComponent<CompanionMemberView>();
            if (view == null || view.VisualDriver == null)
            {
                throw new InvalidOperationException("Member Prefab is missing required presentation components: " + sourcePrefab.name);
            }

            ConfigureMemberView(view, memberOrder, promotedLeaderVisual, view.VisualDriver);
            return view;
        }

        private static void ConfigureMemberView(
            CompanionMemberView view,
            int memberOrder,
            bool promotedLeaderVisual,
            UnitVisualDriver driver)
        {
            SerializedObject viewObject = new SerializedObject(view);
            viewObject.FindProperty("_memberOrder").intValue = memberOrder;
            viewObject.FindProperty("_promotedLeaderVisual").boolValue = promotedLeaderVisual;
            viewObject.FindProperty("_visualDriver").objectReferenceValue = driver;
            viewObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SavePrefab(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success || saved == null)
            {
                throw new InvalidOperationException("Failed to save Prefab: " + path);
            }
        }

        private static void CaptureSnapshot(string outputDirectory, string fileName, SquadSnapshot snapshot)
        {
            GameObject squadPrefab = LoadRequired<GameObject>(SquadRootPrefabPath);
            GameObject squadInstance = null;
            GameObject cameraObject = null;
            RenderTexture renderTexture = null;
            Texture2D image = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                squadInstance = UnityEngine.Object.Instantiate(squadPrefab);
                squadInstance.name = "S5SwordCaptureSquad";
                CompanionSquadRoot squadRoot = squadInstance.GetComponent<CompanionSquadRoot>();
                if (squadRoot == null || !squadRoot.TryApplySnapshot(in snapshot))
                {
                    throw new InvalidOperationException("S5 sword capture snapshot was rejected.");
                }

                cameraObject = new GameObject("S5SwordCaptureCamera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = CaptureOrthographicSize;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(24, 29, 36, 255);
                camera.transform.position = new Vector3(0.0f, 0.0f, -10.0f);

                renderTexture = new RenderTexture(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                image = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(Path.Combine(outputDirectory, fileName), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (squadInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(squadInstance);
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
        }

        private static SquadSnapshot CreateSnapshot(bool promoted, params CompanionMemberSnapshot[] members)
        {
            return new SquadSnapshot(
                "squad-0",
                0,
                "sword_soldier",
                promoted ? "sword-promoted" : "sword-base",
                members.Length,
                promoted,
                true,
                0.0f,
                CompanionPoint.Zero,
                SquadActionPhase.Idle,
                -1,
                CompanionPoint.Zero,
                null,
                members);
        }

        private static AnimationClip FindRequiredClip(RuntimeAnimatorController controller, string clipName)
        {
            AnimationClip[] clips = controller.animationClips;
            for (int index = 0; index < clips.Length; index += 1)
            {
                AnimationClip clip = clips[index];
                if (clip != null && string.Equals(clip.name, clipName, StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            throw new InvalidOperationException("Required animation clip is missing: " + clipName);
        }

        private static Sprite RequireSprite(
            SpriteLibraryAsset library,
            string libraryPath,
            string category,
            string label)
        {
            Sprite sprite = library.GetSprite(category, label);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    "Required SpriteLibrary entry is missing: "
                    + libraryPath
                    + " ["
                    + category
                    + "/"
                    + label
                    + "]");
            }

            return sprite;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Required asset is missing: " + path);
            }

            return asset;
        }

        private readonly struct LineageAuthoring
        {
            public LineageAuthoring(
                string companionId,
                string namePrefix,
                string baseLibraryPath,
                string promotedLibraryPath,
                string basePrefabPath,
                string promotedPrefabPath,
                string squadPrefabPath)
            {
                CompanionId = companionId;
                NamePrefix = namePrefix;
                BaseLibraryPath = baseLibraryPath;
                PromotedLibraryPath = promotedLibraryPath;
                BasePrefabPath = basePrefabPath;
                PromotedPrefabPath = promotedPrefabPath;
                SquadPrefabPath = squadPrefabPath;
            }

            public string CompanionId { get; }

            public string NamePrefix { get; }

            public string BaseLibraryPath { get; }

            public string PromotedLibraryPath { get; }

            public string BasePrefabPath { get; }

            public string PromotedPrefabPath { get; }

            public string SquadPrefabPath { get; }
        }
    }
}
