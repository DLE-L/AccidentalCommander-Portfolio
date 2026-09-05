using System.Collections.Generic;
using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CommonPassiveTests
    {
        [Test]
        public void CatalogContainsTwelveUniqueEqualWeightChoices()
        {
            CommonPassiveDefinition definition = CreateDefinition();
            HashSet<string> cardIds = new HashSet<string>();
            HashSet<CommonPassiveEffect> effects = new HashSet<CommonPassiveEffect>();

            Assert.That(definition.Cards.Count, Is.EqualTo(12));
            for (int index = 0; index < definition.Cards.Count; index++)
            {
                CommonPassiveCardDefinition card = definition.Cards[index];
                Assert.That(cardIds.Add(card.CardId), Is.True, card.CardId);
                Assert.That(effects.Add(card.Effect), Is.True, card.Effect.ToString());
                Assert.That(card.Weight, Is.EqualTo(1.0f));
            }
            Assert.That(effects.Count, Is.EqualTo(12));
        }

        [Test]
        public void ApplyingLevelsRefreshesCachedModifiersAndCapsAtThree()
        {
            CommonPassiveRuntime runtime = new CommonPassiveRuntime(CreateDefinition());

            Assert.That(runtime.Apply(CommonPassiveId.StandardBearer), Is.True);
            Assert.That(runtime.Apply(CommonPassiveId.StandardBearer), Is.True);
            Assert.That(runtime.Apply(CommonPassiveId.StandardBearer), Is.True);
            Assert.That(runtime.Apply(CommonPassiveId.StandardBearer), Is.False);

            CommonPassiveSnapshot snapshot = runtime.CreateSnapshot();
            Assert.That(snapshot.GetLevel(CommonPassiveId.StandardBearer), Is.EqualTo(3));
            Assert.That(snapshot.AppliedLevelCount, Is.EqualTo(3));
            Assert.That(snapshot.CacheRevision, Is.EqualTo(3));
            Assert.That(snapshot.Modifiers.LegionDamageMultiplier, Is.EqualTo(1.30f).Within(0.0001f));

            CommonPassiveSnapshot repeatedRead = runtime.CreateSnapshot();
            Assert.That(repeatedRead.CacheRevision, Is.EqualTo(snapshot.CacheRevision));
        }

        [Test]
        public void ExperienceMultiplierIsCachedOnSelectionAndAppliedAtAbsorption()
        {
            using RunRuntimeHost host = BuildHost();
            host.Start();
            host.Submit(RunCommand.ChooseGrowthOffer(0));
            host.Advance(0.0f);

            host.Submit(RunCommand.ApplyCommonPassive(CommonPassiveId.SupplyPouch));
            host.Advance(0.0f);
            host.Submit(RunCommand.ExperienceAbsorbed(10));
            host.Advance(0.0f);

            FormationGrowthSnapshot growth = host.CurrentSnapshot.FormationGrowth;
            Assert.That(growth.AccumulatedExperience, Is.EqualTo(11));
            Assert.That(growth.PendingLevelCount, Is.EqualTo(1));
            Assert.That(host.CurrentSnapshot.CommonPassives.Modifiers.ExperienceGainMultiplier, Is.EqualTo(1.10f).Within(0.0001f));
        }

        [Test]
        public void LevelOfferMixesEligibleLegionsAndPassivesAtConfiguredWeights()
        {
            using RunRuntimeHost host = BuildHost();
            host.Start();
            host.Submit(RunCommand.ChooseGrowthOffer(0));
            host.Advance(0.0f);
            host.Submit(RunCommand.ExperienceAbsorbed(10));
            host.Advance(0.0f);

            GrowthOfferSnapshot offer = host.CurrentSnapshot.FormationGrowth.ActiveOffer;
            Assert.That(offer.Count, Is.EqualTo(3));
            int passiveSlot = -1;
            for (int index = 0; index < offer.Count; index++)
            {
                Assert.That(offer.GetWeight(index), Is.EqualTo(1.0f));
                if (offer.GetCardId(index) != "sword_soldier")
                    passiveSlot = index;
            }
            Assert.That(passiveSlot, Is.GreaterThanOrEqualTo(0));

            host.Submit(RunCommand.ChooseGrowthOffer(passiveSlot));
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.CommonPassives.AppliedLevelCount, Is.EqualTo(1));
        }

        [Test]
        public void GrowthCatalogRejectsLegionAndPassiveCardIdCollision()
        {
            CommonPassiveDefinition source = CreateDefinition();
            CommonPassiveCardDefinition[] cards = new CommonPassiveCardDefinition[source.Cards.Count];
            for (int index = 0; index < cards.Length; index++)
                cards[index] = source.Cards[index];
            cards[0] = new CommonPassiveCardDefinition(
                CommonPassiveId.StandardBearer,
                "sword_soldier",
                CommonPassiveEffect.LegionDamageMultiplier,
                1.0f,
                1.10f,
                1.20f,
                1.30f);
            CommonPassiveDefinition passives = new CommonPassiveDefinition(cards);
            FormationGrowthDefinition formation = new FormationGrowthDefinition(
                10,
                2.0f,
                0.35f,
                0.25f,
                0.30f,
                new[] { new LegionGrowthDefinition("sword_soldier", "sword_captain", 1.0f) });

            Assert.Throws<System.ArgumentException>(() =>
                RunCompositionRoot.Build(new RunDefinitionSnapshot(
                    1,
                    SwordVerticalDefinition.Disabled,
                    formation,
                    FrontlineLegionDefinition.Disabled,
                    RangedLegionDefinition.Disabled,
                    CasterLegionDefinition.Disabled,
                    SummonedLegionDefinition.Disabled,
                    passives)));
        }

        [Test]
        public void EliteDoctrineOnlyAddsDamageForThreeOrFewerActiveLegions()
        {
            CommonPassiveRuntime runtime = new CommonPassiveRuntime(CreateDefinition());
            runtime.Apply(CommonPassiveId.StandardBearer);
            runtime.Apply(CommonPassiveId.EliteDoctrine);

            CommonModifierSnapshot modifiers = runtime.CreateSnapshot().Modifiers;
            Assert.That(modifiers.ResolveLegionDamageMultiplier(0), Is.EqualTo(1.10f).Within(0.0001f));
            Assert.That(modifiers.ResolveLegionDamageMultiplier(3), Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(modifiers.ResolveLegionDamageMultiplier(4), Is.EqualTo(1.10f).Within(0.0001f));
        }

        [Test]
        public void CommonPassiveSelectionParticipatesInRunDigest()
        {
            using RunRuntimeHost first = BuildHost();
            using RunRuntimeHost second = BuildHost();
            first.Start();
            second.Start();
            Assert.That(first.CurrentSnapshot.StateDigest, Is.EqualTo(second.CurrentSnapshot.StateDigest));

            first.Submit(RunCommand.ApplyCommonPassive(CommonPassiveId.WarDrum));
            first.Advance(0.0f);

            Assert.That(first.CurrentSnapshot.StateDigest, Is.Not.EqualTo(second.CurrentSnapshot.StateDigest));
            Assert.That(first.CurrentSnapshot.CommonPassives.GetLevel(CommonPassiveId.WarDrum), Is.EqualTo(1));
        }

        private static RunRuntimeHost BuildHost()
        {
            return RunCompositionRoot.Build(new RunDefinitionSnapshot(
                301,
                SwordVerticalDefinition.Disabled,
                new FormationGrowthDefinition(
                    10,
                    2.0f,
                    0.35f,
                    0.25f,
                    0.30f,
                    new[] { new LegionGrowthDefinition("sword_soldier", "sword_captain", 1.0f) }),
                FrontlineLegionDefinition.Disabled,
                RangedLegionDefinition.Disabled,
                CasterLegionDefinition.Disabled,
                SummonedLegionDefinition.Disabled,
                CreateDefinition()));
        }

        private static CommonPassiveDefinition CreateDefinition()
        {
            return new CommonPassiveDefinition(new[]
            {
                Card(CommonPassiveId.StandardBearer, "standard_bearer", CommonPassiveEffect.LegionDamageMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.WarDrum, "war_drum", CommonPassiveEffect.ActionIntervalMultiplier, 0.94f, 0.88f, 0.82f),
                Card(CommonPassiveId.ScoutingBanner, "scouting_banner", CommonPassiveEffect.AcquisitionRangeMultiplier, 1.08f, 1.16f, 1.24f),
                Card(CommonPassiveId.WideFormation, "wide_formation", CommonPassiveEffect.AreaRadiusMultiplier, 1.08f, 1.16f, 1.24f),
                Card(CommonPassiveId.MarchingBoots, "marching_boots", CommonPassiveEffect.CommanderMoveSpeedMultiplier, 1.08f, 1.16f, 1.24f),
                Card(CommonPassiveId.ReinforcedArmor, "reinforced_armor", CommonPassiveEffect.CommanderMaxHealthMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.SupplyPouch, "supply_pouch", CommonPassiveEffect.ExperienceGainMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.EliteDoctrine, "elite_doctrine", CommonPassiveEffect.EliteRosterDamageMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.HeavyFormation, "heavy_formation", CommonPassiveEffect.ForcedMovementDistanceMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.LingeringTactics, "lingering_tactics", CommonPassiveEffect.StatusDurationMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.SustainedSummons, "sustained_summons", CommonPassiveEffect.OwnedEffectDurationMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.VeteranCommand, "veteran_command", CommonPassiveEffect.PromotedActionIntervalMultiplier, 0.94f, 0.88f, 0.82f),
            });
        }

        private static CommonPassiveCardDefinition Card(
            CommonPassiveId id,
            string cardId,
            CommonPassiveEffect effect,
            float level1,
            float level2,
            float level3)
        {
            return new CommonPassiveCardDefinition(id, cardId, effect, 1.0f, level1, level2, level3);
        }
    }
}
