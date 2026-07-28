using System;
using System.Collections.Generic;
using System.IO;
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
    public sealed class SynergyHealingBondLifecycleIntegrationTests
    {
        [Test]
        public void RunServices_OwnsBoundHealingBondThatReceivesClericHealAndClearsOnDispose()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 50, new Vector3(2.0f, 0.0f));
            fixture.CreatePlayer(100, Vector3.zero);
            fixture.ActivateHealingBond();

            HealingBondRunModule module = fixture.Services.HealingBond;
            Assert.IsNotNull(module);
            Assert.IsTrue(module.TryResolvePending(0.0f));
            Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Services.Party, 10));
            Assert.AreEqual(60, companion.Hp);

            Assert.IsTrue(module.TryResolvePending(1.0f));
            Assert.AreEqual(new Vector3(2.0f, 0.0f), module.ZoneCenter);
            Assert.AreEqual(0.80f, PartyServiceAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Services.Party, companion, 1.0f));
            Assert.IsTrue(PartyServiceAccess.HasHealingBondKnockdownImmunity(fixture.Services.Party, companion));

            fixture.Services.Dispose();
            Assert.IsFalse(module.HasActiveZone);
            Assert.AreEqual(1.0f, PartyServiceAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Services.Party, companion, 1.0f));
            Assert.IsFalse(PartyServiceAccess.HasHealingBondKnockdownImmunity(fixture.Services.Party, companion));
        }

        [Test]
        public void RunOrchestration_OrdersAndClearsHealingBondWithExistingSynergyLifecycle()
        {
            string bootstrap = File.ReadAllText("Assets/_LizzoPV/Scripts/Runtime/Core/Bootstrap/RunBootstrap.cs");
            string services = File.ReadAllText("Assets/_LizzoPV/Scripts/Runtime/Core/Bootstrap/RunServices.cs");

            Assert.GreaterOrEqual(services.IndexOf("public HealingBondRunModule HealingBond"), 0);
            Assert.Less(services.IndexOf("Party = new PartyService("), services.IndexOf("HealingBond = new HealingBondRunModule("));
            Assert.Less(services.IndexOf("SynergyTriggers = new SynergyTriggerState("), services.IndexOf("HealingBond = new HealingBondRunModule("));
            Assert.Less(services.IndexOf("HealingBond = new HealingBondRunModule("), services.IndexOf("Party.BindHealingBondRunModule(HealingBond);"));
            Assert.Less(bootstrap.IndexOf("Services.SynergyTriggers.Tick("), bootstrap.IndexOf("Services.HealingBond.TryResolvePending("));
            Assert.Less(bootstrap.IndexOf("Services.HealingBond.TryResolvePending("), bootstrap.IndexOf("Services.HealingBond.Tick("));
            Assert.GreaterOrEqual(bootstrap.IndexOf("Services.HealingBond.Reset();"), 0);
            Assert.GreaterOrEqual(bootstrap.IndexOf("Services.HealingBond?.Reset();"), 0);
            Assert.Less(services.IndexOf("Party.UnbindHealingBondRunModule(HealingBond);"), services.IndexOf("HealingBond.Dispose();"));
            Assert.Less(services.IndexOf("HealingBond.Dispose();"), services.IndexOf("Party.Dispose();"));
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
            }

            public RunServices Services => _services.Run;

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
                companion.Configure(Services.Party, _services.Data.GetUnit(unitId), unitId, false);
                SetProperty(companion, "Hp", hp);
                GetCompanions(Services.Party).Add(companion);
                return companion;
            }

            public PlayerController CreatePlayer(int hp, Vector3 position)
            {
                PlayerController player = Track(new GameObject("RegistryPlayer")).AddComponent<PlayerController>();
                player.transform.position = position;
                player.MaxHp = 100;
                player.Hp = hp;
                Services.Registry.RegisterPlayer(player);
                return player;
            }

            public void ActivateHealingBond()
            {
                Services.Synergies.Refresh(CreateSlots("cleric", "field_herbalist", "wraith_knight"));
                Assert.IsTrue(Services.Synergies.IsActive(SynergyActivationIds.HealingBond));
            }

            public void Dispose()
            {
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

            public static float ResolveCompanionIncomingDamageMultiplier(PartyService party, CompanionRuntime companion, float currentTime)
            {
                return (float)Invoke(party, "ResolveCompanionIncomingDamageMultiplier", companion, currentTime);
            }

            public static bool HasHealingBondKnockdownImmunity(PartyService party, CompanionRuntime companion)
            {
                return (bool)Invoke(party, "HasHealingBondKnockdownImmunity", companion);
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
