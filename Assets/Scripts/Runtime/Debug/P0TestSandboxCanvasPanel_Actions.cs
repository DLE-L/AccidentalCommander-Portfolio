using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using System;
using UnityEngine;

namespace Lizzo.PV.P0.Debugging
{
    public sealed partial class P0TestSandboxCanvasPanel
    {
        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_panelObject != null)
                _panelObject.SetActive(_isVisible);
            if (_collapsedButtonObject != null)
                _collapsedButtonObject.SetActive(SHOW_COLLAPSED_BUTTON && _isVisible == false);
            ApplyPanelPause();
            LogHiddenStateForCurrentRun();
            RefreshAll();
        }

        private void LogHiddenStateForCurrentRun()
        {
            if (_hasLoggedHiddenForRun || _isVisible || P0Telemetry.HasLogged(P0Telemetry.RunStart) == false)
                return;

            _hasLoggedHiddenForRun = true;
            P0Telemetry.Log(
                P0Telemetry.DebugOverlayHidden,
                "source=test_sandbox_canvas",
                $"collapsed_button_visible={SHOW_COLLAPSED_BUTTON.ToString().ToLowerInvariant()}");
        }

        private GameScene ResolveGameScene()
        {
            if (_gameScene != null)
                return _gameScene;

            _gameScene = FindFirstObjectByType<GameScene>();
            return _gameScene;
        }

        private void SetRunLevelFromText()
        {
            if (int.TryParse(_runLevelInput.text, out int level) == false)
                return;

            LogDevButtonAction("SetLevel", "set_run_level", $"value={level}");
            ResolveGameScene()?.DebugSetRunLevel(level);
        }

        private void SetRunTimeFromText()
        {
            if (float.TryParse(_runTimeInput.text, out float seconds) == false)
                return;

            LogDevButtonAction("SetTime", "set_run_time", $"value={seconds:0.##}");
            ResolveGameScene()?.DebugSetRunElapsedSeconds(seconds);
        }

        private void SetPlayerHpFromText(PlayerController player)
        {
            if (player == null || int.TryParse(_playerHpInput.text, out int hp) == false)
                return;

            LogDevButtonAction("SetHp", "set_player_hp", $"value={hp}");
            player.MaxHp = Mathf.Max(player.MaxHp, hp);
            player.Hp = Mathf.Clamp(hp, 1, player.MaxHp);
        }

        private void ToggleKeepHp()
        {
            _keepPlayerHpFull = !_keepPlayerHpFull;
            LogDevButtonAction("KeepHp", "toggle_keep_hp", $"enabled={_keepPlayerHpFull.ToString().ToLowerInvariant()}");
            if (_keepPlayerHpFull)
                HealPlayerFull();
            RefreshToggleLabels();
        }

        private void ToggleFreezeSpawns()
        {
            _freezeSpawns = !_freezeSpawns;
            LogDevButtonAction("FreezeSpawns", "toggle_freeze_spawns", $"enabled={_freezeSpawns.ToString().ToLowerInvariant()}");
            ResolveGameScene()?.DebugSetSpawnStopped(_freezeSpawns);
            RefreshToggleLabels();
        }

        private void ToggleCombatGizmos()
        {
            P0CombatDebugSettings.ShowCombatGizmos = !P0CombatDebugSettings.ShowCombatGizmos;
            LogDevButtonAction("CombatGizmos", "toggle_combat_gizmos", $"enabled={P0CombatDebugSettings.ShowCombatGizmos.ToString().ToLowerInvariant()}");
            RefreshToggleLabels();
        }

        private void ToggleNoIdleAnimationTest()
        {
            P0CombatDebugSettings.NoIdleAnimationTestEnabled = !P0CombatDebugSettings.NoIdleAnimationTestEnabled;
            LogDevButtonAction("NoIdleTest", "toggle_no_idle_animation_test", $"enabled={P0CombatDebugSettings.NoIdleAnimationTestEnabled.ToString().ToLowerInvariant()}");
            RefreshToggleLabels();
        }

        private void ToggleAttackAnimationTest()
        {
            P0CombatDebugSettings.AttackAnimationTestEnabled = !P0CombatDebugSettings.AttackAnimationTestEnabled;
            LogDevButtonAction("AttackAnimTest", "toggle_attack_animation_test", $"enabled={P0CombatDebugSettings.AttackAnimationTestEnabled.ToString().ToLowerInvariant()}");
            RefreshToggleLabels();
        }

        private void ToggleShieldAreaPushTest()
        {
            P0CombatDebugSettings.ShieldSoldierAreaPushTestEnabled = !P0CombatDebugSettings.ShieldSoldierAreaPushTestEnabled;
            LogDevButtonAction("ShieldAreaPushTest", "toggle_shield_area_push_test", $"enabled={P0CombatDebugSettings.ShieldSoldierAreaPushTestEnabled.ToString().ToLowerInvariant()}");
            Party.RefreshShieldSoldierAreaPushTest();
            RefreshToggleLabels();
        }

        private void ToggleBossAttackMotionTest()
        {
            P0CombatDebugSettings.BossAttackMotionTestEnabled = !P0CombatDebugSettings.BossAttackMotionTestEnabled;
            LogDevButtonAction("BossAttackMotionTest", "toggle_boss_attack_motion_test", $"enabled={P0CombatDebugSettings.BossAttackMotionTestEnabled.ToString().ToLowerInvariant()}");
            RefreshToggleLabels();
        }

        private void HealPlayerFull()
        {
            PlayerController player = Party.Registry?.Player;
            if (player == null)
                return;

            player.Hp = player.MaxHp;
        }

        private static CommanderAttack FindCommanderAttack(PlayerController player)
        {
            if (player == null)
                return null;

            return player.GetComponent<CommanderAttack>();
        }

        private static string FormatTimeAgo(float timestamp)
        {
            if (timestamp < 0.0f)
                return "none";

            return $"{Mathf.Max(0.0f, Time.time - timestamp):0.0}s ago";
        }

        private void ToggleCommanderAttack()
        {
            CommanderAttack.DebugAttackEnabled = !CommanderAttack.DebugAttackEnabled;
            LogDevButtonAction("ToggleAttack", "toggle_commander_attack", $"enabled={CommanderAttack.DebugAttackEnabled.ToString().ToLowerInvariant()}");
        }

        private void RecruitFromDevButton(string buttonId, CompanionKind kind)
        {
            LogDevButtonAction(buttonId, "recruit_companion", $"companion={kind}");
            Party.Recruit(kind);
        }

        private void RecruitThreeShields()
        {
            LogDevButtonAction("Recruit3Shields", "recruit_companion_batch", "companion=ShieldSoldier", "count=3");
            Party.Recruit(CompanionKind.ShieldSoldier);
            Party.Recruit(CompanionKind.ShieldSoldier);
            Party.Recruit(CompanionKind.ShieldSoldier);
        }

        private void StartBossVisibilityTest()
        {
            LogDevButtonAction(
                "BossVisibilityTest",
                "boss_visibility_test",
                "spawn_stopped=true",
                "shield_captains=1",
                "clerics=1",
                "swordsmen=1",
                "archers=2");

            Party.Recruit(CompanionKind.ShieldSoldier);
            Party.Recruit(CompanionKind.ShieldSoldier);
            Party.Recruit(CompanionKind.ShieldSoldier);
            Party.Recruit(CompanionKind.Cleric);
            Party.Recruit(CompanionKind.Swordsman);
            Party.Recruit(CompanionKind.Archer);
            Party.Recruit(CompanionKind.Archer);

            ResolveGameScene()?.DebugSetSpawnStopped(true);

            BossSpawnController bossSpawnController = FindFirstObjectByType<BossSpawnController>();
            if (bossSpawnController == null)
            {
                Debug.LogError("Boss visibility test could not find BossSpawnController.");
                return;
            }

            bossSpawnController.DebugJumpToHungryGiantPrelude();
        }

        private void SetTimeScaleFromText()
        {
            if (float.TryParse(_timeScaleInput.text, out float value) == false)
                return;

            LogDevButtonAction("SetTimeScale", "set_time_scale", $"value={value:0.##}");
            SetTimeScale(value);
        }

        private void SetTimeScaleFromDevButton(string buttonId, float value)
        {
            LogDevButtonAction(buttonId, "set_time_scale", $"value={value:0.##}");
            SetTimeScale(value);
        }

        private void SetTimeScale(float value)
        {
            _resumeTimeScale = Mathf.Clamp(value, 0.1f, 10.0f);
            if (_isPanelPauseApplied == false)
                Time.timeScale = _resumeTimeScale;

            if (_timeScaleInput != null)
                _timeScaleInput.text = _resumeTimeScale.ToString("0.##");
        }

        private void RunDevButton(string buttonId, string action, Action callback, params string[] parameters)
        {
            LogDevButtonAction(buttonId, action, parameters);
            callback?.Invoke();
        }

        private void LogDevButtonAction(string buttonId, string action, params string[] parameters)
        {
            string[] slotState = Party.BuildSlotStateParameters("dev_button_action");
            int extraCount = parameters == null ? 0 : parameters.Length;
            string[] logParameters = new string[10 + slotState.Length + extraCount];
            int index = 0;
            logParameters[index++] = P0Telemetry.RunTimeSecondsParameter;
            logParameters[index++] = $"button={buttonId}";
            logParameters[index++] = $"action={action}";
            logParameters[index++] = $"legion={Party.BuildLegionSummary().Replace(' ', '_')}";
            logParameters[index++] = $"guard_active={Party.IsGuardSquadActivated.ToString().ToLowerInvariant()}";
            logParameters[index++] = $"shield_soldiers={Party.ShieldSoldierCount}";
            logParameters[index++] = $"shield_captains={Party.ShieldCaptainCount}";
            logParameters[index++] = $"swordsmen={Party.SwordsmanCount}";
            logParameters[index++] = $"clerics={Party.ClericCount}";
            logParameters[index++] = $"archers={Party.ArcherCount}";

            for (int i = 0; i < slotState.Length; i++)
                logParameters[index++] = slotState[i];

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                    logParameters[index++] = parameters[i];
            }

            P0Telemetry.Log(P0Telemetry.DevButtonAction, logParameters);
        }

        private void OnDestroy()
        {
            if (Mathf.Approximately(Time.timeScale, 1.0f) == false)
                Time.timeScale = 1.0f;
        }
    }
}
