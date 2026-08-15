using System.Collections.Generic;
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
        private const string BaseLibraryPath =
            "Assets/_LizzoPV/Art/Characters/Companions/sword_soldier_SpriteLibrary.asset";
        private const string PromotedLibraryPath =
            "Assets/_LizzoPV/Art/Characters/Companions/sword_captain_SpriteLibrary.asset";
        private const string SharedControllerPath =
            "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";

        [Test]
        public void SwordTracerPrefabs_AreThinAndUseCanonicalVisualAssets()
        {
            GameObject basePrefab = LoadRequired<GameObject>(CompanionPresentationTracerAuthoring.BaseMemberPrefabPath);
            GameObject promotedPrefab = LoadRequired<GameObject>(CompanionPresentationTracerAuthoring.PromotedMemberPrefabPath);
            GameObject squadPrefab = LoadRequired<GameObject>(CompanionPresentationTracerAuthoring.SquadRootPrefabPath);
            SpriteLibraryAsset baseLibrary = LoadRequired<SpriteLibraryAsset>(BaseLibraryPath);
            SpriteLibraryAsset promotedLibrary = LoadRequired<SpriteLibraryAsset>(PromotedLibraryPath);
            RuntimeAnimatorController controller = LoadRequired<RuntimeAnimatorController>(SharedControllerPath);

            AssertThinMemberPrefab(basePrefab, baseLibrary, controller, false);
            AssertThinMemberPrefab(promotedPrefab, promotedLibrary, controller, true);

            CompanionSquadRoot squadRoot = squadPrefab.GetComponent<CompanionSquadRoot>();
            Assert.That(squadRoot, Is.Not.Null);
            Assert.That(squadRoot.CompanionId, Is.EqualTo("sword_soldier"));
            Assert.That(squadRoot.MemberViewCount, Is.EqualTo(3));
            Assert.That(squadPrefab.GetComponentsInChildren<CompanionMemberView>(true).Length, Is.EqualTo(3));
            for (int index = 0; index < squadRoot.MemberViewCount; index += 1)
            {
                CompanionMemberView view = squadRoot.GetMemberView(index);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.MemberOrder, Is.EqualTo(index));
                Assert.That(view.IsPromotedLeaderVisual, Is.EqualTo(index == 2));
            }

            AssertNoObsoleteRuntimeOwnership(squadPrefab);
        }

        [Test]
        public void SnapshotFlow_DrivesOneTwoAndPromotedThreeMemberViews()
        {
            GameObject prefab = LoadRequired<GameObject>(CompanionPresentationTracerAuthoring.SquadRootPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                CompanionSquadRoot root = instance.GetComponent<CompanionSquadRoot>();
                Assert.That(root, Is.Not.Null);

                SquadSnapshot one = CreateSnapshot(
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

        private static void AssertThinMemberPrefab(
            GameObject prefab,
            SpriteLibraryAsset expectedLibrary,
            RuntimeAnimatorController expectedController,
            bool promotedLeader)
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
            Assert.That(member.VisualDriver.AttackFrameCount, Is.EqualTo(4));
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
                "sword_soldier",
                promoted ? "sword-promoted" : "sword-base",
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
    }
}
