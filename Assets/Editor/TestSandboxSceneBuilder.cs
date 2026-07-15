#if UNITY_EDITOR
using Lizzo.PV.P0.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools
{
    public static class TestSandboxSceneBuilder
    {
        private const string SourceScenePath = "Assets/Scenes/Gameplay.unity";
        private const string TestScenePath = "Assets/Scenes/TestSandbox.unity";
        private const string PanelObjectName = "@TestSandboxPanel";

        [MenuItem("Lizzo PV/Tools/Create Test Sandbox Scene")]
        public static void CreateOrUpdate()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                Debug.LogError("Save the current scene before creating the test sandbox scene.");
                return;
            }

            SceneAsset sourceScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath);
            if (sourceScene == null)
            {
                Debug.LogError($"Source scene not found: {SourceScenePath}");
                return;
            }

            SceneAsset testScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath);
            if (testScene == null && AssetDatabase.CopyAsset(SourceScenePath, TestScenePath) == false)
            {
                Debug.LogError($"Failed to create test sandbox scene: {TestScenePath}");
                return;
            }

            AssetDatabase.ImportAsset(TestScenePath);
            Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            EnsureTestPanel();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Test sandbox scene ready: {TestScenePath}");
        }

        private static void EnsureTestPanel()
        {
            GameObject panelObject = GameObject.Find(PanelObjectName);
            if (panelObject == null)
                panelObject = new GameObject(PanelObjectName);

            if (panelObject.GetComponent<P0TestSandboxCanvasPanel>() == null)
                panelObject.AddComponent<P0TestSandboxCanvasPanel>();
        }
    }
}
#endif
