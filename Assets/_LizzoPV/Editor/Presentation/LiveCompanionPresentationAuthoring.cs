using System;
using Lizzo.PV.P0.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Presentation
{
    public static class LiveCompanionPresentationAuthoring
    {
        private const string PrefabFolder = "Assets/_LizzoPV/Prefabs/Characters/Companions";
        private const string UnitPresentationSetPath = "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset";

        private static readonly EntrySpec[] LiveFamilyEntries =
        {
            new EntrySpec("sword_soldier", "Lizzo/Characters/Companions/sword_soldier", "SwordSoldier", "sword_soldier", "Assets/_LizzoPV/Prefabs/Units/Companions/Swordsman.prefab"),
            new EntrySpec("sword_captain", "Lizzo/Characters/Companions/sword_captain", "SwordCaptain", "sword_captain", "Assets/_LizzoPV/Prefabs/Units/Companions/Swordsman.prefab"),
            new EntrySpec("cleric", "Lizzo/Characters/Companions/cleric", "Cleric", "cleric", "Assets/_LizzoPV/Prefabs/Units/Companions/Cleric.prefab"),
            new EntrySpec("light_guide", "Lizzo/Characters/Companions/light_guide", "LightGuide", "light_guide", "Assets/_LizzoPV/Prefabs/Units/Companions/Cleric.prefab"),
            new EntrySpec("falcon_archer", "Lizzo/Characters/Companions/falcon_archer", "FalconArcher", "falcon_archer", "Assets/_LizzoPV/Prefabs/Units/Companions/Archer.prefab"),
            new EntrySpec("falcon_captain", "Lizzo/Characters/Companions/falcon_captain", "FalconCaptain", "falcon_captain", "Assets/_LizzoPV/Prefabs/Units/Companions/Archer.prefab"),
        };

        public static void CreateOrUpdate()
        {
            ShieldPresentationTracerAuthoring.CreateOrUpdate();
            AnimatorController sharedController = ShieldPresentationTracerAuthoring.EnsureSharedController();
            GameObject[] prefabs = new GameObject[LiveFamilyEntries.Length];
            for (int i = 0; i < LiveFamilyEntries.Length; i++)
            {
                EntrySpec entry = LiveFamilyEntries[i];
                prefabs[i] = ShieldPresentationTracerAuthoring.EnsureCanonicalPrefab(
                    entry.SourcePrefabPath,
                    entry.PrefabPath,
                    entry.LibraryPath,
                    sharedController,
                    entry.PrefabName);
                ShieldPresentationTracerAuthoring.ConfigureAddressable(entry.PrefabPath, entry.AddressableKey);
            }

            ConfigureUnitPresentationSet(prefabs);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureUnitPresentationSet(GameObject[] prefabs)
        {
            UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            if (set == null)
                throw new InvalidOperationException("Unit presentation set is missing.");

            GameObject shieldGuard = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/ShieldGuard.prefab");
            GameObject shieldCaptain = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/ShieldCaptain.prefab");
            if (shieldGuard == null || shieldCaptain == null)
                throw new InvalidOperationException("P9D1 shield presentation prefabs are missing.");

            UnitPresentationSet.Entry[] entries = new UnitPresentationSet.Entry[8];
            entries[0] = CreateEntry("shield_guard", "Lizzo/Characters/Companions/shield_guard", shieldGuard, "shield_guard");
            entries[1] = CreateEntry("shield_captain", "Lizzo/Characters/Companions/shield_captain", shieldCaptain, "shield_captain");
            for (int i = 0; i < LiveFamilyEntries.Length; i++)
            {
                EntrySpec specification = LiveFamilyEntries[i];
                entries[i + 2] = CreateEntry(specification.UnitId, specification.AddressableKey, prefabs[i], specification.LibraryId);
            }

            set.SetEntriesForEditor(entries);
            EditorUtility.SetDirty(set);
        }

        private static UnitPresentationSet.Entry CreateEntry(string unitId, string addressableKey, GameObject prefab, string libraryId)
        {
            return new UnitPresentationSet.Entry(
                unitId,
                addressableKey,
                prefab,
                ShieldPresentationTracerAuthoring.FindSprite(
                    "Assets/_LizzoPV/Art/Characters/Companions/" + libraryId + "_SpriteSheet.png",
                    "Idle_0"));
        }

        private readonly struct EntrySpec
        {
            public readonly string UnitId;
            public readonly string AddressableKey;
            public readonly string PrefabName;
            public readonly string LibraryId;
            public readonly string SourcePrefabPath;

            public EntrySpec(string unitId, string addressableKey, string prefabName, string libraryId, string sourcePrefabPath)
            {
                UnitId = unitId;
                AddressableKey = addressableKey;
                PrefabName = prefabName;
                LibraryId = libraryId;
                SourcePrefabPath = sourcePrefabPath;
            }

            public string PrefabPath => PrefabFolder + "/" + PrefabName + ".prefab";
            public string LibraryPath => "Assets/_LizzoPV/Art/Characters/Companions/" + LibraryId + "_SpriteLibrary.asset";
        }
    }
}
