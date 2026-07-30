using System.Runtime.Serialization;
using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Config;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class HumanGatePartySpacingJoystickAuthoringTests
    {
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        [Test]
        public void FormationWorldOffset_UsesApprovedSpacingForRadialSeparation()
        {
            RuntimeObjectRegistry registry = (RuntimeObjectRegistry)FormatterServices.GetUninitializedObject(typeof(RuntimeObjectRegistry));
            PartyService party = (PartyService)FormatterServices.GetUninitializedObject(typeof(PartyService));
            System.Type formationType = typeof(PartyService).Assembly.GetType("Lizzo.PV.Legion.FormationService");
            object formation = System.Activator.CreateInstance(
                formationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { registry, party },
                null);

            MethodInfo resolveWorldOffset = formationType.GetMethod("ResolveWorldOffset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Vector3 worldOffset = (Vector3)resolveWorldOffset.Invoke(formation, new object[] { Vector3.right, "front_left_01" });

            Assert.That(RemoteConfig.FormationSpacing, Is.EqualTo(0.85f).Within(0.0001f));
            Assert.That(worldOffset.x, Is.EqualTo(0.85f).Within(0.0001f));
            Assert.That(worldOffset.magnitude, Is.GreaterThan(0.60f));
        }

        [Test]
        public void GameplayScene_JoystickVisualAuthoring_UsesCompactDimensionsAndFullScreenTouch()
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
                Transform gameplayUiRoot = FindRoot(gameplayScene, "GameplayUIRoot");
                Transform joystick = gameplayUiRoot.Find("Joystick");
                Assert.That(joystick, Is.Not.Null);

                Assert.That(joystick.Find("Joystick_Direction").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(96.0f, 96.0f)));
                Assert.That(joystick.Find("Joystick_Direction/Bg").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(96.0f, 96.0f)));
                Assert.That(joystick.Find("Joystick_Direction/Center").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(42.0f, 42.0f)));
                Assert.That(joystick.Find("Joystick_Direction/Center (1)").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(42.0f, 42.0f)));

                Transform touchBg = joystick.Find("TouchBG");
                Assert.That(touchBg, Is.Not.Null);
                Assert.That(touchBg.GetComponent<RectTransform>().anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(touchBg.GetComponent<RectTransform>().anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(touchBg.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(Vector2.zero));
                Assert.That(touchBg.GetComponent<Image>().raycastTarget, Is.True);
            }
            finally
            {
                if (openedGameplayScene)
                    EditorSceneManager.CloseScene(gameplayScene, true);
            }
        }

        private static Transform FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root.transform;
            }

            Assert.Fail("Missing root: " + name);
            return null;
        }
    }
}
