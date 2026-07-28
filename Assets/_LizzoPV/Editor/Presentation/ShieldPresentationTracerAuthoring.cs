using System;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class ShieldPresentationTracerAuthoring
    {
        private const string AnimationFolder = "Assets/_LizzoPV/Animations/Characters/Companions";
        private const string PrefabFolder = "Assets/_LizzoPV/Prefabs/Characters/Companions";
        private const string ControllerPath = AnimationFolder + "/CompanionSpriteShared.controller";
        private const string GuardPrefabPath = PrefabFolder + "/ShieldGuard.prefab";
        private const string CaptainPrefabPath = PrefabFolder + "/ShieldCaptain.prefab";
        private const string GuardSourcePrefabPath = "Assets/_LizzoPV/Prefabs/Units/Companions/ShieldSoldier.prefab";
        private const string CaptainSourcePrefabPath = "Assets/_LizzoPV/Prefabs/Units/Companions/ShieldCaptain.prefab";
        private const string GuardLibraryPath = "Assets/_LizzoPV/Art/Characters/Companions/shield_guard_SpriteLibrary.asset";
        private const string CaptainLibraryPath = "Assets/_LizzoPV/Art/Characters/Companions/shield_captain_SpriteLibrary.asset";
        private const string GuardSheetPath = "Assets/_LizzoPV/Art/Characters/Companions/shield_guard_SpriteSheet.png";
        private const string CaptainSheetPath = "Assets/_LizzoPV/Art/Characters/Companions/shield_captain_SpriteSheet.png";
        private const string UnitPresentationSetPath = "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset";
        private const string GuardAddress = "Lizzo/Characters/Companions/shield_guard";
        private const string CaptainAddress = "Lizzo/Characters/Companions/shield_captain";

        public static void CreateOrUpdate()
        {
            EnsureFolder("Assets/_LizzoPV/Animations");
            EnsureFolder("Assets/_LizzoPV/Animations/Characters");
            EnsureFolder(AnimationFolder);
            EnsureFolder("Assets/_LizzoPV/Prefabs/Characters");
            EnsureFolder(PrefabFolder);

            AnimatorController controller = EnsureSharedController();
            GameObject guardPrefab = EnsureCanonicalPrefab(GuardSourcePrefabPath, GuardPrefabPath, GuardLibraryPath, controller, "ShieldGuard");
            GameObject captainPrefab = EnsureCanonicalPrefab(CaptainSourcePrefabPath, CaptainPrefabPath, CaptainLibraryPath, controller, "ShieldCaptain");
            ConfigureUnitPresentationSet(guardPrefab, captainPrefab);
            ConfigureAddressable(GuardPrefabPath, GuardAddress);
            ConfigureAddressable(CaptainPrefabPath, CaptainAddress);
            AssetDatabase.SaveAssets();
        }

        internal static AnimatorController EnsureSharedController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = EnsureState(stateMachine, "Idle", EnsureClip("Idle", true));
            EnsureState(stateMachine, "Run", EnsureClip("Run", true));
            EnsureState(stateMachine, "Attack", EnsureClip("Attack", false));
            EnsureState(stateMachine, "Death", EnsureClip("Death", false));
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip EnsureClip(string stateName, bool loop)
        {
            string path = AnimationFolder + "/CompanionSpriteShared_" + stateName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
                return clip;

            clip = new AnimationClip { frameRate = 9.0f, name = "CompanionSpriteShared_" + stateName };
            clip.SetCurve(string.Empty, typeof(Transform), "m_LocalPosition.x", new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(8.0f / 9.0f, 0.0f)));
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static AnimatorState EnsureState(AnimatorStateMachine stateMachine, string stateName, Motion motion)
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                {
                    states[i].state.motion = motion;
                    return states[i].state;
                }
            }

            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = motion;
            return state;
        }

        internal static GameObject EnsureCanonicalPrefab(
            string sourcePath,
            string targetPath,
            string libraryPath,
            AnimatorController controller,
            string rootName)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) == null)
            {
                if (AssetDatabase.CopyAsset(sourcePath, targetPath) == false)
                    throw new InvalidOperationException("Failed to create canonical companion prefab: " + targetPath);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(targetPath);
            try
            {
                root.name = rootName;
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                    throw new InvalidOperationException("Canonical companion prefab requires Visual: " + targetPath);

                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                Animator animator = visual.GetComponent<Animator>();
                UnitVisualDriver driver = visual.GetComponent<UnitVisualDriver>();
                if (renderer == null || animator == null || driver == null)
                    throw new InvalidOperationException("Canonical companion Visual is missing required renderer, animator, or driver: " + targetPath);

                SpriteLibraryAsset libraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
                if (libraryAsset == null)
                    throw new InvalidOperationException("Accepted SpriteLibrary is missing: " + libraryPath);

                SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
                if (library == null)
                    library = visual.gameObject.AddComponent<SpriteLibrary>();
                library.spriteLibraryAsset = libraryAsset;

                SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
                if (resolver == null)
                    resolver = visual.gameObject.AddComponent<SpriteResolver>();
                resolver.SetCategoryAndLabel("Idle", "0");
                resolver.ResolveSpriteToSpriteRenderer();

                animator.runtimeAnimatorController = controller;
                SerializedObject driverObject = new SerializedObject(driver);
                driverObject.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
                driverObject.FindProperty("_animator").objectReferenceValue = animator;
                driverObject.FindProperty("_spriteResolver").objectReferenceValue = resolver;
                driverObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
        }

        private static void ConfigureUnitPresentationSet(GameObject guardPrefab, GameObject captainPrefab)
        {
            UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            if (set == null)
                throw new InvalidOperationException("Unit presentation set is missing.");

            set.SetEntriesForEditor(new[]
            {
                new UnitPresentationSet.Entry("shield_guard", GuardAddress, guardPrefab, FindSprite(GuardSheetPath, "Idle_0")),
                new UnitPresentationSet.Entry("shield_captain", CaptainAddress, captainPrefab, FindSprite(CaptainSheetPath, "Idle_0")),
            });
            EditorUtility.SetDirty(set);
        }

        internal static Sprite FindSprite(string sheetPath, string spriteName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }

            throw new InvalidOperationException("Required sprite is missing: " + sheetPath + "/" + spriteName);
        }

        internal static void ConfigureAddressable(string prefabPath, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings == null ? null : settings.FindGroup("Prefabs");
            if (group == null)
                throw new InvalidOperationException("Addressables Prefabs group is missing.");

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("Canonical prefab GUID is missing: " + prefabPath);

            settings.AddLabel("Prefab", true);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, true);
            entry.address = address;
            entry.SetLabel("Prefab", true, true, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true, true);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            if (slash <= 0)
                throw new InvalidOperationException("Invalid asset folder: " + path);

            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
