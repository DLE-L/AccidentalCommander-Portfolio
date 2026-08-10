using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Editor.Presentation
{
    [CustomEditor(typeof(FeedbackPresentationSet))]
    public sealed class FeedbackPresentationSetEditor : UnityEditor.Editor
    {
        private static readonly GUIContent PrefabLabel = new GUIContent("일회성 VFX Prefab");
        private static readonly GUIContent ScaleLabel = new GUIContent("Scale");
        private static readonly GUIContent LifetimeLabel = new GUIContent("Lifetime");
        private static readonly GUIContent RotationLabel = new GUIContent("VFX Rotation (Euler)", "Prefab 원본을 바꾸지 않고 이 슬롯에서만 적용하는 회전 오프셋입니다.");
        private static readonly GUIContent SfxPolicyLabel = new GUIContent("SFX 정책", "Required는 AudioClip 필수, Optional은 없어도 허용, None은 의도적으로 재생하지 않습니다.");
        private static readonly GUIContent SfxClipLabel = new GUIContent("SFX AudioClip", "일회성 VFX와 독립적으로 유지되는 이벤트 오디오입니다.");
        private static readonly GUIContent ActorFeedbackLabel = new GUIContent("Actor Feedback", "Prefab을 생성하지 않고 대상 본체에 Flash/Shake를 적용하는 선택 채널입니다.");

        private readonly struct InspectorDescription
        {
            public InspectorDescription(string koreanName, string playbackHint)
            {
                KoreanName = koreanName;
                PlaybackHint = playbackHint;
            }

            public string KoreanName { get; }
            public string PlaybackHint { get; }
        }

        private static readonly Dictionary<RetroVfxKind, InspectorDescription> GeneralDescriptions =
            new Dictionary<RetroVfxKind, InspectorDescription>
            {
                { RetroVfxKind.HealingReceived, new InspectorDescription("회복 수신", "모든 회복·치유가 적용될 때 대상 위치에서 1회 재생.") },
                { RetroVfxKind.BuffApplied, new InspectorDescription("버프 적용", "스탯·보호·스폰 보호 버프가 적용될 때 해당 대상에 재생.") },
                { RetroVfxKind.PlayerDamaged, new InspectorDescription("지휘관 피격", "적의 접촉·돌진 공격이 지휘관에게 적중할 때 지휘관 위치에 재생.") },
                { RetroVfxKind.ShieldOrcCrack, new InspectorDescription("방패 오크 균열", "방패 오크의 보호막이 처음 균열될 때 적 위치에서 1회 재생.") },
                { RetroVfxKind.RedChargerCharge, new InspectorDescription("붉은 돌진병 돌진", "돌진 경로 경고 시작 때 적 위치와 방향에 1회 재생.") },
                { RetroVfxKind.BossAoeImpact, new InspectorDescription("보스 범위 타격", "보스 범위 공격의 피해 프레임에 범위 중심에서 1회 재생.") },
                { RetroVfxKind.GuardSquadActivate, new InspectorDescription("수호 분대 발동", "수호 분대 방사형 시전 시작 때(쿨다운 제외) 중심에서 재생.") },
                { RetroVfxKind.GuardShockwave, new InspectorDescription("수호 충격파", "수호 분대 중앙 시전에서만 1회 재생.") },
                { RetroVfxKind.GuardRadialShield, new InspectorDescription("수호 방패 전개", "수호 분대 방사형 시전 때 중심에서 재생; 첫·반복 시전 크기는 다름.") },
                { RetroVfxKind.LevelUp, new InspectorDescription("레벨업", "경험치로 레벨이 상승할 때 지휘관 위치에서 재생.") },
                { RetroVfxKind.CardSelect, new InspectorDescription("카드 선택", "레벨업 카드를 선택한 직후 지휘관 위치에서 재생.") },
                { RetroVfxKind.ResultClear, new InspectorDescription("클리어 결과", "클리어 결과 처리 때 지휘관 위치에서 1회 재생; 오디오 전용은 Prefab 없이 허용.") },
                { RetroVfxKind.XpAbsorb, new InspectorDescription("경험치 흡수", "경험치 보주를 흡수할 때 보주 위치에서 재생; 엘리트 붉은 돌진병 보주는 별도 크기.") },
                { RetroVfxKind.CompanionRecruit, new InspectorDescription("동료 영입", "동료 영입 적용 후 새 동료에게 부착해 재생.") },
                { RetroVfxKind.CompanionPromotion, new InspectorDescription("동료 승급", "동료 승급 적용 후 해당 동료에게 부착해 재생.") },
                { RetroVfxKind.PromotionShoutActivate, new InspectorDescription("승급 외침 발동", "승급 외침 특성이 적용될 때 지휘관 위치에서 1회 재생.") },
                { RetroVfxKind.SynergyReady, new InspectorDescription("시너지 준비", "Build 1 세 시너지 공용: READY 변경 때 지휘관 위치에서 1회 재생.") },
                { RetroVfxKind.SynergyComplete, new InspectorDescription("시너지 완성", "Build 1 세 시너지 공용: COMPLETE 변경 때 지휘관 위치에서 1회 재생.") },
                { RetroVfxKind.RapidCrossbowCast, new InspectorDescription("연발 석궁 발사", "선택된 무기 발사 시작 때 지휘관 fire socket에서 1회 재생.") },
                { RetroVfxKind.PiercingSpearCast, new InspectorDescription("관통 창 발사", "선택된 무기 발사 시작 때 지휘관 fire socket에서 1회 재생.") },
                { RetroVfxKind.BlastStaffCast, new InspectorDescription("폭발 지팡이 발사", "선택된 무기 발사 시작 때 지휘관 fire socket에서 1회 재생.") },
                { RetroVfxKind.BlastStaffExplosion, new InspectorDescription("폭발 지팡이 폭발", "폭발 지팡이 충돌 범위에서 폭발 1회마다 1회 재생.") },
                { RetroVfxKind.BossSpawn, new InspectorDescription("보스 등장", "보스 도착 시에만 보스 위치에서 1회 재생.") },
            };

        private static readonly Dictionary<string, InspectorDescription> CompanionAttackDescriptions =
            new Dictionary<string, InspectorDescription>(StringComparer.Ordinal)
            {
                { "dmg_shield_bash_v1", new InspectorDescription("방패병 - 방패 밀치기", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_sword_slash_v1", new InspectorDescription("검병 - 검 베기", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_cleric_bolt_v1", new InspectorDescription("성직자 - 성광탄", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_falcon_arrow_v1", new InspectorDescription("매 궁수 - 화살", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_herbal_dart_v1", new InspectorDescription("약초사 - 약초 다트", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_bomb_explosion_v1", new InspectorDescription("폭파병 - 폭탄 폭발", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dot_fire_field_v1", new InspectorDescription("화염 마법사 - 화염 장판", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_chain_lightning_v1", new InspectorDescription("번개 마법사 - 연쇄 번개", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_wolf_assault_v1", new InspectorDescription("늑대 조련사 - 늑대 돌진", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_wraith_slash_v1", new InspectorDescription("망령 기사 - 망령 베기", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_curse_bolt_v1", new InspectorDescription("강령술사 - 저주 탄환", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
                { "dmg_skeleton_bomb_v1", new InspectorDescription("해골 폭파병 - 해골 폭탄", "성공한 기본 공격 1회마다 시전·충돌 위치에서 재생; 타격·연쇄·tick 반복 없음.") },
            };

        private SerializedProperty _entries;
        private SerializedProperty _companionAttacks;
        private SerializedProperty _straightProjectileShell;
        private SerializedProperty _homingProjectileShell;
        private SerializedProperty _projectileVisuals;

        private void OnEnable()
        {
            _entries = serializedObject.FindProperty("_entries");
            _companionAttacks = serializedObject.FindProperty("_companionAttacks");
            _straightProjectileShell = serializedObject.FindProperty("_straightProjectileShell");
            _homingProjectileShell = serializedObject.FindProperty("_homingProjectileShell");
            _projectileVisuals = serializedObject.FindProperty("_projectileVisuals");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FeedbackPresentationSet presentationSet = (FeedbackPresentationSet)target;
            if (presentationSet.TryValidate(out string issue) == false)
                EditorGUILayout.HelpBox(issue, MessageType.Error);

            Dictionary<int, int> kindCounts = CountKinds();
            DrawMissingKinds(kindCounts);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("이벤트 채널 — SFX / 일회성 VFX / Actor Feedback", EditorStyles.boldLabel);
            for (int i = 0; i < _entries.arraySize; i++)
                DrawEntry(_entries.GetArrayElementAtIndex(i), i, kindCounts);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("동료 기본 공격 이벤트 채널", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("동료 공격 VFX는 공격 방향을 자동으로 바라봅니다. Rotation은 Prefab 축 보정에만 사용합니다.", MessageType.Info);
            for (int i = 0; i < _companionAttacks.arraySize; i++)
                DrawCompanionAttackEntry(_companionAttacks.GetArrayElementAtIndex(i), i);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("이동 Projectile 비주얼", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Straight/Homing 공용 Shell은 시스템 소유입니다. 각 공격은 Body Sprite, Tint, Scale, Rotation만 설정합니다. " +
                "Body Sprite가 비어 있으면 투사체 이동·충돌·피해는 유지되고 본체만 보이지 않습니다. 최종 Sprite와 수치는 사용자가 직접 조정합니다.",
                MessageType.Info);
            EditorGUILayout.PropertyField(_straightProjectileShell, new GUIContent("Straight 공용 Shell"));
            EditorGUILayout.PropertyField(_homingProjectileShell, new GUIContent("Homing 공용 Shell"));
            for (int i = 0; i < _projectileVisuals.arraySize; i++)
                DrawProjectileVisualEntry(_projectileVisuals.GetArrayElementAtIndex(i), i);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawCompanionAttackEntry(SerializedProperty entry, int index)
        {
            SerializedProperty effectId = entry.FindPropertyRelative("_effectId");
            InspectorDescription description = GetCompanionAttackDescription(effectId.stringValue);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{index + 1}. {description.KoreanName} — {effectId.stringValue}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(description.PlaybackHint, MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(effectId);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_prefab"), PrefabLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_rotationEuler"), RotationLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_sfxPolicy"), SfxPolicyLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_sfx"), SfxClipLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_actorFeedback"), ActorFeedbackLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_scale"), ScaleLabel, GUILayout.MinWidth(120.0f));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_lifetime"), LifetimeLabel, GUILayout.MinWidth(120.0f));
            EditorGUILayout.EndHorizontal();
            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, "Advanced", true);
            if (entry.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_forwardOffset"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_upOffset"));
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawProjectileVisualEntry(SerializedProperty entry, int index)
        {
            SerializedProperty presentationId = entry.FindPropertyRelative("_presentationId");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{index + 1}. {presentationId.stringValue}", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(presentationId, new GUIContent("Presentation ID"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_bodySprite"), new GUIContent("Body Sprite (선택)"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_tint"), new GUIContent("Body Tint"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_scale"), new GUIContent("Visual Scale"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_rotationEuler"), new GUIContent("Visual Rotation"));
            EditorGUILayout.HelpBox("Projectile은 이동 방향을 자동으로 바라봅니다. Visual Rotation은 Sprite 축 보정에만 사용합니다.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private Dictionary<int, int> CountKinds()
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int i = 0; i < _entries.arraySize; i++)
            {
                SerializedProperty kind = _entries.GetArrayElementAtIndex(i).FindPropertyRelative("_kind");
                int value = kind.intValue;
                counts.TryGetValue(value, out int count);
                counts[value] = count + 1;
            }

            return counts;
        }

        private static void DrawMissingKinds(Dictionary<int, int> kindCounts)
        {
            Array values = Enum.GetValues(typeof(RetroVfxKind));
            for (int i = 0; i < values.Length; i++)
            {
                RetroVfxKind kind = (RetroVfxKind)values.GetValue(i);
                if (FeedbackPresentationSet.IsAuthorableKind(kind) == false)
                    continue;

                if (kindCounts.ContainsKey((int)kind) == false)
                    EditorGUILayout.HelpBox($"Missing fixed slot: {kind} / {FeedbackPresentationSet.GetExpectedSlotId(kind)}", MessageType.Error);
            }
        }

        private static void DrawEntry(SerializedProperty entry, int index, Dictionary<int, int> kindCounts)
        {
            SerializedProperty kindProperty = entry.FindPropertyRelative("_kind");
            SerializedProperty slotIdProperty = entry.FindPropertyRelative("_slotId");
            SerializedProperty prefabProperty = entry.FindPropertyRelative("_prefab");
            RetroVfxKind kind = (RetroVfxKind)kindProperty.intValue;
            string rowIssue = GetRowIssue(kind, slotIdProperty.stringValue, kindCounts);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (string.IsNullOrEmpty(rowIssue) == false)
                EditorGUILayout.HelpBox($"Entry {index}: {rowIssue}", MessageType.Error);

            InspectorDescription description = GetGeneralDescription(kind);
            EditorGUILayout.LabelField($"{index + 1}. {description.KoreanName} — {kind} / {slotIdProperty.stringValue}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(description.PlaybackHint, MessageType.Info);
            EditorGUILayout.PropertyField(prefabProperty, PrefabLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_rotationEuler"), RotationLabel);
            if (prefabProperty.objectReferenceValue == null)
                EditorGUILayout.HelpBox("일회성 VFX 없음: 이 채널만 조용히 건너뛰며 SFX와 Actor Feedback은 유지됩니다.", MessageType.Info);

            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_sfxPolicy"), SfxPolicyLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_sfx"), SfxClipLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_actorFeedback"), ActorFeedbackLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_scale"), ScaleLabel, GUILayout.MinWidth(120.0f));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("_lifetime"), LifetimeLabel, GUILayout.MinWidth(120.0f));
            EditorGUILayout.EndHorizontal();

            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, "Advanced", true);
            if (entry.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(kindProperty);
                        EditorGUILayout.PropertyField(slotIdProperty);
                    }

                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_forwardOffset"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_upOffset"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_alignToDirection"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_angleOffset"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_sfxVolumeScale"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_minScale"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_maxScale"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_isScaleException"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_isHitFeedback"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("_hasRewardCue"));
                }
            }

            EditorGUILayout.EndVertical();
        }

        private static string GetRowIssue(
            RetroVfxKind kind,
            string slotId,
            Dictionary<int, int> kindCounts)
        {
            if (Enum.IsDefined(typeof(RetroVfxKind), kind) == false || FeedbackPresentationSet.IsAuthorableKind(kind) == false)
                return $"undefined kind value {(int)kind}";

            if (kindCounts.TryGetValue((int)kind, out int count) && count > 1)
                return $"duplicate kind {kind}";

            string expectedSlotId = FeedbackPresentationSet.GetExpectedSlotId(kind);
            if (string.Equals(slotId, expectedSlotId, StringComparison.Ordinal) == false)
                return $"slot ID must remain '{expectedSlotId}'";

            return string.Empty;
        }

        private static InspectorDescription GetGeneralDescription(RetroVfxKind kind)
        {
            return GeneralDescriptions.TryGetValue(kind, out InspectorDescription description)
                ? description
                : new InspectorDescription("미등록 VFX 슬롯", "설명 매핑이 없습니다.");
        }

        private static InspectorDescription GetCompanionAttackDescription(string effectId)
        {
            return CompanionAttackDescriptions.TryGetValue(effectId, out InspectorDescription description)
                ? description
                : new InspectorDescription("미등록 동료 공격", "설명 매핑이 없습니다.");
        }
    }
}
