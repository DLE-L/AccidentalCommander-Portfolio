using StringComparer = System.StringComparer;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Gameplay.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionPresentationTracerTests
    {
        [Test]
        public void ReturningFlightView_UsesCombatCoordinatesInsteadOfASecondTravelClock()
        {
            PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset");
            var providerObject = new GameObject("ReturningFlightViewTestProvider");
            GameObject payload = null;
            providerObject.SetActive(false);
            try
            {
                PresentationCatalogProvider provider = providerObject.AddComponent<PresentationCatalogProvider>();
                var serialized = new SerializedObject(provider);
                serialized.FindProperty("_catalog").objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                providerObject.SetActive(true);
                typeof(PresentationCatalogProvider).GetMethod("Awake",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(provider, null);
                var flight = new Lizzo.PV.Legion.ReturningAttackFlight(Vector3.zero, new Vector3(4, 0), 0.4f);
                typeof(CompanionTravelingPayloadView).GetMethod("TryPlayFlight",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, new object[] { "dmg_skeleton_scythe_throw_v1", flight, 1.0f });
                payload = GameObject.Find("CompanionTravelingReturningScythe");
                Assert.That(payload, Is.Not.Null);
                var view = payload.GetComponent<CompanionTravelingPayloadView>();
                var display = typeof(CompanionTravelingPayloadView).GetMethod("LateUpdate",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                flight.Advance(0.4f, Vector3.zero);
                flight.BeginReturn(1.0f);
                flight.Advance(0.5f, new Vector3(0, 2));
                display.Invoke(view, null);
                Assert.That(payload.transform.position, Is.EqualTo(new Vector3(2, 1)));
                flight.Advance(0.5f, new Vector3(0, 4));
                display.Invoke(view, null);
                Assert.That(payload.transform.position, Is.EqualTo(new Vector3(0, 4)));
            }
            finally
            {
                if (payload != null) Object.DestroyImmediate(payload);
                Object.DestroyImmediate(providerObject);
            }
        }

        private const string SharedControllerPath =
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Shared/CompanionSpriteShared.controller";
        private const string ApprovedControllerPath =
            "Assets/_LizzoPV/Gameplay/Legion/Animations/Approved/ApprovedPlayerUnitIdle.controller";
        private const string ApprovedArtRoot =
            "Assets/_LizzoPV/Gameplay/Presentation/Art/Characters/ApprovedPlayerUnits";

        private static readonly LineageFixture[] Lineages =
        {
            new LineageFixture(
                "shield_guard",
                3,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/shield_guard_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/shield_captain_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ShieldGuardSquadRoot.prefab"),
            new LineageFixture(
                "sword_soldier",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/sword_soldier_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/sword_captain_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSoldierMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordCaptainMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SwordSquadRoot.prefab"),
            new LineageFixture(
                "cleric",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/cleric_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/light_guide_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/ClericSquadRoot.prefab"),
            new LineageFixture(
                "falcon_archer",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/falcon_archer_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/falcon_captain_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FalconArcherSquadRoot.prefab"),
            new LineageFixture(
                "bombardier",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/bombardier_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/powder_captain_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/BombardierSquadRoot.prefab"),
            new LineageFixture(
                "fire_mage",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/fire_mage_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/fire_sage_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMagePromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FireMageSquadRoot.prefab"),
            new LineageFixture(
                "skeleton_scythe_thrower",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/skeleton_scythe_thrower_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/skeleton_reaper_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonScytheThrowerBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonReaperPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/SkeletonScytheThrowerSquadRoot.prefab"),
            new LineageFixture(
                "wolf_tamer",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/wolf_tamer_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/beast_commander_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WolfTamerSquadRoot.prefab"),
            new LineageFixture(
                "field_herbalist",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/field_herbalist_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/battle_apothecary_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FieldHerbalistBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FieldHerbalistPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/FieldHerbalistSquadRoot.prefab"),
            new LineageFixture(
                "lightning_mage",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/lightning_mage_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/storm_mage_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/LightningMageBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/LightningMagePromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/LightningMageSquadRoot.prefab"),
            new LineageFixture(
                "wraith_knight",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/wraith_knight_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/wraith_guardian_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WraithKnightBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WraithKnightPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/WraithKnightSquadRoot.prefab"),
            new LineageFixture(
                "necromancer",
                4,
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/necromancer_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/dark_ritualist_SpriteLibrary.asset",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/NecromancerBaseMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/NecromancerPromotedMemberView.prefab",
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/CompanionRuntime/Presentation/NecromancerSquadRoot.prefab")
        };

        [Test]
        public void ApprovedLineagePrefabs_AreThinAndUseCanonicalVisualAssets()
        {
            RuntimeAnimatorController controller = LoadRequired<RuntimeAnimatorController>(SharedControllerPath);
            RuntimeAnimatorController approvedController = LoadRequired<RuntimeAnimatorController>(ApprovedControllerPath);
            for (int lineageIndex = 0; lineageIndex < Lineages.Length; lineageIndex += 1)
            {
                LineageFixture lineage = Lineages[lineageIndex];
                GameObject basePrefab = LoadRequired<GameObject>(lineage.BasePrefabPath);
                GameObject promotedPrefab = LoadRequired<GameObject>(lineage.PromotedPrefabPath);
                GameObject squadPrefab = LoadRequired<GameObject>(lineage.SquadPrefabPath);
                bool usesApprovedBaseArt = TryGetApprovedBaseLibraryPath(
                    lineage.CompanionId,
                    out string approvedBaseLibraryPath);
                SpriteLibraryAsset baseLibrary = LoadRequired<SpriteLibraryAsset>(
                    usesApprovedBaseArt ? approvedBaseLibraryPath : lineage.BaseLibraryPath);
                SpriteLibraryAsset promotedLibrary = LoadRequired<SpriteLibraryAsset>(lineage.PromotedLibraryPath);

                AssertThinMemberPrefab(
                    basePrefab,
                    baseLibrary,
                    usesApprovedBaseArt ? approvedController : controller,
                    false,
                    lineage.AttackFrameCount,
                    usesApprovedBaseArt);
                AssertThinMemberPrefab(
                    promotedPrefab,
                    promotedLibrary,
                    controller,
                    true,
                    lineage.AttackFrameCount,
                    false);

                CompanionSquadRoot squadRoot = squadPrefab.GetComponent<CompanionSquadRoot>();
                Assert.That(squadRoot, Is.Not.Null, lineage.CompanionId);
                Assert.That(squadRoot.CompanionId, Is.EqualTo(lineage.CompanionId));
                Assert.That(squadRoot.MemberViewCount, Is.EqualTo(3));
                Assert.That(squadPrefab.GetComponentsInChildren<CompanionMemberView>(true).Length, Is.EqualTo(3));
                for (int memberIndex = 0; memberIndex < squadRoot.MemberViewCount; memberIndex += 1)
                {
                    CompanionMemberView view = squadRoot.GetMemberView(memberIndex);
                    Assert.That(view, Is.Not.Null);
                    Assert.That(view.MemberOrder, Is.EqualTo(memberIndex));
                    Assert.That(view.IsPromotedLeaderVisual, Is.EqualTo(memberIndex == 2));
                    Assert.That(view.gameObject.activeSelf, Is.EqualTo(memberIndex == 0));
                }

                AssertNoObsoleteRuntimeOwnership(squadPrefab);
            }
        }

        [Test]
        public void ReturningScythePresentation_UsesCatalogSpriteAndCreatesTravelingView()
        {
            const string catalogPath = "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset";
            const string effectId = "dmg_skeleton_scythe_throw_v1";
            GameObject providerObject = new GameObject("ReturningScythePresentationTestProvider");
            GameObject payload = null;
            try
            {
                providerObject.SetActive(false);
                PresentationCatalogProvider provider = providerObject.AddComponent<PresentationCatalogProvider>();
                PresentationCatalog catalog = LoadRequired<PresentationCatalog>(catalogPath);
                var providerSerialized = new SerializedObject(provider);
                providerSerialized.FindProperty("_catalog").objectReferenceValue = catalog;
                providerSerialized.ApplyModifiedPropertiesWithoutUndo();
                providerObject.SetActive(true);
                typeof(PresentationCatalogProvider)
                    .GetMethod(
                        "Awake",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(provider, null);

                Assert.That(
                    CompanionTravelingPayloadView.TryPlay(
                        AttackDelivery.ReturningProjectile,
                        effectId,
                        Vector3.zero,
                        Vector3.right * 4.0f,
                        0.5f,
                        1.0f),
                    Is.True);

                payload = GameObject.Find("CompanionTravelingReturningScythe");
                Assert.That(payload, Is.Not.Null);
                SpriteRenderer renderer = payload.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(catalog.Projectiles.TryGetVisual(effectId, out ProjectilePresentationCatalog.VisualDefinition visual), Is.True);
                Assert.That(renderer.sprite, Is.SameAs(visual.BodySprite));
                Assert.That(payload.GetComponent<CompanionTravelingPayloadView>(), Is.Not.Null);
            }
            finally
            {
                if (payload != null)
                    Object.DestroyImmediate(payload);
                Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void ReturningScytheResolution_CreatesVisibleReturnLeg()
        {
            const string catalogPath = "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset";
            const string effectId = "dmg_skeleton_scythe_throw_v1";
            GameObject providerObject = new GameObject("ReturningScytheResolutionTestProvider");
            GameObject commander = new GameObject("ReturningScytheResolutionTestCommander");
            GameObject payload = null;
            object host = null;
            try
            {
                providerObject.SetActive(false);
                PresentationCatalogProvider provider = providerObject.AddComponent<PresentationCatalogProvider>();
                PresentationCatalog catalog = LoadRequired<PresentationCatalog>(catalogPath);
                var providerSerialized = new SerializedObject(provider);
                providerSerialized.FindProperty("_catalog").objectReferenceValue = catalog;
                providerSerialized.ApplyModifiedPropertiesWithoutUndo();
                providerObject.SetActive(true);
                typeof(PresentationCatalogProvider)
                    .GetMethod(
                        "Awake",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(provider, null);

                var followUps = new[]
                {
                    new IndependentEffectRequest(
                        "squad-scythe",
                        "skeleton_scythe_thrower",
                        effectId,
                        15.0f,
                        new CompanionPoint(4.0f, 0.0f),
                        CombatMotion.Stationary,
                        AttackDelivery.ReturningProjectile,
                        0,
                        effectId,
                        1.0f),
                };
                var resolution = new EffectResolution(true, effectId, 15.0f, 1, followUps);
                var cue = new PresentationCue(
                    effectId,
                    "squad-scythe",
                    0,
                    CompanionPoint.Zero,
                    new CompanionPoint(4.0f, 0.0f),
                    AttackDelivery.ReturningProjectile,
                    0.35f);
                var runEvent = new CompanionRunEvent(
                    1,
                    CompanionRunEventKind.EffectResolved,
                    "squad-scythe",
                    "skeleton_scythe_thrower",
                    resolution,
                    cue);
                var squad = new SquadSnapshot(
                    "squad-scythe",
                    0,
                    "skeleton_scythe_thrower",
                    "skeleton-scythe-base",
                    1,
                    false,
                    true,
                    0.0f,
                    CompanionPoint.Zero,
                    new[] { new CompanionMemberSnapshot(0, false, CompanionPoint.Zero) });
                var batch = new CompanionRunOutputBatch(
                    new CompanionRunSnapshot(0, 0, 0.0f, new[] { squad }),
                    new[] { runEvent });

                System.Type hostType = typeof(CompanionTravelingPayloadView).Assembly.GetType(
                    "Lizzo.PV.Legion.RunCore.CompanionRuntimePresentationHost",
                    true);
                host = System.Activator.CreateInstance(
                    hostType,
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic,
                    null,
                    new object[] { catalog.CompanionRuntime },
                    null);
                hostType.GetMethod(
                        "Consume",
                        System.Reflection.BindingFlags.Instance
                            | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(host, new object[] { batch, commander.transform, 0.0f });

                payload = GameObject.Find("CompanionTravelingReturningScythe");
                Assert.That(payload, Is.Not.Null);
                Assert.That(payload.transform.position.x, Is.EqualTo(4.0f).Within(0.001f));
                Assert.That(payload.transform.position.y, Is.EqualTo(0.0f).Within(0.001f));
                CompanionSquadRoot squadRoot = commander.GetComponentInChildren<CompanionSquadRoot>();
                Assert.That(squadRoot, Is.Not.Null);
                CompanionMemberView memberView = squadRoot.GetMemberView(0);
                Assert.That(memberView, Is.Not.Null);
                var targetField = typeof(CompanionTravelingPayloadView).GetField(
                    "_targetTransform",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(targetField, Is.Not.Null);
                Assert.That(
                    targetField.GetValue(payload.GetComponent<CompanionTravelingPayloadView>()),
                    Is.SameAs(memberView.transform));
            }
            finally
            {
                if (payload != null)
                    Object.DestroyImmediate(payload);
                Object.DestroyImmediate(commander);
                Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void ApprovedLineageSnapshotFlow_DrivesOneTwoAndPromotedThreeMemberViews()
        {
            for (int lineageIndex = 0; lineageIndex < Lineages.Length; lineageIndex += 1)
            {
                LineageFixture lineage = Lineages[lineageIndex];
                GameObject prefab = LoadRequired<GameObject>(lineage.SquadPrefabPath);
                GameObject instance = Object.Instantiate(prefab);
                try
                {
                    CompanionSquadRoot root = instance.GetComponent<CompanionSquadRoot>();
                    Assert.That(root, Is.Not.Null);

                    SquadSnapshot one = CreateSnapshot(
                        lineage.CompanionId,
                        false,
                        SquadActionPhase.Idle,
                        -1,
                        new CompanionPoint(1.0f, 2.0f),
                        new CompanionPoint(1.0f, 2.0f),
                        null,
                        new CompanionMemberSnapshot(0, false, CompanionPoint.Zero));
                    Assert.That(root.TryApplySnapshot(in one), Is.True);
                    Assert.That(ActiveViewCount(root), Is.EqualTo(1));
                    AssertPoint(root.transform.localPosition, 1.0f, 2.0f);
                    AssertPoint(root.GetMemberView(0).transform.localPosition, 0.0f, 0.0f);

                    SquadSnapshot two = CreateSnapshot(
                        lineage.CompanionId,
                        false,
                        SquadActionPhase.Idle,
                        -1,
                        new CompanionPoint(-1.0f, 1.2f),
                        new CompanionPoint(-1.0f, 1.2f),
                        null,
                        new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, 0.0f)),
                        new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, 0.0f)));
                    Assert.That(root.TryApplySnapshot(in two), Is.True);
                    Assert.That(ActiveViewCount(root), Is.EqualTo(2));
                    AssertPoint(root.GetMemberView(0).transform.localPosition, -0.22f, 0.0f);
                    AssertPoint(root.GetMemberView(1).transform.localPosition, 0.22f, 0.0f);

                    SquadSnapshot three = CreateSnapshot(
                        lineage.CompanionId,
                        true,
                        SquadActionPhase.Idle,
                        -1,
                        new CompanionPoint(0.0f, 1.2f),
                        new CompanionPoint(0.0f, 1.2f),
                        null,
                        new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, -0.14f)),
                        new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, -0.14f)),
                        new CompanionMemberSnapshot(2, true, new CompanionPoint(0.0f, 0.22f)));
                    Assert.That(root.TryApplySnapshot(in three), Is.True);
                    Assert.That(ActiveViewCount(root), Is.EqualTo(3));
                    Assert.That(root.GetMemberView(2).IsPromotedLeaderVisual, Is.True);
                    AssertPoint(root.GetMemberView(2).transform.localPosition, 0.0f, 0.22f);

                    SquadSnapshot approaching = CreateSnapshot(
                        lineage.CompanionId,
                        true,
                        SquadActionPhase.Approaching,
                        0,
                        new CompanionPoint(0.0f, 1.2f),
                        new CompanionPoint(0.8f, 1.2f),
                        new CompanionPoint(2.0f, 1.2f),
                        new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, -0.14f)),
                        new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, -0.14f)),
                        new CompanionMemberSnapshot(2, true, new CompanionPoint(0.0f, 0.22f)));
                    Assert.That(root.TryApplySnapshot(in approaching), Is.True);
                    AssertPoint(root.GetMemberView(0).transform.localPosition, 0.8f, 0.0f);
                    Assert.That(root.TryPlayAttack(0, new CompanionPoint(2.0f, 1.2f), 0.28f), Is.True);
                    Assert.That(root.TryPlayAttack(7, new CompanionPoint(2.0f, 1.2f), 0.28f), Is.False);
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void ProductionPresentationSet_CoversApprovedThinSquadRoots()
        {
            PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.CompanionRuntime, Is.Not.Null);
            Assert.That(catalog.CompanionRuntime.Entries.Count, Is.EqualTo(Lineages.Length));

            var expectedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < Lineages.Length; index += 1)
            {
                expectedIds.Add(Lineages[index].CompanionId);
            }

            var actualIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CompanionRuntimePresentationSet.Entry entry in catalog.CompanionRuntime.Entries)
            {
                Assert.That(actualIds.Add(entry.CompanionId), Is.True, entry.CompanionId);
                Assert.That(entry.SquadRootPrefab, Is.Not.Null, entry.CompanionId);
                Assert.That(entry.SquadRootPrefab.CompanionId, Is.EqualTo(entry.CompanionId));
                AssertNoObsoleteRuntimeOwnership(entry.SquadRootPrefab.gameObject);
            }

            Assert.That(actualIds, Is.EquivalentTo(expectedIds));
        }

        private static void AssertThinMemberPrefab(
            GameObject prefab,
            SpriteLibraryAsset expectedLibrary,
            RuntimeAnimatorController expectedController,
            bool promotedLeader,
            int expectedAttackFrameCount,
            bool usesApprovedIdleOnlyArt)
        {
            CompanionMemberView member = prefab.GetComponent<CompanionMemberView>();
            Assert.That(member, Is.Not.Null);
            Assert.That(member.IsPromotedLeaderVisual, Is.EqualTo(promotedLeader));
            Assert.That(member.VisualDriver, Is.Not.Null);

            Transform visual = prefab.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null);
            float expectedScale = promotedLeader ? 0.8f : 0.6f;
            Assert.That(visual.localScale, Is.EqualTo(new Vector3(expectedScale, expectedScale, 1.0f)));
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            Animator animator = visual.GetComponent<Animator>();
            SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
            SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(library, Is.Not.Null);
            Assert.That(resolver, Is.Not.Null);
            Assert.That(renderer.sortingOrder, Is.EqualTo(20));
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.sprite.name, Is.EqualTo(usesApprovedIdleOnlyArt ? "Frame_0" : "Idle_0"));
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(expectedController));
            Assert.That(library.spriteLibraryAsset, Is.SameAs(expectedLibrary));
            Assert.That(member.VisualDriver.IdleFrameCount, Is.EqualTo(usesApprovedIdleOnlyArt ? 8 : 2));

            if (usesApprovedIdleOnlyArt)
            {
                Assert.That(expectedController.animationClips.Length, Is.EqualTo(1));
            }
            else
            {
                Assert.That(member.VisualDriver.RunFrameCount, Is.EqualTo(4));
                Assert.That(member.VisualDriver.AttackFrameCount, Is.EqualTo(expectedAttackFrameCount));
                Assert.That(member.VisualDriver.DeathFrameCount, Is.EqualTo(3));

                AnimationClip attackClip = FindClip(expectedController, "CompanionSpriteShared_Attack");
                SerializedProperty attackClipLength = new SerializedObject(member.VisualDriver)
                    .FindProperty("_attackClipLength");
                Assert.That(attackClipLength, Is.Not.Null);
                Assert.That(attackClipLength.floatValue, Is.EqualTo(attackClip.length).Within(0.0001f));
            }
            AssertNoObsoleteRuntimeOwnership(prefab);
        }

        private static bool TryGetApprovedBaseLibraryPath(string companionId, out string path)
        {
            switch (companionId)
            {
                case "shield_guard":
                    path = ApprovedArtRoot + "/ShieldGuard_SpriteLibrary.asset";
                    return true;
                case "sword_soldier":
                    path = ApprovedArtRoot + "/SwordSoldier_SpriteLibrary.asset";
                    return true;
                case "cleric":
                    path = ApprovedArtRoot + "/Cleric_SpriteLibrary.asset";
                    return true;
                case "falcon_archer":
                    path = ApprovedArtRoot + "/FalconArcher_SpriteLibrary.asset";
                    return true;
                case "bombardier":
                    path = ApprovedArtRoot + "/Bombardier_SpriteLibrary.asset";
                    return true;
                case "skeleton_scythe_thrower":
                    path = ApprovedArtRoot + "/SkeletonScytheThrower_SpriteLibrary.asset";
                    return true;
                default:
                    path = null;
                    return false;
            }
        }

        private static void AssertNoObsoleteRuntimeOwnership(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int index = 0; index < components.Length; index += 1)
            {
                Component component = components[index];
                if (component == null)
                {
                    Assert.Fail("Missing script exists on " + root.name);
                }

                Assert.That(component, Is.Not.InstanceOf<Rigidbody2D>());
                Assert.That(component, Is.Not.InstanceOf<Collider2D>());
                string typeName = component.GetType().Name;
                Assert.That(typeName, Is.Not.EqualTo("AllyCombat"));
                Assert.That(typeName, Is.Not.EqualTo("AllyFollower"));
                Assert.That(typeName, Is.Not.EqualTo("CompanionRuntime"));
                Assert.That(typeName, Is.Not.EqualTo("CompanionHealthBar"));
                Assert.That(component, Is.Not.InstanceOf<UnitVisualRole>());
            }
        }

        private static SquadSnapshot CreateSnapshot(
            string companionId,
            bool promoted,
            SquadActionPhase actionPhase,
            int activeMemberOrder,
            CompanionPoint formationAnchor,
            CompanionPoint activeMemberPosition,
            CompanionPoint? committedTargetPosition,
            params CompanionMemberSnapshot[] members)
        {
            return new SquadSnapshot(
                "squad-0",
                0,
                companionId,
                promoted ? companionId + "-promoted" : companionId + "-base",
                members.Length,
                promoted,
                true,
                0.0f,
                formationAnchor,
                actionPhase,
                activeMemberOrder,
                activeMemberPosition,
                committedTargetPosition,
                members);
        }

        private static int ActiveViewCount(CompanionSquadRoot root)
        {
            int count = 0;
            for (int index = 0; index < root.MemberViewCount; index += 1)
            {
                CompanionMemberView view = root.GetMemberView(index);
                if (view != null && view.gameObject.activeSelf)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static void AssertPoint(Vector3 actual, float expectedX, float expectedY)
        {
            Assert.That(actual.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expectedY).Within(0.0001f));
            Assert.That(actual.z, Is.Zero.Within(0.0001f));
        }

        private static AnimationClip FindClip(RuntimeAnimatorController controller, string name)
        {
            AnimationClip[] clips = controller.animationClips;
            for (int index = 0; index < clips.Length; index += 1)
            {
                if (clips[index] != null && clips[index].name == name)
                {
                    return clips[index];
                }
            }

            Assert.Fail("Missing animation clip: " + name);
            return null;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private readonly struct LineageFixture
        {
            public LineageFixture(
                string companionId,
                int attackFrameCount,
                string baseLibraryPath,
                string promotedLibraryPath,
                string basePrefabPath,
                string promotedPrefabPath,
                string squadPrefabPath)
            {
                CompanionId = companionId;
                AttackFrameCount = attackFrameCount;
                BaseLibraryPath = baseLibraryPath;
                PromotedLibraryPath = promotedLibraryPath;
                BasePrefabPath = basePrefabPath;
                PromotedPrefabPath = promotedPrefabPath;
                SquadPrefabPath = squadPrefabPath;
            }

            public string CompanionId { get; }

            public int AttackFrameCount { get; }

            public string BaseLibraryPath { get; }

            public string PromotedLibraryPath { get; }

            public string BasePrefabPath { get; }

            public string PromotedPrefabPath { get; }

            public string SquadPrefabPath { get; }
        }
    }
}
