using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    public delegate bool BossHealthSnapshotProvider(out int hp, out int maxHp);

    internal sealed class RunGameplayUpdateCoordinator
    {
        private readonly RunServices _services;
        private readonly IGameplayRunUi _ui;
        private readonly BossHealthSnapshotProvider _bossHealthSnapshotProvider;
        private readonly Action _updateTraitOfferPresentation;
        private readonly Action _requestTutorialCompletionCorrection;

        internal RunGameplayUpdateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            BossHealthSnapshotProvider bossHealthSnapshotProvider,
            Action updateTraitOfferPresentation)
            : this(
                services,
                ui,
                bossHealthSnapshotProvider,
                updateTraitOfferPresentation,
                () => { })
        {
        }

        internal RunGameplayUpdateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            BossHealthSnapshotProvider bossHealthSnapshotProvider,
            Action updateTraitOfferPresentation,
            Action requestTutorialCompletionCorrection)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _bossHealthSnapshotProvider = bossHealthSnapshotProvider
                ?? throw new ArgumentNullException(nameof(bossHealthSnapshotProvider));
            _updateTraitOfferPresentation = updateTraitOfferPresentation
                ?? throw new ArgumentNullException(nameof(updateTraitOfferPresentation));
            _requestTutorialCompletionCorrection = requestTutorialCompletionCorrection
                ?? throw new ArgumentNullException(nameof(requestTutorialCompletionCorrection));
        }

        internal void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (_services.State.IsLoaded == false)
                return;

            P0Telemetry.SamplePerformance(unscaledDeltaTime);
            _services.State.AdvanceTime(deltaTime);
            if (_services.Context.IsTutorial)
                TutorialCheckpointProgress.TryAdvance(_services.State.ElapsedSeconds);
            _requestTutorialCompletionCorrection();
            _ui.SetRunStatus(_services.State.KillCount, _services.State.ElapsedSeconds);
            UpdateBossHud();
            _updateTraitOfferPresentation();
        }

        private void UpdateBossHud()
        {
            if (_bossHealthSnapshotProvider(out int hp, out int maxHp))
            {
                float ratio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
                _ui.ShowBoss("BOSS Hungry Giant", hp, maxHp);
                P0PlaytestDiagnostics.LogBossHpSample(hp, maxHp, ratio, "ui_update");
                P0PlaytestDiagnostics.SampleBossBodyVisibility(_ui.IsThreatDirectionVisible);
                return;
            }

            _ui.HideBoss();
        }
    }
}
