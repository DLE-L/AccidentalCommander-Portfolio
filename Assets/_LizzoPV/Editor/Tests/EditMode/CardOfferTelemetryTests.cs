using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Telemetry;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CardOfferTelemetryTests
    {
        [Test]
        public void RunStart_RecordsBuildConfigAndSelectedWeaponFields()
        {
            RunTelemetry.BeginRun(RunMode.Normal, "policy-7", "assignment-a", "weapon_rapid_crossbow");

            Assert.That(RunTelemetry.TryGetEventSnapshot(RunTelemetry.RunStart, out RunTelemetry.EventSnapshot snapshot), Is.True);
            StringAssert.Contains("build_version=", snapshot.LastParametersText);
            StringAssert.Contains("config_version=policy-7", snapshot.LastParametersText);
            StringAssert.Contains("config_assignment_hash=assignment-a", snapshot.LastParametersText);
            StringAssert.Contains("selected_weapon_id=weapon_rapid_crossbow", snapshot.LastParametersText);
        }

        [Test]
        public void RunStart_EditorSessionDoesNotReportMissingBuildIdentity()
        {
            RunTelemetry.BeginRun();

            Assert.That(RunTelemetry.HasLogged(RunTelemetry.EditorSession), Is.True);
            Assert.That(RunTelemetry.HasLogged(RunTelemetry.BuildIdentityMissing), Is.False);
        }

        [Test]
        public void OfferTelemetry_RecordsVisibleSlotsAndKeepsDiagnosticsLocal()
        {
            RunTelemetry.BeginRun();
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

            RunTelemetry.LogCardOfferGenerated(snapshot);
            RunTelemetry.LogCardOfferSelected(snapshot, snapshot.Slots[1], 250, false);

            Assert.That(RunTelemetry.TryGetEventSnapshot(RunTelemetry.CardOfferGenerated, out RunTelemetry.EventSnapshot generated), Is.True);
            StringAssert.Contains("offer_seed=42", generated.LastParametersText);
            StringAssert.Contains("candidate_count=4", generated.LastParametersText);
            StringAssert.Contains("offer_1_id=card_a", generated.LastParametersText);
            StringAssert.Contains("offer_3_weight=3", generated.LastParametersText);
            StringAssert.DoesNotContain("eligible_ids=", generated.LastParametersText);
            Assert.That(RunTelemetry.TryGetEventSnapshot(RunTelemetry.CardOfferDiagnostic, out RunTelemetry.EventSnapshot diagnostic), Is.True);
            StringAssert.Contains("eligible_ids=card_a;card_b;card_c;card_d", diagnostic.LastParametersText);
            Assert.That(RunTelemetry.TryGetEventSnapshot(RunTelemetry.CardOfferSelected, out RunTelemetry.EventSnapshot selected), Is.True);
            StringAssert.Contains("chosen_id=card_b", selected.LastParametersText);
            StringAssert.Contains("decision_time_ms=250", selected.LastParametersText);
            StringAssert.Contains("active_build_snapshot_id=run-local:2:42", selected.LastParametersText);
        }
    }
}
