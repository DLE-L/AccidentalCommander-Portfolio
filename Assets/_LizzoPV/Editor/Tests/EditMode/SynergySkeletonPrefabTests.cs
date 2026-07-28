using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergySkeletonPrefabTests
    {
        private const string SynergyPath = "Assets/_LizzoPV/Prefabs/Characters/Supports/SynergySkeletonSummon.prefab";
        private const string PersonalPath = "Assets/_LizzoPV/Prefabs/Characters/Supports/PersonalSkeletonSummon.prefab";
        private const string SynergyAddress = "Lizzo/Characters/Supports/UNIT_SYNERGY_SKELETON_01";
        private const string PersonalAddress = "Lizzo/Characters/Supports/UNIT_PERSONAL_SKELETON_01";

        [Test]
        public void IndependentSynergySkeletonPrefab_HasCanonicalRuntimeVisualAndAddressableSeams()
        {
            GameObject personal = AssetDatabase.LoadAssetAtPath<GameObject>(PersonalPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SynergyPath);
            Assert.IsNotNull(personal);
            Assert.IsNotNull(prefab);
            Assert.AreEqual("SynergySkeletonSummon", prefab.name);
            Assert.AreNotEqual(AssetDatabase.AssetPathToGUID(PersonalPath), AssetDatabase.AssetPathToGUID(SynergyPath));
            Assert.AreEqual("cd6e8719e4fff074980cdd6dfe86074d", AssetDatabase.AssetPathToGUID(PersonalPath));

            SynergySkeletonRuntime runtime = prefab.GetComponent<SynergySkeletonRuntime>();
            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            UnitColliderRefs refs = prefab.GetComponent<UnitColliderRefs>();
            HitFlash flash = prefab.GetComponent<HitFlash>();
            Assert.IsNotNull(runtime); Assert.IsNotNull(body); Assert.IsNotNull(refs); Assert.IsNotNull(flash);
            Assert.IsNotNull(refs.BodyCollider); Assert.IsNotNull(refs.CombatCollider);
            Assert.IsFalse(refs.BodyCollider.isTrigger); Assert.IsTrue(refs.CombatCollider.isTrigger);
            Assert.IsNull(prefab.GetComponent<PersonalSummonRuntime>());
            Assert.IsNull(prefab.GetComponent<CompanionRuntime>());

            Transform visual = prefab.transform.Find("Visual");
            Transform personalVisual = personal.transform.Find("Visual");
            Assert.IsNotNull(visual); Assert.AreEqual(1, prefab.transform.childCount); Assert.IsNotNull(personalVisual);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            Animator animator = visual.GetComponent<Animator>();
            SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
            SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
            UnitVisualDriver driver = visual.GetComponent<UnitVisualDriver>();
            Assert.IsNotNull(renderer); Assert.IsNotNull(animator); Assert.IsNotNull(library); Assert.IsNotNull(resolver); Assert.IsNotNull(driver);
            Assert.AreSame(personalVisual.GetComponent<Animator>().runtimeAnimatorController, animator.runtimeAnimatorController);
            Assert.AreSame(personalVisual.GetComponent<SpriteLibrary>().spriteLibraryAsset, library.spriteLibraryAsset);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings);
            AddressableAssetEntry synergyEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(SynergyPath));
            AddressableAssetEntry personalEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(PersonalPath));
            Assert.IsNotNull(synergyEntry); Assert.AreEqual(SynergyAddress, synergyEntry.address);
            Assert.IsNotNull(personalEntry); Assert.AreEqual(PersonalAddress, personalEntry.address);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                TestAssetService assets = new TestAssetService();
                assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
                LocalDataProvider provider = new LocalDataProvider(assets);
                Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
                Assert.IsTrue(instance.GetComponent<SynergySkeletonRuntime>().Configure(provider.GetSynergySummon("UNIT_SYNERGY_SKELETON_01"), 0.0f, new CombatImmediateHitModule()));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
