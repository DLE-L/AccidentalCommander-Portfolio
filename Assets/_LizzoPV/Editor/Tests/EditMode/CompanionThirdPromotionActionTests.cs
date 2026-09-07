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
            Assert.That(setup.Reaper.Damage, Is.EqualTo(1));
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
        public void LineageCounters_KeepKillActionCurseDeathAndHitStreamsSeparate()
        {
            CompanionThirdPromotionTriggerState state = new CompanionThirdPromotionTriggerState(3, 3, 3, 3);

            Assert.That(state.RecordKill("wolf_tamer", 4), Is.EqualTo(1));
            Assert.That(state.BeastCurrentCount, Is.EqualTo(1));
            Assert.That(state.RecordAction("wraith_knight", CanonicalCompanionActionKind.BasicAttack, 3), Is.EqualTo(1));
            Assert.That(state.RecordAction("wraith_knight", CanonicalCompanionActionKind.ActiveSkill, 3), Is.Zero);
            Assert.That(state.RecordCursedDeath("necromancer", 3), Is.EqualTo(1));
            Assert.That(state.RecordHit("skeleton_scythe_thrower", 6), Is.EqualTo(2));

            state.Reset();
            Assert.That(state.BeastCurrentCount, Is.Zero);
            Assert.That(state.WraithCurrentCount, Is.Zero);
            Assert.That(state.RitualCurrentCount, Is.Zero);
            Assert.That(state.ReaperCurrentCount, Is.Zero);
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
