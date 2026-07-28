using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyMixedCommandMovementIntegrationTests
    {
        [Test]
        public void MixedCommand_MultipliesFollowerMovement_WhileNeutralDownAndExternalStayUnchanged()
        {
            using Fixture fixture = new Fixture();
            FollowerSetup neutral = fixture.CreateFollower("shield_guard", 40, 0);
            FollowerSetup active = fixture.CreateFollower("sword_soldier", 40, 1);
            FollowerSetup down = fixture.CreateFollower("cleric", 40, 2);
            FollowerSetup external = fixture.CreateFollower("falcon_archer", 40, 3);

            AssertStep(0.20f, fixture.MeasureOneFixedStep(neutral), "neutral");

            MixedCommandRunModule module = fixture.CreateModule();
            PartyServiceAccess.BindMixedCommandRunModule(fixture.Party, module);
            fixture.ActivateMixedCommand();
            Assert.IsTrue(module.TryResolvePending(0.0f));

            AssertStep(0.23f, fixture.MeasureOneFixedStep(active), "active");

            SetProperty(down.Companion, "IsDown", true);
            module.Tick(0.1f);
            AssertStep(0.20f, fixture.MeasureOneFixedStep(down), "down");

            SetSynergyExternalMovement(external.Follower, true);
            AssertStep(0.00f, fixture.MeasureOneFixedStep(external), "external");
        }

        sealed class Fixture : IDisposable
        {
            readonly ServiceTestFixture _services = new ServiceTestFixture();
            readonly List<GameObject> _objects = new List<GameObject>();
            readonly List<MixedCommandRunModule> _modules = new List<MixedCommandRunModule>();
            readonly ClericHealTestVisualFactory _visualFactory = new ClericHealTestVisualFactory();
            readonly SimulationMode2D _previousSimulationMode;

            public Fixture()
            {
                LogAssert.ignoreFailingMessages = true;
                _previousSimulationMode = Physics2D.simulationMode;
                Physics2D.simulationMode = SimulationMode2D.Script;
                ConfigureMixedCommandFamilies();
                AttackVisual.Configure(_visualFactory);
                FloatingDamageText.Configure(_visualFactory);
                RetroVfx.Configure(_services.App.Assets, _services.Run.Factory);
            }

            public PartyService Party => _services.Run.Party;

            public MixedCommandRunModule CreateModule()
            {
                MixedCommandRunModule module = new MixedCommandRunModule(_services.Data, _services.Run.SynergyTriggers, Party);
                _modules.Add(module);
                return module;
            }

            public FollowerSetup CreateFollower(string unitId, int hp, int isolationIndex)
            {
                GameObject companionObject = Track(new GameObject(unitId));
                Rigidbody2D body = companionObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0.0f;
                Vector2 startPosition = new Vector2(20.0f, 10.0f + isolationIndex * 3.0f);
                body.position = startPosition;
                body.linearVelocity = Vector2.zero;
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

                AllyFollower follower = companionObject.AddComponent<AllyFollower>();
                follower.BindParty(Party);
                GameObject target = Track(new GameObject($"{unitId}_target"));
                target.transform.position = new Vector3(30.0f, startPosition.y, 0.0f);
                follower.SetTarget(target.transform, Vector3.zero, 1.0f);
                InvokeOnEnable(follower);
                return new FollowerSetup(companion, follower, body, startPosition.x);
            }

            public void ActivateMixedCommand()
            {
                _services.Run.Synergies.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage"));
                Assert.IsTrue(_services.Run.Synergies.IsActive(SynergyActivationIds.MixedCommand), "Mixed Command activation");
            }

            public float MeasureOneFixedStep(FollowerSetup setup)
            {
                Physics2D.SyncTransforms();
                InvokeFixedUpdate(setup.Follower);
                Physics2D.Simulate(Time.fixedDeltaTime);
                return setup.Body.position.x - setup.StartX;
            }

            public void Dispose()
            {
                for (int index = _modules.Count - 1; index >= 0; index--)
                {
                    PartyServiceAccess.UnbindMixedCommandRunModule(Party, _modules[index]);
                    _modules[index].Dispose();
                }

                for (int index = _objects.Count - 1; index >= 0; index--)
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
                _services.Dispose();
                Physics2D.simulationMode = _previousSimulationMode;
                AttackVisual.ClearServices();
                FloatingDamageText.ClearServices();
                RetroVfx.ClearServices();
                _visualFactory.Clear();
                LogAssert.ignoreFailingMessages = false;
            }

            void ConfigureMixedCommandFamilies()
            {
                _services.Data.GetCompanionRoster("shield_guard").FamilyTags = "shield_family,defense_family";
                _services.Data.GetCompanionRoster("sword_soldier").FamilyTags = "sword_family,melee_family";
                _services.Data.GetCompanionRoster("cleric").FamilyTags = "cleric_family,healing_family";
                _services.Data.GetCompanionRoster("falcon_archer").FamilyTags = "ranged_family,beast_family";
                _services.Data.GetCompanionRoster("fire_mage").FamilyTags = "magic_family,explosive_family";
            }

            GameObject Track(GameObject value)
            {
                _objects.Add(value);
                return value;
            }
        }

        readonly struct FollowerSetup
        {
            public FollowerSetup(CompanionRuntime companion, AllyFollower follower, Rigidbody2D body, float startX)
            {
                Companion = companion;
                Follower = follower;
                Body = body;
                StartX = startX;
            }

            public CompanionRuntime Companion { get; }
            public AllyFollower Follower { get; }
            public Rigidbody2D Body { get; }
            public float StartX { get; }
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

        static void AssertStep(float expected, float actual, string state)
        {
            Assert.AreEqual(expected, actual, 0.0001f, state);
        }

        static void SetSynergyExternalMovement(AllyFollower follower, bool active)
        {
            typeof(AllyFollower)
                .GetMethod("SetSynergyExternalMovement", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(follower, new object[] { active });
        }

        static void InvokeOnEnable(AllyFollower follower)
        {
            typeof(AllyFollower)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(follower, null);
        }

        static void InvokeFixedUpdate(AllyFollower follower)
        {
            typeof(AllyFollower)
                .GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(follower, null);
        }

        static class PartyServiceAccess
        {
            const BindingFlags InstanceInternal = BindingFlags.Instance | BindingFlags.NonPublic;

            public static void BindMixedCommandRunModule(PartyService party, MixedCommandRunModule module)
            {
                Invoke(party, "BindMixedCommandRunModule", module);
            }

            public static void UnbindMixedCommandRunModule(PartyService party, MixedCommandRunModule module)
            {
                Invoke(party, "UnbindMixedCommandRunModule", module);
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
