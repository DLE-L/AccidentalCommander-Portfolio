using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRuntimeProductionTests
    {
        private static readonly string[] ExpectedLineages =
        {
            "shield_guard",
            "sword_soldier",
            "cleric",
            "falcon_archer",
            "field_herbalist",
            "bombardier",
            "fire_mage",
            "lightning_mage",
            "wraith_knight",
            "necromancer",
            "skeleton_scythe_thrower",
            "wolf_tamer",
        };

        [Test]
        public void CompanionPassiveCatalog_ExposesTwelveCommonAndFortyEightRoleChoices()
        {
            int commonCount = 0;
            int roleCount = 0;
            HashSet<CardKind> kinds = new HashSet<CardKind>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<CompanionPassiveCatalogEntry> entries = CompanionPassiveCatalog.All;

            for (int index = 0; index < entries.Count; index++)
            {
                CompanionPassiveCatalogEntry entry = entries[index];
                Assert.That(kinds.Add(entry.CardKind), Is.True, entry.CardKind.ToString());
                Assert.That(ids.Add(entry.Id), Is.True, entry.Id);
                Assert.That(CompanionPassiveCatalog.ResolveMaxLevel(entry.Id), Is.EqualTo(entry.IsCommon ? 3 : 1));
                if (entry.IsCommon) commonCount++;
                else roleCount++;
            }

            Assert.That(entries.Count, Is.EqualTo(60));
            Assert.That(commonCount, Is.EqualTo(12));
            Assert.That(roleCount, Is.EqualTo(48));
        }

        [Test]
        public void ProductionSynergyEntityId_IsAlwaysPositiveForUnityObjects()
        {
            GameObject target = new GameObject("ProductionSynergyEntityId_Target");
            try
            {
                Assert.That(CompanionSynergyProductionHost.StableEntityId(target), Is.GreaterThan(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void CompanionPassiveCatalog_EveryChoiceChangesItsProductionModifierSnapshot()
        {
            FakeDataProvider data = CreateInitializedData();
            IReadOnlyList<CompanionPassiveCatalogEntry> entries = CompanionPassiveCatalog.All;
            for (int index = 0; index < entries.Count; index++)
            {
                CompanionPassiveCatalogEntry entry = entries[index];
                PassiveRosterState roster = new PassiveRosterState();
                Assert.That(roster.TryApply(CompanionPassiveCatalog.CreateData(in entry), out _), Is.True, entry.Id);
                CompanionPassiveCombatResolver resolver = new CompanionPassiveCombatResolver(data, roster, () => 1);
                string lineage = entry.IsCommon ? "sword_soldier" : entry.RequiredLineageId;
                CompanionPassiveCombatModifiers companion = resolver.Resolve(lineage);
                CommanderPassiveModifiers commander = resolver.ResolveCommander();
                Assert.That(
                    HasDifferentPublicField(companion, CompanionPassiveCombatModifiers.Identity)
                    || HasDifferentPublicField(commander, CommanderPassiveModifiers.Identity),
                    Is.True,
                    entry.Id);
            }
        }

        static bool HasDifferentPublicField<T>(T left, T right) where T : struct
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (int index = 0; index < fields.Length; index++)
            {
                object leftValue = fields[index].GetValue(left);
                object rightValue = fields[index].GetValue(right);
                if (Equals(leftValue, rightValue) == false)
                    return true;
            }
            return false;
        }

        [Test]
        public void RuntimeDefinitionCatalog_CoversApprovedRecruitLineagesAndDataDrivenSwordDelivery()
        {
            FakeDataProvider data = CreateInitializedData();
            CompanionRuntimeDefinitionCatalog catalog = new CompanionRuntimeDefinitionCatalog(data);

            Assert.That(catalog.LineageIds, Is.EqualTo(ExpectedLineages));
            for (int index = 0; index < ExpectedLineages.Length; index += 1)
                Assert.That(catalog.TryGetDefinition(ExpectedLineages[index], out _), Is.True, ExpectedLineages[index]);
            Assert.That(catalog.TryGetDefinition("falcon_archer", out _), Is.True);

            Assert.That(catalog.TryGetDefinition("skeleton_scythe_thrower", out CompanionDefinition skeleton), Is.True);
            Assert.That(skeleton.BaseActionSet.Steps[0].Delivery, Is.EqualTo(AttackDelivery.ReturningProjectile));
            Assert.That(catalog.TryGetDefinition("wolf_tamer", out CompanionDefinition wolf), Is.True);
            Assert.That(wolf.BaseActionSet.Steps[0].Delivery, Is.EqualTo(AttackDelivery.OwnedProxy));
            Assert.That(catalog.TryGetDefinition("lightning_mage", out CompanionDefinition lightning), Is.True);
            Assert.That(lightning.BaseActionSet.Steps[0].Delivery, Is.EqualTo(AttackDelivery.Chain));

            Assert.That(catalog.TryGetDefinition("sword_soldier", out CompanionDefinition sword), Is.True);
            CombatEffectData effect = data.GetCombatEffect("dmg_sword_slash_v1");
            CompanionPromotionData promotion = data.GetCompanionPromotion("sword_captain");
            Assert.That(effect.BaseMotion, Is.EqualTo(CompanionSourceMotionKind.Excursion));
            Assert.That(effect.PromotedMotion, Is.EqualTo(CompanionSourceMotionKind.Stationary));
            Assert.That(effect.PromotedPresentationCueId, Is.EqualTo("traveling-forward"));
            Assert.That(effect.OmitPromotedSecondaryEffect, Is.True);
            Assert.That(sword.BaseActionSet.Steps.Count, Is.EqualTo(1));
            Assert.That(sword.BaseActionSet.Steps[0].Motion, Is.EqualTo(CombatMotion.Excursion));
            Assert.That(sword.BaseActionSet.Steps[0].EffectId, Is.EqualTo(effect.Id));
            Assert.That(sword.BaseActionSet.Steps[0].ActionDurationSeconds, Is.EqualTo(0.12f));
            Assert.That(sword.BaseActionSet.Steps[0].ExcursionStandOffDistance, Is.EqualTo(1.35f));
            Assert.That(sword.BaseActionSet.Steps[0].ExcursionLateralOffset, Is.EqualTo(0.30f));
            Assert.That(sword.BaseActionSet.Steps[0].PresentationCueId, Is.EqualTo(effect.Id));
            Assert.That(sword.PromotedActionSet.Steps.Count, Is.EqualTo(1));
            Assert.That(sword.PromotedActionSet.Steps[0].Motion, Is.EqualTo(CombatMotion.Stationary));
            Assert.That(sword.PromotedActionSet.Steps[0].ActionDurationSeconds, Is.EqualTo(0.12f));
            Assert.That(sword.PromotedActionSet.Steps[0].ExcursionSpeed, Is.EqualTo(7.5f));
            Assert.That(sword.PromotedActionSet.Steps[0].PresentationCueId, Is.EqualTo("traveling-forward"));
            Assert.That(sword.PromotedActionSet.Steps[0].Magnitude, Is.EqualTo(effect.BaseValue * promotion.EffectMultiplier));
            Assert.That(sword.PromotedActionSet.CooldownSeconds, Is.EqualTo(effect.CastInterval * promotion.IntervalMultiplier));

            Assert.That(catalog.TryGetDefinition("cleric", out CompanionDefinition cleric), Is.True);
            Assert.That(cleric.BaseActionSet.Steps.Count, Is.EqualTo(2));
            Assert.That(cleric.BaseActionSet.Steps[1].EffectId, Is.EqualTo("heal_cleric_v1"));
            Assert.That(cleric.PromotedActionSet.Steps.Count, Is.EqualTo(2));
        }

        [Test]
        public void PassiveModifierCache_RebuildsOnlyWhenPassiveRosterChanges()
        {
            FakeDataProvider data = CreateInitializedData();
            PassiveRosterState roster = new PassiveRosterState();
            CompanionPassiveCombatResolver resolver = new CompanionPassiveCombatResolver(data, roster);
            using CompanionRuntimeModifierCache cache = new CompanionRuntimeModifierCache(resolver, roster);

            Assert.That(cache.Revision, Is.EqualTo(1));
            Assert.That(cache.Resolve("sword_soldier").DamageMultiplier, Is.EqualTo(1.0f));
            Assert.That(cache.Resolve("sword_soldier").DamageMultiplier, Is.EqualTo(1.0f));
            Assert.That(cache.Revision, Is.EqualTo(1));

            Assert.That(CompanionPassiveCatalog.TryGet("passive_standard_bearer", out CompanionPassiveCatalogEntry entry), Is.True);
            Assert.That(roster.TryApply(CompanionPassiveCatalog.CreateData(in entry), out _), Is.True);
            Assert.That(cache.Revision, Is.EqualTo(2));
            Assert.That(cache.Resolve("sword_soldier").DamageMultiplier, Is.EqualTo(1.10f));
        }

        [Test]
        public void ExternalAdapter_ExposesCanonicalRosterProgressWithoutLegacyActors()
        {
            ActionSet set = new ActionSet(
                "shield-base",
                1.0f,
                new[] { new ActionStep(CombatMotion.Stationary, AttackDelivery.Direct, "hit", 1.0f, "hit") });
            using CompanionRunModule module = new CompanionRunModule(
                new RunCombatContext(14UL, new SingleDefinitionCatalog("shield_guard", set), new NoTargetWorld()));
            CompanionRunExternalAdapter adapter = new CompanionRunExternalAdapter(module);

            Assert.That(adapter.SubmitCard(1L, "shield_guard").Accepted, Is.True);
            Assert.That(adapter.SubmitCard(2L, "shield_guard").Accepted, Is.True);
            Assert.That(adapter.ActiveCompanionSlotCount, Is.EqualTo(1));
            Assert.That(adapter.ActiveCompanionSlotCap, Is.EqualTo(7));
            Assert.That(adapter.PreviewCanonicalRecruit("shield_guard"), Is.EqualTo(PartyRosterChangeResult.Promote));
            Assert.That(adapter.TryGetCanonicalCompanionProgress("shield_guard", out int owned, out int preview), Is.True);
            Assert.That(owned, Is.EqualTo(2));
            Assert.That(preview, Is.EqualTo(3));
            Assert.That(adapter.GetSquadSlotSnapshot().Count, Is.EqualTo(7));
            Assert.That(adapter.GetSquadSlotSnapshot()[0].BaseUnitId, Is.EqualTo("shield_guard"));
        }

        [Test]
        public void CardOfferRuntime_ActiveProfileRoutesCanonicalCardToProductionHost()
        {
            PresentationCatalog presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset");
            Assert.That(presentationCatalog, Is.Not.Null);

            GameObject presentationProviderRoot = new GameObject(
                "CompanionRuntimeProductionTests_PresentationCatalogProvider");
            PresentationCatalogProvider presentationProvider =
                presentationProviderRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serializedPresentationProvider = new SerializedObject(presentationProvider);
            serializedPresentationProvider.FindProperty("_catalog").objectReferenceValue = presentationCatalog;
            serializedPresentationProvider.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo presentationAwake = typeof(PresentationCatalogProvider).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(presentationAwake, Is.Not.Null);
            presentationAwake.Invoke(presentationProvider, null);
            try
            {
                GameObject fixtureRoot = new GameObject("CompanionRuntimeProductionTests_ServiceFixture");
                Transform poolRoot = new GameObject("PoolRoot").transform;
                poolRoot.SetParent(fixtureRoot.transform, false);
                TestAssetService assets = new TestAssetService();
                FakeDataProvider data = CreateInitializedData();
                AppServices app = new AppServices(assets, data);
                ObjectPoolService pool = new ObjectPoolService(poolRoot);
                ServiceTestFixture.RecordingPrefabFactory factory = new ServiceTestFixture.RecordingPrefabFactory();
                RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
                RunServices run = new RunServices(
                    app,
                    new Lizzo.PV.Flow.RunState(),
                    registry,
                    pool,
                    factory,
                    Lizzo.PV.Flow.RunContext.Normal);
                Assert.That(run.CompanionRuntimeHost, Is.Not.Null);
                Assert.That(run.ProductionSynergies, Is.Not.Null);
                Assert.That(run.ProductionSynergies.CatalogCount, Is.EqualTo(20));
                try
                {
                    CardData card = new CardData(
                        CardKind.AddShieldSoldier,
                        "shield",
                        "shield",
                        CardHighlight.None,
                        "shield_guard");

                    Assert.That(run.CardOffers.TryApplyCard(card), Is.True);
                    Assert.That(run.CompanionRuntimeHost.Adapter.TryGetCanonicalCompanionProgress(
                        "shield_guard",
                        out int owned,
                        out int preview), Is.True);
                    Assert.That(owned, Is.EqualTo(1));
                    Assert.That(preview, Is.EqualTo(2));
                    Assert.That(run.Party.ActiveCompanionSlotCount, Is.EqualTo(1));
                    Assert.That(run.Party.ActiveCompanionCount, Is.EqualTo(1));
                    IReadOnlyList<SquadSlotState> summarySnapshot = run.Party.GetSquadSlotSnapshot();
                    SquadSlotState activeSummarySlot = default;
                    int activeSummarySlotCount = 0;
                    for (int summaryIndex = 0; summaryIndex < summarySnapshot.Count; summaryIndex++)
                    {
                        if (summarySnapshot[summaryIndex].IsActive == false)
                            continue;

                        activeSummarySlot = summarySnapshot[summaryIndex];
                        activeSummarySlotCount += 1;
                    }
                    Assert.That(activeSummarySlotCount, Is.EqualTo(1));
                    Assert.That(
                        run.Party.BuildLegionSummary(),
                        Is.EqualTo($"군단 / {activeSummarySlot.DisplayName} x1"));
                    Assert.That(run.CardOffers.TryApplyCard(new CardData(
                        CardKind.RecruitSkeletonScytheThrower,
                        "skeleton",
                        "skeleton",
                        CardHighlight.None,
                        "skeleton_scythe_thrower")), Is.True);
                    Assert.That(run.CompanionRuntimeHost.Adapter.TryGetCanonicalCompanionProgress(
                        "skeleton_scythe_thrower",
                        out int skeletonOwned,
                        out _), Is.True);
                    Assert.That(skeletonOwned, Is.EqualTo(1));

                    Assert.That(run.CardOffers.TryApplyCard(new CardData(
                        CardKind.RecruitWolfTamer,
                        "wolf",
                        "wolf",
                        CardHighlight.None,
                        "wolf_tamer")), Is.True);
                    Assert.That(run.CompanionRuntimeHost.Adapter.TryGetCanonicalCompanionProgress(
                        "wolf_tamer",
                        out int wolfOwned,
                        out _), Is.True);
                    Assert.That(wolfOwned, Is.EqualTo(1));

                    Assert.That(run.CardOffers.TryApplyCard(new CardData(
                        CardKind.AddShieldSoldier,
                        "legacy",
                        "legacy",
                        CardHighlight.None)), Is.False);
                    Assert.That(run.Party.ActiveCompanionCount, Is.EqualTo(3));
                    Assert.That(run.ProductionSynergies.ActiveCount, Is.GreaterThanOrEqualTo(1));

                    Assert.That(run.CardOffers.TryApplyCard(new CardData(
                        CardKind.SmallHeal,
                        "retired heal",
                        "retired heal",
                        CardHighlight.None)), Is.False);
                    Assert.That(run.CardOffers.TryApplyCard(new CardData(
                        CardKind.LegionBanner,
                        "retired banner",
                        "retired banner",
                        CardHighlight.None)), Is.False);
                    Assert.That(run.Party.ActiveCompanionCount, Is.EqualTo(3));
                }
                finally
                {
                    run.Dispose();
                    app.ReleaseAll();
                    UnityEngine.Object.DestroyImmediate(fixtureRoot);
                    Time.timeScale = 1.0f;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presentationProviderRoot);
            }
        }

        [Test]
        public void Tutorial_UsesCurrentCompanionRuntime()
        {
            PresentationCatalog presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset");
            Assert.That(presentationCatalog, Is.Not.Null);

            GameObject presentationProviderRoot = new GameObject(
                "CompanionRuntimeProductionCutoverTests_RecoveryPresentationCatalogProvider");
            PresentationCatalogProvider presentationProvider =
                presentationProviderRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serializedPresentationProvider = new SerializedObject(presentationProvider);
            serializedPresentationProvider.FindProperty("_catalog").objectReferenceValue = presentationCatalog;
            serializedPresentationProvider.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo presentationAwake = typeof(PresentationCatalogProvider).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(presentationAwake, Is.Not.Null);
            presentationAwake.Invoke(presentationProvider, null);
            try
            {
                GameObject fixtureRoot = new GameObject(
                    "CompanionRuntimeProductionCutoverTests_RecoveryServiceFixture");
                Transform poolRoot = new GameObject("PoolRoot").transform;
                poolRoot.SetParent(fixtureRoot.transform, false);
                TestAssetService assets = new TestAssetService();
                FakeDataProvider data = CreateInitializedData();
                AppServices app = new AppServices(assets, data);
                ObjectPoolService pool = new ObjectPoolService(poolRoot);
                ServiceTestFixture.RecordingPrefabFactory factory =
                    new ServiceTestFixture.RecordingPrefabFactory();
                RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
                GameObject playerRoot = new GameObject("TutorialRecoveryPlayer");
                playerRoot.transform.SetParent(fixtureRoot.transform, false);
                PlayerController player = playerRoot.AddComponent<PlayerController>();
                registry.RegisterPlayer(player);
                RunState state = new RunState();
                RunServices run = new RunServices(
                    app,
                    state,
                    registry,
                    pool,
                    factory,
                    RunContext.Tutorial);
                Assert.That(run.CompanionRuntimeHost, Is.Not.Null);
                Assert.That(run.ProductionSynergies, Is.Not.Null);
                try
                {
                    Type recoveryTargetType = typeof(RunServices).Assembly.GetType(
                        "Lizzo.PV.Gameplay.Run.RunServicesTutorialRecoveryTarget");
                    Assert.That(recoveryTargetType, Is.Not.Null);
                    ConstructorInfo recoveryTargetConstructor = recoveryTargetType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(RunServices) },
                        null);
                    Assert.That(recoveryTargetConstructor, Is.Not.Null);
                    ITutorialRecoveryApplicationTarget recoveryTarget =
                        (ITutorialRecoveryApplicationTarget)recoveryTargetConstructor.Invoke(new object[] { run });

                    Assert.That(TutorialRecoveryApplication.TryApply(
                        TutorialCheckpointRecovery.Resolve(TutorialCheckpointId.RangedExpansion),
                        recoveryTarget), Is.True);
                    Assert.That(run.Party.ActiveCompanionSlotCount, Is.EqualTo(3));
                    Assert.That(run.Party.ActiveCompanionCount, Is.EqualTo(5));
                    Assert.That(state.ElapsedSeconds, Is.EqualTo(30.0f));
                    Assert.That(run.Party.TryGetCanonicalCompanionProgress(
                        "shield_guard",
                        out int shieldCount,
                        out _), Is.True);
                    Assert.That(shieldCount, Is.EqualTo(3));
                    Assert.That(run.Party.TryGetCanonicalCompanionProgress(
                        "sword_soldier",
                        out int swordCount,
                        out _), Is.True);
                    Assert.That(swordCount, Is.EqualTo(1));

                    Assert.That(
                        run.CompanionRuntimeHost.CardInput.SubmitCard(100L, "falcon_archer").Accepted,
                        Is.True);
                    Assert.That(run.Party.TryGetCanonicalCompanionProgress(
                        "falcon_archer",
                        out int archerCount,
                        out _), Is.True);
                    Assert.That(archerCount, Is.EqualTo(1));

                    Assert.That(
                        run.CompanionRuntimeHost.CardInput.SubmitCard(101L, "skeleton_scythe_thrower").Accepted,
                        Is.True);
                    Assert.That(
                        run.CompanionRuntimeHost.CardInput.SubmitCard(102L, "wolf_tamer").Accepted,
                        Is.True);
                    Assert.That(run.Party.TryGetCanonicalCompanionProgress(
                        "skeleton_scythe_thrower",
                        out int skeletonCount,
                        out _), Is.True);
                    Assert.That(skeletonCount, Is.GreaterThanOrEqualTo(1));
                    Assert.That(run.Party.TryGetCanonicalCompanionProgress(
                        "wolf_tamer",
                        out int wolfCount,
                        out _), Is.True);
                    Assert.That(wolfCount, Is.GreaterThanOrEqualTo(1));
                }
                finally
                {
                    run.Dispose();
                    app.ReleaseAll();
                    UnityEngine.Object.DestroyImmediate(fixtureRoot);
                    Time.timeScale = 1.0f;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presentationProviderRoot);
            }
        }

        [Test]
        public void ProductionPresentationSet_CoversApprovedThinSquadRoots()
        {
            PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.CompanionRuntime, Is.Not.Null);
            Assert.That(catalog.CompanionRuntime.Entries.Count, Is.EqualTo(ExpectedLineages.Length));

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (CompanionRuntimePresentationSet.Entry entry in catalog.CompanionRuntime.Entries)
            {
                Assert.That(ids.Add(entry.CompanionId), Is.True);
                Assert.That(entry.SquadRootPrefab, Is.Not.Null, entry.CompanionId);
                Assert.That(entry.SquadRootPrefab.CompanionId, Is.EqualTo(entry.CompanionId));
                Component[] components = entry.SquadRootPrefab.GetComponentsInChildren<Component>(true);
                foreach (Component component in components)
                {
                    Assert.That(component, Is.Not.InstanceOf<Collider2D>());
                    Assert.That(component.GetType().Name, Is.Not.EqualTo("AllyFollower"));
                    Assert.That(component.GetType().Name, Is.Not.EqualTo("AllyCombat"));
                    Assert.That(component.GetType().Name, Is.Not.EqualTo("CompanionRuntime"));
                }
            }

            Assert.That(ids, Is.EquivalentTo(ExpectedLineages));
        }

        private static FakeDataProvider CreateInitializedData()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            return data;
        }

        private sealed class SingleDefinitionCatalog : ICompanionDefinitionCatalog
        {
            private readonly CompanionDefinition _definition;

            internal SingleDefinitionCatalog(string id, ActionSet set)
            {
                _definition = new CompanionDefinition(id, set);
            }

            public bool TryGetDefinition(string companionId, out CompanionDefinition definition)
            {
                definition = string.Equals(companionId, _definition.CompanionId, StringComparison.Ordinal)
                    ? _definition
                    : null;
                return definition != null;
            }
        }

        private sealed class NoTargetWorld : ICompanionCombatWorld
        {
            public bool TrySelectTargetPosition(out CompanionPoint targetPosition)
            {
                targetPosition = default;
                return false;
            }

            public EffectResolution Resolve(in EffectIntent intent) => default;
        }

    }
}
