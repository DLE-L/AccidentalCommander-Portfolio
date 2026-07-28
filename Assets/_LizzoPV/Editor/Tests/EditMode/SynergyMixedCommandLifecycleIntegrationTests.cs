using System;
using System.Collections.Generic;
using System.IO;
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
    public sealed class SynergyMixedCommandLifecycleIntegrationTests
    {
        [Test]
        public void RunServices_OwnsBoundMixedCommandThatResolvesLiveCompanionAndClearsOnDispose()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 40);
            fixture.ActivateMixedCommand();

            MixedCommandRunModule module = fixture.Services.MixedCommand;
            Assert.IsNotNull(module);
            Assert.IsTrue(module.TryResolvePending(0.0f));
            Assert.AreEqual(1.15f, PartyServiceAccess.ResolveCompanionAttackIntervalDivisor(fixture.Services.Party, companion));
            Assert.AreEqual(1.15f, PartyServiceAccess.ResolveCompanionMoveSpeedMultiplier(fixture.Services.Party, companion));
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("MixedCommand"));

            fixture.Services.Dispose();
            Assert.AreEqual(0, module.ActiveTargetCount);
            Assert.AreEqual(1.0f, PartyServiceAccess.ResolveCompanionAttackIntervalDivisor(fixture.Services.Party, companion));
            Assert.AreEqual(1.0f, PartyServiceAccess.ResolveCompanionMoveSpeedMultiplier(fixture.Services.Party, companion));
        }

        [Test]
        public void RunOrchestration_OrdersAndClearsMixedCommandWithExistingSynergyLifecycle()
        {
            string bootstrap = File.ReadAllText("Assets/_LizzoPV/Scripts/Runtime/Core/Bootstrap/RunBootstrap.cs");
            string services = File.ReadAllText("Assets/_LizzoPV/Scripts/Runtime/Core/Bootstrap/RunServices.cs");

            Assert.GreaterOrEqual(services.IndexOf("public MixedCommandRunModule MixedCommand"), 0);
            Assert.Less(services.IndexOf("Party = new PartyService("), services.IndexOf("MixedCommand = new MixedCommandRunModule("));
            Assert.Less(services.IndexOf("SynergyTriggers = new SynergyTriggerState("), services.IndexOf("MixedCommand = new MixedCommandRunModule("));
            Assert.Less(services.IndexOf("MixedCommand = new MixedCommandRunModule("), services.IndexOf("Party.BindMixedCommandRunModule(MixedCommand);"));
            Assert.Less(bootstrap.IndexOf("Services.SynergyTriggers.Tick("), bootstrap.IndexOf("Services.MixedCommand.TryResolvePending("));
            Assert.Less(bootstrap.IndexOf("Services.MixedCommand.TryResolvePending("), bootstrap.IndexOf("Services.MixedCommand.Tick("));
            Assert.GreaterOrEqual(bootstrap.IndexOf("Services.MixedCommand.Reset();"), 0);
            Assert.GreaterOrEqual(bootstrap.IndexOf("Services.MixedCommand?.Reset();"), 0);
            Assert.Less(services.IndexOf("Party.UnbindMixedCommandRunModule(MixedCommand);"), services.IndexOf("MixedCommand.Dispose();"));
            Assert.Less(services.IndexOf("MixedCommand.Dispose();"), services.IndexOf("Party.Dispose();"));
        }

        sealed class Fixture : IDisposable
        {
            readonly ServiceTestFixture _services = new ServiceTestFixture();
            readonly List<GameObject> _objects = new List<GameObject>();
            readonly ClericHealTestVisualFactory _visualFactory = new ClericHealTestVisualFactory();

            public Fixture()
            {
                LogAssert.ignoreFailingMessages = true;
                ConfigureMixedCommandFamilies();
                AttackVisual.Configure(_visualFactory);
                FloatingDamageText.Configure(_visualFactory);
                RetroVfx.Configure(_services.App.Assets, _services.Run.Factory);
            }

            public RunServices Services => _services.Run;

            public CompanionRuntime AddCompanion(string unitId, int hp)
            {
                GameObject companionObject = Track(new GameObject(unitId));
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

            public void ActivateMixedCommand()
            {
                Services.Synergies.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage"));
                Assert.IsTrue(Services.Synergies.IsActive(SynergyActivationIds.MixedCommand), "Mixed Command activation");
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

            void ConfigureMixedCommandFamilies()
            {
                _services.Data.GetCompanionRoster("shield_guard").FamilyTags = "shield_family,defense_family";
                _services.Data.GetCompanionRoster("sword_soldier").FamilyTags = "sword_family,melee_family";
                _services.Data.GetCompanionRoster("cleric").FamilyTags = "cleric_family,healing_family";
                _services.Data.GetCompanionRoster("falcon_archer").FamilyTags = "ranged_family,beast_family";
                _services.Data.GetCompanionRoster("fire_mage").FamilyTags = "magic_family,explosive_family";
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

            public static float ResolveCompanionAttackIntervalDivisor(PartyService party, CompanionRuntime companion)
            {
                return (float)Invoke(party, "ResolveCompanionAttackIntervalDivisor", companion);
            }

            public static float ResolveCompanionMoveSpeedMultiplier(PartyService party, CompanionRuntime companion)
            {
                return (float)Invoke(party, "ResolveCompanionMoveSpeedMultiplier", companion);
            }

            static object Invoke(PartyService party, string methodName, params object[] arguments)
            {
                MethodInfo method = typeof(PartyService).GetMethod(methodName, InstanceInternal);
                Assert.IsNotNull(method, methodName);
                return method.Invoke(party, arguments);
            }
        }
    }
}
