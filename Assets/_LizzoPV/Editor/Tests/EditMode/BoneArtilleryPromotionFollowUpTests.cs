using System.Collections.Generic;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class BoneArtilleryPromotionTargetAreaFollowUpTests
    {
        [Test]
        public void PromotedBoneArtillery_PreservesBasePrimaryGeometryAndUsesOneSixtyPercentFollowUp()
        {
            CompanionTargetAreaCombatSetup baseSetup = new CompanionTargetAreaCombatSetup(
                "skeleton_bomber", 15, 2.4f, 4.8f, 1.5f, 6, 0.0f, 0.15f);
            CompanionTargetAreaCombatSetup scaled = baseSetup.WithGrowthScale(new CompanionGrowthScale(1.70f, 2.10f, 1.10f, 3));
            PromotedTargetAreaFollowUpSetup followUp = scaled.CreatePromotedBoneArtilleryFollowUp();

            Assert.AreEqual(4.8f, scaled.Range);
            Assert.AreEqual(1.5f, scaled.Radius);
            Assert.AreEqual(6, scaled.MaxTargets);
            Assert.AreEqual(26, scaled.Damage);
            Assert.That(scaled.Period, Is.EqualTo(2.64f).Within(0.0001f));
            Assert.AreEqual("skeleton_bomber", followUp.SourceId);
            Assert.AreEqual(2.0f, followUp.Radius);
            Assert.AreEqual(1, followUp.MaxTargets);
            Assert.AreEqual(0.60f, followUp.DamageRatio);
            Assert.AreEqual(16, followUp.ResolveDamage(scaled.Damage));
        }

        [Test]
        public void FollowUpSelector_ExcludesPrimaryTargetAndUsesRadiusThenInstanceId()
        {
            List<TargetAreaImpactCandidate> source = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(0.1f, 0.0f), 99),
                new TargetAreaImpactCandidate(null, new Vector3(1.0f, 0.0f), 30),
                new TargetAreaImpactCandidate(null, new Vector3(-1.0f, 0.0f), 10),
                new TargetAreaImpactCandidate(null, new Vector3(2.1f, 0.0f), 40),
            };

            Assert.IsTrue(PromotedTargetAreaFollowUpSelector.TrySelect(source, Vector3.zero, 99, 2.0f, out TargetAreaImpactCandidate followUp));
            Assert.AreEqual(10, followUp.InstanceId);
        }

        [Test]
        public void FollowUpSelector_SkipsWhenOnlyPrimaryOrOutOfRangeTargetsRemain()
        {
            List<TargetAreaImpactCandidate> source = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, Vector3.zero, 99),
                new TargetAreaImpactCandidate(null, new Vector3(2.1f, 0.0f), 1),
            };

            Assert.IsFalse(PromotedTargetAreaFollowUpSelector.TrySelect(source, Vector3.zero, 99, 2.0f, out _));
        }

        [Test]
        public void CastState_LocksPrimaryIdentityAndConsumesOneImpactWithoutRecursion()
        {
            TargetAreaCastState state = new TargetAreaCastState();
            state.Configure(new CompanionTargetAreaCombatSetup(
                "skeleton_bomber", 15, 2.4f, 4.8f, 1.5f, 6, 0.0f, 0.15f), 0.0f, 0.0f);

            Assert.IsTrue(state.TryBeginCast(0.0f, Vector3.one, 99));
            Assert.IsTrue(state.TryConsumeImpact(0.0f, out Vector3 point));
            Assert.AreEqual(Vector3.one, point);
            Assert.AreEqual(99, state.PrimaryTargetInstanceId);
            Assert.IsFalse(state.TryConsumeImpact(0.0f, out _));
        }
    }
}
