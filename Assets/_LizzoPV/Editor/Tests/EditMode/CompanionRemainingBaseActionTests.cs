using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionRemainingBaseActionTests
    {
        [Test]
        public void Revision6RemainingSetups_ExposeExecutionWeakeningCursePullAndReturningScythe()
        {
            LocalDataProvider data = CreateProjectProvider();

            CompanionWolfOwnedProxyCombatResolver wolfResolver = new CompanionWolfOwnedProxyCombatResolver(data);
            Assert.That(wolfResolver.TryResolve("wolf_tamer", out CompanionWolfOwnedProxyCombatSetup wolf), Is.True);
            Assert.That(wolf.TargetRule, Is.EqualTo(CombatTargetRule.LowestHealth));
            Assert.That(wolf.MaxChainTargets, Is.GreaterThan(1));
            Assert.That(wolf.ChainRange, Is.GreaterThan(0.0f));

            CompanionMeleeCombatResolver meleeResolver = new CompanionMeleeCombatResolver(data);
            Assert.That(meleeResolver.TryResolveWraithMeleeDefense(1.0f, out CompanionWraithMeleeDefenseSetup wraith), Is.True);
            Assert.That(wraith.Melee.TargetRule, Is.EqualTo(CombatTargetRule.CommanderThreat));
            Assert.That(wraith.Melee.AppliedStatusKind, Is.EqualTo(CompanionEnemyStatusKind.Weakening));
            Assert.That(wraith.Melee.StatusMagnitude, Is.InRange(0.01f, 0.99f));
            Assert.That(wraith.Melee.StatusDuration, Is.GreaterThan(0.0f));

            CompanionProjectileCombatResolver projectileResolver = new CompanionProjectileCombatResolver(data);
            Assert.That(projectileResolver.TryResolve("necromancer", 1.0f, out CompanionProjectileCombatSetup necromancer), Is.True);
            Assert.That(necromancer.AppliedStatusKind, Is.EqualTo(CompanionEnemyStatusKind.Curse));
            Assert.That(necromancer.StatusDuration, Is.GreaterThan(0.0f));
            Assert.That(necromancer.DeathReactionRadius, Is.GreaterThan(0.0f));
            Assert.That(necromancer.DeathReactionMaxTargets, Is.GreaterThan(0));
            Assert.That(necromancer.DeathReactionDistance, Is.GreaterThan(0.0f));
            Assert.That(new CompanionCurseDeathPullResolver(data).TryResolve(out CompanionCurseDeathPullSetup deathPull), Is.True);
            Assert.That(deathPull.Radius, Is.EqualTo(necromancer.DeathReactionRadius));
            Assert.That(deathPull.MaxTargets, Is.EqualTo(necromancer.DeathReactionMaxTargets));
            Assert.That(deathPull.PullDistance, Is.EqualTo(necromancer.DeathReactionDistance));

            CompanionReturningAttackCombatResolver returningResolver = new CompanionReturningAttackCombatResolver(data);
            Assert.That(returningResolver.TryResolve("skeleton_bomber", 1.0f, out CompanionReturningAttackCombatSetup scythe), Is.True);
            Assert.That(scythe.Range, Is.GreaterThan(0.0f));
            Assert.That(scythe.Width, Is.GreaterThan(0.0f));
            Assert.That(scythe.TravelDuration, Is.GreaterThan(0.0f));
            Assert.That(scythe.MaxTargetsPerPass, Is.GreaterThan(1));
        }

        [Test]
        public void ExecutionAndReturningSelectors_PreservePriorityAndPerPassOrder()
        {
            List<TargetAreaImpactCandidate> candidates = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(1.0f, 0.0f), 10, TargetAreaImpactTargetClass.Normal, 8),
                new TargetAreaImpactCandidate(null, new Vector3(2.0f, 0.1f), 20, TargetAreaImpactTargetClass.Normal, 3),
                new TargetAreaImpactCandidate(null, new Vector3(3.0f, 0.0f), 30, TargetAreaImpactTargetClass.Normal, 3),
            };

            Assert.That(CompanionPrimaryTargetSelector.TrySelectLowestHealth(
                candidates,
                Vector3.zero,
                4.0f,
                null,
                out TargetAreaImpactCandidate executionTarget), Is.True);
            Assert.That(executionTarget.InstanceId, Is.EqualTo(20));

            List<TargetAreaImpactCandidate> outbound = new List<TargetAreaImpactCandidate>();
            List<TargetAreaImpactCandidate> returning = new List<TargetAreaImpactCandidate>();
            CompanionReturningAttackTargetSelector.Collect(
                candidates,
                Vector3.zero,
                new Vector3(4.0f, 0.0f),
                0.5f,
                3,
                ReturningAttackPass.Outbound,
                outbound);
            CompanionReturningAttackTargetSelector.Collect(
                candidates,
                Vector3.zero,
                new Vector3(4.0f, 0.0f),
                0.5f,
                3,
                ReturningAttackPass.Return,
                returning);

            Assert.That(outbound.ConvertAll(candidate => candidate.InstanceId), Is.EqualTo(new[] { 10, 20, 30 }));
            Assert.That(returning.ConvertAll(candidate => candidate.InstanceId), Is.EqualTo(new[] { 30, 20, 10 }));
        }

        [Test]
        public void WeakeningBridge_ReducesOnlyTheNextCommanderHit()
        {
            GameObject owner = new GameObject("Revision6WeakeningBridge");
            try
            {
                MonsterController monster = owner.AddComponent<MonsterController>();
                Assert.That(monster.ApplyCompanionStatus(
                    CompanionEnemyStatusKind.Weakening,
                    new CompanionStatusSource("wraith_knight", 10),
                    0.70f,
                    3.0f,
                    1.0f), Is.True);

                Assert.That(monster.ResolveCompanionOutgoingCommanderDamage(10, 2.0f), Is.EqualTo(7));
                Assert.That(monster.ResolveCompanionOutgoingCommanderDamage(10, 2.1f), Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void FourRemainingPrimaryContracts_AreRuntimeConnectedWithPlaceholderTuning()
        {
            LocalDataProvider data = CreateProjectProvider();
            string[] connected = { "wolf_tamer", "wraith_knight", "necromancer", "skeleton_bomber" };
            for (int index = 0; index < connected.Length; index += 1)
            {
                CompanionRosterData roster = data.GetCompanionRoster(connected[index]);
                Assert.That(roster.PrimaryContractStage, Is.EqualTo(CompanionCombatContractStage.RuntimeConnected));
                Assert.That(roster.PromotionContractStage, Is.EqualTo(CompanionCombatContractStage.Skeleton));
                Assert.That(roster.TuningState, Is.EqualTo(CompanionTuningState.Placeholder));
            }
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
