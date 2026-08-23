using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyGuardShockwaveTests
    {
        [Test]
        public void ImmediateAndTimedNoTargetRounds_AreConsumedAndStillUseTypedGuardData()
        {
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml"));
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            RecordingFactory factory = new RecordingFactory();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            using (PartyService party = new PartyService(data, registry, factory, new NoProjectile(), new NoImmediate(), new NoField()))
            using (SynergyActivationState activations = new SynergyActivationState(data))
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            using (GuardShockwaveSynergy guard = new GuardShockwaveSynergy(data, activations, triggers, party, registry, new NoImmediate()))
            {
                activations.Refresh(new[]
                {
                    new SquadSlotState("squad_00", "shield_guard", string.Empty, 1, 3, false, "shield_guard"),
                    new SquadSlotState("squad_01", "sword_soldier", string.Empty, 1, 3, false, "sword_soldier"),
                    new SquadSlotState("squad_02", "cleric", string.Empty, 1, 3, false, "cleric"),
                });

                Assert.IsTrue(guard.TryResolvePending(0.0f));
                Assert.AreEqual(0, guard.LastResolvedTargetCount);
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.GuardShockwave));
                triggers.Tick(12.0f, true, false, 10);
                Assert.IsTrue(guard.TryResolvePending(12.0f));
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.GuardShockwave));
            }
        }

        [Test]
        public void SectorCollection_LocksNearestDirectionThenOrdersByDistanceAndStableIdentity()
        {
            GuardShockwaveTargetCandidate[] candidates =
            {
                new GuardShockwaveTargetCandidate(new Vector3(1, 0), 9, true),
                new GuardShockwaveTargetCandidate(new Vector3(2, 0), 8, true),
                new GuardShockwaveTargetCandidate(new Vector3(2, 0), 3, true),
                new GuardShockwaveTargetCandidate(new Vector3(1, 1), 4, true),
                new GuardShockwaveTargetCandidate(new Vector3(-1.1f, 0), 1, true),
                new GuardShockwaveTargetCandidate(new Vector3(4, 0), 2, true),
                new GuardShockwaveTargetCandidate(new Vector3(1, -1), 5, true),
                new GuardShockwaveTargetCandidate(new Vector3(2, 1), 6, true),
                new GuardShockwaveTargetCandidate(new Vector3(2, -1), 7, true),
            };
            System.Collections.Generic.List<GuardShockwaveTargetCandidate> selected = new System.Collections.Generic.List<GuardShockwaveTargetCandidate>();

            GuardShockwaveResolutionRules.CollectSector(candidates, Vector3.zero, 3.5f, 120.0f, 6, selected);

            Assert.That(selected.Count, Is.EqualTo(6));
            Assert.That(selected[0].InstanceId, Is.EqualTo(9));
            Assert.That(selected[1].InstanceId, Is.EqualTo(4));
            Assert.That(selected[2].InstanceId, Is.EqualTo(5));
            Assert.That(selected[3].InstanceId, Is.EqualTo(3));
            Assert.That(selected[4].InstanceId, Is.EqualTo(8));
            Assert.That(selected.Exists(x => x.InstanceId == 1), Is.False);
            Assert.That(GuardShockwaveResolutionRules.ResolveRadius(3.5f, 1.04f), Is.EqualTo(3.64f).Within(0.0001f));
            Assert.That(GuardShockwaveResolutionRules.ResolveRadius(3.5f, 1.08f), Is.EqualTo(3.78f).Within(0.0001f));
            Assert.That(GuardShockwaveResolutionRules.ResolveRadius(3.5f, 1.12f), Is.EqualTo(3.92f).Within(0.0001f));
        }

        [Test]
        public void DamageAndProtectionRules_UseBossCapRefreshAndSixtyPercentReductionCap()
        {
            Assert.That(GuardShockwaveResolutionRules.ResolveDamage(18, true, 1000, 0.01f), Is.EqualTo(10));
            Assert.That(GuardShockwaveResolutionRules.ResolveDamage(18, false, 1000, 0.01f), Is.EqualTo(18));

            GuardShockwaveProtectionWindow protection = new GuardShockwaveProtectionWindow();
            protection.Refresh(10, 6.0f, 0.0f);
            protection.Refresh(10, 6.3f, 3.0f);
            Assert.That(protection.IsActive(10, 9.2f), Is.True);
            Assert.That(protection.IsActive(10, 9.31f), Is.False);
            Assert.That(GuardShockwaveProtectionWindow.ResolveCombinedMultiplier(0.5f, 0.9f, 0.75f), Is.EqualTo(0.4f).Within(0.0001f));
            protection.Remove(10);
            Assert.That(protection.IsActive(10, 3.0f), Is.False);
        }

        [Test]
        public void PartyProtection_TracksLivingCompanionAndClearsOnDownAndReset()
        {
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml"));
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            RecordingFactory factory = new RecordingFactory();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            GameObject companionObject = new GameObject("GuardShockwaveProtectionCompanion");
            try
            {
                using PartyService party = new PartyService(
                    data,
                    registry,
                    factory,
                    new NoProjectile(),
                    new NoImmediate(),
                    new NoField());
                CompanionRuntime companion = companionObject.AddComponent<CompanionRuntime>();
                FieldInfo companionsField = typeof(PartyService).GetField(
                    "Companions",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(companionsField);
                ((List<CompanionRuntime>)companionsField.GetValue(party)).Add(companion);

                MethodInfo apply = typeof(PartyService).GetMethod(
                    "ApplyGuardShockwaveProtection",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo has = typeof(PartyService).GetMethod(
                    "HasGuardShockwaveProtection",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo resolveDamage = typeof(PartyService).GetMethod(
                    "ResolveCompanionIncomingDamage",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(apply);
                Assert.IsNotNull(has);
                Assert.IsNotNull(resolveDamage);

                apply.Invoke(party, new object[] { 6.0f, 2.0f });
                Assert.IsTrue((bool)has.Invoke(party, new object[] { companion, 7.99f }));
                Assert.IsFalse((bool)has.Invoke(party, new object[] { companion, 8.0f }));
                object resolution = resolveDamage.Invoke(
                    party,
                    new object[] { companion, 10, 100, 3.0f });
                PropertyInfo appliedDamage = resolution.GetType().GetProperty(
                    "AppliedDamage",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                PropertyInfo guardPreventedDamage = resolution.GetType().GetProperty(
                    "GuardShockwavePreventedDamage",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                PropertyInfo healingPreventedDamage = resolution.GetType().GetProperty(
                    "HealingBondPreventedDamage",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(appliedDamage);
                Assert.IsNotNull(guardPreventedDamage);
                Assert.IsNotNull(healingPreventedDamage);
                Assert.AreEqual(8, appliedDamage.GetValue(resolution));
                Assert.AreEqual(2, guardPreventedDamage.GetValue(resolution));
                Assert.AreEqual(0, healingPreventedDamage.GetValue(resolution));

                apply.Invoke(party, new object[] { 6.0f, 3.0f });
                party.NotifyCompanionDown(companion);
                Assert.IsFalse((bool)has.Invoke(party, new object[] { companion, 3.1f }));

                apply.Invoke(party, new object[] { 6.0f, 4.0f });
                party.ResetRunState();
                Assert.IsFalse((bool)has.Invoke(party, new object[] { companion, 4.1f }));
            }
            finally
            {
                Object.DestroyImmediate(companionObject);
            }
        }

        [Test]
        public void ChargeCancellation_AlwaysCancelsButOnlyStunsNonImmuneTargets()
        {
            ChargeCancellationResult normal = ChargeCancellationRules.Resolve(true, false, 0.8f);
            ChargeCancellationResult immune = ChargeCancellationRules.Resolve(true, true, 0.8f);
            ChargeCancellationResult inactive = ChargeCancellationRules.Resolve(false, false, 0.8f);

            Assert.That(normal.ChargeCancelled && normal.StunApplied, Is.True);
            Assert.That(immune.ChargeCancelled && immune.StunApplied == false, Is.True);
            Assert.That(inactive.ChargeCancelled || inactive.StunApplied, Is.False);
        }

        sealed class RecordingFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
        sealed class NoProjectile : ICombatProjectileModule { public bool TrySpawn(in CombatProjectileRequest request) => false; }
        sealed class NoImmediate : ICombatImmediateHitModule { public bool TryApply(in CombatImmediateHitRequest request) => false; }
        sealed class NoField : ICombatPersistentFieldModule
        {
            public int ActiveFieldCount => 0;
            public bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime) => false;
            public void Tick(float currentTime) { }
            public void Reset() { }
            public void Dispose() { }
        }
    }
}
