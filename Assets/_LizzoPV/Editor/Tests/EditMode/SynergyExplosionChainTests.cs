using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
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
    public sealed class SynergyExplosionChainTests
    {
        [Test]
        public void ActiveChain_TriggersOnEighthKill_ConsumesDeathOriginAndRetainsOverflow()
        {
            using ExplosionFixture fixture = new();
            fixture.CreateEnemy(Vector3.right, 100, false);
            fixture.ResolveImmediate();

            for (int index = 1;
            index <= 7;
            index++)
                fixture.ReportEligibleKill(index, Vector3.zero, 1);

            Assert.That(fixture.Triggers.GetCounter(SynergyActivationIds.ExplosionChain), Is.EqualTo(7));
            Assert.That(fixture.Triggers.HasPending(SynergyActivationIds.ExplosionChain), Is.False);

            fixture.ReportEligibleKill(8, Vector3.zero, 1);
            fixture.ReportEligibleKill(9, Vector3.zero, 2);

            Assert.That(fixture.Triggers.HasPending(SynergyActivationIds.ExplosionChain), Is.True);
            Assert.That(fixture.Explosion.TryResolvePending(), Is.True);
            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(1));
            Assert.That(fixture.Triggers.GetCounter(SynergyActivationIds.ExplosionChain), Is.EqualTo(1));

            for (int index = 10;
            index <= 16;
            index++)
                fixture.ReportEligibleKill(index, Vector3.zero, 3);

            Assert.That(fixture.Triggers.HasPending(SynergyActivationIds.ExplosionChain), Is.True);
        }

        [Test]
        public void Resolution_OrdersByDistanceThenSpawnSequence_UsesDataCapDamageAndSynergyAttribution()
        {
            using ExplosionFixture fixture = new();
            fixture.ResolveImmediate();
            MonsterController boss = fixture.CreateEnemy(new Vector3(0.4f, 0.0f), 1000, true);
            fixture.CreateEnemy(new Vector3(0.4f, 0.0f), 100, false);
            for (int index = 0;
            index < 7;
            index++)
                fixture.CreateEnemy(new Vector3(0.6f + index * 0.1f, 0.0f), 100, false);
            MonsterController outside = fixture.CreateEnemy(new Vector3(2.0f, 0.0f), 100, false);
            MonsterController dead = fixture.CreateEnemy(new Vector3(0.2f, 0.0f), 0, false);

            fixture.QueueExplosion(Vector3.zero, 10);

            Assert.That(fixture.Explosion.TryResolvePending(), Is.True);
            Assert.That(fixture.Hits.Requests, Has.Count.EqualTo(8));
            Assert.That(fixture.Hits.Requests.Exists(request => request.Target == outside), Is.False);
            Assert.That(fixture.Hits.Requests.Exists(request => request.Target == dead), Is.False);
            Assert.That(fixture.Hits.Requests.Find(request => request.Target == boss).Damage, Is.EqualTo(8));

            float previousDistance = -1.0f;
            long previousSequence = 0L;
            for (int index = 0;
            index < fixture.Hits.Requests.Count;
            index++)
            {
                CombatImmediateHitRequest request = fixture.Hits.Requests[index];
                MonsterController target = request.Target as MonsterController;
                float distance = target.transform.position.sqrMagnitude;
                if (Mathf.Approximately(distance, previousDistance))
                    Assert.That(target.SpawnSequence, Is.GreaterThan(previousSequence));
                else
                    Assert.That(distance, Is.GreaterThanOrEqualTo(previousDistance));

                Assert.That(request.SourceId, Is.EqualTo(SynergyActivationIds.ExplosionChain));
                Assert.That(request.KillAttribution.Category, Is.EqualTo(CombatKillSourceCategory.SynergyAction));
                Assert.That(request.Damage, Is.EqualTo(target == boss ? 8 : 20));
                previousDistance = distance;
                previousSequence = target.SpawnSequence;
            }
        }

        [Test]
        public void SameResolutionExplosionKills_DoNotAdvanceCounterOrQueueAgain()
        {
            using ExplosionFixture fixture = new();
            fixture.ResolveImmediate();
            fixture.CreateEnemy(Vector3.right, 100, false);
            fixture.Hits.OnApplied = request =>
            {
                MonsterController target = request.Target as MonsterController;
                fixture.State.RegisterCountableKill(request.KillAttribution.WithLethalContext(target.SpawnSequence, target.transform.position, 20));
            }
            ;

            fixture.QueueExplosion(Vector3.zero, 20);

            Assert.That(fixture.Explosion.TryResolvePending(), Is.True);
            Assert.That(fixture.Triggers.GetCounter(SynergyActivationIds.ExplosionChain), Is.Zero);
            Assert.That(fixture.Triggers.HasPending(SynergyActivationIds.ExplosionChain), Is.False);
        }

        [Test]
        public void ResetAndDispose_ClearPendingOriginAndUnsubscribeExactlyOnce()
        {
            using ExplosionFixture fixture = new();
            fixture.ResolveImmediate();
            fixture.QueueExplosion(Vector3.zero, 30);
            fixture.Explosion.Reset();
            fixture.Triggers.Reset();
            fixture.Explosion.Dispose();
            fixture.ReportEligibleKill(31, Vector3.zero, 31);

            Assert.That(fixture.Triggers.GetCounter(SynergyActivationIds.ExplosionChain), Is.Zero);
            Assert.That(fixture.Triggers.HasPending(SynergyActivationIds.ExplosionChain), Is.False);
        }

        sealed class ExplosionFixture : IDisposable
        {
            readonly List<GameObject> _objects = new();
            public readonly RunState State = new();
            public readonly RuntimeObjectRegistry Registry;
            public readonly SynergyActivationState Activations;
            public readonly SynergyTriggerState Triggers;
            public readonly RecordingImmediateHits Hits = new();
            public readonly ExplosionChainSynergy Explosion;

            public ExplosionFixture()
            {
                TestAssetService assets = new();
                assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
                LocalDataProvider data = new(assets);
                Assert.That(data.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
                Registry = new RuntimeObjectRegistry(new NullFactory());
                Activations = new SynergyActivationState(data);
                Activations.Refresh(CreateSlots("bombardier", "fire_mage", "skeleton_bomber"));
                Triggers = new SynergyTriggerState(Activations);
                State.Reset(1);
                State.MarkLoaded();
                Explosion = new ExplosionChainSynergy(data, Activations, Triggers, State, Registry, Hits);
            }

            public void ResolveImmediate() => Assert.That(Explosion.TryResolvePending(), Is.True);

            public void QueueExplosion(Vector3 position, int firstLifeId)
            {
                for (int index = 0;
                index < 8;
                index++)
                    ReportEligibleKill(firstLifeId + index, position, firstLifeId + index);
            }

            public void ReportEligibleKill(long lifeId, Vector3 position, int frameId)
            {
                State.RegisterCountableKill(new CountableKillAttribution(1, "fixture_companion",
                    CombatKillSourceCategory.CompanionOwnedAction).WithLethalContext(lifeId, position, frameId));
            }

            public MonsterController CreateEnemy(Vector3 position, int hp, bool boss)
            {
                GameObject instance = new("ExplosionTarget");
                _objects.Add(instance);
                instance.SetActive(false);
                instance.transform.position = position;
                if (boss) instance.AddComponent<HungryGiantBehaviour>();
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
                Registry.RegisterEnemy(target);
                return target;
            }

            public void Dispose()
            {
                Explosion.Dispose();
                Triggers.Dispose();
                Activations.Dispose();
                for (int index = _objects.Count - 1;
                index >= 0;
                index--)
                    if (_objects[index] != null) UnityEngine.Object.DestroyImmediate(_objects[index]);
            }
        }

        sealed class RecordingImmediateHits : ICombatImmediateHitModule
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

        sealed class NullFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) {
            }
            public void Clear() {
            }
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int index = 0;
            index < slots.Length;
            index++)
            {
                string baseUnitId = index < baseUnitIds.Length ? baseUnitIds[index] : string.Empty;
                slots[index] = new SquadSlotState(
                    $"squad_{index:00}",
                    baseUnitId,
                    string.Empty,
                    string.IsNullOrEmpty(baseUnitId) ? 0 : 1,
                    3,
                    false,
                    baseUnitId);
            }

            return Array.AsReadOnly(slots);
        }
    }
}
