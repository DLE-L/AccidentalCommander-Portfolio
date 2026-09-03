using Lizzo.PV.Flow;
using Lizzo.PV.Lobby;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class WeaponlessRunLaunchTests
    {
        [Test]
        public void UnselectedContextDoesNotClaimACommanderWeapon()
        {
            Assert.That(RunContext.Normal.HasCommanderWeapon, Is.False);
            Assert.That(RunContext.Tutorial.HasCommanderWeapon, Is.False);
        }

        [TestCase(RunMode.Normal)]
        [TestCase(RunMode.Tutorial)]
        public void LaunchAndRetryPreserveOnlyTheRunMode(RunMode mode)
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(new RunContext(mode));

            Assert.That(state.ConsumeForLaunch(), Is.EqualTo(new RunContext(mode)));
            state.PrepareRetry();
            Assert.That(state.ConsumeForLaunch(), Is.EqualTo(new RunContext(mode)));
        }

        [Test]
        public void NewLobbyLaunchCanReplaceThePreviousRunMode()
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(RunContext.Tutorial);
            Assert.That(state.ConsumeForLaunch(), Is.EqualTo(RunContext.Tutorial));

            state.Prepare(RunContext.Normal);
            Assert.That(state.ConsumeForLaunch(), Is.EqualTo(RunContext.Normal));
        }

        [Test]
        public void LobbyDepartureContainsOnlyTheWeaponlessDepartureAction()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene lobby = EditorSceneManager.OpenScene(GameFlowRoutes.LobbyScenePath, OpenSceneMode.Single);
                Transform departure = Find(lobby, "@HomeLobby/SafeArea/Lobby/Screens/Departure");
                Assert.That(departure, Is.Not.Null);
                Assert.That(departure.Find("Content/CommanderWeaponSelection"), Is.Null);

                LobbyDepartureController controller = departure.GetComponent<LobbyDepartureController>();
                Assert.That(controller, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(controller);
                Button button = serialized.FindProperty("_departureButton").objectReferenceValue as Button;
                Assert.That(button, Is.Not.Null);
                Assert.That(button.gameObject.name, Is.EqualTo("DepartureButton"));
                Assert.That(button.transform.parent, Is.EqualTo(departure.Find("Content")));

                foreach (MonoBehaviour component in departure.GetComponents<MonoBehaviour>())
                    Assert.That(component.GetType().Name, Is.Not.EqualTo("CommanderWeaponSelectionView"));

                Transform overlays = Find(lobby, "@HomeLobby/SafeArea/Lobby/Overlays");
                Assert.That(overlays.Find("ConfirmDeparture"), Is.Null);
            }
            finally
            {
                if (originalSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        static Transform Find(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            Transform current = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == parts[0])
                {
                    current = root.transform;
                    break;
                }
            }

            for (int i = 1; i < parts.Length && current != null; i++)
                current = current.Find(parts[i]);

            return current;
        }
    }
}
