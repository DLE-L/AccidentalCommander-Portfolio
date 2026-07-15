using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Debugging
{
    public sealed partial class P0TestSandboxCanvasPanel
    {
        private void SetActiveTab(bool logTab)
        {
            _isLogTabActive = logTab;

            if (_logTabObject != null)
                _logTabObject.SetActive(_isLogTabActive);
            if (_controlsTabObject != null)
                _controlsTabObject.SetActive(_isLogTabActive == false);

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1.0f;

            RefreshToggleLabels();
        }

        private void RefreshAll()
        {
            _gameScene = ResolveGameScene();
            RefreshStatus();
            RefreshQa();
            RefreshAttack();
            RefreshToggleLabels();
        }

        private void RefreshStatus()
        {
            PlayerController player = Party.Registry?.Player;
            bool isLoaded = _gameScene != null && _gameScene.IsRunLoaded;
            string hpText = player == null ? "none" : $"{player.Hp}/{player.MaxHp}";
            string expText = _gameScene == null ? "none" : $"{_gameScene.TestCollectedExp}/{_gameScene.TestRequiredExp}";
            string levelText = _gameScene == null ? "none" : _gameScene.TestCurrentRunLevel.ToString();
            string timeText = _gameScene == null ? "none" : Mathf.FloorToInt(_gameScene.TestRunElapsedSeconds).ToString();
            int monsterCount = Party.Registry?.Enemies.Count ?? 0;
            int gemCount = Party.Registry?.Gems.Count ?? 0;
            string timeScaleText = _isPanelPauseApplied ? $"Paused (resume {_resumeTimeScale:0.##}x)" : $"{Time.timeScale:0.##}x";

            _statusText.text =
                $"Loaded: {isLoaded}\n" +
                $"Level: {levelText}  EXP: {expText}  Time: {timeText}s  Scale: {timeScaleText}\n" +
                $"Player HP: {hpText}  Monsters: {monsterCount}  Gems: {gemCount}\n" +
                $"Slots: {Party.ActiveCompanionSlotCount}/{Party.ActiveCompanionSlotCap}  Free: {Party.FreeCompanionSlots}\n" +
                $"Gem Spawn: {Party.Registry.DebugGemSpawnSuccesses}/{Party.Registry.DebugGemSpawnRequests}  Fail: {Party.Registry.DebugGemSpawnFailures}";
        }

        private void RefreshAttack()
        {
            CommanderAttack commanderAttack = FindCommanderAttack(Party.Registry?.Player);
            int projectileCount = Party.Registry?.Projectiles.Count ?? 0;
            string skillState = commanderAttack == null ? "missing" : "active";
            _attackText.text =
                $"Attack: {skillState}  Enabled: {CommanderAttack.DebugAttackEnabled}  Projectiles: {projectileCount}\n" +
                $"Fired: {CommanderAttack.DebugFireCount}  Hit: {CommanderAttack.DebugHitCount}  Kill: {CommanderAttack.DebugKillCount}\n" +
                $"Last Fire: {FormatTimeAgo(CommanderAttack.DebugLastFireTime)}  Last Hit: {FormatTimeAgo(CommanderAttack.DebugLastHitTime)}";
        }

        private void RefreshToggleLabels()
        {
            if (_keepHpButtonText != null)
                _keepHpButtonText.text = _keepPlayerHpFull ? "Keep HP: ON" : "Keep HP: OFF";
            if (_freezeSpawnButtonText != null)
                _freezeSpawnButtonText.text = _freezeSpawns ? "Freeze: ON" : "Freeze: OFF";
            if (_gizmoButtonText != null)
                _gizmoButtonText.text = P0CombatDebugSettings.ShowCombatGizmos ? "Gizmos: ON" : "Gizmos: OFF";
            if (_noIdleTestButtonText != null)
                _noIdleTestButtonText.text = P0CombatDebugSettings.NoIdleAnimationTestEnabled ? "No Idle: ON" : "No Idle: OFF";
            if (_attackAnimTestButtonText != null)
                _attackAnimTestButtonText.text = P0CombatDebugSettings.AttackAnimationTestEnabled ? "Attack Anim: ON" : "Attack Anim: OFF";
            if (_shieldAreaPushTestButtonText != null)
                _shieldAreaPushTestButtonText.text = P0CombatDebugSettings.ShieldSoldierAreaPushTestEnabled ? "Shield AoE: ON" : "Shield AoE: OFF";
            if (_bossAnimTestButtonText != null)
                _bossAnimTestButtonText.text = P0CombatDebugSettings.BossAttackMotionTestEnabled ? "Boss Anim: ON" : "Boss Anim: OFF";
            if (_logTabButtonText != null)
                _logTabButtonText.text = _isLogTabActive ? "[Log]" : "Log";
            if (_controlsTabButtonText != null)
                _controlsTabButtonText.text = _isLogTabActive ? "Controls" : "[Controls]";
            if (_timeScaleInput != null && _timeScaleInput.isFocused == false)
                _timeScaleInput.text = (_isPanelPauseApplied ? _resumeTimeScale : Time.timeScale).ToString("0.##");
        }

    }
}
