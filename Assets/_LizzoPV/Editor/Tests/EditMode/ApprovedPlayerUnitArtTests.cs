using System.Collections.Generic;
using System.Linq;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.EditorTests
{
    public sealed class ApprovedPlayerUnitArtTests
    {
        private const string ArtRoot =
            "Assets/_LizzoPV/Gameplay/Presentation/Art/Characters/ApprovedPlayerUnits";
        private const string ApprovedControllerPath =
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Approved/ApprovedPlayerUnitIdle.controller";
        private const string SharedControllerPath =
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Shared/CompanionSpriteShared.controller";

        private static readonly ApprovedFixture[] ApprovedFixtures =
        {
            new ApprovedFixture(
                "Commander",
                "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab",
                1.0f,
                true),
            new ApprovedFixture(
                "SwordSoldier",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab",
                0.6f,
                false),
            new ApprovedFixture(
                "FalconArcher",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherBaseMemberView.prefab",
                0.6f,
                false),
            new ApprovedFixture(
                "Cleric",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericBaseMemberView.prefab",
                0.6f,
                false),
            new ApprovedFixture(
                "ShieldGuard",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardBaseMemberView.prefab",
                0.6f,
                false),
            new ApprovedFixture(
                "Bombardier",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierBaseMemberView.prefab",
                0.6f,
                false),
            new ApprovedFixture(
                "SkeletonScytheThrower",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonScytheThrowerBaseMemberView.prefab",
                0.6f,
                false),
            new ApprovedFixture(
                "GreyWolf",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Supports/GreyWolfSupport.prefab",
                0.3f,
                false),
        };

        private static readonly ScaleFixture[] BaseScaleFixtures =
        {
            new ScaleFixture("ShieldGuard", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardBaseMemberView.prefab", 0.6f),
            new ScaleFixture("SwordSoldier", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab", 0.6f),
            new ScaleFixture("Cleric", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericBaseMemberView.prefab", 0.6f),
            new ScaleFixture("Bombardier", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierBaseMemberView.prefab", 0.6f),
            new ScaleFixture("FireMage", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageBaseMemberView.prefab", 0.6f),
            new ScaleFixture("FalconArcher", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherBaseMemberView.prefab", 0.6f),
            new ScaleFixture("SkeletonScytheThrower", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonScytheThrowerBaseMemberView.prefab", 0.6f),
            new ScaleFixture("WolfTamer", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerBaseMemberView.prefab", 0.6f),
        };

        private static readonly ScaleFixture[] PromotedScaleFixtures =
        {
            new ScaleFixture("ShieldGuardPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardPromotedMemberView.prefab", 0.8f),
            new ScaleFixture("SwordCaptain", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordCaptainMemberView.prefab", 0.8f),
            new ScaleFixture("ClericPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericPromotedMemberView.prefab", 0.8f),
            new ScaleFixture("BombardierPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierPromotedMemberView.prefab", 0.8f),
            new ScaleFixture("FireMagePromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMagePromotedMemberView.prefab", 0.8f),
            new ScaleFixture("FalconArcherPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherPromotedMemberView.prefab", 0.8f),
            new ScaleFixture("SkeletonReaperPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonReaperPromotedMemberView.prefab", 0.8f),
            new ScaleFixture("WolfTamerPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerPromotedMemberView.prefab", 0.8f),
        };

        [Test]
        public void ApprovedSheets_ContainOnlyEightVisibleIdleFrames()
        {
            foreach (ApprovedFixture fixture in ApprovedFixtures)
            {
                SpriteLibraryAsset library = LoadRequired<SpriteLibraryAsset>(
                    ArtRoot + "/" + fixture.Role + "_SpriteLibrary.asset");
                Assert.That(library.GetCategoryNames(), Is.EquivalentTo(new[] { "Idle" }), fixture.Role);
                Assert.That(
                    library.GetCategoryLabelNames("Idle"),
                    Is.EquivalentTo(Enumerable.Range(0, 8).Select(index => index.ToString())),
                    fixture.Role);

                for (int frame = 0; frame < 8; frame += 1)
                {
                    Sprite sprite = library.GetSprite("Idle", frame.ToString());
                    Assert.That(sprite, Is.Not.Null, fixture.Role + " frame " + frame);
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(128.0f, 128.0f)), fixture.Role);
                    Assert.That(MaxAlpha(sprite), Is.GreaterThan(0.0f), fixture.Role + " frame " + frame);
                }
            }
        }

        [Test]
        public void ApprovedPrefabs_UseIdleOnlyProductionArtAndFixedScale()
        {
            RuntimeAnimatorController controller = LoadRequired<RuntimeAnimatorController>(ApprovedControllerPath);
            Assert.That(controller.animationClips.Length, Is.EqualTo(1));
            Assert.That(controller.animationClips[0].name, Does.EndWith("_Idle"));

            foreach (ApprovedFixture fixture in ApprovedFixtures)
            {
                GameObject prefab = LoadRequired<GameObject>(fixture.PrefabPath);
                UnitVisualDriver driver = prefab.GetComponentInChildren<UnitVisualDriver>(true);
                Assert.That(driver, Is.Not.Null, fixture.Role);
                Transform visual = driver.transform;
                SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
                Animator animator = visual.GetComponent<Animator>();
                Assert.That(library, Is.Not.Null, fixture.Role);
                Assert.That(animator, Is.Not.Null, fixture.Role);
                Assert.That(
                    AssetDatabase.GetAssetPath(library.spriteLibraryAsset),
                    Is.EqualTo(ArtRoot + "/" + fixture.Role + "_SpriteLibrary.asset"),
                    fixture.Role);
                Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller), fixture.Role);
                Assert.That(driver.IdleFrameCount, Is.EqualTo(8), fixture.Role);
                Assert.That(visual.localScale, Is.EqualTo(new Vector3(fixture.Scale, fixture.Scale, 1.0f)), fixture.Role);
                Assert.That(
                    visual.GetComponent<CommanderIdleSpriteOverride>() != null,
                    Is.EqualTo(fixture.HasCommanderOverride),
                    fixture.Role);
            }
        }

        [Test]
        public void MissingApprovedSheets_KeepExistingArtAndFullAnimationContract()
        {
            RuntimeAnimatorController sharedController = LoadRequired<RuntimeAnimatorController>(SharedControllerPath);
            Assert.That(sharedController.animationClips.Length, Is.EqualTo(4));

            AssertExistingArt(
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordCaptainMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/sword_captain_SpriteLibrary.asset",
                sharedController);
            AssertExistingArt(
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/light_guide_SpriteLibrary.asset",
                sharedController);
            AssertExistingArt(
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/falcon_captain_SpriteLibrary.asset",
                sharedController);
        }

        [Test]
        public void LegionScales_AreFixedByProgressionTier()
        {
            foreach (ScaleFixture fixture in BaseScaleFixtures)
                AssertVisualScale(fixture);
            foreach (ScaleFixture fixture in PromotedScaleFixtures)
                AssertVisualScale(fixture);
        }

        [Test]
        public void ApprovedIdlePhase_IsDistributedPerInstance()
        {
            GameObject prefab = LoadRequired<GameObject>(ApprovedFixtures[1].PrefabPath);
            var phases = new HashSet<int>();
            var instances = new List<GameObject>();
            try
            {
                for (int index = 0; index < 8; index += 1)
                {
                    GameObject instance = Object.Instantiate(prefab);
                    instances.Add(instance);
                    UnitVisualDriver driver = instance.GetComponentInChildren<UnitVisualDriver>(true);
                    Assert.That(driver.UsesIdleOnlyAnimation, Is.True);
                    Assert.That(driver.IdlePhaseOffset, Is.InRange(0.0f, UnitVisualDriver.MaxIdlePhaseOffset));
                    phases.Add(Mathf.RoundToInt(driver.IdlePhaseOffset * 10000.0f));
                }

                Assert.That(phases.Count, Is.GreaterThan(1));
            }
            finally
            {
                foreach (GameObject instance in instances)
                    Object.DestroyImmediate(instance);
            }
        }

        private static void AssertExistingArt(
            string prefabPath,
            string expectedLibraryPath,
            RuntimeAnimatorController expectedController)
        {
            GameObject prefab = LoadRequired<GameObject>(prefabPath);
            UnitVisualDriver driver = prefab.GetComponentInChildren<UnitVisualDriver>(true);
            Assert.That(driver, Is.Not.Null, prefabPath);
            SpriteLibrary library = driver.GetComponent<SpriteLibrary>();
            Animator animator = driver.GetComponent<Animator>();
            Assert.That(library, Is.Not.Null, prefabPath);
            Assert.That(animator, Is.Not.Null, prefabPath);
            Assert.That(AssetDatabase.GetAssetPath(library.spriteLibraryAsset), Is.EqualTo(expectedLibraryPath));
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(expectedController));
        }

        private static void AssertVisualScale(ScaleFixture fixture)
        {
            GameObject prefab = LoadRequired<GameObject>(fixture.PrefabPath);
            UnitVisualDriver driver = prefab.GetComponentInChildren<UnitVisualDriver>(true);
            Assert.That(driver, Is.Not.Null, fixture.Role);
            Assert.That(
                driver.transform.localScale,
                Is.EqualTo(new Vector3(fixture.Scale, fixture.Scale, 1.0f)),
                fixture.Role);
        }

        private static float MaxAlpha(Sprite sprite)
        {
            Rect rect = sprite.textureRect;
            Color[] pixels = sprite.texture.GetPixels(
                Mathf.RoundToInt(rect.x),
                Mathf.RoundToInt(rect.y),
                Mathf.RoundToInt(rect.width),
                Mathf.RoundToInt(rect.height));
            float maxAlpha = 0.0f;
            foreach (Color pixel in pixels)
                maxAlpha = Mathf.Max(maxAlpha, pixel.a);
            return maxAlpha;
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private readonly struct ApprovedFixture
        {
            public ApprovedFixture(string role, string prefabPath, float scale, bool hasCommanderOverride)
            {
                Role = role;
                PrefabPath = prefabPath;
                Scale = scale;
                HasCommanderOverride = hasCommanderOverride;
            }

            public string Role { get; }
            public string PrefabPath { get; }
            public float Scale { get; }
            public bool HasCommanderOverride { get; }
        }

        private readonly struct ScaleFixture
        {
            public ScaleFixture(string role, string prefabPath, float scale)
            {
                Role = role;
                PrefabPath = prefabPath;
                Scale = scale;
            }

            public string Role { get; }
            public string PrefabPath { get; }
            public float Scale { get; }
        }
    }
}
