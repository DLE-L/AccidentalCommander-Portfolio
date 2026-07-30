using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class DamageContributionSummaryTelemetryTests
    {
        [Test]
        public void FormatSummaries_UsesCanonicalOrderAndExactFields()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            using DamageContributionLedger ledger = new DamageContributionLedger(fixture.Data);
            ledger.RecordAppliedDamage(SynergyActivationIds.GuardShockwave, 5);
            ledger.RecordPreventedDamage(SynergyActivationIds.GuardShockwave, 3);
            ledger.RecordAppliedDamage("shield_guard", 10, 1.25f);
            ledger.RecordAppliedDamage("shield_captain", 2);
            ledger.RecordAppliedDamage("necromancer:UNIT_PERSONAL_SKELETON_01", 7);

            DamageContributionSnapshot snapshot = ledger.CaptureSnapshot(new[]
            {
                new SynergyActivationSnapshot(SynergyActivationIds.GuardShockwave, true, null),
                new SynergyActivationSnapshot(SynergyActivationIds.MixedCommand, true, null),
            });

            Assert.That(
                DamageContributionSummaryTelemetry.FormatSynergySummary("clear", snapshot),
                Is.EqualTo(
                    "{\"result\":\"clear\",\"winner_id\":\"synergy_guard_shockwave\",\"synergies\":["
                    + "{\"id\":\"synergy_guard_shockwave\",\"direct_damage\":5,\"prevented_damage\":3,\"attributed_mixed_command_bonus_damage\":0,\"total_score\":8,\"active\":true},"
                    + "{\"id\":\"synergy_archer_rain\",\"direct_damage\":0,\"prevented_damage\":0,\"attributed_mixed_command_bonus_damage\":0,\"total_score\":0,\"active\":false},"
                    + "{\"id\":\"synergy_magic_chain\",\"direct_damage\":0,\"prevented_damage\":0,\"attributed_mixed_command_bonus_damage\":0,\"total_score\":0,\"active\":false},"
                    + "{\"id\":\"synergy_explosion_chain\",\"direct_damage\":0,\"prevented_damage\":0,\"attributed_mixed_command_bonus_damage\":0,\"total_score\":0,\"active\":false},"
                    + "{\"id\":\"synergy_beast_hunt\",\"direct_damage\":0,\"prevented_damage\":0,\"attributed_mixed_command_bonus_damage\":0,\"total_score\":0,\"active\":false},"
                    + "{\"id\":\"synergy_undead_summon\",\"direct_damage\":0,\"prevented_damage\":0,\"attributed_mixed_command_bonus_damage\":0,\"total_score\":0,\"active\":false},"
                    + "{\"id\":\"synergy_healing_bond\",\"direct_damage\":0,\"prevented_damage\":0,\"attributed_mixed_command_bonus_damage\":0,\"total_score\":0,\"active\":false},"
                    + "{\"id\":\"synergy_mixed_command\",\"direct_damage\":0,\"prevented_damage\":0,\"attributed_mixed_command_bonus_damage\":2,\"total_score\":2,\"active\":true}]}"));

            Assert.That(
                DamageContributionSummaryTelemetry.FormatCompanionSummary("clear", snapshot),
                Is.EqualTo(
                    "{\"result\":\"clear\",\"companions\":["
                    + "{\"id\":\"shield_guard\",\"applied_hp_damage\":12},"
                    + "{\"id\":\"sword_soldier\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"cleric\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"falcon_archer\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"field_herbalist\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"bombardier\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"fire_mage\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"lightning_mage\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"wolf_tamer\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"wraith_knight\",\"applied_hp_damage\":0},"
                    + "{\"id\":\"necromancer\",\"applied_hp_damage\":7},"
                    + "{\"id\":\"skeleton_bomber\",\"applied_hp_damage\":0}]}"));
        }

        [Test]
        public void FormatSummaries_ReportsNoWinnerAndZeroEntries()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            using DamageContributionLedger ledger = new DamageContributionLedger(fixture.Data);
            DamageContributionSnapshot snapshot = ledger.CaptureSnapshot();

            string synergy = DamageContributionSummaryTelemetry.FormatSynergySummary("failure", snapshot);
            string companion = DamageContributionSummaryTelemetry.FormatCompanionSummary("failure", snapshot);

            Assert.That(synergy, Does.StartWith("{\"result\":\"failure\",\"winner_id\":\"none\",\"synergies\":["));
            Assert.That(synergy, Does.Not.Contain("\"active\":true"));
            Assert.That(synergy, Does.Not.Contain("\"total_score\":1"));
            Assert.That(companion, Does.StartWith("{\"result\":\"failure\",\"companions\":["));
            Assert.That(companion, Does.Not.Contain("\"applied_hp_damage\":1"));
        }

        [Test]
        public void Emit_ProducesExactlyOneSummaryEventPerTerminalOutcome()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            using DamageContributionLedger ledger = new DamageContributionLedger(fixture.Data);
            DamageContributionSnapshot snapshot = ledger.CaptureSnapshot();
            List<string> events = new List<string>();

            DamageContributionSummaryTelemetry.Emit("clear", snapshot, (eventName, payload) => events.Add(eventName + "|" + payload));
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[0], Does.StartWith("synergy_contribution_summary|{\"result\":\"clear\""));
            Assert.That(events[1], Does.StartWith("companion_damage_contribution_summary|{\"result\":\"clear\""));

            events.Clear();
            DamageContributionSummaryTelemetry.Emit("failure", snapshot, (eventName, payload) => events.Add(eventName + "|" + payload));
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[0], Does.StartWith("synergy_contribution_summary|{\"result\":\"failure\""));
            Assert.That(events[1], Does.StartWith("companion_damage_contribution_summary|{\"result\":\"failure\""));
        }

        [TestCase("clear")]
        [TestCase("failure")]
        public void LegacyRunEndSummaries_KeepDamageTakenAndCompanionDownEvents(string result)
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            P0Telemetry.BeginRun();
            P0PlaytestDiagnostics.ConfigureParty(fixture.Run.Party);
            try
            {
                P0PlaytestDiagnostics.RecordCompanionDamage("shield_guard", 4, 75, "enemy_contact");
                P0PlaytestDiagnostics.RecordCompanionDown("shield_guard", "enemy_contact");

                P0Telemetry.EndRun(result, 0);

                Assert.That(P0Telemetry.GetCount(P0Telemetry.CompanionDamageSummary), Is.EqualTo(1));
                Assert.That(P0Telemetry.GetCount(P0Telemetry.CompanionDownCountPreBoss), Is.EqualTo(1));
                Assert.That(P0Telemetry.GetCount(P0Telemetry.CompanionDownReasonSummary), Is.EqualTo(1));
                Assert.That(P0Telemetry.TryGetEventSnapshot(P0Telemetry.CompanionDamageSummary, out P0Telemetry.EventSnapshot snapshot), Is.True);
                StringAssert.Contains("damage_by_unit=shield_guard=4", snapshot.LastParametersText);
            }
            finally
            {
                P0PlaytestDiagnostics.ClearParty();
            }
        }
    }
}
