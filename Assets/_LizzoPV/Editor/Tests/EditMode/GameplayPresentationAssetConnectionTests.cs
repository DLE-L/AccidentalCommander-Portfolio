using System.Collections.Generic;
using System.Linq;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Pause;
using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Gameplay.Result;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayPresentationAssetConnectionTests
    {
        const string ScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        const string ProfileRoot = "Assets/_LizzoPV/Gameplay/UI/Presentation/Data/Profiles";

        [Test]
        public void GameplayProfiles_ResolveEveryProductionAssetFromSharedAndGameplayBundles()
        {
            GameplayPresentationSetSO set = Load<GameplayPresentationSetSO>(ProfileRoot + "/GameplayPresentationSet.asset");
            Assert.That(set.TryValidate(out string issue), Is.True, issue);
            Assert.That(MonoScript.FromScriptableObject(set), Is.Not.Null);
            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(set.SharedCatalogBundle, out AssetCatalogBundleLease shared, out string sharedIssue), Is.True, sharedIssue);
            Assert.That(runtime.Acquire(set.GameplayCatalogBundle, out AssetCatalogBundleLease gameplay, out string gameplayIssue), Is.True, gameplayIssue);
            try
            {
                AssertAudio(runtime, set.AudioProfile.GameplayBgmId); AssertAudio(runtime, set.AudioProfile.BossBgmId);
                GameplayHudPresentationProfileSO hud = set.HudProfile;
                AssertSprites(runtime, hud.KillIconSpriteId, hud.TimerFrameSpriteId, hud.ExperienceTrackSpriteId,
                    hud.ExperienceFillSpriteId, hud.BossHealthTrackSpriteId, hud.BossHealthFillSpriteId,
                    hud.PauseIconSpriteId, hud.SpeedIconSpriteId);
                AssertAudio(runtime, hud.SpeedChangedSfxId);
                AssertMotions(runtime, hud.SpeedChangedMotionId, hud.ExperienceToBossMotionId, hud.BossToExperienceMotionId);
                GameplayInputPresentationProfileSO input = set.InputProfile;
                AssertSprites(runtime, input.JoystickBackgroundSpriteId, input.JoystickCenterSpriteId, input.JoystickHandleSpriteId);
                AssertMotions(runtime, input.AppearMotionId, input.ResetMotionId);
                CardOfferPresentationProfileSO cards = set.CardOfferProfile;
                AssertSprites(runtime, cards.StatusBadgeSpriteId, cards.ProgressEmptySpriteId, cards.ProgressFilledSpriteId);
                AssertAudios(runtime, cards.CardOfferOpenSfxId, cards.CardAcceptedSfxId, cards.CardOfferCloseSfxId);
                AssertMotions(runtime, cards.OverlayEnterMotionId, cards.OverlayExitMotionId, cards.CardAcceptedMotionId, cards.CardDisabledMotionId);
                PausePresentationProfileSO pause = set.PauseProfile;
                AssertSprite(runtime, pause.PanelSpriteId);
                AssertAudios(runtime, pause.PauseEnterSfxId, pause.ResumeAcceptedSfxId, pause.AbandonAcceptedSfxId);
                AssertMotions(runtime, pause.OverlayEnterMotionId, pause.OverlayExitMotionId, pause.ResumeAcceptedMotionId,
                    pause.AbandonAcceptedMotionId, pause.SummaryItemEnterMotionId);
                AssertNotifications(runtime, set.NotificationProfile);
                AssertResults(runtime, set.RunResultSet);
                AssertContentSprites(runtime, set.ContentSpriteProfile);
            }
            finally { gameplay.Dispose(); shared.Dispose(); }
        }

        [Test]
        public void CommanderAndEnemyWorldPrefabs_HaveResolvableSpriteAssetBindings()
        {
            GameplayPresentationSetSO set = Load<GameplayPresentationSetSO>(ProfileRoot + "/GameplayPresentationSet.asset");
            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(set.SharedCatalogBundle, out AssetCatalogBundleLease shared, out string sharedIssue), Is.True, sharedIssue);
            Assert.That(runtime.Acquire(set.GameplayCatalogBundle, out AssetCatalogBundleLease gameplay, out string gameplayIssue), Is.True, gameplayIssue);
            try
            {
                string[] prefabRoots =
                {
                    "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units",
                    "Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units",
                };
                string[] paths = AssetDatabase.FindAssets("t:Prefab", prefabRoots)
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => path)
                    .ToArray();
                Assert.That(paths, Has.Length.EqualTo(6));

                foreach (string path in paths)
                {
                    GameObject prefab = Load<GameObject>(path);
                    WorldSpriteAssetBinder[] binders = prefab.GetComponentsInChildren<WorldSpriteAssetBinder>(true);
                    Assert.That(binders, Has.Length.EqualTo(1), path);
                    WorldSpriteAssetBinder binder = binders[0];
                    Assert.That(binder.GameDataId, Is.Not.Null.And.Not.Empty, path);
                    Assert.That(binder.SpriteId.IsNone, Is.False, path);
                    AssertSprite(runtime, binder.SpriteId);
                    Assert.That(new SerializedObject(binder).FindProperty("_target").objectReferenceValue, Is.Not.Null, path);
                }
            }
            finally { gameplay.Dispose(); shared.Dispose(); }
        }

        [Test]
        public void GameplayScene_HasModularProductionBindersCatalogScopeAndNoMissingScripts()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                GameplayPresentationBinder coordinator = roots.SelectMany(x => x.GetComponentsInChildren<GameplayPresentationBinder>(true)).Single();
                Assert.That(new SerializedObject(coordinator).FindProperty("_presentationSet").objectReferenceValue, Is.Not.Null);
                Assert.That(roots.SelectMany(x => x.GetComponentsInChildren<GameplayAudioPresentationBinder>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(x => x.GetComponentsInChildren<GameplayHudPresentationBinder>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(x => x.GetComponentsInChildren<GameplayInputPresentationBinder>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(x => x.GetComponentsInChildren<GameplayCardOfferPresentationBinder>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(x => x.GetComponentsInChildren<GameplayPausePresentationBinder>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(x => x.GetComponentsInChildren<GameplayNotificationPresentationBinder>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(x => x.GetComponentsInChildren<GameplayRunResultPresentationBinder>(true)).Count(), Is.EqualTo(1));
                SceneAssetCatalogScope scope = roots.SelectMany(x => x.GetComponentsInChildren<SceneAssetCatalogScope>(true)).Single();
                Assert.That(scope.Bundles.Select(x => x.BundleKey), Is.EquivalentTo(new[] { "shared", "gameplay" }));
                GameplayCardOfferItemView[] cards = roots.SelectMany(x => x.GetComponentsInChildren<GameplayCardOfferItemView>(true)).ToArray();
                Assert.That(cards, Has.Length.EqualTo(3));
                foreach (GameplayCardOfferItemView card in cards)
                    Assert.That(new SerializedObject(card).FindProperty("_relationIcon").objectReferenceValue, Is.Not.Null, card.name);
                GameplayRunResultPopupView result = roots.SelectMany(x => x.GetComponentsInChildren<GameplayRunResultPopupView>(true)).Single();
                SerializedProperty rewards = new SerializedObject(result).FindProperty("_rewardItems");
                Assert.That(rewards.arraySize, Is.EqualTo(2));
                for (int index = 0; index < rewards.arraySize; index++)
                    Assert.That(rewards.GetArrayElementAtIndex(index).FindPropertyRelative("_icon").objectReferenceValue, Is.Not.Null, $"Reward {index}");
                GameplayPauseSynergyItemView pauseSynergy = Load<GameObject>("Assets/_LizzoPV/Gameplay/UI/Prefabs/Pause/GameplayPauseSynergyItem.prefab")
                    .GetComponent<GameplayPauseSynergyItemView>();
                Assert.That(new SerializedObject(pauseSynergy).FindProperty("_icon").objectReferenceValue, Is.Not.Null);
                foreach (GameObject root in roots)
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root), Is.Zero, root.name);
                Assert.That(scene.isDirty, Is.False);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void TemporaryLoopAsset_IsSharedOnceAndNotDuplicatedAcrossCatalogs()
        {
            AudioCatalogSO[] catalogs = AssetDatabase.FindAssets("t:AudioCatalogSO")
                .Select(guid => AssetDatabase.LoadAssetAtPath<AudioCatalogSO>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            AudioCatalogEntry[] matches = catalogs.SelectMany(x => x.Entries).Where(x => x.Id.Value == 163).ToArray();
            Assert.That(matches, Has.Length.EqualTo(1));
            Assert.That(matches[0].Name, Is.EqualTo("Fire"));
            Assert.That(matches[0].Description, Does.Contain("Fire.ogg"));
            Assert.That(matches[0].Loop, Is.True);
            Assert.That(matches[0].Asset, Is.Not.Null);
        }

        static void AssertNotifications(AssetCatalogBundleRuntime runtime, GameplayNotificationPresentationProfileSO profile)
        {
            CombatPhasePresentation phase = profile.CombatPhasePresentation;
            if (!phase.FrameSpriteId.IsNone) AssertSprite(runtime, phase.FrameSpriteId);
            if (!phase.AlertSfxId.IsNone) AssertAudio(runtime, phase.AlertSfxId);
            AssertMotion(runtime, phase.EnterMotionId); AssertMotion(runtime, phase.ExitMotionId);
            EliteAlertPresentation elite = profile.EliteAlertPresentation;
            AssertSprite(runtime, elite.FrameSpriteId); AssertAudio(runtime, elite.AlertSfxId);
            AssertMotions(runtime, elite.EnterMotionId, elite.PulseMotionId, elite.ExitMotionId);
            BossWarningPresentation boss = profile.BossWarningPresentation;
            AssertSprites(runtime, boss.FrameSpriteId, boss.EdgeAccentSpriteId); AssertAudio(runtime, boss.WarningSfxId);
            AssertMotions(runtime, boss.EnterMotionId, boss.PulseMotionId, boss.ExitMotionId);
        }

        static void AssertResults(AssetCatalogBundleRuntime runtime, RunResultPresentationSetSO set)
        {
            RunResultSharedPresentationProfileSO shared = set.SharedProfile;
            AssertAudios(runtime, shared.RewardRevealSfxId, shared.MainAcceptedSfxId);
            AssertMotions(runtime, shared.RewardRevealMotionId, shared.MainAcceptedMotionId, shared.PopupExitMotionId);
            foreach (RunResultPresentationProfileSO profile in new[] { set.VictoryProfile, set.FailureProfile, set.AbandonedProfile })
            { AssertSprites(runtime, profile.GlowSpriteId, profile.EmblemSpriteId, profile.BannerSpriteId); AssertAudio(runtime, profile.OutcomeStingerId); AssertMotion(runtime, profile.PopupEnterMotionId); }
        }

        static void AssertContentSprites(AssetCatalogBundleRuntime runtime, GameplayContentSpriteProfileSO profile)
        {
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.TryValidate(out string issue), Is.True, issue);
            SerializedObject serialized = new SerializedObject(profile);
            foreach (string field in new[]
                     {
                         "_cardPortraits", "_cardSynergyIcons", "_notificationSynergyIcons",
                         "_buildSummaryCompanionIcons", "_buildSummaryPassiveIcons",
                         "_buildSummarySynergyIcons", "_rewardIcons",
                     })
            {
                SerializedProperty bindings = serialized.FindProperty(field);
                Assert.That(bindings, Is.Not.Null, field);
                bool optionalSynergyBinding = field == "_cardSynergyIcons"
                    || field == "_notificationSynergyIcons"
                    || field == "_buildSummarySynergyIcons";
                if (optionalSynergyBinding == false)
                    Assert.That(bindings.arraySize, Is.GreaterThan(0), field);
                for (int index = 0; index < bindings.arraySize; index++)
                {
                    SerializedProperty binding = bindings.GetArrayElementAtIndex(index);
                    int id = binding.FindPropertyRelative("_spriteId").FindPropertyRelative("_value").intValue;
                    AssertSprite(runtime, new SpriteAssetId(id));
                }
            }
        }
        static void AssertSprites(AssetCatalogBundleRuntime r, params SpriteAssetId[] ids) { foreach (SpriteAssetId id in ids) AssertSprite(r, id); }
        static void AssertAudios(AssetCatalogBundleRuntime r, params AudioAssetId[] ids) { foreach (AudioAssetId id in ids) AssertAudio(r, id); }
        static void AssertMotions(AssetCatalogBundleRuntime r, params MotionAssetId[] ids) { foreach (MotionAssetId id in ids) AssertMotion(r, id); }
        static void AssertSprite(AssetCatalogBundleRuntime r, SpriteAssetId id) => Assert.That(r.SpriteCatalog.TryGet(id, out _), Is.True, $"Missing Sprite {id.Value}");
        static void AssertAudio(AssetCatalogBundleRuntime r, AudioAssetId id) => Assert.That(r.AudioCatalog.TryGet(id, out _), Is.True, $"Missing Audio {id.Value}");
        static void AssertMotion(AssetCatalogBundleRuntime r, MotionAssetId id) => Assert.That(r.MotionCatalog.TryGet(id, out _), Is.True, $"Missing Motion {id.Value}");
        static T Load<T>(string path) where T : Object { T value = AssetDatabase.LoadAssetAtPath<T>(path); Assert.That(value, Is.Not.Null, path); return value; }
    }
}
