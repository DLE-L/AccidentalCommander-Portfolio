using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyHealingBondClericHealIntegrationTests
    {
        [Test]
        public void NormalCompanionHeal_ReplacesZoneAtEventTimeTargetPosition()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 50, new Vector3(2.0f, 0.0f));
            fixture.CreatePlayer(100, Vector3.zero);
            fixture.ActivateHealingBond();

            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f));
            companion.transform.position = new Vector3(4.0f, 0.0f);
            Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Party, 10));
            Assert.AreEqual(60, companion.Hp);

            companion.transform.position = new Vector3(9.0f, 0.0f);
            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f));
            Assert.AreEqual(new Vector3(4.0f, 0.0f), fixture.Module.ZoneCenter);
        }

        [Test]
        public void NormalCommanderHeal_ReplacesZoneAtRegistryPlayerEventTimePosition()
        {
            using Fixture fixture = new Fixture();
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
        public void PartialOverhealAndDownedRecovery_DoNotReplacePriorZone()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime baseline = fixture.AddCompanion("shield_guard", 10, new Vector3(1.0f, 0.0f));
            CompanionRuntime partial = fixture.AddCompanion("sword_soldier", 95, new Vector3(4.0f, 0.0f));
            fixture.CreatePlayer(100, Vector3.zero);
            fixture.ActivateHealingBond();

            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f));
            Assert.AreEqual(new Vector3(1.0f, 0.0f), fixture.Module.ZoneCenter);
            SetProperty(baseline, "Hp", baseline.MaxHp);
            SetProperty(partial, "Hp", partial.MaxHp - 5);
            Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Party, 10));
            Assert.AreEqual(partial.MaxHp, partial.Hp);
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

        [Test]
        public void PromotedNoReviveTwoTargetHeal_UsesLatestEligibleTargetEventPosition()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime first = fixture.AddCompanion("shield_guard", 20, new Vector3(1.0f, 0.0f));
            CompanionRuntime latest = fixture.AddCompanion("sword_soldier", 32, new Vector3(2.0f, 0.0f));
            fixture.CreatePlayer(100, Vector3.zero);
            fixture.ActivateHealingBond();

            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f));
            List<ClericHealAttack.SupportHealTarget> targets = new List<ClericHealAttack.SupportHealTarget>(2);
            Assert.IsTrue(ClericHealAttack.TryResolveNoRevive(fixture.Party, Vector3.zero, 14, 4.0f, 2, 0.70f, targets));
            Assert.AreEqual(34, first.Hp);
            Assert.AreEqual(42, latest.Hp);

            first.transform.position = new Vector3(8.0f, 0.0f);
            latest.transform.position = new Vector3(9.0f, 0.0f);
            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f));
            Assert.AreEqual(new Vector3(2.0f, 0.0f), fixture.Module.ZoneCenter);
        }

        sealed class Fixture : IDisposable
        {
            readonly ServiceTestFixture _services = new ServiceTestFixture();
            readonly List<GameObject> _objects = new List<GameObject>();
            readonly ClericHealTestVisualFactory _visualFactory = new ClericHealTestVisualFactory();

            public Fixture()
            {
                LogAssert.ignoreFailingMessages = true;
                AttackVisual.Configure(_visualFactory);
                FloatingDamageText.Configure(_visualFactory);
                RetroVfx.Configure(_services.App.Assets, _services.Run.Factory);
                Module = new HealingBondRunModule(
                    _services.Data,
                    _services.Run.SynergyTriggers,
                    _services.Run.Party,
                    _services.Run.Registry);
                PartyServiceAccess.BindHealingBondRunModule(_services.Run.Party, Module);
            }

            public PartyService Party => _services.Run.Party;
            public HealingBondRunModule Module { get; }

            public CompanionRuntime AddCompanion(string unitId, int hp, Vector3 position)
            {
                GameObject companionObject = Track(new GameObject(unitId));
                companionObject.transform.position = position;
                companionObject.AddComponent<Rigidbody2D>();
                CircleCollider2D bodyCollider = companionObject.AddComponent<CircleCollider2D>();
                CircleCollider2D combatCollider = companionObject.AddComponent<CircleCollider2D>();
                combatCollider.isTrigger = true;
                companionObject.AddComponent<AllyCombat>();
                companionObject.AddComponent<HitFlash>();
                companionObject.AddComponent<CompanionHealthBar>();
                CreateCompanionUi(companionObject);

                CompanionRuntime companion = companionObject.AddComponent<CompanionRuntime>();
                SetPrivateField(companion, "_bodyCollider", bodyCollider);
                SetPrivateField(companion, "_combatCollider", combatCollider);
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

            public void Dispose()
            {
                PartyServiceAccess.UnbindHealingBondRunModule(Party, Module);
                Module.Dispose();
                for (int index = _objects.Count - 1; index >= 0; index--)
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
                _services.Dispose();
                AttackVisual.ClearServices();
                FloatingDamageText.ClearServices();
                RetroVfx.ClearServices();
                _visualFactory.Clear();
                LogAssert.ignoreFailingMessages = false;
            }

            GameObject Track(GameObject value)
            {
                _objects.Add(value);
                return value;
            }
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int index = 0; index < slots.Length; index++)
            {
                string baseUnitId = index < baseUnitIds.Length ? baseUnitIds[index] : string.Empty;
                bool active = string.IsNullOrEmpty(baseUnitId) == false;
                slots[index] = new SquadSlotState($"squad_{index:00}", baseUnitId, string.Empty, active ? 1 : 0, 3, false, baseUnitId);
            }

            return Array.AsReadOnly(slots);
        }

        static List<CompanionRuntime> GetCompanions(PartyService party)
        {
            return (List<CompanionRuntime>)typeof(PartyService)
                .GetField("Companions", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(party);
        }

        static void CreateCompanionUi(GameObject companionObject)
        {
            Transform ui = new GameObject("UI").transform;
            ui.SetParent(companionObject.transform, false);
            Transform hpBarAnchor = new GameObject("HpBarAnchor").transform;
            hpBarAnchor.SetParent(ui, false);
            GameObject downMarker = new GameObject("P0_DownMarker");
            downMarker.transform.SetParent(hpBarAnchor, false);
            downMarker.AddComponent<TextMeshPro>();
            GameObject healthBar = new GameObject("P0_CompanionHPBar");
            healthBar.transform.SetParent(hpBarAnchor, false);
            CreateBarSprite(healthBar.transform, "Back");
            CreateBarSprite(healthBar.transform, "Fill");
            GameObject hpText = new GameObject("Text");
            hpText.transform.SetParent(healthBar.transform, false);
            hpText.AddComponent<TextMeshPro>();
        }

        static void CreateBarSprite(Transform parent, string name)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f));
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(CompanionRuntime)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            property.GetSetMethod(true).Invoke(target, new[] { value });
        }

        static class PartyServiceAccess
        {
            const BindingFlags InstanceInternal = BindingFlags.Instance | BindingFlags.NonPublic;

            public static void BindHealingBondRunModule(PartyService party, HealingBondRunModule module)
            {
                Invoke(party, "BindHealingBondRunModule", module);
            }

            public static void UnbindHealingBondRunModule(PartyService party, HealingBondRunModule module)
            {
                Invoke(party, "UnbindHealingBondRunModule", module);
            }

            static object Invoke(PartyService party, string methodName, params object[] arguments)
            {
                MethodInfo method = typeof(PartyService).GetMethod(methodName, InstanceInternal);
                Assert.IsNotNull(method, $"PartyService must expose {methodName}.");
                return method.Invoke(party, arguments);
            }
        }
    }
}
