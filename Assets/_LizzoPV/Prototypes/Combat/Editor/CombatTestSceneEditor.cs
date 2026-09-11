using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lizzo.PV.Prototypes.Combat.EditorTools
{
    [CustomEditor(typeof(CombatTestScene))]
    public sealed class CombatTestSceneEditor : UnityEditor.Editor
    {
        public const string ScenePath = "Assets/_LizzoPV/Prototypes/Combat/CombatTest.unity";

        [MenuItem("Lizzo/Combat Test/Open Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play Mode before opening CombatTest."); return; }
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            { Debug.LogWarning("Close the current Prefab Stage before opening CombatTest."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            Selection.activeGameObject = Object.FindFirstObjectByType<CombatTestScene>().gameObject;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var scene = (CombatTestScene)target;
            EditorGUILayout.HelpBox("Editor-only combat lab. Enter Play Mode. Scenario/positions apply on Reset. " +
                "Attack, manual AI, infinite HP and repeat settings apply live. WASD/arrows require Game view focus.", MessageType.Info);
            EditorGUILayout.LabelField("Status", scene.Status, EditorStyles.wordWrappedLabel);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || !scene.IsReady))
            {
                if (GUILayout.Button("Reset / Respawn Scenario")) scene.ResetScenario();
                EditorGUILayout.HelpBox("Promotion Test Unit에서 군단원을 선택하고 아래 버튼을 누르면 진급 분대로 시작합니다. 기본 공격과 특수공격을 함께 실행하며, Reset은 동일한 진급 구성을 재시작합니다.", MessageType.Info);
                if (GUILayout.Button("선택한 군단원 진급 테스트 시작")) scene.StartSelectedPromotionTest();
                if (GUILayout.Button("방패병 진급 + 전용 패시브 4종 테스트")) scene.StartShieldGuardPassiveTest();
                EditorGUILayout.HelpBox("방패병 버튼은 범위·밀치기·복귀 타격·근접 피해 패시브를 적용합니다. 6초 게이지와 충격파를 확인하세요. 복귀 타격은 실제 복귀 경로에 있는 적만 맞습니다. Reset은 패시브를 지우므로 재연하려면 이 버튼을 다시 누르세요.", MessageType.Info);
                if (scene.PromotionRangeTarget != null)
                    EditorGUILayout.LabelField("회전 공격 경로 표적 HP", scene.PromotionRangeTarget.Hp.ToString());
                if (GUILayout.Button("Hungry Giant 공격 목록 테스트 시작")) scene.StartGiantAttackListScenario();
                if (GUILayout.Button("Red Charger 공격 목록 테스트 시작")) scene.StartPatternAttackListScenario(false);
                if (GUILayout.Button("Wolf 공격 목록 테스트 시작")) scene.StartPatternAttackListScenario(true);
                EditorGUILayout.HelpBox("Pattern Attack Set: Authored=접촉+돌진, Contact Only=접촉만, Charge Only=돌진만. 선택 후 해당 적의 목록 테스트 시작을 누르세요. Red의 경고 중 접촉과 Wolf의 접촉 우선 규칙은 기존대로 유지됩니다.", MessageType.Info);
                EditorGUILayout.HelpBox("Giant Attack Set을 고른 뒤 목록 테스트 시작 또는 Reset을 누르세요. Authored는 프리팹 기본 목록을 사용합니다. 이 버튼은 자동 AI로 시작합니다.", MessageType.Info);
                if (GUILayout.Button("검병 진급 + 전용 패시브·관통 표적 테스트")) scene.StartSwordPassiveTest();
                EditorGUILayout.HelpBox("검병 패시브 테스트: 3칸 누적·잔상 추가 베기·검기 관통을 7개 표적으로 비교합니다. Reset은 패시브와 추가 표적을 지우므로 같은 버튼으로 재시작하세요.", MessageType.Info);
                if (GUILayout.Button("검병 기본 공격 테스트 시작")) scene.StartSwordScenario();
                if (GUILayout.Button("검병 교대: 1명")) scene.StartSwordTurnTest(1);
                if (GUILayout.Button("검병 교대: 2명")) scene.StartSwordTurnTest(2);
                if (GUILayout.Button("검병 교대: 진급 3명")) scene.StartSwordTurnTest(3);
                if (GUILayout.Button("검병 교대: 21명 혼합 편성")) scene.StartSwordTurnTest(3, true);
                if (GUILayout.Button("늑대 조련사 진급 처치 테스트 시작")) scene.StartWolfTamerScenario();
                if (GUILayout.Button("사령술사 진급 저주 사망 테스트 시작")) scene.StartNecromancerScenario();
                if (GUILayout.Button("사령술사 다중 당김 재현 (6명 중 무작위 4명)")) scene.StartCursePullReplay();
                EditorGUILayout.LabelField("당김 재현", scene.CursePullReplayStatus, EditorStyles.wordWrappedLabel);
                using (new EditorGUI.DisabledScope(scene.SummonReplayRunning))
                    if (GUILayout.Button("사령술사 소환 재현 (저주·처치 자동 3회)")) scene.StartSummonReplay();
                EditorGUILayout.LabelField("소환 재현", scene.SummonReplayStatus, EditorStyles.wordWrappedLabel);
                if (scene.IsCurseScenario)
                    EditorGUILayout.LabelField("현재 표적 저주", scene.Enemy != null && scene.Enemy.EditorHasNecromancerCurse ? "적용 중" : "없음");
                if (GUILayout.Button("약초사 진급 취약 전파 테스트 시작")) scene.StartHerbalistScenario();
                if (scene.IsSpreadScenario)
                {
                    EditorGUILayout.HelpBox("기본 표적 3명은 세로 2m 간격이며, 패시브 테스트는 첫 표적 주위 반경 2.3에 6명을 추가합니다. 첫 표적에 취약이 적용되면 Pause 후 처치하여 두 번째 표적으로 전파되는지 확인하세요. 전파된 두 번째 표적을 처치해도 세 번째로 재전파되지 않아야 합니다. 후속 약병에 직접 맞으면 새 취약이므로 구분해서 확인하세요. Reset으로 다시 시작합니다.", MessageType.Info);
                    for (int i = 0; i < scene.SpreadTestTargets.Count; i++)
                    {
                        var target = scene.SpreadTestTargets[i];
                        EditorGUILayout.LabelField($"전파 표적 {i + 1} HP / 받는 피해 배율", target == null ? "-" : $"{target.Hp} / x{target.ResolveCompanionIncomingDamageMultiplier(Time.time):0.00}");
                        using (new EditorGUI.DisabledScope(target == null || target.Hp <= 0))
                            if (GUILayout.Button($"전파 표적 {i + 1} 처치")) scene.KillSpreadTarget(i);
                    }
                }
                if (scene.IsKillScenario)
                {
                    EditorGUILayout.HelpBox(scene.IsCurseScenario
                        ? "저주탄에 맞은 표적을 Kill Enemy로 처치하고 재생성하세요. 사령술사 주변에서 저주 상태로 3회 사망하면 해골 3기를 소환합니다. 저주 없는 처치와 범위 밖 사망은 제외됩니다. Curse Target Hp로 표적 체력을 조절하고 Reset으로 누적·소환을 초기화하세요."
                        : "늑대 조련사의 실제 처치 3회로 특수공격을 준비합니다. 표적 사망 후 재생성하면 누적이 유지됩니다. Kill Target Hp를 높여 다음 표적에서 특수공격을 확인하세요. 아래 Kill Enemy 버튼은 군단장 처치이므로 진급 조건에 포함되지 않습니다. Reset은 누적도 초기화합니다.", MessageType.Info);
                    using (new EditorGUI.DisabledScope(scene.Enemy != null && scene.Enemy.Hp > 0))
                        if (GUILayout.Button("처치 표적만 재생성 (누적 유지)")) scene.RespawnKillTarget();
                    EditorGUILayout.LabelField(scene.IsCurseScenario ? "전체 처치 / 의식 대기 / 의식 해골" : "전체 처치 / 무리 공격 대기",
                        scene.IsCurseScenario ? $"{scene.KillCount} / {scene.PendingRitualCount} / {scene.RitualSummonCount}" : $"{scene.KillCount} / {scene.PendingPackAssaultCount}");
                }
                if (GUILayout.Button("매 궁수 투사체 테스트 시작")) scene.StartFalconScenario();
                if (GUILayout.Button("적 투사체 테스트 시작")) scene.StartEnemyProjectileScenario();
                if (GUILayout.Button("폭탄병 범위 공격 테스트 시작")) scene.StartBombardierScenario();
                if (GUILayout.Button("화염 마법사 장판 테스트 시작")) scene.StartFireMageScenario();
                if (GUILayout.Button("적 장판 테스트 시작")) scene.StartEnemyFieldScenario();
                if (GUILayout.Button("번개 마법사 연쇄 공격 테스트 시작")) scene.StartLightningMageScenario();
                if (GUILayout.Button("해골 낫 왕복 공격 테스트 시작")) scene.StartSkeletonScytheScenario();
                if (GUILayout.Button("폭탄병 진급 + 전용 패시브 4종 테스트")) scene.StartBombardierPassiveTest();
                if (GUILayout.Button("화염술사 진급 + 전용 패시브 4종 테스트")) scene.StartFireMagePassiveTest();
                if (GUILayout.Button("번개술사 진급 + 전용 패시브 4종 테스트")) scene.StartLightningMagePassiveTest();
                if (GUILayout.Button("늑대 조련사 진급 + 전용 패시브 4종 테스트")) scene.StartWolfTamerPassiveTest();
                if (GUILayout.Button("망령 기사 진급 + 전용 패시브 4종 테스트")) scene.StartWraithKnightPassiveTest();
                if (GUILayout.Button("약초사 전파 + 전용 패시브 3종 테스트")) scene.StartHerbalistPassiveTest();
                if (GUILayout.Button("매 궁수 진급 + 전용 패시브 4종 테스트")) scene.StartFalconPassiveTest();
                if (GUILayout.Button("성직자 진급 + 전용 패시브 4종 테스트")) scene.StartClericPassiveTest();
                if (GUILayout.Button("성직자 일반체 행동 목록 테스트")) scene.StartClericScenario(false);
                if (GUILayout.Button("성직자 진급체 행동 목록 테스트")) scene.StartClericScenario(true);
                if (scene.CompanionLabel.StartsWith("성직자"))
                    EditorGUILayout.HelpBox("군단장 HP 50%로 시작합니다. 빛이 돌아온 뒤 회복·누적됩니다. 진급체는 6칸을 채우면 기존 성역을 지우고 현재 군단장 발밑에 새 성역을 만듭니다. 만피 누적·이동 중 귀환·Reset도 확인하세요.", MessageType.Info);
                if (scene.IsCompanionScenario)
                    EditorGUILayout.HelpBox($"{scene.CompanionLabel}는 자동 공격합니다. Attack / Manual Attacks / Repeat 설정은 적 공격용입니다. " +
                        "적은 공격하지 않으며, 초기 HP는 10,000입니다. 시작 버튼은 검병 2m, 매 궁수·폭탄병 4m 앞에 적을 배치합니다. " +
                        "매는 진급체의 3회 누적 급강하에서만 출현합니다. Reset으로 군단원과 대상을 다시 생성합니다.", MessageType.Info);
                else
                {
                    if (scene.IsEnemyProjectileScenario)
                        EditorGUILayout.HelpBox("같은 공통 화살을 적이 발사합니다. 발사당 9 피해이며 군단장만 맞힙니다. " +
                            "Repeat로 반복 발사, Keyboard Movement로 회피를 검사하세요. Infinite Hp 해제 시 HP가 감소합니다.", MessageType.Info);
                    if (scene.IsEnemyFieldScenario)
                        EditorGUILayout.HelpBox("군단장 위치에 반경 1.6m 장판을 생성합니다. 즉시 5 피해 후 1초마다 피해, 3초 뒤 종료. " +
                            "Infinite Hp를 끄고 범위 밖으로 이동해 확인하세요. 화염 효과는 기존 아군 자원을 테스트용으로 공유합니다.", MessageType.Info);
                    if (GUILayout.Button(scene.IsEnemyProjectileScenario ? "적 화살 발사" : scene.IsEnemyFieldScenario ? "적 장판 생성" : "Request Selected Attack")) scene.RequestAttack();
                }
                if (GUILayout.Button("Place Target at Configured Position")) scene.PlaceTarget();
                if (scene.IsCompanionScenario && GUILayout.Button("설정한 Enemy Position으로 적 이동")) scene.PlaceEnemy();
                if (GUILayout.Button("Place Target at Contact Range")) scene.PlaceTargetAtContact();
                if (GUILayout.Button("Kill Enemy (Production Damage)")) scene.KillEnemy();
                EditorGUILayout.Space();
                if (scene.IsCompanionScenario)
                    EditorGUILayout.LabelField($"{scene.CompanionLabel} 공격 횟수", scene.CompanionCastCount.ToString());
                else if (scene.IsEnemyProjectileScenario)
                    EditorGUILayout.LabelField("적 화살 발사 횟수", scene.EnemyProjectileShotCount.ToString());
                else if (scene.IsEnemyFieldScenario)
                    EditorGUILayout.LabelField("장판", "생성 버튼 또는 Repeat 사용");
                else
                {
                    var frame = scene.ActionFrame;
                    EditorGUILayout.LabelField("Action", $"{frame.Kind} / {frame.Phase}");
                    EditorGUILayout.LabelField("Remaining", $"{frame.Remaining:0.000} s");
                }
                if (scene.CompanionLabel == "해골 낫 투척병")
                    EditorGUILayout.HelpBox("낫이 지나갈 때와 돌아올 때 각각 피해를 줍니다. 군단장을 이동해 복귀 경로를 확인하고, 비행 중 Reset으로 취소를 검사하세요.", MessageType.Info);
                if (scene.ChainTestTargets.Count > 0)
                {
                    EditorGUILayout.HelpBox("가까운 표적 3개는 1.4m 간격, 마지막 표적은 연쇄 거리 밖에 배치합니다. 각 표적 HP와 반복 공격·Reset을 확인하세요.", MessageType.Info);
                    for (int i = 0; i < scene.ChainTestTargets.Count; i++)
                        EditorGUILayout.LabelField(i == 3 ? "거리 밖 표적 HP" : $"연쇄 표적 {i + 1} HP",
                            scene.ChainTestTargets[i] == null ? "-" : scene.ChainTestTargets[i].Hp.ToString());
                }
                EditorGUILayout.LabelField("Player HP", scene.Player == null ? "-" : $"{scene.Player.Hp} / {scene.Player.MaxHp}");
                EditorGUILayout.LabelField("활성 장판 수", scene.ActiveFieldCount.ToString());
                EditorGUILayout.LabelField("성역 활성 / 전체 군단원 공속", $"{scene.SanctuaryActive} / x{scene.SanctuaryPartySpeed:0.00}");
                EditorGUILayout.LabelField("Enemy HP", scene.Enemy == null ? "-" : $"{scene.Enemy.Hp} / {scene.Enemy.MaxHp}");
                if (GUILayout.Button(EditorApplication.isPaused ? "Resume" : "Pause"))
                    EditorApplication.isPaused = !EditorApplication.isPaused;
                using (new EditorGUI.DisabledScope(!EditorApplication.isPaused))
                    if (GUILayout.Button("Step One Frame")) EditorApplication.Step();
                float speed = EditorGUILayout.Slider("Time Scale", Time.timeScale, .1f, 2f);
                if (!Mathf.Approximately(speed, Time.timeScale)) Time.timeScale = speed;
            }
        }
        public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;
    }
}
