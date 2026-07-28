using System;
using Lizzo.PV.P0.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class RemainingCompanionPresentationAuthoring
    {
        private const string PrefabFolder = "Assets/_LizzoPV/Prefabs/Characters/Companions";
        private const string UnitPresentationSetPath = "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset";
        private const string SharedControllerPath = "Assets/_LizzoPV/Animations/Characters/Companions/CompanionSpriteShared.controller";
        private const string PartyUnitBasePath = "Assets/_LizzoPV/Prefabs/Units/Base/PartyUnitBase.prefab";

        private static readonly EntrySpec[] ExistingEntries =
        {
            new EntrySpec("shield_guard", "ShieldGuard", "shield_guard"),
            new EntrySpec("shield_captain", "ShieldCaptain", "shield_captain"),
            new EntrySpec("sword_soldier", "SwordSoldier", "sword_soldier"),
            new EntrySpec("sword_captain", "SwordCaptain", "sword_captain"),
            new EntrySpec("cleric", "Cleric", "cleric"),
            new EntrySpec("light_guide", "LightGuide", "light_guide"),
            new EntrySpec("falcon_archer", "FalconArcher", "falcon_archer"),
            new EntrySpec("falcon_captain", "FalconCaptain", "falcon_captain"),
        };

        private static readonly EntrySpec[] RemainingEntries =
        {
            new EntrySpec("field_herbalist", "FieldHerbalist", "field_herbalist"),
            new EntrySpec("battle_apothecary", "BattleApothecary", "battle_apothecary"),
            new EntrySpec("bombardier", "Bombardier", "bombardier"),
            new EntrySpec("powder_captain", "PowderCaptain", "powder_captain"),
            new EntrySpec("fire_mage", "FireMage", "fire_mage"),
            new EntrySpec("fire_sage", "FireSage", "fire_sage"),
            new EntrySpec("lightning_mage", "LightningMage", "lightning_mage"),
            new EntrySpec("storm_mage", "StormMage", "storm_mage"),
            new EntrySpec("wolf_tamer", "WolfTamer", "wolf_tamer"),
            new EntrySpec("beast_commander", "BeastCommander", "beast_commander"),
            new EntrySpec("wraith_knight", "WraithKnight", "wraith_knight"),
            new EntrySpec("wraith_guardian", "WraithGuardian", "wraith_guardian"),
            new EntrySpec("necromancer", "Necromancer", "necromancer"),
            new EntrySpec("dark_ritualist", "DarkRitualist", "dark_ritualist"),
            new EntrySpec("skeleton_bomber", "SkeletonBomber", "skeleton_bomber"),
            new EntrySpec("bone_artillery", "BoneArtillery", "bone_artillery"),
        };

        public static void CreateOrUpdate()
        {
            AnimatorController sharedController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath);
            if (sharedController == null || sharedController.animationClips.Length != 4)
                throw new InvalidOperationException("The accepted P9D shared controller is missing or incomplete.");

            GameObject[] entries = new GameObject[ExistingEntries.Length + RemainingEntries.Length];
            for (int i = 0; i < ExistingEntries.Length; i++)
                entries[i] = RequireExistingPrefab(ExistingEntries[i]);

            for (int i = 0; i < RemainingEntries.Length; i++)
            {
                EntrySpec entry = RemainingEntries[i];
                entries[i + ExistingEntries.Length] = ShieldPresentationTracerAuthoring.EnsureCanonicalPrefab(
                    PartyUnitBasePath,
                    entry.PrefabPath,
                    entry.LibraryPath,
                    sharedController,
                    entry.PrefabName);
                ShieldPresentationTracerAuthoring.ConfigureAddressable(entry.PrefabPath, entry.AddressableKey);
            }

            ConfigureUnitPresentationSet(entries);
            AssetDatabase.SaveAssets();
        }

        private static GameObject RequireExistingPrefab(EntrySpec entry)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Accepted P9D prefab is missing: " + entry.PrefabPath);

            return prefab;
        }

        private static void ConfigureUnitPresentationSet(GameObject[] prefabs)
        {
            UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            if (set == null)
                throw new InvalidOperationException("Unit presentation set is missing.");

            UnitPresentationSet.Entry[] catalogEntries = new UnitPresentationSet.Entry[prefabs.Length];
            for (int i = 0; i < ExistingEntries.Length; i++)
                catalogEntries[i] = CreateEntry(ExistingEntries[i], prefabs[i]);
            for (int i = 0; i < RemainingEntries.Length; i++)
                catalogEntries[i + ExistingEntries.Length] = CreateEntry(RemainingEntries[i], prefabs[i + ExistingEntries.Length]);

            set.SetEntriesForEditor(catalogEntries);
            EditorUtility.SetDirty(set);
        }

        private static UnitPresentationSet.Entry CreateEntry(EntrySpec entry, GameObject prefab)
        {
            return new UnitPresentationSet.Entry(
                entry.UnitId,
                entry.AddressableKey,
                prefab,
                ShieldPresentationTracerAuthoring.FindSprite(entry.SheetPath, "Idle_0"));
        }

        private readonly struct EntrySpec
        {
            public readonly string UnitId;
            public readonly string PrefabName;
            public readonly string LibraryId;

            public EntrySpec(string unitId, string prefabName, string libraryId)
            {
                UnitId = unitId;
                PrefabName = prefabName;
                LibraryId = libraryId;
            }

            public string AddressableKey => "Lizzo/Characters/Companions/" + UnitId;
            public string PrefabPath => PrefabFolder + "/" + PrefabName + ".prefab";
            public string LibraryPath => "Assets/_LizzoPV/Art/Characters/Companions/" + LibraryId + "_SpriteLibrary.asset";
            public string SheetPath => "Assets/_LizzoPV/Art/Characters/Companions/" + LibraryId + "_SpriteSheet.png";
        }
    }
}
