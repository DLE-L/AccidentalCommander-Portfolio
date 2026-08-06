using Lizzo.PV.Flow;
using Lizzo.PV.Lobby;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class CommanderWeaponSelectionTests
    {
        [TestCase(CommanderWeaponId.RapidCrossbow, "rapid_crossbow")]
        [TestCase(CommanderWeaponId.PiercingSpear, "piercing_spear")]
        [TestCase(CommanderWeaponId.BlastStaff, "blast_staff")]
        public void SelectableWeaponsExposeTheApprovedIdentity(CommanderWeaponId weapon, string expectedId)
        {
            Assert.That(CommanderWeaponCatalog.IsSelectable(weapon), Is.True);
            Assert.That(CommanderWeaponCatalog.ToId(weapon), Is.EqualTo(expectedId));
        }

        [Test]
        public void UnselectedContextDoesNotClaimACommanderWeapon()
        {
            Assert.That(RunContext.Normal.HasCommanderWeapon, Is.False);
            Assert.That(RunContext.Tutorial.HasCommanderWeapon, Is.False);
        }

        [TestCase(RunMode.Normal, CommanderWeaponId.RapidCrossbow)]
        [TestCase(RunMode.Normal, CommanderWeaponId.PiercingSpear)]
        [TestCase(RunMode.Normal, CommanderWeaponId.BlastStaff)]
        [TestCase(RunMode.Tutorial, CommanderWeaponId.RapidCrossbow)]
        [TestCase(RunMode.Tutorial, CommanderWeaponId.PiercingSpear)]
        [TestCase(RunMode.Tutorial, CommanderWeaponId.BlastStaff)]
        public void LaunchAndRetryPreserveTheSelectedWeapon(RunMode mode, CommanderWeaponId weapon)
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(new RunContext(mode, weapon));

            Assert.That(state.ConsumeForLaunch(), Is.EqualTo(new RunContext(mode, weapon)));
            state.PrepareRetry();
            Assert.That(state.ConsumeForLaunch(), Is.EqualTo(new RunContext(mode, weapon)));
        }

        [Test]
        public void NewLobbyLaunchCanReplaceThePreviousWeapon()
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(new RunContext(RunMode.Normal, CommanderWeaponId.RapidCrossbow));
            Assert.That(state.ConsumeForLaunch().CommanderWeapon, Is.EqualTo(CommanderWeaponId.RapidCrossbow));

            state.Prepare(new RunContext(RunMode.Normal, CommanderWeaponId.BlastStaff));
            Assert.That(state.ConsumeForLaunch().CommanderWeapon, Is.EqualTo(CommanderWeaponId.BlastStaff));
        }

        [Test]
        public void LobbyDepartureRequiresAWeaponBeforeSortieAndCanReplaceIt()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene lobby = EditorSceneManager.OpenScene(GameFlowRoutes.LobbyScenePath, OpenSceneMode.Single);
                Transform departure = Find(lobby, "@HomeLobby/SafeArea/Lobby/Screens/Departure");
                CommanderWeaponSelectionView view = departure.GetComponent<CommanderWeaponSelectionView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(view.Configure(), Is.True);

                Transform selection = departure.Find("CommanderWeaponSelection");
                Assert.That(selection, Is.Not.Null);
                Assert.That(selection.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(4));
                AssertButton(selection, "연발 쇠뇌Button", "연발 쇠뇌");
                AssertButton(selection, "관통창Button", "관통창");
                AssertButton(selection, "폭렬 지팡이Button", "폭렬 지팡이");
                AssertButton(selection, "이 무기로 출정Button", "이 무기로 출정");

                Button rapid = selection.Find("연발 쇠뇌Button").GetComponent<Button>();
                Button staff = selection.Find("폭렬 지팡이Button").GetComponent<Button>();
                Button sortie = selection.Find("이 무기로 출정Button").GetComponent<Button>();
                Assert.That(sortie.interactable, Is.False);

                rapid.onClick.Invoke();
                Assert.That(view.SelectedWeapon, Is.EqualTo(CommanderWeaponId.RapidCrossbow));
                Assert.That(sortie.interactable, Is.True);

                staff.onClick.Invoke();
                Assert.That(view.SelectedWeapon, Is.EqualTo(CommanderWeaponId.BlastStaff));
            }
            finally
            {
                if (originalSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        static void AssertButton(Transform selection, string name, string label)
        {
            Transform root = selection.Find(name);
            Assert.That(root, Is.Not.Null, name);
            Assert.That(root.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(1), name);
            Assert.That(root.Find("Visual"), Is.Not.Null, name);
            TMP_Text text = root.Find("Content/Label").GetComponent<TMP_Text>();
            Assert.That(text.text, Is.EqualTo(label), name);
            Assert.That(root.Find("Visual").GetComponent<Graphic>().raycastTarget, Is.True, name);
            Assert.That(text.raycastTarget, Is.False, name);
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
