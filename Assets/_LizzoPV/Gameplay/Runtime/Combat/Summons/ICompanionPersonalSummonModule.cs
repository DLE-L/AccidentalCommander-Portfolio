using UnityEngine;
using Lizzo.PV.Legion;

namespace Lizzo.PV.Combat.Summons
{
    public interface ICompanionPersonalSummonModule
    {
        int ActiveCount { get; }

        bool TrySpawn(in PersonalSummonSpawnRequest request, float currentTime);
        int GetActiveCount(string ownerKey, string sourceId);
        bool Release(PersonalSummonRuntime runtime);
        void Tick(float currentTime, float deltaTime);
        void Reset();
        void Dispose();
    }
}
