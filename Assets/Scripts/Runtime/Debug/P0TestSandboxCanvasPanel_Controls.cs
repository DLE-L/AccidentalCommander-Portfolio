using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Debugging
{
    public sealed partial class P0TestSandboxCanvasPanel
    {
        private void CreateRunControls(Transform parent)
        {
            CreateLabel(parent, "Run / Level");
            GameObject row = CreateRow(parent, "RunButtons", 52.0f);
            CreateButton(row.transform, "ExpPlus", "EXP +1", 112.0f, 48.0f, () => RunDevButton("ExpPlus", "add_exp", () => ResolveGameScene()?.DebugAddExperience(1), "amount=1"));
            CreateButton(row.transform, "ExpFill", "EXP Fill", 124.0f, 48.0f, () =>
            {
                LogDevButtonAction("ExpFill", "fill_exp");
                GameScene scene = ResolveGameScene();
                scene?.DebugAddExperience(scene.TestRequiredExp);
            });
            CreateButton(row.transform, "LevelUp", "Level Up", 126.0f, 48.0f, () => RunDevButton("LevelUp", "force_level_up", () => ResolveGameScene()?.DebugForceLevelUp()));GameObject inputRow = CreateRow(parent, "RunInputs", 52.0f);
            _runLevelInput = CreateInput(inputRow.transform, "RunLevelInput", "1", 95.0f);
            CreateButton(inputRow.transform, "SetLevel", "Set Lv", 96.0f, 48.0f, SetRunLevelFromText);
            _runTimeInput = CreateInput(inputRow.transform, "RunTimeInput", "0", 95.0f);
            CreateButton(inputRow.transform, "SetTime", "Set Sec", 106.0f, 48.0f, SetRunTimeFromText);
        }

        private void CreatePartyControls(Transform parent)
        {
            CreateLabel(parent, "Party");
            GameObject row = CreateRow(parent, "PartyButtons", 52.0f);
            CreateButton(row.transform, "Shield", "Shield", 102.0f, 48.0f, () => RecruitFromDevButton("Shield", CompanionKind.ShieldSoldier));
            CreateButton(row.transform, "Sword", "Sword", 96.0f, 48.0f, () => RecruitFromDevButton("Sword", CompanionKind.Swordsman));
            CreateButton(row.transform, "Cleric", "Cleric", 98.0f, 48.0f, () => RecruitFromDevButton("Cleric", CompanionKind.Cleric));
            CreateButton(row.transform, "Archer", "Archer", 98.0f, 48.0f, () => RecruitFromDevButton("Archer", CompanionKind.Archer));
            CreateButton(parent, "Recruit3Shields", "Recruit 3 Shields", -1.0f, 50.0f, RecruitThreeShields);
        }

        private void CreatePlayerControls(Transform parent)
        {
            CreateLabel(parent, "Player / Commander");
            GameObject row = CreateRow(parent, "PlayerHpRow", 52.0f);
            _playerHpInput = CreateInput(row.transform, "PlayerHpInput", "9999", 122.0f);
            CreateButton(row.transform, "SetHp", "Set HP", 104.0f, 48.0f, () => SetPlayerHpFromText(Party.Registry?.Player));
            CreateButton(row.transform, "FullHp", "Full", 86.0f, 48.0f, () => RunDevButton("FullHp", "heal_player_full", HealPlayerFull));
            _keepHpButtonText = CreateButton(row.transform, "KeepHp", "Keep HP: OFF", 158.0f, 48.0f, ToggleKeepHp);

            _attackText = CreateSectionText(parent, "AttackStatus", 20.0f, 112.0f);
            GameObject attackRow = CreateRow(parent, "AttackButtons", 52.0f);
            CreateButton(attackRow.transform, "ForceShot", "Force Shot", 148.0f, 48.0f, () => RunDevButton("ForceShot", "force_commander_shot", () => FindCommanderAttack(Party.Registry?.Player)?.DebugFireProjectile()));
            CreateButton(attackRow.transform, "ResetAttack", "Reset Count", 154.0f, 48.0f, () => RunDevButton("ResetAttack", "reset_attack_counters", CommanderAttack.DebugResetCounters));
            CreateButton(attackRow.transform, "ToggleAttack", "Attack On/Off", 164.0f, 48.0f, ToggleCommanderAttack);

            GameObject visualRowA = CreateRow(parent, "VisualTestButtonsA", 52.0f);
            _noIdleTestButtonText = CreateButton(visualRowA.transform, "NoIdleTest", "No Idle: OFF", 178.0f, 48.0f, ToggleNoIdleAnimationTest);
            _attackAnimTestButtonText = CreateButton(visualRowA.transform, "AttackAnimTest", "Attack Anim: OFF", 210.0f, 48.0f, ToggleAttackAnimationTest);

            GameObject visualRowB = CreateRow(parent, "VisualTestButtonsB", 52.0f);
            _shieldAreaPushTestButtonText = CreateButton(visualRowB.transform, "ShieldAreaPushTest", "Shield AoE: OFF", 204.0f, 48.0f, ToggleShieldAreaPushTest);
            _bossAnimTestButtonText = CreateButton(visualRowB.transform, "BossAttackMotionTest", "Boss Anim: OFF", 202.0f, 48.0f, ToggleBossAttackMotionTest);
        }

        private void CreateEnemyControls(Transform parent)
        {
            CreateLabel(parent, "Enemies");
            GameObject row = CreateRow(parent, "EnemyButtonsA", 52.0f);
            CreateButton(row.transform, "Goblin", "Goblin", 102.0f, 48.0f, () => SpawnEnemy("Goblin", Define.GOBLIN_ID, Party.Registry?.Player));
            CreateButton(row.transform, "Wolf", "Wolf", 96.0f, 48.0f, () => SpawnEnemy("Wolf", Define.SNAKE_ID, Party.Registry?.Player));
            CreateButton(row.transform, "Orc", "Orc", 86.0f, 48.0f, () => SpawnEnemy("Orc", Define.ORC_ID, Party.Registry?.Player));
            CreateButton(row.transform, "Clear", "Clear", 92.0f, 48.0f, () => RunDevButton("Clear", "despawn_all_monsters", () => Party.Registry.ReleaseAllEnemies()));

            GameObject rowB = CreateRow(parent, "EnemyButtonsB", 52.0f);
            CreateButton(rowB.transform, "RedCharger", "Red Charger", 152.0f, 48.0f, () => SpawnEnemy("RedCharger", Define.RED_CHARGER_ID, Party.Registry?.Player));
            CreateButton(rowB.transform, "Boss", "Boss", 92.0f, 48.0f, () => SpawnEnemy("Boss", Define.BOSS_ID, Party.Registry?.Player));
            CreateButton(rowB.transform, "SpawnGem", "Gem", 82.0f, 48.0f, () => SpawnGem(Party.Registry?.Player));
            CreateButton(rowB.transform, "ResetGem", "Gem Reset", 126.0f, 48.0f, () => RunDevButton("ResetGem", "reset_gem_counters", Party.Registry.ResetGemSpawnCounters));

            GameObject toggleRow = CreateRow(parent, "DebugToggleButtons", 52.0f);
            _freezeSpawnButtonText = CreateButton(toggleRow.transform, "FreezeSpawns", "Freeze: OFF", 156.0f, 48.0f, ToggleFreezeSpawns);
            _gizmoButtonText = CreateButton(toggleRow.transform, "CombatGizmos", "Gizmos: OFF", 156.0f, 48.0f, ToggleCombatGizmos);
            CreateButton(parent, "BossVisibilityTest", "Boss Visibility Test", -1.0f, 50.0f, StartBossVisibilityTest);
        }

        private void CreateTimeControls(Transform parent)
        {
            CreateLabel(parent, "Time Scale");
            GameObject row = CreateRow(parent, "TimeScaleButtons", 52.0f);
            CreateButton(row.transform, "Time025", "0.25x", 92.0f, 48.0f, () => SetTimeScaleFromDevButton("Time025", 0.25f));
            CreateButton(row.transform, "Time1", "1x", 74.0f, 48.0f, () => SetTimeScaleFromDevButton("Time1", 1.0f));
            CreateButton(row.transform, "Time2", "2x", 74.0f, 48.0f, () => SetTimeScaleFromDevButton("Time2", 2.0f));
            CreateButton(row.transform, "Time5", "5x", 74.0f, 48.0f, () => SetTimeScaleFromDevButton("Time5", 5.0f));
            _timeScaleInput = CreateInput(row.transform, "TimeScaleInput", "1", 80.0f);
            CreateButton(row.transform, "SetTimeScale", "Set", 72.0f, 48.0f, SetTimeScaleFromText);
        }

    }
}
