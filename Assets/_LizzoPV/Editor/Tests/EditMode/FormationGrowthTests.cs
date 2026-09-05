using System.Collections.Generic;
using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class FormationGrowthTests
    {
        [Test]
        public void InitialOfferUsesUniqueEqualWeightLegionsAndRecruitOccupiesA3()
        {
            using RunRuntimeHost host = BuildHost(7, ThreeLegions());
            host.Start();

            FormationGrowthSnapshot before = host.CurrentSnapshot.FormationGrowth;
            Assert.That(before.ActiveOffer.Count, Is.EqualTo(3));
            AssertUniqueEqualWeight(before.ActiveOffer);

            int swordIndex = FindOfferIndex(before.ActiveOffer, "sword_soldier");
            Assert.That(swordIndex, Is.GreaterThanOrEqualTo(0));
            host.Submit(RunCommand.ChooseGrowthOffer(swordIndex));
            host.Advance(0.0f);

            FormationGrowthSnapshot after = host.CurrentSnapshot.FormationGrowth;
            Assert.That(after.GetProgression("sword_soldier"), Is.EqualTo(1));
            Assert.That(after.ActiveLegionCount, Is.EqualTo(1));
            Assert.That(after.LayoutRevision, Is.EqualTo(1));
            Assert.That(after.GetSlot("A1").HasOccupant, Is.False);
            Assert.That(after.GetSlot("A2").HasOccupant, Is.False);
            Assert.That(after.GetSlot("A3").OccupantUnitId, Is.EqualTo("sword_soldier"));
            Assert.That(host.CurrentSnapshot.ActiveBlockers, Is.EqualTo(SimulationBlocker.None));
        }

        [Test]
        public void ReinforceMovesBaseMembersToFrontAndPromotionFillsRearWithoutGlobalReflow()
        {
            using RunRuntimeHost host = BuildHost(8, SwordOnly());
            host.Start();
            ChooseOnlyOffer(host);
            int recruitLayoutRevision = host.CurrentSnapshot.FormationGrowth.LayoutRevision;

            GrantOneLevelAndChoose(host);
            FormationGrowthSnapshot reinforced = host.CurrentSnapshot.FormationGrowth;
            Assert.That(reinforced.GetProgression("sword_soldier"), Is.EqualTo(2));
            Assert.That(reinforced.GetSlot("A1").OccupantUnitId, Is.EqualTo("sword_soldier"));
            Assert.That(reinforced.GetSlot("A2").OccupantUnitId, Is.EqualTo("sword_soldier"));
            Assert.That(reinforced.GetSlot("A3").HasOccupant, Is.False);
            Assert.That(reinforced.LayoutRevision, Is.EqualTo(recruitLayoutRevision));

            GrantOneLevelAndChoose(host);
            FormationGrowthSnapshot promoted = host.CurrentSnapshot.FormationGrowth;
            Assert.That(promoted.GetProgression("sword_soldier"), Is.EqualTo(3));
            Assert.That(promoted.GetSlot("A1").IsPromoted, Is.False);
            Assert.That(promoted.GetSlot("A2").IsPromoted, Is.False);
            Assert.That(promoted.GetSlot("A3").OccupantUnitId, Is.EqualTo("sword_captain"));
            Assert.That(promoted.GetSlot("A3").IsPromoted, Is.True);
            Assert.That(promoted.LayoutRevision, Is.EqualTo(recruitLayoutRevision));
        }

        [Test]
        public void SymmetricLayoutsKeepOddBottomVertexAndExposeTwentyOneStableIds()
        {
            HashSet<string> ids = new HashSet<string>();
            for (int group = 0; group < 7; group++)
            {
                for (int member = 1; member <= 3; member++)
                    Assert.That(ids.Add(FormationLayout.CreateSlotId(group, member)), Is.True);
            }
            Assert.That(ids.Count, Is.EqualTo(21));

            const float radius = 2.0f;
            for (int count = 1; count <= 7; count++)
            {
                float sumX = 0.0f;
                for (int index = 0; index < count; index++)
                    sumX += FormationLayout.ResolveGroupCenter(count, index, radius).X;
                Assert.That(sumX, Is.Zero.Within(0.001f), $"count={count}");

                if ((count & 1) != 0)
                {
                    RunPoint bottom = FormationLayout.ResolveGroupCenter(count, 0, radius);
                    Assert.That(bottom.X, Is.Zero.Within(0.001f), $"count={count}");
                    Assert.That(bottom.Y, Is.EqualTo(-radius).Within(0.001f), $"count={count}");
                }
            }

            RunPoint left = FormationLayout.ResolveGroupCenter(2, 0, radius);
            RunPoint right = FormationLayout.ResolveGroupCenter(2, 1, radius);
            Assert.That(left.X, Is.EqualTo(-radius).Within(0.001f));
            Assert.That(right.X, Is.EqualTo(radius).Within(0.001f));
            Assert.That(left.Y, Is.Zero.Within(0.001f));
            Assert.That(right.Y, Is.Zero.Within(0.001f));
        }

        [Test]
        public void SameSeedProducesSameOfferAndDuplicateSelectionCommitsOnce()
        {
            using RunRuntimeHost first = BuildHost(91, SevenLegions());
            using RunRuntimeHost second = BuildHost(91, SevenLegions());
            first.Start();
            second.Start();

            GrowthOfferSnapshot a = first.CurrentSnapshot.FormationGrowth.ActiveOffer;
            GrowthOfferSnapshot b = second.CurrentSnapshot.FormationGrowth.ActiveOffer;
            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (int index = 0; index < a.Count; index++)
                Assert.That(a.GetCardId(index), Is.EqualTo(b.GetCardId(index)));

            string selectedId = a.GetCardId(0);
            first.Submit(RunCommand.ChooseGrowthOffer(0));
            first.Submit(RunCommand.ChooseGrowthOffer(0));
            first.Advance(0.0f);
            Assert.That(first.CurrentSnapshot.FormationGrowth.GetProgression(selectedId), Is.EqualTo(1));
            Assert.That(first.CurrentSnapshot.FormationGrowth.CommittedSelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void LegacySwordGrowthCommandCannotBypassFormationOffer()
        {
            SwordVerticalDefinition vertical = new SwordVerticalDefinition(
                100,
                RunPoint.Zero,
                new RunPoint(-1.0f, 0.0f),
                5.0f,
                20.0f,
                0.6f,
                10,
                0.01f,
                0.50f,
                10,
                0.30f,
                3,
                5);
            FormationGrowthDefinition growth = CreateGrowth(SwordOnly());
            using RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(13, vertical, growth));
            host.Start();

            host.Submit(RunCommand.ChooseSwordGrowthCard());
            host.Advance(0.0f);

            Assert.That(host.CurrentSnapshot.SwordVertical.Progression, Is.Zero);
            Assert.That(host.CurrentSnapshot.FormationGrowth.GetProgression("sword_soldier"), Is.Zero);
            Assert.That(
                (host.CurrentSnapshot.ActiveBlockers & SimulationBlocker.InitialRecruit) != 0,
                Is.True);

            ChooseOnlyOffer(host);
            Assert.That(host.CurrentSnapshot.SwordVertical.Progression, Is.EqualTo(1));
            Assert.That(host.CurrentSnapshot.FormationGrowth.GetProgression("sword_soldier"), Is.EqualTo(1));
        }

        [Test]
        public void PublishedSnapshotDoesNotChangeAfterLaterSelection()
        {
            using RunRuntimeHost host = BuildHost(17, SwordOnly());
            host.Start();
            FormationGrowthSnapshot before = host.CurrentSnapshot.FormationGrowth;

            ChooseOnlyOffer(host);

            Assert.That(before.GetProgression("sword_soldier"), Is.Zero);
            Assert.That(before.GetSlot("A3").HasOccupant, Is.False);
            Assert.That(host.CurrentSnapshot.FormationGrowth.GetProgression("sword_soldier"), Is.EqualTo(1));
        }

        [Test]
        public void PendingLevelsRecalculateSequentiallyAndDoNotBlockAfterMaxGrowth()
        {
            using RunRuntimeHost host = BuildHost(11, SwordOnly());
            host.Start();
            ChooseOnlyOffer(host);

            host.Submit(RunCommand.ExperienceAbsorbed(2));
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.FormationGrowth.PendingLevelCount, Is.EqualTo(2));
            Assert.That(host.CurrentSnapshot.FormationGrowth.ActiveOffer.Count, Is.EqualTo(1));

            ChooseOnlyOffer(host);
            Assert.That(host.CurrentSnapshot.FormationGrowth.PendingLevelCount, Is.EqualTo(1));
            Assert.That(
                (host.CurrentSnapshot.ActiveBlockers & SimulationBlocker.GrowthSelection) != 0,
                Is.True);

            ChooseOnlyOffer(host);
            FormationGrowthSnapshot complete = host.CurrentSnapshot.FormationGrowth;
            Assert.That(complete.GetProgression("sword_soldier"), Is.EqualTo(3));
            Assert.That(complete.PendingLevelCount, Is.Zero);
            Assert.That(complete.ActiveOffer.Count, Is.Zero);
            Assert.That(complete.IsGrowthComplete, Is.True);
            Assert.That(host.CurrentSnapshot.ActiveBlockers, Is.EqualTo(SimulationBlocker.None));
        }

        [Test]
        public void FullGrowthAssignsGroupsAThroughGAndReflowsOnlySevenTimes()
        {
            using RunRuntimeHost host = BuildHost(42, SevenLegions());
            host.Start();
            ChooseOnlyOffer(host);

            for (int selection = 1; selection < 21; selection++)
            {
                host.Submit(RunCommand.ExperienceAbsorbed(1));
                host.Advance(0.0f);
                ChooseOnlyOffer(host);
            }

            FormationGrowthSnapshot snapshot = host.CurrentSnapshot.FormationGrowth;
            Assert.That(snapshot.ActiveLegionCount, Is.EqualTo(7));
            Assert.That(snapshot.LayoutRevision, Is.EqualTo(7));
            Assert.That(snapshot.IsGrowthComplete, Is.True);
            for (int group = 0; group < 7; group++)
            {
                for (int member = 1; member <= 3; member++)
                {
                    FormationSlotSnapshot slot = snapshot.GetSlot(FormationLayout.CreateSlotId(group, member));
                    Assert.That(slot.HasOccupant, Is.True, slot.SlotId);
                }
            }
            RunPoint bottomGroup = snapshot.GetSlot("A3").GroupCenter;
            Assert.That(bottomGroup.X, Is.Zero.Within(0.001f));
            Assert.That(bottomGroup.Y, Is.LessThan(0.0f));
        }

        private static RunRuntimeHost BuildHost(int seed, LegionGrowthDefinition[] legions)
        {
            return RunCompositionRoot.Build(new RunDefinitionSnapshot(
                seed,
                SwordVerticalDefinition.Disabled,
                CreateGrowth(legions)));
        }

        private static FormationGrowthDefinition CreateGrowth(LegionGrowthDefinition[] legions)
        {
            return new FormationGrowthDefinition(
                experiencePerLevel: 1,
                groupRadius: 2.0f,
                frontDepth: 0.35f,
                rearDepth: 0.25f,
                halfWidth: 0.30f,
                legions: legions);
        }

        private static LegionGrowthDefinition[] SwordOnly()
        {
            return new[] { new LegionGrowthDefinition("sword_soldier", "sword_captain", 1.0f) };
        }

        private static LegionGrowthDefinition[] SevenLegions()
        {
            return new[]
            {
                new LegionGrowthDefinition("sword_soldier", "sword_captain", 1.0f),
                new LegionGrowthDefinition("shield_guard", "shield_captain", 1.0f),
                new LegionGrowthDefinition("cleric", "light_guide", 1.0f),
                new LegionGrowthDefinition("falcon_archer", "falcon_captain", 1.0f),
                new LegionGrowthDefinition("field_herbalist", "battle_apothecary", 1.0f),
                new LegionGrowthDefinition("bombardier", "powder_captain", 1.0f),
                new LegionGrowthDefinition("fire_mage", "fire_sage", 1.0f),
            };
        }

        private static LegionGrowthDefinition[] ThreeLegions()
        {
            return new[]
            {
                new LegionGrowthDefinition("sword_soldier", "sword_captain", 1.0f),
                new LegionGrowthDefinition("shield_guard", "shield_captain", 1.0f),
                new LegionGrowthDefinition("cleric", "light_guide", 1.0f),
            };
        }

        private static void GrantOneLevelAndChoose(RunRuntimeHost host)
        {
            host.Submit(RunCommand.ExperienceAbsorbed(1));
            host.Advance(0.0f);
            ChooseOnlyOffer(host);
        }

        private static void ChooseOnlyOffer(RunRuntimeHost host)
        {
            GrowthOfferSnapshot offer = host.CurrentSnapshot.FormationGrowth.ActiveOffer;
            Assert.That(offer.Count, Is.GreaterThan(0));
            host.Submit(RunCommand.ChooseGrowthOffer(0));
            host.Advance(0.0f);
        }

        private static int FindOfferIndex(GrowthOfferSnapshot offer, string cardId)
        {
            for (int index = 0; index < offer.Count; index++)
            {
                if (offer.GetCardId(index) == cardId)
                    return index;
            }
            return -1;
        }

        private static void AssertUniqueEqualWeight(GrowthOfferSnapshot offer)
        {
            HashSet<string> ids = new HashSet<string>();
            for (int index = 0; index < offer.Count; index++)
            {
                Assert.That(ids.Add(offer.GetCardId(index)), Is.True);
                Assert.That(offer.GetWeight(index), Is.EqualTo(1.0f));
            }
        }
    }
}
