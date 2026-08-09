using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public sealed class UndeadSummonSynergy : IDisposable
    {
        const string SummonId = "UNIT_SYNERGY_SKELETON_01";
        const string SynergyId = "synergy_undead_summon";
        const float DesiredRearOffset = 1.0f;
        const float SpawnFallbackRadius = 1.5f;

        readonly SynergyTriggerState _triggers;
        readonly SynergySummonData _data;
        readonly IWorld _world;
        readonly List<ActiveActor> _actors = new List<ActiveActor>(5);
        readonly List<ITarget> _targets = new List<ITarget>(32);

        int _lastAttemptFrame = int.MinValue;
        bool _disposed;
        public readonly struct SpawnRequest
        {
            public SpawnRequest(SynergySummonData data, Vector3 position, float spawnTime)
            {
                Data = data;
                Position = position;
                SpawnTime = spawnTime;
            }

            public SynergySummonData Data { get; }
            public Vector3 Position { get; }
            public float SpawnTime { get; }
        }

        public interface IWorld
        {
            bool TryResolveRearSpawn(float desiredRearOffset, float fallbackRadius, out Vector3 position);
            bool TrySpawn(in SpawnRequest request, out IActor actor);
            void Release(IActor actor);
            void CollectTargets(List<ITarget> destination);
        }

        public interface IActor
        {
            bool IsAlive { get; }
            Vector3 Position { get; }
            void SetTarget(ITarget target);
        }

        public interface ITarget
        {
            int StableIdentity { get; }
            Vector3 Position { get; }
            bool IsBoss { get; }
            bool IsTargetable { get; }
        }

        public interface ICombatTarget : ITarget
        {
            Lizzo.PV.Combat.ICombatImmediateHitTarget CombatTarget { get; }
        }

        public UndeadSummonSynergy(SynergyTriggerState triggers, SynergySummonData data, IWorld world)
        {
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            if (data == null
                || data.Id != SummonId
                || data.SynergyId != SynergyId
                || data.ActiveCap <= 0
                || data.FrameSpawnCap != 1
                || data.BossLockCount <= 0)
            {
                throw new ArgumentException("Expected the canonical Undead synergy summon data.", nameof(data));
            }

            _data = data;
        }

        public int ActiveCount => _actors.Count;

        public bool TryResolvePending(float now, int frameId)
        {
            if (_disposed || _lastAttemptFrame == frameId || _triggers.HasPending(SynergyActivationIds.UndeadSummon) == false)
                return false;

            CleanupDeadActors();
            if (_triggers.TryConsumePending(SynergyActivationIds.UndeadSummon, out _) == false)
                return false;

            _lastAttemptFrame = frameId;
            if (_actors.Count >= _data.ActiveCap
                || _world.TryResolveRearSpawn(DesiredRearOffset, SpawnFallbackRadius, out Vector3 position) == false
                || _world.TrySpawn(new SpawnRequest(_data, position, now), out IActor actor) == false
                || actor == null)
            {
                UpdateAliveCap();
                return true;
            }

            _actors.Add(new ActiveActor(actor, now));
            UpdateAliveCap();
            return true;
        }

        public void Tick(float now)
        {
            if (_disposed)
                return;

            CleanupDeadActors();
            bool collectNormalTargets = false;
            for (int i = 0; i < _actors.Count; i++)
            {
                ActiveActor active = _actors[i];
                if (GetLockedBoss(active, now) == null)
                {
                    if (active.LockedBoss != null)
                    {
                        active.LockedBoss = null;
                        active.NextNormalScanAt = now;
                    }

                    if (active.NormalTarget != null && active.NormalTarget.IsTargetable == false)
                        active.NormalTarget = null;

                    if (now >= active.NextNormalScanAt)
                        collectNormalTargets = true;
                }

                _actors[i] = active;
            }

            _targets.Clear();
            if (collectNormalTargets)
                _world.CollectTargets(_targets);

            for (int i = 0; i < _actors.Count; i++)
            {
                ActiveActor active = _actors[i];
                ITarget target = GetLockedBoss(active, now);
                if (target == null)
                {
                    if (collectNormalTargets && now >= active.NextNormalScanAt)
                    {
                        active.NormalTarget = SelectNearest(active.Actor.Position);
                        active.NextNormalScanAt = now + _data.AiScanInterval;
                    }

                    target = active.NormalTarget;
                }

                active.Actor.SetTarget(target);
                _actors[i] = active;
            }
        }

        public void OnBossPhaseStarted(ITarget boss, float now)
        {
            if (_disposed || boss == null || boss.IsBoss == false || boss.IsTargetable == false)
                return;

            CleanupDeadActors();
            float lockUntil = now + _data.BossLockCount;
            for (int i = 0; i < _actors.Count; i++)
            {
                ActiveActor active = _actors[i];
                active.LockedBoss = boss;
                active.LockUntil = lockUntil;
                active.Actor.SetTarget(boss);
                _actors[i] = active;
            }
        }

        public void ResetForResult()
        {
            if (_disposed)
                return;

            for (int i = _actors.Count - 1; i >= 0; i--)
                ReleaseAt(i);

            _targets.Clear();
            _lastAttemptFrame = int.MinValue;
            UpdateAliveCap();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            ResetForResult();
            _disposed = true;
        }

        void CleanupDeadActors()
        {
            bool removed = false;
            for (int i = _actors.Count - 1; i >= 0; i--)
            {
                IActor actor = _actors[i].Actor;
                if (actor != null && actor.IsAlive)
                    continue;

                ReleaseAt(i);
                removed = true;
            }

            if (removed)
                UpdateAliveCap();
        }

        ITarget GetLockedBoss(in ActiveActor active, float now)
        {
            if (active.LockedBoss != null
                && now < active.LockUntil
                && active.LockedBoss.IsBoss
                && active.LockedBoss.IsTargetable)
            {
                return active.LockedBoss;
            }

            return null;
        }

        ITarget SelectNearest(Vector3 origin)
        {
            ITarget selected = null;
            float selectedDistance = float.MaxValue;
            int selectedIdentity = int.MaxValue;
            for (int i = 0; i < _targets.Count; i++)
            {
                ITarget candidate = _targets[i];
                if (candidate == null || candidate.IsTargetable == false)
                    continue;

                float distance = (candidate.Position - origin).sqrMagnitude;
                if (distance < selectedDistance
                    || (Mathf.Approximately(distance, selectedDistance) && candidate.StableIdentity < selectedIdentity))
                {
                    selected = candidate;
                    selectedDistance = distance;
                    selectedIdentity = candidate.StableIdentity;
                }
            }

            return selected;
        }

        void ReleaseAt(int index)
        {
            IActor actor = _actors[index].Actor;
            _actors.RemoveAt(index);
            if (actor != null)
                _world.Release(actor);
        }

        void UpdateAliveCap()
        {
            _triggers.SetUndeadAliveCapFull(_actors.Count >= _data.ActiveCap);
        }

        struct ActiveActor
        {
            public readonly IActor Actor;
            public ITarget LockedBoss;
            public float LockUntil;
            public ITarget NormalTarget;
            public float NextNormalScanAt;

            public ActiveActor(IActor actor, float spawnTime)
            {
                Actor = actor;
                LockedBoss = null;
                LockUntil = 0.0f;
                NormalTarget = null;
                NextNormalScanAt = spawnTime;
            }
        }
    }
}
