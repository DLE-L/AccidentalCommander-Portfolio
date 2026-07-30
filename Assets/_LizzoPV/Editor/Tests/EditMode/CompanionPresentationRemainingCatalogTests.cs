using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionPresentationRemainingCatalogTests
    {
        private const string UnitPresentationSetPath = "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset";
        private const string SharedControllerPath = "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";

        private static readonly Expectation[] Expectations =
        {
            new Expectation("shield_guard", "ShieldGuard"),
            new Expectation("shield_captain", "ShieldCaptain"),
            new Expectation("sword_soldier", "SwordSoldier"),
            new Expectation("sword_captain", "SwordCaptain"),
            new Expectation("cleric", "Cleric"),
            new Expectation("light_guide", "LightGuide"),
            new Expectation("falcon_archer", "FalconArcher"),
            new Expectation("falcon_captain", "FalconCaptain"),
            new Expectation("field_herbalist", "FieldHerbalist"),
            new Expectation("battle_apothecary", "BattleApothecary"),
            new Expectation("bombardier", "Bombardier"),
            new Expectation("powder_captain", "PowderCaptain"),
            new Expectation("fire_mage", "FireMage"),
            new Expectation("fire_sage", "FireSage"),
            new Expectation("lightning_mage", "LightningMage"),
            new Expectation("storm_mage", "StormMage"),
            new Expectation("wolf_tamer", "WolfTamer"),
            new Expectation("beast_commander", "BeastCommander"),
            new Expectation("wraith_knight", "WraithKnight"),
            new Expectation("wraith_guardian", "WraithGuardian"),
            new Expectation("necromancer", "Necromancer"),
            new Expectation("dark_ritualist", "DarkRitualist"),
            new Expectation("skeleton_bomber", "SkeletonBomber"),
            new Expectation("bone_artillery", "BoneArtillery"),
        };

        private static readonly string[] NewlyScaledPrefabs =
        {
            "BattleApothecary", "BeastCommander", "Bombardier", "BoneArtillery",
            "DarkRitualist", "FieldHerbalist", "FireMage", "FireSage",
            "LightningMage", "Necromancer", "PowderCaptain", "SkeletonBomber",
            "StormMage", "WolfTamer", "WraithGuardian", "WraithKnight",
        };

        private static readonly VisualScaleExpectation[] AcceptedVisualScales =
        {
            new VisualScaleExpectation("Cleric", 0.44f),
            new VisualScaleExpectation("FalconArcher", 0.44f),
            new VisualScaleExpectation("FalconCaptain", 0.44f),
            new VisualScaleExpectation("LightGuide", 0.44f),
            new VisualScaleExpectation("ShieldCaptain", 0.56f),
            new VisualScaleExpectation("ShieldGuard", 0.44f),
            new VisualScaleExpectation("SwordCaptain", 0.44f),
            new VisualScaleExpectation("SwordSoldier", 0.44f),
        };

        [Test]
        public void UnitPresentationSet_ContainsAllCanonicalCompanionEntriesInOrder()
        {
            Object set = AssetDatabase.LoadAssetAtPath<Object>(UnitPresentationSetPath);
            Assert.IsNotNull(set);

            SerializedProperty entries = new SerializedObject(set).FindProperty("_entries");
            Assert.IsNotNull(entries);
            Assert.AreEqual(Expectations.Length, entries.arraySize);

            for (int i = 0; i < Expectations.Length; i++)
                Assert.AreEqual(Expectations[i].UnitId, entries.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue);
        }

        [Test]
        public void CanonicalCompanionEntries_UseAcceptedAssetsAndOneSharedController()
        {
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SharedControllerPath);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(controller);
            Assert.AreEqual(4, controller.animationClips.Length);
            Assert.IsNotNull(settings);

            var ids = new HashSet<string>();
            var addresses = new HashSet<string>();

            for (int i = 0; i < Expectations.Length; i++)
            {
                Expectation expected = Expectations[i];
                Assert.IsTrue(ids.Add(expected.UnitId), expected.UnitId);
                Assert.IsTrue(addresses.Add(expected.Address), expected.Address);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expected.PrefabPath);
                SpriteLibraryAsset libraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(expected.LibraryPath);
                Assert.IsNotNull(prefab, expected.UnitId);
                Assert.IsNotNull(libraryAsset, expected.UnitId);

                Transform visual = prefab.transform.Find("Visual");
                Assert.IsNotNull(visual, expected.UnitId);
                SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
                SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
                Animator animator = visual.GetComponent<Animator>();
                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(library, expected.UnitId);
                Assert.IsNotNull(resolver, expected.UnitId);
                Assert.IsNotNull(animator, expected.UnitId);
                Assert.IsNotNull(renderer, expected.UnitId);
                Assert.AreSame(libraryAsset, library.spriteLibraryAsset);
                Assert.AreSame(controller, animator.runtimeAnimatorController);
                Assert.AreEqual("Idle", resolver.GetCategory());
                Assert.AreEqual("0", resolver.GetLabel());
                Assert.IsNotNull(renderer.sprite);
                Assert.AreEqual("Idle_0", renderer.sprite.name);
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));

                if (i >= 8)
                {
                    GameObject partyUnitBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Prefabs/Units/Base/PartyUnitBase.prefab");
                    GameObject partyUnitSource = PrefabUtility.GetCorrespondingObjectFromSource(partyUnitBase);
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                    Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(prefab));
                    Assert.AreSame(partyUnitSource, prefabSource);

                    Component[] partyComponents = partyUnitBase.GetComponents<Component>();
                    for (int componentIndex = 0; componentIndex < partyComponents.Length; componentIndex++)
                    {
                        Component partyComponent = partyComponents[componentIndex];
                        if (partyComponent != null)
                            Assert.IsNotNull(prefab.GetComponent(partyComponent.GetType()), partyComponent.GetType().Name);
                    }
                }

                AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(expected.PrefabPath));
                Assert.IsNotNull(entry, expected.UnitId);
                Assert.AreEqual(expected.Address, entry.address);
                Assert.IsFalse(entry.address.StartsWith("P0/"));
            }
        }

        [Test]
        public void CanonicalCompanionVisuals_UseApprovedScaleContract()
        {
            for (int i = 0; i < NewlyScaledPrefabs.Length; i++)
                AssertVisualScale(NewlyScaledPrefabs[i], 0.44f);

            for (int i = 0; i < AcceptedVisualScales.Length; i++)
                AssertVisualScale(AcceptedVisualScales[i].PrefabName, AcceptedVisualScales[i].Scale);
        }

        private static void AssertVisualScale(string prefabName, float scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Prefabs/Characters/Companions/" + prefabName + ".prefab");
            Assert.IsNotNull(prefab, prefabName);
            Transform visual = prefab.transform.Find("Visual");
            Assert.IsNotNull(visual, prefabName);
            Assert.AreEqual(new Vector3(scale, scale, 1.0f), visual.localScale, prefabName);
        }

        private readonly struct Expectation
        {
            public readonly string UnitId;
            public readonly string PrefabName;

            public Expectation(string unitId, string prefabName)
            {
                UnitId = unitId;
                PrefabName = prefabName;
            }

            public string Address => "Lizzo/Characters/Companions/" + UnitId;
            public string PrefabPath => "Assets/_LizzoPV/Prefabs/Characters/Companions/" + PrefabName + ".prefab";
            public string LibraryPath => "Assets/_LizzoPV/Art/Characters/Companions/" + UnitId + "_SpriteLibrary.asset";
        }

        private readonly struct VisualScaleExpectation
        {
            public readonly string PrefabName;
            public readonly float Scale;

            public VisualScaleExpectation(string prefabName, float scale)
            {
                PrefabName = prefabName;
                Scale = scale;
            }
        }
    }
}
