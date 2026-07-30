using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools
{
    public sealed class FtueHomeTestWindow : EditorWindow
    {
        bool _advancedFixturesExpanded;
        bool _automatedSoakExpanded;
        Vector2 _scrollPosition;
        string _hpInput = "9999";

        [MenuItem("Lizzo/FTUE Home Test")]
        public static void Open() => GetWindow<FtueHomeTestWindow>("FTUE Home Test");

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            GameScene gameScene = FindFirstObjectByType<GameScene>();
            bool isPlaying = EditorApplication.isPlaying;
            bool runLoaded = gameScene != null && gameScene.IsRunLoaded;
            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;
            bool validBattleRun = FtueHomeTestActions.IsValidBattleRun(runLoaded, activeScene.path);
            bool tutorialRun = validBattleRun && gameScene?.Services?.Context.IsTutorial == true;
            bool gameplayRun = validBattleRun && tutorialRun == false;

            EditorGUILayout.LabelField("FTUE / Home Test", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Editor-only controls. They call shared GameScene debug commands and are not compiled into Android or Release builds.", MessageType.Info);

            DrawFtueStateAndRoute(isPlaying, activeSceneName);
            DrawCombatFlow(gameScene, validBattleRun, isPlaying, runLoaded, activeScene.path);
            DrawAutomatedSoak(validBattleRun, isPlaying, runLoaded, activeScene.path);
            DrawEncounterAndResult(gameScene, validBattleRun, tutorialRun, gameplayRun, isPlaying, runLoaded, activeScene.path);
            DrawSaveAndReset(gameScene, validBattleRun, isPlaying, runLoaded, activeScene.path);
            DrawDiagnostics(gameScene, isPlaying, runLoaded, activeSceneName);
            DrawAdvancedFixtures(gameScene, validBattleRun, isPlaying, runLoaded, activeScene.path);
            EditorGUILayout.EndScrollView();
        }

        static void DrawFtueStateAndRoute(bool isPlaying, string activeSceneName)
        {
            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("FTUE State & Route", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("State", isPlaying ? "Play Mode" : "Edit Mode");
            EditorGUILayout.LabelField("Active scene", activeSceneName);
            EditorGUILayout.LabelField("Tutorial completion", FtueHomeTestActions.IsTutorialCompleted ? "Completed (returning)" : "Incomplete (fresh)");
            if (GUILayout.Button("Fresh: Reset + Play Loading -> Lobby -> Tutorial")) { FtueHomeTestActions.ResetFirstRunState(); FtueHomeTestActions.LaunchFromLoading(); }
            if (GUILayout.Button("Returning: Set Complete + Play Loading -> Lobby -> Normal")) { FtueHomeTestActions.SetReturningState(); FtueHomeTestActions.LaunchFromLoading(); }
            if (GUILayout.Button("Reload Loading With Current State")) FtueHomeTestActions.LaunchFromLoading();
        }

        void DrawCombatFlow(GameScene gameScene, bool validBattleRun, bool isPlaying, bool runLoaded, string scenePath)
        {
            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Combat Flow", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current Time Scale", $"{Time.timeScale:0.##}x");
            using (new EditorGUI.DisabledScope(!validBattleRun))
            {
                DrawHorizontalButtons(
                    () => { if (GUILayout.Button("EXP +1")) gameScene.DebugAddExperience(1); },
                    () => { if (GUILayout.Button("EXP Fill")) gameScene.DebugAddExperience(gameScene.TestRequiredExp); },
                    () => { if (GUILayout.Button("Force Level Up")) gameScene.DebugForceLevelUp(); });
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("1x")) SetTimeScalePreset(1.0f);
                if (GUILayout.Button("2x")) SetTimeScalePreset(2.0f);
                if (GUILayout.Button("5x")) SetTimeScalePreset(5.0f);
                if (GUILayout.Button("Time Scale 1x Recovery")) FtueHomeTestAutomation.SetRequestedTimeScale(1.0f);
                EditorGUILayout.EndHorizontal();
            }
            DrawRunDisabledReason(isPlaying, runLoaded, validBattleRun, scenePath);
        }

        void DrawAutomatedSoak(bool validBattleRun, bool isPlaying, bool runLoaded, string scenePath)
        {
            EditorGUILayout.Space(8.0f);
            _automatedSoakExpanded = EditorGUILayout.Foldout(_automatedSoakExpanded, "Automated Soak", true);
            if (!_automatedSoakExpanded)
                return;
            EditorGUILayout.LabelField("Auto Select Cards", FtueHomeTestAutomation.AutoSelectCardsEnabled ? "ON" : "OFF");
            EditorGUILayout.LabelField("Infinite HP", FtueHomeTestAutomation.InfiniteHpEnabled ? "ON" : "OFF");
            EditorGUILayout.LabelField("Requested speed", $"{FtueHomeTestAutomation.RequestedTimeScale:0}x");
            using (new EditorGUI.DisabledScope(!validBattleRun))
            {
                bool autoSelect = EditorGUILayout.Toggle("Auto Select Cards", FtueHomeTestAutomation.AutoSelectCardsEnabled);
                if (autoSelect != FtueHomeTestAutomation.AutoSelectCardsEnabled)
                    FtueHomeTestAutomation.SetAutoSelectCards(autoSelect);
                bool infiniteHp = EditorGUILayout.Toggle("Infinite HP", FtueHomeTestAutomation.InfiniteHpEnabled);
                if (infiniteHp != FtueHomeTestAutomation.InfiniteHpEnabled)
                    FtueHomeTestAutomation.SetInfiniteHp(infiniteHp);
                if (GUILayout.Button("Enable Soak + 5x")) FtueHomeTestAutomation.EnableSoakAtFiveTimes();
                if (GUILayout.Button("Stop Soak + 1x")) FtueHomeTestAutomation.StopSoakAndRestoreOneTimes();
            }
            DrawRunDisabledReason(isPlaying, runLoaded, validBattleRun, scenePath);
        }

        static void DrawEncounterAndResult(GameScene gameScene, bool validBattleRun, bool tutorialRun, bool gameplayRun, bool isPlaying, bool runLoaded, string scenePath)
        {
            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Encounter / Result", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!validBattleRun))
            {
                DrawHorizontalButtons(
                    () => { if (GUILayout.Button("Spawn Normal Enemy")) gameScene.DebugSpawnEnemy(Define.GOBLIN_ID); },
                    () => { if (GUILayout.Button("Spawn Elite (Red Charger)")) gameScene.DebugSpawnEnemy(Define.RED_CHARGER_ID); },
                    () => { if (GUILayout.Button("Spawn Boss (Hungry Giant)")) gameScene.DebugSpawnEnemy(Define.BOSS_ID); },
                    () => { if (GUILayout.Button("Clear Enemies")) gameScene.DebugClearEnemies(); });
                if (GUILayout.Button("Boss Visibility Test")) gameScene.DebugStartBossVisibilityFixture();
            }
            using (new EditorGUI.DisabledScope(!tutorialRun))
            {
                DrawHorizontalButtons(
                    () => { if (GUILayout.Button("Force Tutorial Clear -> Lobby")) ShowResult(gameScene, true); },
                    () => { if (GUILayout.Button("Force Tutorial Failure -> Retry")) ShowResult(gameScene, false); });
            }
            using (new EditorGUI.DisabledScope(!gameplayRun))
            {
                DrawHorizontalButtons(
                    () => { if (GUILayout.Button("Force Normal Clear -> Lobby")) ShowResult(gameScene, true); },
                    () => { if (GUILayout.Button("Force Normal Failure -> Retry")) ShowResult(gameScene, false); });
            }
            DrawRunDisabledReason(isPlaying, runLoaded, validBattleRun, scenePath);
        }

        static void DrawSaveAndReset(GameScene gameScene, bool validBattleRun, bool isPlaying, bool runLoaded, string scenePath)
        {
            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Save / Reset", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Key", FtueHomeTestActions.TutorialCompletionKey);
            DrawHorizontalButtons(
                () => { if (GUILayout.Button("Reset Tutorial Completion (Fresh)")) FtueHomeTestActions.ResetFirstRunState(); },
                () => { if (GUILayout.Button("Set Tutorial Completion (Returning)")) FtueHomeTestActions.SetReturningState(); });
            using (new EditorGUI.DisabledScope(!validBattleRun))
            {
                if (GUILayout.Button("Reset Attack Counters")) CommanderAttack.DebugResetCounters();
                if (GUILayout.Button("Reset Gem Counters")) gameScene.Services.Registry.ResetGemSpawnCounters();
            }
            DrawRunDisabledReason(isPlaying, runLoaded, validBattleRun, scenePath);
        }

        static void DrawDiagnostics(GameScene gameScene, bool isPlaying, bool runLoaded, string activeSceneName)
        {
            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scene / run", $"{activeSceneName} / {(runLoaded ? "loaded" : "not loaded")}");
            EditorGUILayout.LabelField("Time scale / background", $"{Time.timeScale:0.##}x / {Application.runInBackground}");
            if (!isPlaying || !runLoaded) return;

            RuntimeObjectRegistry registry = gameScene.Services.Registry;
            PartyService party = gameScene.Services.Party;
            PlayerController player = registry.Player;
            EditorGUILayout.LabelField("HP", player == null ? "none" : $"{player.Hp}/{player.MaxHp}");
            EditorGUILayout.LabelField("Level / EXP / time", $"{gameScene.TestCurrentRunLevel} / {gameScene.TestCollectedExp}/{gameScene.TestRequiredExp} / {gameScene.TestRunElapsedSeconds:0.0}s");
            EditorGUILayout.LabelField("Entities", $"Enemy {registry.EnemyResidualCount}, EXP {registry.ExpResidualCount}, Projectile {registry.Projectiles.Count}");
            EditorGUILayout.LabelField("Party slots", $"{party.ActiveCompanionSlotCount}/{party.ActiveCompanionSlotCap}, free {party.FreeCompanionSlots}");
            EditorGUILayout.LabelField("Attack counters", $"fire {CommanderAttack.DebugFireCount}, hit {CommanderAttack.DebugHitCount}, kill {CommanderAttack.DebugKillCount}");
            EditorGUILayout.LabelField("Gem counters", $"success {registry.DebugGemSpawnSuccesses}/{registry.DebugGemSpawnRequests}, fail {registry.DebugGemSpawnFailures}");
        }

        void DrawAdvancedFixtures(GameScene gameScene, bool validBattleRun, bool isPlaying, bool runLoaded, string scenePath)
        {
            EditorGUILayout.Space(8.0f);
            _advancedFixturesExpanded = EditorGUILayout.Foldout(_advancedFixturesExpanded, "Advanced Fixtures", true);
            if (!_advancedFixturesExpanded) return;
            EditorGUILayout.HelpBox("Available only while a Gameplay run has finished loading.", MessageType.None);
            using (new EditorGUI.DisabledScope(!validBattleRun))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Recruit Shield")) gameScene.DebugRecruit(CompanionKind.ShieldSoldier);
                if (GUILayout.Button("Recruit Sword")) gameScene.DebugRecruit(CompanionKind.Swordsman);
                if (GUILayout.Button("Recruit Cleric")) gameScene.DebugRecruit(CompanionKind.Cleric);
                if (GUILayout.Button("Recruit Archer")) gameScene.DebugRecruit(CompanionKind.Archer);
                EditorGUILayout.EndHorizontal();
                _hpInput = EditorGUILayout.TextField("Set HP", _hpInput);
                if (GUILayout.Button("Apply HP")) TrySetHp(gameScene);
                if (GUILayout.Button("Full HP")) gameScene.DebugRestoreCommanderHp();
                if (GUILayout.Button("Freeze Spawns")) gameScene.DebugSetSpawnStopped(true);
                if (GUILayout.Button("Resume Spawns")) gameScene.DebugSetSpawnStopped(false);
                if (GUILayout.Button("Spawn Gem")) gameScene.DebugSpawnGem();
            }
            DrawSynergyFixtures(gameScene, validBattleRun, isPlaying, runLoaded, scenePath);
            DrawRunDisabledReason(isPlaying, runLoaded, validBattleRun, scenePath);
        }

        void DrawSynergyFixtures(GameScene gameScene, bool validBattleRun, bool isPlaying, bool runLoaded, string scenePath)
        {
            EditorGUILayout.Space(4.0f);
            EditorGUILayout.LabelField("Synergy Fixtures", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!validBattleRun))
            {
                for (int i = 0; i < FtueHomeTestActions.SynergyFixtureDefinitions.Count; i++)
                {
                    if (i % FtueHomeTestActions.SynergyFixtureColumnCount == 0)
                        EditorGUILayout.BeginHorizontal();

                    FtueHomeTestActions.SynergyFixtureDefinition fixture = FtueHomeTestActions.SynergyFixtureDefinitions[i];
                    string label = FtueHomeTestActions.ResolveSynergyFixtureDisplayName(gameScene?.Services?.App?.Data, fixture.Id);
                    if (GUILayout.Button(label))
                        FtueHomeTestActions.TryApplySynergyFixture(gameScene, fixture.Id);

                    if (i % FtueHomeTestActions.SynergyFixtureColumnCount == FtueHomeTestActions.SynergyFixtureColumnCount - 1)
                        EditorGUILayout.EndHorizontal();
                }

                if (string.IsNullOrWhiteSpace(FtueHomeTestActions.LastSynergyFixtureStatus) == false)
                    EditorGUILayout.HelpBox(FtueHomeTestActions.LastSynergyFixtureStatus, MessageType.None);
            }
        }

        static void DrawHorizontalButtons(params Action[] buttons)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < buttons.Length; i++)
                buttons[i]?.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        void TrySetHp(GameScene gameScene) { if (int.TryParse(_hpInput, out int hp)) gameScene.DebugSetCommanderHp(hp); }

        static void SetTimeScalePreset(float value)
        {
            FtueHomeTestAutomation.SetRequestedTimeScale(value);
        }

        static void ShowResult(GameScene gameScene, bool isClear)
        {
            FtueHomeTestAutomation.ResetForRouteOrResult();
            FtueHomeTestActions.RestoreTimeScale();
            if (isClear)
                gameScene.ShowClearResult();
            else
                gameScene.ShowFailureResult(0);
        }

        static void DrawRunDisabledReason(bool isPlaying, bool runLoaded, bool validBattleRun, string scenePath)
        {
            if (!validBattleRun)
                EditorGUILayout.HelpBox(FtueHomeTestActions.GetBattleControlDisabledReason(isPlaying, runLoaded, scenePath), MessageType.None);
        }
    }
}
