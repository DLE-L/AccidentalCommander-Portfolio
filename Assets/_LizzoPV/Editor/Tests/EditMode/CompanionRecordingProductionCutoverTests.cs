using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRecordingProductionCutoverTests
    {
        private static readonly string[] ExpectedLineages =
        {
            "shield_guard",
            "sword_soldier",
            "cleric",
            "falcon_archer",
            "bombardier",
            "fire_mage",
            "skeleton_bomber",
            "wolf_tamer",
        };

        [Test]
        public void CardCatalogProvider_InitializesAfterAppBootstrapAndBeforeRunBootstrap()
        {
            DefaultExecutionOrder appOrder = typeof(AppBootstrap).GetCustomAttribute<DefaultExecutionOrder>();
            DefaultExecutionOrder providerOrder = typeof(CardCatalogProvider).GetCustomAttribute<DefaultExecutionOrder>();
            DefaultExecutionOrder runOrder = typeof(RunBootstrap).GetCustomAttribute<DefaultExecutionOrder>();

            Assert.That(appOrder, Is.Not.Null);
            Assert.That(providerOrder, Is.Not.Null);
            Assert.That(runOrder, Is.Not.Null);
            Assert.That(providerOrder.order, Is.GreaterThan(appOrder.order));
            Assert.That(providerOrder.order, Is.LessThan(runOrder.order));
        }

        [Test]
        public void RecordingDefinitionCatalog_CoversApprovedRecruitLineagesAndDataDrivenSwordRoles()
        {
            FakeDataProvider data = CreateInitializedData();
            CompanionRecordingDefinitionCatalog catalog = new CompanionRecordingDefinitionCatalog(
                data,
                RunDefinitionResolver.Resolve(RunContext.Normal, data));

            Assert.That(catalog.LineageIds, Is.EqualTo(ExpectedLineages));
            for (int index = 0; index < ExpectedLineages.Length; index += 1)
                Assert.That(catalog.TryGetDefinition(ExpectedLineages[index], out _), Is.True, ExpectedLineages[index]);
            Assert.That(catalog.TryGetDefinition("falcon_archer", out _), Is.True);

            Assert.That(catalog.TryGetDefinition("skeleton_bomber", out CompanionDefinition skeleton), Is.True);
            Assert.That(skeleton.BaseActionSet.Steps[0].Delivery, Is.EqualTo(AttackDelivery.ReturningProjectile));
            Assert.That(catalog.TryGetDefinition("wolf_tamer", out CompanionDefinition wolf), Is.True);
            Assert.That(wolf.BaseActionSet.Steps[0].Delivery, Is.EqualTo(AttackDelivery.OwnedProxy));

            Assert.That(catalog.TryGetDefinition("sword_soldier", out CompanionDefinition sword), Is.True);
            CombatEffectData effect = data.GetCombatEffect("dmg_sword_slash_v1");
            CompanionPromotionData promotion = data.GetCompanionPromotion("sword_captain");
            Assert.That(sword.BaseActionSet.Steps.Count, Is.EqualTo(1));
            Assert.That(sword.BaseActionSet.Steps[0].Motion, Is.EqualTo(CombatMotion.Excursion));
            Assert.That(sword.BaseActionSet.Steps[0].EffectId, Is.EqualTo(effect.Id));
            Assert.That(sword.BaseActionSet.Steps[0].ActionDurationSeconds, Is.EqualTo(0.12f));
            Assert.That(sword.BaseActionSet.Steps[0].ExcursionStandOffDistance, Is.EqualTo(1.35f));
            Assert.That(sword.BaseActionSet.Steps[0].ExcursionLateralOffset, Is.EqualTo(0.30f));
            Assert.That(sword.PromotedActionSet.Steps.Count, Is.EqualTo(1));
            Assert.That(sword.PromotedActionSet.Steps[0].Motion, Is.EqualTo(CombatMotion.Stationary));
            Assert.That(sword.PromotedActionSet.Steps[0].Magnitude, Is.EqualTo(effect.BaseValue * promotion.EffectMultiplier));
            Assert.That(sword.PromotedActionSet.CooldownSeconds, Is.EqualTo(effect.CastInterval * promotion.IntervalMultiplier));

            Assert.That(catalog.TryGetDefinition("cleric", out CompanionDefinition cleric), Is.True);
            Assert.That(cleric.BaseActionSet.Steps.Count, Is.EqualTo(2));
            Assert.That(cleric.BaseActionSet.Steps[1].EffectId, Is.EqualTo("heal_cleric_v1"));
            Assert.That(cleric.PromotedActionSet.Steps.Count, Is.EqualTo(2));
        }

        [Test]
        public void TutorialShieldDefinition_UsesExistingMoveSpeedAndInterceptContract()
        {
            FakeDataProvider data = CreateInitializedData();
            CompanionRecordingDefinitionCatalog catalog = new CompanionRecordingDefinitionCatalog(
                data,
                RunDefinitionResolver.Resolve(RunContext.Tutorial, data));

            Assert.That(catalog.TryGetDefinition("shield_guard", out CompanionDefinition shield), Is.True);
            ActionStep step = shield.BaseActionSet.Steps[0];
            Assert.That(shield.BaseActionSet.CooldownSeconds, Is.EqualTo(2.5f));
            Assert.That(step.Magnitude, Is.EqualTo(10.0f));
            Assert.That(step.Motion, Is.EqualTo(CombatMotion.Excursion));
            Assert.That(step.TargetAcquisitionRange, Is.EqualTo(3.0f));
            Assert.That(step.ExcursionMaxDepartureDistance, Is.EqualTo(2.0f));
            Assert.That(step.ExcursionSpeed, Is.EqualTo(data.GetCompanionCombatProfile("shield_guard").MoveSpeed));
            Assert.That(step.ReturnSpeed, Is.EqualTo(step.ExcursionSpeed));
            Assert.That(step.AvoidSharedTarget, Is.True);
            Assert.That(shield.PromotedActionSet.Steps[0], Is.EqualTo(step));
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
        public void FixedCardPool_RecordingProfileRoutesCanonicalCardToProductionHost()
        {
            CardCatalog catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(
                "Assets/_LizzoPV/Gameplay/CardOffer/Data/CardCatalog.asset");
            PresentationCatalog presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(presentationCatalog, Is.Not.Null);

            GameObject providerRoot = new GameObject("CompanionRecordingProductionCutoverTests_CatalogProvider");
            CardCatalogProvider provider = providerRoot.AddComponent<CardCatalogProvider>();
            SerializedObject serializedProvider = new SerializedObject(provider);
            serializedProvider.FindProperty("_catalog").objectReferenceValue = catalog;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo awake = typeof(CardCatalogProvider).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null);
            awake.Invoke(provider, null);

            GameObject presentationProviderRoot = new GameObject(
                "CompanionRecordingProductionCutoverTests_PresentationCatalogProvider");
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
                GameObject fixtureRoot = new GameObject("CompanionRecordingProductionCutoverTests_ServiceFixture");
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
                    RunStartRequest.Fresh(RunContext.Normal).Resolve(data),
                    cardPoolDefinition: catalog.Pool);
                Assert.That(run.RecordingCompanions, Is.Not.Null);
                FixedCardPool.Configure(
                    run.Registry,
                    run.Party,
                    companionCardInput: run.RecordingCompanions.CardInput,
                    companionRosterView: run.RecordingCompanions.Adapter,
                    runDefinition: run.Definition);
                CardEffectRuntime.Configure(run.Registry, run.Party);
                CardEffectRuntime.ResetRunState();
                try
                {
                    CardData card = new CardData(
                        CardKind.AddShieldSoldier,
                        "shield",
                        "shield",
                        CardHighlight.None,
                        "shield_guard");

                    Assert.That(FixedCardPool.TryApplyCard(card), Is.True);
                    Assert.That(run.RecordingCompanions.Adapter.TryGetCanonicalCompanionProgress(
                        "shield_guard",
                        out int owned,
                        out int preview), Is.True);
                    Assert.That(owned, Is.EqualTo(1));
                    Assert.That(preview, Is.EqualTo(2));
                    Assert.That(run.Party.ActiveCompanionSlotCount, Is.EqualTo(1));
                    Assert.That(run.Party.ActiveCompanionCount, Is.EqualTo(1));
                    Assert.That(run.Party.TryGetCompanionProgress(
                        CompanionKind.ShieldSoldier,
                        out int enumOwned,
                        out int enumPreview), Is.True);
                    Assert.That(enumOwned, Is.EqualTo(1));
                    Assert.That(enumPreview, Is.EqualTo(2));
                    Assert.That(run.Party.TryGetSquadSlotForCompanion(
                        CompanionKind.ShieldSoldier,
                        out SquadSlotState slot), Is.True);
                    Assert.That(slot.BaseUnitId, Is.EqualTo("shield_guard"));
                    Assert.That(slot.CurrentCount, Is.EqualTo(1));

                    Assert.That(FixedCardPool.TryApplyCard(new CardData(
                        CardKind.RecruitSkeletonBomber,
                        "skeleton",
                        "skeleton",
                        CardHighlight.None,
                        "skeleton_bomber")), Is.True);
                    Assert.That(run.RecordingCompanions.Adapter.TryGetCanonicalCompanionProgress(
                        "skeleton_bomber",
                        out int skeletonOwned,
                        out _), Is.True);
                    Assert.That(skeletonOwned, Is.EqualTo(1));

                    Assert.That(FixedCardPool.TryApplyCard(new CardData(
                        CardKind.RecruitWolfTamer,
                        "wolf",
                        "wolf",
                        CardHighlight.None,
                        "wolf_tamer")), Is.True);
                    Assert.That(run.RecordingCompanions.Adapter.TryGetCanonicalCompanionProgress(
                        "wolf_tamer",
                        out int wolfOwned,
                        out _), Is.True);
                    Assert.That(wolfOwned, Is.EqualTo(1));

                    Assert.That(FixedCardPool.TryApplyCard(new CardData(
                        CardKind.AddShieldSoldier,
                        "legacy",
                        "legacy",
                        CardHighlight.None)), Is.False);
                    Assert.That(run.Party.ActiveCompanionCount, Is.EqualTo(3));

                    Assert.That(FixedCardPool.TryApplyCard(new CardData(
                        CardKind.SmallHeal,
                        "heal",
                        "heal",
                        CardHighlight.None)), Is.True);
                    Assert.That(FixedCardPool.TryApplyCard(new CardData(
                        CardKind.LegionBanner,
                        "banner",
                        "banner",
                        CardHighlight.None)), Is.True);
                    Assert.That(run.Party.ActiveCompanionCount, Is.EqualTo(3));
                }
                finally
                {
                    FixedCardPool.ClearServices();
                    CardEffectRuntime.ResetRunState();
                    run.Dispose();
                    app.ReleaseAll();
                    UnityEngine.Object.DestroyImmediate(fixtureRoot);
                    Time.timeScale = 1.0f;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presentationProviderRoot);
                UnityEngine.Object.DestroyImmediate(providerRoot);
            }
        }

        [Test]
        public void TutorialRecovery_RecordingProfileRestoresRosterAndPreservesCardSequence()
        {
            CardCatalog catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(
                "Assets/_LizzoPV/Gameplay/CardOffer/Data/CardCatalog.asset");
            PresentationCatalog presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(presentationCatalog, Is.Not.Null);

            GameObject providerRoot = new GameObject(
                "CompanionRecordingProductionCutoverTests_RecoveryCatalogProvider");
            CardCatalogProvider provider = providerRoot.AddComponent<CardCatalogProvider>();
            SerializedObject serializedProvider = new SerializedObject(provider);
            serializedProvider.FindProperty("_catalog").objectReferenceValue = catalog;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo awake = typeof(CardCatalogProvider).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null);
            awake.Invoke(provider, null);

            GameObject presentationProviderRoot = new GameObject(
                "CompanionRecordingProductionCutoverTests_RecoveryPresentationCatalogProvider");
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
                    "CompanionRecordingProductionCutoverTests_RecoveryServiceFixture");
                Transform poolRoot = new GameObject("PoolRoot").transform;
                poolRoot.SetParent(fixtureRoot.transform, false);
                TestAssetService assets = new TestAssetService();
                FakeDataProvider data = CreateInitializedData();
                AppServices app = new AppServices(assets, data);
                ObjectPoolService pool = new ObjectPoolService(poolRoot);
                ServiceTestFixture.RecordingPrefabFactory factory =
                    new ServiceTestFixture.RecordingPrefabFactory();
                RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
                RunState state = new RunState();
                RunServices run = new RunServices(
                    app,
                    state,
                    registry,
                    pool,
                    factory,
                    RunStartRequest.Fresh(RunContext.Tutorial).Resolve(data),
                    cardPoolDefinition: catalog.Pool);
                Assert.That(run.RecordingCompanions, Is.Not.Null);
                FixedCardPool.Configure(
                    run.Registry,
                    run.Party,
                    RunContext.Tutorial,
                    app.CompanionUnlockProgress,
                    companionCardInput: run.RecordingCompanions.CardInput,
                    companionRosterView: run.RecordingCompanions.Adapter,
                    runDefinition: run.Definition);
                try
                {
                    Type recoveryTargetType = typeof(RunServices).Assembly.GetType(
                        "Lizzo.PV.Gameplay.Run.RunServicesSnapshotApplicationTarget");
                    Assert.That(recoveryTargetType, Is.Not.Null);
                    ConstructorInfo recoveryTargetConstructor = recoveryTargetType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(RunServices) },
                        null);
                    Assert.That(recoveryTargetConstructor, Is.Not.Null);
                    IRunSnapshotApplicationTarget recoveryTarget =
                        (IRunSnapshotApplicationTarget)recoveryTargetConstructor.Invoke(new object[] { run });

                    Assert.That(RunSnapshotApplication.TryApply(
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

                    CardData[] offer = FixedCardPool.GetNextLevelUpCards();
                    CardData archer = default;
                    for (int index = 0; index < offer.Length; index += 1)
                    {
                        if (offer[index].Kind == CardKind.RecruitArcher)
                        {
                            archer = offer[index];
                            break;
                        }
                    }

                    Assert.That(archer, Is.Not.Null, "RangedExpansion must offer the approved archer recruit.");
                    Assert.That(FixedCardPool.TrySelect(archer), Is.True);
                    Assert.That(run.Party.TryGetCanonicalCompanionProgress(
                        "falcon_archer",
                        out int archerCount,
                        out _), Is.True);
                    Assert.That(archerCount, Is.EqualTo(1));

                    bool skeletonSelected = false;
                    bool wolfSelected = false;
                    for (int selectionIndex = 0; selectionIndex < 20 && !wolfSelected; selectionIndex += 1)
                    {
                        CardData[] nextOffer = FixedCardPool.GetNextLevelUpCards();
                        Assert.That(nextOffer, Is.Not.Empty, "selection=" + (selectionIndex + 1));
                        CardData selected = nextOffer[0];
                        Assert.That(
                            FixedCardPool.TrySelect(selected),
                            Is.True,
                            "selection=" + (selectionIndex + 1) + " card=" + selected.Kind);
                        skeletonSelected |= selected.Kind == CardKind.RecruitSkeletonBomber;
                        wolfSelected |= selected.Kind == CardKind.RecruitWolfTamer;
                    }

                    Assert.That(skeletonSelected, Is.True);
                    Assert.That(wolfSelected, Is.True);
                    Assert.That(run.Party.TryGetCanonicalCompanionProgress(
                        "skeleton_bomber",
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
                    FixedCardPool.ClearServices();
                    run.Dispose();
                    app.ReleaseAll();
                    UnityEngine.Object.DestroyImmediate(fixtureRoot);
                    Time.timeScale = 1.0f;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presentationProviderRoot);
                UnityEngine.Object.DestroyImmediate(providerRoot);
            }
        }

        [Test]
        public void RecordingCardPool_AllowsEveryProductionRecordingLineageAndRejectsUnsupportedOnes()
        {
            CardCatalog catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(
                "Assets/_LizzoPV/Gameplay/CardOffer/Data/CardCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Pool, Is.Not.Null);

            CardKind[] supported =
            {
                CardKind.AddShieldSoldier,
                CardKind.RecruitSwordsman,
                CardKind.RecruitCleric,
                CardKind.RecruitArcher,
                CardKind.RecruitBombardier,
                CardKind.RecruitFireMage,
                CardKind.RecruitSkeletonBomber,
                CardKind.RecruitWolfTamer,
            };
            for (int index = 0; index < supported.Length; index += 1)
                Assert.That(catalog.Pool.IsCompanionCardAllowed(supported[index]), Is.True, supported[index].ToString());

            Assert.That(catalog.Pool.IsCompanionCardAllowed(CardKind.RecruitFieldHerbalist), Is.False);
            Assert.That(catalog.Pool.IsCompanionCardAllowed(CardKind.RecruitLightningMage), Is.False);
            Assert.That(catalog.Pool.IsCompanionCardAllowed(CardKind.RecruitWraithKnight), Is.False);
            Assert.That(catalog.Pool.IsCompanionCardAllowed(CardKind.RecruitNecromancer), Is.False);
        }

        [Test]
        public void RecordingProfileDetection_IsExactAndIndependentFromRunContext()
        {
            CardPoolDefinition pool = ScriptableObject.CreateInstance<CardPoolDefinition>();
            try
            {
                SerializedObject serialized = new SerializedObject(pool);
                serialized.FindProperty("_profileId").stringValue = CardPoolProfileIds.Standard;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(CompanionRecordingProductionHost.IsRecordingProfile(pool), Is.False);

                serialized.Update();
                serialized.FindProperty("_profileId").stringValue = CardPoolProfileIds.Recording;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(CompanionRecordingProductionHost.IsRecordingProfile(pool), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pool);
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
            AddCombatProfile(data, "shield_guard", "shield_captain", "dmg_shield_bash_v1");
            AddCombatProfile(data, "sword_soldier", "sword_captain", "dmg_sword_slash_v1");
            AddCombatProfile(data, "cleric", "light_guide", "dmg_cleric_bolt_v1", "heal_cleric_v1");
            AddCombatProfile(data, "falcon_archer", "falcon_captain", "dmg_falcon_arrow_v1");
            AddCombatEffect(data, "dmg_sword_slash_v1", "sword_soldier", CombatDeliveryKind.Cone, 12.0f, 1.0f);
            AddCombatEffect(data, "dmg_cleric_bolt_v1", "cleric", CombatDeliveryKind.Projectile, 5.0f, 1.6f);
            AddCombatEffect(data, "heal_cleric_v1", "cleric", CombatDeliveryKind.Projectile, 8.0f, 4.0f, CombatEffectKind.Heal);
            AddCombatEffect(data, "dmg_falcon_arrow_v1", "falcon_archer", CombatDeliveryKind.Projectile, 9.0f, 0.9f);
            data.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            return data;
        }

        private static void AddCombatProfile(
            FakeDataProvider data,
            string unitId,
            string promotionId,
            string effectId,
            string secondaryEffectId = "")
        {
            MethodInfo method = typeof(FakeDataProvider).GetMethod(
                "AddCompanionCombatProfile",
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(data, new object[]
            {
                unitId,
                50,
                2.7f,
                promotionId,
                "skill",
                effectId,
                string.IsNullOrEmpty(secondaryEffectId) ? string.Empty : "secondary_skill",
                secondaryEffectId,
            });
        }

        private static void AddCombatEffect(
            FakeDataProvider data,
            string effectId,
            string unitId,
            CombatDeliveryKind delivery,
            float magnitude,
            float cooldown,
            CombatEffectKind effectKind = CombatEffectKind.Damage)
        {
            MethodInfo method = typeof(FakeDataProvider).GetMethod(
                "AddCombatEffect",
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(data, new object[]
            {
                new CombatEffectData
                {
                    Id = effectId,
                    OwnerUnitId = unitId,
                    SkillId = effectKind == CombatEffectKind.Heal ? "secondary_skill" : "skill",
                    EffectKind = effectKind,
                    DeliveryKind = delivery,
                    BaseValue = magnitude,
                    CastInterval = cooldown,
                    Range = 5.0f,
                    Radius = 1.5f,
                    MaxTargets = 3,
                    ProjectileLifetime = 2.0f,
                    TargetRule = CombatTargetRule.Nearest,
                },
            });
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
