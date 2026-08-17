using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionVideoReadabilityTests
    {
        [Test]
        public void Snapshot_EnteringActing_PlaysTheCommittedMemberAttackImmediately()
        {
            GameObject rootObject = new GameObject("CompanionVideoReadabilityTests_Root");
            GameObject memberObject = new GameObject("Member");
            memberObject.transform.SetParent(rootObject.transform, false);
            SpriteRenderer renderer = memberObject.AddComponent<SpriteRenderer>();
            Animator animator = memberObject.AddComponent<Animator>();
            UnitVisualDriver driver = memberObject.AddComponent<UnitVisualDriver>();
            CompanionMemberView member = memberObject.AddComponent<CompanionMemberView>();
            CompanionSquadRoot root = rootObject.AddComponent<CompanionSquadRoot>();

            try
            {
                SetField(member, "_memberOrder", 0);
                SetField(member, "_visualDriver", driver);
                SetField(driver, "_spriteRenderer", renderer);
                SetField(driver, "_animator", animator);
                SetField(root, "_companionId", "sword_soldier");
                SetField(root, "_memberViews", new[] { member, CreateInactiveView(rootObject.transform, 1), CreateInactiveView(rootObject.transform, 2, true) });

                CompanionMemberSnapshot[] members =
                {
                    new CompanionMemberSnapshot(0, false, CompanionPoint.Zero),
                };
                SquadSnapshot approaching = CreateSnapshot(SquadActionPhase.Approaching, 0, members);
                Assert.That(root.TryApplySnapshot(in approaching), Is.True);
                float before = GetField<float>(driver, "_attackUntil");

                SquadSnapshot acting = CreateSnapshot(SquadActionPhase.Acting, 0, members);
                Assert.That(root.TryApplySnapshot(in acting), Is.True);

                Assert.That(GetField<float>(driver, "_attackUntil"), Is.GreaterThan(before));
                Assert.That(driver.SpriteRenderer.flipX, Is.False, "Committed target is to the right.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Snapshot_TargetMicroJitter_DoesNotFlipTheWholeSquadFacing()
        {
            GameObject rootObject = new GameObject("CompanionVideoReadabilityTests_FacingRoot");
            GameObject memberObject = new GameObject("Member");
            memberObject.transform.SetParent(rootObject.transform, false);
            SpriteRenderer renderer = memberObject.AddComponent<SpriteRenderer>();
            Animator animator = memberObject.AddComponent<Animator>();
            UnitVisualDriver driver = memberObject.AddComponent<UnitVisualDriver>();
            CompanionMemberView member = memberObject.AddComponent<CompanionMemberView>();
            CompanionSquadRoot root = rootObject.AddComponent<CompanionSquadRoot>();

            try
            {
                SetField(member, "_memberOrder", 0);
                SetField(member, "_visualDriver", driver);
                SetField(driver, "_spriteRenderer", renderer);
                SetField(driver, "_animator", animator);
                SetField(root, "_companionId", "shield_guard");
                SetField(root, "_memberViews", new[] { member, CreateInactiveView(rootObject.transform, 1), CreateInactiveView(rootObject.transform, 2, true) });

                CompanionMemberSnapshot[] members =
                {
                    new CompanionMemberSnapshot(0, false, CompanionPoint.Zero),
                };
                SquadSnapshot right = CreateSnapshot(
                    "shield_guard",
                    SquadActionPhase.Acting,
                    0,
                    new CompanionPoint(0.60f, 2.0f),
                    members);
                Assert.That(root.TryApplySnapshot(in right), Is.True);

                SquadSnapshot tinyLeft = CreateSnapshot(
                    "shield_guard",
                    SquadActionPhase.Acting,
                    0,
                    new CompanionPoint(-0.05f, 2.0f),
                    members);
                Assert.That(root.TryApplySnapshot(in tinyLeft), Is.True);

                CompanionPoint facing = GetField<CompanionPoint>(root, "_lastCombatFacing");
                Assert.That(facing.X, Is.GreaterThan(0.0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void PauseState_PausesAndResumesPersistentAudio()
        {
            bool originalPause = AudioListener.pause;
            GameObject pauseObject = new GameObject("CompanionVideoReadabilityTests_Pause");
            RunPauseController pause = pauseObject.AddComponent<RunPauseController>();

            try
            {
                AudioListener.pause = false;
                pause.Initialize();
                pause.ToggleUserPause();
                Assert.That(AudioListener.pause, Is.True);

                pause.ResumeFromPauseButton();
                Assert.That(AudioListener.pause, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pauseObject);
                AudioListener.pause = originalPause;
                Time.timeScale = 1.0f;
            }
        }

        private static SquadSnapshot CreateSnapshot(
            SquadActionPhase phase,
            int activeMemberOrder,
            CompanionMemberSnapshot[] members)
        {
            return CreateSnapshot(
                "sword_soldier",
                phase,
                activeMemberOrder,
                new CompanionPoint(3.0f, 0.0f),
                members);
        }

        private static SquadSnapshot CreateSnapshot(
            string companionId,
            SquadActionPhase phase,
            int activeMemberOrder,
            CompanionPoint targetPosition,
            CompanionMemberSnapshot[] members)
        {
            return new SquadSnapshot(
                "squad-0",
                0,
                companionId,
                companionId + "-action",
                members.Length,
                false,
                true,
                0.0f,
                CompanionPoint.Zero,
                phase,
                activeMemberOrder,
                CompanionPoint.Zero,
                targetPosition,
                members);
        }

        private static CompanionMemberView CreateInactiveView(Transform parent, int order, bool promoted = false)
        {
            GameObject gameObject = new GameObject("Member" + order);
            gameObject.transform.SetParent(parent, false);
            CompanionMemberView view = gameObject.AddComponent<CompanionMemberView>();
            SetField(view, "_memberOrder", order);
            SetField(view, "_promotedLeaderVisual", promoted);
            return view;
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
