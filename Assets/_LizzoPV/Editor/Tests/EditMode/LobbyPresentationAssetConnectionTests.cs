using System.Linq;
using Lizzo.PV.Lobby;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class LobbyPresentationAssetConnectionTests
    {
        const string ScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";
        const string BundleRoot = "Assets/_LizzoPV/App/Presentation/Data/Bundles";
        const string ProfileRoot = "Assets/_LizzoPV/Lobby/Presentation/Data/Profiles";

        [Test]
        public void LobbyProfiles_ResolveRequiredAssetsFromSharedAndLobbyBundles()
        {
            LobbyPresentationSetSO set = Load<LobbyPresentationSetSO>(ProfileRoot + "/LobbyPresentationSet.asset");
            Assert.That(set.TryValidate(out string issue), Is.True, issue);

            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(set.SharedCatalogBundle, out AssetCatalogBundleLease sharedLease, out string sharedIssue), Is.True, sharedIssue);
            Assert.That(runtime.Acquire(set.LobbyCatalogBundle, out AssetCatalogBundleLease lobbyLease, out string lobbyIssue), Is.True, lobbyIssue);
            try
            {
                LobbyShellPresentationProfileSO shell = set.LobbyShellProfile;
                DepartureScreenPresentationSO departure = set.DepartureScreenProfile;
                AssertAudio(runtime, shell.LobbyBgmId);
                AssertAudio(runtime, shell.LockedFeatureSfxId);
                AssertAudio(runtime, shell.TabSelectedSfxId);
                AssertSprite(runtime, shell.LockedToastSpriteId);
                AssertSprite(runtime, shell.LockedBadgeSpriteId);
                AssertMotion(runtime, shell.ToastEnterMotionId);
                AssertMotion(runtime, shell.ToastExitMotionId);
                foreach (LobbyTabPresentationBinding tab in shell.TabBindings)
                {
                    AssertSprite(runtime, tab.IconSpriteId);
                    AssertMotion(runtime, tab.SelectedMotionId);
                }

                AssertSprite(runtime, departure.LobbyBackgroundSpriteId);
                AssertMotion(runtime, departure.CommanderDisplayEnterMotionId);
                AssertAudio(runtime, departure.DepartureAcceptedSfxId);
                AssertAudio(runtime, departure.DepartureFailedSfxId);
                AssertMotion(runtime, departure.DepartureAcceptedMotionId);
                AssertMotion(runtime, departure.DepartureFailedMotionId);
                AssertMotion(runtime, departure.LoadingIndicatorMotionId);
                Assert.That(departure.TryGetCommanderVisual("commander_01", out CommanderVisualBinding commander), Is.True);
                AssertSprite(runtime, commander.PortraitSpriteId);
            }
            finally
            {
                lobbyLease.Dispose();
                sharedLease.Dispose();
            }
        }

        [Test]
        public void LobbyScene_HasCompleteProductionBindingsAndNoMissingScripts()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                LobbyPresentationBinder binder = roots
                    .SelectMany(root => root.GetComponentsInChildren<LobbyPresentationBinder>(true))
                    .Single();
                LobbyRootController rootController = roots
                    .SelectMany(root => root.GetComponentsInChildren<LobbyRootController>(true))
                    .Single();
                SceneAssetCatalogScope scope = roots
                    .SelectMany(root => root.GetComponentsInChildren<SceneAssetCatalogScope>(true))
                    .Single();

                string[] requiredBinderReferences =
                {
                    "_presentationSet", "_navigation", "_overlays", "_departure", "_lobbyBackground",
                    "_commanderPortrait", "_lockedToastRoot", "_lockedToastBackground", "_lockedToastLabel",
                    "_toastMotion", "_departureMotion", "_bgmSource", "_sfxSource",
                };
                SerializedObject binderSerialized = new SerializedObject(binder);
                foreach (string field in requiredBinderReferences)
                    Assert.That(binderSerialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);

                Assert.That(
                    new SerializedObject(rootController).FindProperty("_presentationBinder").objectReferenceValue,
                    Is.SameAs(binder));
                Assert.That(scope.Bundles.Select(bundle => bundle.BundleKey), Is.EquivalentTo(new[] { "shared", "lobby" }));

                LobbyNavigationItemView[] items = roots
                    .SelectMany(root => root.GetComponentsInChildren<LobbyNavigationItemView>(true))
                    .ToArray();
                Assert.That(items, Has.Length.EqualTo(5));
                foreach (LobbyNavigationItemView item in items)
                    Assert.That(new SerializedObject(item).FindProperty("_motionPlayer").objectReferenceValue, Is.Not.Null, item.name);

                RectTransform toast = binderSerialized.FindProperty("_lockedToastRoot").objectReferenceValue as RectTransform;
                Assert.That(toast, Is.Not.Null);
                Assert.That(toast.Find("Visual"), Is.Not.Null);
                Assert.That(toast.Find("Content/Label"), Is.Not.Null);
                Image commanderPortrait = binderSerialized.FindProperty("_commanderPortrait").objectReferenceValue as Image;
                Assert.That(commanderPortrait, Is.Not.Null);
                Assert.That(commanderPortrait.sprite, Is.Not.Null);

                foreach (GameObject root in roots)
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root), Is.Zero, root.name);
                Assert.That(scene.isDirty, Is.False);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void SharedAudioCatalog_UsesFileDescriptiveIdentityForReusableReplaceableLoop()
        {
            AudioCatalogSO catalog = Load<AudioCatalogSO>(
                "Assets/_LizzoPV/App/Presentation/Data/Catalogs/SharedAudioCatalog.asset");
            AudioCatalogEntry entry = catalog.Entries.Single(value => value.Id.Value == 163);
            Assert.That(entry.Id.Value, Is.EqualTo(163));
            Assert.That(entry.Name, Is.EqualTo("Fire"));
            Assert.That(entry.Description, Does.Contain("Fire.ogg"));
            Assert.That(entry.Kind, Is.EqualTo(AudioKind.Bgm));
            Assert.That(entry.Loop, Is.True);
            Assert.That(entry.Asset, Is.Not.Null);
        }

        static void AssertSprite(AssetCatalogBundleRuntime runtime, SpriteAssetId id) =>
            Assert.That(runtime.SpriteCatalog.TryGet(id, out _), Is.True, $"Missing Sprite Asset ID {id.Value}.");

        static void AssertAudio(AssetCatalogBundleRuntime runtime, AudioAssetId id) =>
            Assert.That(runtime.AudioCatalog.TryGet(id, out _), Is.True, $"Missing Audio Asset ID {id.Value}.");

        static void AssertMotion(AssetCatalogBundleRuntime runtime, MotionAssetId id) =>
            Assert.That(runtime.MotionCatalog.TryGet(id, out _), Is.True, $"Missing Motion Asset ID {id.Value}.");

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
    }
}
