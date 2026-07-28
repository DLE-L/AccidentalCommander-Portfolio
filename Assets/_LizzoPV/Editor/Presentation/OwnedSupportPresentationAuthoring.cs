using System;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class OwnedSupportPresentationAuthoring
    {
        private const string SupportFolder = "Assets/_LizzoPV/Prefabs/Characters/Supports";
        private const string SetPath = "Assets/_LizzoPV/Data/Presentation/OwnedSupportPresentationSet.asset";
        private const string CatalogPath = "Assets/_LizzoPV/Data/Presentation/PresentationCatalog.asset";
        private const string ControllerPath = "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";

        private static readonly Spec[] Specs =
        {
            new Spec("bird_temporary_stand_in", "BirdTemporaryStandIn", "bird_temporary_stand_in", "Attack", "Not a final falcon claim; temporary Bird/parrot-shaped stand-in."),
            new Spec("grey_wolf_support", "GreyWolfSupport", "grey_wolf_support", "Attack", "Owner-bound support visual; not a squad-slot owner."),
            new Spec("UNIT_PERSONAL_SKELETON_01", "PersonalSkeletonSummon", "necromancer_skeleton_summon", "Slash", "Necromancer personal Skeleton; distinct from UNIT_SYNERGY_SKELETON_01."),
        };

        public static void CreateOrUpdate()
        {
            EnsureFolder("Assets/_LizzoPV/Prefabs");
            EnsureFolder("Assets/_LizzoPV/Prefabs/Characters");
            EnsureFolder(SupportFolder);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null || controller.animationClips.Length != 4)
                throw new InvalidOperationException("Accepted shared companion controller is missing or incomplete.");

            OwnedSupportPresentationSet.Entry[] entries = new OwnedSupportPresentationSet.Entry[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
                entries[i] = CreateOrUpdatePrefab(Specs[i], controller);

            OwnedSupportPresentationSet set = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<OwnedSupportPresentationSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            set.SetEntriesForEditor(entries);
            EditorUtility.SetDirty(set);

            PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Presentation catalog is missing.");
            catalog.SetPresentationSetsForEditor(catalog.Feedback, catalog.Announcements, catalog.Cards, catalog.SquadSlots, catalog.Units, set);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static OwnedSupportPresentationSet.Entry CreateOrUpdatePrefab(Spec spec, AnimatorController controller)
        {
            string libraryPath = "Assets/_LizzoPV/Art/Characters/Summons/" + spec.LibraryId + "_SpriteLibrary.asset";
            string sheetPath = "Assets/_LizzoPV/Art/Characters/Summons/" + spec.LibraryId + "_SpriteSheet.png";
            string prefabPath = SupportFolder + "/" + spec.PrefabName + ".prefab";
            SpriteLibraryAsset library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
            if (library == null) throw new InvalidOperationException("Support SpriteLibrary missing: " + libraryPath);
            Sprite portrait = ShieldPresentationTracerAuthoring.FindSprite(sheetPath, "Idle_0");

            GameObject root = new GameObject(spec.PrefabName);
            try
            {
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                Animator animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                SpriteLibrary spriteLibrary = visual.AddComponent<SpriteLibrary>();
                spriteLibrary.spriteLibraryAsset = library;
                SpriteResolver resolver = visual.AddComponent<SpriteResolver>();
                resolver.SetCategoryAndLabel("Idle", "0");
                resolver.ResolveSpriteToSpriteRenderer();
                UnitVisualDriver driver = visual.AddComponent<UnitVisualDriver>();
                SerializedObject driverObject = new SerializedObject(driver);
                driverObject.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
                driverObject.FindProperty("_animator").objectReferenceValue = animator;
                driverObject.FindProperty("_spriteResolver").objectReferenceValue = resolver;
                driverObject.ApplyModifiedPropertiesWithoutUndo();
                driver.SetMotionCategoriesForPresentation("Idle", "Run", spec.AttackCategory, "Death");
                driver.SetLinkedDriversForPresentation(Array.Empty<UnitVisualDriver>());

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            ConfigureAddressable(prefabPath, spec.AddressableKey);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return new OwnedSupportPresentationSet.Entry(spec.Id, spec.AddressableKey, prefab, portrait, library, "Idle", "Run", spec.AttackCategory, "Death", spec.Limitation);
        }

        private static void ConfigureAddressable(string prefabPath, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings == null ? null : settings.FindGroup("Prefabs");
            if (group == null) throw new InvalidOperationException("Addressables Prefabs group is missing.");
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath), group, false, true);
            entry.address = address;
            entry.SetLabel("Prefab", true, true, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true, true);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private readonly struct Spec
        {
            public readonly string Id, PrefabName, LibraryId, AttackCategory, Limitation;
            public Spec(string id, string prefabName, string libraryId, string attackCategory, string limitation) { Id=id; PrefabName=prefabName; LibraryId=libraryId; AttackCategory=attackCategory; Limitation=limitation; }
            public string AddressableKey => "Lizzo/Characters/Supports/" + Id;
        }
    }
}
