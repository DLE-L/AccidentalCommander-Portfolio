using System;
using Lizzo.PV.Legion.Synergy;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal sealed class MomentOfCompletionRunModule : IDisposable
    {
        static readonly string[] SynergyIds =
        {
            SynergyActivationIds.GuardShockwave,
            SynergyActivationIds.ArcherRain,
            SynergyActivationIds.MagicChain,
            SynergyActivationIds.ExplosionChain,
            SynergyActivationIds.BeastHunt,
            SynergyActivationIds.UndeadSummon,
            SynergyActivationIds.HealingBond,
            SynergyActivationIds.MixedCommand,
        };

        readonly bool[] _bonusConsumed = new bool[SynergyIds.Length];
        bool _disposed;

        public int GetFirstActivationExecutionCreditCount(string synergyId)
        {
            if (_disposed)
                return 1;

            int index = FindSynergyIndex(synergyId);
            if (index < 0 || _bonusConsumed[index])
                return 1;

            _bonusConsumed[index] = true;
            return 2;
        }

        public void Reset()
        {
            if (_disposed == false)
                Array.Clear(_bonusConsumed, 0, _bonusConsumed.Length);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }

        static int FindSynergyIndex(string synergyId)
        {
            for (int i = 0; i < SynergyIds.Length; i++)
            {
                if (SynergyIds[i] == synergyId)
                    return i;
            }

            return -1;
        }
    }
}
