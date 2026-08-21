using Lizzo.PV.Editor.CompanionRuntime;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionPresentationTracerTests
    {
        private const string SharedControllerPath =
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared/CompanionSpriteShared.controller";

        private static readonly LineageFixture[] Lineages =
        {
            new LineageFixture(
                "shield_guard",
                3,
                "Assets/_LizzoPV/Art/Characters/Companions/shield_guard_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/shield_captain_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardSquadRoot.prefab"),
            new LineageFixture(
                "sword_soldier",
                4,
                "Assets/_LizzoPV/Art/Characters/Companions/sword_soldier_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/sword_captain_SpriteLibrary.asset",
                CompanionPresentationTracerAuthoring.BaseMemberPrefabPath,
                CompanionPresentationTracerAuthoring.PromotedMemberPrefabPath,
                CompanionPresentationTracerAuthoring.SquadRootPrefabPath),
            new LineageFixture(
                "cleric",
                4,
                "Assets/_LizzoPV/Art/Characters/Companions/cleric_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/light_guide_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericSquadRoot.prefab"),
            new LineageFixture(
                "bombardier",
                4,
                "Assets/_LizzoPV/Art/Characters/Companions/bombardier_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/powder_captain_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierSquadRoot.prefab"),
            new LineageFixture(
                "fire_mage",
                4,
                "Assets/_LizzoPV/Art/Characters/Companions/fire_mage_SpriteLibrary.asset",
                "Assets/_LizzoPV/Art/Characters/Companions/fire_sage_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMagePromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageSquadRoot.prefab")
        };

        [Test]
        public void FiveLineagePrefabs_AreThinAndUseCanonicalVisualAssets()
        {
            RuntimeAnimatorController controller = LoadRequired<RuntimeAnimatorController>(SharedControllerPath);
            for (int lineageIndex = 0; lineageIndex < Lineages.Length; lineageIndex += 1)
            {
                LineageFixture lineage = Lineages[lineageIndex];
                GameObject basePrefab = LoadRequired<GameObject>(lineage.BasePrefabPath);
                GameObject promotedPrefab = LoadRequired<GameObject>(lineage.PromotedPrefabPath);
                GameObject squadPrefab = LoadRequired<GameObject>(lineage.SquadPrefabPath);
                SpriteLibraryAsset baseLibrary = LoadRequired<SpriteLibraryAsset>(lineage.BaseLibraryPath);
                SpriteLibraryAsset promotedLibrary = LoadRequired<SpriteLibraryAsset>(lineage.PromotedLibraryPath);

                AssertThinMemberPrefab(basePrefab, baseLibrary, controller, false, lineage.AttackFrameCount);
                AssertThinMemberPrefab(promotedPrefab, promotedLibrary, controller, true, lineage.AttackFrameCount);

                CompanionSquadRoot squadRoot = squadPrefab.GetComponent<CompanionSquadRoot>();
                Assert.That(squadRoot, Is.Not.Null, lineage.CompanionId);
                Assert.That(squadRoot.CompanionId, Is.EqualTo(lineage.CompanionId));
                Assert.That(squadRoot.MemberViewCount, Is.EqualTo(3));
                Assert.That(squadPrefab.GetComponentsInChildren<CompanionMemberView>(true).Length, Is.EqualTo(3));
                for (int memberIndex = 0; memberIndex < squadRoot.MemberViewCount; memberIndex += 1)
                {
                    CompanionMemberView view = squadRoot.GetMemberView(memberIndex);
                    Assert.That(view, Is.Not.Null);
                    Assert.That(view.MemberOrder, Is.EqualTo(memberIndex));
                    Assert.That(view.IsPromotedLeaderVisual, Is.EqualTo(memberIndex == 2));
                    Assert.That(view.gameObject.activeSelf, Is.EqualTo(memberIndex == 0));
                }

                AssertNoObsoleteRuntimeOwnership(squadPrefab);
            }
        }

        [Test]
        public void FiveLineageSnapshotFlow_DrivesOneTwoAndPromotedThreeMemberViews()
        {
            for (int lineageIndex = 0; lineageIndex < Lineages.Length; lineageIndex += 1)
            {
                LineageFixture lineage = Lineages[lineageIndex];
                GameObject prefab = LoadRequired<GameObject>(lineage.SquadPrefabPath);
                GameObject instance = Object.Instantiate(prefab);
                try
                {
                    CompanionSquadRoot root = instance.GetComponent<CompanionSquadRoot>();
                    Assert.That(root, Is.Not.Null);

                    SquadSnapshot one = CreateSnapshot(
                        lineage.CompanionId,
                        false,
                        SquadActionPhase.Idle,
                        -1,
                        new CompanionPoint(1.0f, 2.0f),
                        new CompanionPoint(1.0f, 2.0f),
                        null,
                        new CompanionMemberSnapshot(0, false, CompanionPoint.Zero));
                    Assert.That(root.TryApplySnapshot(in one), Is.True);
                    Assert.That(ActiveViewCount(root), Is.EqualTo(1));
                    AssertPoint(root.transform.localPosition, 1.0f, 2.0f);
                    AssertPoint(root.GetMemberView(0).transform.localPosition, 0.0f, 0.0f);

                    SquadSnapshot two = CreateSnapshot(
                        lineage.CompanionId,
                        false,
                        SquadActionPhase.Idle,
                        -1,
                        new CompanionPoint(-1.0f, 1.2f),
                        new CompanionPoint(-1.0f, 1.2f),
                        null,
                        new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, 0.0f)),
                        new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, 0.0f)));
                    Assert.That(root.TryApplySnapshot(in two), Is.True);
                    Assert.That(ActiveViewCount(root), Is.EqualTo(2));
                    AssertPoint(root.GetMemberView(0).transform.localPosition, -0.22f, 0.0f);
                    AssertPoint(root.GetMemberView(1).transform.localPosition, 0.22f, 0.0f);

                    SquadSnapshot three = CreateSnapshot(
                        lineage.CompanionId,
                        true,
                        SquadActionPhase.Idle,
                        -1,
                        new CompanionPoint(0.0f, 1.2f),
                        new CompanionPoint(0.0f, 1.2f),
                        null,
                        new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, -0.14f)),
                        new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, -0.14f)),
                        new CompanionMemberSnapshot(2, true, new CompanionPoint(0.0f, 0.22f)));
                    Assert.That(root.TryApplySnapshot(in three), Is.True);
                    Assert.That(ActiveViewCount(root), Is.EqualTo(3));
                    Assert.That(root.GetMemberView(2).IsPromotedLeaderVisual, Is.True);
                    AssertPoint(root.GetMemberView(2).transform.localPosition, 0.0f, 0.22f);

                    SquadSnapshot approaching = CreateSnapshot(
                        lineage.CompanionId,
                        true,
                        SquadActionPhase.Approaching,
                        0,
                        new CompanionPoint(0.0f, 1.2f),
                        new CompanionPoint(0.8f, 1.2f),
                        new CompanionPoint(2.0f, 1.2f),
                        new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, -0.14f)),
                        new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, -0.14f)),
                        new CompanionMemberSnapshot(2, true, new CompanionPoint(0.0f, 0.22f)));
                    Assert.That(root.TryApplySnapshot(in approaching), Is.True);
                    AssertPoint(root.GetMemberView(0).transform.localPosition, 0.8f, 0.0f);
                    Assert.That(root.TryPlayAttack(0, new CompanionPoint(2.0f, 1.2f), 0.28f), Is.True);
                    Assert.That(root.TryPlayAttack(7, new CompanionPoint(2.0f, 1.2f), 0.28f), Is.False);
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        private static void AssertThinMemberPrefab(
            GameObject prefab,
            SpriteLibraryAsset expectedLibrary,
            RuntimeAnimatorController expectedController,
            bool promotedLeader,
            int expectedAttackFrameCount)
        {
            CompanionMemberView member = prefab.GetComponent<CompanionMemberView>();
            Assert.That(member, Is.Not.Null);
            Assert.That(member.IsPromotedLeaderVisual, Is.EqualTo(promotedLeader));
            Assert.That(member.VisualDriver, Is.Not.Null);

            Transform visual = prefab.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null);
            float expectedScale = promotedLeader ? 0.55f : 0.44f;
            Assert.That(visual.localScale, Is.EqualTo(new Vector3(expectedScale, expectedScale, 1.0f)));
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            Animator animator = visual.GetComponent<Animator>();
            SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
            SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(library, Is.Not.Null);
            Assert.That(resolver, Is.Not.Null);
            Assert.That(renderer.sortingOrder, Is.EqualTo(20));
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.sprite.name, Is.EqualTo("Idle_0"));
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(expectedController));
            Assert.That(library.spriteLibraryAsset, Is.SameAs(expectedLibrary));
            Assert.That(member.VisualDriver.IdleFrameCount, Is.EqualTo(2));
            Assert.That(member.VisualDriver.RunFrameCount, Is.EqualTo(4));
            Assert.That(member.VisualDriver.AttackFrameCount, Is.EqualTo(expectedAttackFrameCount));
            Assert.That(member.VisualDriver.DeathFrameCount, Is.EqualTo(3));

            AnimationClip attackClip = FindClip(expectedController, "CompanionSpriteShared_Attack");
            SerializedProperty attackClipLength = new SerializedObject(member.VisualDriver)
                .FindProperty("_attackClipLength");
            Assert.That(attackClipLength, Is.Not.Null);
            Assert.That(attackClipLength.floatValue, Is.EqualTo(attackClip.length).Within(0.0001f));
            AssertNoObsoleteRuntimeOwnership(prefab);
        }

        private static void AssertNoObsoleteRuntimeOwnership(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int index = 0; index < components.Length; index += 1)
            {
                Component component = components[index];
                if (component == null)
                {
                    Assert.Fail("Missing script exists on " + root.name);
                }

                Assert.That(component, Is.Not.InstanceOf<Rigidbody2D>());
                Assert.That(component, Is.Not.InstanceOf<Collider2D>());
                string typeName = component.GetType().Name;
                Assert.That(typeName, Is.Not.EqualTo("AllyCombat"));
                Assert.That(typeName, Is.Not.EqualTo("AllyFollower"));
                Assert.That(typeName, Is.Not.EqualTo("CompanionRuntime"));
                Assert.That(typeName, Is.Not.EqualTo("CompanionHealthBar"));
                Assert.That(component, Is.Not.InstanceOf<UnitVisualRole>());
            }
        }

        private static SquadSnapshot CreateSnapshot(
            string companionId,
            bool promoted,
            SquadActionPhase actionPhase,
            int activeMemberOrder,
            CompanionPoint formationAnchor,
            CompanionPoint activeMemberPosition,
            CompanionPoint? committedTargetPosition,
            params CompanionMemberSnapshot[] members)
        {
            return new SquadSnapshot(
                "squad-0",
                0,
                companionId,
                promoted ? companionId + "-promoted" : companionId + "-base",
                members.Length,
                promoted,
                true,
                0.0f,
                formationAnchor,
                actionPhase,
                activeMemberOrder,
                activeMemberPosition,
                committedTargetPosition,
                members);
        }

        private static int ActiveViewCount(CompanionSquadRoot root)
        {
            int count = 0;
            for (int index = 0; index < root.MemberViewCount; index += 1)
            {
                CompanionMemberView view = root.GetMemberView(index);
                if (view != null && view.gameObject.activeSelf)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static void AssertPoint(Vector3 actual, float expectedX, float expectedY)
        {
            Assert.That(actual.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expectedY).Within(0.0001f));
            Assert.That(actual.z, Is.Zero.Within(0.0001f));
        }

        private static AnimationClip FindClip(RuntimeAnimatorController controller, string name)
        {
            AnimationClip[] clips = controller.animationClips;
            for (int index = 0; index < clips.Length; index += 1)
            {
                if (clips[index] != null && clips[index].name == name)
                {
                    return clips[index];
                }
            }

            Assert.Fail("Missing animation clip: " + name);
            return null;
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private readonly struct LineageFixture
        {
            public LineageFixture(
                string companionId,
                int attackFrameCount,
                string baseLibraryPath,
                string promotedLibraryPath,
                string basePrefabPath,
                string promotedPrefabPath,
                string squadPrefabPath)
            {
                CompanionId = companionId;
                AttackFrameCount = attackFrameCount;
                BaseLibraryPath = baseLibraryPath;
                PromotedLibraryPath = promotedLibraryPath;
                BasePrefabPath = basePrefabPath;
                PromotedPrefabPath = promotedPrefabPath;
                SquadPrefabPath = squadPrefabPath;
            }

            public string CompanionId { get; }

            public int AttackFrameCount { get; }

            public string BaseLibraryPath { get; }

            public string PromotedLibraryPath { get; }

            public string BasePrefabPath { get; }

            public string PromotedPrefabPath { get; }

            public string SquadPrefabPath { get; }
        }
    }
}
