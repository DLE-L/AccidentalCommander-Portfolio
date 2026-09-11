using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools.UI.Typography
{
    internal static partial class PretendardTypographyGenerator
    {
        [MenuItem("Lizzo/UI/Typography/Validate Runtime World Fonts", false, 363)]
        private static void ValidateRuntimeWorld()
        {
            try
            {
                bool passed;
                string report = BuildRuntimeWorldValidationReport(out passed);
                if (passed)
                    Debug.Log(report);
                else
                    Debug.LogError(report);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Pretendard Typography] RUNTIME WORLD VALIDATION BLOCKED\n" + exception);
            }
        }

        private static string BuildRuntimeWorldValidationReport(out bool passed)
        {
            passed = true;
            StringBuilder output = new StringBuilder();
            output.AppendLine("[Pretendard Typography] RUNTIME WORLD VALIDATION");

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != GameplayScenePath)
            {
                passed = false;
                output.AppendLine("SCENE expected=" + GameplayScenePath + " actual=" + scene.path);
                return output.ToString();
            }

            string corpus = BuildRuntimeWorldCorpus(scene);
            output.AppendLine("CORPUS count=" + corpus.Length + " sha256=" + Sha256(corpus));
            TMP_FontAsset semiBold = RequireFont(OutputRoot + "/Pretendard-SemiBold SDF.asset");
            TMP_FontAsset extraBold = RequireFont(OutputRoot + "/Pretendard-ExtraBold SDF.asset");
            TMP_FontAsset afacadBridge = RequireFont(AfacadBridgePath);
            TMP_FontAsset ltAvocadoBridge = RequireFont(LtAvocadoBridgePath);

            AppendResolvedFontValidation(output, extraBold, corpus, "Pretendard-ExtraBold", ref passed);
            AppendResolvedFontValidation(output, afacadBridge, corpus, "Afacad bridge", ref passed);
            AppendResolvedFontValidation(output, ltAvocadoBridge, "0123456789×★", "LT Avocado bridge", ref passed);
            AppendFallbackValidation(output, afacadBridge, ExtraBoldPath, "Afacad bridge", ref passed);
            AppendFallbackValidation(output, ltAvocadoBridge, SemiBoldPath, "LT Avocado bridge", ref passed);

            UnityEngine.Object settings = AssetDatabase.LoadMainAssetAtPath(TmpSettingsPath);
            SerializedObject serializedSettings = new SerializedObject(settings);
            UnityEngine.Object defaultFont = serializedSettings.FindProperty("m_defaultFontAsset").objectReferenceValue;
            if (defaultFont != semiBold)
                passed = false;
            output.AppendLine("TMP_SETTINGS default=" + AssetDatabase.GetAssetPath(defaultFont));

            GameObject root = FindSceneObject(scene, "GameplayUIRoot");
            Transform countRoot = FindTransform(root, "HUD/PauseOverlay/Panel/CompanionSection/CompanionSlotRow");
            TMP_Text[] countTexts = countRoot.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => text.name == "CountText")
                .ToArray();
            output.AppendLine("SCENE_COUNT_TEXT count=" + countTexts.Length);
            if (countTexts.Length == 0)
                passed = false;
            for (int i = 0; i < countTexts.Length; i++)
            {
                AppendSceneTextValidation(output, countTexts[i], LtAvocadoBridgePath, LtAvocadoBridgePath, ref passed);
            }

            foreach (string path in new[]
            {
                "CardSelectPopup/Content/SelectionGuideText",
                "HUD/PauseOverlay/Panel/ButtonRow/ResumeButton/Text (TMP)",
                "HUD/PauseOverlay/Panel/ButtonRow/LobbyButton/Text (TMP)"
            })
            {
                TMP_Text text = FindText(root, path);
                output.AppendLine("SCENE_TMP path=" + path + " font=" + AssetDatabase.GetAssetPath(text.font)
                    + " material=" + AssetDatabase.GetAssetPath(text.fontSharedMaterial)
                    + " fallbackMaterials=" + GetFallbackMaterialCount(text));
                if (GetFallbackMaterialCount(text) != 0)
                    passed = false;
            }

            AppendRuntimePrefabValidation(output, PartyUnitBasePrefabPath, new string[][]
            {
                new[] { "UI/HpBarAnchor/DownMarker", ExtraBoldPath, OutputRoot + "/Pretendard-ExtraBold_OutlineBlack.mat" },
                new[] { "UI/HpBarAnchor/CompanionHPBar/Text", LtAvocadoOutlineFontPath, LtAvocadoOutlineFontPath }
            }, ref passed);
            AppendRuntimePrefabValidation(output, CommanderPrefabPath, new string[][]
            {
                new[] { "CommanderHPBar/Text", LtAvocadoOutlineFontPath, LtAvocadoOutlineFontPath }
            }, ref passed);
            AppendRuntimePrefabValidation(output, FloatingDamageTextPrefabPath, new string[][]
            {
                new[] { "", AfacadBridgePath, AfacadBridgePath }
            }, ref passed);
            return output.ToString();
        }

        private static void AppendResolvedFontValidation(StringBuilder output, TMP_FontAsset font, string corpus, string label, ref bool passed)
        {
            string missing = GetMissingResolvedCharacters(font, corpus);
            if (missing.Length > 0)
                passed = false;
            output.AppendLine("FONT label=" + label + " missing=" + (missing.Length == 0 ? "<none>" : EscapeForLog(missing)));
        }

        private static void AppendFallbackValidation(StringBuilder output, TMP_FontAsset font, string expectedPath, string label, ref bool passed)
        {
            string fallback = font.fallbackFontAssetTable == null || font.fallbackFontAssetTable.Count == 0
                ? "<none>"
                : string.Join(",", font.fallbackFontAssetTable.Select(AssetDatabase.GetAssetPath).ToArray());
            if (font.fallbackFontAssetTable == null
                || font.fallbackFontAssetTable.Count != 1
                || AssetDatabase.GetAssetPath(font.fallbackFontAssetTable[0]) != expectedPath)
                passed = false;
            output.AppendLine("BRIDGE label=" + label + " fallback=" + fallback);
        }

        private static void AppendSceneTextValidation(StringBuilder output, TMP_Text text, string expectedFontPath, string expectedMaterialPath, ref bool passed)
        {
            string fontPath = AssetDatabase.GetAssetPath(text.font);
            string materialPath = AssetDatabase.GetAssetPath(text.fontSharedMaterial);
            int fallbackMaterials = GetFallbackMaterialCount(text);
            if (fontPath != expectedFontPath || materialPath != expectedMaterialPath || fallbackMaterials != 0)
                passed = false;
            output.AppendLine("SCENE_TMP path=" + text.transform.name + " font=" + fontPath
                + " material=" + materialPath + " fallbackMaterials=" + fallbackMaterials);
        }

        private static void AppendRuntimePrefabValidation(StringBuilder output, string prefabPath, string[][] mappings, ref bool passed)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                for (int i = 0; i < mappings.Length; i++)
                {
                    TMP_Text text = FindText(root, mappings[i][0]);
                    string fontPath = AssetDatabase.GetAssetPath(text.font);
                    string materialPath = AssetDatabase.GetAssetPath(text.fontSharedMaterial);
                    if (fontPath != mappings[i][1] || materialPath != mappings[i][2])
                        passed = false;
                    output.AppendLine("PREFAB_TMP asset=" + prefabPath + " path=" + mappings[i][0]
                        + " font=" + fontPath + " material=" + materialPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int GetFallbackMaterialCount(TMP_Text text)
        {
            SerializedObject serialized = new SerializedObject(text);
            SerializedProperty fallbackMaterials = serialized.FindProperty("m_fontSharedMaterials");
            return fallbackMaterials == null ? -1 : fallbackMaterials.arraySize;
        }

    }
}
