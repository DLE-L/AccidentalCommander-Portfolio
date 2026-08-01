using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PartyRosterStateTests
    {
        [Test]
        public void CanonicalBaseIds_AreAccepted_AndLegacyOrChildIdsAreRejectedWithoutMutation()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            IReadOnlyList<CompanionRosterData> roster = provider.CompanionRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                PartyRosterState state = new PartyRosterState(provider);

                Assert.AreEqual(PartyRosterChangeResult.Recruit, state.TryAdd(roster[i].UnitId));
                Assert.AreEqual(roster[i].UnitId, state.Snapshot[0].BaseUnitId);
                Assert.AreEqual(1, state.ActiveSquadCount);
            }

            string[] invalidIds = { "archer", "crossbow", "bear", "skeleton", "shield_captain", "wolf" };
            for (int i = 0; i < invalidIds.Length; i++)
            {
                PartyRosterState state = new PartyRosterState(provider);

                Assert.AreEqual(PartyRosterChangeResult.RejectedUnknown, state.TryAdd(invalidIds[i]));
                Assert.AreEqual(0, state.ActiveSquadCount);
                AssertEmptySnapshot(state.Snapshot);
            }
        }

        [Test]
        public void SameBase_RecruitsReinforcesPromotesWithoutMovingAndThenRejectsMaxed()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            PartyRosterState state = new PartyRosterState(provider);
            IReadOnlyList<SquadSlotState> snapshot = state.Snapshot;

            Assert.AreEqual(PartyRosterChangeResult.Recruit, state.TryAdd("shield_guard"));
            Assert.AreSame(snapshot, state.Snapshot);
            AssertSlot(snapshot[0], "squad_00", "shield_guard", "shield_guard", 1, false);

            Assert.AreEqual(PartyRosterChangeResult.Reinforce, state.TryAdd("shield_guard"));
            AssertSlot(snapshot[0], "squad_00", "shield_guard", "shield_guard", 2, false);

            Assert.AreEqual(PartyRosterChangeResult.Promote, state.TryAdd("shield_guard"));
            AssertSlot(snapshot[0], "squad_00", "shield_guard", "shield_captain", 3, true);
            Assert.AreEqual(1, state.ActiveSquadCount);

            Assert.AreEqual(PartyRosterChangeResult.RejectedMaxed, state.TryAdd("shield_guard"));
            AssertSlot(snapshot[0], "squad_00", "shield_guard", "shield_captain", 3, true);

            state.Reset();
            Assert.AreSame(snapshot, state.Snapshot);
            AssertEmptySnapshot(snapshot);
        }

        [Test]
        public void SevenDistinctBases_BlockAnEighthButAllowOwnedReinforcementAndPromotion()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            PartyRosterState state = new PartyRosterState(provider);
            IReadOnlyList<CompanionRosterData> roster = provider.CompanionRoster;

            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(PartyRosterChangeResult.Recruit, state.TryAdd(roster[i].UnitId));
                Assert.AreEqual($"squad_{i:00}", state.Snapshot[i].SlotId);
                Assert.AreEqual(roster[i].UnitId, state.Snapshot[i].BaseUnitId);
            }

            Assert.AreEqual(7, state.ActiveSquadCount);
            Assert.AreEqual(PartyRosterChangeResult.RejectedFull, state.TryAdd(roster[7].UnitId));
            Assert.AreEqual(7, state.ActiveSquadCount);
            Assert.AreEqual(PartyRosterChangeResult.Reinforce, state.TryAdd(roster[0].UnitId));
            Assert.AreEqual(PartyRosterChangeResult.Promote, state.TryAdd(roster[0].UnitId));
            AssertSlot(state.Snapshot[0], "squad_00", roster[0].UnitId, "shield_captain", 3, true);
            Assert.AreEqual(7, state.ActiveSquadCount);
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
        }

        static void AssertSlot(SquadSlotState slot, string slotId, string baseUnitId, string leaderUnitId, int count, bool promoted)
        {
            Assert.AreEqual(slotId, slot.SlotId);
            Assert.AreEqual(baseUnitId, slot.BaseUnitId);
            Assert.AreEqual(leaderUnitId, slot.LeaderUnitId);
            Assert.AreEqual(count, slot.CurrentCount);
            Assert.AreEqual(3, slot.MaxCount);
            Assert.AreEqual(promoted, slot.IsPromoted);
        }

        static void AssertEmptySnapshot(IReadOnlyList<SquadSlotState> snapshot)
        {
            Assert.AreEqual(7, snapshot.Count);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Assert.AreEqual($"squad_{i:00}", snapshot[i].SlotId);
                Assert.IsFalse(snapshot[i].IsActive);
                Assert.IsEmpty(snapshot[i].BaseUnitId);
                Assert.IsEmpty(snapshot[i].LeaderUnitId);
            }
        }
    }

    public sealed class PartyServiceRosterIntegrationTests
    {
        [Test]
        public void PartyService_ExposesCanonicalFixedRosterSnapshotAndCompatibilityPreviewMapping()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            PartyService party = fixture.Run.Party;

            Assert.AreEqual(0, party.ActiveCompanionSlotCount);
            Assert.AreEqual(7, party.ActiveCompanionSlotCap);
            Assert.AreEqual(7, party.FreeCompanionSlots);
            Assert.AreEqual(0, party.PromotionReadyCount);
            Assert.AreEqual(7, party.GetSquadSlotSnapshot().Count);
            Assert.AreEqual("squad_00", party.GetSquadSlotSnapshot()[0].SlotId);

            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewRosterRecruit(CompanionKind.ShieldSoldier));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewRosterRecruit(CompanionKind.Swordsman));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewRosterRecruit(CompanionKind.Cleric));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewRosterRecruit(CompanionKind.Archer));
            Assert.AreEqual(PartyRosterChangeResult.RejectedUnknown, party.PreviewRosterRecruit(CompanionKind.ShieldCaptain));
        }
    }
}
