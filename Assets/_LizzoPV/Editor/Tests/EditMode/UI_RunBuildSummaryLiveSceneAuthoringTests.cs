using System.Linq;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UI_RunBuildSummaryLiveSceneAuthoringTests
    {
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        [Test]
        public void GameplayResultViews_HaveBoundSevenFiveAndSynergySummaryContracts()
        {
            Scene gameplay = EditorSceneManager.GetSceneByPath(GameplayScenePath);
            bool opened = false;
            if (!gameplay.isLoaded)
            {
                gameplay = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
                opened = true;
            }

            try
            {
                Transform root = gameplay.GetRootGameObjects().Single(item => item.name == "GameplayUIRoot").transform;
                UI_RunResultPopup popup = root.Find("ResultPopup").GetComponent<UI_RunResultPopup>();
                Assert.That(popup, Is.Not.Null);
                Assert.That(popup.ConfigureForClear(), Is.True);
                Assert.That(popup.ConfigureForFailure(), Is.True);
                Assert.That(popup.Configure(), Is.True);

                AssertSummary(root.Find("ResultPopup/SafeAreaContent/MainResult"), false);
                AssertSummary(root.Find("ResultPopup/SafeAreaContent/FailureResult"), true);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(gameplay, true);
            }
        }

        private static void AssertSummary(Transform result, bool requirePassiveBindings)
        {
            UI_RunBuildSummaryView summary = result.GetComponent<UI_RunBuildSummaryView>();
            Assert.That(summary, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(summary);
            SerializedProperty companions = serialized.FindProperty("_companionSlots");
            SerializedProperty passives = serialized.FindProperty("_passiveSlots");
            Assert.That(companions.arraySize, Is.EqualTo(7));
            Assert.That(passives.arraySize, Is.EqualTo(5));
            Assert.That(serialized.FindProperty("_synergySummaryText").objectReferenceValue, Is.Not.Null);
            Assert.That(
                ((TMP_Text)serialized.FindProperty("_synergySummaryText").objectReferenceValue).raycastTarget,
                Is.False);

            for (int i = 0; i < companions.arraySize; i++)
            {
                Assert.That(companions.GetArrayElementAtIndex(i).FindPropertyRelative("_root").objectReferenceValue, Is.Not.Null);
                Assert.That(companions.GetArrayElementAtIndex(i).FindPropertyRelative("_icon").objectReferenceValue, Is.Not.Null);
                Assert.That(companions.GetArrayElementAtIndex(i).FindPropertyRelative("_countText").objectReferenceValue, Is.Not.Null);
            }

            for (int i = 0; i < passives.arraySize; i++)
            {
                TMP_Text level = passives.GetArrayElementAtIndex(i).FindPropertyRelative("_levelText").objectReferenceValue as TMP_Text;
                if (requirePassiveBindings)
                {
                    Assert.That(level, Is.Not.Null);
                    Assert.That(level.name, Is.EqualTo("LevelText"));
                    Assert.That(level.raycastTarget, Is.False);
                }
                else
                {
                    Assert.That(level, Is.Null);
                }
            }
        }
    }
}
