using System;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class RunLevelProgressionCoordinator : IDisposable
    {
        private readonly RunServices _services;
        private readonly IGameplayRunUi _ui;
        private int _pendingRewardCount;
        private bool _rewardModalOpen;
        private bool _processingExperience;
        private bool _disposed;

        internal RunLevelProgressionCoordinator(RunServices services, IGameplayRunUi ui)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _ui.ModalChanged += HandleModalChanged;
        }

        internal void HandleExperienceChanged(int currentExperience, int requiredExperience)
        {
            if (_disposed)
                return;

            if (_processingExperience)
            {
                RefreshExperienceUi();
                return;
            }

            _processingExperience = true;
            try
            {
                while (_services.State.IsLoaded
                    && _services.State.Experience >= _services.State.RequiredExperience)
                {
                    int nextLevel = _services.State.Level + 1;
                    _services.State.AdvanceLevel(Mathf.Max(1, _services.App.Data.GetLevelExp(nextLevel)));
                    _pendingRewardCount++;

                    if (_services.Registry.Player != null)
                        RetroVfx.Spawn(RetroVfxKind.LevelUp, _services.Registry.Player.transform.position, Vector3.zero, 1.0f);
                }
            }
            finally
            {
                _processingExperience = false;
            }

            TryPresentNextReward();
            RefreshExperienceUi();
        }

        internal void Tick()
        {
            if (_disposed || _services.State.IsLoaded == false || _rewardModalOpen)
                return;

            TryPresentNextReward();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _ui.ModalChanged -= HandleModalChanged;
            _pendingRewardCount = 0;
            _rewardModalOpen = false;
        }

        private void HandleModalChanged(bool isOpen)
        {
            _rewardModalOpen = isOpen;
        }

        private void TryPresentNextReward()
        {
            while (_pendingRewardCount > 0 && _rewardModalOpen == false)
            {
                _pendingRewardCount--;
                if (_ui.ShowSkillSelection())
                {
                    _rewardModalOpen = true;
                    HitStop.Request(0.15f, "level_up_card_select");
                    return;
                }
            }
        }

        private void RefreshExperienceUi()
        {
            int requiredExperience = Mathf.Max(1, _services.State.RequiredExperience);
            _ui.SetExperienceStatus(
                _services.State.Level,
                _services.State.Experience,
                requiredExperience);
        }
    }
}
