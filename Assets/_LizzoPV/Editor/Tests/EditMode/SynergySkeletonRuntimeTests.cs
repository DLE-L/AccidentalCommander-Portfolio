using System;
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
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergySkeletonRuntimeTests
    {
        [Test]
        public void Configure_UsesCanonicalTypedDataAndExplicitSynergyIdentity()
        {
            using RuntimeFixture fixture = new RuntimeFixture();
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
        public void EnemyContactOnlyDamage_AndResetRelease_ClearActorState()
        {
            using RuntimeFixture fixture = new RuntimeFixture();
            SynergySkeletonRuntime actor = fixture.CreateActor();
            Assert.IsTrue(actor.Configure(fixture.Data, 0.0f, fixture.Hits));

            CombatImmediateHitModule hits = new CombatImmediateHitModule();
            Assert.IsFalse(hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                "ally", actor, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.AreEqual(22, actor.Hp);
            Assert.IsTrue(hits.TryApply(CombatImmediateHitRequest.CreateEnemyContact(
                "enemy", actor, Vector3.zero, Vector3.right, 22, "contact", RetroVfxKind.EnemyContactHit)));
            Assert.IsFalse(actor.IsAlive);

            actor.ResetForRelease();
            Assert.IsFalse(actor.IsConfigured);
            Assert.AreEqual(0, actor.Hp);
            Assert.IsNull(actor.Target);
        }

        [Test]
        public void Tick_IdlesMovesToRangeAndUsesTypedCadenceAttribution()
        {
            using RuntimeFixture fixture = new RuntimeFixture();
            SynergySkeletonRuntime actor = fixture.CreateActor();
            Assert.IsTrue(actor.Configure(fixture.Data, 0.0f, fixture.Hits));

            actor.Tick(0.0f, 0.1f, null);
            Assert.AreEqual(Vector3.zero, actor.transform.position);
            RecordingTarget target = new RecordingTarget(new Vector3(1.5f, 0.0f));
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
            Assert.IsFalse(fixture.Hits.LastRequest.SpawnAllyFeedback);
            actor.Tick(1.49f, 0.1f, null);
            Assert.AreEqual(1, fixture.Hits.Count);
            actor.Tick(1.5f, 0.1f, null);
            Assert.AreEqual(2, fixture.Hits.Count);
        }

        sealed class RuntimeFixture : IDisposable
        {
            readonly GameObject _root = new GameObject("SynergySkeletonRuntimeTests");
            public readonly SynergySummonData Data;
            public readonly RecordingHits Hits = new RecordingHits();

            public RuntimeFixture()
            {
                TestAssetService assets = new TestAssetService();
                TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
                assets.Register("PlayerData.xml", gameData);
                LocalDataProvider provider = new LocalDataProvider(assets);
                Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
                Data = provider.GetSynergySummon("UNIT_SYNERGY_SKELETON_01");
            }

            public SynergySkeletonRuntime CreateActor()
            {
                GameObject instance = new GameObject("SynergySkeleton");
                instance.transform.SetParent(_root.transform);
                Rigidbody2D body = instance.AddComponent<Rigidbody2D>();
                BoxCollider2D bodyCollider = instance.AddComponent<BoxCollider2D>();
                BoxCollider2D combatCollider = instance.AddComponent<BoxCollider2D>();
                UnitColliderRefs colliders = instance.AddComponent<UnitColliderRefs>();
                HitFlash flash = instance.AddComponent<HitFlash>();
                UnitVisualDriver visual = instance.AddComponent<UnitVisualDriver>();
                SynergySkeletonRuntime actor = instance.AddComponent<SynergySkeletonRuntime>();
                SetField(colliders, "_bodyCollider", bodyCollider);
                SetField(colliders, "_combatCollider", combatCollider);
                SetField(actor, "_body", body);
                SetField(actor, "_colliders", colliders);
                SetField(actor, "_hitFlash", flash);
                SetField(actor, "_visualDriver", visual);
                return actor;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        sealed class RecordingHits : ICombatImmediateHitModule
        {
            public int Count;
            public CombatImmediateHitRequest LastRequest;

            public bool TryApply(in CombatImmediateHitRequest request)
            {
                Count++;
                LastRequest = request;
                return true;
            }
        }

        sealed class RecordingTarget : UndeadSummonSynergy.ICombatTarget, ICombatImmediateHitTarget
        {
            public RecordingTarget(Vector3 position)
            {
                Position = position;
            }

            public Vector3 Position { get; }
            public int StableIdentity => 1;
            public bool IsBoss => false;
            public bool IsTargetable => true;
            public ICombatImmediateHitTarget CombatTarget => this;
            public CombatImmediateHitFaction Faction => CombatImmediateHitFaction.Enemy;
            public bool IsAlive => true;

            public void ReceiveImmediateHit(in CombatImmediateHitRequest request)
            {
            }
        }

        static void SetField(object target, string name, object value)
        {
            typeof(UnitColliderRefs).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
            typeof(SynergySkeletonRuntime).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
        }
    }
}
