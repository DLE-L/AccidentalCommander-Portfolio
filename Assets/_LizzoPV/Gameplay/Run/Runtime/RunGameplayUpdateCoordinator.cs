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
        private float _openingRealSeconds;
        private bool _openingCardCharged;

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
            if (!TryCompleteOpeningCardGate(unscaledDeltaTime))
            {
                _ui.SetRunStatus(_services.State.KillCount, _services.State.ElapsedSeconds);
                UpdateBossHud();
                _updateTraitOfferPresentation();
                return;
            }

            _services.State.AdvanceTime(deltaTime);
            _services.SessionOutput.ReportProgress(_services.State.ElapsedSeconds);
            _requestTutorialCompletionCorrection();
            _ui.SetRunStatus(_services.State.KillCount, _services.State.ElapsedSeconds);
            UpdateBossHud();
            _updateTraitOfferPresentation();
        }

        private bool TryCompleteOpeningCardGate(float unscaledDeltaTime)
        {
            RunDefinition definition = _services.Definition;
            if (_openingCardCharged)
                return true;
            if (definition.InitialExperienceCharge <= 0
                || _services.Party.ActiveCompanionSlotCount > 0)
            {
                _openingCardCharged = true;
                return true;
            }

            _openingRealSeconds += Mathf.Max(0.0f, unscaledDeltaTime);
            if (_openingRealSeconds < definition.InitialExperienceChargeSeconds)
                return false;

            _openingCardCharged = true;
            int charge = Mathf.Min(
                definition.InitialExperienceCharge,
                Mathf.Max(0, _services.State.RequiredExperience - _services.State.Experience));
            if (charge > 0)
                _services.State.AddExperience(charge);

            return false;
        }

        private void UpdateBossHud()
        {
            if (_bossHealthSnapshotProvider(out int hp, out int maxHp))
            {
                float ratio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
                _ui.ShowBoss("BOSS " + _services.Definition.Boss.DisplayName, hp, maxHp);
                P0PlaytestDiagnostics.LogBossHpSample(hp, maxHp, ratio, "ui_update");
                P0PlaytestDiagnostics.SampleBossBodyVisibility(_ui.IsThreatDirectionVisible);
                return;
            }

            _ui.HideBoss();
        }
    }
}
