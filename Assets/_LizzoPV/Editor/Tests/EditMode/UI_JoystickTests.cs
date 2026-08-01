using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UI_JoystickTests
    {
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        GameObject _root;
        GameObject _joystickObject;
        GameObject _touchBG;
        EventSystem _eventSystem;
        UI_Joystick _joystick;
        RectTransform _backgroundRect;
        RectTransform _handlerRect;
        PlayerController _player;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("JoystickTestRoot", typeof(RectTransform), typeof(Canvas));
            _eventSystem = _root.AddComponent<EventSystem>();
            Canvas canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform rootRect = _root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1080.0f, 1920.0f);

            _joystickObject = new GameObject("Joystick", typeof(RectTransform), typeof(UI_Joystick));
            _joystickObject.transform.SetParent(_root.transform, false);
            RectTransform joystickRect = _joystickObject.GetComponent<RectTransform>();
            joystickRect.anchorMin = Vector2.zero;
            joystickRect.anchorMax = Vector2.zero;
            joystickRect.pivot = new Vector2(0.5f, 0.5f);
            joystickRect.anchoredPosition = new Vector2(540.0f, 960.0f);
            joystickRect.sizeDelta = new Vector2(1080.0f, 1920.0f);

            GameObject directionObject = new GameObject("Joystick_Direction", typeof(RectTransform));
            directionObject.transform.SetParent(_joystickObject.transform, false);
            RectTransform directionRect = directionObject.GetComponent<RectTransform>();
            directionRect.anchorMin = new Vector2(0.5f, 0.5f);
            directionRect.anchorMax = new Vector2(0.5f, 0.5f);
            directionRect.pivot = new Vector2(0.5f, 0.5f);
            directionRect.anchoredPosition = Vector2.zero;
            directionRect.sizeDelta = new Vector2(282.0f, 282.0f);

            GameObject backgroundObject = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(directionObject.transform, false);
            _backgroundRect = backgroundObject.GetComponent<RectTransform>();
            _backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            _backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            _backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            _backgroundRect.sizeDelta = new Vector2(282.0f, 282.0f);
            backgroundObject.GetComponent<Image>().raycastTarget = false;

            GameObject handlerObject = new GameObject("Center (1)", typeof(RectTransform), typeof(Image));
            handlerObject.transform.SetParent(directionObject.transform, false);
            _handlerRect = handlerObject.GetComponent<RectTransform>();
            _handlerRect.anchorMin = new Vector2(0.5f, 0.5f);
            _handlerRect.anchorMax = new Vector2(0.5f, 0.5f);
            _handlerRect.pivot = new Vector2(0.5f, 0.5f);
            _handlerRect.anchoredPosition = new Vector2(5.4f, 76.7f);
            _handlerRect.sizeDelta = new Vector2(124.0f, 124.0f);
            handlerObject.GetComponent<Image>().raycastTarget = false;

            _touchBG = new GameObject("TouchBG", typeof(RectTransform), typeof(Image));
            _touchBG.transform.SetParent(_joystickObject.transform, false);
            RectTransform touchBGRect = _touchBG.GetComponent<RectTransform>();
            touchBGRect.anchorMin = Vector2.zero;
            touchBGRect.anchorMax = Vector2.one;
            touchBGRect.anchoredPosition = Vector2.zero;
            touchBGRect.sizeDelta = Vector2.zero;
            Image touchBGImage = _touchBG.GetComponent<Image>();
            touchBGImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            touchBGImage.raycastTarget = true;

            _joystick = _joystickObject.GetComponent<UI_Joystick>();
            SerializedObject serializedJoystick = new SerializedObject(_joystick);
            serializedJoystick.FindProperty("_inputSurface").objectReferenceValue = _touchBG.GetComponent<Image>();
            serializedJoystick.FindProperty("_visual").objectReferenceValue = directionRect;
            serializedJoystick.FindProperty("_background").objectReferenceValue = backgroundObject.GetComponent<Image>();
            serializedJoystick.FindProperty("_handler").objectReferenceValue = handlerObject.GetComponent<Image>();
            serializedJoystick.ApplyModifiedPropertiesWithoutUndo();

            GameObject playerObject = new GameObject("Player");
            _player = playerObject.AddComponent<PlayerController>();
            Assert.IsTrue(_joystick.Init());
            _joystick.BindPlayer(_player);
            _joystick.SetInputEnabled(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            if (_player != null)
                Object.DestroyImmediate(_player.gameObject);
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

        [Test]
        public void PointerDragOutsideBaseMovesOnlyMovableHandleAndResetsToAuthoredCenter()
        {
            Vector2 authoredCenter = _handlerRect.anchoredPosition;
            PointerEventData pointer = new PointerEventData(_eventSystem)
            {
                position = new Vector2(900.0f, 960.0f)
            };

            Assert.IsTrue(_touchBG.GetComponent<Image>().raycastTarget);
            ExecuteEvents.ExecuteHierarchy(_touchBG, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = new Vector2(1100.0f, 960.0f);
            ExecuteEvents.ExecuteHierarchy(_touchBG, pointer, ExecuteEvents.dragHandler);

            Assert.Greater(_handlerRect.anchoredPosition.x, authoredCenter.x);
            Assert.LessOrEqual(_handlerRect.anchoredPosition.magnitude, 79.0f);
            Assert.Greater(_player.MoveDirection.sqrMagnitude, 0.0f);
            Assert.AreEqual(Vector2.zero, _backgroundRect.anchoredPosition);

            _joystick.OnPointerUp(pointer);

            Assert.That(_handlerRect.anchoredPosition.x, Is.EqualTo(authoredCenter.x).Within(0.001f));
            Assert.That(_handlerRect.anchoredPosition.y, Is.EqualTo(authoredCenter.y).Within(0.001f));
            Assert.That(_player.MoveDirection.x, Is.EqualTo(0.0f).Within(0.001f));
            Assert.That(_player.MoveDirection.y, Is.EqualTo(0.0f).Within(0.001f));
        }

        [Test]
        public void DisablingInputAfterDragRestoresAuthoredCenterAndStopsMovement()
        {
            Vector2 authoredCenter = _handlerRect.anchoredPosition;
            PointerEventData pointer = new PointerEventData(_eventSystem)
            {
                position = new Vector2(900.0f, 960.0f)
            };

            ExecuteEvents.ExecuteHierarchy(_touchBG, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = new Vector2(1100.0f, 960.0f);
            ExecuteEvents.ExecuteHierarchy(_touchBG, pointer, ExecuteEvents.dragHandler);
            Assert.Greater(_player.MoveDirection.sqrMagnitude, 0.0f);

            _joystick.SetInputEnabled(false);

            Assert.That(_handlerRect.anchoredPosition.x, Is.EqualTo(authoredCenter.x).Within(0.001f));
            Assert.That(_handlerRect.anchoredPosition.y, Is.EqualTo(authoredCenter.y).Within(0.001f));
            Assert.That(_player.MoveDirection, Is.EqualTo(Vector2.zero));
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
