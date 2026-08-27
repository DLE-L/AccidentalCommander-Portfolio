using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionRangedStatusBaseActionTests
    {
        [Test]
        public void Revision6RangedSetups_ExposeReturningPiercingAreaStatusAndShockContracts()
        {
            LocalDataProvider data = CreateProjectProvider();

            CompanionRangedSupportCombatResolver support = new CompanionRangedSupportCombatResolver(data);
            Assert.That(support.TryResolve("cleric", 1.0f, out CompanionRangedSupportCombatSetup cleric), Is.True);
            Assert.That(cleric.HealOnPrimaryReturn, Is.True);
            Assert.That(cleric.PrimaryReturnDelaySeconds, Is.GreaterThan(0.0f));
            Assert.That(cleric.SecondaryMaxTargets, Is.EqualTo(1));
            Assert.That(support.TryResolve("field_herbalist", 1.0f, out _), Is.False);

            CompanionProjectileCombatResolver projectile = new CompanionProjectileCombatResolver(data);
            Assert.That(projectile.TryResolve("falcon_archer", 1.0f, out CompanionProjectileCombatSetup archer), Is.True);
            Assert.That(archer.IsStraightPiercing, Is.True);
            Assert.That(archer.MaxTargets, Is.GreaterThan(1));
            Assert.That(archer.ProjectileSpeedMultiplier, Is.GreaterThan(1.0f));

            CompanionTargetAreaCombatResolver area = new CompanionTargetAreaCombatResolver(data);
            Assert.That(area.TryResolve("field_herbalist", 1.0f, out CompanionTargetAreaCombatSetup herbalist), Is.True);
            Assert.That(herbalist.AppliedStatusKind, Is.EqualTo(CompanionEnemyStatusKind.Vulnerable));
            Assert.That(herbalist.StatusMagnitude, Is.GreaterThan(1.0f));
            Assert.That(herbalist.StatusDuration, Is.GreaterThan(0.0f));

            CompanionChainCombatResolver chain = new CompanionChainCombatResolver(data);
            Assert.That(chain.TryResolve("lightning_mage", 1.0f, out CompanionChainCombatSetup lightning), Is.True);
            Assert.That(lightning.FirstTargetStatusKind, Is.EqualTo(CompanionEnemyStatusKind.Shock));
            Assert.That(lightning.FirstTargetStatusMagnitude, Is.InRange(0.01f, 0.99f));
            Assert.That(lightning.FirstTargetStatusDuration, Is.GreaterThan(0.0f));
        }

        [Test]
        public void MonsterStatusBridge_AppliesRuntimeVulnerableAndShockState()
        {
            GameObject owner = new GameObject("Revision6StatusBridge");
            try
            {
                MonsterController monster = owner.AddComponent<MonsterController>();
                CompanionStatusSource herbalist = new CompanionStatusSource("field_herbalist", 10);
                CompanionStatusSource lightning = new CompanionStatusSource("lightning_mage", 20);

                Assert.That(monster.ApplyCompanionStatus(
                    CompanionEnemyStatusKind.Vulnerable,
                    herbalist,
                    1.20f,
                    2.0f,
                    1.0f), Is.True);
                Assert.That(monster.ResolveCompanionIncomingDamageMultiplier(2.0f), Is.EqualTo(1.20f));

                Assert.That(monster.ApplyCompanionStatus(
                    CompanionEnemyStatusKind.Shock,
                    lightning,
                    0.75f,
                    2.0f,
                    1.0f), Is.True);
                Assert.That(monster.ResolveCompanionMovementSpeedMultiplier(2.0f), Is.EqualTo(0.75f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void FourRangedPrimaryContracts_AreRuntimeConnectedWithPlaceholderTuning()
        {
            LocalDataProvider data = CreateProjectProvider();
            string[] connected = { "cleric", "falcon_archer", "field_herbalist", "lightning_mage" };
            for (int index = 0; index < connected.Length; index += 1)
            {
                CompanionRosterData roster = data.GetCompanionRoster(connected[index]);
                Assert.That(roster.PrimaryContractStage, Is.EqualTo(CompanionCombatContractStage.RuntimeConnected));
                CompanionCombatContractStage expectedPromotionStage = connected[index] == "cleric" || connected[index] == "falcon_archer"
                    ? CompanionCombatContractStage.RuntimeConnected
                    : CompanionCombatContractStage.Skeleton;
                Assert.That(roster.PromotionContractStage, Is.EqualTo(expectedPromotionStage));
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
