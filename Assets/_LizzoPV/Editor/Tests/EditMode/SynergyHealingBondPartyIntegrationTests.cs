using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyHealingBondPartyIntegrationTests
    {
        [Test]
        public void BoundHealingBond_UsesExistingDamageChainAndOnlyCurrentBindingDelegatesImmunity()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 40, new Vector3(1.0f, 0.0f));
            fixture.CreatePlayer(new Vector3(10.0f, 0.0f));
            HealingBondRunModule olderModule = fixture.CreateModule();
            HealingBondRunModule currentModule = fixture.CreateModule();
            fixture.ActivateHealingBond();

            Assert.IsTrue(currentModule.TryResolvePending(0.0f));
            PartyServiceAccess.BindHealingBondRunModule(fixture.Party, olderModule);
            PartyServiceAccess.BindHealingBondRunModule(fixture.Party, currentModule);
            PartyServiceAccess.UnbindHealingBondRunModule(fixture.Party, olderModule);

            Assert.IsTrue(PartyServiceAccess.TryActivateShieldCaptainPromotionProtection(fixture.Party, PartyRosterChangeResult.Promote, "shield_guard", 0.0f));
            PartyServiceAccess.ApplyGuardShockwaveProtection(fixture.Party, 10.0f, 0.0f);
            Assert.AreEqual(0.54f, PartyServiceAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Party, companion, 0.0f), 0.0001f);
            Assert.IsTrue(PartyServiceAccess.HasHealingBondKnockdownImmunity(fixture.Party, companion));

            companion.transform.position = new Vector3(4.0f, 0.0f);
            Assert.AreEqual(0.675f, PartyServiceAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Party, companion, 0.0f), 0.0001f);
            Assert.IsFalse(PartyServiceAccess.HasHealingBondKnockdownImmunity(fixture.Party, companion));

            companion.transform.position = new Vector3(1.0f, 0.0f);
            SetProperty(companion, "IsDown", true);
            Assert.AreEqual(0.675f, PartyServiceAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Party, companion, 0.0f), 0.0001f);
            Assert.IsFalse(PartyServiceAccess.HasHealingBondKnockdownImmunity(fixture.Party, companion));

            SetProperty(companion, "IsDown", false);
            SetProperty(companion, "IncomingDamageMultiplier", 0.50f);
            Assert.AreEqual(0.40f, PartyServiceAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Party, companion, 0.0f), 0.0001f);

            PartyServiceAccess.UnbindHealingBondRunModule(fixture.Party, currentModule);
            SetProperty(companion, "IncomingDamageMultiplier", 1.0f);
            Assert.AreEqual(0.675f, PartyServiceAccess.ResolveCompanionIncomingDamageMultiplier(fixture.Party, companion, 0.0f), 0.0001f);
            Assert.IsFalse(PartyServiceAccess.HasHealingBondKnockdownImmunity(fixture.Party, companion));
        }

        [Test]
        public void ActualCompanionDamageRecordsCappedShapleyPreventionOnce()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 100, new Vector3(1.0f, 0.0f));
            fixture.ActivateHealingBond();
            Assert.IsTrue(fixture.Run.HealingBond.TryResolvePending(0.0f), $"Healing pending={fixture.Run.SynergyTriggers.HasPending(SynergyActivationIds.HealingBond)}");
            PartyServiceAccess.ApplyGuardShockwaveProtection(fixture.Party, 10.0f, 0.0f);
            ClearSpawnProtection(companion);

            Assert.IsTrue(InvokeCompanionDamage(companion, 10, "enemy:test"));
            Assert.AreEqual(94, companion.Hp);
            Assert.AreEqual(2, Find(fixture.Run.DamageContributions.CaptureSnapshot().SynergyEntries, SynergyActivationIds.GuardShockwave).PreventedDamage);
            Assert.AreEqual(2, Find(fixture.Run.DamageContributions.CaptureSnapshot().SynergyEntries, SynergyActivationIds.HealingBond).PreventedDamage);
        }

        [Test]
        public void ActualEnemyDamageAttributesMixedBonusOnlyForAffectedCompanionSource()
        {
            using Fixture fixture = new Fixture();
            fixture.AddCompanion("shield_guard", 100, Vector3.zero);
            fixture.Run.Synergies.Refresh(CreateSlots("cleric", "field_herbalist", "fire_mage", "wraith_knight", "bombardier"));
            Assert.IsTrue(fixture.Run.Synergies.IsActive(SynergyActivationIds.MixedCommand), "Mixed activation");
            Assert.IsTrue(fixture.Run.MixedCommand.TryResolvePending(0.0f), $"Mixed pending={fixture.Run.SynergyTriggers.HasPending(SynergyActivationIds.MixedCommand)}");

            MonsterController target = fixture.CreateTarget(200);
            target.OnDamagedFromPosition(Vector3.zero, 115, "shield_guard");

            DamageContributionSnapshot snapshot = fixture.Run.DamageContributions.CaptureSnapshot();
            Assert.AreEqual(115, Find(snapshot.CompanionEntries, "shield_guard").DirectDamage);
            Assert.AreEqual(15, Find(snapshot.SynergyEntries, SynergyActivationIds.MixedCommand).AttributedBonusDamage);
        }

        [Test]
        public void PromotedCompanionBaseSourceReceivesMixedAttribution()
        {
            using Fixture fixture = new Fixture();
            CompanionRuntime promoted = fixture.AddCompanion("shield_guard", 100, Vector3.zero, true);
            Assert.AreEqual("shield_captain", promoted.UnitId);
            Assert.AreEqual("shield_guard", promoted.BaseUnitId);
            fixture.Run.Synergies.Refresh(CreateSlots("cleric", "field_herbalist", "fire_mage", "wraith_knight", "bombardier"));
            Assert.IsTrue(fixture.Run.Synergies.IsActive(SynergyActivationIds.MixedCommand), "Mixed activation");
            Assert.IsTrue(fixture.Run.MixedCommand.TryResolvePending(0.0f), "Mixed pending");

            MonsterController target = fixture.CreateTarget(200);
            target.OnDamagedFromPosition(Vector3.zero, 115, "shield_guard");

            DamageContributionSnapshot snapshot = fixture.Run.DamageContributions.CaptureSnapshot();
            Assert.AreEqual(115, Find(snapshot.CompanionEntries, "shield_guard").DirectDamage);
            Assert.AreEqual(15, Find(snapshot.SynergyEntries, SynergyActivationIds.MixedCommand).AttributedBonusDamage);
        }

        sealed class Fixture : IDisposable
        {
            readonly ServiceTestFixture _services = new ServiceTestFixture();
            readonly List<GameObject> _objects = new List<GameObject>();
            readonly List<HealingBondRunModule> _modules = new List<HealingBondRunModule>();
            readonly ClericHealTestVisualFactory _visualFactory = new ClericHealTestVisualFactory();

            public Fixture()
            {
                LogAssert.ignoreFailingMessages = true;
                AttackVisual.Configure(_visualFactory);
                FloatingDamageText.Configure(_visualFactory);
                RetroVfx.Configure(_services.App.Assets, _services.Run.Factory);
            }

            public PartyService Party => _services.Run.Party;
            public RunServices Run => _services.Run;

            public HealingBondRunModule CreateModule()
            {
                HealingBondRunModule module = new HealingBondRunModule(
                    _services.Data,
                    _services.Run.SynergyTriggers,
                    Party,
                    _services.Run.Registry);
                _modules.Add(module);
                return module;
            }

            public CompanionRuntime AddCompanion(string unitId, int hp, Vector3 position, bool promoted = false)
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
                if (promoted)
                {
                    CompanionRosterData roster = _services.Data.GetCompanionRoster(unitId);
                    CompanionPromotionData promotion = _services.Data.GetCompanionPromotion(roster.PromotionProfileId);
                    UnitData unit = _services.Data.GetUnit(unitId);
                    Assert.IsNotNull(roster, $"Missing roster for {unitId}");
                    Assert.IsNotNull(promotion, $"Missing promotion for {unitId}");
                    Assert.IsNotNull(unit, $"Missing unit for {unitId}");
                    CompanionRuntimeSpec spec = new CompanionRuntimeSpec(
                        unitId,
                        promotion.PromotedUnitId,
                        promotion.DisplayName,
                        roster.FamilyTags,
                        unit.Hp,
                        unit.MoveSpeed,
                        true);
                    companion.Configure(Party, spec, unitId, string.Empty);
                }
                else
                {
                    companion.Configure(Party, _services.Data.GetUnit(unitId), unitId, false);
                }
                SetProperty(companion, "Hp", hp);
                GetCompanions(Party).Add(companion);
                return companion;
            }

            public MonsterController CreateTarget(int hp)
            {
                GameObject instance = Track(new GameObject("DamageContributionTarget"));
                instance.SetActive(false);
                instance.AddComponent<Rigidbody2D>();
                CircleCollider2D body = instance.AddComponent<CircleCollider2D>();
                CircleCollider2D combat = instance.AddComponent<CircleCollider2D>();
                combat.isTrigger = true;
                UnitColliderRefs refs = instance.AddComponent<UnitColliderRefs>();
                typeof(UnitColliderRefs).GetField("_bodyCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(refs, body);
                typeof(UnitColliderRefs).GetField("_combatCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(refs, combat);
                EnemyHealthBar health = instance.AddComponent<EnemyHealthBar>();
                HitFlash flash = instance.AddComponent<HitFlash>();
                instance.AddComponent<UnitVisualDriver>();
                instance.AddComponent<PatternEnemyVisual>();
                MonsterController target = instance.AddComponent<MonsterController>();
                typeof(MonsterController).GetField("_healthBar", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, health);
                typeof(MonsterController).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, flash);
                instance.SetActive(true);
                target.Initialize(_services.Run);
                target.ResetForSpawn();
                target.MaxHp = hp;
                target.Hp = hp;
                _services.Run.Registry.RegisterEnemy(target);
                return target;
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

            public void ActivateHealingBond()
            {
                _services.Run.Synergies.Refresh(CreateSlots("cleric", "field_herbalist", "wraith_knight"));
                Assert.IsTrue(_services.Run.Synergies.IsActive(SynergyActivationIds.HealingBond), "Healing Bond activation");
            }

            public void Dispose()
            {
                for (int index = _modules.Count - 1; index >= 0; index--)
                {
                    PartyServiceAccess.UnbindHealingBondRunModule(Party, _modules[index]);
                    _modules[index].Dispose();
                }

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

        static bool InvokeCompanionDamage(CompanionRuntime companion, int damage, string source)
        {
            FieldInfo survivalField = typeof(CompanionRuntime).GetField("_survival", BindingFlags.Instance | BindingFlags.NonPublic);
            object survival = survivalField.GetValue(companion);
            MethodInfo takeDamage = survival.GetType().GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool)takeDamage.Invoke(survival, new object[] { damage, source });
        }

        static void ClearSpawnProtection(CompanionRuntime companion)
        {
            FieldInfo survivalField = typeof(CompanionRuntime).GetField("_survival", BindingFlags.Instance | BindingFlags.NonPublic);
            object survival = survivalField.GetValue(companion);
            FieldInfo protectedUntil = survival.GetType().GetField("_spawnProtectedUntil", BindingFlags.Instance | BindingFlags.NonPublic);
            protectedUntil.SetValue(survival, -1.0f);
        }

        static DamageContributionEntry Find(IReadOnlyList<DamageContributionEntry> entries, string id)
        {
            for (int index = 0; index < entries.Count; index++)
                if (entries[index].Id == id)
                    return entries[index];

            Assert.Fail($"Missing contribution bucket '{id}'.");
            return default;
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

            public static bool TryActivateShieldCaptainPromotionProtection(PartyService party, PartyRosterChangeResult rosterCommit, string baseUnitId, float currentTime)
            {
                return (bool)Invoke(party, "TryActivateShieldCaptainPromotionProtection", rosterCommit, baseUnitId, currentTime);
            }

            public static void ApplyGuardShockwaveProtection(PartyService party, float duration, float currentTime)
            {
                Invoke(party, "ApplyGuardShockwaveProtection", duration, currentTime);
            }

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
