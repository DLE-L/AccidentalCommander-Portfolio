using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Prototypes.PV48.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Prototypes.PV48.Editor.Tests
{
    public sealed class PV48ArtIntegrationPrototypeTests
    {
        private static readonly Definition[] Definitions =
        {
            new("Commander", "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab", 1.0f),
            new("SwordSoldier", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab", 0.6f),
            new("FalconArcher", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherBaseMemberView.prefab", 0.6f),
            new("Cleric", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericBaseMemberView.prefab", 0.6f),
            new("SwordSoldierLegacyRuntime", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Companions/SwordSoldier.prefab", 0.6f),
            new("FalconArcherLegacyRuntime", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Companions/FalconArcher.prefab", 0.6f),
            new("ClericLegacyRuntime", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Companions/Cleric.prefab", 0.6f),
        };

        private static readonly Definition[] FixedScaleDefinitions =
        {
            new("ShieldGuard", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardBaseMemberView.prefab", 0.6f),
            new("SwordSoldier", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab", 0.6f),
            new("Cleric", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericBaseMemberView.prefab", 0.6f),
            new("Bombardier", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierBaseMemberView.prefab", 0.6f),
            new("FireMage", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageBaseMemberView.prefab", 0.6f),
            new("FalconArcher", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherBaseMemberView.prefab", 0.6f),
            new("SkeletonBomber", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonBomberBaseMemberView.prefab", 0.6f),
            new("WolfTamer", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerBaseMemberView.prefab", 0.6f),
            new("ShieldGuardPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardPromotedMemberView.prefab", 0.8f),
            new("SwordCaptain", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordCaptainMemberView.prefab", 0.8f),
            new("ClericPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericPromotedMemberView.prefab", 0.8f),
            new("BombardierPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierPromotedMemberView.prefab", 0.8f),
            new("FireMagePromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMagePromotedMemberView.prefab", 0.8f),
            new("FalconArcherPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherPromotedMemberView.prefab", 0.8f),
            new("SkeletonBomberPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonBomberPromotedMemberView.prefab", 0.8f),
            new("WolfTamerPromoted", "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerPromotedMemberView.prefab", 0.8f),
        };

        private static readonly string[] Categories = { "Idle" };

        [Test]
        public void FourRepresentativeBodies_ResolveAllEightFramesWithFixedScaleContract()
        {
            foreach (Definition definition in Definitions)
                AssertRepresentativeBody(definition);
        }

        [Test]
        public void ActiveCompanionMemberViews_UseFixedBaseAndPromotedScale()
        {
            foreach (Definition definition in FixedScaleDefinitions)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
                Assert.IsNotNull(prefab, definition.Role);
                UnitVisualDriver[] drivers = prefab.GetComponentsInChildren<UnitVisualDriver>(true);
                Assert.AreEqual(1, drivers.Length, definition.Role);
                Assert.That(drivers[0].transform.localScale.x, Is.EqualTo(definition.ExpectedScale).Within(0.0001f), definition.Role);
                Assert.That(drivers[0].transform.localScale.y, Is.EqualTo(definition.ExpectedScale).Within(0.0001f), definition.Role);
            }
        }

        private static void AssertRepresentativeBody(Definition definition)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
            Assert.IsNotNull(prefab, definition.Role);

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                UnitVisualDriver[] drivers = instance.GetComponentsInChildren<UnitVisualDriver>(true);
                Assert.AreEqual(1, drivers.Length, definition.Role);
                UnitVisualDriver driver = drivers[0];
                Assert.AreEqual("Visual", driver.gameObject.name, definition.Role);
                Assert.That(driver.transform.localScale.x, Is.EqualTo(definition.ExpectedScale).Within(0.0001f), definition.Role);
                Assert.That(driver.transform.localScale.y, Is.EqualTo(definition.ExpectedScale).Within(0.0001f), definition.Role);
                Assert.AreEqual(8, driver.IdleFrameCount, definition.Role);

                SpriteRenderer renderer = driver.GetComponent<SpriteRenderer>();
                SpriteResolver resolver = driver.GetComponent<SpriteResolver>();
                SpriteLibrary library = driver.GetComponent<SpriteLibrary>();
                Animator animator = driver.GetComponent<Animator>();
                Assert.IsNotNull(renderer, definition.Role);
                Assert.IsNotNull(resolver, definition.Role);
                Assert.IsNotNull(library, definition.Role);
                Assert.IsNotNull(animator, definition.Role);
                Assert.IsNotNull(animator.runtimeAnimatorController, definition.Role);
                CollectionAssert.AreEqual(Categories, library.spriteLibraryAsset.GetCategoryNames().ToArray(), definition.Role);
                if (string.Equals(definition.Role, "Commander", StringComparison.Ordinal))
                {
                    PV48CommanderVisualOverride visualOverride = driver.GetComponent<PV48CommanderVisualOverride>();
                    Assert.IsNotNull(visualOverride, definition.Role);
                    Assert.IsNotNull(visualOverride.SpriteLibrary, definition.Role);
                    Assert.That(visualOverride.CycleSeconds, Is.EqualTo(8.0f / 9.0f).Within(0.0001f), definition.Role);
                }

                foreach (string category in Categories)
                {
                    for (int frame = 0; frame < 8; frame++)
                    {
                        resolver.SetCategoryAndLabel(category, frame.ToString());
                        Assert.IsTrue(resolver.ResolveSpriteToSpriteRenderer(), $"{definition.Role} {category}/{frame}");
                        Assert.IsNotNull(renderer.sprite, $"{definition.Role} {category}/{frame}");
                        Assert.AreEqual(128.0f, renderer.sprite.rect.width, $"{definition.Role} {category}/{frame}");
                        Assert.AreEqual(128.0f, renderer.sprite.rect.height, $"{definition.Role} {category}/{frame}");
                        Assert.Greater(MaxAlpha(renderer.sprite), 0.0f, $"{definition.Role} {category}/{frame}");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CommanderAndCompanionControllers_ExposeIdleOnly()
        {
            AssertIdleOnlyController("Assets/_LizzoPV/Gameplay/Commander/Animations/Compatibility/Commander.controller");
            AssertIdleOnlyController("Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared/CompanionSpriteShared.controller");
        }

        [Test]
        public void RepeatedLegionInstances_UseDistinctBoundedIdlePhases()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab");
            Assert.IsNotNull(prefab);

            var instances = new List<GameObject>(4);
            var phases = new HashSet<float>();
            try
            {
                for (int index = 0; index < 4; index++)
                {
                    GameObject instance = UnityEngine.Object.Instantiate(prefab);
                    instances.Add(instance);
                    UnitVisualDriver driver = instance.GetComponentInChildren<UnitVisualDriver>(true);
                    Assert.IsNotNull(driver);
                    Assert.That(driver.IdlePhaseOffset, Is.InRange(0.0f, UnitVisualDriver.MaxIdlePhaseOffset));
                    phases.Add(driver.IdlePhaseOffset);
                }

                Assert.That(phases.Count, Is.GreaterThan(1));
            }
            finally
            {
                foreach (GameObject instance in instances)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void IdleOnlyLegionController_FallsBackToIdleForGameplayVisualCues()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab");
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                UnitVisualDriver driver = instance.GetComponentInChildren<UnitVisualDriver>(true);
                Assert.IsNotNull(driver);
                Animator animator = driver.GetComponent<Animator>();
                Assert.IsNotNull(animator);
                animator.Rebind();
                animator.Update(0.0f);
                Assert.IsTrue(driver.UsesIdleOnlyAnimation);

                MethodInfo resolveState = typeof(UnitVisualDriver).GetMethod(
                    "ResolveAvailableStateHash",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo initializeHashes = typeof(UnitVisualDriver).GetMethod(
                    "InitializeStateHashes",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(resolveState);
                Assert.IsNotNull(initializeHashes);
                initializeHashes.Invoke(driver, null);
                int idleHash = Animator.StringToHash("Base Layer.Idle");

                driver.SetMoving(true);
                Assert.AreEqual(idleHash, (int)resolveState.Invoke(driver, null));
                driver.PlayAttack(Vector3.right, 0.28f);
                Assert.AreEqual(idleHash, (int)resolveState.Invoke(driver, null));
                driver.SetDead(true);
                Assert.AreEqual(idleHash, (int)resolveState.Invoke(driver, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AssertIdleOnlyController(string path)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            Assert.IsNotNull(controller, path);
            Assert.AreEqual(1, controller.animationClips.Length, path);
            CollectionAssert.AreEqual(new[] { "Idle" },
                Array.ConvertAll(controller.layers[0].stateMachine.states, child => child.state.name), path);
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
            for (int i = 0; i < pixels.Length; i++)
                maxAlpha = Mathf.Max(maxAlpha, pixels[i].a);
            return maxAlpha;
        }

        private sealed class Definition
        {
            public Definition(string role, string prefabPath, float expectedScale)
            {
                Role = role;
                PrefabPath = prefabPath;
                ExpectedScale = expectedScale;
            }

            public string Role { get; }
            public string PrefabPath { get; }
            public float ExpectedScale { get; }
        }
    }
}
