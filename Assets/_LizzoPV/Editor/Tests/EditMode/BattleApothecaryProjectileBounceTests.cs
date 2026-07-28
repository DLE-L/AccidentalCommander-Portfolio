using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class BattleApothecaryProjectileBounceTests
    {
        [Test]
        public void PromotedBattleApothecary_EnablesOneBoundedPrimaryDartBounce()
        {
            CompanionRangedSupportCombatSetup setup = ResolveHerbalistSetup()
                .WithPromotedBattleApothecaryHeal()
                .WithPromotedBattleApothecaryBounce();

            Assert.IsTrue(setup.HasPrimaryProjectileBounce);
            Assert.AreEqual("field_herbalist", setup.PrimaryProjectileBounce.SourceId);
            Assert.AreEqual(1.8f, setup.PrimaryProjectileBounce.Radius);
            Assert.AreEqual(1, setup.PrimaryProjectileBounce.MaxTargets);
            Assert.AreEqual(0.60f, setup.PrimaryProjectileBounce.DamageRatio);
            Assert.AreEqual(7, setup.PrimaryProjectileBounce.ResolveDamage(12));
        }

        [Test]
        public void ProjectileBounceSelector_ExcludesPrimaryAndOwnerAndUsesDistanceThenInstanceId()
        {
            List<ProjectileBounceTargetCandidate> candidates = new List<ProjectileBounceTargetCandidate>
            {
                new ProjectileBounceTargetCandidate(null, new Vector3(1.0f, 0.0f), 20, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(1.0f, 0.0f), 10, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(0.2f, 0.0f), 3, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(0.3f, 0.0f), 4, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(1.81f, 0.0f), 5, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(0.5f, 0.0f), 6, false),
            };

            Assert.IsTrue(ProjectileBounceTargetSelector.TrySelect(
                candidates,
                Vector3.zero,
                primaryTargetInstanceId: 3,
                ownerInstanceId: 4,
                radius: 1.8f,
                out ProjectileBounceTargetCandidate result));

            Assert.AreEqual(10, result.InstanceId);
        }

        [Test]
        public void BaseHerbalistAndPromotedCombatRoutingRemainExplicit()
        {
            CompanionRangedSupportCombatSetup baseSetup = ResolveHerbalistSetup();
            Assert.IsFalse(baseSetup.HasPrimaryProjectileBounce);

            GameObject owner = new GameObject("BattleApothecaryBounceRouting");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalRangedSupportInfo(baseSetup.WithPromotedBattleApothecaryHeal());
                Assert.IsFalse(combat.HasPromotedProjectileBounce);

                combat.SetPromotedProjectileBounce(baseSetup
                    .WithPromotedBattleApothecaryHeal()
                    .WithPromotedBattleApothecaryBounce()
                    .PrimaryProjectileBounce);
                Assert.IsTrue(combat.HasPromotedProjectileBounce);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static CompanionRangedSupportCombatSetup ResolveHerbalistSetup()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            CompanionRangedSupportCombatResolver resolver = new CompanionRangedSupportCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("field_herbalist", 1.0f, out CompanionRangedSupportCombatSetup setup));
            return setup;
        }
    }
}
