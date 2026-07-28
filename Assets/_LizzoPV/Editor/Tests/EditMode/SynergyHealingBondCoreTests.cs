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
    public sealed class SynergyHealingBondCoreTests
    {
        [Test]
        public void ImmediateRound_UsesLowestLivingCompanionRatioAndStableTieBreak()
        {
            using Fixture fixture = new Fixture();
            TestCompanion first = fixture.Add("slot_b", 0.50f, new Vector3(3.0f, 0.0f));
            TestCompanion expected = fixture.Add("slot_a", 0.50f, new Vector3(1.0f, 0.0f));
            fixture.Add("commander", 0.10f, Vector3.zero, isCommander: true);

            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            Assert.IsTrue(fixture.Synergy.HasActiveZone);
            Assert.AreSame(expected, fixture.Synergy.ZoneOrigin);
            Assert.AreNotSame(first, fixture.Synergy.ZoneOrigin);
            Assert.AreEqual(expected.Position, fixture.Synergy.ZoneCenter);
        }

        [Test]
        public void EligibleHealing_ReplacesOneZoneWithLatestPendingOriginIncludingCommander()
        {
            using Fixture fixture = new Fixture();
            TestCompanion companion = fixture.Add("companion", 0.50f, Vector3.zero);
            TestCompanion commander = fixture.Add("commander", 0.25f, new Vector3(4.0f, 0.0f), isCommander: true);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));

            SynergyHealingEvent eligible = new SynergyHealingEvent(true, 1, false, false, false);
            Assert.IsTrue(fixture.Synergy.ReportHealing(companion, eligible));
            Assert.IsFalse(fixture.Synergy.ReportHealing(commander, eligible));
            Assert.IsTrue(fixture.Synergy.TryResolvePending(1.0f));

            Assert.AreSame(commander, fixture.Synergy.ZoneOrigin);
            Assert.AreEqual(commander.Position, fixture.Synergy.ZoneCenter);
            Assert.IsFalse(fixture.Synergy.IsAffected(commander));
            companion.Position = new Vector3(4.0f, 2.5f, 0.0f);
            Assert.IsTrue(fixture.Synergy.IsAffected(companion));
            Assert.AreEqual(0.80f, fixture.Synergy.GetDamageTakenMultiplier(companion));
            Assert.IsTrue(fixture.Synergy.HasKnockdownImmunity(companion));
        }

        [Test]
        public void ZoneMembership_TracksEntryLeaveBoundaryAndExpirationWithoutLos()
        {
            using Fixture fixture = new Fixture();
            TestCompanion origin = fixture.Add("origin", 0.20f, Vector3.zero);
            TestCompanion boundary = fixture.Add("boundary", 0.80f, new Vector3(2.5f, 0.0f));
            TestCompanion outside = fixture.Add("outside", 0.80f, new Vector3(2.51f, 0.0f));
            Assert.IsTrue(fixture.Synergy.TryResolvePending(2.0f));

            Assert.IsTrue(fixture.Synergy.IsAffected(origin));
            Assert.IsTrue(fixture.Synergy.IsAffected(boundary));
            Assert.IsFalse(fixture.Synergy.IsAffected(outside));
            boundary.Position = new Vector3(2.6f, 0.0f);
            Assert.IsFalse(fixture.Synergy.IsAffected(boundary));
            fixture.Synergy.Tick(5.0f);
            Assert.IsFalse(fixture.Synergy.HasActiveZone);
            Assert.AreEqual(1.0f, fixture.Synergy.GetDamageTakenMultiplier(origin));
        }

        [Test]
        public void InvalidHealing_DoesNotQueueOrReplaceAndResetDisposeClearState()
        {
            using Fixture fixture = new Fixture();
            TestCompanion origin = fixture.Add("origin", 0.20f, Vector3.zero);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            Vector3 initialCenter = fixture.Synergy.ZoneCenter;

            Assert.IsFalse(fixture.Synergy.ReportHealing(origin, new SynergyHealingEvent(false, 2, false, false, false)));
            Assert.IsFalse(fixture.Synergy.ReportHealing(origin, new SynergyHealingEvent(true, 0, false, false, false)));
            Assert.IsFalse(fixture.Synergy.ReportHealing(origin, new SynergyHealingEvent(true, 2, true, false, false)));
            Assert.IsFalse(fixture.Synergy.ReportHealing(origin, new SynergyHealingEvent(true, 2, false, true, false)));
            Assert.IsFalse(fixture.Synergy.ReportHealing(origin, new SynergyHealingEvent(true, 2, false, false, true)));
            Assert.IsFalse(fixture.Synergy.TryResolvePending(1.0f));
            Assert.AreEqual(initialCenter, fixture.Synergy.ZoneCenter);

            fixture.Synergy.Reset();
            Assert.IsFalse(fixture.Synergy.HasActiveZone);
            fixture.Synergy.Dispose();
            Assert.IsFalse(fixture.Synergy.HasActiveZone);
        }

        [Test]
        public void EligibleHealing_UsesLatestEventTimeOriginSnapshotEvenWhenTargetLaterMovesOrDies()
        {
            using Fixture fixture = new Fixture();
            TestCompanion first = fixture.Add("first", 0.20f, Vector3.zero);
            TestCompanion latest = fixture.Add("latest", 0.30f, new Vector3(2.0f, 0.0f));
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));

            SynergyHealingEvent eligible = new SynergyHealingEvent(true, 1, false, false, false);
            Assert.IsTrue(fixture.Synergy.ReportHealing(first, eligible));
            Assert.IsFalse(fixture.Synergy.ReportHealing(latest, eligible));
            latest.Position = new Vector3(9.0f, 0.0f);
            latest.IsLiving = false;

            Assert.IsTrue(fixture.Synergy.TryResolvePending(1.0f));
            Assert.AreSame(latest, fixture.Synergy.ZoneOrigin);
            Assert.AreEqual(new Vector3(2.0f, 0.0f), fixture.Synergy.ZoneCenter);
        }

        [Test]
        public void ImmediateRound_WithNoLivingNonCommander_ConsumesWithoutZone()
        {
            using Fixture fixture = new Fixture();
            fixture.Add("commander", 0.10f, Vector3.zero, isCommander: true);

            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            Assert.AreEqual(0, fixture.Synergy.ActiveZoneCount);
            Assert.IsFalse(fixture.Synergy.HasActiveZone);
        }

        sealed class Fixture : IDisposable
        {
            readonly LocalDataProvider _data;
            readonly SynergyActivationState _activations;
            readonly SynergyTriggerState _triggers;

            public Fixture()
            {
                TestAssetService assets = new TestAssetService();
                TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
                Assert.IsNotNull(gameData);
                assets.Register("PlayerData.xml", gameData);
                _data = new LocalDataProvider(assets);
                Assert.IsTrue(_data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
                _activations = new SynergyActivationState(_data);
                _triggers = new SynergyTriggerState(_activations);
                _activations.Refresh(CreateSlots("cleric", "field_herbalist", "wraith_knight"));
                World = new TestWorld();
                Synergy = new HealingBondSynergy(_data, _triggers, World);
            }

            public TestWorld World { get; }
            public HealingBondSynergy Synergy { get; }

            public TestCompanion Add(string stableIdentity, float hpRatio, Vector3 position, bool isCommander = false)
            {
                TestCompanion companion = new TestCompanion(stableIdentity, hpRatio, position, isCommander);
                World.Companions.Add(companion);
                return companion;
            }

            public void Dispose()
            {
                Synergy.Dispose();
                _triggers.Dispose();
                _activations.Dispose();
            }
        }

        sealed class TestWorld : HealingBondSynergy.IWorld
        {
            public readonly List<TestCompanion> Companions = new List<TestCompanion>();

            public void CollectCompanions(List<HealingBondSynergy.ICompanion> results)
            {
                results.Clear();
                for (int i = 0; i < Companions.Count; i++) results.Add(Companions[i]);
            }
        }

        sealed class TestCompanion : HealingBondSynergy.ICompanion
        {
            public TestCompanion(string stableIdentity, float hpRatio, Vector3 position, bool isCommander)
            {
                StableIdentity = stableIdentity;
                HealthRatio = hpRatio;
                Position = position;
                IsCommander = isCommander;
            }

            public string StableIdentity { get; }
            public float HealthRatio { get; }
            public Vector3 Position { get; set; }
            public bool IsCommander { get; }
            public bool IsLiving { get; set; } = true;
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int i = 0; i < slots.Length; i++)
            {
                string baseUnitId = i < baseUnitIds.Length ? baseUnitIds[i] : string.Empty;
                bool active = string.IsNullOrEmpty(baseUnitId) == false;
                slots[i] = new SquadSlotState($"squad_{i:00}", baseUnitId, string.Empty, active ? 1 : 0, 3, false, baseUnitId);
            }

            return Array.AsReadOnly(slots);
        }
    }
}
