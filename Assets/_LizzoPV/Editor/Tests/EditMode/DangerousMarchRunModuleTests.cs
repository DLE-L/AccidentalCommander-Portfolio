using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class DangerousMarchRunModuleTests
    {
        [Test]
        public void Coordinator_ReturnsNeutralThenSelectedDangerousMarchModifiers()
        {
            using RunTraitRunState traits = new RunTraitRunState();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);

            Assert.That(coordinator.GetNormalSpawnDensityMultiplier(), Is.EqualTo(1.00f));
            Assert.That(coordinator.GetGameplayExperienceMultiplier(), Is.EqualTo(1.00f));
            Assert.That(coordinator.GetExplosionKillCounterIncrement(), Is.EqualTo(1));
            Assert.That(coordinator.GetUndeadKillCounterIncrement(), Is.EqualTo(1));

            Assert.That(traits.TrySelect(RunTraitIds.DangerousMarch), Is.True);
            Assert.That(coordinator.GetNormalSpawnDensityMultiplier(), Is.EqualTo(1.20f));
            Assert.That(coordinator.GetGameplayExperienceMultiplier(), Is.EqualTo(1.25f));
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 2 }, GetExplosionIncrements(coordinator));
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 2 }, GetUndeadIncrements(coordinator));

            coordinator.ResetRunState();
            Assert.That(coordinator.GetExplosionKillCounterIncrement(), Is.EqualTo(1));
            Assert.That(coordinator.GetUndeadKillCounterIncrement(), Is.EqualTo(1));
        }

        [Test]
        public void GameplayExperience_UsesTraitAtEachAwardAndMultipliesPassiveBonus()
        {
            using RunTraitRunState traits = new RunTraitRunState();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);
            using RunState traitOnlyState = CreateLoadedRunState();
            CommanderGemCollector traitOnly = new CommanderGemCollector(
                traitOnlyState,
                new RuntimeObjectRegistry(new TestPrefabFactory()),
                coordinator);

            Assert.That(traitOnly.AwardGameplayExperience(1), Is.EqualTo(1));
            Assert.That(traits.TrySelect(RunTraitIds.DangerousMarch), Is.True);
            Assert.That(traitOnlyState.Experience, Is.EqualTo(1));
            for (int i = 0; i < 4; i++)
                traitOnly.AwardGameplayExperience(1);
            Assert.That(traitOnlyState.Experience, Is.EqualTo(6));

            using RunState composedState = CreateLoadedRunState();
            CommanderGemCollector composed = new CommanderGemCollector(
                composedState,
                new RuntimeObjectRegistry(new TestPrefabFactory()),
                coordinator);
            composed.SetExperienceMultiplier(1.20f);
            for (int i = 0; i < 4; i++)
                composed.AwardGameplayExperience(1);
            Assert.That(composedState.Experience, Is.EqualTo(6));
        }

        [Test]
        public void SynergyTriggerState_AppliesIndependentDangerousMarchKillIncrements()
        {
            using RunTraitRunState traits = new RunTraitRunState();
            Assert.That(traits.TrySelect(RunTraitIds.DangerousMarch), Is.True);
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using SynergyTriggerState triggers = new SynergyTriggerState(activations);
            BindRunTraitEffects(triggers, coordinator, "BindRunTraitEffectCoordinator");
            activations.Refresh(CreateSlots("bombardier", "fire_mage", "skeleton_bomber", "wraith_knight", "necromancer"));
            ConsumeImmediate(triggers, SynergyActivationIds.ExplosionChain);
            ConsumeImmediate(triggers, SynergyActivationIds.UndeadSummon);

            var explosionTotals = new int[4];
            var undeadTotals = new int[4];
            for (int i = 0; i < 4; i++)
            {
                triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent(
                    $"dangerous_march_{i}",
                    SynergyDeathSourceCategory.Companion,
                    false,
                    false,
                    false,
                    0L,
                    i + 1));
                explosionTotals[i] = triggers.GetCounter(SynergyActivationIds.ExplosionChain);
                undeadTotals[i] = triggers.GetCounter(SynergyActivationIds.UndeadSummon);
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 5 }, explosionTotals);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 5 }, undeadTotals);
            Assert.That(triggers.GetCounter(SynergyActivationIds.MagicChain), Is.EqualTo(0));

            BindRunTraitEffects(triggers, coordinator, "UnbindRunTraitEffectCoordinator");
            triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent(
                "dangerous_march_unbound",
                SynergyDeathSourceCategory.Commander,
                false,
                false,
                false,
                0L,
                10));
            Assert.That(triggers.GetCounter(SynergyActivationIds.ExplosionChain), Is.EqualTo(6));
            Assert.That(triggers.GetCounter(SynergyActivationIds.UndeadSummon), Is.EqualTo(6));
        }

        static int[] GetExplosionIncrements(RunTraitEffectCoordinator coordinator)
        {
            return new[]
            {
                coordinator.GetExplosionKillCounterIncrement(),
                coordinator.GetExplosionKillCounterIncrement(),
                coordinator.GetExplosionKillCounterIncrement(),
                coordinator.GetExplosionKillCounterIncrement(),
            };
        }

        static int[] GetUndeadIncrements(RunTraitEffectCoordinator coordinator)
        {
            return new[]
            {
                coordinator.GetUndeadKillCounterIncrement(),
                coordinator.GetUndeadKillCounterIncrement(),
                coordinator.GetUndeadKillCounterIncrement(),
                coordinator.GetUndeadKillCounterIncrement(),
            };
        }

        static RunState CreateLoadedRunState()
        {
            var state = new RunState();
            state.Reset(999);
            state.MarkLoaded();
            return state;
        }

        static LocalDataProvider CreateProjectProvider()
        {
            var assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.That(gameData, Is.Not.Null);
            assets.Register("PlayerData.xml", gameData);
            var provider = new LocalDataProvider(assets);
            Assert.That(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
            return provider;
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] baseUnitIds)
        {
            var slots = new SquadSlotState[7];
            for (int i = 0; i < slots.Length; i++)
            {
                string baseUnitId = i < baseUnitIds.Length ? baseUnitIds[i] : string.Empty;
                bool active = string.IsNullOrEmpty(baseUnitId) == false;
                slots[i] = new SquadSlotState($"squad_{i:00}", baseUnitId, string.Empty, active ? 1 : 0, 3, false, baseUnitId);
            }

            return System.Array.AsReadOnly(slots);
        }

        static void ConsumeImmediate(SynergyTriggerState triggers, string synergyId)
        {
            Assert.That(triggers.TryConsumePending(synergyId, out SynergyTriggerPayload payload), Is.True);
            Assert.That(payload.Kind, Is.EqualTo(SynergyTriggerKind.Immediate));
        }

        static void BindRunTraitEffects(
            SynergyTriggerState triggers,
            RunTraitEffectCoordinator coordinator,
            string methodName)
        {
            MethodInfo method = typeof(SynergyTriggerState).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(triggers, new object[] { coordinator });
        }

        sealed class TestPrefabFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
    }
}
