namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
    {
        public static void Reset()
        {
            SpawnRecords.Clear();
            EnemyRecords.Clear();
            SpawnCounts.Clear();
            DeathCounts.Clear();
            LifetimeSums.Clear();
            LifetimeCounts.Clear();
            FirstDamageTimeSums.Clear();
            FirstDamageTimeCounts.Clear();
            DamageWindowSums.Clear();
            DamageWindowCounts.Clear();
            FirstTargetTimeSums.Clear();
            FirstTargetTimeCounts.Clear();
            TargetWindowSums.Clear();
            TargetWindowCounts.Clear();
            LastHitTimeSums.Clear();
            LastHitTimeCounts.Clear();
            EnemyDamageTaken.Clear();
            EnemyContactDamageCounts.Clear();
            EnemyKilledByCounts.Clear();
            EnemyDeathFeedbackCounts.Clear();
            ExpOrbAbsorbCueCounts.Clear();
            HitFeedbackCounts.Clear();
            HitFeedbackMissingCounts.Clear();
            HitStopCounts.Clear();
            FxBatchDeathMergeCounts.Clear();
            SfxPlayCounts.Clear();
            SfxMissingCounts.Clear();
            SfxCooldownSkipCounts.Clear();
            CompanionDamageByUnit.Clear();
            CompanionHitsByUnit.Clear();
            CompanionDamageBySource.Clear();
            CompanionDownByUnit.Clear();
            CompanionDownBySource.Clear();
            PreBossCompanionDownByUnit.Clear();
            PreBossCompanionDownBySource.Clear();
            BossPhaseCompanionDownBySource.Clear();
            CompanionLastHpPercent.Clear();
            CommanderDamageBySource.Clear();
            CommanderHitsBySource.Clear();
            CommanderDamageByPattern.Clear();
            CommanderHurtboxContacts.Clear();
            NormalEnemyContactDamageCounts.Clear();
            RedChargerImpactHitCounts.Clear();
            RedChargerImpactGraceHitCounts.Clear();
            ScratchCounts.Clear();
            Builder.Clear();
            _nextBossHpSampleRunSeconds = 0.0f;
            _nextBossVisibilitySampleRunSeconds = 0.0f;
            _preBossCompanionDownCount = 0;
            _bossPhaseCompanionDownCount = 0;
            _bossHpbarSyncSamples = 0;
            _bossHpbarSyncMismatchCount = 0;
            _bossVisibilitySamples = 0;
            _bossLowVisibilitySamples = 0;
            _bossIndicatorVisibleSamples = 0;
            _shieldOrcHitFeedbackCount = 0;
            _shieldOrcCrackFeedbackCount = 0;
            _shieldOrcDeathFeedbackCount = 0;
            _commanderDamageTotal = 0;
            _commanderHitCount = 0;
            _commanderLastHpPercent = -1;
            _bossVisibilityRatioSum = 0.0f;
            _bossMinVisibilityRatio = 1.0f;
            _hasLoggedBossLowVisibility = false;
        }
    }
}
