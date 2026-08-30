using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionBaseCombatActionTests
    {
        [Test]
        public void CommanderThreatSelector_PrioritizesCommanderProximityWithinAttackRange()
        {
            List<TargetAreaImpactCandidate> candidates = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(-1.0f, 0.0f), 10),
                new TargetAreaImpactCandidate(null, new Vector3(1.5f, 0.0f), 20),
                new TargetAreaImpactCandidate(null, new Vector3(2.5f, 0.0f), 30),
            };

            Assert.That(CompanionPrimaryTargetSelector.TrySelectCommanderThreat(
                candidates,
                Vector3.zero,
                new Vector3(2.0f, 0.0f),
                2.0f,
                out TargetAreaImpactCandidate selected), Is.True);
            Assert.That(selected.InstanceId, Is.EqualTo(20));
        }

        [Test]
        public void DensestClusterSelector_PrioritizesDensityThenOriginDistanceThenId()
        {
            List<TargetAreaImpactCandidate> candidates = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(1.0f, 0.0f), 5),
                new TargetAreaImpactCandidate(null, new Vector3(3.0f, 0.0f), 30),
                new TargetAreaImpactCandidate(null, new Vector3(3.2f, 0.0f), 20),
                new TargetAreaImpactCandidate(null, new Vector3(3.4f, 0.0f), 10),
                new TargetAreaImpactCandidate(null, new Vector3(6.0f, 0.0f), 1),
            };

            Assert.That(CompanionPrimaryTargetSelector.TrySelectDensestCluster(
                candidates,
                Vector3.zero,
                5.0f,
                0.5f,
                out TargetAreaImpactCandidate selected), Is.True);
            Assert.That(selected.InstanceId, Is.EqualTo(30));
        }

        [Test]
        public void Revision6FrontlineAndAreaSetups_UseLatestTargetingAndExistingDeliveryModules()
        {
            LocalDataProvider data = CreateProjectProvider();
            CompanionMeleeCombatResolver melee = new CompanionMeleeCombatResolver(data);
            CompanionTargetAreaCombatResolver area = new CompanionTargetAreaCombatResolver(data);
            CompanionPersistentFieldCombatResolver field = new CompanionPersistentFieldCombatResolver(data);

            Assert.That(melee.TryResolve("shield_guard", 1.0f, out CompanionMeleeCombatSetup shield), Is.True);
            Assert.That(shield.TargetRule, Is.EqualTo(CombatTargetRule.CommanderThreat));
            Assert.That(shield.AttackStyle, Is.EqualTo(AllyAttackStyle.ForwardPush));
            Assert.That(shield.Knockback, Is.GreaterThan(0.0f));
            Assert.That(shield.Movement.Kind, Is.EqualTo(CompanionMeleeMovementKind.ShieldIntercept));
            Assert.That(shield.Movement.EngagementRange, Is.EqualTo(3.0f));
            Assert.That(shield.Movement.MaxExcursionDistance, Is.EqualTo(2.0f));

            Assert.That(melee.TryResolve("sword_soldier", 1.0f, out CompanionMeleeCombatSetup sword), Is.True);
            Assert.That(sword.TargetRule, Is.EqualTo(CombatTargetRule.DensestCluster));
            Assert.That(sword.AttackStyle, Is.EqualTo(AllyAttackStyle.ForwardSlash));
            Assert.That(sword.Period, Is.LessThan(shield.Period));
            Assert.That(sword.Movement.Kind, Is.EqualTo(CompanionMeleeMovementKind.Pursuit));
            Assert.That(sword.Movement.EngagementRange, Is.EqualTo(3.0f));
            Assert.That(sword.Movement.EngageMoveSpeed, Is.EqualTo(1.2f));
            Assert.That(sword.Movement.ReturnMoveSpeed, Is.EqualTo(1.5f));

            Assert.That(area.TryResolve("bombardier", 1.0f, out CompanionTargetAreaCombatSetup bomb), Is.True);
            Assert.That(bomb.TargetRule, Is.EqualTo(CombatTargetRule.DensestCluster));
            Assert.That(bomb.CastDelay, Is.GreaterThan(0.0f));

            Assert.That(field.TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup fire), Is.True);
            Assert.That(fire.MaxActiveFields, Is.GreaterThan(0));
            Assert.That(fire.Duration, Is.GreaterThan(fire.TickInterval));

            string[] connected = { "shield_guard", "sword_soldier", "bombardier", "fire_mage" };
            for (int index = 0; index < connected.Length; index += 1)
            {
                CompanionRosterData roster = data.GetCompanionRoster(connected[index]);
                Assert.That(roster.PrimaryContractStage, Is.EqualTo(CompanionCombatContractStage.RuntimeConnected));
                Assert.That(roster.TuningState, Is.EqualTo(CompanionTuningState.Placeholder));
            }
        }

        [Test]
        public void ShieldPushDirection_AlwaysPointsAwayFromCommander()
        {
            Vector3 resolved = AllyCombat.ResolveCommanderOutwardDirection(
                new Vector3(2.0f, 0.0f),
                new Vector3(1.0f, 0.0f),
                Vector3.right);

            Assert.That(resolved, Is.EqualTo(Vector3.left));
        }

        [Test]
        public void MeleeApproachAndExcursion_StopsBetweenOriginAndTargetWithinLimit()
        {
            Vector2 approach = AllyCombat.ResolveMeleeApproachPosition(
                Vector2.zero,
                new Vector2(4.0f, 0.0f),
                1.2f);
            Vector2 clamped = AllyFollower.ClampExcursionDestination(
                new Vector2(-1.0f, 0.0f),
                approach,
                2.0f);

            Assert.That(approach.x, Is.GreaterThan(0.0f).And.LessThan(4.0f));
            Assert.That(clamped, Is.EqualTo(new Vector2(1.0f, 0.0f)));
        }

        private static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.That(gameData, Is.Not.Null);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.That(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
            return provider;
        }
    }
}
