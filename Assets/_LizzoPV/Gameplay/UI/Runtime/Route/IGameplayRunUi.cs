using Lizzo.PV.Gameplay.Units;
using System;
using Lizzo.PV.Flow;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    public interface IGameplayRunUiFeedback
    {
        bool IsThreatDirectionVisible { get; }

        void ShowBossPreWarning(string text, Color accentColor, float durationSeconds, bool showEdges);
        void HideBossPreWarning();
        void ShowThreatDirection(Transform target, string label, Color accentColor, float durationSeconds = 0.0f);
        void HideThreatDirection();
    }

    public interface IGameplayRunUi : IGameplayRunUiFeedback
    {
        event Action<bool> ModalChanged;
        event Action MaxBuildCompleteBannerRequested;

        bool Initialize(RunServices services, Camera worldCamera, RunPauseController pauseController);
        void ShowGameplay();
        void BindPlayer(CommanderActor player);
        bool ShowSkillSelection();
        bool ShowResult(RunResultViewData data, Action mainRequested);
        void CloseModal();
        void SetPauseOverlay(bool visible, bool fromAppBackground);
        void SetGameplaySpeed(float speed);
        void SetRunStatus(int kills, float survivalSeconds);
        void SetExperienceStatus(int level, float currentExperience, float requiredExperience);
        void ShowBoss(string name, int hp, int maxHp);
        void HideBoss();
        void HideGameplay();
    }
}
