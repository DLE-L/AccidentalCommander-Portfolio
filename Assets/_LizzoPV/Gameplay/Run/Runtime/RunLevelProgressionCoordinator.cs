using System;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.Flow;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class RunLevelProgressionCoordinator : IDisposable
    {
        private const float QueuedCardTransitionSeconds = 0.3f;
        private readonly RunServices _services;
        private readonly IGameplayRunUi _ui;
        private bool _cardOpen;
        private float _transitionRemaining;
        private bool _disposed;

        internal RunLevelProgressionCoordinator(RunServices services, IGameplayRunUi ui)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _ui.ModalChanged += HandleModalChanged;
        }

        internal void HandleExperienceChanged(int currentExperience, int requiredExperience)
        {
            TryOpenNextCard();
            RefreshExperienceUi();
        }

        internal void Tick(float unscaledDeltaTime)
        {
            if (_disposed || _cardOpen)
                return;

            if (_transitionRemaining > 0.0f)
                _transitionRemaining -= Mathf.Max(0.0f, unscaledDeltaTime);
            if (_transitionRemaining <= 0.0f)
                TryOpenNextCard();
        }

        private void TryOpenNextCard()
        {
            if (_disposed || _cardOpen || _transitionRemaining > 0.0f)
                return;

            RunState state = _services.State;
            if (!state.IsLoaded || !state.ExperienceEnabled || state.Experience < state.RequiredExperience)
                return;

            if (!_ui.ShowSkillSelection())
            {
                _transitionRemaining = QueuedCardTransitionSeconds;
                return;
            }

            _cardOpen = true;
            if (_services.Registry.Player != null)
                RetroVfx.Spawn(RetroVfxKind.LevelUp, _services.Registry.Player.transform.position, Vector3.zero, 1.0f);
            HitStop.Request(0.15f, "level_up_card_select");

            int completedCardCount = state.Level;
            if (_services.Definition.HasCardLimit
                && completedCardCount >= _services.Definition.TargetCardCount)
            {
                state.StopExperienceAccumulation(clearExperience: true);
                _services.Registry.ReleaseAllGems();
                return;
            }

            int nextCardNumber = completedCardCount + 1;
            int required = _services.Definition.ResolveRequiredExperience(
                _services.App.Data,
                nextCardNumber);
            state.AdvanceLevel(required);
        }

        private void HandleModalChanged(bool isOpen)
        {
            if (isOpen)
            {
                _cardOpen = true;
                return;
            }

            if (!_cardOpen)
                return;

            _cardOpen = false;
            _transitionRemaining = QueuedCardTransitionSeconds;
        }

        private void RefreshExperienceUi()
        {
            int requiredExperience = Mathf.Max(1, _services.State.RequiredExperience);
            _ui.SetExperienceStatus(
                _services.State.Level,
                _services.State.Experience,
                requiredExperience);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _ui.ModalChanged -= HandleModalChanged;
        }
    }
}
