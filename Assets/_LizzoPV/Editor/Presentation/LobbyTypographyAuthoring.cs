using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools
{
    /// <summary>
    /// Central Editor-only owner for project-owned Lobby TMP role assignment.
    /// Screen authoring delegates its final save through LobbyUiAuthoringContext,
    /// which invokes this pass so later screen work cannot reintroduce legacy fonts.
    /// </summary>
    public static class LobbyTypographyAuthoring
    {
        const string PretendardRoot = "Assets/_LizzoPV/Fonts/Pretendard/TMP/";
        const string SemiBoldPath = PretendardRoot + "Pretendard-SemiBold SDF.asset";
        const string ExtraBoldPath = PretendardRoot + "Pretendard-ExtraBold SDF.asset";
        const string BlackPath = PretendardRoot + "Pretendard-Black SDF.asset";
        const string SemiBoldPlainPath = PretendardRoot + "Pretendard-SemiBold_Plain.mat";
        const string ExtraBoldPlainPath = PretendardRoot + "Pretendard-ExtraBold_Plain.mat";
        const string BlackPlainPath = PretendardRoot + "Pretendard-Black_Plain.mat";
        const string LtAvocadoBridgePath = PretendardRoot + "LTAvocado-Bold SDF_OutlineBlack_PretendardFallback.asset";
        static readonly string[] RuntimeExtraBoldCorpus =
        {
            "0123456789×★+-/%!", "쓰러짐", "회복!", "근위대 결성!", "방패 진형 전개!",
            "돌격수 격파!", "지금 공격!", "거인이 흔들립니다!", "방패 균열!", "방패병 합류!",
            "방패대장 합류!", "검병 합류!", "성직자 합류!", "궁수 합류!", "동료 합류!", "EXP!"
        };

        enum Role
        {
            SemiBold,
            ExtraBold,
            Black,
            Currency
        }

        struct RoleAssets
        {
            public TMP_FontAsset Font;
            public Material Material;
        }

        [MenuItem("Lizzo/Authoring/Lobby/Apply Typography Roles")]
        public static void Apply()
        {
            Scene lobby = LobbyUiAuthoringContext.GetLoadedLobby(nameof(LobbyTypographyAuthoring));
            if (lobby.IsValid() == false)
                return;

            ApplyToScene(lobby);
            LobbyUiAuthoringContext.MarkSceneDirtyAndSave(lobby);
            Debug.Log("[LobbyTypographyAuthoring] Applied centralized Lobby typography roles.");
        }

        public static int ApplyToScene(Scene scene)
        {
            if (scene.IsValid() == false || scene.isLoaded == false)
                throw new InvalidOperationException("Lobby must be loaded before typography authoring.");

            RoleAssets semiBold = LoadRole(SemiBoldPath, SemiBoldPlainPath);
            RoleAssets extraBold = LoadRole(ExtraBoldPath, ExtraBoldPlainPath);
            RoleAssets black = LoadRole(BlackPath, BlackPlainPath);
            TMP_FontAsset currencyFont = RequireFont(LtAvocadoBridgePath);
            Material currencyMaterial = currencyFont.material;
            if (currencyMaterial == null)
                throw new InvalidOperationException("Currency bridge material is missing: " + LtAvocadoBridgePath);

            List<TMP_Text> projectOwnedTexts = new List<TMP_Text>();
            Dictionary<Role, HashSet<char>> roleCharacters = new Dictionary<Role, HashSet<char>>
            {
                { Role.SemiBold, new HashSet<char>() },
                { Role.ExtraBold, new HashSet<char>() },
                { Role.Black, new HashSet<char>() }
            };

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    projectOwnedTexts.Add(text);
                    Role role = ResolveRole(text);
                    AddCharacters(roleCharacters, role, text.text);
                    ApplyRole(text, role, semiBold, extraBold, black, currencyFont, currencyMaterial);
                    if (IsBattleNavigationLabel(text))
                    {
                        ClearLegacyMaterialOverrides(text);
                        text.fontSharedMaterial = extraBold.Material;
                        EditorUtility.SetDirty(text);
                    }
                }
            }

            foreach (string value in RuntimeExtraBoldCorpus)
                AddCharacters(roleCharacters, Role.ExtraBold, value);

            Prewarm(semiBold.Font, roleCharacters[Role.SemiBold], "SemiBold");
            Prewarm(extraBold.Font, roleCharacters[Role.ExtraBold], "ExtraBold");
            Prewarm(black.Font, roleCharacters[Role.Black], "Black");
            AssetDatabase.SaveAssets();

            if (projectOwnedTexts.Count != 138)
                throw new InvalidOperationException("Expected 138 project-owned Lobby TMP components, found " + projectOwnedTexts.Count + ".");
            return projectOwnedTexts.Count;
        }

        static RoleAssets LoadRole(string fontPath, string materialPath)
        {
            TMP_FontAsset font = RequireFont(fontPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                throw new InvalidOperationException("Required typography material is missing: " + materialPath);
            return new RoleAssets { Font = font, Material = material };
        }

        static TMP_FontAsset RequireFont(string path)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
                throw new InvalidOperationException("Required typography font is missing: " + path);
            return font;
        }

        static void ApplyRole(TMP_Text text, Role role, RoleAssets semiBold, RoleAssets extraBold,
            RoleAssets black, TMP_FontAsset currencyFont, Material currencyMaterial)
        {
            switch (role)
            {
                case Role.Black:
                    text.font = black.Font;
                    text.fontSharedMaterial = black.Material;
                    break;
                case Role.ExtraBold:
                    text.font = extraBold.Font;
                    text.fontSharedMaterial = extraBold.Material;
                    break;
                case Role.Currency:
                    text.font = currencyFont;
                    text.fontSharedMaterial = currencyMaterial;
                    break;
                default:
                    text.font = semiBold.Font;
                    text.fontSharedMaterial = semiBold.Material;
                    break;
            }

            text.fontStyle = FontStyles.Normal;
            text.raycastTarget = false;
            EditorUtility.SetDirty(text);
        }

        static Role ResolveRole(TMP_Text text)
        {
            string path = HierarchyPath(text.transform);
            if (path.EndsWith("PersistentHeader/Title", StringComparison.Ordinal) ||
                path.EndsWith("BattleFocalPoint/ModeTitle", StringComparison.Ordinal))
                return Role.Black;

            if ((path.Contains("GoldCurrencySlot") || path.Contains("GemCurrencySlot")) && text.name == "ValueText")
                return Role.Currency;
            if (text.name == "PriceText" || path.Contains("/GoldCostRow/"))
                return Role.Currency;

            if (text.name == "TitleText" || text.name == "Label" || text.name == "LabelText" ||
                text.name == "StatusText" || text.name == "StatusIndicator" || text.name == "RoleText" ||
                path.Contains("Button") || path.Contains("NavButton"))
                return Role.ExtraBold;

            return Role.SemiBold;
        }

        static string HierarchyPath(Transform transform)
        {
            List<string> names = new List<string>();
            while (transform != null)
            {
                names.Add(transform.name);
                transform = transform.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        static bool IsBattleNavigationLabel(TMP_Text text)
        {
            return HierarchyPath(text.transform).EndsWith("/BottomNavigation/BattleNavButton/Label", StringComparison.Ordinal);
        }

        static void ClearLegacyMaterialOverrides(TMP_Text text)
        {
            ClearMaterialOverrides(text);
            EditorUtility.SetDirty(text);
        }

        static void ClearMaterialOverrides(TMP_Text text)
        {
            SerializedObject serialized = new SerializedObject(text);
            SerializedProperty sharedMaterials = serialized.FindProperty("m_fontSharedMaterials");
            SerializedProperty material = serialized.FindProperty("m_fontMaterial");
            SerializedProperty materials = serialized.FindProperty("m_fontMaterials");
            if (sharedMaterials == null || material == null || materials == null)
                throw new InvalidOperationException("TMP material override properties are missing on " + text.name + ".");

            sharedMaterials.arraySize = 0;
            material.objectReferenceValue = null;
            materials.arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddCharacters(Dictionary<Role, HashSet<char>> roleCharacters, Role role, string value)
        {
            if (role == Role.Currency || string.IsNullOrEmpty(value))
                return;

            foreach (char character in value)
                roleCharacters[role].Add(character);
        }

        static void Prewarm(TMP_FontAsset font, HashSet<char> characters, string label)
        {
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.isMultiAtlasTexturesEnabled = true;
            string corpus = new string(ToSortedArray(characters));
            string missing;
            font.TryAddCharacters(corpus, out missing, false);
            StringBuilderMissing(font, corpus, out string unresolved);
            EditorUtility.SetDirty(font);
            if (unresolved.Length > 0)
                throw new InvalidOperationException("Missing " + label + " glyphs: " + unresolved);
        }

        static char[] ToSortedArray(HashSet<char> characters)
        {
            char[] values = new char[characters.Count];
            characters.CopyTo(values);
            Array.Sort(values);
            return values;
        }

        static void StringBuilderMissing(TMP_FontAsset font, string corpus, out string unresolved)
        {
            List<char> missing = new List<char>();
            foreach (char character in corpus)
            {
                if (font.HasCharacter(character) == false)
                    missing.Add(character);
            }

            unresolved = new string(missing.ToArray());
        }
    }
}
