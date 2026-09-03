using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PresentationCatalogContractTests
    {
        private const string CatalogPath = "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset";

        private static readonly CueExpectation[] CueExpectations =
        {
            new("healing_received", "d70b1dd1e2bf0bd4d99980b878f157c1", 0.75f),
            new("buff_applied", "8e6d898261b23e442bbb610706cf064f", 0.55f),
            new("player_damaged", "d1d2763a716b902498fbd13779dcc09f", 0.75f),
            new("shield_orc_crack", "da6d1fb1b5de1fb4ea5ae74cda8b3a6d", 0.9f),
            new("red_charger_charge", "cf59306605948264bb787b9eadefb1f0", 0.7f),
            new("boss_aoe_impact", "4f64ab2f5615a1e4bbeb85e5b91fb3c8", 0.9f),
            new("guard_squad_activate", "f357a44a1e4668a4589f7f4e26188cbb", 0.72f),
            new("guard_shockwave", "f357a44a1e4668a4589f7f4e26188cbb", 0.85f),
            new("guard_radial_shield", "979ef676942f4ea4fb40da4533014ff4", 0.7f),
            new("level_up", "40e04959db3c3fe4aa59332bf360753d", 0.85f),
            new("card_select", "8e6d898261b23e442bbb610706cf064f", 0.75f),
            new("result_clear", "4666d324dfefb8448bec898fa6a59fa3", 0.85f),
            new("exp_absorb", "dbaa8c0b8123afb4fbd0f26b257a833a", 0.6f),
            new("companion_recruit", "82047224673c2274c9a098cb4a8c8435", 1.0f),
            new("companion_promotion", "4666d324dfefb8448bec898fa6a59fa3", 1.0f),
            new("promotion_shout_activate", "f71c9bb1bf143e7478dd498a6d8a9bbf", 1.0f),
            new("synergy_ready", "82047224673c2274c9a098cb4a8c8435", 1.0f),
            new("synergy_complete", "4666d324dfefb8448bec898fa6a59fa3", 1.0f),
            new("rapid_crossbow_cast", "616aafbbabf60b74d8c2d34678f26cc1", 1.0f),
            new("piercing_spear_cast", "1dbde3963626e6747b20045b89da060d", 1.0f),
            new("blast_staff_cast", "8d1bfd5e9ee5bee49a37b03a15e20f0e", 1.0f),
            new("blast_staff_explosion", "cf0727807f7f9c9478459c147960fbe7", 1.0f),
            new("boss_spawn", "58cdc83c5d08dd646b9f9c3ea7a62b5b", 1.0f),
            new("dmg_shield_bash_v1", "1dbde3963626e6747b20045b89da060d", 1.0f),
            new("dmg_sword_slash_v1", "6b0199c57b3e79448b2a1d172527781f", 1.0f),
            new("dmg_cleric_bolt_v1", "8d1bfd5e9ee5bee49a37b03a15e20f0e", 1.0f),
        };

        private static readonly WrapperExpectation[] WrapperExpectations =
        {
            new("healing_received", "vfx/healing_received", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/healing_received.prefab", 0.7f, 0.62f, Vector3.zero),
            new("buff_applied", "vfx/buff_applied", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/buff_applied.prefab", 0.7f, 0.68f, Vector3.zero),
            new("boss_aoe_impact", "vfx/boss_aoe_impact", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/boss_aoe_impact.prefab", 0.55f, 0.86f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("guard_shockwave", "vfx/guard_shockwave", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/guard_shockwave.prefab", 0.32f, 0.74f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("companion_recruit", "vfx/companion_recruit", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/companion_recruit.prefab", 0.5f, 0.7f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("companion_promotion", "vfx/companion_promotion", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/companion_promotion.prefab", 0.5f, 0.7f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("promotion_shout_activate", "vfx/promotion_shout_activate", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/promotion_shout_activate.prefab", 0.5f, 0.7f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("synergy_ready", "vfx/synergy_ready", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/synergy_ready.prefab", 0.5f, 0.7f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("synergy_complete", "vfx/synergy_complete", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/synergy_complete.prefab", 0.5f, 0.7f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("blast_staff_explosion", "vfx/blast_staff_explosion", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/blast_staff_explosion.prefab", 0.5f, 0.7f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("boss_spawn", "vfx/boss_spawn", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/General/boss_spawn.prefab", 0.5f, 0.7f, new Vector3(30.0f, 0.0f, 0.0f)),
            new("dmg_shield_bash_v1", "vfx/dmg_shield_bash_v1", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/Companion/dmg_shield_bash_v1.prefab", 0.5f, 0.7f, new Vector3(0.0f, 90.0f, 90.0f)),
            new("dmg_sword_slash_v1", "vfx/dmg_sword_slash_v1", "Assets/_LizzoPV/Gameplay/Presentation/Prefabs/VFX/Companion/dmg_sword_slash_v1.prefab", 0.5f, 0.7f, new Vector3(-90.0f, -90.0f, -90.0f)),
        };

        [Test]
        public void CatalogResolvesCanonicalIds_AndAddressableWrappersMatchTheAuthoredPrefabs()
        {
            PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Feedback, Is.Not.Null);
            Assert.That(catalog.Projectiles, Is.Not.Null);
            Assert.That(catalog.Feedback.CueCount, Is.EqualTo(26));
            Assert.That(catalog.Projectiles.Count, Is.EqualTo(9));

            foreach (CueExpectation expected in CueExpectations)
            {
                Assert.That(catalog.Feedback.TryResolve(expected.PresentationId, out FeedbackPresentationCatalog.Definition definition), Is.True, expected.PresentationId);
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition.Sfx)), Is.EqualTo(expected.ClipGuid), expected.PresentationId);
                Assert.That(definition.SfxVolumeScale, Is.EqualTo(expected.Volume).Within(0.0001f), expected.PresentationId);
            }

            string serializedCatalog = File.ReadAllText(CatalogPath);
            StringAssert.DoesNotContain("FeedbackPresentationSet", serializedCatalog);
            StringAssert.DoesNotContain("_entries:", serializedCatalog);
            StringAssert.DoesNotContain("_companionAttacks:", serializedCatalog);
            StringAssert.DoesNotContain("_wrapperAddress:", serializedCatalog);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.That(settings, Is.Not.Null);
            AddressableAssetGroup group = settings.FindGroup("Gameplay Presentation");
            Assert.That(group, Is.Not.Null);

            var seenGuids = new HashSet<string>(StringComparer.Ordinal);
            foreach (WrapperExpectation expected in WrapperExpectations)
            {
                Assert.That(expected.Address, Is.EqualTo($"vfx/{expected.PresentationId}"), expected.PresentationId);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expected.PrefabPath);
                Assert.That(prefab.GetComponents<VfxWrapperInstance>(), Has.Length.EqualTo(1), expected.PresentationId);
                Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero), expected.PresentationId);
                Assert.That(prefab.transform.localRotation, Is.EqualTo(Quaternion.identity), expected.PresentationId);
                Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), expected.PresentationId);
                Transform visual = prefab.transform.Find("Visual");
                Assert.That(visual, Is.Not.Null, expected.PresentationId);
                Assert.That(visual.localPosition, Is.EqualTo(Vector3.zero), expected.PresentationId);
                Assert.That(visual.localScale, Is.EqualTo(Vector3.one * expected.BaseScale), expected.PresentationId);
                Assert.That(Quaternion.Angle(visual.localRotation, Quaternion.Euler(expected.VisualRotation)), Is.LessThan(0.01f), expected.PresentationId);

                SerializedObject wrapperSerialized = new SerializedObject(prefab.GetComponent<VfxWrapperInstance>());
                Assert.That(wrapperSerialized.FindProperty("_lifetimeSeconds").floatValue, Is.EqualTo(expected.Lifetime).Within(0.0001f), expected.PresentationId);

                string guid = AssetDatabase.AssetPathToGUID(expected.PrefabPath);
                Assert.That(guid, Is.Not.Empty, expected.PrefabPath);
                Assert.That(seenGuids.Add(guid), Is.True, expected.PresentationId);
                Assert.That(AssetDatabase.GetMainAssetTypeAtPath(expected.PrefabPath), Is.EqualTo(typeof(GameObject)), expected.PrefabPath);

                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                Assert.That(entry, Is.Not.Null, expected.PresentationId);
                Assert.That(entry.parentGroup, Is.EqualTo(group), expected.PresentationId);
                Assert.That(entry.address, Is.EqualTo(expected.Address), expected.PresentationId);
                CollectionAssert.Contains(entry.labels, "PreLoad", expected.PresentationId);
            }

            Assert.That(group.entries.Count, Is.EqualTo(WrapperExpectations.Length));
            Assert.That(catalog.Feedback.TryValidate(out string issue), Is.True, issue);
        }

        [UnityTest]
        public IEnumerator GameScenePreloadLabel_CachesEveryWrapperAtItsCatalogAddress()
        {
            AsyncOperationHandle<IResourceLocator> initializeHandle = Addressables.InitializeAsync(false);
            Assert.That(initializeHandle.WaitForCompletion(), Is.Not.Null);

            AssetDatabaseProvider assetDatabaseProvider = null;
            foreach (IResourceProvider provider in Addressables.ResourceManager.ResourceProviders)
            {
                if (provider is AssetDatabaseProvider candidate)
                {
                    assetDatabaseProvider = candidate;
                    break;
                }
            }

            Assert.That(assetDatabaseProvider, Is.Not.Null);
            float originalLoadDelay = assetDatabaseProvider.GetLoadDelay();
            assetDatabaseProvider.SetLoadDelay(0f);

            var assets = new AddressableAssetService();
            try
            {
                MethodInfo updateResourceManager = Addressables.ResourceManager
                    .GetType()
                    .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(updateResourceManager, Is.Not.Null, "Addressables ResourceManager update seam changed.");

                UniTask<AssetPreloadResult> preloadTask = assets.PreloadLabelAsync<UnityEngine.Object>("PreLoad");
                UniTask<AssetPreloadResult>.Awaiter preloadAwaiter = preloadTask.GetAwaiter();
                object[] updateArguments = { 0.016f };
                double deadline = EditorApplication.timeSinceStartup + 15.0;
                while (!preloadAwaiter.IsCompleted && EditorApplication.timeSinceStartup < deadline)
                {
                    updateResourceManager.Invoke(Addressables.ResourceManager, updateArguments);
                    yield return null;
                }

                Assert.That(preloadAwaiter.IsCompleted, Is.True, "PreLoad did not complete within 15 seconds.");
                AssetPreloadResult preload = preloadAwaiter.GetResult();
                Assert.That(preload, Is.Not.Null);
                Assert.That(preload.Succeeded, Is.True);

                foreach (WrapperExpectation expected in WrapperExpectations)
                {
                    GameObject authoredPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(expected.PrefabPath);
                    Assert.That(assets.GetCached<GameObject>(expected.Address), Is.EqualTo(authoredPrefab), expected.PresentationId);
                }
            }
            finally
            {
                assets.ReleaseAll();
                assetDatabaseProvider.SetLoadDelay(originalLoadDelay);
                if (initializeHandle.IsValid())
                    Addressables.Release(initializeHandle);
            }
        }

        [Test]
        public void CompanionRuntimeSet_CanBeAssignedAndResolveItsSquadRoot()
        {
            PresentationCatalog catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            CompanionRuntimePresentationSet set = ScriptableObject.CreateInstance<CompanionRuntimePresentationSet>();
            GameObject squadObject = new GameObject("CompanionSquadRoot");
            CompanionSquadRoot squadRoot = squadObject.AddComponent<CompanionSquadRoot>();
            try
            {
                set.SetEntriesForEditor(new[]
                {
                    new CompanionRuntimePresentationSet.Entry("shield_guard", squadRoot),
                });
                catalog.SetPresentationSetsForEditor(null, null, companionRuntime: set);

                Assert.That(catalog.CompanionRuntime, Is.SameAs(set));
                Assert.That(set.TryGetSquadRoot("shield_guard", out CompanionSquadRoot resolved), Is.True);
                Assert.That(resolved, Is.SameAs(squadRoot));
                Assert.That(set.TryGetSquadRoot("unknown", out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(squadObject);
                UnityEngine.Object.DestroyImmediate(set);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        private readonly struct WrapperExpectation
        {
            public WrapperExpectation(string presentationId, string address, string prefabPath, float lifetime, float baseScale, Vector3 visualRotation)
            {
                PresentationId = presentationId;
                Address = address;
                PrefabPath = prefabPath;
                Lifetime = lifetime;
                BaseScale = baseScale;
                VisualRotation = visualRotation;
            }

            public string PresentationId { get; }
            public string Address { get; }
            public string PrefabPath { get; }
            public float Lifetime { get; }
            public float BaseScale { get; }
            public Vector3 VisualRotation { get; }
        }

        private readonly struct CueExpectation
        {
            public CueExpectation(string presentationId, string clipGuid, float volume)
            {
                PresentationId = presentationId;
                ClipGuid = clipGuid;
                Volume = volume;
            }

            public string PresentationId { get; }
            public string ClipGuid { get; }
            public float Volume { get; }
        }
    }
}
