using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0Telemetry
    {
        private static readonly HashSet<string> SummaryConsoleEvents = new HashSet<string>
        {
            RunStart,
            RunEnd,
            BuildIdentity,
            BuildIdentityMissing,
            Crash,
            ResultView,
            DeathReason,
            PerformanceSummary,
            GcGen0Spike,
            RestartResetPostcondition,
            RestartResetResidualViolation,
            CardOptionsShow,
            CardOfferGenerated,
            CardOfferSelected,
            MaxBuildComplete,
            CardSelect,
            CompanionRecruit,
            PromotionComplete,
            SynergyActivate,
            FirstRecruit,
            FirstPromotion,
            FirstSynergy,
            EliteSeen,
            FirstBossSeen,
            FirstBossKill,
            BossPatternWarningShow,
            BossPatternHit,
            BossStaggerStart,
            RedChargerTtk,
            EnemyTtkSummary,
            BossTargetingSummary,
            BossDamageSummary,
            BossHpbarSyncCheck,
            BossArenaCreate,
            BossPhaseStart,
            BossCompanionDamageSummary,
            EnemyAliveSnapshot,
            ShieldOrcTtk,
            ShieldOrcFeedbackCheck,
            CombatReadabilityCheck,
            CompanionDamageSummary,
            CompanionDamageContributionSummary,
            SynergyContributionSummary,
            CompanionDownCountPreBoss,
            CompanionDownReasonSummary,
            CommanderDamageSummary,
            PlayerDamageBySource,
            SynergyRevalidate,
            SynergyKeep,
            SynergyGuardWallHitSummary,
            SynergyGuardProtectStart
        };

        private static readonly HashSet<string> DebugOnlyConsoleEvents = new HashSet<string>
        {
            AnalyticsReady,
            AppsFlyerInstallReady,
            Install,
            TutorialStart,
            TargetAcquired,
            BossBodyVisibleRatio,
            BossWarning15s,
            BossWarning10s,
            BossCountdownTick,
            BossSpawnMarkerShow,
            BattleHudView,
            HudVisibilityCheck,
            DebugOverlayHidden,
            DevButtonAction,
            CardOfferBucketLog,
            CardOfferDiagnostic,
            CardTypeSeen,
            CardEffectApply,
            CardEffectApplyPassive,
            PassiveSlotStateUpdate,
            ActiveSquadSlotStateUpdate,
            ResultMvpView,
            NextRunGoalSeen,
            ResultNextRunClick,
            SynergyGuardWallCast,
            GuardWallCast,
            GuardWallHit,
            GuardWallDamage,
            GuardWallKill,
            GuardWallBlockContact,
            GuardWallPush,
            QaGuardWrongMaterialCheck,
            EnemyAliveTime,
            EnemyDamagedTime,
            EnemyTargetedTime,
            EnemyLastHitTime,
            EnemyTotalDamageTaken,
            EnemyKilledBy,
            EnemyContactDamageCount,
            EnemyRewardDrop,
            EnemyDeathFeedbackShow,
            ExpOrbAbsorb,
            HitFeedbackShow,
            HitstopApply,
            SfxPlay,
            SfxCooldownSkip,
            DamageBlockedInvulnerable,
            HurtboxContactCommander,
            NormalEnemyContactDamage,
            RedChargerImpactHit,
            RedChargerImpactGraceHit,
            BossPatternRepeatBlock,
            GuardFirstCastFeedbackShow,
            SynergyUndeadSummonSpawn,
            CompanionDown,
            CompanionRecover,
            PauseOpen,
            PauseResume,
            RewardDoubleAdShow,
            RewardDoubleAdClick,
            ReviveAdShow,
            ReviveAdClick
        };

        private static readonly HashSet<string> VerboseDiagnosticsEvents = new HashSet<string>
        {
            FirstBossSeen,
            FirstBossKill,
            EliteSeen,
            TargetAcquired,
            AttackCast,
            SkillCast,
            BossTargetingSummary,
            BossDamageSummary,
            BossHpSample,
            BossHpbarSyncCheck,
            BossBodyVisibleRatio,
            BossArenaCreate,
            BossWarning15s,
            BossWarning10s,
            BossCountdownTick,
            BossSpawnMarkerShow,
            BossPhaseStart,
            EnemyAliveSnapshot,
            EnemyTtkSummary,
            ShieldOrcTtk,
            RedChargerTtk,
            ShieldOrcFeedbackCheck,
            CombatReadabilityCheck,
            EnemyAliveTime,
            EnemyDamagedTime,
            EnemyTargetedTime,
            EnemyLastHitTime,
            EnemyTotalDamageTaken,
            EnemyKilledBy,
            EnemyContactDamageCount,
            ExpOrbAbsorb,
            EnemyRewardDrop,
            EnemyDeathFeedbackShow,
            HitFeedbackShow,
            HitstopApply,
            SfxPlay,
            SfxCooldownSkip,
            DamageBlockedInvulnerable,
            HurtboxContact,
            HurtboxContactCommander,
            NormalEnemyContactDamage,
            RedChargerImpactHit,
            RedChargerImpactGraceHit,
            BossPatternHit,
            BossPatternRepeatBlock,
            BossPatternWarningShow,
            BossStaggerStart,
            ChargePathWarning,
            FormationOverlapWarning
        };

        public static bool VerboseDiagnosticsEnabled { get; set; }

        private static bool IsConsoleVisible(string eventName)
        {
            if (VerboseDiagnosticsEvents.Contains(eventName))
                return VerboseDiagnosticsEnabled;

            if (SummaryConsoleEvents.Contains(eventName))
                return true;

            return (Application.isEditor || Debug.isDebugBuild) && DebugOnlyConsoleEvents.Contains(eventName);
        }

        private static bool ShouldRecordEvent(string eventName)
        {
            if (VerboseDiagnosticsEvents.Contains(eventName))
                return VerboseDiagnosticsEnabled;

            return Application.isEditor || Debug.isDebugBuild || SummaryConsoleEvents.Contains(eventName);
        }

        public static void Log(string eventName, params string[] parameters)
        {
            if (ShouldRecordEvent(eventName) == false)
                return;

            RecordEvent(eventName, parameters);

            if (IsConsoleVisible(eventName) == false)
                return;

            if (parameters == null || parameters.Length == 0)
            {
                Debug.Log($"P0 analytics: {eventName}");
                return;
            }

            Debug.Log($"P0 analytics: {eventName} | {string.Join(", ", parameters)}");
        }

        public static void LogOnce(string eventName, params string[] parameters)
        {
            if (ShouldRecordEvent(eventName) == false)
                return;

            if (LoggedOnceEvents.Contains(eventName))
            {
                // Prototype QA needs current-run gate state even when console LogOnce suppresses duplicate first-event lines.
                RecordEvent(eventName, parameters);
                return;
            }

            LoggedOnceEvents.Add(eventName);
            Log(eventName, parameters);
        }

        private static void RecordEvent(string eventName, string[] parameters)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            if (EventStates.TryGetValue(eventName, out EventState state) == false)
            {
                state = new EventState();
                EventStates[eventName] = state;
            }

            float runSeconds = RunElapsedSeconds;
            state.Count++;
            if (state.FirstRunSeconds < 0.0f)
                state.FirstRunSeconds = runSeconds;

            state.LastRunSeconds = runSeconds;
            state.LastParametersText = parameters == null || parameters.Length == 0
                ? string.Empty
                : string.Join(", ", parameters);
            RecordRunLogEntry(eventName, state.LastParametersText);
        }
    }
}
