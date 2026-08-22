using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace Lizzo.PV.EditorTools.UI.Typography
{
    internal static class PretendardTypographyGenerator
    {
        private const string SourceRoot = "Assets/_LizzoPV/Shared/UI/Typography/Pretendard/Source";
        private const string OutputRoot = "Assets/_LizzoPV/Shared/UI/Typography/Pretendard/TMP";
        private const string SemiBoldPath = OutputRoot + "/Pretendard-SemiBold SDF.asset";
        private const string ExtraBoldPath = OutputRoot + "/Pretendard-ExtraBold SDF.asset";
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string AfacadBaseFontPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Font/AfacadFlux-ExtraBold SDF.asset";
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


        private sealed class FontSet
        {
            public TMP_FontAsset Font;
            public Material Plain;
            public Material BaseUnderlay;
            public Material OutlineBlack;
        }

        [MenuItem("Lizzo/UI/Typography/Cleanup Runtime World Fonts", false, 362)]
        private static void CleanupRuntimeWorld()
        {
            try
            {
                CleanupRuntimeWorldInternal();
            }
            catch (Exception exception)
            {
                Debug.LogError("[Pretendard Typography] RUNTIME WORLD BLOCKED\n" + exception);
            }
        }

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

        [MenuItem("Lizzo/UI/Typography/Prewarm Runtime World Font Assets", false, 364)]
        private static void PrewarmRuntimeWorldFontAssets()
        {
            try
            {
                PrewarmRuntimeWorldFontAssetsInternal();
            }
            catch (Exception exception)
            {
                Debug.LogError("[Pretendard Typography] FONT PREWARM BLOCKED\n" + exception);
            }
        }

        private static void CleanupRuntimeWorldInternal()
        {
            RequireAsset(SourceRoot + "/Pretendard-SemiBold.otf");
            RequireAsset(SourceRoot + "/Pretendard-ExtraBold.otf");
            RequireAsset(LtAvocadoOutlineFontPath);
            RequireAsset(AfacadOutlineFontPath);

            Scene scene = SceneManager.GetActiveScene();
            RequireGameplayEditorState(scene);

            TMP_FontAsset semiBold = RequireFont(OutputRoot + "/Pretendard-SemiBold SDF.asset");
            TMP_FontAsset extraBold = RequireFont(OutputRoot + "/Pretendard-ExtraBold SDF.asset");
            string corpus = BuildRuntimeWorldCorpus(scene);
            Debug.Log("[Pretendard Typography] RUNTIME_CORPUS count=" + corpus.Length + " sha256=" + Sha256(corpus));

            PrewarmAndRequire(extraBold, corpus, "Pretendard-ExtraBold");
            PrewarmAndRequire(semiBold, "0123456789×★", "Pretendard-SemiBold");

            TMP_FontAsset afacadBridge = CreateAfacadBridge(extraBold);
            TMP_FontAsset ltAvocadoBridge = CreateLtAvocadoBridge(semiBold);
            TMP_FontAsset ltAvocado = RequireFont(LtAvocadoOutlineFontPath);
            Material extraBoldOutline = RequireMaterial(OutputRoot + "/Pretendard-ExtraBold_OutlineBlack.mat");
            RequireResolvedCharacters(afacadBridge, corpus, "Afacad bridge");
            RequireResolvedCharacters(ltAvocadoBridge, "0123456789×★", "LT Avocado bridge");

            ApplyRuntimeWorldScene(scene, ltAvocadoBridge);
            ApplyRuntimeWorldPrefab(PartyUnitBasePrefabPath, root =>
            {
                TMP_Text downMarker = FindText(root, "UI/HpBarAnchor/P0_DownMarker");
                TMP_Text companionHp = FindText(root, "UI/HpBarAnchor/P0_CompanionHPBar/Text");
                ApplyPrefabText(root, "UI/HpBarAnchor/P0_DownMarker", extraBold, extraBoldOutline);
                ApplyPrefabText(root, "UI/HpBarAnchor/P0_CompanionHPBar/Text", ltAvocado, ltAvocado.material);
                ClearFallbackMaterials(downMarker);
                ClearFallbackMaterials(companionHp);
            });
            ApplyRuntimeWorldPrefab(CommanderPrefabPath, root =>
            {
                TMP_Text commanderHp = FindText(root, "P0_CommanderHPBar/Text");
                ApplyPrefabText(root, "P0_CommanderHPBar/Text", ltAvocado, ltAvocado.material);
                ClearFallbackMaterials(commanderHp);
            });
            ApplyRuntimeWorldPrefab(FloatingDamageTextPrefabPath, root =>
            {
                TMP_Text floatingText = FindText(root, "");
                ApplyPrefabText(root, "", afacadBridge, afacadBridge.material);
                ClearFallbackMaterials(floatingText);
            });
            ApplyTmpSettings(semiBold);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);

            bool passed;
            string report = BuildRuntimeWorldValidationReport(out passed);
            if (!passed)
                throw new InvalidOperationException(report);
            Debug.Log(report);
        }

        private static void PrewarmRuntimeWorldFontAssetsInternal()
        {
            Scene scene = SceneManager.GetActiveScene();
            RequireGameplayEditorState(scene);

            TMP_FontAsset semiBold = RequireFont(SemiBoldPath);
            TMP_FontAsset extraBold = RequireFont(ExtraBoldPath);
            string corpus = BuildRuntimeWorldCorpus(scene);

            PrewarmAndRequire(extraBold, corpus, "Pretendard-ExtraBold");
            PrewarmAndRequire(semiBold, "0123456789×★", "Pretendard-SemiBold");
            AssetDatabase.SaveAssets();
        }

        private static void RequireGameplayEditorState(Scene scene)
        {
            if (scene.path != GameplayScenePath)
                throw new InvalidOperationException("Active scene must be " + GameplayScenePath + ".");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode is not allowed.");
            if (scene.isDirty)
                throw new InvalidOperationException("Gameplay scene is dirty; save authored changes before runtime cleanup.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Prefab Stage is open; close it before runtime cleanup.");
        }

        private static TMP_FontAsset RequireFont(string path)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
                throw new InvalidOperationException("Required TMP font is missing: " + path);
            return font;
        }

        private static Material RequireMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                throw new InvalidOperationException("Required TMP material is missing: " + path);
            return material;
        }

        private static string BuildRuntimeWorldCorpus(Scene scene)
        {
            SortedSet<char> characters = new SortedSet<char>();
            for (int code = 32; code <= 126; code++)
                characters.Add((char)code);
            AddString(characters, "×★");
            for (int i = 0; i < RuntimeWorldFeedbackCorpus.Length; i++)
                AddString(characters, RuntimeWorldFeedbackCorpus[i]);

            foreach (string path in new[] { PartyUnitBasePrefabPath, CommanderPrefabPath, FloatingDamageTextPrefabPath })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    throw new InvalidOperationException("Required runtime prefab is missing: " + path);
                foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
                    AddString(characters, text.text);
            }

            GameObject root = FindSceneObject(scene, "GameplayUIRoot");
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.IsChildOf(FindTransform(root, "ResultPopup")))
                    continue;
                AddString(characters, text.text);
            }

            return new string(characters.ToArray());
        }

        private static void PrewarmAndRequire(TMP_FontAsset font, string corpus, string label)
        {
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.isMultiAtlasTexturesEnabled = true;
            PreserveDynamicDataOnBuild(font);
            string missing;
            bool addResult = font.TryAddCharacters(corpus, out missing, false);
            string unresolved = GetMissingCharacters(font, corpus);
            EditorUtility.SetDirty(font);
            Debug.Log("[Pretendard Typography] PREWARM font=" + label
                + " success=" + (unresolved.Length == 0)
                + " addResult=" + addResult
                + " missing=" + (unresolved.Length == 0 ? "<none>" : EscapeForLog(unresolved)));
            if (unresolved.Length > 0)
                throw new InvalidOperationException("Font prewarm missing characters for " + label + ": " + EscapeForLog(unresolved));
        }

        private static void RequireResolvedCharacters(TMP_FontAsset font, string corpus, string label)
        {
            string missing = GetMissingResolvedCharacters(font, corpus);
            if (missing.Length > 0)
                throw new InvalidOperationException("Resolved font corpus missing characters for " + label + ": " + EscapeForLog(missing));
        }

        private static string GetMissingResolvedCharacters(TMP_FontAsset font, string corpus)
        {
            StringBuilder missing = new StringBuilder();
            for (int i = 0; i < corpus.Length; i++)
            {
                if (!HasResolvedCharacter(font, corpus[i], new HashSet<TMP_FontAsset>()))
                    missing.Append(corpus[i]);
            }
            return missing.ToString();
        }

        private static bool HasResolvedCharacter(TMP_FontAsset font, char character, HashSet<TMP_FontAsset> visited)
        {
            if (font == null || !visited.Add(font))
                return false;
            if (font.HasCharacter(character))
                return true;
            if (font.fallbackFontAssetTable == null)
                return false;
            for (int i = 0; i < font.fallbackFontAssetTable.Count; i++)
            {
                if (HasResolvedCharacter(font.fallbackFontAssetTable[i], character, visited))
                    return true;
            }
            return false;
        }

        private static TMP_FontAsset CreateLtAvocadoBridge(TMP_FontAsset fallback)
        {
            TMP_FontAsset bridge = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LtAvocadoBridgePath);
            if (bridge == null)
            {
                if (!AssetDatabase.CopyAsset(LtAvocadoOutlineFontPath, LtAvocadoBridgePath))
                    throw new InvalidOperationException("Could not create project-owned LT Avocado bridge.");
                AssetDatabase.ImportAsset(LtAvocadoBridgePath, ImportAssetOptions.ForceSynchronousImport);
                bridge = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LtAvocadoBridgePath);
            }

            if (bridge == null)
                throw new InvalidOperationException("Project-owned LT Avocado bridge did not import.");

            List<TMP_FontAsset> fallbacks = bridge.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
            fallbacks.Clear();
            if (!bridge.HasCharacter('★') || !bridge.HasCharacter('×'))
                fallbacks.Add(fallback);
            bridge.fallbackFontAssetTable = fallbacks;
            EditorUtility.SetDirty(bridge);
            Debug.Log("[Pretendard Typography] LT_BRIDGE path=" + LtAvocadoBridgePath
                + " fallback=" + (fallbacks.Count == 0 ? "<none>" : AssetDatabase.GetAssetPath(fallbacks[0])));
            return bridge;
        }

        private static void ApplyRuntimeWorldScene(Scene scene, TMP_FontAsset ltAvocadoBridge)
        {
            GameObject root = FindSceneObject(scene, "GameplayUIRoot");
            Transform countRoot = FindTransform(root, "HUD/PauseOverlay/Panel/CompanionSection/CompanionSlotRow");
            TMP_Text[] countTexts = countRoot.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => text.name == "CountText")
                .ToArray();
            if (countTexts.Length == 0)
                throw new InvalidOperationException("No PauseOverlay CountText targets found.");
            for (int i = 0; i < countTexts.Length; i++)
            {
                ApplyRuntimeText(countTexts[i], ltAvocadoBridge, ltAvocadoBridge.material);
                ClearFallbackMaterials(countTexts[i]);
            }

            foreach (string path in new[]
            {
                "CardSelectPopup/Content/SelectionGuideText",
                "HUD/PauseOverlay/Panel/ButtonRow/ResumeButton/Text (TMP)",
                "HUD/PauseOverlay/Panel/ButtonRow/LobbyButton/Text (TMP)"
            })
            {
                TMP_Text text = FindText(root, path);
                ClearFallbackMaterials(text);
            }
        }

        private static void ApplyRuntimeText(TMP_Text text, TMP_FontAsset font, Material material)
        {
            text.font = font;
            text.fontSharedMaterial = material;
            EditorUtility.SetDirty(text);
        }

        private static void ClearFallbackMaterials(TMP_Text text)
        {
            SerializedObject serialized = new SerializedObject(text);
            SerializedProperty fallbackMaterials = serialized.FindProperty("m_fontMaterials");
            if (fallbackMaterials == null)
                throw new InvalidOperationException("TMP fallback material property is missing on " + text.name + ".");
            if (fallbackMaterials.arraySize > 1)
            {
                fallbackMaterials.arraySize = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(text);
            }
        }

        private static void ApplyTmpSettings(TMP_FontAsset semiBold)
        {
            UnityEngine.Object settings = AssetDatabase.LoadMainAssetAtPath(TmpSettingsPath);
            if (settings == null)
                throw new InvalidOperationException("TMP Settings asset is missing: " + TmpSettingsPath);
            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty defaultFont = serialized.FindProperty("m_defaultFontAsset");
            if (defaultFont == null)
                throw new InvalidOperationException("TMP Settings default font property is missing.");
            defaultFont.objectReferenceValue = semiBold;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void ApplyRuntimeWorldPrefab(string path, Action<GameObject> apply)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                throw new InvalidOperationException("Could not load " + path);
            string hierarchyBefore = BuildHierarchySignature(root);
            try
            {
                apply(root);
                if (BuildHierarchySignature(root) != hierarchyBefore)
                    throw new InvalidOperationException("Prefab hierarchy changed unexpectedly: " + path);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
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
                new[] { "UI/HpBarAnchor/P0_DownMarker", ExtraBoldPath, OutputRoot + "/Pretendard-ExtraBold_OutlineBlack.mat" },
                new[] { "UI/HpBarAnchor/P0_CompanionHPBar/Text", LtAvocadoOutlineFontPath, LtAvocadoOutlineFontPath }
            }, ref passed);
            AppendRuntimePrefabValidation(output, CommanderPrefabPath, new string[][]
            {
                new[] { "P0_CommanderHPBar/Text", LtAvocadoOutlineFontPath, LtAvocadoOutlineFontPath }
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

        private static FontSet CreateFontSet(string name, string sourcePath, string corpus)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null)
                throw new InvalidOperationException("Source font is not imported: " + sourcePath);

            string fontPath = OutputRoot + "/" + name + " SDF.asset";
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                if (font == null)
                    throw new InvalidOperationException("TMP font creation returned null: " + sourcePath);
                AssetDatabase.CreateAsset(font, fontPath);
            }

            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.isMultiAtlasTexturesEnabled = true;
            PreserveDynamicDataOnBuild(font);

            string missing;
            bool addResult = font.TryAddCharacters(corpus, out missing, false);
            StringBuilder unresolved = new StringBuilder();
            for (int i = 0; i < corpus.Length; i++)
            {
                if (font.HasCharacter(corpus[i]) == false)
                    unresolved.Append(corpus[i]);
            }
            bool prewarmSuccess = unresolved.Length == 0;
            string missingReport = unresolved.Length == 0 ? "<none>" : EscapeForLog(unresolved.ToString());
            Debug.Log("[Pretendard Typography] PREWARM font=" + name + " success=" + prewarmSuccess + " addResult=" + addResult + " missing=" + missingReport);

            AddFontSubAssets(font, fontPath);
            Material stylePlain = font.material;
            Material styleBase = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AfacadBaseFontPath).material;
            Material styleOutline = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AfacadOutlineFontPath).material;
            Material plain = CreateStyleMaterial(font, stylePlain, fontPath, name + "_Plain.mat");
            Material baseUnderlay = CreateStyleMaterial(font, styleBase, fontPath, name + "_BaseUnderlay.mat");
            Material outlineBlack = CreateStyleMaterial(font, styleOutline, fontPath, name + "_OutlineBlack.mat");
            font.material = plain;
            EditorUtility.SetDirty(font);
            // Generated style materials are external project-owned assets; do not embed them in the font asset.
            return new FontSet { Font = font, Plain = plain, BaseUnderlay = baseUnderlay, OutlineBlack = outlineBlack };
        }

        private static void PreserveDynamicDataOnBuild(TMP_FontAsset font)
        {
            SerializedObject serializedFont = new SerializedObject(font);
            SerializedProperty clearDynamicData = serializedFont.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearDynamicData == null)
                throw new InvalidOperationException("TMP clear-dynamic-data property is missing: " + font.name);
            clearDynamicData.boolValue = false;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateStyleMaterial(TMP_FontAsset font, Material styleSource, string fontPath, string fileName)
        {
            if (styleSource == null || font.material == null)
                throw new InvalidOperationException("TMP style source or generated font material is missing.");

            string path = OutputRoot + "/" + fileName;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = styleSource.shader;
            material.CopyPropertiesFromMaterial(styleSource);
            material.mainTexture = font.atlasTexture;
            material.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static TMP_FontAsset CreateAfacadBridge(TMP_FontAsset fallback)
        {
            TMP_FontAsset bridge = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AfacadBridgePath);
            if (bridge == null)
            {
                if (!AssetDatabase.CopyAsset(AfacadOutlineFontPath, AfacadBridgePath))
                    throw new InvalidOperationException("Could not create project-owned Afacad bridge.");
                AssetDatabase.ImportAsset(AfacadBridgePath, ImportAssetOptions.ForceSynchronousImport);
                bridge = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AfacadBridgePath);
            }

            if (bridge == null)
                throw new InvalidOperationException("Project-owned Afacad bridge did not import.");

            List<TMP_FontAsset> fallbacks = bridge.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
            fallbacks.Clear();
            fallbacks.Add(fallback);
            bridge.fallbackFontAssetTable = fallbacks;
            EditorUtility.SetDirty(bridge);
            Debug.Log("[Pretendard Typography] BRIDGE primary=" + AssetDatabase.GetAssetPath(bridge) + " fallback=" + AssetDatabase.GetAssetPath(fallback));
            return bridge;
        }

        private static void ApplySceneText(GameObject root, string path, FontSet set, Material material)
        {
            TMP_Text text = FindText(root, path);
            text.font = set.Font;
            text.fontSharedMaterial = material;
            EditorUtility.SetDirty(text);
        }

        private static void ApplyPrefabText(GameObject root, string path, TMP_FontAsset font, Material material)
        {
            TMP_Text text = FindText(root, path);
            text.font = font;
            text.fontSharedMaterial = material;
            EditorUtility.SetDirty(text);
        }

        private static TMP_Text FindText(GameObject root, string path)
        {
            Transform target = FindTransform(root, path);
            TMP_Text text = target.GetComponent<TMP_Text>();
            if (text == null)
                throw new InvalidOperationException("Target is not TMP_Text: " + root.name + "/" + path);
            return text;
        }

        private static Transform FindTransform(GameObject root, string path)
        {
            Transform target = string.IsNullOrEmpty(path) ? root.transform : root.transform.Find(path);
            if (target == null)
                throw new InvalidOperationException("Missing TMP target: " + root.name + "/" + path);
            return target;
        }

        private static GameObject FindSceneObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }

            throw new InvalidOperationException("Missing scene root: " + name);
        }

        private static void AddString(ISet<char> characters, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            foreach (char character in value)
                characters.Add(character);
        }

        private static string GetMissingCharacters(TMP_FontAsset font, string corpus)
        {
            StringBuilder missing = new StringBuilder();
            for (int i = 0; i < corpus.Length; i++)
            {
                if (font.HasCharacter(corpus[i]) == false)
                    missing.Append(corpus[i]);
            }

            return missing.ToString();
        }


        private static void AddFontSubAssets(TMP_FontAsset font, string fontPath)
        {
            // Style materials remain external project-owned assets and are assigned below.
            Texture2D[] atlases = font.atlasTextures;
            if (atlases == null)
                return;
            for (int i = 0; i < atlases.Length; i++)
            {
                Texture2D atlas = atlases[i];
                if (atlas != null && AssetDatabase.GetAssetPath(atlas) != fontPath)
                    AssetDatabase.AddObjectToAsset(atlas, fontPath);
            }
        }

        private static string BuildHierarchySignature(GameObject root)
        {
            StringBuilder output = new StringBuilder();
            AppendHierarchy(root.transform, output);
            return output.ToString();
        }

        private static void AppendHierarchy(Transform transform, StringBuilder output)
        {
            output.Append(transform.name).Append('|').Append(transform.childCount).Append(';');
            for (int i = 0; i < transform.childCount; i++)
                AppendHierarchy(transform.GetChild(i), output);
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                return string.Concat(bytes.Select(value => value.ToString("x2")).ToArray());
            }
        }

        private static string EscapeForLog(string value)
        {
            return value.Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void RequireAsset(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                throw new InvalidOperationException("Required asset is missing: " + path);
        }
    }
}
