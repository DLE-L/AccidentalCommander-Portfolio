using System;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class WorldFeedbackCoreProfileTests
    {
        [Test]
        public void DomainKeys_AreTypedSemanticStringsWithoutImplicitConversion()
        {
            var companionId = new CompanionId("sword_soldier");
            var attackId = new AttackId("sword_slash");

            Assert.That(companionId.Value, Is.EqualTo("sword_soldier"));
            Assert.That(attackId.Value, Is.EqualTo("sword_slash"));
            Assert.That(CompanionId.None.IsNone, Is.True);
            Assert.That(StatusId.None.IsNone, Is.True);
            Assert.That(EnemyAttackId.None.IsNone, Is.True);
            Assert.That(EnemyId.None.IsNone, Is.True);
            Assert.That(CombatImpactKind.None.IsNone, Is.True);
            Assert.That(typeof(CompanionId).GetMethod("op_Implicit"), Is.Null);
            Assert.That(typeof(CompanionId), Is.Not.EqualTo(typeof(AttackId)));
        }

        [Test]
        public void FeedbackEnums_MatchCurrentWorldContract()
        {
            CollectionAssert.AreEqual(
                new[] { "None", "Light", "Medium", "Heavy" },
                Enum.GetNames(typeof(HitStopGrade)));
            CollectionAssert.AreEqual(
                new[] { "Consumed", "TargetDeath" },
                Enum.GetNames(typeof(StatusReactionKind)));
            CollectionAssert.AreEqual(
                new[] { "Small", "Medium", "Large" },
                Enum.GetNames(typeof(OrbVisualTier)));
        }

        [Test]
        public void CommanderProfile_AllowsOptionalLowHealthOverlay()
        {
            CommanderWorldFeedbackProfileSO profile = Create<CommanderWorldFeedbackProfileSO>();
            try
            {
                profile.SetForEditor(
                    new VfxAssetId(1),
                    new AudioAssetId(2),
                    SpriteAssetId.None,
                    new MotionAssetId(3),
                    new MotionAssetId(4),
                    new MotionAssetId(5),
                    new AudioAssetId(6),
                    new AudioAssetId(7),
                    new AudioAssetId(8));

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.LowHealthOverlaySpriteId.IsNone, Is.True);
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void CompanionLifecycleBinding_UsesCompanionIdAndRequiredLifecycleCues()
        {
            CompanionLifecycleFeedbackProfileSO profile = Create<CompanionLifecycleFeedbackProfileSO>();
            try
            {
                profile.SetForEditor(
                    new MotionAssetId(1),
                    new VfxAssetId(2),
                    new AudioAssetId(3),
                    new MotionAssetId(4),
                    new VfxAssetId(5),
                    new AudioAssetId(6));
                var binding = new CompanionLifecycleFeedbackBinding(new CompanionId("cleric"), profile);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(binding.CompanionId.Value, Is.EqualTo("cleric"));
                Assert.That(binding.Profile, Is.SameAs(profile));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void WorldUiProfile_RequiresThreeColorRolesAndPositiveTiming()
        {
            WorldUiFeedbackProfileSO profile = Create<WorldUiFeedbackProfileSO>();
            try
            {
                SetWorldUi(profile, 0.15f, 1f);
                Assert.That(profile.TryValidate(out string issue), Is.True, issue);

                SetWorldUi(profile, 0f, 1f);
                Assert.That(profile.TryValidate(out string timingIssue), Is.False);
                Assert.That(timingIssue, Does.Contain("timing"));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void CombatImpact_AllowsIndependentNoneGradesAndVisualSlotsButRequiresImpactSfx()
        {
            CombatImpactFeedbackProfileSO profile = Create<CombatImpactFeedbackProfileSO>();
            try
            {
                profile.SetForEditor(
                    HitStopGrade.None,
                    MotionAssetId.None,
                    MotionAssetId.None,
                    SpriteAssetId.None,
                    new AudioAssetId(1));
                var binding = new CombatImpactFeedbackBinding(new CombatImpactKind("commander.hit.normal"), profile);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(binding.ImpactKind.IsNone, Is.False);
                Assert.That(profile.CameraMotionId.IsNone, Is.True);
                Assert.That(profile.ScreenFeedbackMotionId.IsNone, Is.True);
                Assert.That(profile.ScreenOverlaySpriteId.IsNone, Is.True);

                profile.SetForEditor(
                    HitStopGrade.None,
                    MotionAssetId.None,
                    MotionAssetId.None,
                    SpriteAssetId.None,
                    AudioAssetId.None);
                Assert.That(profile.TryValidate(out string missingIssue), Is.False);
                Assert.That(missingIssue, Does.Contain(nameof(profile.ImpactSfxId)));
            }
            finally
            {
                Destroy(profile);
            }
        }

        private static void SetWorldUi(
            WorldUiFeedbackProfileSO profile,
            float mergeWindowSeconds,
            float healthBarVisibleSeconds)
        {
            profile.SetForEditor(
                new SpriteAssetId(1),
                new SpriteAssetId(2),
                new MotionAssetId(3),
                new MotionAssetId(4),
                new MotionAssetId(5),
                new MotionAssetId(6),
                new ColorRole("world.damage.enemy"),
                new ColorRole("world.damage.commander"),
                new ColorRole("world.heal"),
                mergeWindowSeconds,
                healthBarVisibleSeconds);
        }

        private static T Create<T>() where T : ScriptableObject
        {
            return ScriptableObject.CreateInstance<T>();
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value != null)
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
