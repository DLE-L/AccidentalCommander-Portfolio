using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0Telemetry
    {
        private static readonly HashSet<string> ConsoleVisibleEvents = new HashSet<string>
        {
            AnalyticsReady,
            AppsFlyerInstallReady,
            Install,
            TutorialStart,
            RunStart,
            RunEnd,
            BuildIdentity,
            BuildIdentityMissing,
            PerformanceSummary,
            GcGen0Spike,
            RestartResetPostcondition,
            RestartResetResidualViolation,
            FirstRecruit,
            FirstPromotion,
            FirstSynergy,
            EliteSeen,
            FirstBossSeen,
            FirstBossKill,
            TargetAcquired,
            BossTargetingSummary,
            BossDamageSummary,
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
            BattleHudView,
            HudVisibilityCheck,
            DebugOverlayHidden,
            DevButtonAction,
            CardOptionsShow,
            CardOfferBucketLog,
            CardTypeSeen,
            CardSelect,
            CardEffectApply,
            CardEffectApplyPassive,
            PassiveSlotStateUpdate,
            ActiveSquadSlotStateUpdate,
            ResultView,
            ResultBuildSummaryShow,
            ResultMvpView,
            NextRunGoalSeen,
            ResultNextRunClick,
            CompanionRecruit,
            PromotionComplete,
            SynergyRevalidate,
            SynergyActivate,
            SynergyKeep,
            SynergyGuardWallCast,
            SynergyGuardWallHitSummary,
            SynergyGuardProtectStart,
            GuardWallCast,
            GuardWallHit,
            GuardWallDamage,
            GuardWallKill,
            GuardWallBlockContact,
            GuardWallPush,
            QaGuardWrongMaterialCheck,
            DeathReason,
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
            FxBatchDeathMerge,
            SfxPlay,
            SfxCooldownSkip,
            DamageBlockedInvulnerable,
            CommanderDamageSummary,
            PlayerDamageBySource,
            HurtboxContactCommander,
            NormalEnemyContactDamage,
            RedChargerImpactHit,
            RedChargerImpactGraceHit,
            BossPatternHit,
            BossPatternRepeatBlock,
            BossPatternWarningShow,
            BossStaggerStart,
            BossCompanionDamageSummary,
            GuardFirstCastFeedbackShow,
            CompanionDown,
            CompanionRecover,
            CompanionDamageSummary,
            CompanionDownCountPreBoss,
            CompanionDownReasonSummary,
            PauseOpen,
            PauseResume,
            RewardDoubleAdShow,
            RewardDoubleAdClick,
            ReviveAdShow,
            ReviveAdClick
        };

        public static void Log(string eventName, params string[] parameters)
        {
            RecordEvent(eventName, parameters);

            if (ConsoleVisibleEvents.Contains(eventName) == false)
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
