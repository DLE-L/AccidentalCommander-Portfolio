using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunResultSnapshotResolverTests
    {
        [Test]
        public void Capture_UsesCanonicalRosterAndReportsCurrentSynergyState()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            Assert.IsTrue(fixture.Run.CompanionRuntimeHost.Adapter.SubmitCard(1L, "wraith_knight").Accepted);
            Assert.IsTrue(fixture.Run.CompanionRuntimeHost.Adapter.SubmitCard(2L, "wraith_knight").Accepted);
            Assert.IsTrue(fixture.Run.CompanionRuntimeHost.Adapter.SubmitCard(3L, "wraith_knight").Accepted);
            Assert.IsTrue(fixture.Run.CompanionRuntimeHost.Adapter.SubmitCard(4L, "necromancer").Accepted);
            Assert.IsTrue(fixture.Run.CompanionRuntimeHost.Adapter.SubmitCard(5L, "skeleton_scythe_thrower").Accepted);
            object snapshot = Capture(fixture.Run);
            IReadOnlyList<RunResultSquadSlotView> squadSlots = Get<IReadOnlyList<RunResultSquadSlotView>>(snapshot, "SquadSlots");
            IReadOnlyList<RunResultCompanionSnapshot> finalLegion = Get<IReadOnlyList<RunResultCompanionSnapshot>>(snapshot, "FinalLegion");
            IReadOnlyList<RunResultSynergySnapshot> completedSynergies = Get<IReadOnlyList<RunResultSynergySnapshot>>(snapshot, "CompletedSynergies");
            IReadOnlyList<RunResultTraitSnapshot> selectedTraits = Get<IReadOnlyList<RunResultTraitSnapshot>>(snapshot, "SelectedTraits");

            Assert.AreEqual(7, squadSlots.Count);
            Assert.AreEqual("wraith_knight", finalLegion[0].CompanionId);
            Assert.AreEqual(3, finalLegion[0].OwnedCount);
            Assert.IsTrue(finalLegion[0].IsPromoted);
            Assert.IsFalse(string.IsNullOrWhiteSpace(finalLegion[0].PromotedRepresentativeId));
            Assert.AreEqual(3, finalLegion.Count);
            Assert.AreEqual(1, completedSynergies.Count);
            Assert.AreEqual("undead-march", completedSynergies[0].SynergyId);
            Assert.IsTrue(completedSynergies[0].IsCompleted);
            Assert.IsEmpty(selectedTraits);
        }

        [Test]
        public void Capture_ProducesSevenEmptySlotsAndEmptyResultCollections()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();

            object snapshot = Capture(fixture.Run);
            IReadOnlyList<RunResultSquadSlotView> squadSlots = Get<IReadOnlyList<RunResultSquadSlotView>>(snapshot, "SquadSlots");

            Assert.AreEqual(7, squadSlots.Count);
            for (int index = 0; index < squadSlots.Count; index++)
            {
                Assert.IsFalse(squadSlots[index].IsActive);
                Assert.AreEqual("빈 슬롯", squadSlots[index].DisplayName);
            }

            Assert.IsEmpty(Get<IReadOnlyList<RunResultCompanionSnapshot>>(snapshot, "FinalLegion"));
            Assert.IsEmpty(Get<IReadOnlyList<RunResultSynergySnapshot>>(snapshot, "CompletedSynergies"));
            Assert.IsEmpty(Get<IReadOnlyList<RunResultTraitSnapshot>>(snapshot, "SelectedTraits"));
        }

        private static object Capture(RunServices services)
        {
            Type resolver = typeof(RunResultViewData).Assembly.GetType("Lizzo.PV.UI.RunResultSnapshotResolver");
            Assert.IsNotNull(resolver, "Missing RunResultSnapshotResolver test type.");
            MethodInfo method = resolver.GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing result snapshot capture method.");
            return method.Invoke(null, new object[] { services });
        }

        private static T Get<T>(object snapshot, string propertyName)
        {
            PropertyInfo property = snapshot.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, $"Missing result snapshot property: {propertyName}");
            return (T)property.GetValue(snapshot);
        }
    }
}
