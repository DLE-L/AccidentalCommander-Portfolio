using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools
{
    public sealed class FtueHomeTestWindow : EditorWindow
    {
        bool _advancedFixturesExpanded;
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
            if (GUILayout.Button("Fresh: Reset Account + Play Loading -> Tutorial")) { FtueHomeTestActions.ResetFirstRunState(); FtueHomeTestActions.LaunchFromLoading(); }
            if (GUILayout.Button("Returning: Set Complete + Play Loading -> Lobby")) { FtueHomeTestActions.SetReturningState(); FtueHomeTestActions.LaunchFromLoading(); }
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
                    () => { if (GUILayout.Button("Spawn Normal Enemy")) gameScene.DebugSpawnEnemy(Define.GOBLIN_ID, EnemyEncounterRank.Normal); },
                    () => { if (GUILayout.Button("Spawn Elite (Red Charger)")) gameScene.DebugSpawnEnemy(Define.RED_CHARGER_ID, EnemyEncounterRank.Elite); },
                    () => { if (GUILayout.Button("Spawn Boss (Hungry Giant)")) gameScene.DebugSpawnEnemy(Define.BOSS_ID, EnemyEncounterRank.Boss); },
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
                () => { if (GUILayout.Button("Reset FTUE + Account Progress (Fresh)")) FtueHomeTestActions.ResetFirstRunState(); },
                () => { if (GUILayout.Button("Set Tutorial Completion (Returning)")) FtueHomeTestActions.SetReturningState(); });
            using (new EditorGUI.DisabledScope(!validBattleRun))
            {
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
            CommanderActor player = registry.Player;
            EditorGUILayout.LabelField("HP", player == null ? "none" : $"{player.Hp}/{player.MaxHp}");
            EditorGUILayout.LabelField("Level / EXP / time", $"{gameScene.TestCurrentRunLevel} / {gameScene.TestCollectedExp}/{gameScene.TestRequiredExp} / {gameScene.TestRunElapsedSeconds:0.0}s");
            EditorGUILayout.LabelField("Entities", $"Enemy {registry.EnemyResidualCount}, EXP {registry.ExpResidualCount}, Projectile {registry.Projectiles.Count}");
            EditorGUILayout.LabelField("Party slots", $"{party.ActiveCompanionSlotCount}/{party.ActiveCompanionSlotCap}, free {party.FreeCompanionSlots}");
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
                if (GUILayout.Button("Recruit Shield")) gameScene.DebugRecruit("shield_guard");
                if (GUILayout.Button("Recruit Sword")) gameScene.DebugRecruit("sword_soldier");
                if (GUILayout.Button("Recruit Cleric")) gameScene.DebugRecruit("cleric");
                if (GUILayout.Button("Recruit Archer")) gameScene.DebugRecruit("falcon_archer");
                EditorGUILayout.EndHorizontal();
                _hpInput = EditorGUILayout.TextField("Set HP", _hpInput);
                if (GUILayout.Button("Apply HP")) TrySetHp(gameScene);
                if (GUILayout.Button("Full HP")) gameScene.DebugRestoreCommanderHp();
                if (GUILayout.Button("Freeze Spawns")) gameScene.DebugSetSpawnStopped(true);
                if (GUILayout.Button("Resume Spawns")) gameScene.DebugSetSpawnStopped(false);
                if (GUILayout.Button("Spawn Gem")) gameScene.DebugSpawnGem();
            }
            DrawRunDisabledReason(isPlaying, runLoaded, validBattleRun, scenePath);
        }

        static void DrawHorizontalButtons(params Action[] buttons)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < buttons.Length; i++)
                buttons[i]?.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        void TrySetHp(GameScene gameScene) { if (int.TryParse(_hpInput, out int hp)) gameScene.DebugSetCommanderHp(hp); }

        static void ShowResult(GameScene gameScene, bool isClear)
        {
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
