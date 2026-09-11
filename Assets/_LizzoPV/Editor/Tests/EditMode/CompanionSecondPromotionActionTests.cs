using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionSecondPromotionActionTests
    {
        [Test]
        public void Resolver_ExposesSecondFourPromotionActionsAsPlaceholderRuntimeContracts()
        {
            LocalDataProvider data = CreateProjectProvider();
            CompanionSecondPromotionCombatResolver resolver = new CompanionSecondPromotionCombatResolver(data);

            Assert.That(resolver.TryResolve(out CompanionSecondPromotionCombatSetup setup), Is.True);
            Assert.That(setup.Apothecary.SourceId, Is.EqualTo("battle_apothecary_vulnerability_spread"));
            Assert.That(setup.Apothecary.StatusKind, Is.EqualTo(CompanionEnemyStatusKind.Vulnerable));
            Assert.That(setup.Apothecary.MaxReactionDepth, Is.EqualTo(1));
            Assert.That(setup.Apothecary.MaxTargets, Is.GreaterThan(0));

            Assert.That(setup.Powder.SourceId, Is.EqualTo("powder_captain_cluster_bomb"));
            Assert.That(setup.Powder.TriggerCount, Is.EqualTo(9));
            Assert.That(setup.Powder.Damage, Is.EqualTo(28));
            Assert.That(setup.Powder.SmallExplosionCount, Is.GreaterThan(1));
            Assert.That(setup.Powder.SmallRadius, Is.LessThan(setup.Powder.MainRadius));

            Assert.That(setup.Fire.SourceId, Is.EqualTo("fire_sage_active_field_ignition"));
            Assert.That(setup.Fire.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Fire.Damage, Is.EqualTo(8));
            Assert.That(setup.Fire.DurationExtension, Is.GreaterThan(0.0f));

            Assert.That(setup.Storm.SourceId, Is.EqualTo("storm_mage_shock_overload"));
            Assert.That(setup.Storm.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Storm.Damage, Is.EqualTo(18));
            Assert.That(setup.Storm.StatusKind, Is.EqualTo(CompanionEnemyStatusKind.Shock));

            string[] connected = { "field_herbalist", "bombardier", "fire_mage", "lightning_mage" };
            for (int index = 0; index < connected.Length; index++)
            {
                CompanionRosterData roster = data.GetCompanionRoster(connected[index]);
                Assert.That(roster.PromotionContractStage, Is.EqualTo(CompanionCombatContractStage.RuntimeConnected));
                Assert.That(roster.TuningState, Is.EqualTo(CompanionTuningState.Placeholder));
            }
        }

        [Test]
        public void ResolvedTriggerList_QueuesOnlyBasicActionsAndRetainsUnconsumedWork()
        {
            Assert.That(new CompanionSecondPromotionCombatResolver(CreateProjectProvider()).TryResolve(out var setup), Is.True);
            var state = new CompanionPromotionTriggerState(setup.CreateTriggers());
            foreach (var binding in setup.CreateTriggers())
            {
                Assert.That(state.Record(binding.BaseUnitId, CanonicalCompanionActionKind.ActiveSkill, 6), Is.Zero);
                Assert.That(state.Record(binding.BaseUnitId, CompanionPromotionEventKind.CountableKill, 6), Is.Zero);
                Assert.That(state.Record(binding.BaseUnitId, CanonicalCompanionActionKind.BasicAttack, binding.TriggerCount * 2 + 1), Is.EqualTo(2));
                Assert.That(state.GetCurrentCount(binding.SourceId), Is.EqualTo(1));
                Assert.That(state.GetPendingCount(binding.SourceId), Is.EqualTo(2));
                Assert.That(state.ConsumePending(binding.SourceId), Is.True);
                Assert.That(state.GetPendingCount(binding.SourceId), Is.EqualTo(1));
            }
            state.Reset();
            foreach (var binding in setup.CreateTriggers())
                Assert.That(state.GetPendingCount(binding.SourceId), Is.Zero);
        }

        [Test]
        public void LineageCounters_UseBasicActionsAndRetainOverflow()
        {
            Assert.That(new CompanionSecondPromotionCombatResolver(CreateProjectProvider()).TryResolve(out var setup), Is.True);
            var state = new CompanionPromotionTriggerState(setup.CreateTriggers());

            Assert.That(state.Record("bombardier", CanonicalCompanionActionKind.BasicAttack, setup.Powder.TriggerCount + 1), Is.EqualTo(1));
            Assert.That(state.GetCurrentCount("powder_captain_cluster_bomb"), Is.EqualTo(1));
            Assert.That(state.Record("fire_mage", CanonicalCompanionActionKind.BasicAttack, 6), Is.EqualTo(2));
            Assert.That(state.Record("lightning_mage", CanonicalCompanionActionKind.ActiveSkill, 3), Is.EqualTo(0));
            Assert.That(state.Record("lightning_mage", CanonicalCompanionActionKind.BasicAttack, 3), Is.EqualTo(1));

            state.Reset();
            Assert.That(state.GetCurrentCount("powder_captain_cluster_bomb"), Is.Zero);
            Assert.That(state.GetCurrentCount("fire_sage_active_field_ignition"), Is.Zero);
            Assert.That(state.GetCurrentCount("storm_mage_shock_overload"), Is.Zero);
        }

        [Test]
        public void VulnerabilitySpreadSource_IncrementsDepthAndStopsAtConfiguredLimit()
        {
            CompanionEnemyStatusState status = new CompanionEnemyStatusState();
            CompanionStatusSource original = new CompanionStatusSource("field_herbalist", 10);
            Assert.That(status.ApplyVulnerable(original, 1.2f, 3.0f, 0.0f), Is.True);
            Assert.That(status.TryCaptureDeath(1.0f, out CompanionEnemyDeathStatusSnapshot snapshot), Is.True);

            Assert.That(CompanionVulnerabilitySpreadRules.TryCreateSpreadSource(snapshot, 20, 1, out CompanionStatusSource spread), Is.True);
            Assert.That(spread.UnitId, Is.EqualTo("field_herbalist"));
            Assert.That(spread.OwnerInstanceId, Is.EqualTo(20));
            Assert.That(spread.ReactionDepth, Is.EqualTo(1));

            CompanionEnemyStatusState propagated = new CompanionEnemyStatusState();
            Assert.That(propagated.ApplyVulnerable(spread, 1.2f, 3.0f, 1.0f), Is.True);
            Assert.That(propagated.TryCaptureDeath(2.0f, out CompanionEnemyDeathStatusSnapshot propagatedSnapshot), Is.True);
            Assert.That(CompanionVulnerabilitySpreadRules.TryCreateSpreadSource(propagatedSnapshot, 20, 1, out _), Is.False);
        }

        [Test]
        public void ClusterOffsets_AreDeterministicAndRemainAroundMainImpact()
        {
            Vector3 first = CompanionClusterBombRules.ResolveSmallExplosionCenter(Vector3.one, 0, 4, 1.0f);
            Vector3 second = CompanionClusterBombRules.ResolveSmallExplosionCenter(Vector3.one, 1, 4, 1.0f);
            Vector3 wrapped = CompanionClusterBombRules.ResolveSmallExplosionCenter(Vector3.one, 4, 4, 1.0f);

            Assert.That((first - new Vector3(2.0f, 1.0f, 1.0f)).sqrMagnitude, Is.LessThan(0.0001f));
            Assert.That((second - new Vector3(1.0f, 2.0f, 1.0f)).sqrMagnitude, Is.LessThan(0.0001f));
            Assert.That((wrapped - first).sqrMagnitude, Is.LessThan(0.0001f));
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
