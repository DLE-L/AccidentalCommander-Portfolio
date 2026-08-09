using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyUndeadTests
    {
        [Test]
        public void Core_ImmediateRoundSpawnsTypedActorAtRearFallbackPoint()
        {
            using UndeadFixture fixture = new UndeadFixture();
            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f, 1));
            Assert.AreEqual(1, fixture.Module.ActiveCount);
            Assert.AreEqual(1, fixture.World.Actors.Count);
            Assert.AreEqual(1.0f, fixture.World.LastDesiredRearOffset);
            Assert.AreEqual(1.5f, fixture.World.LastFallbackRadius);
            Assert.AreEqual("UNIT_SYNERGY_SKELETON_01", fixture.World.LastRequest.Data.Id);
            Assert.AreEqual("synergy_undead_summon", fixture.World.LastRequest.Data.SynergyId);
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", fixture.World.LastRequest.Data.DistinctFromSummonId);
            Assert.AreEqual(22, fixture.World.LastRequest.Data.Hp);
            Assert.AreEqual(5, fixture.World.LastRequest.Data.Damage);
            Assert.AreEqual(1.2f, fixture.World.LastRequest.Data.AttackInterval);
            Assert.AreEqual(2.8f, fixture.World.LastRequest.Data.MoveSpeed);
        }

        [Test]
        public void Core_ThresholdOverflowSpawnsOneRoundPerLaterFrameAndClampsAtCap()
        {
            using UndeadFixture fixture = new UndeadFixture();
            fixture.Module.TryResolvePending(0.0f, 1);
            for (int round = 0;
            round < 4;
            round++)
            {
                for (int kill = 0;
                kill < 15;
                kill++)
                {
                    fixture.Triggers.ReportEnemyDeath(Death((round * 15) + kill + 1, (round * 16) + kill + 2));
                }
                Assert.IsTrue(fixture.Module.TryResolvePending(round + 1.0f, (round * 16) + 16));
            }
            Assert.AreEqual(5, fixture.Module.ActiveCount);
            for (int i = 0;
            i < 14;
            i++)
            {
                fixture.Triggers.ReportEnemyDeath(Death(1000 + i, 100 + i));
            }
            Assert.AreEqual(14, fixture.Triggers.GetCounter(SynergyActivationIds.UndeadSummon));
            fixture.World.Actors[0].IsAlive = false;
            fixture.Module.Tick(5.0f);
            Assert.AreEqual(4, fixture.Module.ActiveCount);
            fixture.Triggers.ReportEnemyDeath(Death(2000, 200));
            Assert.IsTrue(fixture.Triggers.HasPending(SynergyActivationIds.UndeadSummon));
        }

        [Test]
        public void Core_MissingSpawnPointConsumesRoundWithoutRetry()
        {
            using UndeadFixture fixture = new UndeadFixture();
            fixture.World.HasSpawnPoint = false;
            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f, 1));
            Assert.AreEqual(0, fixture.Module.ActiveCount);
            Assert.IsFalse(fixture.Triggers.HasPending(SynergyActivationIds.UndeadSummon));
            for (int i = 0;
            i < 15;
            i++)
            {
                fixture.Triggers.ReportEnemyDeath(Death(i + 1, i + 2));
            }
            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f, 16));
            Assert.AreEqual(0, fixture.Module.ActiveCount);
            Assert.AreEqual(0, fixture.Triggers.GetCounter(SynergyActivationIds.UndeadSummon));
        }

        [Test]
        public void Core_TargetingBossLockAndCleanupUseWorldSeam()
        {
            using UndeadFixture fixture = new UndeadFixture();
            fixture.Module.TryResolvePending(0.0f, 1);
            RecordingActor actor = fixture.World.Actors[0];
            RecordingTarget later = new RecordingTarget(8, new Vector3(1.0f, 0.0f), false, true);
            RecordingTarget earlier = new RecordingTarget(2, new Vector3(1.0f, 0.0f), false, true);
            fixture.World.Targets.Add(later);
            fixture.World.Targets.Add(earlier);
            fixture.Module.Tick(0.1f);
            Assert.AreSame(earlier, actor.LastTarget);
            Assert.AreEqual(1, fixture.World.CollectionCount);
            fixture.World.Targets.Clear();
            RecordingTarget replacement = new RecordingTarget(3, new Vector3(0.5f, 0.0f), false, true);
            fixture.World.Targets.Add(replacement);
            fixture.Module.Tick(0.2f);
            Assert.AreSame(earlier, actor.LastTarget);
            Assert.AreEqual(1, fixture.World.CollectionCount);
            fixture.Module.Tick(0.31f);
            Assert.AreSame(replacement, actor.LastTarget);
            Assert.AreEqual(2, fixture.World.CollectionCount);
            fixture.World.Targets.Clear();
            fixture.Module.Tick(0.6f);
            Assert.IsNull(actor.LastTarget);
            Assert.AreEqual(3, fixture.World.CollectionCount);
            RecordingTarget boss = new RecordingTarget(1, new Vector3(9.0f, 0.0f), true, true);
            fixture.World.Targets.Add(replacement);
            fixture.Module.OnBossPhaseStarted(boss, 1.0f);
            fixture.Module.Tick(1.0f);
            Assert.AreSame(boss, actor.LastTarget);
            Assert.AreEqual(3, fixture.World.CollectionCount);
            for (int i = 0;
            i < 15;
            i++)
            {
                fixture.Triggers.ReportEnemyDeath(Death(i + 1, i + 2));
            }
            Assert.IsTrue(fixture.Module.TryResolvePending(1.1f, 2));
            RecordingActor spawnedLater = fixture.World.Actors[1];
            fixture.Module.Tick(1.2f);
            Assert.AreSame(boss, actor.LastTarget);
            Assert.AreSame(replacement, spawnedLater.LastTarget);
            fixture.Module.Tick(3.99f);
            Assert.AreSame(boss, actor.LastTarget);
            int collectionsBeforeBossExpiry = fixture.World.CollectionCount;
            fixture.Module.Tick(4.0f);
            Assert.AreSame(replacement, actor.LastTarget);
            Assert.AreEqual(collectionsBeforeBossExpiry + 1, fixture.World.CollectionCount);
            RecordingTarget untargetableBoss = new RecordingTarget(3, Vector3.zero, true, false);
            fixture.Module.OnBossPhaseStarted(untargetableBoss, 5.0f);
            Assert.AreSame(replacement, actor.LastTarget);
            actor.IsAlive = false;
            fixture.Module.Tick(5.1f);
            Assert.AreEqual(1, fixture.Module.ActiveCount);
            Assert.AreEqual(1, fixture.World.ReleaseCount);
        }

        [Test]
        public void Core_ResetForResultAndDisposeReleaseAllActors()
        {
            UndeadFixture reset = new UndeadFixture();
            reset.Module.TryResolvePending(0.0f, 1);
            reset.Module.ResetForResult();
            Assert.AreEqual(0, reset.Module.ActiveCount);
            Assert.AreEqual(1, reset.World.ReleaseCount);
            reset.Dispose();
            UndeadFixture disposed = new UndeadFixture();
            disposed.Module.TryResolvePending(0.0f, 1);
            disposed.Module.Dispose();
            Assert.AreEqual(0, disposed.Module.ActiveCount);
            Assert.AreEqual(1, disposed.World.ReleaseCount);
            disposed.Dispose();
        }

        [Test]
        public void SkeletonRuntime_UsesCanonicalTypedDataAndExplicitSynergyIdentity()
        {
            using SkeletonFixture fixture = new SkeletonFixture();
            SynergySkeletonRuntime actor = fixture.CreateActor();
            Assert.IsTrue(actor.Configure(fixture.Data, 0.3f, fixture.Hits));
            Assert.AreEqual(22, actor.Hp);
            Assert.AreEqual("synergy_undead_summon", actor.SourceId);
            Assert.IsFalse(actor.HasCompanionTag);
            Assert.IsFalse(actor.HasFamilyTag);
            Assert.IsFalse(actor.HasRosterIdentity);
            Assert.IsFalse(actor.HasPersonalSummonOwner);
        }

        [Test]
        public void SkeletonRuntime_EnemyContactDamageAndReleaseResetClearActorState()
        {
            using SkeletonFixture fixture = new SkeletonFixture();
            SynergySkeletonRuntime actor = fixture.CreateActor();
            Assert.IsTrue(actor.Configure(fixture.Data, 0.0f, fixture.Hits));
            CombatImmediateHitModule hits = new CombatImmediateHitModule();
            Assert.IsFalse(hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget("ally", actor, Vector3.zero, Vector3.zero, 1,
                AttackVisualKind.SingleHit, false)));
            Assert.IsTrue(hits.TryApply(CombatImmediateHitRequest.CreateEnemyContact("enemy", actor, Vector3.zero, Vector3.right, 22, "contact",
                RetroVfxKind.EnemyContactHit)));
            Assert.IsFalse(actor.IsAlive);
            actor.ResetForRelease();
            Assert.IsFalse(actor.IsConfigured);
            Assert.AreEqual(0, actor.Hp);
            Assert.IsNull(actor.Target);
        }

        [Test]
        public void SkeletonRuntime_TickMovesToRangeAndUsesTypedCadenceAttribution()
        {
            using SkeletonFixture fixture = new SkeletonFixture();
            SynergySkeletonRuntime actor = fixture.CreateActor();
            Assert.IsTrue(actor.Configure(fixture.Data, 0.0f, fixture.Hits));
            actor.Tick(0.0f, 0.1f, null);
            RecordingCombatTarget target = new RecordingCombatTarget(new Vector3(1.5f, 0.0f));
            actor.SetTarget(target);
            actor.Tick(0.1f, 0.1f, null);
            Assert.AreEqual(0.28f, actor.transform.position.x, 0.001f);
            actor.Tick(0.2f, 1.0f, null);
            Assert.AreEqual(0.5f, actor.transform.position.x, 0.001f);
            actor.Tick(0.3f, 0.1f, null);
            Assert.AreEqual(1, fixture.Hits.Count);
            Assert.AreEqual(5, fixture.Hits.LastRequest.Damage);
            Assert.AreEqual("synergy_undead_summon", fixture.Hits.LastRequest.SourceId);
            Assert.AreEqual(CombatKillSourceCategory.SynergySummon, fixture.Hits.LastRequest.KillAttribution.Category);
            actor.Tick(1.49f, 0.1f, null);
            Assert.AreEqual(1, fixture.Hits.Count);
            actor.Tick(1.5f, 0.1f, null);
            Assert.AreEqual(2, fixture.Hits.Count);
        }

        [Test]
        public void SkeletonPrefab_UsesIndependentCanonicalRuntimeVisualAndPreloadAddressableSeams()
        {
            const string synergyPath = "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Supports/SynergySkeletonSummon.prefab";
            const string personalPath = "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Supports/PersonalSkeletonSummon.prefab";
            GameObject personal = AssetDatabase.LoadAssetAtPath<GameObject>(personalPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(synergyPath);
            Assert.IsNotNull(personal);
            Assert.IsNotNull(prefab);
            Assert.AreEqual("SynergySkeletonSummon", prefab.name);
            Assert.AreEqual(Vector3.one, prefab.transform.localScale);
            Assert.AreNotEqual(AssetDatabase.AssetPathToGUID(personalPath), AssetDatabase.AssetPathToGUID(synergyPath));
            Assert.AreEqual("cd6e8719e4fff074980cdd6dfe86074d", AssetDatabase.AssetPathToGUID(personalPath));
            Assert.AreEqual("523309a033db6c649a398e7e9cd39876", AssetDatabase.AssetPathToGUID(synergyPath));
            SynergySkeletonRuntime runtime = prefab.GetComponent<SynergySkeletonRuntime>();
            UnitColliderRefs refs = prefab.GetComponent<UnitColliderRefs>();
            Assert.IsNotNull(runtime);
            Assert.IsNotNull(prefab.GetComponent<Rigidbody2D>());
            Assert.IsNotNull(refs);
            Assert.IsNotNull(prefab.GetComponent<HitFlash>());
            Assert.IsNotNull(refs.BodyCollider);
            Assert.IsNotNull(refs.CombatCollider);
            Assert.IsFalse(refs.BodyCollider.isTrigger);
            Assert.IsTrue(refs.CombatCollider.isTrigger);
            Assert.IsNull(prefab.GetComponent<PersonalSummonRuntime>());
            Assert.IsNull(prefab.GetComponent<CompanionRuntime>());
            Transform visual = prefab.transform.Find("Visual");
            Transform personalVisual = personal.transform.Find("Visual");
            Assert.IsNotNull(visual);
            Assert.AreEqual(1, prefab.transform.childCount);
            Assert.IsNotNull(personalVisual);
            Assert.AreEqual(new Vector3(0.30f, 0.30f, 1.0f), visual.localScale);
            Assert.IsNotNull(visual.GetComponent<SpriteRenderer>());
            Assert.IsNotNull(visual.GetComponent<Animator>());
            Assert.IsNotNull(visual.GetComponent<SpriteLibrary>());
            Assert.IsNotNull(visual.GetComponent<SpriteResolver>());
            Assert.IsNotNull(visual.GetComponent<UnitVisualDriver>());
            Assert.AreSame(personalVisual.GetComponent<Animator>().runtimeAnimatorController, visual.GetComponent<Animator>().runtimeAnimatorController);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings);
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(synergyPath));
            Assert.IsNotNull(entry);
            Assert.AreEqual("Lizzo/Characters/Supports/UNIT_SYNERGY_SKELETON_01", entry.address);
            Assert.AreEqual("Prefabs", entry.parentGroup.Name);
            CollectionAssert.AreEquivalent(new[] {
                "Prefab", "PreLoad" }
            , entry.labels);
        }

        static SynergyEnemyDeathEvent Death(long lifeId, int frameId) => new SynergyEnemyDeathEvent(lifeId, SynergyDeathSourceCategory.Commander,
            false, false, false, 0L, frameId);

        sealed class UndeadFixture : IDisposable
        {
            public readonly SynergyTriggerState Triggers;
            public readonly RecordingWorld World;
            public readonly UndeadSummonSynergy Module;
            readonly SynergyActivationState _activations;
            public UndeadFixture()
            {
                LocalDataProvider provider = CreateProjectProvider();
                _activations = new SynergyActivationState(provider);
                Triggers = new SynergyTriggerState(_activations);
                World = new RecordingWorld();
                Module = new UndeadSummonSynergy(Triggers,
                    provider.GetSynergySummon("UNIT_SYNERGY_SKELETON_01"), World);
                    _activations.Refresh(CreateSlots("necromancer", "wraith_knight", "skeleton_bomber"));
            }
            public void Dispose(){
                Module.Dispose();
                Triggers.Dispose();
                _activations.Dispose();
            }
        }
        sealed class RecordingWorld : UndeadSummonSynergy.IWorld
        {
            public readonly List<RecordingActor> Actors = new List<RecordingActor>();
            public readonly List<UndeadSummonSynergy.ITarget> Targets = new List<UndeadSummonSynergy.ITarget>();
            public bool HasSpawnPoint = true;
            public float LastDesiredRearOffset;
            public float LastFallbackRadius;
            public UndeadSummonSynergy.SpawnRequest LastRequest;
            public int ReleaseCount;
            public int CollectionCount;
            public bool TryResolveRearSpawn(float offset, float radius, out Vector3 position){
                LastDesiredRearOffset = offset;
                LastFallbackRadius = radius;
                position = new Vector3(-1, 0);
                return HasSpawnPoint;
            }
            public bool TrySpawn(in UndeadSummonSynergy.SpawnRequest request, out UndeadSummonSynergy.IActor actor){
                LastRequest = request;
                RecordingActor created = new RecordingActor(request.Position);
                Actors.Add(created);
                actor = created;
                return true;
            }
            public void Release(UndeadSummonSynergy.IActor actor){
                ReleaseCount++;
            }
            public void CollectTargets(List<UndeadSummonSynergy.ITarget> destination){
                CollectionCount++;
                destination.AddRange(Targets);
            }
        }
        sealed class RecordingActor : UndeadSummonSynergy.IActor
        {
            public RecordingActor(Vector3 p){
                Position = p;
                IsAlive = true;
            }
        public bool IsAlive{
            get;
            set;
        }
        public Vector3 Position{
            get;
        }
        public UndeadSummonSynergy.ITarget LastTarget{
            get;
            private set;
        }
        public void SetTarget(UndeadSummonSynergy.ITarget target){
            LastTarget = target;
        }
        }
        sealed class RecordingTarget : UndeadSummonSynergy.ITarget
        {
            public RecordingTarget(int id, Vector3 p, bool boss, bool targetable){
                StableIdentity = id;
                Position = p;
                IsBoss = boss;
                IsTargetable = targetable;
            }
        public int StableIdentity{
            get;
        }
        public Vector3 Position{
            get;
        }
        public bool IsBoss{
            get;
        }
        public bool IsTargetable{
            get;
        }
        }

        sealed class SkeletonFixture : IDisposable
        {
            readonly GameObject _root = new GameObject("SynergySkeletonRuntime");
            public readonly SynergySummonData Data;
            public readonly RecordingHits Hits = new RecordingHits();
            public SkeletonFixture(){
                Data = CreateProjectProvider().GetSynergySummon("UNIT_SYNERGY_SKELETON_01");
                }
            public SynergySkeletonRuntime CreateActor(){
                GameObject instance = new GameObject("SynergySkeleton");
                instance.transform.SetParent(_root.transform);
                Rigidbody2D body = instance.AddComponent<Rigidbody2D>();
                BoxCollider2D bodyCollider = instance.AddComponent<BoxCollider2D>();
                BoxCollider2D combatCollider = instance.AddComponent<BoxCollider2D>();
                UnitColliderRefs refs = instance.AddComponent<UnitColliderRefs>();
                HitFlash flash = instance.AddComponent<HitFlash>();
                UnitVisualDriver visual = instance.AddComponent<UnitVisualDriver>();
                SynergySkeletonRuntime actor = instance.AddComponent<SynergySkeletonRuntime>();
                SetField(refs,"_bodyCollider", bodyCollider);
                SetField(refs,"_combatCollider", combatCollider);
                SetField(actor,"_body", body);
                SetField(actor,"_colliders", refs);
                SetField(actor,"_hitFlash", flash);
                SetField(actor,"_visualDriver", visual);
                return actor;
                }
            public void Dispose(){
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }
        sealed class RecordingHits : ICombatImmediateHitModule{
            public int Count;
            public CombatImmediateHitRequest LastRequest;
            public bool TryApply(in CombatImmediateHitRequest request){
                Count++;
                LastRequest = request;
                return true;
            }
        }
        sealed class RecordingCombatTarget : UndeadSummonSynergy.ICombatTarget, ICombatImmediateHitTarget{
            public RecordingCombatTarget(Vector3 p){
                Position = p;
            }
        public Vector3 Position{
            get;
        }
        public int StableIdentity => 1;
        public bool IsBoss => false;
        public bool IsTargetable => true;
        public ICombatImmediateHitTarget CombatTarget => this;
        public CombatImmediateHitFaction Faction => CombatImmediateHitFaction.Enemy;
        public bool IsAlive => true;
        public void ReceiveImmediateHit(in CombatImmediateHitRequest request){
        }
        }

        static LocalDataProvider CreateProjectProvider(){
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return provider;
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
        static void SetField(object target, string name, object value){
            typeof(UnitColliderRefs).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
            typeof(SynergySkeletonRuntime).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
        }
    }
}
