using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Lizzo.PV.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayNonUiCompositionTests
    {
        const string ScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        const string ExpectedCameraHash = "D55ADC28F9BF7F62FE94DDA0C4939E2A6DD6C09D7520579DD6B50BDEFBB56110";

        [Test]
        public void PresentationCatalogProvider_ExecutesBetweenAppAndRunBootstrap()
        {
            Assert.That(GetExecutionOrder<AppBootstrap>(), Is.EqualTo(-1000));
            Assert.That(GetExecutionOrder<PresentationCatalogProvider>(), Is.EqualTo(-950));
            Assert.That(GetExecutionOrder<RunBootstrap>(), Is.EqualTo(-900));
        }

        [Test]
        public void Gameplay_ContainsBindableNonUiComposition_AndPreservesExistingUiAndCamera()
        {
            Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = false;
            if (scene.IsValid() == false || scene.isLoaded == false)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedForTest = true;
            }

            try
            {
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isDirty, Is.False);
                Assert.That(scene.GetRootGameObjects(), Has.Length.EqualTo(9));

                GameObject app = FindRoot(scene, "@App");
                GameObject run = FindRoot(scene, "@Run");
                GameObject camera = FindRoot(scene, "MainCamera");
                GameObject ui = FindRoot(scene, "GameplayUIRoot");

                Assert.That(app.GetComponents<AppBootstrap>(), Has.Length.EqualTo(1));
                Assert.That(run.GetComponents<GameScene>(), Has.Length.EqualTo(1));
                Assert.That(run.GetComponents<RunPauseController>(), Has.Length.EqualTo(1));
                Assert.That(run.GetComponents<StageSpawner>(), Has.Length.EqualTo(1));
                Assert.That(run.GetComponents<EliteSpawnController>(), Has.Length.EqualTo(1));
                Assert.That(run.GetComponents<BossSpawnController>(), Has.Length.EqualTo(1));
                Assert.That(run.GetComponent<StageSpawner>().enabled, Is.False);
                Assert.That(run.GetComponent<EliteSpawnController>().enabled, Is.False);
                Assert.That(run.GetComponent<BossSpawnController>().enabled, Is.False);
                Assert.That(run.GetComponents<RunBootstrap>(), Has.Length.EqualTo(1));
                Assert.That(run.GetComponents<PresentationCatalogProvider>(), Has.Length.EqualTo(1));

                PresentationCatalogProvider presentationCatalogProvider = run.GetComponent<PresentationCatalogProvider>();
                Assert.That(presentationCatalogProvider.enabled, Is.True);
                Assert.That(GetObjectReference(presentationCatalogProvider, "_catalog"), Is.SameAs(AssetDatabase.LoadAssetAtPath<PresentationCatalog>("Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset")));

                GameObject poolRoot = FindChild(run, "PoolRoot");
                GameObject previewTarget = FindChild(run, "BossDirectionPreviewTarget");
                GameObject safeObject = FindChild(run, "SafeKnockbackWorld");
                Assert.That(poolRoot.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(previewTarget.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(safeObject.transform.localPosition, Is.EqualTo(Vector3.zero));

                BoxCollider2D safeBoundary = safeObject.GetComponent<BoxCollider2D>();
                SafeKnockbackWorld safeWorld = safeObject.GetComponent<SafeKnockbackWorld>();
                Assert.That(safeBoundary, Is.Not.Null);
                Assert.That(safeBoundary.isTrigger, Is.True);
                Assert.That(safeBoundary.size, Is.EqualTo(new Vector2(100.0f, 100.0f)));
                Assert.That(GetObjectReference(safeWorld, "_boundaryCollider"), Is.SameAs(safeBoundary));
                Assert.That(GetObjectReferenceArrayLength(safeWorld, "_obstacleColliders"), Is.EqualTo(0));

                GameScene gameScene = run.GetComponent<GameScene>();
                Assert.That(GetObjectReference(gameScene, "_stageSpawner"), Is.SameAs(run.GetComponent<StageSpawner>()));
                Assert.That(GetObjectReference(gameScene, "_eliteSpawnController"), Is.SameAs(run.GetComponent<EliteSpawnController>()));
                Assert.That(GetObjectReference(gameScene, "_bossSpawnController"), Is.SameAs(run.GetComponent<BossSpawnController>()));
                Assert.That(GetObjectReference(run.GetComponent<BossSpawnController>(), "_authoredBossDirectionPreviewTarget"), Is.SameAs(previewTarget.transform));

                Assert.That(camera.transform.localPosition, Is.EqualTo(new Vector3(0.0f, 0.0f, -10.0f)));
                Assert.That(camera.GetComponent<CameraController>(), Is.Not.Null);
                Assert.That(GetObjectReference(camera.GetComponent<CameraController>(), "_visibilityZone"), Is.SameAs(FindChild(camera, "CameraVisibilityZone").GetComponent<CameraVisibilityZone>()));
                Assert.That(GetObjectReference(camera.GetComponent<CameraController>(), "Target"), Is.Null);
                Assert.That(camera.GetComponent<BoxCollider2D>(), Is.Null);
                Assert.That(camera.GetComponent<Rigidbody2D>(), Is.Null);

                GameObject visibilityZone = FindChild(camera, "CameraVisibilityZone");
                Assert.That(visibilityZone.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(visibilityZone.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
                Assert.That(visibilityZone.GetComponent<Rigidbody2D>().constraints, Is.EqualTo(RigidbodyConstraints2D.FreezeRotation));
                Assert.That(visibilityZone.GetComponent<BoxCollider2D>().isTrigger, Is.True);
                Assert.That(visibilityZone.GetComponent<BoxCollider2D>().size, Is.EqualTo(Vector2.one));
                Assert.That(GetObjectReference(visibilityZone.GetComponent<CameraVisibilityZone>(), "_zoneCollider"), Is.SameAs(visibilityZone.GetComponent<BoxCollider2D>()));
                Assert.That(GetObjectReference(visibilityZone.GetComponent<CameraVisibilityZone>(), "_body"), Is.SameAs(visibilityZone.GetComponent<Rigidbody2D>()));

                AssertUiContracts(ui, camera);
                AssertSemanticBindings(ui, camera);
                AssertCleanRouteBindings(run, app, ui, poolRoot, safeObject);
                Assert.That(HashProtectedCamera(camera), Is.EqualTo(ExpectedCameraHash));
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, false);
            }
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects())
                if (gameObject.name == name)
                    return gameObject;

            Assert.Fail("Missing root: " + name);
            return null;
        }

        static int GetExecutionOrder<T>() where T : MonoBehaviour
        {
            DefaultExecutionOrder attribute = typeof(T).GetCustomAttribute<DefaultExecutionOrder>();
            Assert.That(attribute, Is.Not.Null, "Missing DefaultExecutionOrder on " + typeof(T).Name);
            return attribute.order;
        }

        static GameObject FindChild(GameObject parent, string name)
        {
            Transform child = parent.transform.Find(name);
            Assert.That(child, Is.Not.Null, "Missing child: " + parent.name + "/" + name);
            return child.gameObject;
        }

        static void AssertCleanRouteBindings(
            GameObject run,
            GameObject app,
            GameObject ui,
            GameObject poolRoot,
            GameObject safeObject)
        {
            RunBootstrap bootstrap = run.GetComponent<RunBootstrap>();
            GameplayRunUiController route = ui.GetComponent<GameplayRunUiController>();
            Assert.That(ui.GetComponents<GameplayRunUiController>(), Has.Length.EqualTo(1));

            Assert.That(GetObjectReference(bootstrap, "appBootstrap"), Is.SameAs(app.GetComponent<AppBootstrap>()));
            Assert.That(GetObjectReference(bootstrap, "gameScene"), Is.SameAs(run.GetComponent<GameScene>()));
            Assert.That(GetObjectReference(bootstrap, "poolRoot"), Is.SameAs(poolRoot.transform));
            Assert.That(GetObjectReference(bootstrap, "gameplayRunUiController"), Is.SameAs(route));
            Assert.That(GetObjectReference(bootstrap, "runPauseController"), Is.SameAs(run.GetComponent<RunPauseController>()));
            Assert.That(GetObjectReference(bootstrap, "safeKnockbackWorld"), Is.SameAs(safeObject.GetComponent<SafeKnockbackWorld>()));

            GameObject decisionLayer = FindPath(ui, "DecisionLayer");
            Assert.That(GetObjectReference(route, "_hudController"), Is.SameAs(GetComponentByName(FindPath(ui, "HUDLayer"), "GameplayHudController")));
            Assert.That(GetObjectReference(route, "_cardOfferController"), Is.SameAs(GetComponentByName(FindPath(decisionLayer, "CardOffer"), "GameplayCardOfferController")));
            Assert.That(GetObjectReference(route, "_pauseController"), Is.SameAs(GetComponentByName(FindPath(decisionLayer, "Pause"), "GameplayPauseController")));
            Assert.That(GetObjectReference(route, "_resultController"), Is.SameAs(GetComponentByName(FindPath(decisionLayer, "Result"), "GameplayRunResultPopupController")));
            Assert.That(GetObjectReference(route, "_feedbackController"), Is.SameAs(GetComponentByName(FindPath(ui, "FeedbackLayer"), "GameplayFeedbackController")));
            Assert.That(GetObjectReference(route, "_inputController"), Is.SameAs(GetComponentByName(FindPath(ui, "InputLayer"), "GameplayInputLayerController")));
        }

        static UnityEngine.Object GetObjectReference(UnityEngine.Object target, string field)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            return serializedObject.FindProperty(field).objectReferenceValue;
        }

        static int GetObjectReferenceArrayLength(UnityEngine.Object target, string field)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            return serializedObject.FindProperty(field).arraySize;
        }

        static string HashProtectedCamera(GameObject gameObject)
        {
            StringBuilder builder = new StringBuilder();
            Component[] components = gameObject.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null || (component is Camera) == false && (component is AudioListener) == false && component.GetType() != typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))
                    continue;

                AppendToken(builder, "COMPONENT");
                AppendToken(builder, index.ToString(CultureInfo.InvariantCulture));
                AppendToken(builder, component.GetType().FullName);
                AppendSerializedProperties(builder, component);
            }

            return Hash(builder.ToString());
        }

        static void AppendSerializedProperties(StringBuilder builder, Component component)
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.Next(enterChildren))
            {
                enterChildren = iterator.hasChildren;
                if (IsEphemeralOrStructuralProperty(iterator))
                    continue;

                AppendToken(builder, iterator.propertyPath);
                AppendToken(builder, iterator.propertyType.ToString());
                AppendSerializedValue(builder, iterator);
            }
        }

        static bool IsEphemeralOrStructuralProperty(SerializedProperty property)
        {
            string path = property.propertyPath;
            if (path == "m_GameObject" || path.StartsWith("m_GameObject.", StringComparison.Ordinal) ||
                path == "m_Father" || path.StartsWith("m_Father.", StringComparison.Ordinal) ||
                path == "m_Children" || path.StartsWith("m_Children.", StringComparison.Ordinal) ||
                path == "m_RootOrder" || path.StartsWith("m_RootOrder.", StringComparison.Ordinal) ||
                path == "m_CorrespondingSourceObject" || path.StartsWith("m_CorrespondingSourceObject.", StringComparison.Ordinal) ||
                path == "m_PrefabInstance" || path.StartsWith("m_PrefabInstance.", StringComparison.Ordinal) ||
                path == "m_PrefabAsset" || path.StartsWith("m_PrefabAsset.", StringComparison.Ordinal) ||
                path == "m_InstanceID" || path.StartsWith("m_InstanceID.", StringComparison.Ordinal) ||
                path == "m_CachedPtr" || path.StartsWith("m_CachedPtr.", StringComparison.Ordinal))
                return true;

            switch (property.name)
            {
                case "m_GameObject":
                case "m_Father":
                case "m_Children":
                case "m_RootOrder":
                case "m_CorrespondingSourceObject":
                case "m_PrefabInstance":
                case "m_PrefabAsset":
                case "m_InstanceID":
                case "m_CachedPtr":
                    return true;
                default:
                    return false;
            }
        }

        static void AppendSerializedValue(StringBuilder builder, SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    AppendToken(builder, property.longValue.ToString(CultureInfo.InvariantCulture));
                    break;
                case SerializedPropertyType.Boolean:
                    AppendToken(builder, property.boolValue.ToString(CultureInfo.InvariantCulture));
                    break;
                case SerializedPropertyType.Float:
                    AppendToken(builder, Float(property.doubleValue));
                    break;
                case SerializedPropertyType.String:
                    AppendToken(builder, property.stringValue ?? string.Empty);
                    break;
                case SerializedPropertyType.Color:
                    AppendColor(builder, property.colorValue);
                    break;
                case SerializedPropertyType.ObjectReference:
                    AppendToken(builder, "object-reference");
                    break;
                case SerializedPropertyType.Enum:
                    AppendToken(builder, property.enumValueIndex.ToString(CultureInfo.InvariantCulture));
                    break;
                case SerializedPropertyType.Vector2:
                    AppendVector2(builder, "value", property.vector2Value);
                    break;
                case SerializedPropertyType.Vector3:
                    AppendVector3(builder, "value", property.vector3Value);
                    break;
                case SerializedPropertyType.Vector4:
                    AppendVector4(builder, "value", property.vector4Value);
                    break;
                case SerializedPropertyType.Rect:
                    AppendRect(builder, property.rectValue);
                    break;
                case SerializedPropertyType.Vector2Int:
                    AppendToken(builder, property.vector2IntValue.x.ToString(CultureInfo.InvariantCulture));
                    AppendToken(builder, property.vector2IntValue.y.ToString(CultureInfo.InvariantCulture));
                    break;
                case SerializedPropertyType.Vector3Int:
                    AppendToken(builder, property.vector3IntValue.x.ToString(CultureInfo.InvariantCulture));
                    AppendToken(builder, property.vector3IntValue.y.ToString(CultureInfo.InvariantCulture));
                    AppendToken(builder, property.vector3IntValue.z.ToString(CultureInfo.InvariantCulture));
                    break;
                case SerializedPropertyType.ArraySize:
                    AppendToken(builder, property.intValue.ToString(CultureInfo.InvariantCulture));
                    break;
                case SerializedPropertyType.ManagedReference:
                    AppendToken(builder, property.managedReferenceFullTypename ?? string.Empty);
                    break;
                case SerializedPropertyType.Generic:
                    AppendToken(builder, property.isArray ? property.arraySize.ToString(CultureInfo.InvariantCulture) : "generic");
                    break;
            }
        }

        static void AssertSemanticBindings(GameObject ui, GameObject camera)
        {
            GameObject inputLayer = FindPath(ui, "InputLayer");
            GameObject hudLayer = FindPath(ui, "HUDLayer");
            GameObject hud = FindPath(hudLayer, "HUD");
            GameObject decisionLayer = FindPath(ui, "DecisionLayer");
            GameObject feedbackLayer = FindPath(ui, "FeedbackLayer");

            Component hudController = GetComponentByName(hudLayer, "GameplayHudController");
            Assert.That(GetObjectReference(hudController, "_hud"), Is.SameAs(hud.GetComponent<RectTransform>()));
            Assert.That(GetObjectReference(hudController, "_presentation"), Is.SameAs(GetComponentByName(hud, "GameplayHudPresentationController")));
            Assert.That(GetObjectReference(hudController, "_pauseEntry"), Is.SameAs(GetComponentByName(FindPath(hud, "Content/TopStatus/Content/PauseEntry"), "Button")));
            Assert.That(GetObjectReference(hudController, "_speedEntry"), Is.SameAs(GetComponentByName(FindPath(hud, "Content/TopStatus/Content/SpeedEntry"), "Button")));

            Component feedbackController = GetComponentByName(feedbackLayer, "GameplayFeedbackController");
            Assert.That(GetObjectReference(feedbackController, "_bossWarning"), Is.SameAs(GetComponentByName(FindPath(feedbackLayer, "BossWarning"), "GameplayBossWarningView")));
            Assert.That(GetObjectReference(feedbackController, "_threatDirection"), Is.SameAs(GetComponentByName(FindPath(feedbackLayer, "FloatingFeedback"), "GameplayThreatDirectionView")));
            Assert.That(GetObjectReference(feedbackController, "_viewport"), Is.SameAs(ui.GetComponent<RectTransform>()));
            Assert.That(GetObjectReference(feedbackController, "_worldCamera"), Is.SameAs(camera.GetComponent<Camera>()));

            Component inputController = GetComponentByName(inputLayer, "GameplayInputLayerController");
            Assert.That(GetObjectReference(inputController, "_joystick"), Is.SameAs(GetComponentByName(FindPath(inputLayer, "Joystick"), "GameplayFloatingJoystickController")));
        }

        static void AssertUiContracts(GameObject ui, GameObject camera)
        {
            RectTransform uiRectTransform = ui.GetComponent<RectTransform>();
            Assert.That(uiRectTransform, Is.Not.Null);
            Assert.That(uiRectTransform.rect.size, Is.EqualTo(new Vector2(1080.0f, 2340.0f)));
            Assert.That(uiRectTransform.sizeDelta, Is.EqualTo(new Vector2(1080.0f, 2340.0f)));
            Assert.That(ui.transform.localPosition, Is.EqualTo(new Vector3(540.0f, 1170.0f, 0.0f)));
            Assert.That(ui.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(ui.transform.localScale, Is.EqualTo(Vector3.one));
            AssertComponentOrder(
                ui,
                "RectTransform",
                "Canvas",
                "CanvasScaler",
                "GameplayRunUiController",
                "AudioSource",
                "AudioSource",
                "AudioSource",
                "AudioSource",
                "GameplayAudioPresentationBinder",
                "GameplayPresentationBinder",
                "AudioSource",
                "WorldFeedbackSceneBinder");

            string[] layerNames = { "InputLayer", "HUDLayer", "DecisionLayer", "FeedbackLayer" };
            Assert.That(ui.transform.childCount, Is.EqualTo(layerNames.Length));
            for (int index = 0; index < layerNames.Length; index++)
                Assert.That(ui.transform.GetChild(index).name, Is.EqualTo(layerNames[index]));

            GameObject inputLayer = FindPath(ui, "InputLayer");
            GameObject hudLayer = FindPath(ui, "HUDLayer");
            GameObject decisionLayer = FindPath(ui, "DecisionLayer");
            GameObject feedbackLayer = FindPath(ui, "FeedbackLayer");
            AssertComponentOrder(inputLayer, "RectTransform", "Canvas", "GraphicRaycaster", "GameplayInputLayerController", "SafeAreaLayout", "GameplayInputPresentationBinder");
            AssertComponentOrder(hudLayer, "RectTransform", "Canvas", "GraphicRaycaster", "GameplayHudController", "SafeAreaLayout", "GameplayHudPresentationBinder", "CanvasGroup", "Animation", "UiMotionPlayer");
            AssertComponentOrder(decisionLayer, "RectTransform", "Canvas", "GraphicRaycaster");
            AssertComponentOrder(feedbackLayer, "RectTransform", "Canvas", "GameplayFeedbackController", "GameplayNotificationPresentationBinder");

            Assert.That(CountComponentByName(FindPath(hudLayer, "HUD"), "GameplayHudPresentationController"), Is.EqualTo(1));
            Assert.That(CountComponentByName(FindPath(decisionLayer, "CardOffer"), "GameplayCardOfferController"), Is.EqualTo(1));
            Assert.That(CountComponentByName(FindPath(decisionLayer, "Pause"), "GameplayPauseController"), Is.EqualTo(1));
            Assert.That(CountComponentByName(FindPath(decisionLayer, "Result"), "GameplayRunResultPopupController"), Is.EqualTo(1));
            Assert.That(CountComponentByName(FindPath(feedbackLayer, "BossWarning"), "GameplayBossWarningView"), Is.EqualTo(1));
            Assert.That(CountComponentByName(FindPath(feedbackLayer, "FloatingFeedback"), "GameplayThreatDirectionView"), Is.EqualTo(1));
            Assert.That(CountComponentByName(FindPath(inputLayer, "Joystick"), "GameplayFloatingJoystickController"), Is.EqualTo(1));
            AssertNoMissingComponents(ui.transform);
        }

        static void AssertComponentOrder(GameObject gameObject, params string[] expectedTypes)
        {
            Component[] components = gameObject.GetComponents<Component>();
            Assert.That(components, Has.Length.EqualTo(expectedTypes.Length), "Unexpected component topology on " + GetHierarchyPath(gameObject.transform));
            for (int index = 0; index < expectedTypes.Length; index++)
            {
                Assert.That(components[index], Is.Not.Null, "Missing component at " + GetHierarchyPath(gameObject.transform));
                Assert.That(components[index].GetType().Name, Is.EqualTo(expectedTypes[index]));
            }
        }

        static void AssertNoMissingComponents(Transform root)
        {
            foreach (Component component in root.GetComponents<Component>())
                Assert.That(component, Is.Not.Null, "Missing component at " + GetHierarchyPath(root));

            foreach (Transform child in root)
                AssertNoMissingComponents(child);
        }

        static GameObject FindPath(GameObject parent, string path)
        {
            Transform child = parent.transform.Find(path);
            Assert.That(child, Is.Not.Null, "Missing semantic path: " + parent.name + "/" + path);
            return child.gameObject;
        }

        static Component GetComponentByName(GameObject gameObject, string typeName)
        {
            foreach (Component component in gameObject.GetComponents<Component>())
                if (component != null && component.GetType().Name == typeName)
                    return component;

            Assert.Fail("Missing component " + typeName + " on " + GetHierarchyPath(gameObject.transform));
            return null;
        }

        static int CountComponentByName(GameObject gameObject, string typeName)
        {
            int count = 0;
            foreach (Component component in gameObject.GetComponents<Component>())
                if (component != null && component.GetType().Name == typeName)
                    count++;

            return count;
        }

        static string GetHierarchyPath(Transform transform)
        {
            List<string> segments = new List<string>();
            while (transform != null)
            {
                segments.Add(transform.name + "[" + transform.GetSiblingIndex().ToString(CultureInfo.InvariantCulture) + "]");
                transform = transform.parent;
            }

            segments.Reverse();
            return "/" + string.Join("/", segments);
        }

        static void AppendToken(StringBuilder builder, string value)
        {
            value ??= string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('|');
        }

        static void AppendVector2(StringBuilder builder, string name, Vector2 value)
        {
            AppendToken(builder, name);
            AppendToken(builder, Float(value.x));
            AppendToken(builder, Float(value.y));
        }

        static void AppendVector3(StringBuilder builder, string name, Vector3 value)
        {
            AppendToken(builder, name);
            AppendToken(builder, Float(value.x));
            AppendToken(builder, Float(value.y));
            AppendToken(builder, Float(value.z));
        }

        static void AppendVector4(StringBuilder builder, string name, Vector4 value)
        {
            AppendToken(builder, name);
            AppendToken(builder, Float(value.x));
            AppendToken(builder, Float(value.y));
            AppendToken(builder, Float(value.z));
            AppendToken(builder, Float(value.w));
        }

        static void AppendColor(StringBuilder builder, Color value)
        {
            AppendToken(builder, Float(value.r));
            AppendToken(builder, Float(value.g));
            AppendToken(builder, Float(value.b));
            AppendToken(builder, Float(value.a));
        }

        static void AppendRect(StringBuilder builder, Rect value)
        {
            AppendToken(builder, Float(value.x));
            AppendToken(builder, Float(value.y));
            AppendToken(builder, Float(value.width));
            AppendToken(builder, Float(value.height));
        }

        static string Float(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        static string Float(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        static string Hash(string value)
        {
            using (SHA256 sha256 = SHA256.Create())
                return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }
    }
}
