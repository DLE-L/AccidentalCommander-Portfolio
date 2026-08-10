using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyBeastTests
    {
        static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly PropertyInfo IsDown = typeof(CompanionRuntime).GetProperty("IsDown", BindingFlags.Instance | BindingFlags.Public);

        PresentationCatalogProvider _previousProvider;
        PresentationCatalog _catalog;
        GameObject _providerRoot;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset");
            OwnedSupportPresentationSet supports =
                AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>(
                    "Assets/_LizzoPV/Gameplay/Legion/Data/Presentation/OwnedSupportPresentationSet.asset");
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, units, supports);
            _providerRoot = new GameObject("SynergyBeastCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serialized = new(provider);
            serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ActiveProvider.SetValue(null, provider);
        }

        [TearDown]
        public void TearDown()
        {
            if (_providerRoot != null) UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_catalog != null) UnityEngine.Object.DestroyImmediate(_catalog);
            ActiveProvider.SetValue(null, _previousProvider);
        }

        [Test]
        public void LessThanTwoLivingBeastSquads_ConsumesRoundWithoutHits()
        {
            using BeastFixture fixture = new();
            Assert.That(fixture.Party.RecruitCanonical("wolf_tamer"), Is.True);
            fixture.Activations.Refresh(CreateSlots("wolf_tamer", "falcon_archer"));

            Assert.That(fixture.Beast.TryResolvePending(0.0f), Is.True);
            Assert.That(fixture.Hits.Requests, Is.Empty);
            Assert.That(fixture.Triggers.HasPending(SynergyActivationIds.BeastHunt), Is.False);
        }

        [Test]
        public void ReinforcedSquad_UsesLowestLivingFormationSlot_AndDownFallsBack()
        {
            using BeastFixture fixture = new();
            fixture.RecruitBeastSquads(reinforceWolf: true);
            CompanionRuntime wolfA = fixture.FindCompanion("wolf_tamer", 0);
            CompanionRuntime wolfB = fixture.FindCompanion("wolf_tamer", 1);
            wolfA.SetFormationSlot("slot_z");
            wolfB.SetFormationSlot("slot_a");
            wolfA.transform.position = new Vector3(-2.0f, 0.0f);
            wolfB.transform.position = new Vector3(-1.0f, 0.0f);
            fixture.CreateEnemy(Vector3.zero, 100, false, false, false);

            fixture.ResolveAndHit(0.0f);
            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(2));
            Assert.That(fixture.Hits.Requests.Exists(request => request.Origin == wolfB.transform.position), Is.True);
            Assert.That(fixture.Hits.Requests.Exists(request => request.Origin == wolfA.transform.position), Is.False);

            fixture.Hits.Requests.Clear();
            IsDown.SetValue(wolfB, true);
            fixture.Triggers.Tick(10.0f, true, false, 1);
            fixture.ResolveAndHit(10.0f);
            Assert.That(fixture.Hits.Requests.Exists(request => request.Origin == wolfA.transform.position), Is.True);
        }

        [Test]
        public void CommonRangePriorityLockAndDeathCancellation_AreDeterministic()
        {
            using BeastFixture fixture = new();
            fixture.RecruitBeastSquads(reinforceWolf: false);
            CompanionRuntime wolf = fixture.FindCompanion("wolf_tamer", 0);
            CompanionRuntime falcon = fixture.FindCompanion("falcon_archer", 0);
            wolf.transform.position = new Vector3(-1.0f, 0.0f);
            falcon.transform.position = new Vector3(1.0f, 0.0f);
            MonsterController outsideIntersection = fixture.CreateEnemy(new Vector3(8.0f, 0.0f), 100, true, false, false);
            MonsterController elite = fixture.CreateEnemy(Vector3.zero, 100, false, true, false);
            MonsterController boss = fixture.CreateEnemy(new Vector3(0.4f, 0.0f), 1000, true, false, false);

            fixture.Hits.OnApplied = request =>
            {
                if (ReferenceEquals(request.Target, boss))
                    boss.Hp = 0;
            }
            ;
            fixture.ResolveAndHit(0.0f);

            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(1));
            Assert.That(fixture.Hits.Requests[0].Target, Is.SameAs(boss));
            Assert.That(fixture.Hits.Requests.Exists(request => ReferenceEquals(request.Target, elite)), Is.False);
            Assert.That(fixture.Hits.Requests.Exists(request => ReferenceEquals(request.Target, outsideIntersection)), Is.False);
            Assert.That(fixture.Beast.PendingHitCount, Is.Zero);
        }

        [Test]
        public void HitAndBleed_UseCanonicalDamageCapsRefreshAndImmuneExclusion()
        {
            using BeastFixture fixture = new();
            fixture.RecruitBeastSquads(reinforceWolf: false);
            MonsterController boss = fixture.CreateEnemy(Vector3.zero, 1000, true, false, false);
            fixture.ResolveAndHit(0.0f);

            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(2));
            Assert.That(fixture.Hits.Requests.TrueForAll(request => request.Damage == 4), Is.True);
            Assert.That(fixture.Hits.Requests.TrueForAll(request => request.SourceId == SynergyActivationIds.BeastHunt), Is.True);
            Assert.That(fixture.Hits.Requests.TrueForAll(request => request.KillAttribution.Category == CombatKillSourceCategory.SynergyAction), Is.True);
            Assert.That(fixture.Beast.ActiveBleedCount, Is.EqualTo(1));

            fixture.Hits.Requests.Clear();
            const float bleedAppliedAt = 0.3f;
            for (int tick = 1;
            tick <= 5;
            tick++) fixture.Beast.Tick(bleedAppliedAt + tick);
            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(5));
            Assert.That(fixture.Hits.Requests.TrueForAll(request => request.Damage == 1), Is.True);

            fixture.Hits.Requests.Clear();
            fixture.Beast.Reset();
            boss.ConfigureBleedImmunityForRuntime(true);
            fixture.Activations.Refresh(CreateSlots("wolf_tamer", "falcon_archer"));
            fixture.Triggers.Tick(10.0f, true, false, 1);
            fixture.ResolveAndHit(10.0f);
            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(2));
            Assert.That(fixture.Beast.ActiveBleedCount, Is.Zero);
        }

        [Test]
        public void LateTick_CatchesUpAllDueBleedTicksOnceAndExpires()
        {
            using BeastFixture fixture = new();
            fixture.RecruitBeastSquads(reinforceWolf: false);
            fixture.CreateEnemy(Vector3.zero, 1000, true, false, false);
            fixture.ResolveAndHit(0.0f);

            fixture.Hits.Requests.Clear();
            fixture.Beast.Tick(5.31f);

            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(5));
            Assert.That(fixture.Hits.Requests.TrueForAll(request => request.Damage == 1), Is.True);
            Assert.That(fixture.Beast.ActiveBleedCount, Is.Zero);

            fixture.Beast.Tick(6.0f);
            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(5));
            Assert.That(fixture.Beast.ActiveBleedCount, Is.Zero);
        }

        [Test]
        public void ResetAndDispose_ClearPendingDashBleedAndSubscriptionState()
        {
            using BeastFixture fixture = new();
            fixture.RecruitBeastSquads(reinforceWolf: false);
            fixture.CreateEnemy(Vector3.zero, 1000, true, false, false);
            Assert.That(fixture.Beast.TryResolvePending(0.0f), Is.True);
            Assert.That(fixture.Beast.PendingHitCount, Is.EqualTo(2));

            fixture.Beast.Reset();
            fixture.Triggers.Reset();
            fixture.Beast.Dispose();
            fixture.Triggers.Tick(10.0f, true, false, 2);

            Assert.That(fixture.Beast.PendingHitCount, Is.Zero);
            Assert.That(fixture.Beast.ActiveBleedCount, Is.Zero);
            Assert.That(fixture.Triggers.HasPending(SynergyActivationIds.BeastHunt), Is.False);
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[baseUnitIds.Length];
            for (int index = 0;
            index < baseUnitIds.Length;
            index++)
                slots[index] = new SquadSlotState($"squad_{index:00}", baseUnitIds[index], string.Empty, 1, 3, false, baseUnitIds[index]);
            return slots;
        }

        sealed class BeastFixture : IDisposable
        {
            readonly List<GameObject> _enemies = new();
            readonly GameObject _root = new("SynergyBeastFixture");
            readonly TestAssetService _assets = new();
            public readonly LocalDataProvider Data;
            public readonly CanonicalTargetAreaRuntimeLifecycleTests.TestPrefabFactory Factory = new();
            public readonly AppServices App;
            public readonly RunServices Run;
            public readonly RecordingHits Hits = new();
            public readonly BeastHuntSynergy Beast;

            public BeastFixture()
            {
                _assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml"));
                Data = new LocalDataProvider(_assets);
                Assert.That(Data.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
                App = new AppServices(_assets, Data);
                Run = new RunServices(App, new Lizzo.PV.Flow.RunState(), new RuntimeObjectRegistry(Factory),
                    new ObjectPoolService(new GameObject("SynergyBeastPool").transform), Factory);
                RetroSfx.Configure(_assets);
                RetroVfx.Configure(_assets, Factory);
                AttackVisual.Configure(Factory);
                FloatingDamageText.Configure(Factory);
                PlayerController player = _root.AddComponent<PlayerController>();
                player.MaxHp = 100;
                player.Hp = 100;
                Run.Registry.RegisterPlayer(player);
                Beast = new BeastHuntSynergy(Data, Run.Synergies, Run.SynergyTriggers, Run.Party, Run.Registry, Hits, null);
            }

            public PartyService Party => Run.Party;
            public SynergyActivationState Activations => Run.Synergies;
            public SynergyTriggerState Triggers => Run.SynergyTriggers;

            public void RecruitBeastSquads(bool reinforceWolf)
            {
                Assert.That(Party.RecruitCanonical("wolf_tamer"), Is.True);
                if (reinforceWolf) Assert.That(Party.RecruitCanonical("wolf_tamer"), Is.True);
                Assert.That(Party.RecruitCanonical("falcon_archer"), Is.True);
                Assert.That(Activations.IsActive(SynergyActivationIds.BeastHunt), Is.True);
                Assert.That(Triggers.HasPending(SynergyActivationIds.BeastHunt), Is.True);
            }

            public CompanionRuntime FindCompanion(string baseUnitId, int occurrence)
            {
                int found = 0;
                foreach (GameObject instance in Factory.LiveInstances)
                {
                    CompanionRuntime runtime = instance == null ? null : instance.GetComponent<CompanionRuntime>();
                    if (runtime == null) continue;
                    if (runtime.BaseUnitId != baseUnitId) continue;
                    if (found++ == occurrence) return runtime;
                }
                Assert.Fail($"Missing companion {baseUnitId} at {occurrence}.");
                return null;
            }

            public void ResolveAndHit(float now)
            {
                Assert.That(Beast.TryResolvePending(now), Is.True);
                Beast.Tick(now + 0.3f);
                Beast.Tick(now + 0.6f);
            }

            public MonsterController CreateEnemy(Vector3 position, int hp, bool boss, bool elite, bool bleedImmune)
            {
                GameObject instance = new("BeastTarget");
                _enemies.Add(instance);
                instance.SetActive(false);
                instance.transform.position = position;
                if (boss) instance.AddComponent<HungryGiantBehaviour>();
                if (elite) instance.AddComponent<RedChargerBehaviour>();
                instance.AddComponent<Rigidbody2D>();
                CircleCollider2D body = instance.AddComponent<CircleCollider2D>();
                body.isTrigger = false;
                CircleCollider2D combat = instance.AddComponent<CircleCollider2D>();
                combat.isTrigger = true;
                UnitColliderRefs refs = instance.AddComponent<UnitColliderRefs>();
                typeof(UnitColliderRefs).GetField("_bodyCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(refs, body);
                typeof(UnitColliderRefs).GetField("_combatCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(refs, combat);
                instance.AddComponent<EnemyHealthBar>();
                instance.AddComponent<HitFlash>();
                instance.AddComponent<UnitVisualDriver>();
                instance.AddComponent<PatternEnemyVisual>();
                MonsterController target = instance.AddComponent<MonsterController>();
                instance.SetActive(true);
                target.Init();
                target.MaxHp = hp;
                target.Hp = hp;
                target.ConfigureBleedImmunityForRuntime(bleedImmune);
                Run.Registry.RegisterEnemy(target);
                return target;
            }

            public void Dispose()
            {
                Beast.Dispose();
                for (int index = _enemies.Count - 1;
                index >= 0;
                index--)
                    if (_enemies[index] != null) UnityEngine.Object.DestroyImmediate(_enemies[index]);
                Run.Dispose();
                FloatingDamageText.ClearServices();
                AttackVisual.ClearServices();
                RetroVfx.ClearServices();
                RetroSfx.ClearServices();
                App.ReleaseAll();
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        sealed class RecordingHits : ICombatImmediateHitModule
        {
            public readonly List<CombatImmediateHitRequest> Requests = new();
            public Action<CombatImmediateHitRequest> OnApplied;

            public bool TryApply(in CombatImmediateHitRequest request)
            {
                Requests.Add(request);
                OnApplied?.Invoke(request);
                return true;
            }
        }
    }
}
