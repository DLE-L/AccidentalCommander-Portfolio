using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using System.Collections.Generic;
using System.Linq;
using Lizzo.PV.Gameplay.CardOffer;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTests.EditMode
{
    public sealed class PretendardRuntimeTypographyTests
    {
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string OutputRoot = "Assets/_LizzoPV/Shared/UI/Typography/Pretendard/TMP";
        private const string SemiBoldPath = OutputRoot + "/Pretendard-SemiBold SDF.asset";
        private const string ExtraBoldPath = OutputRoot + "/Pretendard-ExtraBold SDF.asset";
        private const string BlackPath = OutputRoot + "/Pretendard-Black SDF.asset";
        private const string AfacadBridgePath = OutputRoot + "/AfacadFlux-ExtraBold SDF_OutlineBlack_PretendardFallback.asset";
        private const string LtAvocadoBridgePath = OutputRoot + "/LTAvocado-Bold SDF_OutlineBlack_PretendardFallback.asset";
        private const string ExtraBoldMaterialPath = OutputRoot + "/Pretendard-ExtraBold_OutlineBlack.mat";
        private const string AfacadMaterialPath = AfacadBridgePath;
        private const string LtAvocadoMaterialPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Font/LTAvocado-Bold SDF_OutlineBlack.asset";

        private static readonly string[] RuntimeWorldFeedbackCorpus =
        {
            "0123456789×★+-/%!",
            "쓰러짐",
            "회복!",
            "근위대 결성!",
            "방패 진형 전개!",
            "돌격수 격파!",
            "지금 공격!",
            "거인이 흔들립니다!",
            "방패 균열!",
            "방패병 합류!",
            "방패대장 합류!",
            "검병 합류!",
            "성직자 합류!",
            "궁수 합류!",
            "동료 합류!",
            "EXP!"
        }
        ;

        [Test]
        public void DynamicPretendardFontsRetainDynamicDataOnBuild()
        {
            AssertClearDynamicDataOnBuildIsFalse(SemiBoldPath);
            AssertClearDynamicDataOnBuildIsFalse(ExtraBoldPath);
            AssertClearDynamicDataOnBuildIsFalse(BlackPath);
        }

        [Test]
        public void RuntimeWorldTypographyCleanupContractIsSatisfied()
        {
            Scene gameplayScene = EditorSceneManager.GetSceneByPath(GameplayScenePath);
            bool openedGameplayScene = false;
            if (!gameplayScene.isLoaded)
            {
                gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
                openedGameplayScene = true;
            }

            try
            {
                TMP_FontAsset semiBold = LoadFont(SemiBoldPath);
                TMP_FontAsset extraBold = LoadFont(ExtraBoldPath);
                TMP_FontAsset afacadBridge = LoadFont(AfacadBridgePath);
                TMP_FontAsset ltAvocadoBridge = LoadFont(LtAvocadoBridgePath);

                AssertDefaultFont(semiBold);
                AssertFallbackTable(afacadBridge, ExtraBoldPath);
                AssertFallbackTable(ltAvocadoBridge, SemiBoldPath);

                foreach (string value in RuntimeWorldFeedbackCorpus)
                {
                    AssertResolved(extraBold, value);
                    AssertResolved(afacadBridge, value);
                }

                GameObject partyUnitBase = LoadPrefab("Assets/_LizzoPV/Gameplay/Legion/Prefabs/Base/PartyUnitBase.prefab");
                AssertText(partyUnitBase, "UI/HpBarAnchor/DownMarker", ExtraBoldPath, ExtraBoldMaterialPath);
                AssertText(partyUnitBase, "UI/HpBarAnchor/CompanionHPBar/Text", LtAvocadoMaterialPath, LtAvocadoMaterialPath);

                GameObject commander = LoadPrefab("Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab");
                AssertText(commander, "CommanderHPBar/Text", LtAvocadoMaterialPath, LtAvocadoMaterialPath);

                GameObject floatingDamageText = LoadPrefab("Assets/_LizzoPV/Gameplay/Combat/Prefabs/Effects/FloatingDamageText.prefab");
                AssertText(floatingDamageText, "", AfacadBridgePath, AfacadMaterialPath);

                Transform gameplayUiRoot = FindTransform(gameplayScene.GetRootGameObjects().Single(root => root.name == "GameplayUIRoot").transform, "");
                Transform companionSlotRow = FindTransform(
                    gameplayUiRoot,
                    "DecisionLayer/Pause/Content/BuildSummary/Companions/Content/Slots");
                foreach (TMP_Text countText in companionSlotRow
                    .GetComponentsInChildren<TMP_Text>(true)
                    .Where(text => text.name == "CountText"))
                {
                    Assert.That(AssetDatabase.GetAssetPath(countText.font), Is.EqualTo(LtAvocadoBridgePath), countText.name);
                    Assert.That(AssetDatabase.GetAssetPath(countText.fontSharedMaterial), Is.EqualTo(LtAvocadoBridgePath), countText.name);
                AssertNoFallbackMaterials(countText);
                }

                AssertNoFallbackMaterials(
                    FindText(gameplayUiRoot, "DecisionLayer/CardOffer/Content/CardRow/CardItem01/Content/DescriptionArea/DescriptionText"),
                    0);
                AssertCardDescriptionsUseReadableTextColor(gameplayUiRoot);
                AssertNoFallbackMaterials(FindText(gameplayUiRoot, "DecisionLayer/CardOffer/Content/Header/Content/GuideText"));
                AssertNoFallbackMaterials(FindText(gameplayUiRoot, "DecisionLayer/Pause/Content/Actions/ResumeButton/Content/LabelText"));
                AssertNoFallbackMaterials(FindText(gameplayUiRoot, "DecisionLayer/Pause/Content/Actions/AbandonButton/Content/LabelText"));
            }
            finally
            {
                if (openedGameplayScene)
                    EditorSceneManager.CloseScene(gameplayScene, true);
            }
        }

        private static TMP_FontAsset LoadFont(string path)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        private static void AssertClearDynamicDataOnBuildIsFalse(string path)
        {
            TMP_FontAsset font = LoadFont(path);
            Assert.That(font, Is.Not.Null, path);
            SerializedProperty clearDynamicData = new SerializedObject(font).FindProperty("m_ClearDynamicDataOnBuild");
            Assert.That(clearDynamicData, Is.Not.Null, path);
            Assert.That(clearDynamicData.boolValue, Is.False, path);
        }

        private static GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void AssertDefaultFont(TMP_FontAsset expected)
        {
            Object settings = AssetDatabase.LoadMainAssetAtPath(TmpSettingsPath);
            SerializedObject serialized = new SerializedObject(settings);
            Assert.That(serialized.FindProperty("m_defaultFontAsset").objectReferenceValue, Is.SameAs(expected));
        }

        private static void AssertFallbackTable(TMP_FontAsset font, string expectedFallbackPath)
        {
            Assert.That(font.fallbackFontAssetTable, Is.Not.Null);
            Assert.That(font.fallbackFontAssetTable.Count, Is.EqualTo(1));
            Assert.That(AssetDatabase.GetAssetPath(font.fallbackFontAssetTable[0]), Is.EqualTo(expectedFallbackPath));
        }

        private static void AssertResolved(TMP_FontAsset font, string value)
        {
            foreach (char character in value)
                Assert.That(
                    HasResolvedCharacter(font, character, new HashSet<TMP_FontAsset>()),
                    Is.True,
                    font.name + " missing " + character + " from " + value);
        }

        private static bool HasResolvedCharacter(TMP_FontAsset font, char character, HashSet<TMP_FontAsset> visited)
        {
            if (font == null || !visited.Add(font))
                return false;
            if (font.HasCharacter(character))
                return true;
            if (font.fallbackFontAssetTable == null)
                return false;
            return font.fallbackFontAssetTable.Any(fallback => HasResolvedCharacter(fallback, character, visited));
        }

        private static void AssertText(GameObject root, string path, string expectedFontPath, string expectedMaterialPath)
        {
            TMP_Text text = FindText(root.transform, path);
            Assert.That(AssetDatabase.GetAssetPath(text.font), Is.EqualTo(expectedFontPath), path);
            Assert.That(AssetDatabase.GetAssetPath(text.fontSharedMaterial), Is.EqualTo(expectedMaterialPath), path);
        }

        private static void AssertNoFallbackMaterials(TMP_Text text, int expectedFontMaterials = 1)
        {
            SerializedObject serialized = new SerializedObject(text);
            Assert.That(serialized.FindProperty("m_fontSharedMaterials").arraySize, Is.EqualTo(0), text.name);
            Assert.That(serialized.FindProperty("m_fontMaterials").arraySize, Is.EqualTo(expectedFontMaterials), text.name);
        }

        private static void AssertCardDescriptionsUseReadableTextColor(Transform gameplayUiRoot)
        {
            for (int index = 1; index <= 3; index++)
            {
                string itemPath = $"DecisionLayer/CardOffer/Content/CardRow/CardItem0{index}";
                Transform item = FindTransform(gameplayUiRoot, itemPath);
                GameplayCardOfferItemView view = item.GetComponent<GameplayCardOfferItemView>();
                TMP_Text title = FindText(item, "Content/CardNameText");
                TMP_Text description = FindText(item, "Content/DescriptionArea/DescriptionText");

                Assert.That(view.Configure(), Is.True, itemPath);
                Assert.That(description.color, Is.EqualTo(title.color), itemPath);
                Assert.That(description.color.grayscale, Is.LessThan(0.35f), itemPath);
            }
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform target = FindTransform(root, path);
            return target.GetComponent<TMP_Text>();
        }

        private static Transform FindTransform(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;
            Transform target = root.Find(path);
            Assert.That(target, Is.Not.Null, root.name + "/" + path);
            return target;
        }
    }
}
