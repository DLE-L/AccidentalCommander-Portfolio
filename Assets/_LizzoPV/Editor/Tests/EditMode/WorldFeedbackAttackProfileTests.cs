using System;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class WorldFeedbackAttackProfileTests
    {
        [Test]
        public void StatusProfile_RequiresCoreCuesAndKeepsReactionKindsDistinct()
        {
            StatusFeedbackProfileSO profile = Create<StatusFeedbackProfileSO>();
            try
            {
                profile.SetForEditor(
                    new VfxAssetId(1),
                    new VfxAssetId(2),
                    new VfxAssetId(3),
                    new SpriteAssetId(4),
                    new AudioAssetId(5),
                    new AudioAssetId(6),
                    new AudioAssetId(7),
                    new[]
                    {
                        new StatusReactionFeedback(StatusReactionKind.Consumed, new VfxAssetId(8), new AudioAssetId(9)),
                        new StatusReactionFeedback(StatusReactionKind.TargetDeath, new VfxAssetId(10), new AudioAssetId(11))
                    });
                var binding = new StatusFeedbackBinding(new StatusId("shock"), profile);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(binding.StatusId.Value, Is.EqualTo("shock"));
                Assert.That(binding.Profile, Is.SameAs(profile));

                profile.SetForEditor(
                    new VfxAssetId(1),
                    new VfxAssetId(2),
                    new VfxAssetId(3),
                    new SpriteAssetId(4),
                    new AudioAssetId(5),
                    new AudioAssetId(6),
                    new AudioAssetId(7),
                    new[]
                    {
                        new StatusReactionFeedback(StatusReactionKind.Consumed, new VfxAssetId(8), new AudioAssetId(9)),
                        new StatusReactionFeedback(StatusReactionKind.Consumed, new VfxAssetId(10), new AudioAssetId(11))
                    });

                Assert.That(profile.TryValidate(out string duplicateIssue), Is.False);
                Assert.That(duplicateIssue, Does.Contain("Duplicate"));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void StatusProfile_RejectsMissingBaseAssetId()
        {
            StatusFeedbackProfileSO profile = Create<StatusFeedbackProfileSO>();
            try
            {
                profile.SetForEditor(
                    new VfxAssetId(1),
                    new VfxAssetId(2),
                    VfxAssetId.None,
                    new SpriteAssetId(4),
                    new AudioAssetId(5),
                    new AudioAssetId(6),
                    new AudioAssetId(7),
                    Array.Empty<StatusReactionFeedback>());

                Assert.That(profile.TryValidate(out string issue), Is.False);
                Assert.That(issue, Does.Contain(nameof(profile.EndVfxId)));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void AttackProfile_AllowsOnlyRelevantCompleteDeliveryGroups()
        {
            AttackFeedbackProfileSO profile = Create<AttackFeedbackProfileSO>();
            try
            {
                CastFeedback cast = CreateCastFeedback();
                profile.SetForEditor(
                    cast,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default);
                var binding = new AttackFeedbackBinding(new AttackId("cleric.cast"), profile);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.CastFeedback.IsConfigured, Is.True);
                Assert.That(profile.ProjectileFeedback.IsConfigured, Is.False);
                Assert.That(binding.AttackId.Value, Is.EqualTo("cleric.cast"));
                Assert.That(binding.Profile, Is.SameAs(profile));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void AttackProfile_RejectsPartialProjectileGroup()
        {
            AttackFeedbackProfileSO profile = Create<AttackFeedbackProfileSO>();
            try
            {
                var partialProjectile = new ProjectileFeedback(
                    new SpriteAssetId(1),
                    VfxAssetId.None,
                    VfxAssetId.None,
                    AudioAssetId.None,
                    AudioAssetId.None,
                    VfxAssetId.None,
                    AudioAssetId.None);
                profile.SetForEditor(
                    default,
                    partialProjectile,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default);

                Assert.That(profile.TryValidate(out string issue), Is.False);
                Assert.That(issue, Does.Contain(nameof(ProjectileFeedback.SpawnVfxId)));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void AttackProfile_RejectsCompletelyEmptyProfile()
        {
            AttackFeedbackProfileSO profile = Create<AttackFeedbackProfileSO>();
            try
            {
                profile.SetForEditor(default, default, default, default, default, default, default, default, default);

                Assert.That(profile.TryValidate(out string issue), Is.False);
                Assert.That(issue, Does.Contain("at least one"));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void AttackImpact_UsesGlobalImpactKindWithoutOwningImpactSfxOrConeProfile()
        {
            AttackFeedbackProfileSO profile = Create<AttackFeedbackProfileSO>();
            try
            {
                var impact = new ImpactFeedback(new VfxAssetId(1), new CombatImpactKind("enemy.hit.heavy"));
                profile.SetForEditor(default, default, default, default, default, default, default, default, impact);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.ImpactFeedback.GlobalImpactKind.Value, Is.EqualTo("enemy.hit.heavy"));
                AssertNoMemberNamedImpactSfxId(typeof(AttackFeedbackProfileSO), typeof(ImpactFeedback));
                Assert.That(
                    typeof(AttackFeedbackProfileSO).Assembly.GetType("Lizzo.PV.Presentation.ConeFeedback"),
                    Is.Null);
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void EnemyAttackProfile_AllowsRelevantWindupAndTelegraphGroups()
        {
            EnemyAttackFeedbackProfileSO profile = Create<EnemyAttackFeedbackProfileSO>();
            try
            {
                var windup = new WindupFeedback(new MotionAssetId(1), new VfxAssetId(2), new AudioAssetId(3));
                var telegraph = new TelegraphFeedback(
                    new VfxAssetId(4),
                    new MotionAssetId(5),
                    new MotionAssetId(6),
                    new MotionAssetId(7),
                    new MotionAssetId(8),
                    new AudioAssetId(9),
                    new AudioAssetId(10));
                profile.SetForEditor(windup, telegraph, default, default, default);
                var binding = new EnemyAttackFeedbackBinding(new EnemyAttackId("boss.line_strike"), profile);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.EnemyProjectileFeedback.IsConfigured, Is.False);
                Assert.That(binding.EnemyAttackId.Value, Is.EqualTo("boss.line_strike"));
                Assert.That(binding.Profile, Is.SameAs(profile));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void EnemyAttackProfile_RejectsPartialPersistentHazardGroup()
        {
            EnemyAttackFeedbackProfileSO profile = Create<EnemyAttackFeedbackProfileSO>();
            try
            {
                var partialHazard = new PersistentHazardFeedback(
                    new VfxAssetId(1),
                    VfxAssetId.None,
                    VfxAssetId.None,
                    VfxAssetId.None,
                    VfxAssetId.None,
                    AudioAssetId.None,
                    AudioAssetId.None,
                    AudioAssetId.None,
                    AudioAssetId.None);
                profile.SetForEditor(default, default, default, default, partialHazard);

                Assert.That(profile.TryValidate(out string issue), Is.False);
                Assert.That(issue, Does.Contain(nameof(PersistentHazardFeedback.BoundaryVfxId)));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void EnemyImpact_UsesGlobalImpactKindWithoutOwningImpactSfx()
        {
            EnemyAttackFeedbackProfileSO profile = Create<EnemyAttackFeedbackProfileSO>();
            try
            {
                var impact = new EnemyImpactFeedback(new VfxAssetId(1), new CombatImpactKind("commander.hit.normal"));
                profile.SetForEditor(default, default, default, impact, default);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                AssertNoMemberNamedImpactSfxId(typeof(EnemyAttackFeedbackProfileSO), typeof(EnemyImpactFeedback));
            }
            finally
            {
                Destroy(profile);
            }
        }

        private static CastFeedback CreateCastFeedback()
        {
            return new CastFeedback(
                new MotionAssetId(1),
                new VfxAssetId(2),
                new AudioAssetId(3),
                new MotionAssetId(4),
                new VfxAssetId(5),
                new AudioAssetId(6));
        }

        private static void AssertNoMemberNamedImpactSfxId(params Type[] types)
        {
            foreach (Type type in types)
            {
                MemberInfo[] forbiddenMembers = type
                    .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(member => member.Name.IndexOf("ImpactSfxId", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
                Assert.That(forbiddenMembers, Is.Empty, $"{type.Name} must delegate impact SFX to the global impact profile.");
            }
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
