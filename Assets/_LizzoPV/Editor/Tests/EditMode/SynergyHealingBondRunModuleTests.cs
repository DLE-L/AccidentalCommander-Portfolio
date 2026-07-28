using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyHealingBondRunModuleTests
    {
        [Test]
        public void LiveRuntime_UsesCompanionStateAndRegistryPlayerSnapshots()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 40, new Vector3(1.0f, 0.0f));
            PlayerController player = fixture.CreatePlayer(new Vector3(10.0f, 0.0f));
            fixture.ActivateHealingBond();

            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f), "immediate live-companion resolve");
            Assert.AreEqual(companion.transform.position, fixture.Module.ZoneCenter);
            Assert.AreEqual(0.80f, fixture.Module.GetDamageTakenMultiplier(companion));
            Assert.IsTrue(fixture.Module.HasKnockdownImmunity(companion), "live companion immunity inside immediate zone");

            companion.transform.position = new Vector3(4.0f, 0.0f);
            Assert.AreEqual(1.0f, fixture.Module.GetDamageTakenMultiplier(companion));
            Assert.IsFalse(fixture.Module.HasKnockdownImmunity(companion));
            SetProperty(companion, "IsDown", true);
            Assert.AreEqual(1.0f, fixture.Module.GetDamageTakenMultiplier(companion));
            Assert.IsFalse(fixture.Module.HasKnockdownImmunity(companion));

            SetProperty(companion, "IsDown", false);
            player.transform.position = new Vector3(10.0f, 0.0f);
            SynergyHealingEvent eligible = new SynergyHealingEvent(true, 1, false, false, false);
            Assert.IsTrue(fixture.Module.ReportHealing(player, eligible), "registry player healing report");
            PlayerController unrelatedPlayer = fixture.CreateUnregisteredPlayer();
            Assert.IsFalse(fixture.Module.ReportHealing(unrelatedPlayer, eligible));

            player.transform.position = new Vector3(20.0f, 0.0f);
            companion.transform.position = new Vector3(30.0f, 0.0f);
            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f), "registry player replacement resolve");
            Assert.AreEqual(new Vector3(10.0f, 0.0f), fixture.Module.ZoneCenter);

            fixture.Module.Reset();
            Assert.IsFalse(fixture.Module.HasActiveZone);
            fixture.Module.Dispose();
            Assert.IsFalse(fixture.Module.HasActiveZone);
            Assert.AreEqual(1.0f, fixture.Module.GetDamageTakenMultiplier(companion));
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
            }

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
                companion.Configure(_services.Run.Party, _services.Data.GetUnit(unitId), unitId, false);
                SetProperty(companion, "Hp", hp);
                GetCompanions(_services.Run.Party).Add(companion);
                return companion;
            }

            public PlayerController CreatePlayer(Vector3 position)
            {
                PlayerController player = Track(new GameObject("RegistryPlayer")).AddComponent<PlayerController>();
                player.transform.position = position;
                player.MaxHp = 100;
                player.Hp = 100;
                _services.Run.Registry.RegisterPlayer(player);
                return player;
            }

            public PlayerController CreateUnregisteredPlayer()
            {
                PlayerController player = Track(new GameObject("UnregisteredPlayer")).AddComponent<PlayerController>();
                player.MaxHp = 100;
                player.Hp = 100;
                return player;
            }

            public void ActivateHealingBond()
            {
                _services.Run.Synergies.Refresh(CreateSlots("cleric", "field_herbalist", "wraith_knight"));
                Assert.IsTrue(_services.Run.Synergies.IsActive(SynergyActivationIds.HealingBond), "Healing Bond activation");
            }

            public void Dispose()
            {
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
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetSetMethod(true).Invoke(target, new[] { value });
        }
    }
}
