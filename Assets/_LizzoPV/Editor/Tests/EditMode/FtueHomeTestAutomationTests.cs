using System;
using System.Collections.Generic;
using Lizzo.PV.EditorTools;
using Lizzo.PV.EditorTools.UI.Theming.Draft;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.EditorTests
{
    [Category("FtueRunPause")]
    public sealed class FtueHomeTestActionsTests
    {
        [SetUp]
        public void ResetTestLifecycleState()
        {
            FtueHomeTestActions.ResetMissingSynergyPresentationReportsForTests();
        }

        [Test]
        public void DraftThemeIndexRetainsLobbyOnlyEntry()
        {
            DraftUIThemeIndex index = AssetDatabase.LoadAssetAtPath<DraftUIThemeIndex>(
                "Assets/_LizzoPV/Data/UI/DraftTheme/DraftUIThemeIndex.asset");
            Assert.That(index, Is.Not.Null);
            Assert.That(index.Entries, Has.Count.EqualTo(1));
            Assert.That(index.Entries[0].SurfaceId, Is.EqualTo("Lobby"));
        }

        [Test]
        public void SynergyFixtures_ExposeCanonicalEightInFourColumnRowsAndExactCompositions()
        {
            string[] expectedIds =
            {
                "synergy_guard_shockwave", "synergy_archer_rain", "synergy_magic_chain", "synergy_explosion_chain",
                "synergy_beast_hunt", "synergy_undead_summon", "synergy_healing_bond", "synergy_mixed_command"
            };
            string[][] expectedMembers =
            {
                new[] { "shield_guard", "sword_soldier", "cleric" },
                new[] { "field_herbalist", "falcon_archer", "bombardier" },
                new[] { "fire_mage", "lightning_mage", "necromancer" },
                new[] { "bombardier", "fire_mage", "skeleton_bomber" },
                new[] { "falcon_archer", "wolf_tamer" },
                new[] { "wraith_knight", "necromancer", "skeleton_bomber" },
                new[] { "cleric", "field_herbalist", "wraith_knight" },
                new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage" },
            };

            Assert.AreEqual(8, FtueHomeTestActions.SynergyFixtureDefinitions.Count);
            Assert.AreEqual(2, FtueHomeTestActions.SynergyFixtureRowCount);
            Assert.AreEqual(4, FtueHomeTestActions.SynergyFixtureColumnCount);
            for (int i = 0; i < expectedIds.Length; i++)
            {
                Assert.AreEqual(expectedIds[i], FtueHomeTestActions.SynergyFixtureDefinitions[i].Id);
                CollectionAssert.AreEqual(expectedMembers[i], FtueHomeTestActions.SynergyFixtureDefinitions[i].RequiredBaseUnitIds);
            }
        }

        [Test]
        public void SynergyFixtureDisplayName_UsesProviderAndExplicitlyFallsBackToCanonicalId()
        {
            FakeDataProvider data = new FakeDataProvider()
                .SetSynergy(new SynergyData { Id = "synergy_magic_chain", DisplayName = "Localized Magic" });
            data.InitializeAsync().GetAwaiter().GetResult();

            Assert.AreEqual("Localized Magic", FtueHomeTestActions.ResolveSynergyFixtureDisplayName(data, "synergy_magic_chain"));
            LogAssert.Expect(LogType.Error, "FTUE synergy fixture presentation missing: synergy_missing");
            Assert.AreEqual("synergy_missing", FtueHomeTestActions.ResolveSynergyFixtureDisplayName(data, "synergy_missing"));
        }

        [Test]
        public void SynergyFixturePreflightRejectsInsufficientSlotsWithoutRosterMutation()
        {
            FakeSynergyFixtureRecruiter recruiter = new FakeSynergyFixtureRecruiter(1);
            Assert.IsFalse(FtueHomeTestActions.TryApplySynergyFixture("synergy_mixed_command", recruiter));
            Assert.AreEqual(0, recruiter.RecruitCalls);
            StringAssert.Contains("insufficient", FtueHomeTestActions.LastSynergyFixtureStatus.ToLowerInvariant());
        }

        [Test]
        public void SynergyFixtureApplySkipsOwnedMembersAndRecruitsOnlyMissingMembers()
        {
            FakeSynergyFixtureRecruiter recruiter = new FakeSynergyFixtureRecruiter(5, "cleric", "fire_mage");
            Assert.IsTrue(FtueHomeTestActions.TryApplySynergyFixture("synergy_mixed_command", recruiter));
            CollectionAssert.AreEqual(new[] { "shield_guard", "sword_soldier", "falcon_archer" }, recruiter.RecruitedBaseUnitIds);
            StringAssert.Contains("success", FtueHomeTestActions.LastSynergyFixtureStatus.ToLowerInvariant());
        }

        [Test]
        public void SynergyFixtures_ApplyAllEightOnFreshSuitableRecruiters()
        {
            for (int i = 0; i < FtueHomeTestActions.SynergyFixtureDefinitions.Count; i++)
            {
                FtueHomeTestActions.SynergyFixtureDefinition definition = FtueHomeTestActions.SynergyFixtureDefinitions[i];
                FakeSynergyFixtureRecruiter recruiter = new FakeSynergyFixtureRecruiter(5);
                Assert.IsTrue(FtueHomeTestActions.TryApplySynergyFixture(definition.Id, recruiter));
                CollectionAssert.AreEquivalent(definition.RequiredBaseUnitIds, recruiter.RecruitedBaseUnitIds);
            }
        }

        [Test]
        public void SynergyFixtures_ActivateMatchingCanonicalSynergyAgainstRealProjectState()
        {
            LocalDataProvider provider = CreateProjectProvider();
            for (int i = 0; i < FtueHomeTestActions.SynergyFixtureDefinitions.Count; i++)
            {
                FtueHomeTestActions.SynergyFixtureDefinition definition = FtueHomeTestActions.SynergyFixtureDefinitions[i];
                SynergyActivationState state = new SynergyActivationState(provider);
                state.Refresh(CreateSlots(definition.RequiredBaseUnitIds));

                Assert.IsTrue(state.TryGetSnapshot(definition.Id, out SynergyActivationSnapshot snapshot));
                Assert.AreEqual(definition.Id, snapshot.SynergyId);
                Assert.IsTrue(snapshot.IsActive, definition.Id);
                state.Dispose();
            }
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

        static IReadOnlyList<SquadSlotState> CreateSlots(IReadOnlyList<string> baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int i = 0; i < slots.Length; i++)
            {
                string baseUnitId = i < baseUnitIds.Count ? baseUnitIds[i] : string.Empty;
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

        sealed class FakeSynergyFixtureRecruiter : FtueHomeTestActions.ISynergyFixtureRecruiter
        {
            readonly HashSet<string> _owned = new HashSet<string>();
            readonly List<string> _recruited = new List<string>();

            public FakeSynergyFixtureRecruiter(int freeSlots, params string[] owned)
            {
                FreeCompanionSlots = freeSlots;
                if (owned != null)
                    foreach (string id in owned) _owned.Add(id);
            }

            public int FreeCompanionSlots { get; }
            public int RecruitCalls { get; private set; }
            public IReadOnlyList<string> RecruitedBaseUnitIds => _recruited;
            public bool IsBaseUnitOwned(string baseUnitId) => _owned.Contains(baseUnitId);
            public bool CanRecruitBaseUnit(string baseUnitId) => true;
            public bool TryRecruitBaseUnit(string baseUnitId)
            {
                RecruitCalls++;
                _owned.Add(baseUnitId);
                _recruited.Add(baseUnitId);
                return true;
            }
        }
    }
}
