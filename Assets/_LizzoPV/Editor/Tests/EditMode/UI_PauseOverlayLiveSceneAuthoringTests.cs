using System.Linq;
using Lizzo.PV.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UI_PauseOverlayLiveSceneAuthoringTests
    {
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private const string SemiBoldFontPath = "Assets/_LizzoPV/Fonts/Pretendard/TMP/Pretendard-SemiBold SDF.asset";
        private const string SynergyListPath = "GameplayUIRoot/HUD/PauseOverlay/Panel/InfoScroll/Viewport/Content/SynergySection/SynergyList";

        [Test]
        public void GameplayPauseOverlay_HasBoundSemanticSynergyEmptyState()
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
                Transform gameplayUiRoot = gameplayScene.GetRootGameObjects()
                    .Single(root => root.name == "GameplayUIRoot")
                    .transform;
                Transform synergyList = FindTransform(gameplayUiRoot, SynergyListPath.Substring("GameplayUIRoot/".Length));
                UI_PauseOverlay overlay = FindTransform(gameplayUiRoot, "HUD/PauseOverlay").GetComponent<UI_PauseOverlay>();

                Assert.That(overlay, Is.Not.Null);
                if (new SerializedObject(overlay).FindProperty("_synergyEmptyStateText").objectReferenceValue == null)
                {
                    LogAssert.Expect(LogType.Error, "[UI_PauseOverlay] Required authored references are incomplete or companion/passive arrays are not exactly 7/5 entries.");
                }

                Assert.That(overlay.Validate(), Is.True);

                Transform emptyState = synergyList.Find("SynergyEmptyStateText");
                Assert.That(emptyState, Is.Not.Null);
                TextMeshProUGUI emptyStateText = emptyState.GetComponent<TextMeshProUGUI>();
                Assert.That(emptyStateText, Is.Not.Null);
                Assert.That(emptyStateText.text, Is.EqualTo("활성 시너지 없음"));
                Assert.That(emptyStateText.alignment, Is.EqualTo(TextAlignmentOptions.Center));
                Assert.That(emptyStateText.raycastTarget, Is.False);
                Assert.That(AssetDatabase.GetAssetPath(emptyStateText.font), Is.EqualTo(SemiBoldFontPath));

                RectTransform rectTransform = emptyState.GetComponent<RectTransform>();
                Assert.That(rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(rectTransform.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(rectTransform.sizeDelta, Is.EqualTo(Vector2.zero));

                TMP_Text boundEmptyState = new SerializedObject(overlay)
                    .FindProperty("_synergyEmptyStateText")
                    .objectReferenceValue as TMP_Text;
                Assert.That(boundEmptyState, Is.SameAs(emptyStateText));
                Assert.That(synergyList.childCount, Is.EqualTo(1));
                Assert.That(synergyList.GetChild(0), Is.SameAs(emptyState));
                for (int index = 0; index < 4; index++)
                    Assert.That(synergyList.Find("PassiveSlot_0" + index), Is.Null);
            }
            finally
            {
                if (openedGameplayScene)
                    EditorSceneManager.CloseScene(gameplayScene, true);
            }
        }

        private static Transform FindTransform(Transform root, string path)
        {
            Transform target = root.Find(path);
            Assert.That(target, Is.Not.Null, root.name + "/" + path);
            return target;
        }
    }
}
