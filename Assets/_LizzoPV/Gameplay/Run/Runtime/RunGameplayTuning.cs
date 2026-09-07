using System;
using Lizzo.PV.Data;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class RunGameplayTuning
    {
        private readonly IDataProvider _data;

        public RunGameplayTuning(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public int Boss1Hp => _data.GetEnemy("boss_hungry_giant")?.Hp ?? 2500;
        public int Boss1Atk => _data.GetEnemy("boss_hungry_giant")?.Attack ?? 25;
        public int MaxEnemyStage1 => _data.RunTuning.MaxEnemyStage1;
        public float Boss1WarningTime => 1.0f;
        public bool FullSlotNewCompanionBlock => true;
        public float CommanderPostHitInvuln => 0.25f;
        public float ContactDamageSourceCooldown => 0.25f;
        public float CompanionPostHitCooldown => 0.6f;
        public float CompanionHpScale => 1.25f;
        public float CompanionSpawnProtection => 1.2f;
        public float ArcherSpawnProtection => 1.5f;
        public float CompanionDownDuration => 4.0f;
        public float CompanionRecoverHpRatio => 0.3f;
        public float FormationSpacing => 0.85f;
        public float FormationVectorLockSeconds => 1.7f;
        public float CommanderVisibilityPushRadius => 0.7f;
        public float GuardCompanionDamageReduction => 0.25f;
        public float GuardCompanionDamageReductionDuration => 6.0f;
    }
}
