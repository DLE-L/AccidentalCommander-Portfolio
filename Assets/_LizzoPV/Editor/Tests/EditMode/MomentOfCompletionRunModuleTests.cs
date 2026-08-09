using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class MomentOfCompletionRunModuleTests
    {
        static readonly string[] SynergyIds =
        {
            SynergyActivationIds.GuardShockwave,
            SynergyActivationIds.ArcherRain,
            SynergyActivationIds.MagicChain,
            SynergyActivationIds.ExplosionChain,
            SynergyActivationIds.BeastHunt,
            SynergyActivationIds.UndeadSummon,
            SynergyActivationIds.HealingBond,
            SynergyActivationIds.MixedCommand,
        };

        [Test]
        public void Coordinator_GrantsEachExactSynergyOneIndependentFirstActivationBonus()
        {
            using RunTraitRunState traits = new RunTraitRunState();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);

            for (int i = 0; i < SynergyIds.Length; i++)
                Assert.That(GetFirstSynergyActivationExecutionCreditCount(coordinator, SynergyIds[i]), Is.EqualTo(1));

            coordinator.ResetRunState();
            Assert.That(traits.TrySelect(RunTraitIds.MomentOfCompletion), Is.True);
            for (int i = 0; i < SynergyIds.Length; i++)
            {
                Assert.That(GetFirstSynergyActivationExecutionCreditCount(coordinator, SynergyIds[i]), Is.EqualTo(2), SynergyIds[i]);
                Assert.That(GetFirstSynergyActivationExecutionCreditCount(coordinator, SynergyIds[i]), Is.EqualTo(1), SynergyIds[i]);
            }

            Assert.That(GetFirstSynergyActivationExecutionCreditCount(coordinator, "synergy_not_canonical"), Is.EqualTo(1));
            coordinator.ResetRunState();
            for (int i = 0; i < SynergyIds.Length; i++)
                Assert.That(GetFirstSynergyActivationExecutionCreditCount(coordinator, SynergyIds[i]), Is.EqualTo(2), SynergyIds[i]);
        }

        [Test]
        public void TriggerState_FirstConsumptionKeepsPendingAndFinalConsumptionStartsTimedCooldown()
        {
            using RunTraitRunState traits = new RunTraitRunState();
            Assert.That(traits.TrySelect(RunTraitIds.MomentOfCompletion), Is.True);
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);
            using SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using SynergyTriggerState triggers = new SynergyTriggerState(activations);
            BindRunTraitEffects(triggers, coordinator, "BindRunTraitEffectCoordinator");
            int activationCount = 0;
            int triggerReadyCount = 0;
            activations.Activated += _ => activationCount++;
            triggers.TriggerReady += _ => triggerReadyCount++;

            activations.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric"));

            Assert.That(activationCount, Is.EqualTo(1));
            Assert.That(triggerReadyCount, Is.EqualTo(1));
            Assert.That(triggers.PendingCount, Is.EqualTo(1));
            Assert.That(triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out SynergyTriggerPayload first), Is.True);
            Assert.That(first.Kind, Is.EqualTo(SynergyTriggerKind.Immediate));
            Assert.That(triggers.HasPending(SynergyActivationIds.GuardShockwave), Is.True);
            Assert.That(triggers.PendingCount, Is.EqualTo(1));

            triggers.Tick(12.0f, true, false, 1);
            Assert.That(triggerReadyCount, Is.EqualTo(1));
            Assert.That(triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out SynergyTriggerPayload second), Is.True);
            Assert.That(second.Kind, Is.EqualTo(SynergyTriggerKind.Immediate));
            Assert.That(second.ResolutionScopeId, Is.EqualTo(first.ResolutionScopeId));
            Assert.That(triggers.HasPending(SynergyActivationIds.GuardShockwave), Is.False);
            Assert.That(triggers.PendingCount, Is.EqualTo(0));
            Assert.That(activationCount, Is.EqualTo(1));

            triggers.Tick(11.9f, true, false, 2);
            Assert.That(triggers.HasPending(SynergyActivationIds.GuardShockwave), Is.False);
            triggers.Tick(0.1f, true, false, 3);
            Assert.That(triggers.HasPending(SynergyActivationIds.GuardShockwave), Is.True);
            Assert.That(triggerReadyCount, Is.EqualTo(2));
            Assert.That(triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out SynergyTriggerPayload timed), Is.True);
            Assert.That(timed.Kind, Is.EqualTo(SynergyTriggerKind.Timed));
            Assert.That(triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out _), Is.False);
        }

        [Test]
        public void Reset_AllowsTheActivationBonusAgainWithoutEmittingRecursiveActivation()
        {
            using RunTraitRunState traits = new RunTraitRunState();
            Assert.That(traits.TrySelect(RunTraitIds.MomentOfCompletion), Is.True);
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);
            using SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using SynergyTriggerState triggers = new SynergyTriggerState(activations);
            BindRunTraitEffects(triggers, coordinator, "BindRunTraitEffectCoordinator");
            int activationCount = 0;
            activations.Activated += _ => activationCount++;

            IReadOnlyList<SquadSlotState> slots = CreateSlots("shield_guard", "sword_soldier", "cleric");
            activations.Refresh(slots);
            ConsumeTwice(triggers, SynergyActivationIds.GuardShockwave);
            Assert.That(activationCount, Is.EqualTo(1));

            coordinator.ResetRunState();
            triggers.Reset();
            activations.Reset();
            activations.Refresh(slots);
            ConsumeTwice(triggers, SynergyActivationIds.GuardShockwave);
            Assert.That(activationCount, Is.EqualTo(2));
        }

        static void ConsumeTwice(SynergyTriggerState triggers, string synergyId)
        {
            Assert.That(triggers.TryConsumePending(synergyId, out _), Is.True);
            Assert.That(triggers.HasPending(synergyId), Is.True);
            Assert.That(triggers.TryConsumePending(synergyId, out _), Is.True);
            Assert.That(triggers.HasPending(synergyId), Is.False);
        }

        static int GetFirstSynergyActivationExecutionCreditCount(
            RunTraitEffectCoordinator coordinator,
            string synergyId)
        {
            MethodInfo method = typeof(RunTraitEffectCoordinator).GetMethod(
                "GetFirstSynergyActivationExecutionCreditCount",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Missing approved coordinator execution-credit seam.");
            return (int)method.Invoke(coordinator, new object[] { synergyId });
        }

        static LocalDataProvider CreateProjectProvider()
        {
            var assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/RunData/Data/GameData.xml");
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
    }
}
