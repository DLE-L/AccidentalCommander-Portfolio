using System;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class RunGameplayUpdateCoordinator
    {
        private readonly RunServices _services;
        private readonly IGameplayRunUi _ui;
        private readonly Func<bool> _isBossPhaseActive;

        internal RunGameplayUpdateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            Func<bool> isBossPhaseActive)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _isBossPhaseActive = isBossPhaseActive ?? throw new ArgumentNullException(nameof(isBossPhaseActive));
        }

        internal void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (_services.State.IsLoaded == false)
                return;

            P0Telemetry.SamplePerformance(unscaledDeltaTime);
            _services.State.AdvanceTime(deltaTime);
            _ui.SetRunStatus(_services.State.KillCount, _services.State.ElapsedSeconds);
            UpdateBossHud();
            TryPresentRunTraitOffer();
        }

        private void UpdateBossHud()
        {
            if (HungryGiantBehaviour.TryGetCurrentHpSnapshot(out int hp, out int maxHp))
            {
                float ratio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
                _ui.ShowBoss("BOSS Hungry Giant", hp, maxHp);
                P0PlaytestDiagnostics.LogBossHpSample(hp, maxHp, ratio, "ui_update");
                P0PlaytestDiagnostics.SampleBossBodyVisibility(_ui.IsThreatDirectionVisible);
                return;
            }

            _ui.HideBoss();
        }

        private void TryPresentRunTraitOffer()
        {
            float bossSpawnSeconds = _services.App.Data.RunTuning.BossSpawnSeconds;
            if (_services.RunTraitOffers == null
                || _services.State.ElapsedSeconds >= bossSpawnSeconds
                || _isBossPhaseActive()
                || _ui is not IRunTraitOfferUi traitOfferUi)
                return;

            bool isPresentationSafe = traitOfferUi.IsModalOpen == false
                && traitOfferUi.IsPauseOverlayVisible == false;
            RunTraitEligibilityContext context = RunTraitEligibilityContextResolver.Resolve(
                _services,
                emergencyRallyActivated: false,
                secondsUntilBossSpawn: Mathf.Max(
                    0.0f,
                    bossSpawnSeconds - _services.State.ElapsedSeconds),
                isPresentationSafe: isPresentationSafe);
            RunTraitOfferPolicy policy = ResolveRunTraitOfferPolicy(
                _services.RunTraitOffers.GetPendingOpportunityIndex(_services.State.ElapsedSeconds));
            if (_services.RunTraitOffers.TryGetPendingOffer(
                    _services.State.ElapsedSeconds,
                    context,
                    policy,
                    out RunTraitOfferSnapshot snapshot))
                traitOfferUi.ShowRunTraitOffer(snapshot, HandleRunTraitSelection);
        }

        private static RunTraitOfferPolicy ResolveRunTraitOfferPolicy(int opportunityIndex)
        {
            string profileId = CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
                ? pool.ProfileId
                : CardPoolProfileIds.Standard;
            return RunTraitOfferPolicy.Resolve(profileId, opportunityIndex);
        }

        private bool HandleRunTraitSelection(string offerIdentity, int slotIndex, string traitId)
        {
            return _services.State.IsLoaded
                && _isBossPhaseActive() == false
                && _services.RunTraitOffers != null
                && _services.RunTraitOffers.TryAcceptSelection(offerIdentity, slotIndex, traitId);
        }
    }
}
