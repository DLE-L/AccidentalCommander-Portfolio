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
    public sealed class SharedSynergyActivationLifecycleTests
    {
        [Test]
        public void ExactFamilyConditions_ActivateOnceAndIgnoreReinforcePromotionAndChildren()
        {
            SynergyActivationState state = new SynergyActivationState(CreateProjectProvider());
            int activationCount = 0;
            state.Activated += _ => activationCount++;

            state.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric"));

            Assert.IsTrue(state.IsActive(SynergyActivationIds.GuardShockwave));
            Assert.AreEqual("squad_00", state.GetRepresentativeRosterSlotId(SynergyActivationIds.GuardShockwave));
            Assert.AreEqual(1, activationCount);

            state.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric"));
            state.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric"));
            Assert.AreEqual(1, activationCount);
            Assert.IsTrue(state.IsActive(SynergyActivationIds.GuardShockwave));
        }

        [Test]
        public void EightExactConditions_UseDistinctActiveSquadsAndBaseFamilyTags()
        {
            SynergyActivationState state = new SynergyActivationState(CreateProjectProvider());

            state.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric", "falcon_archer", "field_herbalist", "bombardier", "fire_mage"));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.GuardShockwave));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.ArcherRain));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.MixedCommand));

            state.Reset();
            state.Refresh(CreateSlots("fire_mage", "lightning_mage", "necromancer"));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.MagicChain));

            state.Reset();
            state.Refresh(CreateSlots("bombardier", "fire_mage", "skeleton_bomber"));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.ExplosionChain));

            state.Reset();
            state.Refresh(CreateSlots("falcon_archer", "wolf_tamer"));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.BeastHunt));

            state.Reset();
            state.Refresh(CreateSlots("wraith_knight", "necromancer", "skeleton_bomber"));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.UndeadSummon));

            state.Reset();
            state.Refresh(CreateSlots("cleric", "field_herbalist", "wraith_knight"));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.HealingBond));

            state.Reset();
            state.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage"));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.MixedCommand));
        }

        [Test]
        public void FormationRepresentatives_AreDeterministicAndOnlyReselectWhenTheRosterSlotIsRemoved()
        {
            SynergyActivationState state = new SynergyActivationState(CreateProjectProvider());
            state.Refresh(CreateSlots("field_herbalist", "falcon_archer", "bombardier"));

            Assert.IsTrue(state.IsActive(SynergyActivationIds.ArcherRain));
            Assert.AreEqual("squad_00", state.GetRepresentativeRosterSlotId(SynergyActivationIds.ArcherRain));

            state.Refresh(CreateSlots("", "falcon_archer", "bombardier"));
            Assert.AreEqual("squad_01", state.GetRepresentativeRosterSlotId(SynergyActivationIds.ArcherRain));

            state.Refresh(CreateSlots("", "", ""));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.ArcherRain));
            Assert.IsNull(state.GetRepresentativeRosterSlotId(SynergyActivationIds.ArcherRain));

            state.Reset();
            Assert.IsFalse(state.IsActive(SynergyActivationIds.ArcherRain));
            Assert.AreEqual(0, state.ActiveCount);
        }

        [Test]
        public void SnapshotView_IsStableAndDoesNotCountNonRosterChildren()
        {
            SynergyActivationState state = new SynergyActivationState(CreateProjectProvider());
            IReadOnlyList<SynergyActivationSnapshot> snapshot = state.Snapshot;
            state.Refresh(CreateSlots("fire_mage", "lightning_mage", "necromancer"));

            Assert.AreSame(snapshot, state.Snapshot);
            Assert.AreEqual(8, snapshot.Count);
            Assert.AreEqual(1, state.ActiveCount);
            Assert.IsTrue(state.IsActive(SynergyActivationIds.MagicChain));
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
