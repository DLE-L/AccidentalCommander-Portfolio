using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyCatalogActivationTests
    {
        sealed class ActivationCase
        {
            public readonly string SynergyId;
            public readonly string[] BaseUnitIds;

            public ActivationCase(string synergyId, params string[] baseUnitIds)
            {
                SynergyId = synergyId;
                BaseUnitIds = baseUnitIds;
            }
        }

        static IEnumerable<ActivationCase> CanonicalActivationCases()
        {
            yield return new ActivationCase(SynergyActivationIds.GuardShockwave, "shield_guard", "sword_soldier", "cleric");
            yield return new ActivationCase(SynergyActivationIds.ArcherRain, "field_herbalist", "falcon_archer", "bombardier");
            yield return new ActivationCase(SynergyActivationIds.MixedCommand, "shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage");
            yield return new ActivationCase(SynergyActivationIds.MagicChain, "fire_mage", "lightning_mage", "necromancer");
            yield return new ActivationCase(SynergyActivationIds.ExplosionChain, "bombardier", "fire_mage", "skeleton_bomber");
            yield return new ActivationCase(SynergyActivationIds.BeastHunt, "falcon_archer", "wolf_tamer");
            yield return new ActivationCase(SynergyActivationIds.UndeadSummon, "wraith_knight", "necromancer", "skeleton_bomber");
            yield return new ActivationCase(SynergyActivationIds.HealingBond, "cleric", "field_herbalist", "wraith_knight");
        }

        [Test]
        public void CanonicalSynergies_ActivateFromCurrentRosterComposition()
        {
            foreach (ActivationCase activationCase in CanonicalActivationCases())
            {
                SynergyActivationState state = new SynergyActivationState(CreateProjectProvider());
                state.Refresh(CreateSlots(activationCase.BaseUnitIds));

                Assert.IsTrue(state.IsActive(activationCase.SynergyId), activationCase.SynergyId);
            }
        }

        [Test]
        public void ActivationLifecycle_ActivatesOnceAndReselectsOnlyAfterRepresentativeRemoval()
        {
            SynergyActivationState state = new SynergyActivationState(CreateProjectProvider());
            int activationCount = 0;
            state.Activated += _ => activationCount++;

            state.Refresh(CreateSlots("field_herbalist", "falcon_archer", "bombardier"));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.ArcherRain));
            Assert.AreEqual("squad_00", state.GetRepresentativeRosterSlotId(SynergyActivationIds.ArcherRain));
            state.Refresh(CreateSlots("field_herbalist", "falcon_archer", "bombardier"));
            state.Refresh(CreateSlots("field_herbalist", "falcon_archer", "bombardier"));
            Assert.AreEqual(1, activationCount);

            state.Refresh(CreateSlots("", "falcon_archer", "bombardier"));
            Assert.AreEqual("squad_01", state.GetRepresentativeRosterSlotId(SynergyActivationIds.ArcherRain));
            state.Refresh(CreateSlots("", "", ""));
            Assert.IsTrue(state.IsActive(SynergyActivationIds.ArcherRain));
            Assert.IsNull(state.GetRepresentativeRosterSlotId(SynergyActivationIds.ArcherRain));

            state.Reset();
            Assert.IsFalse(state.IsActive(SynergyActivationIds.GuardShockwave));
            Assert.AreEqual(0, state.ActiveCount);
        }

        [Test]
        public void ActivationSnapshot_IsStableAndContainsAllCanonicalSynergies()
        {
            SynergyActivationState state = new SynergyActivationState(CreateProjectProvider());
            IReadOnlyList<SynergyActivationSnapshot> snapshot = state.Snapshot;
            state.Refresh(CreateSlots("fire_mage", "lightning_mage", "necromancer"));

            Assert.AreSame(snapshot, state.Snapshot);
            Assert.AreEqual(8, snapshot.Count);
            Assert.AreEqual(1, state.ActiveCount);
            Assert.IsTrue(state.IsActive(SynergyActivationIds.MagicChain));
        }

        [Test]
        public void TriggerState_ImmediateAndTimedTriggersFreezeWhilePaused()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric"));
                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out SynergyTriggerPayload immediate));
                Assert.AreEqual(SynergyTriggerKind.Immediate, immediate.Kind);
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.GuardShockwave));

                triggers.Tick(11.9f, runReady: true, paused: false, frameId: 1);
                triggers.Tick(20.0f, runReady: true, paused: true, frameId: 2);
                triggers.Tick(0.1f, runReady: true, paused: false, frameId: 3);
                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out SynergyTriggerPayload timed));
                Assert.AreEqual(SynergyTriggerKind.Timed, timed.Kind);
            }
        }

        [Test]
        public void TriggerState_MagicAndHealingEventsRequireEligibilityAndDedupeIds()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("fire_mage", "lightning_mage", "necromancer", "cleric", "field_herbalist", "wraith_knight"));
                ConsumeImmediate(triggers, SynergyActivationIds.MagicChain);
                ConsumeImmediate(triggers, SynergyActivationIds.HealingBond);

                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_0", true, false, true, false)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_1", false, true, true, false)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_2", true, true, false, true)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_3", true, true, true, false)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_3", true, true, true, false)));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_4", true, true, true, false)));
                Assert.IsTrue(triggers.ReportMagicCast(new SynergyMagicCastEvent("magic_5", true, true, true, false)));
                Assert.AreEqual(0, triggers.GetCounter(SynergyActivationIds.MagicChain));
                Assert.IsTrue(triggers.HasPending(SynergyActivationIds.MagicChain));

                Assert.IsFalse(triggers.ReportHealing(new SynergyHealingEvent(false, 4, false, false, false)));
                Assert.IsFalse(triggers.ReportHealing(new SynergyHealingEvent(true, 0, false, false, false)));
                Assert.IsFalse(triggers.ReportHealing(new SynergyHealingEvent(true, 4, true, false, false)));
                Assert.IsTrue(triggers.ReportHealing(new SynergyHealingEvent(true, 1, false, false, false)));
                Assert.IsTrue(triggers.HasPending(SynergyActivationIds.HealingBond));
            }
        }

        [Test]
        public void TriggerState_ExplosionAndUndeadCountersRespectOverflowScopeAndCap()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("bombardier", "fire_mage", "skeleton_bomber", "wraith_knight", "necromancer"));
                ConsumeImmediate(triggers, SynergyActivationIds.ExplosionChain);
                ConsumeImmediate(triggers, SynergyActivationIds.UndeadSummon);

                for (int i = 0;
                i < 8;
                i++)
                    triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent($"explosion_{i}", SynergyDeathSourceCategory.Companion, false, false, false, 0L, 10));

                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.ExplosionChain, out SynergyTriggerPayload explosion));
                Assert.AreEqual(SynergyTriggerKind.ExplosionKills, explosion.Kind);
                Assert.IsFalse(triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent("same_scope", SynergyDeathSourceCategory.Synergy, false, false, true,
                    explosion.ResolutionScopeId, 10)));
                Assert.AreEqual(0, triggers.GetCounter(SynergyActivationIds.ExplosionChain));

                triggers.SetUndeadAliveCapFull(true);
                for (int i = 0;
                i < 20;
                i++)
                    triggers.ReportEnemyDeath(new SynergyEnemyDeathEvent($"undead_{i}", SynergyDeathSourceCategory.Commander, false, false, false, 0L, 11 + i));
                Assert.AreEqual(14, triggers.GetCounter(SynergyActivationIds.UndeadSummon));
                Assert.IsFalse(triggers.HasPending(SynergyActivationIds.UndeadSummon));
                triggers.SetUndeadAliveCapFull(false);
                Assert.IsTrue(
                    triggers.ReportEnemyDeath(
                        new SynergyEnemyDeathEvent(
                            "undead_release",
                            SynergyDeathSourceCategory.Commander,
                            false,
                            false,
                            false,
                            0L,
                            40)));
                Assert.IsTrue(triggers.HasPending(SynergyActivationIds.UndeadSummon));
            }
        }

        [Test]
        public void TriggerState_UndeadOverflowQueuesOnTheLaterFrameAndResetClearsRuntimeState()
        {
            SynergyActivationState activations = new SynergyActivationState(CreateProjectProvider());
            using (SynergyTriggerState triggers = new SynergyTriggerState(activations))
            {
                activations.Refresh(CreateSlots("necromancer", "wraith_knight", "skeleton_bomber"));
                ConsumeImmediate(triggers, SynergyActivationIds.UndeadSummon);
                for (int i = 0;
                i < 30;
                i++)
                    triggers.ReportEnemyDeath(
                        new SynergyEnemyDeathEvent(
                            $"undead_overflow_{i}",
                            SynergyDeathSourceCategory.Commander,
                            false,
                            false,
                            false,
                            0L,
                            i + 1));

                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.UndeadSummon, out SynergyTriggerPayload first));
                Assert.AreEqual(15, first.FrameId);
                Assert.AreEqual(15, triggers.GetCounter(SynergyActivationIds.UndeadSummon));
                triggers.Tick(0.1f, runReady: true, paused: false, frameId: 15);
                triggers.Tick(0.1f, runReady: true, paused: false, frameId: 16);
                Assert.IsTrue(triggers.TryConsumePending(SynergyActivationIds.UndeadSummon, out SynergyTriggerPayload overflow));
                Assert.AreEqual(16, overflow.FrameId);
                Assert.AreEqual(0, triggers.GetCounter(SynergyActivationIds.UndeadSummon));

                triggers.ReportMagicCast(new SynergyMagicCastEvent("persisted_id", true, true, true, false));
                triggers.Reset();
                Assert.AreEqual(0, triggers.PendingCount);
                Assert.AreEqual(0, triggers.GetCounter(SynergyActivationIds.UndeadSummon));
                Assert.IsFalse(triggers.ReportMagicCast(new SynergyMagicCastEvent("persisted_id", true, true, true, false)));
            }
        }

        [Test]
        public void Catalog_ExposesCanonicalCombatRecordsAndStableViews()
        {
            LocalDataProvider provider = CreateProjectProvider();
            IReadOnlyList<SynergyDamageData> damages = provider.SynergyDamages;
            IReadOnlyList<SynergyEffectData> effects = provider.SynergyEffects;
            IReadOnlyList<SynergySummonData> summons = provider.SynergySummons;
            Assert.AreSame(damages, provider.SynergyDamages);
            Assert.AreSame(effects, provider.SynergyEffects);
            Assert.AreSame(summons, provider.SynergySummons);
            Assert.AreEqual(6, damages.Count);
            Assert.AreEqual(3, effects.Count);
            Assert.AreEqual(1, summons.Count);

            SynergyDamageData guard = provider.GetSynergyDamage("DMG_SYNERGY_GUARD_01");
            Assert.IsNotNull(guard);
            Assert.AreEqual("synergy_guard_shockwave", guard.SynergyId);
            Assert.AreEqual(18.0f, guard.BaseValue);
            Assert.AreEqual(12.0f, guard.CadenceSeconds);
            Assert.AreEqual(3.5f, guard.Radius);
            Assert.AreEqual(120.0f, guard.Angle);
            Assert.AreEqual(6, guard.MaxTargets);
            Assert.AreEqual(0.01f, guard.BossMaxHpPercent);
            Assert.AreEqual("sector", guard.DeliveryRuleId);

            SynergyDamageData archer = provider.GetSynergyDamage("DMG_SYNERGY_ARCHER_01");
            Assert.AreEqual(14.0f, archer.BaseValue);
            Assert.AreEqual(8.0f, archer.CadenceSeconds);
            Assert.AreEqual(2.0f, archer.Radius);
            Assert.AreEqual(8, archer.MaxTargets);
            Assert.AreEqual(0.8f, archer.SlowMultiplier);
            Assert.AreEqual("strongest_only", archer.TargetRuleId);

            SynergyDamageData magic = provider.GetSynergyDamage("DMG_SYNERGY_MAGIC_01");
            Assert.AreEqual(10.0f, magic.BaseValue);
            Assert.AreEqual(3, magic.ActionCount);
            Assert.AreEqual(2.0f, magic.ProjectileLifetimeSeconds);
            Assert.AreEqual(5, magic.ProjectileCount);
            Assert.IsTrue(magic.SameTargetDuplicatesAllowed);
            Assert.IsTrue(magic.ExcludesSelfCounter);

            SynergyDamageData explosion = provider.GetSynergyDamage("DMG_SYNERGY_EXPLOSION_01");
            Assert.AreEqual(20.0f, explosion.BaseValue);
            Assert.AreEqual(8, explosion.TriggerThreshold);
            Assert.AreEqual(1.8f, explosion.Radius);
            Assert.IsTrue(explosion.SameScopeRecursionBlocked);
            Assert.IsTrue(explosion.OverflowCarries);

            SynergyDamageData beast = provider.GetSynergyDamage("DMG_SYNERGY_BEAST_01");
            Assert.AreEqual(8.0f, beast.BaseValue);
            Assert.AreEqual(10.0f, beast.CadenceSeconds);
            Assert.AreEqual(1, beast.MaxTargets);
            Assert.IsTrue(beast.EachAliveParticipantOneHit);
            Assert.IsTrue(beast.TargetDeathCancelsRemaining);
            Assert.IsTrue(beast.BossNoStagger);

            SynergyDamageData bleed = provider.GetSynergyDamage("DOT_SYNERGY_BEAST_01");
            Assert.AreEqual(3.0f, bleed.BaseValue);
            Assert.AreEqual(1.0f, bleed.TickIntervalSeconds);
            Assert.AreEqual(5.0f, bleed.DurationSeconds);
            Assert.AreEqual("max_one_refresh_duration", bleed.StackRuleId);
            Assert.IsTrue(bleed.BleedImmuneExcluded);

            SynergyEffectData guardDr = provider.GetSynergyEffect("EFFECT_SYNERGY_GUARD_DR");
            Assert.AreEqual("synergy_guard_shockwave", guardDr.SynergyId);
            Assert.AreEqual(0.75f, guardDr.DamageTakenMultiplier);
            Assert.AreEqual(0.25f, guardDr.DamageReduction);
            Assert.AreEqual(6.0f, guardDr.DurationSeconds);
            Assert.IsTrue(guardDr.AllAliveCompanions);
            Assert.IsTrue(guardDr.CommanderExcluded);
            Assert.IsFalse(guardDr.NumericStackingAllowed);

            SynergyEffectData healing = provider.GetSynergyEffect("EFFECT_HEALING_BOND_DR");
            Assert.AreEqual(0.8f, healing.DamageTakenMultiplier);
            Assert.AreEqual(0.2f, healing.DamageReduction);
            Assert.AreEqual(2.5f, healing.Radius);
            Assert.AreEqual(3.0f, healing.DurationSeconds);
            Assert.IsTrue(healing.KnockdownImmunity);
            Assert.IsTrue(healing.ZoneMembership);
            Assert.IsTrue(healing.LeaveRemoves);
            Assert.IsTrue(healing.NewReplacesOld);

            SynergyEffectData mixed = provider.GetSynergyEffect("EFFECT_MIXED_COMMAND");
            Assert.AreEqual(15.0f, mixed.CadenceSeconds);
            Assert.AreEqual(5.0f, mixed.DurationSeconds);
            Assert.AreEqual(1.15f, mixed.AttackIntervalDivisor);
            Assert.AreEqual(1.15f, mixed.MoveSpeedMultiplier);
            Assert.IsTrue(mixed.ExcludesCompanionTagFalseSummons);

            SynergySummonData skeleton = provider.GetSynergySummon("UNIT_SYNERGY_SKELETON_01");
            Assert.AreEqual("synergy_undead_summon", skeleton.SynergyId);
            Assert.AreEqual(22, skeleton.Hp);
            Assert.AreEqual(5, skeleton.Damage);
            Assert.AreEqual(1.2f, skeleton.AttackInterval);
            Assert.AreEqual(5, skeleton.ActiveCap);
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", skeleton.DistinctFromSummonId);
            Assert.AreEqual("UNIT_SYNERGY_SKELETON_01", provider.GetCompanionSummon("UNIT_PERSONAL_SKELETON_01").DistinctFromSummonId);
        }

        [Test]
        public void Catalog_FallbackMatchesXmlAndInvalidRequiredRowsFailClearly()
        {
            LocalDataProvider xmlProvider = CreateProjectProvider();
            LogAssert.Expect(LogType.Error, new Regex("\\[LocalDataProvider\\] Local data asset was not available."));
            LocalDataProvider fallbackProvider = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(fallbackProvider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.AreEqual(xmlProvider.SynergyDamages.Count, fallbackProvider.SynergyDamages.Count);
            Assert.AreEqual(xmlProvider.SynergyEffects.Count, fallbackProvider.SynergyEffects.Count);
            Assert.AreEqual(xmlProvider.SynergySummons.Count, fallbackProvider.SynergySummons.Count);
            Assert.IsNull(fallbackProvider.GetSynergyDamage("DMG_SYNERGY_UNKNOWN"));

            TextAsset projectData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.IsNotNull(projectData);
            TextAsset invalidData = new TextAsset(projectData.text.Replace("id=\"DMG_SYNERGY_GUARD_01\"", "id=\"DMG_SYNERGY_GUARD_01_BROKEN\""));
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", invalidData);
            LogAssert.Expect(LogType.Error, new Regex("\\[LocalDataProvider\\] Required data missing:"));
            DataLoadResult result = new LocalDataProvider(assets).InitializeAsync().GetAwaiter().GetResult();
            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.MissingRequiredIds, "synergy_damage:missing:DMG_SYNERGY_GUARD_01");
            Object.DestroyImmediate(invalidData);
        }

        static void ConsumeImmediate(SynergyTriggerState triggers, string synergyId)
        {
            Assert.IsTrue(triggers.TryConsumePending(synergyId, out SynergyTriggerPayload payload));
            Assert.AreEqual(SynergyTriggerKind.Immediate, payload.Kind);
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return provider;
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int i = 0;
            i < slots.Length;
            i++)
            {
                string baseUnitId = i < baseUnitIds.Length ? baseUnitIds[i] : string.Empty;
                bool active = string.IsNullOrEmpty(baseUnitId) == false;
                slots[i] = new SquadSlotState($"squad_{i:00}", baseUnitId, string.Empty, active ? 1 : 0, 3, false, baseUnitId);
            }

            return System.Array.AsReadOnly(slots);
        }
    }
}
