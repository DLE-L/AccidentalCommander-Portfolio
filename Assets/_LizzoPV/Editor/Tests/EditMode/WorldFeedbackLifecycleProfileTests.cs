using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Presentation;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class WorldFeedbackLifecycleProfileTests
    {
        private readonly List<ScriptableObject> _created = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void EnemySpawn_OwnsOnlyOptionalSpawnSfxAndCanRequireIt()
        {
            EnemySpawnFeedbackProfileSO profile = CreateEnemySpawnProfile(AudioAssetId.None);

            AssertNoMemberNamed(typeof(EnemySpawnFeedbackProfileSO), "SpawnMarkerVfxId");
            AssertNoMemberNamed(typeof(EnemySpawnFeedbackProfileSO), "SpawnMotionId");
            AssertNoMemberNamed(typeof(EnemySpawnFeedbackProfileSO), "SpawnVfxId");
            AssertNoMemberNamed(typeof(EnemySpawnFeedbackProfileSO), "ActiveSfxId");
            AssertNoMemberNamed(typeof(EnemySpawnFeedbackProfileSO), "VisualRevealMotionId");

            Assert.That(profile.TryValidate(out string normalIssue), Is.True, normalIssue);
            Assert.That(profile.TryValidate(true, out string eliteIssue), Is.False);
            Assert.That(eliteIssue, Does.Contain(nameof(profile.SpawnSfxId)));

            profile.SetForEditor(new AudioAssetId(4));
            Assert.That(profile.TryValidate(true, out string requiredIssue), Is.True, requiredIssue);
        }

        [Test]
        public void EnemyDeath_RequiresAllActualKillCuesAndUsesEnemyIdBinding()
        {
            EnemyDeathFeedbackProfileSO profile = CreateEnemyDeathProfile();
            var binding = new EnemyDeathFeedbackBinding(new EnemyId("boss_01"), profile);

            Assert.That(profile.TryValidate(out string issue), Is.True, issue);
            Assert.That(binding.EnemyId.Value, Is.EqualTo("boss_01"));

            profile.SetForEditor(new MotionAssetId(1), new VfxAssetId(2), AudioAssetId.None, new MotionAssetId(4));
            Assert.That(profile.TryValidate(out string missingIssue), Is.False);
            Assert.That(missingIssue, Does.Contain(nameof(profile.DeathSfxId)));
        }

        [Test]
        public void ExperienceOrb_RequiresAbsorbTickButAllowsOptionalCompleteAndOwnsNoSpawnSfx()
        {
            ExperienceOrbFeedbackProfileSO profile = CreateExperienceProfile(AudioAssetId.None);
            var binding = new ExperienceOrbFeedbackBinding(OrbVisualTier.Large, profile);

            Assert.That(profile.TryValidate(out string issue), Is.True, issue);
            Assert.That(binding.OrbVisualTier, Is.EqualTo(OrbVisualTier.Large));
            Assert.That(profile.AbsorbCompleteSfxId.IsNone, Is.True);
            AssertNoMemberNamed(typeof(ExperienceOrbFeedbackProfileSO), "SpawnSfxId");
        }

        [Test]
        public void RunOutcomeWorldProfile_KeepsOnlyWorldCuesAndCommanderDeathSfx()
        {
            RunOutcomeWorldFeedbackProfileSO profile = CreateRunOutcomeProfile();

            Assert.That(profile.TryValidate(out string issue), Is.True, issue);
            Assert.That(profile.FailureWorldFeedback.CommanderDeathSfxId.Value, Is.EqualTo(5));
            AssertNoMemberNamed(typeof(RunOutcomeWorldFeedbackProfileSO), "OutcomeStingerId");
            AssertNoMemberNamed(typeof(RunOutcomeWorldFeedbackProfileSO), "ResultBgmId");
            AssertNoMemberNamed(typeof(VictoryWorldFeedback), "TransitionSfxId");
            AssertNoMemberNamed(typeof(AbandonedWorldFeedback), "TransitionSfxId");
        }

        [Test]
        public void WorldFeedbackSet_ValidatesRoleProfilesAndTypedBindings()
        {
            WorldFeedbackProfileSetSO set = Create<WorldFeedbackProfileSetSO>();
            PopulateValidSet(set);

            Assert.That(set.TryValidate(out string issue), Is.True, issue);
            Assert.That(set.ExperienceOrbBindings.Count, Is.EqualTo(3));
            Assert.That(set.RunOutcomeProfile, Is.Not.Null);
        }

        [Test]
        public void WorldFeedbackSet_RejectsDuplicateTypedBindingKey()
        {
            WorldFeedbackProfileSetSO set = Create<WorldFeedbackProfileSetSO>();
            SetParts parts = CreateValidSetParts();
            var duplicateAttacks = new[]
            {
                new AttackFeedbackBinding(new AttackId("cleric.cast"), parts.Attack),
                new AttackFeedbackBinding(new AttackId("cleric.cast"), parts.Attack)
            };
            ApplySet(set, parts, duplicateAttacks, CreateExperienceBindings(parts.Experience));

            Assert.That(set.TryValidate(out string issue), Is.False);
            Assert.That(issue, Does.Contain("Duplicate AttackId"));
        }

        [Test]
        public void WorldFeedbackSet_RequiresAllThreeOrbVisualTiers()
        {
            WorldFeedbackProfileSetSO set = Create<WorldFeedbackProfileSetSO>();
            SetParts parts = CreateValidSetParts();
            var incompleteTiers = new[]
            {
                new ExperienceOrbFeedbackBinding(OrbVisualTier.Small, parts.Experience),
                new ExperienceOrbFeedbackBinding(OrbVisualTier.Medium, parts.Experience)
            };
            ApplySet(set, parts, CreateAttackBindings(parts.Attack), incompleteTiers);

            Assert.That(set.TryValidate(out string issue), Is.False);
            Assert.That(issue, Does.Contain("Small, Medium, and Large"));
        }

        [Test]
        public void WorldFeedbackSet_IsAReferenceSetWithoutRuntimeOrPlaybackState()
        {
            string[] forbidden = { "Queue", "Handle", "Cooldown", "CurrentTime", "Target", "Presenter", "Manager" };
            MemberInfo[] members = typeof(WorldFeedbackProfileSetSO)
                .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (string name in forbidden)
            {
                Assert.That(
                    members.Any(member => member.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.False,
                    $"WorldFeedbackProfileSetSO must not own runtime member {name}.");
            }
        }

        [Test]
        public void WorldFeedbackRuntime_MapsStatusAndDistinctAbandonedOutcomeFromRunEvents()
        {
            WorldFeedbackProfileSetSO set = Create<WorldFeedbackProfileSetSO>();
            PopulateValidSet(set);
            using RunState state = new RunState();
            var hits = new CombatImmediateHitModule();
            using var runtime = new WorldFeedbackRuntime(set, hits, state);
            int statusCount = 0;
            int outcomeCount = 0;
            StatusFeedbackPresentation status = default;
            RunOutcomeFeedbackPresentation outcome = default;
            runtime.Sink.StatusPresented += (presentation, _) =>
            {
                status = presentation;
                statusCount++;
            };
            runtime.Sink.RunOutcomePresented += (presentation, _) =>
            {
                outcome = presentation;
                outcomeCount++;
            };

            Assert.That(runtime.TryPresentStatusApplied(
                CompanionEnemyStatusKind.Shock,
                Vector3.one,
                77), Is.True);
            state.Reset(1);
            state.MarkLoaded();
            Assert.That(state.TryAbandon(), Is.True);

            Assert.That(statusCount, Is.EqualTo(1));
            Assert.That(status.StatusId.Value, Is.EqualTo("shock"));
            Assert.That(status.TargetInstanceId, Is.EqualTo(77));
            Assert.That(outcomeCount, Is.EqualTo(1));
            Assert.That(outcome.OutcomeKind, Is.EqualTo(RunOutcomeFeedbackKind.Abandoned));
            Assert.That(outcome.BossHpPercent, Is.EqualTo(-1));
        }

        [Test]
        public void WorldFeedbackRuntime_ConsumesOnlySuccessfulImmediateHitEvents()
        {
            WorldFeedbackProfileSetSO set = Create<WorldFeedbackProfileSetSO>();
            PopulateValidSet(set);
            using RunState state = new RunState();
            var hits = new CombatImmediateHitModule();
            using var runtime = new WorldFeedbackRuntime(set, hits, state);
            var target = new ImmediateHitTarget();
            int presentationCount = 0;
            CompanionAttackPresentation presentation = default;
            runtime.Sink.CompanionAttackPresented += (value, _) =>
            {
                presentation = value;
                presentationCount++;
            };

            Assert.That(hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                "cleric.cast",
                target,
                Vector3.zero,
                Vector3.one,
                3,
                Lizzo.PV.Gameplay.Visuals.AttackVisualKind.SingleHit,
                false)), Is.True);
            target.AcceptsHit = false;
            Assert.That(hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                "cleric.cast",
                target,
                Vector3.zero,
                Vector3.one,
                3,
                Lizzo.PV.Gameplay.Visuals.AttackVisualKind.SingleHit,
                false)), Is.False);

            Assert.That(presentationCount, Is.EqualTo(1));
            Assert.That(presentation.AttackId.Value, Is.EqualTo("cleric.cast"));
            Assert.That(presentation.EventKind, Is.EqualTo(CompanionAttackFeedbackEventKind.Impact));
        }

        [Test]
        public void WorldFeedbackRuntime_ConsumesCanonicalCompanionCastWithExplicitAttackIdentity()
        {
            WorldFeedbackProfileSetSO set = Create<WorldFeedbackProfileSetSO>();
            PopulateValidSet(set);
            using RunState state = new RunState();
            var hits = new CombatImmediateHitModule();
            using var runtime = new WorldFeedbackRuntime(set, hits, state);
            using var casts = new Lizzo.PV.Legion.Combat.CanonicalCompanionCastStream();
            runtime.BindCanonicalCompanionCasts(casts);
            int presentationCount = 0;
            CompanionAttackPresentation presentation = default;
            runtime.Sink.CompanionAttackPresented += (value, _) =>
            {
                presentation = value;
                presentationCount++;
            };
            var identity = new Lizzo.PV.Legion.Combat.CanonicalCompanionCastIdentity(
                12,
                "squad_00",
                "cleric",
                "support_family",
                "cleric.cast",
                new Vector3(3.0f, 4.0f, 0.0f),
                Vector3.left);

            Assert.That(casts.TryEmit(
                identity,
                Lizzo.PV.Legion.Combat.CanonicalCompanionActionKind.ActiveSkill), Is.True);

            Assert.That(presentationCount, Is.EqualTo(1));
            Assert.That(presentation.AttackId.Value, Is.EqualTo("cleric.cast"));
            Assert.That(presentation.EventKind, Is.EqualTo(CompanionAttackFeedbackEventKind.Cast));
            Assert.That(presentation.Position, Is.EqualTo(new Vector3(3.0f, 4.0f, 0.0f)));
            Assert.That(presentation.Direction, Is.EqualTo(Vector3.left));
        }

        private void PopulateValidSet(WorldFeedbackProfileSetSO set)
        {
            SetParts parts = CreateValidSetParts();
            ApplySet(set, parts, CreateAttackBindings(parts.Attack), CreateExperienceBindings(parts.Experience));
        }

        private void ApplySet(
            WorldFeedbackProfileSetSO set,
            SetParts parts,
            IEnumerable<AttackFeedbackBinding> attackBindings,
            IEnumerable<ExperienceOrbFeedbackBinding> experienceBindings)
        {
            set.SetForEditor(
                parts.Commander,
                new[] { new CompanionLifecycleFeedbackBinding(new CompanionId("cleric"), parts.Companion) },
                parts.WorldUi,
                new[] { new CombatImpactFeedbackBinding(new CombatImpactKind("enemy.hit.normal"), parts.Impact) },
                new[] { new StatusFeedbackBinding(new StatusId("shock"), parts.Status) },
                attackBindings,
                new[] { new EnemyAttackFeedbackBinding(new EnemyAttackId("boss.line"), parts.EnemyAttack) },
                new[] { new EnemySpawnFeedbackBinding(new EnemyId("enemy_01"), parts.EnemySpawn) },
                new[] { new EnemyDeathFeedbackBinding(new EnemyId("enemy_01"), parts.EnemyDeath) },
                experienceBindings,
                parts.RunOutcome);
        }

        private SetParts CreateValidSetParts()
        {
            return new SetParts
            {
                Commander = CreateCommanderProfile(),
                Companion = CreateCompanionProfile(),
                WorldUi = CreateWorldUiProfile(),
                Impact = CreateImpactProfile(),
                Status = CreateStatusProfile(),
                Attack = CreateAttackProfile(),
                EnemyAttack = CreateEnemyAttackProfile(),
                EnemySpawn = CreateEnemySpawnProfile(AudioAssetId.None),
                EnemyDeath = CreateEnemyDeathProfile(),
                Experience = CreateExperienceProfile(AudioAssetId.None),
                RunOutcome = CreateRunOutcomeProfile()
            };
        }

        private CommanderWorldFeedbackProfileSO CreateCommanderProfile()
        {
            CommanderWorldFeedbackProfileSO profile = Create<CommanderWorldFeedbackProfileSO>();
            profile.SetForEditor(
                new VfxAssetId(1), new AudioAssetId(2), SpriteAssetId.None,
                new MotionAssetId(3), new MotionAssetId(4), new MotionAssetId(5),
                new AudioAssetId(6), new AudioAssetId(7), new AudioAssetId(8));
            return profile;
        }

        private CompanionLifecycleFeedbackProfileSO CreateCompanionProfile()
        {
            CompanionLifecycleFeedbackProfileSO profile = Create<CompanionLifecycleFeedbackProfileSO>();
            profile.SetForEditor(
                new MotionAssetId(1), new VfxAssetId(2), new AudioAssetId(3),
                new MotionAssetId(4), new VfxAssetId(5), new AudioAssetId(6));
            return profile;
        }

        private WorldUiFeedbackProfileSO CreateWorldUiProfile()
        {
            WorldUiFeedbackProfileSO profile = Create<WorldUiFeedbackProfileSO>();
            profile.SetForEditor(
                new SpriteAssetId(1), new SpriteAssetId(2),
                new MotionAssetId(3), new MotionAssetId(4), new MotionAssetId(5), new MotionAssetId(6),
                new ColorRole("world.damage.enemy"), new ColorRole("world.damage.commander"), new ColorRole("world.heal"),
                0.15f, 1f);
            return profile;
        }

        private CombatImpactFeedbackProfileSO CreateImpactProfile()
        {
            CombatImpactFeedbackProfileSO profile = Create<CombatImpactFeedbackProfileSO>();
            profile.SetForEditor(HitStopGrade.None, MotionAssetId.None, MotionAssetId.None, SpriteAssetId.None, new AudioAssetId(1));
            return profile;
        }

        private StatusFeedbackProfileSO CreateStatusProfile()
        {
            StatusFeedbackProfileSO profile = Create<StatusFeedbackProfileSO>();
            profile.SetForEditor(
                new VfxAssetId(1), new VfxAssetId(2), new VfxAssetId(3), new SpriteAssetId(4),
                new AudioAssetId(5), new AudioAssetId(6), new AudioAssetId(7),
                Array.Empty<StatusReactionFeedback>());
            return profile;
        }

        private AttackFeedbackProfileSO CreateAttackProfile()
        {
            AttackFeedbackProfileSO profile = Create<AttackFeedbackProfileSO>();
            var cast = new CastFeedback(
                new MotionAssetId(1), new VfxAssetId(2), new AudioAssetId(3),
                new MotionAssetId(4), new VfxAssetId(5), new AudioAssetId(6));
            profile.SetForEditor(cast, default, default, default, default, default, default, default, default);
            return profile;
        }

        private EnemyAttackFeedbackProfileSO CreateEnemyAttackProfile()
        {
            EnemyAttackFeedbackProfileSO profile = Create<EnemyAttackFeedbackProfileSO>();
            var windup = new WindupFeedback(new MotionAssetId(1), new VfxAssetId(2), new AudioAssetId(3));
            profile.SetForEditor(windup, default, default, default, default);
            return profile;
        }

        private EnemySpawnFeedbackProfileSO CreateEnemySpawnProfile(AudioAssetId spawnSfxId)
        {
            EnemySpawnFeedbackProfileSO profile = Create<EnemySpawnFeedbackProfileSO>();
            profile.SetForEditor(spawnSfxId);
            return profile;
        }

        private EnemyDeathFeedbackProfileSO CreateEnemyDeathProfile()
        {
            EnemyDeathFeedbackProfileSO profile = Create<EnemyDeathFeedbackProfileSO>();
            profile.SetForEditor(new MotionAssetId(1), new VfxAssetId(2), new AudioAssetId(3), new MotionAssetId(4));
            return profile;
        }

        private ExperienceOrbFeedbackProfileSO CreateExperienceProfile(AudioAssetId completeSfxId)
        {
            ExperienceOrbFeedbackProfileSO profile = Create<ExperienceOrbFeedbackProfileSO>();
            profile.SetForEditor(
                new SpriteAssetId(1), new VfxAssetId(2), new MotionAssetId(3), new MotionAssetId(4),
                new VfxAssetId(5), new MotionAssetId(6), new VfxAssetId(7), new AudioAssetId(8), completeSfxId);
            return profile;
        }

        private RunOutcomeWorldFeedbackProfileSO CreateRunOutcomeProfile()
        {
            RunOutcomeWorldFeedbackProfileSO profile = Create<RunOutcomeWorldFeedbackProfileSO>();
            profile.SetForEditor(
                new VictoryWorldFeedback(new VfxAssetId(1), new MotionAssetId(2)),
                new FailureWorldFeedback(
                    new MotionAssetId(3), new VfxAssetId(4), new AudioAssetId(5),
                    new MotionAssetId(6), new MotionAssetId(7)),
                new AbandonedWorldFeedback(new MotionAssetId(8), new MotionAssetId(9)));
            return profile;
        }

        private static AttackFeedbackBinding[] CreateAttackBindings(AttackFeedbackProfileSO profile)
        {
            return new[] { new AttackFeedbackBinding(new AttackId("cleric.cast"), profile) };
        }

        private static ExperienceOrbFeedbackBinding[] CreateExperienceBindings(ExperienceOrbFeedbackProfileSO profile)
        {
            return new[]
            {
                new ExperienceOrbFeedbackBinding(OrbVisualTier.Small, profile),
                new ExperienceOrbFeedbackBinding(OrbVisualTier.Medium, profile),
                new ExperienceOrbFeedbackBinding(OrbVisualTier.Large, profile)
            };
        }

        private static void AssertNoMemberNamed(Type type, string memberName)
        {
            MemberInfo[] matches = type
                .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(member => member.Name.IndexOf(memberName, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            Assert.That(matches, Is.Empty, $"{type.Name} must not own {memberName}.");
        }

        private T Create<T>() where T : ScriptableObject
        {
            T value = ScriptableObject.CreateInstance<T>();
            _created.Add(value);
            return value;
        }

        private sealed class SetParts
        {
            public CommanderWorldFeedbackProfileSO Commander;
            public CompanionLifecycleFeedbackProfileSO Companion;
            public WorldUiFeedbackProfileSO WorldUi;
            public CombatImpactFeedbackProfileSO Impact;
            public StatusFeedbackProfileSO Status;
            public AttackFeedbackProfileSO Attack;
            public EnemyAttackFeedbackProfileSO EnemyAttack;
            public EnemySpawnFeedbackProfileSO EnemySpawn;
            public EnemyDeathFeedbackProfileSO EnemyDeath;
            public ExperienceOrbFeedbackProfileSO Experience;
            public RunOutcomeWorldFeedbackProfileSO RunOutcome;
        }

        private sealed class ImmediateHitTarget : ICombatImmediateHitTarget
        {
            public bool AcceptsHit = true;
            public CombatImmediateHitFaction Faction => CombatImmediateHitFaction.Enemy;
            public bool IsAlive => true;
            public bool TryReceiveImmediateHit(in CombatImmediateHitRequest request) => AcceptsHit;
        }
    }
}
