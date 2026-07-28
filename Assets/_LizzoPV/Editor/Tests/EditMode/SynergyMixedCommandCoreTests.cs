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
    public sealed class SynergyMixedCommandCoreTests
    {
        [Test]
        public void Constructor_RequiresCanonicalTypedEffectData()
        {
            using Fixture fixture = new Fixture();
            SynergyEffectData effect = fixture.Data.GetSynergyEffect("EFFECT_MIXED_COMMAND");

            Assert.IsNotNull(effect);
            Assert.AreEqual("synergy_mixed_command", effect.SynergyId);
            Assert.AreEqual(15.0f, effect.CadenceSeconds);
            Assert.AreEqual(5.0f, effect.DurationSeconds);
            Assert.AreEqual(1.15f, effect.AttackIntervalDivisor);
            Assert.AreEqual(1.15f, effect.MoveSpeedMultiplier);
            Assert.IsTrue(effect.AllAliveCompanions);
            Assert.IsTrue(effect.CompanionsOnly);
            Assert.IsTrue(effect.ExcludesCompanionTagFalseSummons);
            Assert.IsTrue(effect.SameSourceRefresh);
            Assert.IsFalse(effect.NumericStackingAllowed);
            Assert.AreEqual("all_alive_companions", effect.ActivationRuleId);
            Assert.AreEqual("same_source_refresh_no_multiplier_stack", effect.StackRuleId);
            Assert.AreEqual("rc_mixed_command_multiplier", effect.RemoteConfigKey);
            Assert.Throws<InvalidOperationException>(() => new MixedCommandSynergy(null, fixture.Triggers, fixture.World));
        }

        [Test]
        public void ImmediateRound_SnapshotsOnlyLivingNonCommanderCompanionTagTargets()
        {
            using Fixture fixture = new Fixture();
            TestCompanion living = fixture.Add("living");
            TestCompanion downed = fixture.Add("downed", isLiving: false);
            TestCompanion commander = fixture.Add("commander", isCommander: true);
            TestCompanion summon = fixture.Add("summon", hasCompanionTag: false);

            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            Assert.AreEqual(1, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(5.0f, fixture.Synergy.ExpiresAt);
            Assert.AreEqual(1.15f, fixture.Synergy.GetAttackIntervalDivisor(living));
            Assert.AreEqual(1.15f, fixture.Synergy.GetMoveSpeedMultiplier(living));
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(downed));
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(commander));
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(summon));
        }

        [Test]
        public void Tick_RemovesDownedTargetWithoutRestoringItOnRevivalAndExpiresAtFiveSeconds()
        {
            using Fixture fixture = new Fixture();
            TestCompanion target = fixture.Add("target");
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));

            target.IsLiving = false;
            fixture.Synergy.Tick(1.0f);
            Assert.AreEqual(0, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(1.0f, fixture.Synergy.GetMoveSpeedMultiplier(target));

            target.IsLiving = true;
            fixture.Synergy.Tick(2.0f);
            Assert.AreEqual(1.0f, fixture.Synergy.GetMoveSpeedMultiplier(target));

            fixture.Triggers.Tick(15.0f, true, false, 1);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(15.0f));
            Assert.AreEqual(1.15f, fixture.Synergy.GetMoveSpeedMultiplier(target));
            fixture.Synergy.Tick(20.0f);
            Assert.AreEqual(0, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(1.0f, fixture.Synergy.GetMoveSpeedMultiplier(target));
        }

        [Test]
        public void TimedRound_RefreshesCurrentSnapshotWithoutNumericStackingAndConsumesNoTargetRound()
        {
            using Fixture fixture = new Fixture();
            TestCompanion first = fixture.Add("first");
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            Assert.AreEqual(1.15f, fixture.Synergy.GetAttackIntervalDivisor(first));

            TestCompanion second = fixture.Add("second");
            fixture.Triggers.Tick(15.0f, true, false, 1);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(15.0f));
            Assert.AreEqual(2, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(20.0f, fixture.Synergy.ExpiresAt);
            Assert.AreEqual(1.15f, fixture.Synergy.GetAttackIntervalDivisor(first));
            Assert.AreEqual(1.15f, fixture.Synergy.GetAttackIntervalDivisor(second));

            first.IsLiving = false;
            second.IsLiving = false;
            fixture.Triggers.Tick(15.0f, true, false, 2);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(30.0f));
            Assert.AreEqual(0, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(first));
        }

        [Test]
        public void ResetAndDispose_ClearTargetsAndRemainNeutralAndIdempotent()
        {
            using Fixture fixture = new Fixture();
            TestCompanion target = fixture.Add("target");
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));

            fixture.Synergy.Reset();
            Assert.AreEqual(0, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(target));

            fixture.Synergy.Dispose();
            fixture.Synergy.Dispose();
            Assert.IsFalse(fixture.Synergy.TryResolvePending(15.0f));
            Assert.AreEqual(1.0f, fixture.Synergy.GetMoveSpeedMultiplier(target));
        }

        sealed class Fixture : IDisposable
        {
            readonly SynergyActivationState _activations;

            public Fixture()
            {
                TestAssetService assets = new TestAssetService();
                TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
                Assert.IsNotNull(gameData);
                assets.Register("PlayerData.xml", gameData);
                Data = new LocalDataProvider(assets);
                Assert.IsTrue(Data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
                _activations = new SynergyActivationState(Data);
                Triggers = new SynergyTriggerState(_activations);
                _activations.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage"));
                World = new TestWorld();
                Synergy = new MixedCommandSynergy(Data, Triggers, World);
            }

            public LocalDataProvider Data { get; }
            public SynergyTriggerState Triggers { get; }
            public TestWorld World { get; }
            public MixedCommandSynergy Synergy { get; }

            public TestCompanion Add(string stableIdentity, bool isLiving = true, bool isCommander = false, bool hasCompanionTag = true)
            {
                TestCompanion companion = new TestCompanion(stableIdentity, isLiving, isCommander, hasCompanionTag);
                World.Companions.Add(companion);
                return companion;
            }

            public void Dispose()
            {
                Synergy.Dispose();
                Triggers.Dispose();
                _activations.Dispose();
            }
        }

        sealed class TestWorld : MixedCommandSynergy.IWorld
        {
            public readonly List<TestCompanion> Companions = new List<TestCompanion>();

            public void CollectCompanions(List<MixedCommandSynergy.ICompanion> results)
            {
                results.Clear();
                for (int i = 0; i < Companions.Count; i++)
                    results.Add(Companions[i]);
            }
        }

        sealed class TestCompanion : MixedCommandSynergy.ICompanion
        {
            public TestCompanion(string stableIdentity, bool isLiving, bool isCommander, bool hasCompanionTag)
            {
                StableIdentity = stableIdentity;
                IsLiving = isLiving;
                IsCommander = isCommander;
                HasCompanionTag = hasCompanionTag;
            }

            public string StableIdentity { get; }
            public bool IsLiving { get; set; }
            public bool IsCommander { get; }
            public bool HasCompanionTag { get; }
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
