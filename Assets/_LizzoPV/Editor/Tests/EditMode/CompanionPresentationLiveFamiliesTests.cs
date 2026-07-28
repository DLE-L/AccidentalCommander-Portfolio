using Lizzo.PV.P0.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionPresentationLiveFamiliesTests
    {
        private const string UnitPresentationSetPath = "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset";
        private const string SharedControllerPath = "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";

        private static readonly Expectation[] Expectations =
        {
            new Expectation("sword_soldier", "Lizzo/Characters/Companions/sword_soldier", "SwordSoldier", "sword_soldier"),
            new Expectation("sword_captain", "Lizzo/Characters/Companions/sword_captain", "SwordCaptain", "sword_captain"),
            new Expectation("cleric", "Lizzo/Characters/Companions/cleric", "Cleric", "cleric"),
            new Expectation("light_guide", "Lizzo/Characters/Companions/light_guide", "LightGuide", "light_guide"),
            new Expectation("falcon_archer", "Lizzo/Characters/Companions/falcon_archer", "FalconArcher", "falcon_archer"),
            new Expectation("falcon_captain", "Lizzo/Characters/Companions/falcon_captain", "FalconCaptain", "falcon_captain"),
        };

        [Test]
        public void UnitPresentationSet_ResolvesAllLiveFamilyBaseAndPromotionPairs()
        {
            UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            Assert.IsNotNull(set);

            AssertPair(set, "sword_soldier", "sword_captain");
            AssertPair(set, "cleric", "light_guide");
            AssertPair(set, "falcon_archer", "falcon_captain");

            foreach (Expectation expected in Expectations)
            {
                Assert.IsTrue(set.TryGetEntry(expected.UnitId, out UnitPresentationSet.Entry entry));
                Assert.AreEqual(expected.Address, entry.AddressableKey);
                Assert.IsNotNull(entry.Portrait);
                Assert.AreEqual("Idle_0", entry.Portrait.name);
            }
        }

        [Test]
        public void CanonicalLiveFamilyPrefabs_UseAcceptedLibrariesAndP9D1SharedController()
        {
            RuntimeAnimatorController sharedController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SharedControllerPath);
            Assert.IsNotNull(sharedController);
            Assert.AreEqual(4, sharedController.animationClips.Length);

            foreach (Expectation expected in Expectations)
                AssertCanonicalPrefab(expected, sharedController);
        }

        [Test]
        public void CanonicalLiveFamilyPrefabs_HaveStableNonP0Addresses()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings);

            foreach (Expectation expected in Expectations)
            {
                string guid = AssetDatabase.AssetPathToGUID(expected.PrefabPath);
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                Assert.IsNotNull(entry, expected.UnitId);
                Assert.AreEqual(expected.Address, entry.address);
                Assert.IsFalse(entry.address.StartsWith("P0/"));
            }
        }

        [Test]
        public void FalconCatalogIdentity_UsesCanonicalFalconIdWithoutLegacyArcherAlias()
        {
            UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            Assert.IsNotNull(set);
            Assert.IsTrue(set.TryGetEntry("falcon_archer", out UnitPresentationSet.Entry falcon));
            Assert.AreEqual("Lizzo/Characters/Companions/falcon_archer", falcon.AddressableKey);
            Assert.IsFalse(set.TryGetEntry("archer", out _));
        }

        [Test]
        public void LegacyLiveFamilyAddresses_RemainRegisteredForCompatibilityFallback()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings);
            AssertLegacyAddress(settings, "Assets/_LizzoPV/Prefabs/Units/Companions/Swordsman.prefab", "P0/Units/Companions/Swordsman.prefab");
            AssertLegacyAddress(settings, "Assets/_LizzoPV/Prefabs/Units/Companions/Cleric.prefab", "P0/Units/Companions/Cleric.prefab");
            AssertLegacyAddress(settings, "Assets/_LizzoPV/Prefabs/Units/Companions/Archer.prefab", "P0/Units/Companions/Archer.prefab");
        }

        private static void AssertPair(UnitPresentationSet set, string baseUnitId, string promotedUnitId)
        {
            Assert.IsTrue(set.TryGetEntry(baseUnitId, out UnitPresentationSet.Entry baseEntry));
            Assert.IsTrue(set.TryGetEntry(promotedUnitId, out UnitPresentationSet.Entry promotedEntry));
            Assert.AreNotEqual(baseEntry.AddressableKey, promotedEntry.AddressableKey);
            Assert.AreNotSame(baseEntry.Prefab, promotedEntry.Prefab);
        }

        private static void AssertLegacyAddress(AddressableAssetSettings settings, string prefabPath, string expectedAddress)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
            Assert.IsNotNull(entry, prefabPath);
            Assert.AreEqual(expectedAddress, entry.address);
        }

        private static void AssertCanonicalPrefab(Expectation expected, RuntimeAnimatorController sharedController)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expected.PrefabPath);
            SpriteLibraryAsset libraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(expected.LibraryPath);
            Assert.IsNotNull(prefab, expected.UnitId);
            Assert.IsNotNull(libraryAsset, expected.UnitId);

            Transform visual = prefab.transform.Find("Visual");
            Assert.IsNotNull(visual, expected.UnitId);
            SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
            SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            Animator animator = visual.GetComponent<Animator>();
            Assert.IsNotNull(library, expected.UnitId);
            Assert.IsNotNull(resolver, expected.UnitId);
            Assert.IsNotNull(renderer, expected.UnitId);
            Assert.IsNotNull(animator, expected.UnitId);
            Assert.AreSame(libraryAsset, library.spriteLibraryAsset);
            Assert.AreSame(sharedController, animator.runtimeAnimatorController);
            Assert.AreEqual("Idle", resolver.GetCategory());
            Assert.AreEqual("0", resolver.GetLabel());
            Assert.IsNotNull(renderer.sprite);
            Assert.AreEqual("Idle_0", renderer.sprite.name);
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));
        }

        private readonly struct Expectation
        {
            public readonly string UnitId;
            public readonly string Address;
            public readonly string PrefabName;
            public readonly string LibraryId;

            public Expectation(string unitId, string address, string prefabName, string libraryId)
            {
                UnitId = unitId;
                Address = address;
                PrefabName = prefabName;
                LibraryId = libraryId;
            }

            public string PrefabPath => "Assets/_LizzoPV/Prefabs/Characters/Companions/" + PrefabName + ".prefab";
            public string LibraryPath => "Assets/_LizzoPV/Art/Characters/Companions/" + LibraryId + "_SpriteLibrary.asset";
        }
    }
}
