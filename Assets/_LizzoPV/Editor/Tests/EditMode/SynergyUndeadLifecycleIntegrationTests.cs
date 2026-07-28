using System.IO;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyUndeadLifecycleIntegrationTests
    {
        [Test]
        public void RunServices_OwnsExactlyOneUndeadRunModuleOutsideRegistry()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            SynergySummonData summon = fixture.Data.GetSynergySummon("UNIT_SYNERGY_SKELETON_01");

            Assert.AreEqual(6, fixture.Data.SynergyDamages.Count);
            Assert.AreEqual(3, fixture.Data.SynergyEffects.Count);
            Assert.AreEqual(1, fixture.Data.SynergySummons.Count);
            CollectionAssert.AreEquivalent(
                new[] { "DMG_SYNERGY_GUARD_01", "DMG_SYNERGY_ARCHER_01", "DMG_SYNERGY_MAGIC_01", "DMG_SYNERGY_EXPLOSION_01", "DMG_SYNERGY_BEAST_01", "DOT_SYNERGY_BEAST_01" },
                new[]
                {
                    fixture.Data.GetSynergyDamage("DMG_SYNERGY_GUARD_01")?.Id,
                    fixture.Data.GetSynergyDamage("DMG_SYNERGY_ARCHER_01")?.Id,
                    fixture.Data.GetSynergyDamage("DMG_SYNERGY_MAGIC_01")?.Id,
                    fixture.Data.GetSynergyDamage("DMG_SYNERGY_EXPLOSION_01")?.Id,
                    fixture.Data.GetSynergyDamage("DMG_SYNERGY_BEAST_01")?.Id,
                    fixture.Data.GetSynergyDamage("DOT_SYNERGY_BEAST_01")?.Id,
                });
            CollectionAssert.AreEquivalent(
                new[] { "EFFECT_SYNERGY_GUARD_DR", "EFFECT_HEALING_BOND_DR", "EFFECT_MIXED_COMMAND" },
                new[]
                {
                    fixture.Data.GetSynergyEffect("EFFECT_SYNERGY_GUARD_DR")?.Id,
                    fixture.Data.GetSynergyEffect("EFFECT_HEALING_BOND_DR")?.Id,
                    fixture.Data.GetSynergyEffect("EFFECT_MIXED_COMMAND")?.Id,
                });
            Assert.IsNotNull(summon);
            Assert.AreEqual("synergy_undead_summon", summon.SynergyId);
            Assert.AreEqual(22, summon.Hp);
            Assert.AreEqual(5, summon.Damage);
            Assert.AreEqual(1.2f, summon.AttackInterval);
            Assert.AreEqual(1.0f, summon.Range);
            Assert.AreEqual(2.8f, summon.MoveSpeed);
            Assert.AreEqual(0.2f, summon.AiScanInterval);
            Assert.AreEqual("battle_end_or_hp0", summon.LifetimeRuleId);
            Assert.AreEqual(CombatTargetRule.Nearest, summon.TargetRule);
            Assert.AreEqual(5, summon.ActiveCap);
            Assert.AreEqual(3, summon.BossLockCount);
            Assert.AreEqual(1, summon.FrameSpawnCap);
            Assert.AreEqual("summon_object,companion_tag=false,no_family_tag", summon.Tags);
            Assert.AreEqual("battle_end", summon.ResetRuleId);
            Assert.AreEqual("rc_synergy_skeleton_stats", summon.RemoteConfigKey);
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", summon.DistinctFromSummonId);
            Assert.IsNotNull(fixture.Run.UndeadSummon);
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("UndeadSummon"));
        }

        [Test]
        public void RunAndBossOrchestration_UsesTheRequiredPublicLifecycleOrder()
        {
            string bootstrap = File.ReadAllText("Assets/_LizzoPV/Scripts/Runtime/Core/Bootstrap/RunBootstrap.cs");
            string services = File.ReadAllText("Assets/_LizzoPV/Scripts/Runtime/Core/Bootstrap/RunServices.cs");
            string boss = File.ReadAllText("Assets/_LizzoPV/Scripts/Runtime/Gameplay/Spawning/BossSpawnController.cs");

            Assert.Less(bootstrap.IndexOf("Services.SynergyTriggers.Tick("), bootstrap.IndexOf("Services.UndeadSummon.TryResolvePending("));
            Assert.Less(bootstrap.IndexOf("Services.UndeadSummon.TryResolvePending("), bootstrap.IndexOf("Services.UndeadSummon.Tick("));
            Assert.GreaterOrEqual(bootstrap.IndexOf("Services.UndeadSummon.ResetForResult();"), 0);
            Assert.GreaterOrEqual(bootstrap.IndexOf("Services.UndeadSummon?.ResetForResult();"), 0);
            Assert.Less(services.IndexOf("UndeadSummon.Dispose();"), services.IndexOf("Party.Dispose();"));
            Assert.Less(boss.IndexOf("hungryGiant.Setup(monster);"), boss.IndexOf("_services.UndeadSummon.OnBossPhaseStarted(monster, Time.time);"));
            Assert.Less(boss.IndexOf("_services.UndeadSummon.OnBossPhaseStarted(monster, Time.time);"), boss.IndexOf("_uiController?.HideBossPreWarning();"));
        }
    }
}
