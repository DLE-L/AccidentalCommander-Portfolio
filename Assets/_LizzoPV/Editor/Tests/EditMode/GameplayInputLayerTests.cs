using System.Collections;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class GameplayInputLayerTests
    {
        private const string GameplayCleanScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        [Test]
        public void InputLayerWithoutAuthoredJoystickFailsExplicitConfiguration()
        {
            GameObject root = new GameObject("InputLayerTestRoot");
            try
            {
                GameplayInputLayerController inputLayer = root.AddComponent<GameplayInputLayerController>();
                LogAssert.Expect(LogType.Error, "[GameplayInputLayerController] Authored GameplayFloatingJoystickController reference is required.");

                Assert.That(inputLayer.Configure(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GameplayCleanInputLayerPreservesDonorGeometryAndSingleRaycastOwner()
        {
            Scene scene = EditorSceneManager.GetSceneByPath(GameplayCleanScenePath);
            bool opened = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(GameplayCleanScenePath, OpenSceneMode.Additive);
                opened = true;
            }

            try
            {
                Transform gameplayRoot = FindRoot(scene, "GameplayUIRoot");
                Transform inputLayer = gameplayRoot.Find("InputLayer");
                Transform joystick = inputLayer.Find("Joystick");
                Transform visual = joystick.Find("Visual");
                Transform content = joystick.Find("Content");
                Image background = visual.Find("Background").GetComponent<Image>();
                Image centerAccent = visual.Find("CenterAccent").GetComponent<Image>();
                Image handle = visual.Find("Handle").GetComponent<Image>();
                Image inputSurface = content.Find("InputSurface").GetComponent<Image>();
                Canvas canvas = inputLayer.GetComponent<Canvas>();
                GameplayInputLayerController inputController = inputLayer.GetComponent<GameplayInputLayerController>();
                GameplayRootController rootController = gameplayRoot.GetComponent<GameplayRootController>();

                Assert.That(inputLayer.GetSiblingIndex(), Is.EqualTo(0));
                Assert.That(canvas.overrideSorting, Is.True);
                Assert.That(canvas.sortingOrder, Is.EqualTo(-10));
                Assert.That(inputLayer.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                Assert.That(inputLayer.GetComponent<CanvasScaler>(), Is.Null);
                Assert.That(joystick.GetComponent<Canvas>(), Is.Null);
                Assert.That(inputController.Joystick, Is.EqualTo(joystick.GetComponent<GameplayFloatingJoystickController>()));
                Assert.That(rootController.InputLayer, Is.EqualTo(inputController));

                AssertRect(visual.GetComponent<RectTransform>(), new Vector2(96f, 96f));
                AssertRect(background.rectTransform, new Vector2(96f, 96f));
                AssertRect(centerAccent.rectTransform, new Vector2(42f, 42f));
                AssertRect(handle.rectTransform, new Vector2(42f, 42f));
                Assert.That(handle.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(background.sprite.name, Is.EqualTo("Joystick_Direction_White_Bg"));
                Assert.That(centerAccent.sprite.name, Is.EqualTo("Joystick_Direction_White_Center"));
                Assert.That(handle.sprite.name, Is.EqualTo("Joystick_Direction_Handle"));
                Assert.That(background.color, Is.EqualTo(new Color(0f, 0f, 0f, 0.2f)));
                Assert.That(centerAccent.color, Is.EqualTo(new Color(1f, 1f, 1f, 0.2f)));
                Assert.That(handle.color, Is.EqualTo(Color.white));
                Assert.That(visual.gameObject.activeSelf, Is.False);
                Assert.That(inputSurface.raycastTarget, Is.False);

                Graphic[] graphics = inputLayer.GetComponentsInChildren<Graphic>(true);
                foreach (Graphic graphic in graphics)
                    Assert.That(graphic == inputSurface || !graphic.raycastTarget, Is.True, graphic.name);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void PointerDragUsesFirstPointerAndOnlyMovesVisualAndHandle()
        {
            using InputFixture fixture = new InputFixture();
            PointerEventData first = fixture.CreatePointer(1, new Vector2(800f, 1170f));

            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, first, ExecuteEvents.pointerDownHandler);
            Vector2 firstVisualPosition = fixture.Visual.anchoredPosition;
            PointerEventData second = fixture.CreatePointer(2, new Vector2(300f, 1170f));
            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, second, ExecuteEvents.pointerDownHandler);
            Assert.That(fixture.Visual.anchoredPosition, Is.EqualTo(firstVisualPosition));

            first.position = new Vector2(1200f, 1170f);
            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, first, ExecuteEvents.dragHandler);

            Assert.That(fixture.Player.MoveDirection.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(fixture.Player.MoveDirection.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(fixture.Handle.anchoredPosition.magnitude, Is.LessThanOrEqualTo(27.001f));

            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, first, ExecuteEvents.pointerUpHandler);
            Assert.That(fixture.Handle.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.Player.MoveDirection, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.Visual.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator IdleHideWaitsForPointerReleaseAndUsesUnscaledTime()
        {
            using InputFixture fixture = new InputFixture();
            PointerEventData pointer = fixture.CreatePointer(1, new Vector2(800f, 1170f));

            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(fixture.Visual.gameObject.activeSelf, Is.True);

            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(fixture.Visual.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void DisableResetsMovementHandleAndRaycastSurfaceImmediately()
        {
            using InputFixture fixture = new InputFixture();
            PointerEventData pointer = fixture.CreatePointer(1, new Vector2(800f, 1170f));
            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = new Vector2(1200f, 1170f);
            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.dragHandler);

            Assert.That(fixture.InputLayer.SetInputEnabled(false), Is.True);
            Assert.That(fixture.Player.MoveDirection, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.Handle.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.Visual.gameObject.activeSelf, Is.False);
            Assert.That(fixture.InputSurface.raycastTarget, Is.False);
        }

        [Test]
        public void ReapplyingEnabledStatePreservesActivePointerGesture()
        {
            using InputFixture fixture = new InputFixture();
            PointerEventData pointer = fixture.CreatePointer(1, new Vector2(800f, 1170f));
            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = new Vector2(1200f, 1170f);
            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.dragHandler);

            Vector2 handleBefore = fixture.Handle.anchoredPosition;
            Vector2 movementBefore = fixture.Player.MoveDirection;

            Assert.That(fixture.InputLayer.SetInputEnabled(true), Is.True);
            Assert.That(fixture.Joystick.IsInputEnabled, Is.True);
            Assert.That(fixture.Visual.gameObject.activeSelf, Is.True);
            Assert.That(fixture.InputSurface.raycastTarget, Is.True);
            Assert.That(fixture.Handle.anchoredPosition, Is.EqualTo(handleBefore));
            Assert.That(fixture.Player.MoveDirection, Is.EqualTo(movementBefore));

            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(fixture.Handle.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.Player.MoveDirection, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void DestroyedPlayerClearsInputOnNextPointerEvent()
        {
            using InputFixture fixture = new InputFixture();
            PointerEventData pointer = fixture.CreatePointer(1, new Vector2(800f, 1170f));
            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.pointerDownHandler);

            Object.DestroyImmediate(fixture.Player.gameObject);
            ExecuteEvents.ExecuteHierarchy(fixture.InputSurface.gameObject, pointer, ExecuteEvents.dragHandler);

            Assert.That(fixture.Joystick.IsInputEnabled, Is.False);
            Assert.That(fixture.Handle.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.Visual.gameObject.activeSelf, Is.False);
            Assert.That(fixture.InputSurface.raycastTarget, Is.False);
        }

        [Test]
        public void HigherCanvasControlsWinBeforeEmptyWorldInputSurface()
        {
            using InputFixture fixture = new InputFixture();
            Image pauseEntry = fixture.CreateOverlay("PauseEntry", 0);
            Image modalBlocker = fixture.CreateOverlay("ModalInputBlocker", 10);

            Assert.That(modalBlocker.GetComponent<Canvas>().sortingOrder, Is.EqualTo(10));
            Assert.That(pauseEntry.GetComponent<Canvas>().sortingOrder, Is.EqualTo(0));
            Assert.That(fixture.InputLayer.GetComponent<Canvas>().sortingOrder, Is.EqualTo(-10));
            Assert.That(modalBlocker.raycastTarget, Is.True);
            Assert.That(pauseEntry.raycastTarget, Is.True);
            Assert.That(fixture.InputSurface.raycastTarget, Is.True);
            Assert.That(fixture.InputLayer.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(pauseEntry.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(modalBlocker.GetComponent<GraphicRaycaster>(), Is.Not.Null);
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

        private static void AssertRect(RectTransform rect, Vector2 size)
        {
            Assert.That(rect.sizeDelta, Is.EqualTo(size));
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        private sealed class InputFixture : System.IDisposable
        {
            public readonly GameplayInputLayerController InputLayer;
            public readonly Image InputSurface;
            public readonly RectTransform Visual;
            public readonly RectTransform Handle;
            public readonly PlayerController Player;
            public readonly GameplayFloatingJoystickController Joystick;
            private readonly GameObject _root;
            private readonly EventSystem _eventSystem;

            public InputFixture()
            {
                _root = new GameObject("InputFixture", typeof(RectTransform), typeof(Canvas));
                Canvas rootCanvas = _root.GetComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _root.GetComponent<RectTransform>().sizeDelta = new Vector2(1080f, 2340f);
                _eventSystem = _root.AddComponent<EventSystem>();

                RectTransform layerRect = CreateRect("InputLayer", _root.transform);
                Stretch(layerRect);
                GameObject layerObject = layerRect.gameObject;
                Canvas layerCanvas = layerObject.AddComponent<Canvas>();
                layerCanvas.overrideSorting = true;
                layerCanvas.sortingOrder = -10;
                layerObject.AddComponent<GraphicRaycaster>();
                InputLayer = layerObject.AddComponent<GameplayInputLayerController>();

                RectTransform joystick = CreateRect("Joystick", layerObject.transform);
                Stretch(joystick);
                Joystick = joystick.gameObject.AddComponent<GameplayFloatingJoystickController>();
                Visual = CreateRect("Visual", joystick);
                SetFixed(Visual, new Vector2(96f, 96f));
                RectTransform content = CreateRect("Content", joystick);
                Stretch(content);

                Image background = CreateRect("Background", Visual).gameObject.AddComponent<Image>();
                SetFixed(background.rectTransform, new Vector2(96f, 96f));
                background.raycastTarget = false;
                Image centerAccent = CreateRect("CenterAccent", Visual).gameObject.AddComponent<Image>();
                SetFixed(centerAccent.rectTransform, new Vector2(42f, 42f));
                centerAccent.raycastTarget = false;
                Handle = CreateRect("Handle", Visual);
                SetFixed(Handle, new Vector2(42f, 42f));
                Handle.gameObject.AddComponent<Image>().raycastTarget = false;
                InputSurface = CreateRect("InputSurface", content).gameObject.AddComponent<Image>();
                Stretch(InputSurface.rectTransform);
                InputSurface.color = new Color(1f, 1f, 1f, 0f);

                SetField(InputLayer, "_joystick", Joystick);
                SetField(Joystick, "_inputSurface", InputSurface);
                SetField(Joystick, "_visual", Visual);
                SetField(Joystick, "_background", background);
                SetField(Joystick, "_centerAccent", centerAccent);
                SetField(Joystick, "_handle", Handle.GetComponent<Image>());

                Player = new GameObject("Player").AddComponent<PlayerController>();
                Assert.That(InputLayer.Configure(), Is.True);
                Assert.That(InputLayer.BindPlayer(Player), Is.True);
                Assert.That(InputLayer.SetInputEnabled(true), Is.True);
            }

            public PointerEventData CreatePointer(int pointerId, Vector2 position)
            {
                return new PointerEventData(_eventSystem)
                {
                    pointerId = pointerId,
                    position = position
                };
            }

            public Image CreateOverlay(string name, int sortingOrder)
            {
                GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
                overlay.transform.SetParent(_root.transform, false);
                RectTransform rect = overlay.GetComponent<RectTransform>();
                Stretch(rect);
                Canvas canvas = overlay.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
                return overlay.GetComponent<Image>();
            }

            public void Dispose()
            {
                if (Player != null)
                    Object.DestroyImmediate(Player.gameObject);
                Object.DestroyImmediate(_root);
            }

            private static RectTransform CreateRect(string name, Transform parent)
            {
                GameObject gameObject = new GameObject(name, typeof(RectTransform));
                RectTransform rect = gameObject.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                return rect;
            }

            private static void SetField(Object target, string fieldName, Object value)
            {
                SerializedObject serialized = new SerializedObject(target);
                serialized.FindProperty(fieldName).objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            private static void Stretch(RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            private static void SetFixed(RectTransform rect, Vector2 size)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = size;
            }
        }
    }
}
