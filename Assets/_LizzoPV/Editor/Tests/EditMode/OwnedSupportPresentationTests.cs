using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class OwnedSupportPresentationTests
    {
        private const string SetPath = "Assets/_LizzoPV/Data/Presentation/OwnedSupportPresentationSet.asset";
        private const string CatalogPath = "Assets/_LizzoPV/Data/Presentation/PresentationCatalog.asset";
        private const string ControllerPath = "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";

        private static readonly Expected[] Entries =
        {
            new Expected("bird_temporary_stand_in", "BirdTemporaryStandIn", "bird_temporary_stand_in", "Attack"),
            new Expected("grey_wolf_support", "GreyWolfSupport", "grey_wolf_support", "Attack"),
            new Expected("UNIT_PERSONAL_SKELETON_01", "PersonalSkeletonSummon", "necromancer_skeleton_summon", "Slash"),
        };

        [Test]
        public void OwnedSupportCatalog_ResolvesThreePresentationOnlyEntries()
        {
            OwnedSupportPresentationSet set = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>(SetPath);
            PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(CatalogPath);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            Assert.IsNotNull(set); Assert.IsNotNull(catalog); Assert.AreSame(set, catalog.OwnedSupports); Assert.AreEqual(3, set.Entries.Length);
            Assert.IsNotNull(controller); Assert.AreEqual(4, controller.animationClips.Length);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (Expected expected in Entries)
            {
                Assert.IsTrue(PresentationCatalogProvider.TryGetOwnedSupport(catalog, expected.Id, out OwnedSupportPresentationSet.Entry entry));
                Assert.AreEqual("Lizzo/Characters/Supports/" + expected.Id, entry.AddressableKey);
                Assert.AreEqual(expected.AttackCategory, entry.AttackCategory);
                Assert.IsNotNull(entry.Portrait); Assert.AreEqual("Idle_0", entry.Portrait.name);
                Assert.IsNotNull(entry.SpriteLibrary);
                GameObject prefab = entry.Prefab;
                Assert.IsNotNull(prefab);
                Assert.AreEqual(expected.PrefabName, prefab.name);
                Transform visual = prefab.transform.Find("Visual");
                Assert.IsNotNull(visual); Assert.AreEqual(1, prefab.transform.childCount);
                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                Animator animator = visual.GetComponent<Animator>();
                SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
                SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
                UnitVisualDriver driver = visual.GetComponent<UnitVisualDriver>();
                Assert.IsNotNull(renderer); Assert.IsNotNull(animator); Assert.IsNotNull(library); Assert.IsNotNull(resolver); Assert.IsNotNull(driver);
                Assert.AreSame(controller, animator.runtimeAnimatorController); Assert.AreSame(entry.SpriteLibrary, library.spriteLibraryAsset);
                Assert.AreEqual("Idle", resolver.GetCategory()); Assert.AreEqual("0", resolver.GetLabel()); Assert.IsNotNull(renderer.sprite); Assert.AreEqual("Idle_0", renderer.sprite.name);
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));
                Assert.IsNull(prefab.GetComponent<Rigidbody2D>()); Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider2D>(true).Length);
            Assert.IsNull(prefab.GetComponent<Lizzo.PV.Legion.CompanionRuntime>());
                AddressableAssetEntry address = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)));
                Assert.IsNotNull(address); Assert.AreEqual(entry.AddressableKey, address.address); Assert.IsFalse(address.address.StartsWith("P0/"));
            }
        }

        private readonly struct Expected
        {
            public readonly string Id, PrefabName, LibraryId, AttackCategory;
            public Expected(string id, string prefabName, string libraryId, string attackCategory) { Id=id; PrefabName=prefabName; LibraryId=libraryId; AttackCategory=attackCategory; }
        }
    }
}
