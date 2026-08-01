using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyHealingBondTests
    {
        [Test]
        public void Core_ImmediateRoundUsesLowestLivingRatioAndStableTieBreak()
        {
            using CoreFixture fixture = new CoreFixture();
            CoreCompanion first = fixture.Add("slot_b", 0.50f, new Vector3(3.0f, 0.0f));
            CoreCompanion expected = fixture.Add("slot_a", 0.50f, new Vector3(1.0f, 0.0f));
            fixture.Add("commander", 0.10f, Vector3.zero, true);

            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            Assert.AreSame(expected, fixture.Synergy.ZoneOrigin);
            Assert.AreNotSame(first, fixture.Synergy.ZoneOrigin);
            Assert.AreEqual(expected.Position, fixture.Synergy.ZoneCenter);
        }

        [Test]
        public void Core_EligibleHealingReplacesZoneWithLatestPendingOriginIncludingCommander()
        {
            using CoreFixture fixture = new CoreFixture();
            CoreCompanion companion = fixture.Add("companion", 0.50f, Vector3.zero);
            CoreCompanion commander = fixture.Add("commander", 0.25f, new Vector3(4.0f, 0.0f), true);
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
        public void Core_ZoneMembershipTracksBoundaryAndExpirationWithoutLos()
        {
            using CoreFixture fixture = new CoreFixture();
            CoreCompanion origin = fixture.Add("origin", 0.20f, Vector3.zero);
            CoreCompanion boundary = fixture.Add("boundary", 0.80f, new Vector3(2.5f, 0.0f));
            CoreCompanion outside = fixture.Add("outside", 0.80f, new Vector3(2.51f, 0.0f));
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
        public void Core_InvalidHealingDoesNotReplaceAndResetDisposeClearState()
        {
            using CoreFixture fixture = new CoreFixture();
            CoreCompanion origin = fixture.Add("origin", 0.20f, Vector3.zero);
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
        public void Core_EligibleHealingUsesEventTimeSnapshotAfterTargetMovesOrDies()
        {
            using CoreFixture fixture = new CoreFixture();
            CoreCompanion first = fixture.Add("first", 0.20f, Vector3.zero);
            CoreCompanion latest = fixture.Add("latest", 0.30f, new Vector3(2.0f, 0.0f));
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
        public void Core_ImmediateRoundWithoutLivingNonCommanderConsumesWithoutZone()
        {
            using CoreFixture fixture = new CoreFixture();
            fixture.Add("commander", 0.10f, Vector3.zero, true);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            Assert.AreEqual(0, fixture.Synergy.ActiveZoneCount);
            Assert.IsFalse(fixture.Synergy.HasActiveZone);
        }

        [Test]
        public void Runtime_RunServicesBindsHealingBondToPartyAndClearsOnDispose()
        {
            using RunFixture fixture = new RunFixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 50, new Vector3(2.0f, 0.0f));
            fixture.CreatePlayer(100, Vector3.zero);
            fixture.ActivateHealingBond();
            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f));
            Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Party, 10));
            Assert.AreEqual(60, companion.Hp);
            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f));
            Assert.AreEqual(new Vector3(2.0f, 0.0f), fixture.Module.ZoneCenter);
            Assert.AreEqual(0.80f, PartyAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Party, companion, 1.0f));
            Assert.IsTrue(PartyAccess.HasHealingBondKnockdownImmunity(fixture.Party, companion));
            fixture.DisposeModuleOnly();
            Assert.IsFalse(fixture.Module.HasActiveZone);
            Assert.AreEqual(1.0f, PartyAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Party, companion, 1.0f));
            Assert.IsFalse(PartyAccess.HasHealingBondKnockdownImmunity(fixture.Party, companion));
        }

        [Test]
        public void Runtime_CompanionHealReplacesZoneAtEventTimePosition()
        {
            using RunFixture fixture = new RunFixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 50, new Vector3(2.0f, 0.0f));
            fixture.CreatePlayer(100, Vector3.zero);
            fixture.ActivateHealingBond();
            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f));
            companion.transform.position = new Vector3(4.0f, 0.0f);
            Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Party, 10));
            companion.transform.position = new Vector3(9.0f, 0.0f);
            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f));
            Assert.AreEqual(new Vector3(4.0f, 0.0f), fixture.Module.ZoneCenter);
        }

        [Test]
        public void Runtime_CommanderHealUsesRegistryPlayerEventTimePosition()
        {
            using RunFixture fixture = new RunFixture();
            fixture.AddCompanion("shield_guard", 100, new Vector3(1.0f, 0.0f));
            PlayerController player = fixture.CreatePlayer(50, new Vector3(3.0f, 0.0f));
            fixture.ActivateHealingBond();
            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f));
            Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Party, 10));
            Assert.AreEqual(60, player.Hp);
            player.transform.position = new Vector3(9.0f, 0.0f);
            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f));
            Assert.AreEqual(new Vector3(3.0f, 0.0f), fixture.Module.ZoneCenter);
        }

        [Test]
        public void Runtime_PartialOverhealAndDownedRecoveryKeepPriorZone()
        {
            using RunFixture fixture = new RunFixture();
            CompanionRuntime baseline = fixture.AddCompanion("shield_guard", 10, new Vector3(1.0f, 0.0f));
            CompanionRuntime partial = fixture.AddCompanion("sword_soldier", 95, new Vector3(4.0f, 0.0f));
            fixture.CreatePlayer(100, Vector3.zero);
            fixture.ActivateHealingBond();
            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f));
            Assert.AreEqual(new Vector3(1.0f, 0.0f), fixture.Module.ZoneCenter);
            SetProperty(baseline, "Hp", baseline.MaxHp);
            SetProperty(partial, "Hp", partial.MaxHp - 5);
            Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Party, 10));
            partial.transform.position = new Vector3(9.0f, 0.0f);
            Assert.IsFalse(fixture.Module.TryResolvePending(1.0f));
            Assert.AreEqual(new Vector3(1.0f, 0.0f), fixture.Module.ZoneCenter);
            SetProperty(baseline, "Hp", 0);
            SetProperty(baseline, "IsDown", true);
            Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Party, 10));
            Assert.IsFalse(baseline.IsDown);
            Assert.IsFalse(fixture.Module.TryResolvePending(2.0f));
            Assert.AreEqual(new Vector3(1.0f, 0.0f), fixture.Module.ZoneCenter);
        }

        sealed class CoreFixture : IDisposable
        {
            readonly SynergyActivationState _activations;
            readonly SynergyTriggerState _triggers;
            public readonly CoreWorld World;
            public readonly HealingBondSynergy Synergy;

            public CoreFixture()
            {
                TestAssetService assets = new TestAssetService();
                TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
                Assert.IsNotNull(gameData);
                assets.Register("PlayerData.xml", gameData);
                LocalDataProvider data = new LocalDataProvider(assets);
                Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
                _activations = new SynergyActivationState(data);
                _triggers = new SynergyTriggerState(_activations);
                _activations.Refresh(CreateSlots("cleric", "field_herbalist", "wraith_knight"));
                World = new CoreWorld();
                Synergy = new HealingBondSynergy(data, _triggers, World);
            }

            public CoreCompanion Add(string id, float ratio, Vector3 position, bool commander = false)
            {
                CoreCompanion companion = new CoreCompanion(id, ratio, position, commander);
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

        sealed class CoreWorld : HealingBondSynergy.IWorld
        {
            public readonly List<CoreCompanion> Companions = new List<CoreCompanion>();
            public void CollectCompanions(List<HealingBondSynergy.ICompanion> results)
            {
                results.Clear();
                for (int i = 0;
                i < Companions.Count;
                i++) results.Add(Companions[i]);
            }
        }

        sealed class CoreCompanion : HealingBondSynergy.ICompanion
        {
            public CoreCompanion(string id, float ratio, Vector3 position, bool commander)
            {
                StableIdentity = id;
                HealthRatio = ratio;
                Position = position;
                IsCommander = commander;
            }
            public string StableIdentity {
                get;
            }
            public float HealthRatio {
                get;
            }
            public Vector3 Position {
                get;
                set;
            }
            public bool IsCommander {
                get;
            }
            public bool IsLiving {
                get;
                set;
            }
            = true;
        }

        sealed class RunFixture : IDisposable
        {
            readonly ServiceTestFixture _services = new ServiceTestFixture();
            readonly List<GameObject> _objects = new List<GameObject>();
            readonly ClericHealTestVisualFactory _visualFactory = new ClericHealTestVisualFactory();
            bool _moduleDisposed;
            public RunFixture()
            {
                LogAssert.ignoreFailingMessages = true;
                AttackVisual.Configure(_visualFactory);
                FloatingDamageText.Configure(_visualFactory);
                RetroVfx.Configure(_services.App.Assets, _services.Run.Factory);
                Module = new HealingBondRunModule(_services.Data, _services.Run.SynergyTriggers, Party, _services.Run.Registry);
                PartyAccess.BindHealingBondRunModule(Party, Module);
            }
            public PartyService Party => _services.Run.Party;
            public HealingBondRunModule Module {
                get;
            }

            public CompanionRuntime AddCompanion(string unitId, int hp, Vector3 position)
            {
                GameObject instance = Track(new GameObject(unitId));
                instance.transform.position = position;
                instance.AddComponent<Rigidbody2D>();
                CircleCollider2D body = instance.AddComponent<CircleCollider2D>();
                CircleCollider2D combat = instance.AddComponent<CircleCollider2D>();
                combat.isTrigger = true;
                instance.AddComponent<AllyCombat>();
                instance.AddComponent<HitFlash>();
                instance.AddComponent<CompanionHealthBar>();
                CreateCompanionUi(instance);
                CompanionRuntime companion = instance.AddComponent<CompanionRuntime>();
                SetPrivateField(companion, "_bodyCollider", body);
                SetPrivateField(companion, "_combatCollider", combat);
                companion.Configure(Party, _services.Data.GetUnit(unitId), unitId, false);
                SetProperty(companion, "Hp", hp);
                GetCompanions(Party).Add(companion);
                return companion;
            }

            public PlayerController CreatePlayer(int hp, Vector3 position)
            {
                PlayerController player = Track(new GameObject("RegistryPlayer")).AddComponent<PlayerController>();
                player.transform.position = position;
                player.MaxHp = 100;
                player.Hp = hp;
                _services.Run.Registry.RegisterPlayer(player);
                return player;
            }

            public void ActivateHealingBond()
            {
                _services.Run.Synergies.Refresh(CreateSlots("cleric", "field_herbalist", "wraith_knight"));
                Assert.IsTrue(_services.Run.Synergies.IsActive(SynergyActivationIds.HealingBond));
            }

            public void DisposeModuleOnly()
            {
                if (_moduleDisposed) return;
                PartyAccess.UnbindHealingBondRunModule(Party, Module);
                Module.Dispose();
                _moduleDisposed = true;
            }

            public void Dispose()
            {
                DisposeModuleOnly();
                for (int i = _objects.Count - 1;
                i >= 0;
                i--) UnityEngine.Object.DestroyImmediate(_objects[i]);
                _services.Dispose();
                AttackVisual.ClearServices();
                FloatingDamageText.ClearServices();
                RetroVfx.ClearServices();
                _visualFactory.Clear();
                LogAssert.ignoreFailingMessages = false;
            }

            GameObject Track(GameObject value) {
                _objects.Add(value);
                return value;
            }
        }

        static class PartyAccess
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            public static void BindHealingBondRunModule(PartyService party, HealingBondRunModule module) {
                Invoke(party, "BindHealingBondRunModule", module);
            }
            public static void UnbindHealingBondRunModule(PartyService party, HealingBondRunModule module) {
                Invoke(party, "UnbindHealingBondRunModule", module);
            }
            public static float ResolveCompanionIncomingDamageMultiplier(PartyService party, CompanionRuntime companion, float time) {
                return (float)Invoke(party, "ResolveCompanionIncomingDamageMultiplier", companion, time);
            }
            public static bool HasHealingBondKnockdownImmunity(PartyService party, CompanionRuntime companion) {
                return (bool)Invoke(party, "HasHealingBondKnockdownImmunity", companion);
            }
            static object Invoke(PartyService party, string method, params object[] args)
            {
                MethodInfo info = typeof(PartyService).GetMethod(method, Flags);
                Assert.IsNotNull(info, $"PartyService must expose {method}.");
                return info.Invoke(party, args);
            }
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] ids)
        {
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int i = 0;
            i < slots.Length;
            i++)
            {
                string id = i < ids.Length ? ids[i] : string.Empty;
                bool active = string.IsNullOrEmpty(id) == false;
                slots[i] = new SquadSlotState($"squad_{i:00}", id, string.Empty, active ? 1 : 0, 3, false, id);
            }
            return Array.AsReadOnly(slots);
        }

        static List<CompanionRuntime> GetCompanions(PartyService party)
        {
            return (List<CompanionRuntime>)typeof(PartyService).GetField("Companions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(party);
        }

        static void CreateCompanionUi(GameObject root)
        {
            Transform ui = new GameObject("UI").transform;
            ui.SetParent(root.transform, false);
            Transform anchor = new GameObject("HpBarAnchor").transform;
            anchor.SetParent(ui, false);
            GameObject marker = new GameObject("P0_DownMarker");
            marker.transform.SetParent(anchor, false);
            marker.AddComponent<TextMeshPro>();
            GameObject bar = new GameObject("P0_CompanionHPBar");
            bar.transform.SetParent(anchor, false);
            CreateBarSprite(bar.transform, "Back");
            CreateBarSprite(bar.transform, "Fill");
            GameObject text = new GameObject("Text");
            text.transform.SetParent(bar.transform, false);
            text.AddComponent<TextMeshPro>();
        }

        static void CreateBarSprite(Transform parent, string name)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f));
        }

        static void SetPrivateField(object target, string name, object value)
        {
            typeof(CompanionRuntime).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void SetProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            property.GetSetMethod(true).Invoke(target, new[] {
                value }
            );
        }
    }
}
