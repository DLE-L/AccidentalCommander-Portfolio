using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionPromotionFormationTests
    {
        private const string SharedControllerPath = "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";

        private static readonly Pair[] Pairs =
        {
            new Pair("ShieldGuard", "ShieldCaptain", "shield_guard"),
            new Pair("SwordSoldier", "SwordCaptain", "sword_soldier"),
            new Pair("Cleric", "LightGuide", "cleric"),
            new Pair("FalconArcher", "FalconCaptain", "falcon_archer"),
            new Pair("FieldHerbalist", "BattleApothecary", "field_herbalist"),
            new Pair("Bombardier", "PowderCaptain", "bombardier"),
            new Pair("FireMage", "FireSage", "fire_mage"),
            new Pair("LightningMage", "StormMage", "lightning_mage"),
            new Pair("WolfTamer", "BeastCommander", "wolf_tamer"),
            new Pair("WraithKnight", "WraithGuardian", "wraith_knight"),
            new Pair("Necromancer", "DarkRitualist", "necromancer"),
            new Pair("SkeletonBomber", "BoneArtillery", "skeleton_bomber"),
        };

        [Test]
        public void PromotedPrefabs_HaveTwoBaseLibrarySupports_AndBasesHaveNone()
        {
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SharedControllerPath);
            Assert.IsNotNull(controller);
            Assert.AreEqual(4, controller.animationClips.Length);

            foreach (Pair pair in Pairs)
            {
                GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.BasePath);
                GameObject promotedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.PromotedPath);
                Assert.IsNotNull(basePrefab, pair.BasePrefabName);
                Assert.IsNull(basePrefab.transform.Find("SupportVisuals"), pair.BasePrefabName);

                Transform captainVisual = promotedPrefab.transform.Find("Visual");
                UnitVisualDriver captainDriver = captainVisual.GetComponent<UnitVisualDriver>();
                SpriteRenderer captainRenderer = captainVisual.GetComponent<SpriteRenderer>();
                Transform supportRoot = promotedPrefab.transform.Find("SupportVisuals");
                Assert.IsNotNull(supportRoot, pair.PromotedPrefabName);
                Assert.AreEqual(2, supportRoot.childCount, pair.PromotedPrefabName);
                Assert.AreEqual(2, captainDriver.LinkedDriverCount, pair.PromotedPrefabName);

                for (int index = 0; index < 2; index++)
                    AssertSupport(pair, supportRoot.Find("Support_" + index.ToString("00")), index, controller, captainRenderer);
            }
        }

        [Test]
        public void LinkedDrivers_ForwardState_AndGuardAgainstCycles()
        {
            GameObject captainRoot = CreateDriverObject("Captain");
            GameObject supportRoot = CreateDriverObject("Support");
            try
            {
                UnitVisualDriver captainDriver = captainRoot.transform.Find("Visual").GetComponent<UnitVisualDriver>();
                UnitVisualDriver supportDriver = supportRoot.transform.Find("Visual").GetComponent<UnitVisualDriver>();
                captainDriver.SetLinkedDriversForPresentation(new[] { supportDriver });
                supportDriver.SetLinkedDriversForPresentation(new[] { captainDriver });

                captainDriver.SetMoving(true);
                captainDriver.SetDead(true);
                captainDriver.FaceDirection(Vector3.left);
                captainDriver.PlayAttack(Vector3.left, 0.50f);

                Assert.IsTrue(ReadPrivate<bool>(supportDriver, "_isMoving"));
                Assert.IsTrue(ReadPrivate<bool>(supportDriver, "_isDead"));
                Assert.AreEqual(0.50f, ReadPrivate<float>(supportDriver, "_attackDuration"), 0.0001f);
                Assert.IsTrue(supportRoot.transform.Find("Visual").GetComponent<SpriteRenderer>().flipX);
            }
            finally
            {
                Object.DestroyImmediate(captainRoot);
                Object.DestroyImmediate(supportRoot);
            }
        }

        [Test]
        public void PromotedPrefabs_ResolvedIdleSpritesRemainAuthored()
        {
            foreach (Pair pair in Pairs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.PromotedPath);
                Assert.IsNotNull(prefab, pair.PromotedPrefabName);
                GameObject instance = Object.Instantiate(prefab);
                try
                {
                    SpriteRenderer captain = instance.transform.Find("Visual").GetComponent<SpriteRenderer>();
                    SpriteRenderer support0 = instance.transform.Find("SupportVisuals/Support_00").GetComponent<SpriteRenderer>();
                    SpriteRenderer support1 = instance.transform.Find("SupportVisuals/Support_01").GetComponent<SpriteRenderer>();
                    Assert.That(captain.sprite, Is.Not.Null, pair.PromotedPrefabName);
                    Assert.That(support0.sprite, Is.Not.Null, pair.PromotedPrefabName);
                    Assert.That(support1.sprite, Is.Not.Null, pair.PromotedPrefabName);

                    Assert.AreEqual(new Vector3(0.38f, 0.38f, 1.0f), instance.transform.Find("SupportVisuals/Support_00").localScale);
                    Assert.AreEqual(new Vector3(0.38f, 0.38f, 1.0f), instance.transform.Find("SupportVisuals/Support_01").localScale);
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void ShieldCaptain_FormationAndResolvedIdleSpritesRemainAuthored()
        {
            Pair pair = Pairs[0];
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SharedControllerPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.PromotedPath);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(prefab, pair.PromotedPrefabName);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                SpriteRenderer captain = instance.transform.Find("Visual").GetComponent<SpriteRenderer>();
                Transform supportRoot = instance.transform.Find("SupportVisuals");
                AssertSupport(pair, supportRoot.Find("Support_00"), 0, controller, captain);
                AssertSupport(pair, supportRoot.Find("Support_01"), 1, controller, captain);

                Assert.AreEqual(new Vector3(0.38f, 0.38f, 1.0f), supportRoot.Find("Support_00").localScale);
                Assert.AreEqual(new Vector3(0.38f, 0.38f, 1.0f), supportRoot.Find("Support_01").localScale);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PromotedPrefabs_SharedPresentationClipsPreserveSupportAuthoringWhenSampled()
        {
            AnimationClip[] clips =
            {
                AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared_Idle.anim"),
                AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared_Run.anim"),
                AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared_Attack.anim"),
                AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared_Death.anim"),
            };
            for (int pairIndex = 0; pairIndex < Pairs.Length; pairIndex++)
            {
                Pair pair = Pairs[pairIndex];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.PromotedPath);
                Assert.IsNotNull(prefab, pair.PromotedPrefabName);
                for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
                {
                    Assert.IsNotNull(clips[clipIndex]);
                    GameObject instance = Object.Instantiate(prefab);
                    try
                    {
                        for (int supportIndex = 0; supportIndex < 2; supportIndex++)
                        {
                            Transform support = instance.transform.Find("SupportVisuals/Support_" + supportIndex.ToString("00"));
                            Vector3 authoredPosition = supportIndex == 0 ? pair.SupportPosition00 : pair.SupportPosition01;
                            Assert.AreEqual(authoredPosition, support.localPosition, pair.PromotedPrefabName + " " + clips[clipIndex].name);

                            clips[clipIndex].SampleAnimation(support.gameObject, 0.0f);

                            Assert.That(support.localPosition.x, Is.EqualTo(authoredPosition.x).Within(0.0001f), pair.PromotedPrefabName + " " + clips[clipIndex].name + " Support_" + supportIndex.ToString("00") + " x");
                            Assert.That(support.localPosition.y, Is.EqualTo(authoredPosition.y).Within(0.0001f), pair.PromotedPrefabName + " " + clips[clipIndex].name + " Support_" + supportIndex.ToString("00") + " y");
                            Assert.That(support.localPosition.z, Is.EqualTo(authoredPosition.z).Within(0.0001f), pair.PromotedPrefabName + " " + clips[clipIndex].name + " Support_" + supportIndex.ToString("00") + " z");
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(instance);
                    }
                }
            }
        }

        private static void AssertSupport(Pair pair, Transform support, int index, RuntimeAnimatorController controller, SpriteRenderer captainRenderer)
        {
            Assert.IsNotNull(support, pair.PromotedPrefabName);
            Assert.AreEqual(index == 0 ? pair.SupportPosition00 : pair.SupportPosition01, support.localPosition);
            Assert.AreEqual(pair.SupportScale, support.localScale);
            Assert.IsNull(support.GetComponent<Rigidbody2D>());
            Assert.AreEqual(0, support.GetComponents<Collider2D>().Length);

            SpriteRenderer renderer = support.GetComponent<SpriteRenderer>();
            Animator animator = support.GetComponent<Animator>();
            SpriteLibrary library = support.GetComponent<SpriteLibrary>();
            SpriteResolver resolver = support.GetComponent<SpriteResolver>();
            UnitVisualDriver driver = support.GetComponent<UnitVisualDriver>();
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(animator);
            Assert.IsNotNull(library);
            Assert.IsNotNull(resolver);
            Assert.IsNotNull(driver);
            Assert.AreEqual(0, driver.LinkedDriverCount);
            Assert.AreEqual(captainRenderer.sortingLayerID, renderer.sortingLayerID);
            Assert.AreEqual(captainRenderer.sortingOrder - 1, renderer.sortingOrder);
            Assert.AreSame(controller, animator.runtimeAnimatorController);
            Assert.AreEqual("Idle", resolver.GetCategory());
            Assert.AreEqual("0", resolver.GetLabel());
            Assert.IsNotNull(renderer.sprite);
            Assert.AreEqual("Idle_0", renderer.sprite.name);
            Assert.AreEqual("Assets/_LizzoPV/Art/Characters/Companions/" + pair.BaseUnitId + "_SpriteLibrary.asset", AssetDatabase.GetAssetPath(library.spriteLibraryAsset));
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(support.gameObject));
        }

        private static GameObject CreateDriverObject(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Prefabs/Characters/Companions/ShieldGuard.prefab");
            Assert.IsNotNull(prefab);

            GameObject instance = Object.Instantiate(prefab);
            instance.name = name;
            instance.SetActive(true);
            return instance;
        }

        private static T ReadPrivate<T>(UnitVisualDriver driver, string fieldName)
        {
            FieldInfo field = typeof(UnitVisualDriver).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (T)field.GetValue(driver);
        }

        private readonly struct Pair
        {
            public readonly string BasePrefabName;
            public readonly string PromotedPrefabName;
            public readonly string BaseUnitId;
            public readonly Vector3 SupportPosition00;
            public readonly Vector3 SupportPosition01;
            public readonly Vector3 SupportScale;

            public Pair(string basePrefabName, string promotedPrefabName, string baseUnitId)
            {
                BasePrefabName = basePrefabName;
                PromotedPrefabName = promotedPrefabName;
                BaseUnitId = baseUnitId;
                SupportPosition00 = new Vector3(-0.60f, -0.35f, 0.0f);
                SupportPosition01 = new Vector3(0.60f, -0.35f, 0.0f);
                SupportScale = new Vector3(0.38f, 0.38f, 1.0f);
            }

            public string BasePath => "Assets/_LizzoPV/Prefabs/Characters/Companions/" + BasePrefabName + ".prefab";
            public string PromotedPath => "Assets/_LizzoPV/Prefabs/Characters/Companions/" + PromotedPrefabName + ".prefab";
        }
    }
}
