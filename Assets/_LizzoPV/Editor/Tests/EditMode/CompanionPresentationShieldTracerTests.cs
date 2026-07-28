using Lizzo.PV.P0.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionPresentationShieldTracerTests
    {
        private const string GuardId = "shield_guard";
        private const string CaptainId = "shield_captain";
        private const string GuardAddress = "Lizzo/Characters/Companions/shield_guard";
        private const string CaptainAddress = "Lizzo/Characters/Companions/shield_captain";
        private const string UnitPresentationSetPath = "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset";
        private const string SharedControllerPath = "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";
        private const string GuardPrefabPath = "Assets/_LizzoPV/Prefabs/Characters/Companions/ShieldGuard.prefab";
        private const string CaptainPrefabPath = "Assets/_LizzoPV/Prefabs/Characters/Companions/ShieldCaptain.prefab";
        private const string GuardLibraryPath = "Assets/_LizzoPV/Art/Characters/Companions/shield_guard_SpriteLibrary.asset";
        private const string CaptainLibraryPath = "Assets/_LizzoPV/Art/Characters/Companions/shield_captain_SpriteLibrary.asset";

        [Test]
        public void UnitPresentationSet_ResolvesBothCanonicalShieldEntries()
        {
            UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            Assert.IsNotNull(set);
            Assert.IsTrue(set.TryGetEntry(GuardId, out UnitPresentationSet.Entry guard));
            Assert.IsTrue(set.TryGetEntry(CaptainId, out UnitPresentationSet.Entry captain));

            Assert.AreEqual(GuardAddress, guard.AddressableKey);
            Assert.AreEqual(CaptainAddress, captain.AddressableKey);
            Assert.IsNotNull(guard.Prefab);
            Assert.IsNotNull(captain.Prefab);
            Assert.IsNotNull(guard.Portrait);
            Assert.IsNotNull(captain.Portrait);
            Assert.AreEqual("Idle_0", guard.Portrait.name);
            Assert.AreEqual("Idle_0", captain.Portrait.name);
        }

        [Test]
        public void PresentationCatalogProvider_RoutesBothShieldPrefabAddresses()
        {
            PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>("Assets/_LizzoPV/Data/Presentation/PresentationCatalog.asset");
            Assert.IsNotNull(catalog);

            Assert.IsTrue(PresentationCatalogProvider.TryGetUnit(catalog, GuardId, out UnitPresentationSet.Entry guard));
            Assert.IsTrue(PresentationCatalogProvider.TryGetUnit(catalog, CaptainId, out UnitPresentationSet.Entry captain));
            Assert.AreEqual(GuardAddress, guard.AddressableKey);
            Assert.AreEqual(CaptainAddress, captain.AddressableKey);
        }

        [Test]
        public void CanonicalShieldPrefabs_UseTheirAcceptedLibrariesAndOneSharedFourStateController()
        {
            RuntimeAnimatorController sharedController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SharedControllerPath);
            Assert.IsNotNull(sharedController);
            Assert.AreEqual(4, sharedController.animationClips.Length);
            AssertSharedStateContract(AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath));

            AssertCanonicalPrefab(GuardPrefabPath, GuardLibraryPath, sharedController);
            AssertCanonicalPrefab(CaptainPrefabPath, CaptainLibraryPath, sharedController);
        }

        [Test]
        public void CanonicalShieldPrefabs_HaveStableNonP0AddressableKeys()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings);

            AssertAddress(GuardPrefabPath, GuardAddress, settings);
            AssertAddress(CaptainPrefabPath, CaptainAddress, settings);
        }

        private static void AssertCanonicalPrefab(string prefabPath, string libraryPath, RuntimeAnimatorController sharedController)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            SpriteLibraryAsset expectedLibrary = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(expectedLibrary);

            Transform visual = prefab.transform.Find("Visual");
            Assert.IsNotNull(visual);
            SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
            SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            Animator animator = visual.GetComponent<Animator>();
            Assert.IsNotNull(library);
            Assert.IsNotNull(resolver);
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(animator);
            Assert.AreSame(expectedLibrary, library.spriteLibraryAsset);
            Assert.AreSame(sharedController, animator.runtimeAnimatorController);
            Assert.AreEqual("Idle", resolver.GetCategory());
            Assert.AreEqual("0", resolver.GetLabel());
            Assert.IsNotNull(renderer.sprite);
            Assert.AreEqual("Idle_0", renderer.sprite.name);
        }

        private static void AssertAddress(string prefabPath, string expectedAddress, AddressableAssetSettings settings)
        {
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            Assert.IsNotNull(entry);
            Assert.AreEqual(expectedAddress, entry.address);
            Assert.IsFalse(entry.address.StartsWith("P0/"));
        }

        private static void AssertSharedStateContract(AnimatorController controller)
        {
            Assert.IsNotNull(controller);
            Assert.AreEqual("Base Layer", controller.layers[0].name);
            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            Assert.AreEqual(4, states.Length);
            Assert.IsTrue(HasState(states, "Idle"));
            Assert.IsTrue(HasState(states, "Run"));
            Assert.IsTrue(HasState(states, "Attack"));
            Assert.IsTrue(HasState(states, "Death"));
        }

        private static bool HasState(ChildAnimatorState[] states, string stateName)
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                    return true;
            }

            return false;
        }
    }
}
