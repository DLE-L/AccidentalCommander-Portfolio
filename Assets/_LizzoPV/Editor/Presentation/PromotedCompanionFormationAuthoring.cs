using System;
using Lizzo.PV.P0.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class PromotedCompanionFormationAuthoring
    {
        private const string PrefabFolder = "Assets/_LizzoPV/Prefabs/Characters/Companions";
        private const string LibraryFolder = "Assets/_LizzoPV/Art/Characters/Companions";
        private const string SharedControllerPath = "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";
        private static readonly Vector3[] SupportPositions = { new Vector3(-0.34f, -0.14f, 0.0f), new Vector3(0.34f, -0.14f, 0.0f) };
        private static readonly Vector3 SupportScale = new Vector3(0.50f, 0.50f, 1.0f);

        private static readonly PromotionSpec[] Promotions =
        {
            new PromotionSpec("ShieldCaptain", "shield_guard"),
            new PromotionSpec("SwordCaptain", "sword_soldier"),
            new PromotionSpec("LightGuide", "cleric"),
            new PromotionSpec("FalconCaptain", "falcon_archer"),
            new PromotionSpec("BattleApothecary", "field_herbalist"),
            new PromotionSpec("PowderCaptain", "bombardier"),
            new PromotionSpec("FireSage", "fire_mage"),
            new PromotionSpec("StormMage", "lightning_mage"),
            new PromotionSpec("BeastCommander", "wolf_tamer"),
            new PromotionSpec("WraithGuardian", "wraith_knight"),
            new PromotionSpec("DarkRitualist", "necromancer"),
            new PromotionSpec("BoneArtillery", "skeleton_bomber"),
        };

        public static void CreateOrUpdate()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath);
            if (controller == null || controller.animationClips.Length != 4)
                throw new InvalidOperationException("The accepted shared companion controller is missing or incomplete.");

            for (int i = 0; i < Promotions.Length; i++)
                ConfigurePromotion(Promotions[i], controller);

            AssetDatabase.SaveAssets();
        }

        private static void ConfigurePromotion(PromotionSpec specification, AnimatorController controller)
        {
            string prefabPath = PrefabFolder + "/" + specification.PrefabName + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform captainVisual = root.transform.Find("Visual");
                if (captainVisual == null)
                    throw new InvalidOperationException("Promoted companion lacks Visual: " + prefabPath);

                UnitVisualDriver captainDriver = captainVisual.GetComponent<UnitVisualDriver>();
                SpriteRenderer captainRenderer = captainVisual.GetComponent<SpriteRenderer>();
                if (captainDriver == null || captainRenderer == null)
                    throw new InvalidOperationException("Promoted companion Visual lacks driver or renderer: " + prefabPath);

                SpriteLibraryAsset baseLibrary = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(LibraryFolder + "/" + specification.BaseUnitId + "_SpriteLibrary.asset");
                if (baseLibrary == null)
                    throw new InvalidOperationException("Base support library is missing: " + specification.BaseUnitId);

                Transform supportRoot = root.transform.Find("SupportVisuals");
                if (supportRoot == null)
                {
                    supportRoot = new GameObject("SupportVisuals").transform;
                    supportRoot.SetParent(root.transform, false);
                }

                UnitVisualDriver[] supportDrivers = new UnitVisualDriver[2];
                for (int i = 0; i < supportDrivers.Length; i++)
                    supportDrivers[i] = ConfigureSupport(supportRoot, i, baseLibrary, controller, captainRenderer);

                captainDriver.SetLinkedDriversForPresentation(supportDrivers);
                EditorUtility.SetDirty(captainDriver);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static UnitVisualDriver ConfigureSupport(
            Transform supportRoot,
            int index,
            SpriteLibraryAsset baseLibrary,
            AnimatorController controller,
            SpriteRenderer captainRenderer)
        {
            string name = "Support_" + index.ToString("00");
            Transform support = supportRoot.Find(name);
            if (support == null)
            {
                support = new GameObject(name).transform;
                support.SetParent(supportRoot, false);
            }

            support.localPosition = SupportPositions[index];
            support.localRotation = Quaternion.identity;
            support.localScale = SupportScale;

            SpriteRenderer renderer = support.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = support.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = captainRenderer.sortingLayerID;
            renderer.sortingOrder = captainRenderer.sortingOrder - 1;

            Animator animator = support.GetComponent<Animator>();
            if (animator == null)
                animator = support.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            SpriteLibrary library = support.GetComponent<SpriteLibrary>();
            if (library == null)
                library = support.gameObject.AddComponent<SpriteLibrary>();
            library.spriteLibraryAsset = baseLibrary;

            SpriteResolver resolver = support.GetComponent<SpriteResolver>();
            if (resolver == null)
                resolver = support.gameObject.AddComponent<SpriteResolver>();
            resolver.SetCategoryAndLabel("Idle", "0");
            resolver.ResolveSpriteToSpriteRenderer();

            UnitVisualDriver driver = support.GetComponent<UnitVisualDriver>();
            if (driver == null)
                driver = support.gameObject.AddComponent<UnitVisualDriver>();
            SerializedObject driverObject = new SerializedObject(driver);
            driverObject.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
            driverObject.FindProperty("_animator").objectReferenceValue = animator;
            driverObject.FindProperty("_spriteResolver").objectReferenceValue = resolver;
            driverObject.ApplyModifiedPropertiesWithoutUndo();
            driver.SetLinkedDriversForPresentation(Array.Empty<UnitVisualDriver>());
            EditorUtility.SetDirty(driver);
            return driver;
        }

        private readonly struct PromotionSpec
        {
            public readonly string PrefabName;
            public readonly string BaseUnitId;

            public PromotionSpec(string prefabName, string baseUnitId)
            {
                PrefabName = prefabName;
                BaseUnitId = baseUnitId;
            }
        }
    }
}
