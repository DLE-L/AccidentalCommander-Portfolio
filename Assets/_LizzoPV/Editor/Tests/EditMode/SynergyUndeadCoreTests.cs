using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyUndeadCoreTests
    {
        [Test]
        public void ImmediateRound_SpawnsOneTypedActorAtRearFallbackPoint()
        {
            using UndeadFixture fixture = new UndeadFixture();

            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f, 1));
            Assert.AreEqual(1, fixture.Module.ActiveCount);
            Assert.AreEqual(1, fixture.World.Actors.Count);
            Assert.AreEqual(1.0f, fixture.World.LastDesiredRearOffset);
            Assert.AreEqual(1.5f, fixture.World.LastFallbackRadius);
            Assert.AreEqual(0.0f, fixture.World.LastRequest.SpawnTime);
            Assert.AreEqual("UNIT_SYNERGY_SKELETON_01", fixture.World.LastRequest.Data.Id);
            Assert.AreEqual("synergy_undead_summon", fixture.World.LastRequest.Data.SynergyId);
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", fixture.World.LastRequest.Data.DistinctFromSummonId);
            Assert.AreEqual(22, fixture.World.LastRequest.Data.Hp);
            Assert.AreEqual(5, fixture.World.LastRequest.Data.Damage);
            Assert.AreEqual(1.2f, fixture.World.LastRequest.Data.AttackInterval);
            Assert.AreEqual(1.0f, fixture.World.LastRequest.Data.Range);
            Assert.AreEqual(2.8f, fixture.World.LastRequest.Data.MoveSpeed);
            Assert.AreEqual(0.2f, fixture.World.LastRequest.Data.AiScanInterval);
        }

        [Test]
        public void ThresholdOverflow_SpawnsOneRoundPerLaterFrame()
        {
            using UndeadFixture fixture = new UndeadFixture();
            fixture.Module.TryResolvePending(0.0f, 1);

            for (int i = 0; i < 30; i++)
                fixture.Triggers.ReportEnemyDeath(Death(i + 1, i + 2));

            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f, 16));
            Assert.AreEqual(2, fixture.Module.ActiveCount);
            Assert.AreEqual(15, fixture.Triggers.GetCounter(SynergyActivationIds.UndeadSummon));
            Assert.IsFalse(fixture.Triggers.HasPending(SynergyActivationIds.UndeadSummon));

            fixture.Triggers.Tick(0.1f, runReady: true, paused: false, frameId: 32);
            Assert.IsTrue(fixture.Triggers.HasPending(SynergyActivationIds.UndeadSummon));
            Assert.IsTrue(fixture.Module.TryResolvePending(2.0f, 32));
            Assert.AreEqual(3, fixture.Module.ActiveCount);
            Assert.AreEqual(0, fixture.Triggers.GetCounter(SynergyActivationIds.UndeadSummon));
        }

        [Test]
        public void FullCap_ClampsCounterAndDeathMakesNextKillEligible()
        {
            using UndeadFixture fixture = new UndeadFixture();
            fixture.Module.TryResolvePending(0.0f, 1);

            for (int round = 0; round < 4; round++)
            {
                for (int kill = 0; kill < 15; kill++)
                    fixture.Triggers.ReportEnemyDeath(Death((round * 15) + kill + 1, (round * 16) + kill + 2));

                Assert.IsTrue(fixture.Module.TryResolvePending(round + 1.0f, (round * 16) + 16));
            }

            Assert.AreEqual(5, fixture.Module.ActiveCount);
            for (int i = 0; i < 14; i++)
                fixture.Triggers.ReportEnemyDeath(Death(1000 + i, 100 + i));
            Assert.AreEqual(14, fixture.Triggers.GetCounter(SynergyActivationIds.UndeadSummon));

            fixture.World.Actors[0].IsAlive = false;
            fixture.Module.Tick(5.0f);
            Assert.AreEqual(4, fixture.Module.ActiveCount);
            fixture.Triggers.ReportEnemyDeath(Death(2000, 200));
            Assert.IsTrue(fixture.Triggers.HasPending(SynergyActivationIds.UndeadSummon));
        }

        [Test]
        public void MissingSpawnPoint_CancelsConsumedRoundWithoutRetry()
        {
            using UndeadFixture fixture = new UndeadFixture();
            fixture.World.HasSpawnPoint = false;

            Assert.IsTrue(fixture.Module.TryResolvePending(0.0f, 1));
            Assert.AreEqual(0, fixture.Module.ActiveCount);
            Assert.IsFalse(fixture.Triggers.HasPending(SynergyActivationIds.UndeadSummon));

            for (int i = 0; i < 15; i++)
                fixture.Triggers.ReportEnemyDeath(Death(i + 1, i + 2));
            Assert.IsTrue(fixture.Module.TryResolvePending(1.0f, 16));
            Assert.AreEqual(0, fixture.Module.ActiveCount);
            Assert.IsFalse(fixture.Triggers.HasPending(SynergyActivationIds.UndeadSummon));
            Assert.AreEqual(0, fixture.Triggers.GetCounter(SynergyActivationIds.UndeadSummon));
        }

        [Test]
        public void TargetingBossLockAndCleanup_UsePublicWorldSeam()
        {
            using UndeadFixture fixture = new UndeadFixture();
            fixture.Module.TryResolvePending(0.0f, 1);
            RecordingActor actor = fixture.World.Actors[0];
            RecordingTarget laterIdentity = new RecordingTarget(8, new Vector3(1.0f, 0.0f), false, true);
            RecordingTarget earlierIdentity = new RecordingTarget(2, new Vector3(1.0f, 0.0f), false, true);
            fixture.World.Targets.Add(laterIdentity);
            fixture.World.Targets.Add(earlierIdentity);

            fixture.Module.Tick(0.1f);
            Assert.AreSame(earlierIdentity, actor.LastTarget);
            Assert.AreEqual(1, fixture.World.CollectionCount);
            fixture.World.Targets.Clear();
            RecordingTarget replacement = new RecordingTarget(3, new Vector3(0.5f, 0.0f), false, true);
            fixture.World.Targets.Add(replacement);
            fixture.Module.Tick(0.2f);
            Assert.AreSame(earlierIdentity, actor.LastTarget);
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
            for (int i = 0; i < 15; i++)
                fixture.Triggers.ReportEnemyDeath(Death(i + 1, i + 2));
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
        public void ResetForResultAndDispose_ReleaseAllActors()
        {
            UndeadFixture resetFixture = new UndeadFixture();
            resetFixture.Module.TryResolvePending(0.0f, 1);
            resetFixture.Module.ResetForResult();
            Assert.AreEqual(0, resetFixture.Module.ActiveCount);
            Assert.AreEqual(1, resetFixture.World.ReleaseCount);
            resetFixture.Dispose();

            UndeadFixture disposeFixture = new UndeadFixture();
            disposeFixture.Module.TryResolvePending(0.0f, 1);
            disposeFixture.Module.Dispose();
            Assert.AreEqual(0, disposeFixture.Module.ActiveCount);
            Assert.AreEqual(1, disposeFixture.World.ReleaseCount);
            disposeFixture.Dispose();
        }

        static SynergyEnemyDeathEvent Death(long lifeId, int frameId)
        {
            return new SynergyEnemyDeathEvent(lifeId, SynergyDeathSourceCategory.Commander, false, false, false, 0L, frameId);
        }

        sealed class UndeadFixture : IDisposable
        {
            public readonly SynergyActivationState Activations;
            public readonly SynergyTriggerState Triggers;
            public readonly RecordingWorld World;
            public readonly UndeadSummonSynergy Module;

            public UndeadFixture()
            {
                LocalDataProvider provider = CreateProjectProvider();
                Activations = new SynergyActivationState(provider);
                Triggers = new SynergyTriggerState(Activations);
                World = new RecordingWorld();
                Module = new UndeadSummonSynergy(Triggers, provider.GetSynergySummon("UNIT_SYNERGY_SKELETON_01"), World);
                Activations.Refresh(CreateSlots("necromancer", "wraith_knight", "skeleton_bomber"));
            }

            public void Dispose()
            {
                Module.Dispose();
                Triggers.Dispose();
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

            public bool TryResolveRearSpawn(float desiredRearOffset, float fallbackRadius, out Vector3 position)
            {
                LastDesiredRearOffset = desiredRearOffset;
                LastFallbackRadius = fallbackRadius;
                position = new Vector3(-1.0f, 0.0f);
                return HasSpawnPoint;
            }

            public bool TrySpawn(in UndeadSummonSynergy.SpawnRequest request, out UndeadSummonSynergy.IActor actor)
            {
                LastRequest = request;
                RecordingActor created = new RecordingActor(request.Position);
                Actors.Add(created);
                actor = created;
                return true;
            }

            public void Release(UndeadSummonSynergy.IActor actor)
            {
                ReleaseCount++;
            }

            public void CollectTargets(List<UndeadSummonSynergy.ITarget> destination)
            {
                CollectionCount++;
                destination.AddRange(Targets);
            }
        }

        sealed class RecordingActor : UndeadSummonSynergy.IActor
        {
            public RecordingActor(Vector3 position)
            {
                Position = position;
                IsAlive = true;
            }

            public bool IsAlive { get; set; }
            public Vector3 Position { get; }
            public UndeadSummonSynergy.ITarget LastTarget { get; private set; }

            public void SetTarget(UndeadSummonSynergy.ITarget target)
            {
                LastTarget = target;
            }
        }

        sealed class RecordingTarget : UndeadSummonSynergy.ITarget
        {
            public RecordingTarget(int stableIdentity, Vector3 position, bool isBoss, bool isTargetable)
            {
                StableIdentity = stableIdentity;
                Position = position;
                IsBoss = isBoss;
                IsTargetable = isTargetable;
            }

            public int StableIdentity { get; }
            public Vector3 Position { get; }
            public bool IsBoss { get; }
            public bool IsTargetable { get; }
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return provider;
        }

        static IReadOnlyList<SquadSlotState> CreateSlots(params string[] baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[7];
            for (int i = 0; i < slots.Length; i++)
            {
                string baseUnitId = i < baseUnitIds.Length ? baseUnitIds[i] : string.Empty;
                bool active = string.IsNullOrEmpty(baseUnitId) == false;
                slots[i] = new SquadSlotState($"squad_{i:00}", baseUnitId, string.Empty, active ? 1 : 0, 3, false, baseUnitId);
            }

            return Array.AsReadOnly(slots);
        }
    }
}
