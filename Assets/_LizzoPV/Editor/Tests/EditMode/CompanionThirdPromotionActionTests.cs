using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionThirdPromotionActionTests
    {
        [Test]
        public void Resolver_ExposesFinalFourPromotionActionsAsPlaceholderRuntimeContracts()
        {
            Assert.That((int)CombatTargetRule.Self, Is.EqualTo(9), "Existing serialized enum values must remain stable.");
            Assert.That((int)CombatTargetRule.HighestHealth, Is.GreaterThan((int)CombatTargetRule.Self));
            LocalDataProvider data = CreateProjectProvider();
            CompanionThirdPromotionCombatResolver resolver = new CompanionThirdPromotionCombatResolver(data);

            Assert.That(resolver.TryResolve(out CompanionThirdPromotionCombatSetup setup), Is.True);
            Assert.That(setup.Beast.SourceId, Is.EqualTo("beast_commander_pack_assault"));
            Assert.That(setup.Beast.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Beast.WolfHitCount, Is.EqualTo(3));
            Assert.That(setup.Beast.TargetRule, Is.EqualTo(CombatTargetRule.HighestHealth));

            Assert.That(setup.Wraith.SourceId, Is.EqualTo("wraith_guardian_orbit_patrol"));
            Assert.That(setup.Wraith.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Wraith.StatusKind, Is.EqualTo(CompanionEnemyStatusKind.Weakening));

            Assert.That(setup.Ritual.SourceId, Is.EqualTo("dark_ritualist_undead_ritual"));
            Assert.That(setup.Ritual.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Ritual.GroupSize, Is.EqualTo(3));
            Assert.That(setup.Ritual.Duration, Is.GreaterThan(0.0f));
            Assert.That(setup.Ritual.MaxGroups, Is.EqualTo(1));

            Assert.That(setup.Reaper.SourceId, Is.EqualTo("skeleton_reaper_orbit_scythe"));
            Assert.That(setup.Reaper.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Reaper.Damage, Is.EqualTo(25.5f), "Promotion uses the promoted basic attack reference before shared attack-power normalization.");
            Assert.That(setup.Reaper.OrbitRadius, Is.GreaterThan(setup.Reaper.PathHalfWidth));

            string[] connected = { "wolf_tamer", "wraith_knight", "necromancer", "skeleton_scythe_thrower" };
            for (int index = 0; index < connected.Length; index++)
            {
                CompanionRosterData roster = data.GetCompanionRoster(connected[index]);
                Assert.That(roster.PromotionContractStage, Is.EqualTo(CompanionCombatContractStage.RuntimeConnected));
                Assert.That(roster.TuningState, Is.EqualTo(CompanionTuningState.Placeholder));
                Assert.That(roster.PromotionEffectRef, Is.Not.Empty);
            }
        }

        [Test]
        public void ResolvedTriggerList_IsolatesKillsCursedDeathsAndReturningHitEvents()
        {
            Assert.That(new CompanionThirdPromotionCombatResolver(CreateProjectProvider()).TryResolve(out var setup), Is.True);
            var state = new CompanionPromotionTriggerState(setup.CreateTriggers());
            Assert.That(state.Record("wolf_tamer", CanonicalCompanionActionKind.BasicAttack, 6), Is.Zero);
            Assert.That(state.Record("wolf_tamer", CompanionPromotionEventKind.CursedDeath, 6), Is.Zero);
            Assert.That(state.Record("necromancer", CompanionPromotionEventKind.CountableKill, 6), Is.Zero);
            Assert.That(state.Record("skeleton_scythe_thrower", CanonicalCompanionActionKind.BasicAttack, 6), Is.Zero);
            Assert.That(state.Record("wolf_tamer", CompanionPromotionEventKind.CountableKill, 7), Is.EqualTo(2));
            Assert.That(state.Record("necromancer", CompanionPromotionEventKind.CursedDeath, 4), Is.EqualTo(1));
            Assert.That(state.Record("wraith_knight", CanonicalCompanionActionKind.BasicAttack, 3), Is.EqualTo(1));
            Assert.That(state.Record("skeleton_scythe_thrower", CanonicalCompanionActionKind.ReturningAttackResolved, 6), Is.EqualTo(2));
            Assert.That(state.GetPendingCount(setup.Beast.SourceId), Is.EqualTo(2));
            Assert.That(state.GetPendingCount(setup.Ritual.SourceId), Is.EqualTo(1));
            Assert.That(state.GetPendingCount(setup.Wraith.SourceId), Is.EqualTo(1));
            Assert.That(state.GetPendingCount(setup.Reaper.SourceId), Is.EqualTo(2));
            state.Reset();
            foreach (var binding in setup.CreateTriggers())
            {
                Assert.That(state.GetPendingCount(binding.SourceId), Is.Zero);
                Assert.That(state.GetCurrentCount(binding.SourceId), Is.Zero);
            }
        }

        [Test]
        public void SharedTriggerList_DistinguishesDeathKindsForTheSameOwner()
        {
            var state = new CompanionPromotionTriggerState(new[]
            {
                new CompanionPromotionTriggerBinding("owner", CompanionPromotionEventKind.CountableKill, "kill", 2),
                new CompanionPromotionTriggerBinding("owner", CompanionPromotionEventKind.CursedDeath, "curse", 3),
            });
            Assert.That(state.Record("owner", CompanionPromotionEventKind.CountableKill, 4), Is.EqualTo(2));
            Assert.That(state.GetPendingCount("curse"), Is.Zero);
            Assert.That(state.Record("owner", CompanionPromotionEventKind.CursedDeath, 3), Is.EqualTo(1));
            Assert.That(state.GetPendingCount("kill"), Is.EqualTo(2));
            Assert.Throws<System.ArgumentException>(() => state.Record("owner", CompanionPromotionEventKind.Action));
        }

        [Test]
        public void LineageCounters_KeepKillActionCurseDeathAndHitStreamsSeparate()
        {
            Assert.That(new CompanionThirdPromotionCombatResolver(CreateProjectProvider()).TryResolve(out var setup), Is.True);
            var state = new CompanionPromotionTriggerState(setup.CreateTriggers());

            Assert.That(state.Record("wolf_tamer", CompanionPromotionEventKind.CountableKill, 4), Is.EqualTo(1));
            Assert.That(state.GetCurrentCount("beast_commander_pack_assault"), Is.EqualTo(1));
            Assert.That(state.Record("wraith_knight", CanonicalCompanionActionKind.BasicAttack, 3), Is.EqualTo(1));
            Assert.That(state.Record("wraith_knight", CanonicalCompanionActionKind.ActiveSkill, 3), Is.Zero);
            Assert.That(state.Record("necromancer", CompanionPromotionEventKind.CursedDeath, 3), Is.EqualTo(1));
            Assert.That(state.Record("skeleton_scythe_thrower", CanonicalCompanionActionKind.ReturningAttackResolved, 6), Is.EqualTo(2));

            state.Reset();
            Assert.That(state.GetCurrentCount("beast_commander_pack_assault"), Is.Zero);
            Assert.That(state.GetCurrentCount("wraith_guardian_orbit_patrol"), Is.Zero);
            Assert.That(state.GetCurrentCount("dark_ritualist_undead_ritual"), Is.Zero);
            Assert.That(state.GetCurrentCount("skeleton_reaper_orbit_scythe"), Is.Zero);
        }

        [Test]
        public void OrbitPathSelector_UsesRingWidthAndStableAngularOrder()
        {
            List<CompanionPromotionTargetCandidate> source = new List<CompanionPromotionTargetCandidate>
            {
                new CompanionPromotionTargetCandidate(null, new Vector3(0.0f, -3.0f), 40, 5, false, false),
                new CompanionPromotionTargetCandidate(null, new Vector3(3.0f, 0.0f), 30, 5, false, false),
                new CompanionPromotionTargetCandidate(null, new Vector3(0.0f, 3.0f), 20, 5, false, false),
                new CompanionPromotionTargetCandidate(null, new Vector3(1.0f, 0.0f), 10, 5, false, false),
            };
            List<CompanionPromotionTargetCandidate> results = new List<CompanionPromotionTargetCandidate>();

            CompanionOrbitPathTargetSelector.Collect(source, Vector3.zero, 3.0f, 0.5f, 8, results);

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].InstanceId, Is.EqualTo(30));
            Assert.That(results[1].InstanceId, Is.EqualTo(20));
            Assert.That(results[2].InstanceId, Is.EqualTo(40));
        }

        private static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.That(gameData, Is.Not.Null);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.That(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
            return provider;
        }
    }
}
