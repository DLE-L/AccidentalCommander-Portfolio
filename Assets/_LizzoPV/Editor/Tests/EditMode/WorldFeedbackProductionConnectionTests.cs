using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Legion;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTests
{
    public sealed class WorldFeedbackProductionConnectionTests
    {
        private const string ScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private const string WorldProfilePath = "Assets/_LizzoPV/Gameplay/Presentation/Data/Profiles/WorldFeedback/WorldFeedbackProfileSet.asset";
        private const string GameplayPresentationPath = "Assets/_LizzoPV/Gameplay/UI/Presentation/Data/Profiles/GameplayPresentationSet.asset";

        private static readonly string[] CompanionIds =
        {
            "shield_guard", "sword_soldier", "cleric", "falcon_archer",
            "fire_mage", "wolf_tamer", "skeleton_scythe_thrower",
        };

        private static readonly string[] EnemyIds =
        {
            "small_goblin", "hungry_wolf", "shield_orc", "red_charger", "boss_hungry_giant",
        };

        private static readonly string[] EnemyAttackIds =
        {
            "contact_attack", "red_charger_dash", "red_charger_impact_grace",
            "boss_slow_charge", "boss_aoe_slam",
        };

        [Test]
        public void WorldFeedbackProfileSet_HasRequiredProductionCoverageAndValidScripts()
        {
            WorldFeedbackProfileSetSO set = Load<WorldFeedbackProfileSetSO>(WorldProfilePath);
            Assert.That(set.TryValidate(out string issue), Is.True, issue);
            Assert.That(MonoScript.FromScriptableObject(set), Is.Not.Null);
            Assert.That(set.CompanionLifecycleBindings.Select(x => x.CompanionId.Value),
                Is.EquivalentTo(CompanionIds));
            Assert.That(set.CombatImpactBindings.Select(x => x.ImpactKind.Value),
                Is.EquivalentTo(new[] { "enemy.hit.normal", "commander.hit.normal" }));
            Assert.That(set.StatusBindings.Select(x => x.StatusId.Value),
                Is.EquivalentTo(new[] { "vulnerable", "shock", "weakening", "curse" }));
            Assert.That(set.AttackBindings.Select(x => x.AttackId.Value),
                Is.EquivalentTo(CompanionIds.SelectMany(id => new[]
                {
                    $"{id}.basic", $"{id}.skill", $"{id}.returning_light",
                })));
            Assert.That(set.EnemyAttackBindings.Select(x => x.EnemyAttackId.Value),
                Is.EquivalentTo(EnemyAttackIds));
            Assert.That(set.EnemySpawnBindings.Select(x => x.EnemyId.Value),
                Is.EquivalentTo(EnemyIds));
            Assert.That(set.EnemyDeathBindings.Select(x => x.EnemyId.Value),
                Is.EquivalentTo(EnemyIds));
            Assert.That(set.ExperienceOrbBindings.Select(x => x.OrbVisualTier),
                Is.EquivalentTo(new[] { OrbVisualTier.Small, OrbVisualTier.Medium, OrbVisualTier.Large }));

            foreach (ScriptableObject profile in CollectProfiles(set))
                Assert.That(MonoScript.FromScriptableObject(profile), Is.Not.Null, AssetDatabase.GetAssetPath(profile));
        }

        [Test]
        public void WorldFeedbackProfileSet_ResolvesEveryReferencedAssetId()
        {
            WorldFeedbackProfileSetSO set = Load<WorldFeedbackProfileSetSO>(WorldProfilePath);
            GameplayPresentationSetSO presentation = Load<GameplayPresentationSetSO>(GameplayPresentationPath);
            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(presentation.SharedCatalogBundle, out AssetCatalogBundleLease shared, out string sharedIssue), Is.True, sharedIssue);
            Assert.That(runtime.Acquire(presentation.GameplayCatalogBundle, out AssetCatalogBundleLease gameplay, out string gameplayIssue), Is.True, gameplayIssue);
            try
            {
                var ids = new ReferencedIds();
                CollectIds(set, ids, new HashSet<int>());
                Assert.That(ids.SpriteIds, Is.Not.Empty);
                Assert.That(ids.AudioIds, Is.Not.Empty);
                Assert.That(ids.VfxIds, Is.Not.Empty);
                Assert.That(ids.MotionIds, Is.Not.Empty);
                foreach (SpriteAssetId id in ids.SpriteIds)
                    Assert.That(runtime.SpriteCatalog.TryGet(id, out _), Is.True, $"Missing Sprite {id.Value}");
                foreach (AudioAssetId id in ids.AudioIds)
                    Assert.That(runtime.AudioCatalog.TryGet(id, out _), Is.True, $"Missing Audio {id.Value}");
                foreach (VfxAssetId id in ids.VfxIds)
                {
                    Assert.That(runtime.VfxCatalog.TryGet(id, out GameObject prefab), Is.True, $"Missing VFX {id.Value}");
                    Assert.That(prefab.GetComponents<VfxWrapperInstance>(), Has.Length.EqualTo(1),
                        $"VFX {id.Value} must use the pooled wrapper contract.");
                }
                foreach (MotionAssetId id in ids.MotionIds)
                    Assert.That(runtime.MotionCatalog.TryGet(id, out _), Is.True, $"Missing Motion {id.Value}");
            }
            finally
            {
                gameplay.Dispose();
                shared.Dispose();
            }
        }

        [Test]
        public void CommanderHitImpactFeedback_ScreenOverlayMotionEndsTransparent()
        {
            WorldFeedbackProfileSetSO set = Load<WorldFeedbackProfileSetSO>(WorldProfilePath);
            GameplayPresentationSetSO presentation = Load<GameplayPresentationSetSO>(GameplayPresentationPath);
            CombatImpactFeedbackProfileSO profile = set.CombatImpactBindings
                .Single(binding => binding.ImpactKind.Value == "commander.hit.normal")
                .Profile;
            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(presentation.SharedCatalogBundle, out AssetCatalogBundleLease shared, out string sharedIssue), Is.True, sharedIssue);
            Assert.That(runtime.Acquire(presentation.GameplayCatalogBundle, out AssetCatalogBundleLease gameplay, out string gameplayIssue), Is.True, gameplayIssue);
            try
            {
                Assert.That(runtime.MotionCatalog.TryGet(profile.ScreenFeedbackMotionId, out AnimationClip clip), Is.True);
                EditorCurveBinding alphaBinding = AnimationUtility.GetCurveBindings(clip)
                    .Single(binding => binding.type == typeof(CanvasGroup) && binding.propertyName == "m_Alpha");
                AnimationCurve alpha = AnimationUtility.GetEditorCurve(clip, alphaBinding);
                Assert.That(alpha.keys[^1].value, Is.EqualTo(0f).Within(0.001f),
                    "Commander hit overlay must not remain visible after its transient feedback finishes.");
            }
            finally
            {
                gameplay.Dispose();
                shared.Dispose();
            }
        }

        [Test]
        public void GameplayScene_HasOneAuthoredWorldFeedbackBinderAndMatchingBootstrapProfile()
        {
            WorldFeedbackProfileSetSO expected = Load<WorldFeedbackProfileSetSO>(WorldProfilePath);
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                WorldFeedbackSceneBinder binder = roots.SelectMany(x => x.GetComponentsInChildren<WorldFeedbackSceneBinder>(true)).Single();
                RunBootstrap bootstrap = roots.SelectMany(x => x.GetComponentsInChildren<RunBootstrap>(true)).Single();
                Assert.That(binder.Profiles, Is.SameAs(expected));
                Assert.That(new SerializedObject(bootstrap).FindProperty("worldFeedbackProfiles").objectReferenceValue, Is.SameAs(expected));

                SerializedObject serializedBinder = new SerializedObject(binder);
                Assert.That(serializedBinder.FindProperty("_runBootstrap").objectReferenceValue, Is.SameAs(bootstrap));
                Assert.That(serializedBinder.FindProperty("_vfxRoot").objectReferenceValue, Is.Not.Null);
                Assert.That(serializedBinder.FindProperty("_audioSource").objectReferenceValue, Is.Not.Null);
                Assert.That(serializedBinder.FindProperty("_screenOverlay").objectReferenceValue, Is.Not.Null);
                Assert.That(serializedBinder.FindProperty("_screenMotion").objectReferenceValue, Is.Not.Null);
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
        public void GenericEnemySpawn_DoesNotPlayBossSpawnVfx()
        {
            string source = System.IO.File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Presentation/Runtime/WorldFeedbackSceneBinder.cs");
            int methodStart = source.IndexOf("private void OnEnemySpawn", StringComparison.Ordinal);
            int nextMethod = source.IndexOf("private void OnEnemyDeath", methodStart, StringComparison.Ordinal);

            Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(nextMethod, Is.GreaterThan(methodStart));
            string methodSource = source.Substring(methodStart, nextMethod - methodStart);
            StringAssert.DoesNotContain("SpawnMarkerVfxId", methodSource);
            StringAssert.DoesNotContain("SpawnVfxId", methodSource);
        }

        [Test]
        public void ExperienceAbsorption_UsesDedicatedNonHealingFeedbackRoute()
        {
            WorldFeedbackProfileSetSO set = Load<WorldFeedbackProfileSetSO>(WorldProfilePath);
            VfxAssetId healingVfxId = set.CommanderProfile.HealVfxId;
            VfxAssetId[] absorbVfxIds = set.ExperienceOrbBindings
                .Select(binding => binding.Profile.AbsorbBurstVfxId)
                .Distinct()
                .ToArray();

            Assert.That(absorbVfxIds, Has.Length.EqualTo(1));
            Assert.That(absorbVfxIds[0].IsNone, Is.False);
            Assert.That(absorbVfxIds[0], Is.Not.EqualTo(healingVfxId));
            Assert.That(set.ExperienceOrbBindings.All(binding =>
                binding.Profile.AbsorbTrailVfxId == absorbVfxIds[0]), Is.True);

            string collectorSource = System.IO.File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Commander/Runtime/CommanderGemCollector.cs");
            StringAssert.DoesNotContain("RetroVfxKind.XpAbsorb", collectorSource);
        }

        [Test]
        public void RunOutcome_DoesNotLeaveTheSharedScreenFeedbackOverlayVisible()
        {
            string source = System.IO.File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Presentation/Runtime/WorldFeedbackSceneBinder.cs");
            int methodStart = source.IndexOf("private void OnRunOutcome", StringComparison.Ordinal);
            int nextMethod = source.IndexOf("private void ApplyExperienceSprite", methodStart, StringComparison.Ordinal);

            Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(nextMethod, Is.GreaterThan(methodStart));
            string methodSource = source.Substring(methodStart, nextMethod - methodStart);
            StringAssert.Contains("_screenOverlay.gameObject.SetActive(false);", methodSource);
            StringAssert.DoesNotContain("TryPlayMotion", methodSource);
        }

        private static IEnumerable<ScriptableObject> CollectProfiles(WorldFeedbackProfileSetSO set)
        {
            var profiles = new HashSet<ScriptableObject> { set, set.CommanderProfile, set.WorldUiProfile, set.RunOutcomeProfile };
            foreach (CompanionLifecycleFeedbackBinding binding in set.CompanionLifecycleBindings) profiles.Add(binding.Profile);
            foreach (CombatImpactFeedbackBinding binding in set.CombatImpactBindings) profiles.Add(binding.Profile);
            foreach (StatusFeedbackBinding binding in set.StatusBindings) profiles.Add(binding.Profile);
            foreach (AttackFeedbackBinding binding in set.AttackBindings) profiles.Add(binding.Profile);
            foreach (EnemyAttackFeedbackBinding binding in set.EnemyAttackBindings) profiles.Add(binding.Profile);
            foreach (EnemySpawnFeedbackBinding binding in set.EnemySpawnBindings) profiles.Add(binding.Profile);
            foreach (EnemyDeathFeedbackBinding binding in set.EnemyDeathBindings) profiles.Add(binding.Profile);
            foreach (ExperienceOrbFeedbackBinding binding in set.ExperienceOrbBindings) profiles.Add(binding.Profile);
            return profiles.Where(x => x != null);
        }

        private static void CollectIds(object value, ReferencedIds ids, HashSet<int> visitedObjects)
        {
            if (value == null)
                return;
            if (value is SpriteAssetId sprite) { if (!sprite.IsNone) ids.SpriteIds.Add(sprite); return; }
            if (value is AudioAssetId audio) { if (!audio.IsNone) ids.AudioIds.Add(audio); return; }
            if (value is VfxAssetId vfx) { if (!vfx.IsNone) ids.VfxIds.Add(vfx); return; }
            if (value is MotionAssetId motion) { if (!motion.IsNone) ids.MotionIds.Add(motion); return; }
            if (value is string || value.GetType().IsPrimitive || value.GetType().IsEnum)
                return;
            if (value is UnityEngine.Object unityObject)
            {
                if (!(unityObject is ScriptableObject) || !visitedObjects.Add(unityObject.GetInstanceID()))
                    return;
            }
            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                    CollectIds(item, ids, visitedObjects);
                return;
            }

            Type type = value.GetType();
            if (type.Namespace == null || !type.Namespace.StartsWith("Lizzo.PV", StringComparison.Ordinal))
                return;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.IsStatic && !field.IsNotSerialized)
                    CollectIds(field.GetValue(value), ids, visitedObjects);
            }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(value, Is.Not.Null, path);
            return value;
        }

        private sealed class ReferencedIds
        {
            public readonly HashSet<SpriteAssetId> SpriteIds = new HashSet<SpriteAssetId>();
            public readonly HashSet<AudioAssetId> AudioIds = new HashSet<AudioAssetId>();
            public readonly HashSet<VfxAssetId> VfxIds = new HashSet<VfxAssetId>();
            public readonly HashSet<MotionAssetId> MotionIds = new HashSet<MotionAssetId>();
        }
    }
}
