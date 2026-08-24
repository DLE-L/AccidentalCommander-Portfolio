using System.Collections.Generic;
using System.Text;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.Legion;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
    {
        private static PartyService _party;

        public static void ConfigureParty(PartyService party)
        {
            _party = party ?? throw new System.ArgumentNullException(nameof(party));
        }

        public static void ClearParty()
        {
            _party = null;
        }
        private const string UNKNOWN_ENEMY_ID = CombatIds.Unknown;
        private const string SHIELD_ORC_ID = CombatIds.ShieldOrc;
        private const string RED_CHARGER_ID = CombatIds.EliteRedCharger;
        private const float BOSS_VISIBILITY_SAMPLE_INTERVAL_SECONDS = 1.0f;
        private const float BOSS_LOW_VISIBILITY_THRESHOLD = 0.6f;

        private static readonly Dictionary<int, EnemySpawnRecord> SpawnRecords = new Dictionary<int, EnemySpawnRecord>();
        private static readonly Dictionary<int, EnemyAnalysisRecord> EnemyRecords = new Dictionary<int, EnemyAnalysisRecord>();
        private static readonly Dictionary<string, int> SpawnCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> DeathCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> LifetimeSums = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> LifetimeCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> FirstDamageTimeSums = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> FirstDamageTimeCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> DamageWindowSums = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> DamageWindowCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> FirstTargetTimeSums = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> FirstTargetTimeCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> TargetWindowSums = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> TargetWindowCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> LastHitTimeSums = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> LastHitTimeCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> EnemyDamageTaken = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> EnemyContactDamageCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> EnemyKilledByCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> EnemyDeathFeedbackCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> ExpOrbAbsorbCueCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> HitFeedbackCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> HitFeedbackMissingCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> HitStopCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> SfxPlayCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> SfxMissingCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> SfxCooldownSkipCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CompanionDamageByUnit = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CompanionHitsByUnit = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CompanionDamageBySource = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CompanionDownByUnit = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CompanionDownBySource = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> PreBossCompanionDownByUnit = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> PreBossCompanionDownBySource = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> BossPhaseCompanionDownBySource = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CompanionLastHpPercent = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CommanderDamageBySource = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CommanderHitsBySource = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CommanderDamageByPattern = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> CommanderHurtboxContacts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> NormalEnemyContactDamageCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> RedChargerImpactHitCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> RedChargerImpactGraceHitCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> ScratchCounts = new Dictionary<string, int>();
        private static readonly StringBuilder Builder = new StringBuilder(160);

        private static float _nextBossHpSampleRunSeconds;
        private static float _nextBossVisibilitySampleRunSeconds;
        private static int _preBossCompanionDownCount;
        private static int _bossPhaseCompanionDownCount;
        private static int _bossHpbarSyncSamples;
        private static int _bossHpbarSyncMismatchCount;
        private static int _bossVisibilitySamples;
        private static int _bossLowVisibilitySamples;
        private static int _bossIndicatorVisibleSamples;
        private static int _shieldOrcHitFeedbackCount;
        private static int _shieldOrcCrackFeedbackCount;
        private static int _shieldOrcDeathFeedbackCount;
        private static int _commanderDamageTotal;
        private static int _commanderHitCount;
        private static int _commanderLastHpPercent;
        private static float _bossVisibilityRatioSum;
        private static float _bossMinVisibilityRatio;
        private static bool _hasLoggedBossLowVisibility;

    }
}
