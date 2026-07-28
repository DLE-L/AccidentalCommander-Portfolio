using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SharedSynergyTriggerStateTests
    {
        [Test]
        public void ImmediateAndTimedTriggers_ConsumeNoTargetRoundsAndFreezeWhilePaused()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric"));

                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out SynergyTriggerPayload immediate));
                Assert.AreEqual(SynergyTriggerKind.Immediate, immediate.Kind);
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.GuardShockwave));

                triggers.Tick(11.9f, runReady: true, paused: false, frameId: 1);
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.GuardShockwave));
                triggers.Tick(20.0f, runReady: true, paused: true, frameId: 2);
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.GuardShockwave));
                triggers.Tick(0.1f, runReady: true, paused: false, frameId: 3);

                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out SynergyTriggerPayload timed));
                Assert.AreEqual(SynergyTriggerKind.Timed, timed.Kind);
            }
        }

        [Test]
        public void MagicAndHealingEvents_RequireExactEligibilityAndDedupeMissionIds()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("fire_mage", "lightning_mage", "necromancer", "cleric", "field_herbalist", "wraith_knight"));
                ConsumeImmediate(triggers, SynergyActivationIds.MagicChain);
                ConsumeImmediate(triggers, SynergyActivationIds.HealingBond);

                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_0", true, false, true, false)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_1", false, true, true, false)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_2", true, true, false, true)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_3", true, true, true, false)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_3", true, true, true, false)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_4", true, true, true, false)));
                Assert.IsTrue(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_5", true, true, true, false)));
                Assert.AreEqual(0, triggers.GetCounter(SynergyActivationIds.MagicChain));
                Assert.IsTrue(triggers.HasPending(SynergyActivationIds.MagicChain));

                Assert.IsFalse(triggers.ReportHealing(new SynergyHealingEvent(false, 4, false, false, false)));
                Assert.IsFalse(triggers.ReportHealing(new SynergyHealingEvent(true, 0, false, false, false)));
                Assert.IsFalse(triggers.ReportHealing(new SynergyHealingEvent(true, 4, true, false, false)));
                Assert.IsTrue(triggers.ReportHealing(new SynergyHealingEvent(true, 1, false, false, false)));
                Assert.IsTrue(triggers.HasPending(SynergyActivationIds.HealingBond));
            }
        }

        [Test]
        public void ExplosionAndUndeadDeaths_CarryOverflowBlockSameScopeAndClampAtFullCap()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("bombardier", "fire_mage", "skeleton_bomber", "wraith_knight", "necromancer"));
                ConsumeImmediate(triggers, SynergyActivationIds.ExplosionChain);
                ConsumeImmediate(triggers, SynergyActivationIds.UndeadSummon);

                for (int i = 0; i < 8; i++)
                    triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent($"explosion_{i}", SynergyDeathSourceCategory.Companion, false, false, false, 0L, 10));

                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.ExplosionChain, out SynergyTriggerPayload explosion));
                Assert.AreEqual(SynergyTriggerKind.ExplosionKills, explosion.Kind);
                Assert.IsFalse(triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent("same_scope", SynergyDeathSourceCategory.Synergy, false, false, true, explosion.ResolutionScopeId, 10)));
                Assert.AreEqual(0, triggers.GetCounter(SynergyActivationIds.ExplosionChain));

                triggers.SetUndeadAliveCapFull(true);
                for (int i = 0; i < 20; i++)
                    triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent($"undead_{i}", SynergyDeathSourceCategory.Commander, false, false, false, 0L, 11 + i));

                Assert.AreEqual(14, triggers.GetCounter(SynergyActivationIds.UndeadSummon));
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.UndeadSummon));
                triggers.SetUndeadAliveCapFull(false);
                Assert.IsTrue(triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent("undead_release", SynergyDeathSourceCategory.Commander, false, false, false, 0L, 40)));
                Assert.IsTrue(triggers.HasPending(SynergyActivationIds.UndeadSummon));
            }
        }

        [Test]
        public void UndeadOverflow_AfterConsumedPendingQueuesOnlyOnLaterFrame()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("necromancer", "wraith_knight", "skeleton_bomber"));
                ConsumeImmediate(triggers, SynergyActivationIds.UndeadSummon);

                for (int i = 0; i < 30; i++)
                    triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent($"undead_overflow_{i}", SynergyDeathSourceCategory.Commander, false, false, false, 0L, i + 1));

                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.UndeadSummon, out SynergyTriggerPayload first));
                Assert.AreEqual(SynergyTriggerKind.UndeadKills, first.Kind);
                Assert.AreEqual(15, first.FrameId);
                Assert.AreEqual(15, triggers.GetCounter(SynergyActivationIds.UndeadSummon));

                triggers.Tick(0.1f, runReady: true, paused: false, frameId: 15);
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.UndeadSummon));
                triggers.Tick(0.1f, runReady: true, paused: false, frameId: 16);

                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.UndeadSummon, out SynergyTriggerPayload overflow));
                Assert.AreEqual(SynergyTriggerKind.UndeadKills, overflow.Kind);
                Assert.AreEqual(16, overflow.FrameId);
                Assert.AreEqual(0, triggers.GetCounter(SynergyActivationIds.UndeadSummon));
            }
        }

        [Test]
        public void Reset_ClearsPendingCountersDedupeAndClockWithoutReactivating()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("fire_mage", "lightning_mage", "necromancer"));
                ConsumeImmediate(triggers, SynergyActivationIds.MagicChain);
                triggers.ReportMagicCast(new SynergyMagicCastEvent("persisted_id", true, true, true, false));
                triggers.Reset();

                Assert.AreEqual(0, triggers.PendingCount);
                Assert.AreEqual(0, triggers.GetCounter(SynergyActivationIds.MagicChain));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("persisted_id", true, true, true, false)));
                Assert.AreEqual(1, triggers.GetCounter(SynergyActivationIds.MagicChain));
            }
        }

        static void ConsumeImmediate(SynergyTriggerState triggers, string synergyId)
        {
            Assert.IsTrue(triggers.TryConsumePending(synergyId, out SynergyTriggerPayload payload));
            Assert.AreEqual(SynergyTriggerKind.Immediate, payload.Kind);
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return provider;
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int i = 0; i < slots.Length; i++)
            {
                string baseUnitId = i < baseUnitIds.Length ? baseUnitIds[i] : string.Empty;
                bool active = string.IsNullOrEmpty(baseUnitId) == false;
                slots[i] = new SquadSlotState(
                    $"squad_{i:00}",
                    baseUnitId,
                    string.Empty,
                    active ? 1 : 0,
                    3,
                    false,
                    baseUnitId);
            }

            return Array.AsReadOnly(slots);
        }
    }
}
