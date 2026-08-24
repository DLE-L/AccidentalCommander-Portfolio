using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public sealed class UndeadSummonUnityWorld : UndeadSummonSynergy.IWorld, IDisposable
    {
        public const string SkeletonAddress = "Lizzo/Characters/Supports/UNIT_SYNERGY_SKELETON_01";

        readonly ICommanderAnchor _commander;
        readonly RuntimeObjectRegistry _registry;
        readonly IPrefabFactory _factory;
        readonly ICombatImmediateHitModule _immediateHits;
        readonly SafeKnockbackWorld _safeWorld;
        readonly Dictionary<SynergySkeletonRuntime, GameObject> _owned = new Dictionary<SynergySkeletonRuntime, GameObject>(5);
        readonly Dictionary<MonsterController, TargetAdapter> _targetAdapters = new Dictionary<MonsterController, TargetAdapter>(32);
        readonly List<SynergySkeletonRuntime> _releaseScratch = new List<SynergySkeletonRuntime>(5);

        bool _disposed;

        public interface ICommanderAnchor
        {
            bool TryGetPosition(out Vector3 position);
            Vector2 MoveDirection { get; }
            Vector3 ResolveFormationForward();
        }

        internal UndeadSummonUnityWorld(
            RuntimeObjectRegistry registry,
            Lizzo.PV.Legion.FormationService formation,
            IPrefabFactory factory,
            ICombatImmediateHitModule immediateHits,
            SafeKnockbackWorld safeWorld)
            : this(new RuntimeCommanderAnchor(registry, formation), registry, factory, immediateHits, safeWorld)
        {
        }

        public UndeadSummonUnityWorld(
            ICommanderAnchor commander,
            RuntimeObjectRegistry registry,
            IPrefabFactory factory,
            ICombatImmediateHitModule immediateHits,
            SafeKnockbackWorld safeWorld)
        {
            _commander = commander ?? throw new ArgumentNullException(nameof(commander));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _safeWorld = safeWorld;
        }

        public bool TryResolveRearSpawn(float desiredRearOffset, float fallbackRadius, out Vector3 position)
        {
            position = default;
            if (_disposed || _commander.TryGetPosition(out Vector3 commanderPosition) == false)
                return false;

            Vector2 direction = _commander.MoveDirection;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                Vector3 formationForward = _commander.ResolveFormationForward();
                direction = new Vector2(formationForward.x, formationForward.y);
            }

            if (direction.sqrMagnitude <= 0.000001f)
                return false;

            Vector3 desired = commanderPosition - (Vector3)(direction.normalized * desiredRearOffset);
            if (_safeWorld == null)
            {
                position = desired;
                return true;
            }

            if (_safeWorld.TryResolveNearestValidPoint(desired, fallbackRadius, out Vector2 resolved) == false)
                return false;

            position = new Vector3(resolved.x, resolved.y, commanderPosition.z);
            return true;
        }

        public bool TrySpawn(in UndeadSummonSynergy.SpawnRequest request, out UndeadSummonSynergy.IActor actor)
        {
            actor = null;
            if (_disposed)
                return false;

            GameObject instance = _factory.Spawn(SkeletonAddress, null, pooled: true);
            if (instance == null)
                return false;

            SynergySkeletonRuntime skeleton = instance.GetComponent<SynergySkeletonRuntime>();
            if (skeleton == null)
            {
                _factory.Release(instance);
                return false;
            }

            instance.transform.position = request.Position;
            if (skeleton.Configure(request.Data, request.SpawnTime, _immediateHits) == false)
            {
                skeleton.ResetForRelease();
                _factory.Release(instance);
                return false;
            }

            _owned.Add(skeleton, instance);
            actor = skeleton;
            return true;
        }

        public void Release(UndeadSummonSynergy.IActor actor)
        {
            if ((actor is SynergySkeletonRuntime skeleton) == false
                || _owned.TryGetValue(skeleton, out GameObject instance) == false)
            {
                return;
            }

            _owned.Remove(skeleton);
            skeleton.ResetForRelease();
            _factory.Release(instance);
        }

        public void CollectTargets(List<UndeadSummonSynergy.ITarget> destination)
        {
            if (_disposed || destination == null || _registry.Enemies == null)
                return;

            foreach (MonsterController monster in _registry.Enemies)
            {
                if (IsTargetable(monster) == false)
                    continue;

                if (_targetAdapters.TryGetValue(monster, out TargetAdapter adapter) == false)
                {
                    adapter = new TargetAdapter(monster);
                    _targetAdapters.Add(monster, adapter);
                }

                destination.Add(adapter);
            }
        }

        public bool TryGetCombatTarget(MonsterController monster, out UndeadSummonSynergy.ICombatTarget target)
        {
            target = null;
            if (_disposed || IsTargetable(monster) == false)
                return false;

            if (_targetAdapters.TryGetValue(monster, out TargetAdapter adapter) == false)
            {
                adapter = new TargetAdapter(monster);
                _targetAdapters.Add(monster, adapter);
            }

            target = adapter;
            return true;
        }

        public void AdvanceActors(float now, float deltaTime)
        {
            if (_disposed)
                return;

            foreach (KeyValuePair<SynergySkeletonRuntime, GameObject> pair in _owned)
            {
                if (pair.Key != null && pair.Key.IsAlive)
                    pair.Key.Tick(now, deltaTime);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _releaseScratch.Clear();
            foreach (SynergySkeletonRuntime skeleton in _owned.Keys)
                _releaseScratch.Add(skeleton);
            for (int index = 0; index < _releaseScratch.Count; index++)
                Release(_releaseScratch[index]);
            _releaseScratch.Clear();
            _targetAdapters.Clear();
            _disposed = true;
        }

        static bool IsTargetable(MonsterController monster)
        {
            return monster != null
                && monster.IsValid()
                && monster.Hp > 0
                && ((ICombatImmediateHitTarget)monster).IsAlive;
        }

        sealed class TargetAdapter : UndeadSummonSynergy.ICombatTarget
        {
            readonly MonsterController _monster;

            public TargetAdapter(MonsterController monster) { _monster = monster; }

            public int StableIdentity
            {
                get
                {
                    long sequence = _monster == null ? 0L : _monster.SpawnSequence;
                    return sequence > 0L && sequence <= int.MaxValue ? (int)sequence : _monster.GetInstanceID();
                }
            }

            public Vector3 Position => _monster == null ? default : _monster.transform.position;
            public bool IsBoss => _monster != null && _monster.IsBoss;
            public bool IsTargetable => UndeadSummonUnityWorld.IsTargetable(_monster);
            public ICombatImmediateHitTarget CombatTarget => _monster;
        }

        sealed class RuntimeCommanderAnchor : ICommanderAnchor
        {
            readonly RuntimeObjectRegistry _registry;
            readonly Lizzo.PV.Legion.FormationService _formation;

            public RuntimeCommanderAnchor(RuntimeObjectRegistry registry, Lizzo.PV.Legion.FormationService formation)
            {
                _registry = registry;
                _formation = formation;
            }

            public bool TryGetPosition(out Vector3 position)
            {
                PlayerController commander = _registry == null ? null : _registry.Player;
                if (commander == null)
                {
                    position = default;
                    return false;
                }

                position = commander.transform.position;
                return true;
            }

            public Vector2 MoveDirection => _registry == null || _registry.Player == null ? Vector2.zero : _registry.Player.MoveDirection;
            public Vector3 ResolveFormationForward() => _formation == null ? Vector3.zero : _formation.ResolveForward();
        }
    }
}
