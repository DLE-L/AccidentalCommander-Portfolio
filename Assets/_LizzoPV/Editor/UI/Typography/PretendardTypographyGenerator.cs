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
    internal static partial class PretendardTypographyGenerator
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

        private static void ApplySceneText(GameObject root, string path, FontSet set, Material material)
        {
            TMP_Text text = FindText(root, path);
            text.font = set.Font;
            text.fontSharedMaterial = material;
            EditorUtility.SetDirty(text);
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

    }
}
