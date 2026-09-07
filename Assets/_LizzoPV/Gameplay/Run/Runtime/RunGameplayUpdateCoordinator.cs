using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    public delegate bool BossHealthSnapshotProvider(out string hudLabel, out int hp, out int maxHp);

    internal sealed class RunGameplayUpdateCoordinator
    {
        private readonly RunServices _services;
        private readonly IGameplayRunUi _ui;
        private readonly BossHealthSnapshotProvider _bossHealthSnapshotProvider;
        private readonly Action _requestTutorialCompletionCorrection;

        internal RunGameplayUpdateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            BossHealthSnapshotProvider bossHealthSnapshotProvider)
            : this(
                services,
                ui,
                bossHealthSnapshotProvider,
                () => { })
        {
        }

        internal RunGameplayUpdateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            BossHealthSnapshotProvider bossHealthSnapshotProvider,
            Action requestTutorialCompletionCorrection)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _bossHealthSnapshotProvider = bossHealthSnapshotProvider
                ?? throw new ArgumentNullException(nameof(bossHealthSnapshotProvider));
            _requestTutorialCompletionCorrection = requestTutorialCompletionCorrection
                ?? throw new ArgumentNullException(nameof(requestTutorialCompletionCorrection));
        }

        internal void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (_services.State.IsLoaded == false)
                return;

            RunTelemetry.SamplePerformance(unscaledDeltaTime);
            bool waitingForInitialRecruit = _services.Context.IsNormal
                && _services.CompanionRuntimeHost != null
                && _services.Party.ActiveCompanionSlotCount == 0
                && _services.CompanionRuntimeHost.Adapter.ActiveCompanionSlotCount == 0;
            if (waitingForInitialRecruit == false)
                _services.State.AdvanceTime(deltaTime);
            if (_services.Context.IsTutorial)
                TutorialCheckpointProgress.TryAdvance(_services.State.ElapsedSeconds);
            _requestTutorialCompletionCorrection();
            _ui.SetRunStatus(_services.State.KillCount, _services.State.ElapsedSeconds);
            UpdateBossHud();
        }

        private void UpdateBossHud()
        {
            if (_bossHealthSnapshotProvider(out string hudLabel, out int hp, out int maxHp))
            {
                float ratio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
                _ui.ShowBoss(hudLabel, hp, maxHp);
                RunDiagnostics.LogBossHpSample(hp, maxHp, ratio, "ui_update");
                RunDiagnostics.SampleBossBodyVisibility(_ui.IsThreatDirectionVisible);
                return;
            }

            _ui.HideBoss();
        }
    }
}
