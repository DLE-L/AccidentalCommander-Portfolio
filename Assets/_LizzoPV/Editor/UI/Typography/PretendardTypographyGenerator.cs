using System;
using TMPro;
using UnityEditor;

namespace Lizzo.PV.EditorTools.UI.Typography
{
    internal static partial class PretendardTypographyGenerator
    {
        private const string SourceRoot = "Assets/_LizzoPV/Shared/UI/Typography/Pretendard/Source";
        private const string OutputRoot = "Assets/_LizzoPV/Shared/UI/Typography/Pretendard/TMP";
        private const string SemiBoldPath = OutputRoot + "/Pretendard-SemiBold SDF.asset";
        private const string ExtraBoldPath = OutputRoot + "/Pretendard-ExtraBold SDF.asset";
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string AfacadOutlineFontPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Font/AfacadFlux-ExtraBold SDF_OutlineBlack.asset";
        private const string LtAvocadoOutlineFontPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Font/LTAvocado-Bold SDF_OutlineBlack.asset";
        private const string AfacadBridgePath = OutputRoot + "/AfacadFlux-ExtraBold SDF_OutlineBlack_PretendardFallback.asset";
        private const string LtAvocadoBridgePath = OutputRoot + "/LTAvocado-Bold SDF_OutlineBlack_PretendardFallback.asset";
        private const string PartyUnitBasePrefabPath = "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Base/PartyUnitBase.prefab";
        private const string CommanderPrefabPath = "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab";
        private const string FloatingDamageTextPrefabPath = "Assets/_LizzoPV/Gameplay/Combat/Prefabs/Effects/FloatingDamageText.prefab";
        private static readonly string[] RuntimeWorldFeedbackCorpus =
        {
            "쓰러짐", "회복!", "근위대 결성!", "방패 진형 전개!", "돌격수 격파!",
            "지금 공격!", "거인이 흔들립니다!", "방패 균열!", "방패병 합류!",
            "방패대장 합류!", "검병 합류!", "성직자 합류!", "궁수 합류!", "동료 합류!", "EXP!"
        };


        private static void PreserveDynamicDataOnBuild(TMP_FontAsset font)
        {
            SerializedObject serializedFont = new SerializedObject(font);
            SerializedProperty clearDynamicData = serializedFont.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearDynamicData == null)
                throw new InvalidOperationException("TMP clear-dynamic-data property is missing: " + font.name);
            clearDynamicData.boolValue = false;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();
        }

    }
}
