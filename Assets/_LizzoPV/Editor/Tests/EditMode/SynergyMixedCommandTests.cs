using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyMixedCommandTests
    {
        [Test]
        public void Core_RequiresCanonicalTypedEffectDataAndTargetsOnlyEligibleCompanions()
        {
            using CoreFixture fixture = new CoreFixture();
            SynergyEffectData effect = fixture.Data.GetSynergyEffect("EFFECT_MIXED_COMMAND");
            Assert.IsNotNull(effect);
            Assert.AreEqual("synergy_mixed_command", effect.SynergyId);
            Assert.AreEqual(15.0f, effect.CadenceSeconds);
            Assert.AreEqual(5.0f, effect.DurationSeconds);
            Assert.AreEqual(1.15f, effect.AttackIntervalDivisor);
            Assert.AreEqual(1.15f, effect.MoveSpeedMultiplier);
            Assert.IsTrue(effect.AllAliveCompanions);
            Assert.IsTrue(effect.CompanionsOnly);
            Assert.IsTrue(effect.ExcludesCompanionTagFalseSummons);
            Assert.Throws<InvalidOperationException>(() => new MixedCommandSynergy(null, fixture.Triggers, fixture.World));

            TestCompanion living = fixture.Add("living");
            TestCompanion downed = fixture.Add("downed", false);
            TestCompanion commander = fixture.Add("commander", true, true);
            TestCompanion summon = fixture.Add("summon", true, false, false);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            Assert.AreEqual(1, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(5.0f, fixture.Synergy.ExpiresAt);
            Assert.AreEqual(1.15f, fixture.Synergy.GetAttackIntervalDivisor(living));
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(downed));
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(commander));
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(summon));
        }

        [Test]
        public void Core_TickRemovesDownedTargetsAndTimedRoundsRefreshWithoutStacking()
        {
            using CoreFixture fixture = new CoreFixture();
            TestCompanion target = fixture.Add("target");
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            target.IsLiving = false;
            fixture.Synergy.Tick(1.0f);
            Assert.AreEqual(0, fixture.Synergy.ActiveTargetCount);
            target.IsLiving = true;
            fixture.Synergy.Tick(2.0f);
            fixture.Triggers.Tick(15.0f, true, false, 1);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(15.0f));
            Assert.AreEqual(1.15f, fixture.Synergy.GetMoveSpeedMultiplier(target));
            fixture.Synergy.Tick(20.0f);
            Assert.AreEqual(0, fixture.Synergy.ActiveTargetCount);

            TestCompanion second = fixture.Add("second");
            fixture.Triggers.Tick(15.0f, true, false, 2);
            Assert.IsTrue(fixture.Synergy.TryResolvePending(30.0f));
            Assert.AreEqual(2, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(1.15f, fixture.Synergy.GetAttackIntervalDivisor(second));
        }

        [Test]
        public void Core_ResetAndDisposeClearTargetsAndRemainNeutral()
        {
            using CoreFixture fixture = new CoreFixture();
            TestCompanion target = fixture.Add("target");
            Assert.IsTrue(fixture.Synergy.TryResolvePending(0.0f));
            fixture.Synergy.Reset();
            Assert.AreEqual(0, fixture.Synergy.ActiveTargetCount);
            Assert.AreEqual(1.0f, fixture.Synergy.GetAttackIntervalDivisor(target));
            fixture.Synergy.Dispose();
            fixture.Synergy.Dispose();
            Assert.IsFalse(fixture.Synergy.TryResolvePending(15.0f));
            Assert.AreEqual(1.0f, fixture.Synergy.GetMoveSpeedMultiplier(target));
        }

        [Test]
        public void AbilityCadence_DivisorScalesOnlySuccessfulPeriodAndKeepsNeutralCompatibility()
        {
            CombatAbilitySchedule schedule = new CombatAbilitySchedule();
            schedule.Configure(2.30f, 0.20f, 0.0f, 0.0f);
            MethodInfo record = typeof(CombatAbilitySchedule).GetMethod("RecordResolution", new[] {
                typeof(float), typeof(bool), typeof(float) }
            );
            Assert.IsNotNull(record);
            record.Invoke(schedule, new object[] {
                10.0f, true, 1.15f }
            );
            Assert.AreEqual(12.00f, schedule.NextDueTime, 0.0001f);
            record.Invoke(schedule, new object[] {
                10.0f, false, 1.15f }
            );
            Assert.AreEqual(10.20f, schedule.NextDueTime, 0.0001f);
            schedule.RecordResolution(10.0f, true);
            Assert.AreEqual(12.30f, schedule.NextDueTime, 0.0001f);
        }

        [Test]
        public void TargetAreaCadence_DivisorChangesOnlySuccessfulImpact()
        {
            TargetAreaCastState state = new TargetAreaCastState();
            state.Configure(new CompanionTargetAreaCombatSetup("test", 1, 2.30f, 5.0f, 1.0f, 1, 0.10f, 0.20f), 0.0f, 0.0f);
            state.RecordNoTarget(0.0f);
            Assert.AreEqual(0.20f, state.NextTargetDueTime, 0.0001f);
            Assert.IsTrue(state.TryBeginCast(0.20f, Vector3.right));
            Assert.IsFalse(state.TryConsumeImpact(0.29f, out _));
            MethodInfo divisor = typeof(TargetAreaCastState).GetMethod("TryConsumeImpact", new[] {
                typeof(float), typeof(float), typeof(Vector3).MakeByRefType() }
            );
            Assert.IsNotNull(divisor);
            object[] args = {
                0.30f, 1.15f, Vector3.zero }
            ;
            Assert.IsTrue((bool)divisor.Invoke(state, args));
            Assert.AreEqual(2.30f, state.NextTargetDueTime, 0.0001f);
        }

        [Test]
        public void WolfCadence_UsesMixedCommandDividedPeriodAfterNoTargetRetry()
        {
            FieldInfo activeProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
            PresentationCatalogProvider previous = activeProvider.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset");
            OwnedSupportPresentationSet supports = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/OwnedSupportPresentationSet.asset");
            PresentationCatalog catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            GameObject providerRoot = new GameObject("MixedCommandWolfCadenceCatalog");
            providerRoot.SetActive(false);
            GameObject enemy = null;
            try
            {
                catalog.SetPresentationSetsForEditor(null, null, units, supports);
                PresentationCatalogProvider provider = providerRoot.AddComponent<PresentationCatalogProvider>();
                SerializedObject serialized = new SerializedObject(provider);
                serialized.FindProperty("_catalog").objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                activeProvider.SetValue(null, provider);

                using CanonicalTargetAreaRuntimeLifecycleTests.Fixture fixture = new CanonicalTargetAreaRuntimeLifecycleTests.Fixture();
                ConfigureMixedCommandFamilies(fixture);
                Assert.IsTrue(fixture.Run.Party.RecruitCanonical("wolf_tamer"));
                CompanionRuntime runtime = fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>();
                AllyCombat combat = runtime.GetComponent<AllyCombat>();
                combat.SetCanonicalWolfOwnedProxyInfo(new CompanionWolfOwnedProxyCombatSetup("wolf_tamer", 1, 2.30f, 4.0f, 0.10f, 1, 1, 0.20f, 1, 1.0f));
                MixedCommandRunModule module = new MixedCommandRunModule(fixture.Data, fixture.Run.SynergyTriggers, fixture.Run.Party);
                BindMixedCommand(fixture.Run.Party, module);
                fixture.Run.Synergies.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage"));
                Assert.IsTrue(module.TryResolvePending(0.0f));

                combat.TryAdvanceCanonicalCastForTests(10.0f);
                Assert.IsFalse(combat.HasWolfOwnedProxy);
                combat.TryAdvanceCanonicalCastForTests(10.199f);
                Assert.IsFalse(combat.HasWolfOwnedProxy);
                enemy = AddWolfEnemy(fixture, combat.transform.position + Vector3.right * 0.8f);
                combat.TryAdvanceCanonicalCastForTests(10.2f);
                Assert.IsTrue(combat.HasWolfOwnedProxy);
                combat.TryAdvanceCanonicalCastForTests(10.3f);
                Assert.IsFalse(combat.HasWolfOwnedProxy);
                combat.TryAdvanceCanonicalCastForTests(12.299f);
                Assert.IsFalse(combat.HasWolfOwnedProxy);
                combat.TryAdvanceCanonicalCastForTests(12.3f);
                Assert.IsTrue(combat.HasWolfOwnedProxy);
                UnbindMixedCommand(fixture.Run.Party, module);
                module.Dispose();
            }
            finally
            {
                if (enemy != null)
                {
                    UnityEngine.Object.DestroyImmediate(enemy);
                }
                UnityEngine.Object.DestroyImmediate(providerRoot);
                UnityEngine.Object.DestroyImmediate(catalog);
                activeProvider.SetValue(null, previous);
            }
        }

        static void ConfigureMixedCommandFamilies(CanonicalTargetAreaRuntimeLifecycleTests.Fixture fixture)
        {
            fixture.Data.GetCompanionRoster("shield_guard").FamilyTags = "shield_family,defense_family";
            fixture.Data.GetCompanionRoster("sword_soldier").FamilyTags = "sword_family,melee_family";
            fixture.Data.GetCompanionRoster("cleric").FamilyTags = "cleric_family,healing_family";
            fixture.Data.GetCompanionRoster("falcon_archer").FamilyTags = "ranged_family,beast_family";
            fixture.Data.GetCompanionRoster("fire_mage").FamilyTags = "magic_family,explosive_family";
        }

        static GameObject AddWolfEnemy(CanonicalTargetAreaRuntimeLifecycleTests.Fixture fixture, Vector3 position)
        {
            GameObject enemy = new GameObject("MixedCommandWolfEnemy");
            enemy.transform.position = position;
            MonsterController monster = enemy.AddComponent<MonsterController>();
            EnemyHealthBar health = enemy.AddComponent<EnemyHealthBar>();
            HitFlash flash = enemy.AddComponent<HitFlash>();
            typeof(MonsterController).GetField("_healthBar", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(monster, health);
            typeof(MonsterController).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(monster, flash);
            monster.MaxHp = 100;
            monster.Hp = 100;
            fixture.Run.Registry.RegisterEnemy(monster);
            return enemy;
        }

        static void BindMixedCommand(PartyService party, MixedCommandRunModule module)
        {
            typeof(PartyService).GetMethod("BindMixedCommandRunModule", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(party, new object[] {
                module }
            );
        }

        static void UnbindMixedCommand(PartyService party, MixedCommandRunModule module)
        {
            typeof(PartyService).GetMethod("UnbindMixedCommandRunModule", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(party, new object[] {
                module }
            );
        }

        [Test]
        public void Runtime_UsesLivingCompanionForAttackAndMovementMultipliers()
        {
            using RunFixture fixture = new RunFixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 40);
            MixedCommandRunModule module = fixture.CreateModule();
            PartyAccess.Bind(fixture.Party, module);
            fixture.ActivateMixedCommand();
            Assert.IsTrue(module.TryResolvePending(0.0f));
            Assert.AreEqual(1, module.ActiveTargetCount);
            Assert.AreEqual(1.15f, PartyAccess.AttackDivisor(fixture.Party, companion));
            Assert.AreEqual(1.15f, PartyAccess.MoveMultiplier(fixture.Party, companion));

            SetProperty(companion, "IsDown", true);
            module.Tick(1.0f);
            Assert.AreEqual(1.0f, PartyAccess.AttackDivisor(fixture.Party, companion));
            SetProperty(companion, "IsDown", false);
            fixture.Triggers.Tick(15.0f, true, false, 1);
            Assert.IsTrue(module.TryResolvePending(15.0f));
            Assert.AreEqual(1.15f, PartyAccess.MoveMultiplier(fixture.Party, companion));
        }

        [Test]
        public void Runtime_BindingReplacementResetDisposeAndUnbindLeaveOnlyCurrentModuleQueryable()
        {
            using RunFixture fixture = new RunFixture();
            CompanionRuntime companion = fixture.AddCompanion("shield_guard", 40);
            MixedCommandRunModule older = fixture.CreateModule();
            MixedCommandRunModule current = fixture.CreateModule();
            PartyAccess.Bind(fixture.Party, older);
            PartyAccess.Bind(fixture.Party, current);
            PartyAccess.Unbind(fixture.Party, older);
            fixture.ActivateMixedCommand();
            Assert.IsTrue(current.TryResolvePending(0.0f));
            Assert.AreEqual(1.15f, PartyAccess.AttackDivisor(fixture.Party, companion));
            current.Reset();
            Assert.AreEqual(1.0f, PartyAccess.MoveMultiplier(fixture.Party, companion));
            current.Dispose();
            PartyAccess.Unbind(fixture.Party, current);
            Assert.AreEqual(1.0f, PartyAccess.AttackDivisor(fixture.Party, companion));
        }

        [Test]
        public void Runtime_FollowerMovementOnlyChangesEligibleLiveCompanion()
        {
            using RunFixture fixture = new RunFixture();
            FollowerSetup neutral = fixture.CreateFollower("shield_guard", 0);
            FollowerSetup active = fixture.CreateFollower("sword_soldier", 1);
            FollowerSetup down = fixture.CreateFollower("cleric", 2);
            FollowerSetup external = fixture.CreateFollower("falcon_archer", 3);
            Assert.AreEqual(0.20f, fixture.MeasureOneFixedStep(neutral), 0.0001f);
            MixedCommandRunModule module = fixture.CreateModule();
            PartyAccess.Bind(fixture.Party, module);
            fixture.ActivateMixedCommand();
            Assert.IsTrue(module.TryResolvePending(0.0f));
            Assert.AreEqual(0.23f, fixture.MeasureOneFixedStep(active), 0.0001f);
            SetProperty(down.Companion, "IsDown", true);
            module.Tick(0.1f);
            Assert.AreEqual(0.20f, fixture.MeasureOneFixedStep(down), 0.0001f);
            SetExternalMovement(external.Follower, true);
            Assert.AreEqual(0.00f, fixture.MeasureOneFixedStep(external), 0.0001f);
        }

        sealed class CoreFixture : IDisposable
        {
            readonly SynergyActivationState _activations;
            public readonly LocalDataProvider Data;
            public readonly SynergyTriggerState Triggers;
            public readonly TestWorld World;
            public readonly MixedCommandSynergy Synergy;
            public CoreFixture()
            {
                TestAssetService assets = new TestAssetService();
                TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
                Assert.IsNotNull(gameData);
                assets.Register("PlayerData.xml", gameData);
                Data = new LocalDataProvider(assets);
                Assert.IsTrue(Data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
                _activations = new SynergyActivationState(Data);
                Triggers = new SynergyTriggerState(_activations);
                _activations.Refresh(CreateSlots("shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage"));
                World = new TestWorld();
                Synergy = new MixedCommandSynergy(Data, Triggers, World);
            }
            public TestCompanion Add(string id, bool living = true, bool commander = false, bool tag = true)
            {
                TestCompanion value = new TestCompanion(id, living, commander, tag);
                World.Companions.Add(value);
                return value;
            }
            public void Dispose(){
                Synergy.Dispose();
                Triggers.Dispose();
                _activations.Dispose();
            }
        }
        sealed class TestWorld : MixedCommandSynergy.IWorld
        {
            public readonly List<TestCompanion> Companions = new List<TestCompanion>();
            public void CollectCompanions(List<MixedCommandSynergy.ICompanion> results){
                results.Clear();
                for (int i = 0;
                i < Companions.Count;
                i++)
                {
                    results.Add(Companions[i]);
                }
            }
        }
        sealed class TestCompanion : MixedCommandSynergy.ICompanion
        {
            public TestCompanion(string id, bool living, bool commander, bool tag){
                StableIdentity = id;
                IsLiving = living;
                IsCommander = commander;
                HasCompanionTag = tag;
            }
        public string StableIdentity{
            get;
        }
        public bool IsLiving{
            get;
            set;
        }
        public bool IsCommander{
            get;
        }
        public bool HasCompanionTag{
            get;
        }
        }

        sealed class RunFixture : IDisposable
        {
            readonly ServiceTestFixture _services = new ServiceTestFixture();
            readonly List<GameObject> _objects = new List<GameObject>();
            readonly List<MixedCommandRunModule> _modules = new List<MixedCommandRunModule>();
            readonly ClericHealTestVisualFactory _visualFactory = new ClericHealTestVisualFactory();
            readonly SimulationMode2D _previousSimulationMode;
            public RunFixture(){
                LogAssert.ignoreFailingMessages = true;
                _previousSimulationMode = Physics2D.simulationMode;
                Physics2D.simulationMode = SimulationMode2D.Script;
                ConfigureFamilies();
                AttackVisual.Configure(_visualFactory);
                FloatingDamageText.Configure(_visualFactory);
                RetroVfx.Configure(_services.App.Assets, _services.Run.Factory);
            }
            public PartyService Party => _services.Run.Party;
            public SynergyTriggerState Triggers => _services.Run.SynergyTriggers;
            public MixedCommandRunModule CreateModule(){
                MixedCommandRunModule m = new MixedCommandRunModule(_services.Data, Triggers, Party);
                _modules.Add(m);
                return m;
            }
            public CompanionRuntime AddCompanion(string id, int hp){
                GameObject o = Track(new GameObject(id));
                CircleCollider2D body = o.AddComponent<CircleCollider2D>();
                CircleCollider2D combat = o.AddComponent<CircleCollider2D>();
                combat.isTrigger = true;
                o.AddComponent<Rigidbody2D>();
                o.AddComponent<AllyCombat>();
                o.AddComponent<HitFlash>();
                o.AddComponent<CompanionHealthBar>();
                CreateUi(o);
                CompanionRuntime c = o.AddComponent<CompanionRuntime>();
                SetField(c,
                    "_bodyCollider", body);
                    SetField(c,"_combatCollider", combat);
                    c.Configure(Party, _services.Data.GetUnit(id), id, false);
                    SetProperty(c,"Hp", hp);
                    GetCompanions(Party).Add(c);
                    return c;
                    }
            public FollowerSetup CreateFollower(string id, int index){
                GameObject o = Track(new GameObject(id));
                Rigidbody2D body = o.AddComponent<Rigidbody2D>();
                body.gravityScale = 0;
                Vector2 start = new Vector2(20, 10 + index * 3);
                body.position = start;
                CircleCollider2D bodyCollider = o.AddComponent<CircleCollider2D>();
                CircleCollider2D combat = o.AddComponent<CircleCollider2D>();
                combat.isTrigger = true;
                o.AddComponent<AllyCombat>();
                o.AddComponent<HitFlash>();
                o.AddComponent<CompanionHealthBar>();
                CreateUi(o);
                CompanionRuntime c = o.AddComponent<CompanionRuntime>();
                SetField(c,
                    "_bodyCollider", bodyCollider);
                    SetField(c,"_combatCollider", combat);
                    c.Configure(Party, _services.Data.GetUnit(id), id, false);
                    SetProperty(c,"Hp", 40);
                    GetCompanions(Party).Add(c);
                    AllyFollower f = o.AddComponent<AllyFollower>();
                    f.BindParty(Party);
                    GameObject target = Track(new GameObject(id+"_target"));
                    target.transform.position = new Vector3(30, start.y);
                    f.SetTarget(target.transform, Vector3.zero, 1);
                    Invoke(f,"OnEnable");
                    return new FollowerSetup(c, f, body, start.x);
                    }
            public float MeasureOneFixedStep(FollowerSetup setup){
                Physics2D.SyncTransforms();
                Invoke(setup.Follower,"FixedUpdate");
                Physics2D.Simulate(Time.fixedDeltaTime);
                return setup.Body.position.x-setup.StartX;
                }
            public void ActivateMixedCommand(){
                _services.Run.Synergies.Refresh(CreateSlots("shield_guard","sword_soldier","cleric","falcon_archer","fire_mage"));
                Assert.IsTrue(_services.Run.Synergies.IsActive(SynergyActivationIds.MixedCommand));
                }
            public void Dispose(){
                for (int i = _modules.Count-1;
                i >= 0;
                i--){
                    PartyAccess.Unbind(Party, _modules[i]);
                    _modules[i].Dispose();
                }
            for (int i = _objects.Count-1;
            i >= 0;
            i--)UnityEngine.Object.DestroyImmediate(_objects[i]);
            _services.Dispose();
            Physics2D.simulationMode = _previousSimulationMode;
            AttackVisual.ClearServices();
            FloatingDamageText.ClearServices();
            RetroVfx.ClearServices();
            _visualFactory.Clear();
            LogAssert.ignoreFailingMessages = false;
            }
            GameObject Track(GameObject o){
                _objects.Add(o);
                return o;
            }
            void ConfigureFamilies(){
                _services.Data.GetCompanionRoster("shield_guard").FamilyTags = "shield_family,defense_family";
                _services.Data.GetCompanionRoster("sword_soldier").FamilyTags = "sword_family,melee_family";
                _services.Data.GetCompanionRoster("cleric").FamilyTags = "cleric_family,healing_family";
                _services.Data.GetCompanionRoster("falcon_archer").FamilyTags = "ranged_family,beast_family";
                _services.Data.GetCompanionRoster("fire_mage").FamilyTags = "magic_family,explosive_family";
                }
        }
        readonly struct FollowerSetup{
            public FollowerSetup(CompanionRuntime c, AllyFollower f, Rigidbody2D b, float x){
                Companion = c;
                Follower = f;
                Body = b;
                StartX = x;
            }
        public CompanionRuntime Companion{
            get;
        }
        public AllyFollower Follower{
            get;
        }
        public Rigidbody2D Body{
            get;
        }
        public float StartX{
            get;
        }
        }
        static class PartyAccess{
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            public static void Bind(PartyService p, MixedCommandRunModule m){
                Invoke(p,
                    "BindMixedCommandRunModule", m);
                    }
                    public static void Unbind(PartyService p, MixedCommandRunModule m){
                        Invoke(p,"UnbindMixedCommandRunModule", m);
                    }
                    public static float AttackDivisor(PartyService p, CompanionRuntime c){
                        return(float)Invoke(p,"ResolveCompanionAttackIntervalDivisor", c);
                    }
                    public static float MoveMultiplier(PartyService p, CompanionRuntime c){
                        return(float)Invoke(p,"ResolveCompanionMoveSpeedMultiplier", c);
                    }
                    static object Invoke(PartyService p, string n, params object[] a){
                        MethodInfo m = typeof(PartyService).GetMethod(n, Flags);
                        Assert.IsNotNull(m, n);
                        return m.Invoke(p, a);
                    }
                    }
        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] ids){
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int i = 0;
            i < slots.Length;
            i++)
            {
                string id = i < ids.Length ? ids[i] : string.Empty;
                bool active = string.IsNullOrEmpty(id) == false;
                slots[i] = new SquadSlotState($"squad_{i:00}", id, string.Empty, active ? 1 : 0, 3, false, id);
                }
                return Array.AsReadOnly(slots);
                }
        static List<CompanionRuntime> GetCompanions(PartyService p){
            return(List<CompanionRuntime>)typeof(PartyService).GetField("Companions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(p);
            }
        static void CreateUi(GameObject o){
            Transform ui = new GameObject("UI").transform;
            ui.SetParent(o.transform, false);
            Transform a = new GameObject("HpBarAnchor").transform;
            a.SetParent(ui, false);
            GameObject d = new GameObject("P0_DownMarker");
            d.transform.SetParent(a, false);
            d.AddComponent<TextMeshPro>();
            GameObject bar = new GameObject("P0_CompanionHPBar");
            bar.transform.SetParent(a, false);
            CreateBar(bar.transform,"Back");
            CreateBar(bar.transform,"Fill");
            GameObject t = new GameObject("Text");
            t.transform.SetParent(bar.transform, false);
            t.AddComponent<TextMeshPro>();
            }
        static void CreateBar(Transform p, string n){
            GameObject g = new GameObject(n);
            g.transform.SetParent(p, false);
            SpriteRenderer r = g.AddComponent<SpriteRenderer>();
            r.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f));
        }
        static void SetField(object o, string n, object v){
            typeof(CompanionRuntime).GetField(n, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, v);
        }
        static void SetProperty(object o, string n, object v){
            PropertyInfo p = o.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            p.GetSetMethod(true).Invoke(o, new[]{
                v}
            );
        }
        static void Invoke(object o, string n){
            o.GetType().GetMethod(n, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, null);
        }
        static void SetExternalMovement(AllyFollower f, bool active){
            f.GetType().GetMethod("SetSynergyExternalMovement", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[]{
                active}
            );
            }
    }
}
