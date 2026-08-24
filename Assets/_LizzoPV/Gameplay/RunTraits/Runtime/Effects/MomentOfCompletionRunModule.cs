using System;
using Lizzo.PV.Gameplay.Diagnostics;
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
            int creditCount = 1;
            if (_disposed == false)
            {
                int index = FindSynergyIndex(synergyId);
                if (index >= 0 && _bonusConsumed[index] == false)
                {
                    _bonusConsumed[index] = true;
                    creditCount = 2;
                }
            }

            Build1RuntimeDiagnostics.Log(creditCount > 1 ? "trait_effect_applied" : "trait_effect_blocked",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.MomentOfCompletion),
                Build1RuntimeDiagnostics.Text("synergy_id", synergyId),
                Build1RuntimeDiagnostics.Int("execution_credit_count", creditCount),
                Build1RuntimeDiagnostics.Bool("extra_credit_granted", creditCount > 1),
                Build1RuntimeDiagnostics.Text("pending_count", "unavailable"),
                Build1RuntimeDiagnostics.Text("block_reason", creditCount > 1 ? "none" : "second_use_or_unsupported"));
            return creditCount;
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
