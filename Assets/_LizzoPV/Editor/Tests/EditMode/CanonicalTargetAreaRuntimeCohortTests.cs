using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CanonicalTargetAreaRuntimeCohortTests
    {
        [Test]
        public void Resolver_MapsBombardierAndSkeletonToCanonicalBaseAndPromotionContracts()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            CompanionTargetAreaCombatResolver resolver = new CompanionTargetAreaCombatResolver(fixture.Data);

            Assert.IsTrue(resolver.TryResolve("bombardier", 1.0f, out CompanionTargetAreaCombatSetup bombardier));
            Assert.AreEqual(16, bombardier.Damage);
            Assert.AreEqual(2.2f, bombardier.Period, 0.0001f);
            Assert.AreEqual(5.0f, bombardier.Range, 0.0001f);
            Assert.AreEqual(1.6f, bombardier.Radius, 0.0001f);
            Assert.AreEqual(6, bombardier.MaxTargets);
            Assert.AreEqual(0.5f, bombardier.CastDelay, 0.0001f);
            CompanionTargetAreaCombatSetup powder = bombardier.WithPromotedPowderCaptainImpact();
            Assert.AreEqual(2.0f, powder.Radius, 0.0001f);
            Assert.AreEqual(8, powder.MaxTargets);
            Assert.AreEqual(0.4f, powder.NormalPush, 0.0001f);
            Assert.AreEqual(0.0f, powder.EliteBossPush, 0.0001f);

            Assert.IsTrue(resolver.TryResolve("skeleton_bomber", 1.0f, out CompanionTargetAreaCombatSetup skeleton));
            Assert.AreEqual(15, skeleton.Damage);
            Assert.AreEqual(2.4f, skeleton.Period, 0.0001f);
            Assert.AreEqual(4.8f, skeleton.Range, 0.0001f);
            Assert.AreEqual(1.5f, skeleton.Radius, 0.0001f);
            Assert.AreEqual(6, skeleton.MaxTargets);
            Assert.AreEqual(0.0f, skeleton.CastDelay, 0.0001f);
            PromotedTargetAreaFollowUpSetup followUp = skeleton.CreatePromotedBoneArtilleryFollowUp();
            Assert.AreEqual(2.0f, followUp.Radius, 0.0001f);
            Assert.AreEqual(1, followUp.MaxTargets);
            Assert.AreEqual(0.60f, followUp.DamageRatio, 0.0001f);
        }
    }
}
