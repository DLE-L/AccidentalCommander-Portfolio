using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Combat.Summons
{
    public readonly struct PersonalSummonSpawnRequest
    {
        public readonly string OwnerKey;
        public readonly string SourceId;
        public readonly Transform SpawnOrigin;
        public readonly string Address;
        public readonly CompanionPersonalSummonSetup Setup;
        public readonly int ActiveCap;

        public PersonalSummonSpawnRequest(string ownerKey, string sourceId, Transform spawnOrigin, string address, CompanionPersonalSummonSetup setup, int activeCap)
        {
            OwnerKey = ownerKey;
            SourceId = sourceId;
            SpawnOrigin = spawnOrigin;
            Address = address;
            Setup = setup;
            ActiveCap = activeCap;
        }

        public PersonalSummonSpawnRequest(int ownerId, string sourceId, Transform owner, string address, CompanionPersonalSummonSetup setup)
            : this(ownerId.ToString(), sourceId, owner, address, setup, setup.BaseActiveCap)
        {
        }

        public bool IsValid => string.IsNullOrEmpty(OwnerKey) == false
            && SpawnOrigin != null
            && string.IsNullOrEmpty(SourceId) == false
            && string.IsNullOrEmpty(Address) == false
            && string.IsNullOrEmpty(Setup.SummonId) == false
            && Setup.Hp > 0
            && Setup.Damage > 0
            && Setup.AttackInterval > 0.0f
            && Setup.Range > 0.0f
            && Setup.MoveSpeed >= 0.0f
            && Setup.AiScanInterval > 0.0f
            && ActiveCap >= 0;
    }

    public sealed class CompanionPersonalSummonModule : ICompanionPersonalSummonModule
    {
        private readonly IPrefabFactory _factory;
        private readonly ICompanionPersonalSummonTargetSource _targetSource;
        private readonly ICombatImmediateHitModule _immediateHitModule;
        private readonly List<ActiveSummon> _activeSummons = new List<ActiveSummon>(4);
        private readonly List<PersonalSummonTarget> _targets = new List<PersonalSummonTarget>(32);
        private bool _disposed;

        public int ActiveCount => _activeSummons.Count;

        public CompanionPersonalSummonModule(
            IPrefabFactory factory,
            ICompanionPersonalSummonTargetSource targetSource,
            ICombatImmediateHitModule immediateHitModule)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _targetSource = targetSource ?? throw new ArgumentNullException(nameof(targetSource));
            _immediateHitModule = immediateHitModule ?? throw new ArgumentNullException(nameof(immediateHitModule));
        }

        public bool TrySpawn(in PersonalSummonSpawnRequest request, float currentTime)
        {
            if (_disposed || request.IsValid == false || request.ActiveCap <= 0 || GetActiveCount(request.OwnerKey, request.SourceId) >= request.ActiveCap)
                return false;

            GameObject instance = _factory.Spawn(request.Address, pooled: true);
            if (instance == null)
                return false;

            PersonalSummonRuntime runtime = instance.GetComponent<PersonalSummonRuntime>();
            if (runtime == null || runtime.Configure(request.SpawnOrigin, request.SourceId, request.Setup) == false)
            {
                _factory.Release(instance);
                return false;
            }

            runtime.transform.position = request.SpawnOrigin.position;
            _activeSummons.Add(new ActiveSummon(request, runtime, currentTime));
            return true;
        }

        public bool Release(PersonalSummonRuntime runtime)
        {
            if (runtime == null)
                return false;

            for (int i = _activeSummons.Count - 1; i >= 0; i--)
            {
                if (_activeSummons[i].Runtime != runtime)
                    continue;

                ReleaseAt(i);
                return true;
            }

            return false;
        }

        public void Tick(float currentTime, float deltaTime)
        {
            if (_disposed)
                return;

            for (int i = _activeSummons.Count - 1; i >= 0; i--)
            {
                ActiveSummon active = _activeSummons[i];
                if (active.Runtime == null || active.Runtime.IsAlive == false)
                {
                    ReleaseAt(i);
                    continue;
                }

                if (currentTime >= active.NextScanAt)
                {
                    active.Target = SelectNearest(active.Runtime.transform.position);
                    active.NextScanAt = currentTime + active.Setup.AiScanInterval;
                }

                if (active.Target.Target != null && active.Target.Target.IsAlive)
                {
                    active.Runtime.MoveTowards(active.Target.Position, active.Setup.MoveSpeed, deltaTime);
                    if (currentTime >= active.NextAttackAt
                        && active.Runtime.IsInAttackRange(active.Target.Position, active.Setup.Range))
                    {
                        if (active.Runtime.TryAttack(active.Target, active.Setup.Damage, _immediateHitModule))
                            active.NextAttackAt = currentTime + active.Setup.AttackInterval;
                    }
                }
                else
                {
                    active.Runtime.SetMoving(false);
                }

                _activeSummons[i] = active;
            }
        }

        public void Reset()
        {
            for (int i = _activeSummons.Count - 1; i >= 0; i--)
                ReleaseAt(i);
            _targets.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }

        public int GetActiveCount(string ownerKey, string sourceId)
        {
            int count = 0;
            for (int i = 0; i < _activeSummons.Count; i++)
            {
                ActiveSummon active = _activeSummons[i];
                if (active.OwnerKey == ownerKey && active.SourceId == sourceId)
                    count++;
            }

            return count;
        }

        private PersonalSummonTarget SelectNearest(Vector3 origin)
        {
            _targets.Clear();
            _targetSource.CollectTargets(_targets);
            int selectedIndex = -1;
            float selectedDistance = float.MaxValue;
            int selectedInstanceId = int.MaxValue;
            for (int i = 0; i < _targets.Count; i++)
            {
                PersonalSummonTarget candidate = _targets[i];
                if (candidate.Target == null || candidate.Target.IsAlive == false)
                    continue;

                float distance = (candidate.Position - origin).sqrMagnitude;
                if (distance < selectedDistance
                    || (Mathf.Approximately(distance, selectedDistance) && candidate.InstanceId < selectedInstanceId))
                {
                    selectedIndex = i;
                    selectedDistance = distance;
                    selectedInstanceId = candidate.InstanceId;
                }
            }

            return selectedIndex < 0 ? default : _targets[selectedIndex];
        }

        private void ReleaseAt(int index)
        {
            ActiveSummon active = _activeSummons[index];
            _activeSummons.RemoveAt(index);
            if (active.Runtime != null)
            {
                _factory.Release(active.Runtime.gameObject);
            }
        }

        private struct ActiveSummon
        {
            public readonly string OwnerKey;
            public readonly string SourceId;
            public readonly CompanionPersonalSummonSetup Setup;
            public readonly PersonalSummonRuntime Runtime;
            public float NextScanAt;
            public float NextAttackAt;
            public PersonalSummonTarget Target;

            public ActiveSummon(in PersonalSummonSpawnRequest request, PersonalSummonRuntime runtime, float currentTime)
            {
                OwnerKey = request.OwnerKey;
                SourceId = request.SourceId;
                Setup = request.Setup;
                Runtime = runtime;
                NextScanAt = currentTime;
                NextAttackAt = currentTime;
                Target = default;
            }
        }
    }

}
