using Lizzo.PV.Flow;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Telemetry;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CardOfferTelemetryTests
    {
        [Test]
        public void RunStart_RecordsBuildConfigAndSelectedWeaponFields()
        {
            P0Telemetry.BeginRun(RunMode.Normal, "policy-7", "assignment-a", "weapon_rapid_crossbow");

            Assert.That(P0Telemetry.TryGetEventSnapshot(P0Telemetry.RunStart, out P0Telemetry.EventSnapshot snapshot), Is.True);
            StringAssert.Contains("build_version=", snapshot.LastParametersText);
            StringAssert.Contains("config_version=policy-7", snapshot.LastParametersText);
            StringAssert.Contains("config_assignment_hash=assignment-a", snapshot.LastParametersText);
            StringAssert.Contains("selected_weapon_id=weapon_rapid_crossbow", snapshot.LastParametersText);
        }

        [Test]
        public void OfferTelemetry_RecordsVisibleSlotsAndKeepsDiagnosticsLocal()
        {
            P0Telemetry.BeginRun();
            CardOfferSnapshot snapshot = new CardOfferSnapshot(
                "run-local",
                7,
                2,
                42UL,
                "policy-7",
                "assignment-a",
                "state-a",
                4,
                new[]
                {
                    new CardOfferSlot(0, CardKind.BasicAttackUp, "card_a", 1.0f),
                    new CardOfferSlot(1, CardKind.MoveSpeedUp, "card_b", 2.0f),
                    new CardOfferSlot(2, CardKind.LegionBanner, "card_c", 3.0f),
                },
                new[]
                {
                    new CardOfferCandidate(CardKind.BasicAttackUp, "card_a", 1.0f),
                    new CardOfferCandidate(CardKind.MoveSpeedUp, "card_b", 2.0f),
                    new CardOfferCandidate(CardKind.LegionBanner, "card_c", 3.0f),
                    new CardOfferCandidate(CardKind.GuardShockwaveCrest, "card_d", 4.0f),
                });

            P0Telemetry.LogCardOfferGenerated(snapshot);
            P0Telemetry.LogCardOfferSelected(snapshot, snapshot.Slots[1], 250, false);

            Assert.That(P0Telemetry.TryGetEventSnapshot(P0Telemetry.CardOfferGenerated, out P0Telemetry.EventSnapshot generated), Is.True);
            StringAssert.Contains("offer_seed=42", generated.LastParametersText);
            StringAssert.Contains("candidate_count=4", generated.LastParametersText);
            StringAssert.Contains("offer_1_id=card_a", generated.LastParametersText);
            StringAssert.Contains("offer_3_weight=3", generated.LastParametersText);
            StringAssert.DoesNotContain("eligible_ids=", generated.LastParametersText);
            Assert.That(P0Telemetry.TryGetEventSnapshot(P0Telemetry.CardOfferDiagnostic, out P0Telemetry.EventSnapshot diagnostic), Is.True);
            StringAssert.Contains("eligible_ids=card_a;card_b;card_c;card_d", diagnostic.LastParametersText);
            Assert.That(P0Telemetry.TryGetEventSnapshot(P0Telemetry.CardOfferSelected, out P0Telemetry.EventSnapshot selected), Is.True);
            StringAssert.Contains("chosen_id=card_b", selected.LastParametersText);
            StringAssert.Contains("decision_time_ms=250", selected.LastParametersText);
            StringAssert.Contains("active_build_snapshot_id=run-local:2:42", selected.LastParametersText);
        }
    }
}
