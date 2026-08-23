using System;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class RunLevelProgressionCoordinator
    {
        private readonly RunServices _services;
        private readonly IGameplayRunUi _ui;

        internal RunLevelProgressionCoordinator(RunServices services, IGameplayRunUi ui)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        internal void HandleExperienceChanged(int currentExperience, int requiredExperience)
        {
            if (currentExperience >= requiredExperience)
            {
                int nextLevel = _services.State.Level + 1;
                _services.State.AdvanceLevel(Mathf.Max(1, _services.App.Data.GetLevelExp(nextLevel)));

                if (_services.Registry.Player != null)
                    RetroVfx.Spawn(RetroVfxKind.LevelUp, _services.Registry.Player.transform.position, Vector3.zero, 1.0f);

                if (_ui.ShowSkillSelection())
                    HitStop.Request(0.15f, "level_up_card_select");
            }

            RefreshExperienceUi();
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
