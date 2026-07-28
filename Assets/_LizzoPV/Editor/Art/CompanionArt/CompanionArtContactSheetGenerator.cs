using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTools.Art.Companions
{
    [Serializable]
    public sealed class CompanionArtManifest
    {
        public string version;
        public string sourcePrefab;
        public string sourceSpriteCollection;
        public string sourceFont;
        public string frame;
        public int previewWidth;
        public int previewHeight;
        public CompanionArtManifestEntry[] entries;
    }

    [Serializable]
    public sealed class CompanionArtManifestEntry
    {
        public string baseId;
        public string promotionId;
        public string baseKoreanLabel;
        public string promotionKoreanLabel;
        public string role;
        public string basePreview;
        public string promotionPreview;
        public string baseBody;
        public string promotionBody;
        public string baseArmor;
        public string promotionArmor;
        public string baseHelmet;
        public string promotionHelmet;
        public string baseWeapon;
        public string promotionWeapon;
        public string baseShield;
        public string promotionShield;
        public string baseBack;
        public string promotionBack;
        public string[] basePalette;
        public string[] promotionPalette;
        public int integerScale;
        public int baseVisibleHeight;
        public int promotionVisibleHeight;
        public bool externalPropRequired;
        public string externalPropReason;
    }

    [Serializable]
    public sealed class CompanionArtV4Manifest
    {
        public string version;
        public string sourcePrefab;
        public string sourceSpriteCollection;
        public string sourceFont;
        public string frame;
        public int previewWidth;
        public int previewHeight;
        public CompanionArtV4PairEntry[] pairs;
        public CompanionArtV4SupportEntry[] support;
    }

    [Serializable]
    public sealed class CompanionArtV4PairEntry
    {
        public string baseId;
        public string promotionId;
        public string baseKoreanLabel;
        public string promotionKoreanLabel;
        public string role;
        public string basePreview;
        public string promotionPreview;
        public string baseBody;
        public string promotionBody;
        public string baseArmor;
        public string promotionArmor;
        public string baseHelmet;
        public string promotionHelmet;
        public string baseWeapon;
        public string promotionWeapon;
        public string baseShield;
        public string promotionShield;
        public string baseBack;
        public string promotionBack;
        public string[] basePalette;
        public string[] promotionPalette;
        public string[] persistentIdentityCues;
        public string promotionDelta;
        public int integerScale;
        public int baseVisibleHeight;
        public int promotionVisibleHeight;
        public int baseOpaquePixels;
        public int promotionOpaquePixels;
        public int baseSideMass;
        public int promotionSideMass;
        public int baseHeadCorePixels;
        public int promotionHeadCorePixels;
        public int baseCentroidX1000;
        public int promotionCentroidX1000;
        public int silhouetteDelta;
        public bool externalPropRequired;
        public string externalPropReason;
    }

    [Serializable]
    public sealed class CompanionArtV4SupportEntry
    {
        public string id;
        public string label;
        public string sourcePrefab;
        public string sourceArt;
        public string sourceLabel;
        public string preview;
        public string[] composition;
        public string limitation;
    }

    [Serializable]
    public sealed class CompanionArtV6Manifest
    {
        public string version;
        public string sourcePrefab;
        public string sourceSpriteCollection;
        public string sourceFont;
        public string frame;
        public int previewWidth;
        public int previewHeight;
        public string[] selectionProvenance;
        public CompanionArtV4PairEntry[] pairs;
        public CompanionArtV4SupportEntry[] support;
    }

    [Serializable]
    public sealed class CompanionFullSpriteSheetManifest
    {
        public string exportVersion;
        public string sourceManifest;
        public string sourceProvenance;
        public string[] rowOrder;
        public int sheetWidth;
        public int sheetHeight;
        public int categoryCount;
        public int labelCount;
        public CompanionFullSpriteSheetEntry[] entries;
    }

    [Serializable]
    public sealed class CompanionFullSpriteSheetEntry
    {
        public string id;
        public string promotionOf;
        public string body;
        public string armor;
        public string helmet;
        public string weapon;
        public string shield;
        public string back;
        public string sourceContactSheet;
        public string sourceSelection;
        public string[] sourceRows;
        public string sourceAttackMotion;
        public string[] emptySourceSlots;
        public string sheetPath;
        public string libraryPath;
        public int width;
        public int height;
        public int categoryCount;
        public int labelCount;
    }

    [Serializable]
    public sealed class SummonSpriteSheetManifest
    {
        public string exportVersion;
        public string sourceManifest;
        public int sheetWidth;
        public int sheetHeight;
        public SummonSpriteSheetEntry[] entries;
    }

    [Serializable]
    public sealed class SummonSpriteSheetEntry
    {
        public string id;
        public string label;
        public string sourcePrefab;
        public string sourceArt;
        public string sourceLabel;
        public string limitation;
        public string sheetPath;
        public string libraryPath;
        public int width;
        public int height;
        public int categoryCount;
        public int labelCount;
    }

    [Serializable]
    public sealed class CompanionArtThreeFamilyCandidateManifest
    {
        public string version;
        public string sourcePrefab;
        public string sourceSpriteCollection;
        public string sourceFont;
        public string[] availableIdleFrames;
        public string[] renderableIdleFrames;
        public int previewWidth;
        public int previewHeight;
        public CompanionArtThreeFamilyCandidateEntry[] pairs;
    }

    [Serializable]
    public sealed class CompanionArtThreeFamilyCandidateEntry
    {
        public string optionId;
        public string family;
        public string baseId;
        public string promotionId;
        public string baseKoreanLabel;
        public string promotionKoreanLabel;
        public string reviewFrame;
        public string directionEvidence;
        public string baseBody;
        public string promotionBody;
        public string baseArmor;
        public string promotionArmor;
        public string baseHead;
        public string promotionHead;
        public string baseHelmet;
        public string promotionHelmet;
        public string baseWeapon;
        public string promotionWeapon;
        public string baseShield;
        public string promotionShield;
        public string baseBack;
        public string promotionBack;
        public string[] basePalette;
        public string[] promotionPalette;
        public string basePreview;
        public string promotionPreview;
        public int baseOpaquePixels;
        public int promotionOpaquePixels;
        public int baseSideMass;
        public int promotionSideMass;
        public int baseHeadCorePixels;
        public int promotionHeadCorePixels;
        public int baseMinX;
        public int baseMaxX;
        public int promotionMinX;
        public int promotionMaxX;
        public int baseCentroidX1000;
        public int promotionCentroidX1000;
        public int silhouetteDelta;
    }

    public static class CompanionArtContactSheetGenerator
    {
        public const string OutputDirectory = "Docs/Reference/CompanionArt/V2";
        public const string ManifestAssetPath = OutputDirectory + "/companion_art_manifest.json";
        public const string ContactSheetAssetPath = OutputDirectory + "/companion_art_contact_sheet.png";
        public const string V3OutputDirectory = "Docs/Reference/CompanionArt/V3";
        public const string V3ContactSheetAssetPath = V3OutputDirectory + "/companion_art_contact_sheet.png";
        public const string V4OutputDirectory = "Docs/Reference/CompanionArt/V4";
        public const string V4ManifestAssetPath = V4OutputDirectory + "/companion_art_manifest.json";
        public const string V4ContactSheetAssetPath = V4OutputDirectory + "/companion_art_contact_sheet.png";
        public const string V5OutputDirectory = "Docs/Reference/CompanionArt/V5";
        public const string V5ManifestAssetPath = V5OutputDirectory + "/companion_art_manifest.json";
        public const string V5ContactSheetAssetPath = V5OutputDirectory + "/companion_art_contact_sheet.png";
        public const string V6OutputDirectory = "Docs/Reference/CompanionArt/V6";
        public const string V6ManifestAssetPath = V6OutputDirectory + "/companion_art_manifest.json";
        public const string V6ContactSheetAssetPath = V6OutputDirectory + "/companion_art_contact_sheet.png";
        public const string CompanionSheetOutputDirectory = "Assets/_LizzoPV/Art/Characters/Companions";
        public const string CompanionSheetManifestAssetPath = CompanionSheetOutputDirectory + "/companions_sprite_sheet_manifest.json";
        public const string SummonSheetOutputDirectory = "Assets/_LizzoPV/Art/Characters/Summons";
        public const string SummonSheetManifestAssetPath = SummonSheetOutputDirectory + "/summons_sprite_sheet_manifest.json";
        public const string CompactCompanionExportVersion = "V6_CompactMotionSheets_V1";
        public const string CandidatesThreeFamiliesOutputDirectory = "Docs/Reference/CompanionArt/Candidates_ThreeFamilies";
        public const string CandidatesThreeFamiliesManifestAssetPath = CandidatesThreeFamiliesOutputDirectory + "/companion_art_candidates_manifest.json";
        public const string CandidatesThreeFamiliesContactSheetAssetPath = CandidatesThreeFamiliesOutputDirectory + "/companion_art_candidates_contact_sheet.png";
        public const string PrefabPath = "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Prefabs/Character.prefab";
        public const string SpriteCollectionPath = "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Resources/SpriteCollection.asset";
        public const string FontPath = "Assets/_LizzoPV/Fonts/Pretendard/Source/Pretendard-ExtraBold.otf";

        private const int PreviewSize = 192;
        private const int FrameSize = 64;
        private const int FrameY = 832;
        private const int FootBaseline = 24;
        private const int RowHeight = 300;
        private const int SheetWidth = 1500;
        private const int BaseX = 390;
        private const int PromotionX = 1080;
        private const int LabelWidth = 330;
        private const int LabelHeight = 92;
        private const int LabelLayer = 31;
        public const int V3SheetWidth = 1500;
        public const int V3HeaderHeight = 150;
        public const int V3RowHeight = 430;
        public const int V3LabelTopOffset = 8;
        public const int V3NameHeight = 34;
        public const int V3IdTopOffset = 46;
        public const int V3IdHeight = 34;
        public const int V3PreviewTopOffset = 105;
        public const int V3CardSize = 212;
        public const int V3MarkerTopOffset = 325;
        public const int V3MarkerHeight = 92;
        public const int V4HeaderHeight = 180;
        public const int V4SheetWidth = 1500;
        public const int V4RowHeight = 430;
        public const int V4PreviewTopOffset = 105;
        public const int V4CardSize = 212;
        public const int V4MarkerTopOffset = 325;
        public const int V4MarkerHeight = 92;
        public const int V4SupportHeaderGap = 70;
        public const int V4SupportCellTopOffset = 130;
        public const int V4SupportCellHeight = 420;
        public const int CandidateHeaderHeight = 180;
        public const int CandidateRowHeight = 430;
        public const int CandidatePreviewTopOffset = 150;
        public const int CandidateCardSize = 212;
        public const int CandidateLabelTopOffset = 8;
        public const int CandidateLabelRectHeight = 130;
        private static readonly Color32 Navy = new(16, 44, 98, 255);
        private static readonly Color32 Orange = new(224, 116, 56, 255);
        private static readonly Color32 CardFill = new(234, 247, 255, 255);
        private static readonly Color32 CardBorder = new(117, 199, 255, 255);

        private readonly struct Definition
        {
            public readonly string BaseId;
            public readonly string PromotionId;
            public readonly string BaseKoreanLabel;
            public readonly string PromotionKoreanLabel;
            public readonly string Role;
            public readonly string Body;
            public readonly string Armor;
            public readonly string Helmet;
            public readonly string Weapon;
            public readonly string Shield;
            public readonly string Back;
            public readonly string PromotionBody;
            public readonly string PromotionArmor;
            public readonly string PromotionHelmet;
            public readonly string PromotionWeapon;
            public readonly string PromotionShield;
            public readonly string PromotionBack;
            public readonly string[] BasePalette;
            public readonly string[] PromotionPalette;
            public readonly bool ExternalPropRequired;
            public readonly string ExternalPropReason;

            public Definition(string baseId, string promotionId, string baseKoreanLabel, string promotionKoreanLabel, string role,
                string body, string armor, string helmet, string weapon, string shield, string back,
                string promotionBody, string promotionArmor, string promotionHelmet, string promotionWeapon,
                string promotionShield, string promotionBack, string[] basePalette, string[] promotionPalette,
                bool externalPropRequired, string externalPropReason)
            {
                BaseId = baseId;
                PromotionId = promotionId;
                BaseKoreanLabel = baseKoreanLabel;
                PromotionKoreanLabel = promotionKoreanLabel;
                Role = role;
                Body = body;
                Armor = armor;
                Helmet = helmet;
                Weapon = weapon;
                Shield = shield;
                Back = back;
                PromotionBody = promotionBody;
                PromotionArmor = promotionArmor;
                PromotionHelmet = promotionHelmet;
                PromotionWeapon = promotionWeapon;
                PromotionShield = promotionShield;
                PromotionBack = promotionBack;
                BasePalette = basePalette;
                PromotionPalette = promotionPalette;
                ExternalPropRequired = externalPropRequired;
                ExternalPropReason = externalPropReason;
            }
        }

        private static readonly Definition[] Definitions =
        {
            new("shield_guard", "shield_captain", "방패병", "방패대장", "shield/sword/front tank", "Human", "GuardianTunic#0C2E44", "GuardianHelmet#F3D255", "IronSword#F8FCFF", "GuardianShield#F3D255", "", "Human", "CaptainArmor#0C2E44", "CaptainHelmet#F3D255", "Longsword#F8FCFF", "RoyalGreatShield#F3D255", "", new[] { "allied blue", "gold" }, new[] { "allied blue", "gold" }, false, ""),
            new("sword_soldier", "sword_captain", "검병", "검투대장", "sword/front melee", "Human", "BlueKnight#2E6FB7", "BlueKnightHelmet#F3D255", "IronSword#F8FCFF", "", "", "Human", "IronKnight#0C2E44", "IronKnightHelmet#F3D255", "Longsword#F8FCFF", "", "", new[] { "allied blue", "gold" }, new[] { "allied blue", "gold" }, false, ""),
            new("cleric", "light_guide", "성직자", "빛의 인도자", "heal/rear", "Human", "ClericRobe#D4E9F8", "ClericHood#F3D255", "BishopStaff#7DCE8A", "", "", "Human", "Angel#F8FCFF", "Angel#F3D255", "PriestWand#7DCE8A", "", "", new[] { "allied white", "heal green", "gold" }, new[] { "allied white", "heal green", "gold" }, false, ""),
            new("falcon_archer", "falcon_captain", "매사냥꾼", "매사냥 대장", "bow/beast/rear", "Human", "ArcherTunic#2E6FB7", "ArcherHood#F3D255", "Bow#F8FCFF", "", "LeatherQuiver#D4E9F8", "Human", "CaptainArmor#2E6FB7", "CaptainHelmet#F3D255", "LongBow#F8FCFF", "", "LeatherQuiver#D4E9F8", new[] { "allied blue", "gold" }, new[] { "allied blue", "gold" }, true, "Exact falcon owner prop is not present; add as a later child prop, not a squad-slot owner."),
            new("field_herbalist", "battle_apothecary", "전투 약초사", "전장의 약제사", "heal/rear", "Human", "DruidRobe#2E6FB7", "DruidHelmet#7DCE8A", "", "", "SmallBackpack#D4E9F8", "Human", "PlagueDoctor#2E6FB7", "PlagueDoctor#7DCE8A", "", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "heal green" }, new[] { "allied blue", "heal green" }, true, "No exact bottle or dart hand prop is present; no misleading substitute is used."),
            new("bombardier", "powder_captain", "폭탄병", "화약 대장", "explosive-support/rear", "Human", "MusketeerTunic#2E6FB7", "MusketeerHat [ShowEars]#F3D255", "", "", "SmallBackpack#D4E9F8", "Human", "BulletHeadArmor#0C2E44", "BulletHeadHelm#F3D255", "", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "explosive orange" }, new[] { "allied blue", "explosive orange" }, true, "No exact bomb hand prop is present; no misleading substitute is used."),
            new("fire_mage", "fire_sage", "화염술사", "화염 현자", "fire field mage", "Human", "FireWizardRobe#C64524", "FireWizardHood#F3D255", "FireWand#FFA214", "", "", "Human", "FireWarriorArmor#C64524", "FireWarriorHelment#F3D255", "FlameStaff#FFA214", "", "", new[] { "fire orange-red", "gold" }, new[] { "fire orange-red", "gold" }, false, ""),
            new("lightning_mage", "storm_mage", "번개술사", "폭풍술사", "chain lightning mage", "Human", "BlueWizardTunic#2E6FB7", "BlueWizzardHat#F3D255", "BlueWand#00CDF9", "", "", "Human", "BlueWizardTunic#0C2E44", "BlueWizzardHat#F3D255", "StormStaff#00CDF9", "", "", new[] { "allied blue", "lightning cyan", "gold" }, new[] { "allied blue", "lightning cyan", "gold" }, false, ""),
            new("wolf_tamer", "beast_commander", "늑대 조련사", "야수 지휘관", "beast owner/rear", "Human", "TravelerTunic#2E6FB7", "GuardianHelmet#F3D255", "HunterKnife#D4E9F8", "", "", "Human", "GuardianTunic#0C2E44", "GuardianHelmet#F3D255", "HunterKnife#D4E9F8", "", "", new[] { "allied blue", "beast brown/teal" }, new[] { "allied blue", "beast brown/teal" }, true, "Exact wolf child prop is not present; wolf remains a later child prop, not a squad-slot owner."),
            new("wraith_knight", "wraith_guardian", "망령 기사", "망령 수호장", "spectral/sword/guard/front", "ZombieA", "DarkKnight#42506E", "DarkKnight#2E6FB7", "IronSword#8AB8C9", "IronBuckler#8AB8C9", "", "ZombieA", "DeathRobe#42506E", "DeathHood#2E6FB7", "Longsword#8AB8C9", "GuardianShield#8AB8C9", "", new[] { "undead cyan/blue-gray" }, new[] { "undead cyan/blue-gray" }, false, ""),
            new("necromancer", "dark_ritualist", "사령술사", "검은 의식자", "living occultist/rear", "Human", "NecromancerRobe#C7CFDD", "NecromancerHood#93388F", "NecromancerStaff#93388F", "", "", "Human", "DarkNecromant#C7CFDD", "DarkNecromant#93388F", "NecromancerStaff#93388F", "", "", new[] { "living pale", "restrained violet" }, new[] { "living pale", "restrained violet" }, false, ""),
            new("skeleton_bomber", "bone_artillery", "해골 폭탄병", "해골 포격수", "Skeleton/explosive-support/rear", "Skeleton", "MusketeerTunic#42506E", "MusketeerHat [ShowEars]#D4E9F8", "", "", "SmallBackpack#D4E9F8", "Skeleton", "BulletHeadArmor#42506E", "BulletHeadHelm#D4E9F8", "", "", "LargeBackpack#D4E9F8", new[] { "Skeleton", "explosive orange" }, new[] { "Skeleton", "explosive orange" }, true, "No exact bomb hand prop is present; no misleading substitute is used."),
        };

        private static readonly Definition[] V4Definitions =
        {
            new("shield_guard", "shield_captain", "방패병", "방패대장", "shield/sword/front tank", "Human", "HeavyKnightArmor#2E6FB7", "HeavyKnightHelmet#F3D255", "IronSword#F8FCFF", "KnightShield#F3D255", "", "Human", "HeavyKnightArmor#2E6FB7", "CavalrymanHelmet#F3D255", "Longsword#F8FCFF", "RoyalGreatShield#F3D255", "", new[] { "allied blue", "gold" }, new[] { "allied blue", "gold" }, false, ""),
            new("sword_soldier", "sword_captain", "검병", "검투대장", "sword/front melee", "Human", "BlueKnight#2E6FB7", "BlueKnightHelmet#F3D255", "IronSword#F8FCFF", "", "", "Human", "BlueKnight#2E6FB7", "IronKnightHelmet#F3D255", "Longsword#F8FCFF", "", "", new[] { "allied blue", "gold" }, new[] { "allied blue", "gold" }, false, ""),
            Definitions[2],
            new("falcon_archer", "falcon_captain", "매사냥꾼", "매사냥 대장", "bow/beast/rear", "Human", "ArcherTunic#2E6FB7", "ArcherHood#F3D255", "Bow#F8FCFF", "", "LeatherQuiver#D4E9F8", "Human", "ArcherTunic#2E6FB7", "CaptainHelmet#F3D255", "LongBow#F8FCFF", "", "LeatherQuiver#D4E9F8", new[] { "allied blue", "gold" }, new[] { "allied blue", "gold" }, false, "Bird is reviewed separately as a temporary stand-in, not an attached squad-slot prop."),
            new("field_herbalist", "battle_apothecary", "전투 약초사", "전장의 약제사", "heal/rear", "Human", "TravelerTunic#2E6FB7", "BanditBandana#7DCE8A", "", "", "SmallBackpack#D4E9F8", "Human", "PlagueDoctor#2E6FB7", "PlagueDoctor#7DCE8A", "", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "heal green" }, new[] { "allied blue", "heal green" }, true, "No native bottle, dart, or medicine-hand cue is present; the backpack is retained without a misleading substitute."),
            new("bombardier", "powder_captain", "폭탄병", "화약 대장", "explosive-support/rear", "Human", "BulletHeadArmor#2E6FB7", "BulletHeadHelm#F3D255", "", "", "SmallBackpack#D4E9F8", "Human", "RocketArmour#0C2E44", "RocketHelment#F3D255", "", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "explosive orange" }, new[] { "allied blue", "explosive orange" }, false, "Backpack/storage progression supplies the cue; no external bomb prop is required for this review."),
            Definitions[6],
            Definitions[7],
            new("wolf_tamer", "beast_commander", "늑대 조련사", "야수 지휘관", "beast owner/rear", "Human", "TravelerTunic#2E6FB7", "GuardianHelmet#F3D255", "HunterKnife#D4E9F8", "", "", "Human", "GuardianTunic#0C2E44", "CaptainHelmet#F3D255", "HunterKnife#D4E9F8", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "beast brown/teal" }, new[] { "allied blue", "beast brown/teal" }, false, "GreyWolf is reviewed separately as a child summon/support visual, not a squad-slot owner."),
            Definitions[9],
            Definitions[10],
            new("skeleton_bomber", "bone_artillery", "해골 폭탄병", "해골 포격수", "Skeleton/explosive-support/rear", "Skeleton", "MilitiamanArmor#42506E", "", "", "", "SmallBackpack#D4E9F8", "Skeleton", "BulletHeadArmor#42506E", "", "", "", "LargeBackpack#D4E9F8", new[] { "Skeleton", "explosive orange" }, new[] { "Skeleton", "explosive orange" }, false, "Backpack/storage progression supplies the cue; no external bomb or cannon prop is required for this review."),
        };

        private static readonly Definition[] V5Definitions =
        {
            new("shield_guard", "shield_captain", "방패병", "방패대장", "shield/sword/front tank", "Human", "IronKnight#2E6FB7", "IronKnightHelmet#F3D255", "IronSword#F8FCFF", "GuardianShield#F3D255", "", "Human", "IronKnight#2E6FB7", "CavalrymanHelmet#F3D255", "Longsword#F8FCFF", "RoyalGreatShield#F3D255", "", new[] { "allied blue", "steel", "gold" }, new[] { "allied blue", "steel", "gold" }, false, ""),
            V4Definitions[1],
            V4Definitions[2],
            V4Definitions[3],
            V4Definitions[4],
            V4Definitions[5],
            V4Definitions[6],
            V4Definitions[7],
            new("wolf_tamer", "beast_commander", "늑대 조련사", "야수 지휘관", "beast owner/rear", "Human", "GuardianTunic#2E6FB7", "", "HunterKnife#D4E9F8", "", "", "Human", "GuardianTunic#0C2E44", "", "HunterKnife#D4E9F8", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "beast brown/teal" }, new[] { "allied blue", "beast brown/teal" }, false, "GreyWolf is reviewed separately as a child summon/support visual, not a squad-slot owner."),
            V4Definitions[9],
            V4Definitions[10],
            new("skeleton_bomber", "bone_artillery", "해골 폭탄병", "해골 포격수", "Skeleton/explosive-support/rear", "Skeleton", "MilitiamanArmor#42506E", "", "", "", "SmallBackpack#D4E9F8", "Skeleton", "HeavyKnightArmor#42506E", "", "", "", "LargeBackpack#D4E9F8", new[] { "Skeleton", "explosive orange" }, new[] { "Skeleton", "explosive orange" }, false, "Backpack/storage progression supplies the cue; no external bomb or cannon prop is required for this review."),
        };

        private readonly struct CandidateDefinition
        {
            public readonly string OptionId;
            public readonly string Family;
            public readonly string DirectionEvidence;
            public readonly string ReviewFrame;
            public readonly Definition Pair;

            public CandidateDefinition(string optionId, string family, string directionEvidence, string reviewFrame, Definition pair)
            {
                OptionId = optionId;
                Family = family;
                DirectionEvidence = directionEvidence;
                ReviewFrame = reviewFrame;
                Pair = pair;
            }
        }

        private static readonly CandidateDefinition[] CandidateDefinitions =
        {
            new("shield_A", "shield", "same Idle frame; shield-side silhouette inspected", "Idle_0", new Definition("shield_guard", "shield_captain", "방패병", "방패대장", "shield/sword/front tank", "Human", "IronKnight#2E6FB7", "IronKnightHelmet#F3D255", "IronSword#F8FCFF", "TowerShield#F3D255", "", "Human", "IronKnight#2E6FB7", "CavalrymanHelmet#F3D255", "Longsword#F8FCFF", "AncientGreatShield#F3D255", "", new[] { "allied blue", "steel", "gold" }, new[] { "allied blue", "steel", "gold" }, false, "")),
            new("shield_B", "shield", "same Idle frame; broad shield-side silhouette inspected", "Idle_0", new Definition("shield_guard", "shield_captain", "방패병", "방패대장", "shield/sword/front tank", "Human", "HeavyKnightArmor#2E6FB7", "HeavyKnightHelmet#F3D255", "Longsword#F8FCFF", "KnightShield#F3D255", "", "Human", "HeavyKnightArmor#2E6FB7", "CavalrymanHelmet#F3D255", "Longsword#F8FCFF", "RoyalGreatShield#F3D255", "", new[] { "allied blue", "steel", "gold" }, new[] { "allied blue", "steel", "gold" }, false, "")),
            new("shield_C", "shield", "same Idle frame; ornate shield-side silhouette inspected", "Idle_0", new Definition("shield_guard", "shield_captain", "방패병", "방패대장", "shield/sword/front tank", "Human", "CrossKnight#2E6FB7", "CrossKnight#F3D255", "IronSword#F8FCFF", "CrusaderShield#F3D255", "", "Human", "CavalrymanArmor#2E6FB7", "CavalrymanHelmet#F3D255", "Longsword#F8FCFF", "AncientGreatShield#F3D255", "", new[] { "allied blue", "steel", "gold" }, new[] { "allied blue", "steel", "gold" }, false, "")),
            new("wolf_A", "wolf", "matched Human head/hair and identical Idle frame", "Idle_0", new Definition("wolf_tamer", "beast_commander", "늑대 조련사", "야수 지휘관", "beast owner/rear", "Human", "GuardianTunic#2E6FB7", "", "HunterKnife#D4E9F8", "", "", "Human", "GuardianTunic#0C2E44", "", "HunterKnife#D4E9F8", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "beast brown/teal" }, new[] { "allied blue", "beast brown/teal" }, false, "")),
            new("wolf_B", "wolf", "matched Human head/hair and identical Idle frame", "Idle_0", new Definition("wolf_tamer", "beast_commander", "늑대 조련사", "야수 지휘관", "beast owner/rear", "Human", "CavalrymanArmor#2E6FB7", "", "HunterKnife#D4E9F8", "", "", "Human", "CaptainArmor#0C2E44", "", "HunterKnife#D4E9F8", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "beast brown/teal" }, new[] { "allied blue", "beast brown/teal" }, false, "")),
            new("wolf_C", "wolf", "matched Human head/hair and identical Idle frame", "Idle_0", new Definition("wolf_tamer", "beast_commander", "늑대 조련사", "야수 지휘관", "beast owner/rear", "Human", "BlueKnight#2E6FB7", "", "HunterKnife#D4E9F8", "", "", "Human", "IronKnight#0C2E44", "", "HunterKnife#D4E9F8", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "beast brown/teal" }, new[] { "allied blue", "beast brown/teal" }, false, "")),
            new("skeleton_A", "skeleton", "same exposed skull frame inspected", "Idle_0", new Definition("skeleton_bomber", "bone_artillery", "해골 폭탄병", "해골 포격수", "Skeleton/explosive-support/rear", "Skeleton", "MilitiamanArmor#42506E", "", "", "", "SmallBackpack#D4E9F8", "Skeleton", "HeavyKnightArmor#42506E", "", "", "", "LargeBackpack#D4E9F8", new[] { "Skeleton", "explosive orange" }, new[] { "Skeleton", "explosive orange" }, false, "")),
            new("skeleton_B", "skeleton", "same exposed skull frame inspected", "Idle_0", new Definition("skeleton_bomber", "bone_artillery", "해골 폭탄병", "해골 포격수", "Skeleton/explosive-support/rear", "Skeleton", "TravelerTunic#42506E", "", "", "", "SmallBackpack#D4E9F8", "Skeleton", "BulletHeadArmor#42506E", "", "", "", "LargeBackpack#D4E9F8", new[] { "Skeleton", "explosive orange" }, new[] { "Skeleton", "explosive orange" }, false, "")),
            new("skeleton_C", "skeleton", "same exposed skull frame inspected", "Idle_0", new Definition("skeleton_bomber", "bone_artillery", "해골 폭탄병", "해골 포격수", "Skeleton/explosive-support/rear", "Skeleton", "MusketeerTunic#42506E", "", "", "", "SmallBackpack#D4E9F8", "Skeleton", "RocketArmour#42506E", "", "", "", "LargeBackpack#D4E9F8", new[] { "Skeleton", "explosive orange" }, new[] { "Skeleton", "explosive orange" }, false, "")),
        };

        [MenuItem("Lizzo/Art/Generate Companion Art V2 Contact Sheet")]
        public static void GenerateFromMenu() => Generate();

        public static CompanionArtManifest Generate()
        {
            var collection = AssetDatabase.LoadAssetAtPath<SpriteCollection>(SpriteCollectionPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!collection) throw new InvalidOperationException("SpriteCollection asset is missing: " + SpriteCollectionPath);
            if (!sourceFont) throw new InvalidOperationException("Korean-capable Pretendard source font is missing: " + FontPath);
            var font = CreateReviewFont(sourceFont);
            ValidateDefinitions(collection);
            Directory.CreateDirectory(OutputDirectory);

            var manifest = new CompanionArtManifest
            {
                version = "V2",
                sourcePrefab = PrefabPath,
                sourceSpriteCollection = SpriteCollectionPath,
                sourceFont = FontPath,
                frame = "Idle_0",
                previewWidth = PreviewSize,
                previewHeight = PreviewSize,
                entries = new CompanionArtManifestEntry[Definitions.Length]
            };

            var sheet = CreateSheet(font);
            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                for (var i = 0; i < Definitions.Length; i++)
                {
                    var definition = Definitions[i];
                    Apply(builder, definition, false);
                    builder.Rebuild();
                    var baseFrame = CaptureIdleFrame(builder.Texture);
                    Apply(builder, definition, true);
                    builder.Rebuild();
                    var promotionFrame = CaptureIdleFrame(builder.Texture);
                    var scale = ChooseScale(baseFrame, promotionFrame);
                    var entry = CreateEntry(definition, scale, baseFrame, promotionFrame);
                    manifest.entries[i] = entry;
                    WritePreview(baseFrame, entry.basePreview, sheet, i, BaseX, font, definition.BaseKoreanLabel, definition.BaseId, scale, definition.ExternalPropRequired);
                    WritePreview(promotionFrame, entry.promotionPreview, sheet, i, PromotionX, font, definition.PromotionKoreanLabel, definition.PromotionId, scale, definition.ExternalPropRequired);
                }
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
                NormalizeSheetBackground(sheet);
                sheet.Apply(false, false);
                File.WriteAllBytes(ContactSheetAssetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(font);
            }

            File.WriteAllText(ManifestAssetPath, JsonUtility.ToJson(manifest, true));
            return manifest;
        }

        [MenuItem("Lizzo/Art/Generate Companion Art V3 Contact Sheet")]
        public static void GenerateV3FromMenu() => GenerateV3ContactSheet();

        public static string GenerateV3ContactSheet()
        {
            var manifest = JsonUtility.FromJson<CompanionArtManifest>(File.ReadAllText(ManifestAssetPath));
            if (manifest == null || manifest.entries == null || manifest.entries.Length != Definitions.Length)
            {
                throw new InvalidOperationException("V2 manifest is missing or incomplete; V3 must not invent art selections.");
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!sourceFont) throw new InvalidOperationException("Korean-capable Pretendard source font is missing: " + FontPath);
            Directory.CreateDirectory(V3OutputDirectory);
            var font = CreateReviewFont(sourceFont);
            var sheet = CreateV3Sheet(font);
            try
            {
                for (var row = 0; row < manifest.entries.Length; row++)
                {
                    var entry = manifest.entries[row];
                    var cellTop = V3HeaderHeight + row * V3RowHeight;
                    WriteV3Cell(sheet, font, entry.basePreview, BaseX, cellTop, entry.baseKoreanLabel, entry.baseId, entry.externalPropRequired);
                    WriteV3Cell(sheet, font, entry.promotionPreview, PromotionX, cellTop, entry.promotionKoreanLabel, entry.promotionId, entry.externalPropRequired);
                }
            }
            finally
            {
                NormalizeV3SheetBackground(sheet);
                sheet.Apply(false, false);
                File.WriteAllBytes(V3ContactSheetAssetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(font);
            }

            return V3ContactSheetAssetPath;
        }

        [MenuItem("Lizzo/Art/Generate Companion Art V4 Contact Sheet")]
        public static void GenerateV4FromMenu() => GenerateV4ContactSheet();

        public static string GenerateV4ContactSheet()
        {
            var collection = AssetDatabase.LoadAssetAtPath<SpriteCollection>(SpriteCollectionPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!collection) throw new InvalidOperationException("SpriteCollection asset is missing: " + SpriteCollectionPath);
            if (!sourceFont) throw new InvalidOperationException("Korean-capable Pretendard source font is missing: " + FontPath);
            ValidateDefinitions(collection, V4Definitions, 12);
            Directory.CreateDirectory(V4OutputDirectory);

            var font = CreateReviewFont(sourceFont);
            var sheet = CreateV4Sheet(font);
            var manifest = new CompanionArtV4Manifest
            {
                version = "V4",
                sourcePrefab = PrefabPath,
                sourceSpriteCollection = SpriteCollectionPath,
                sourceFont = FontPath,
                frame = "Idle_0",
                previewWidth = PreviewSize,
                previewHeight = PreviewSize,
                pairs = new CompanionArtV4PairEntry[V4Definitions.Length],
                support = new CompanionArtV4SupportEntry[3],
            };

            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                for (var i = 0; i < V4Definitions.Length; i++)
                {
                    var definition = V4Definitions[i];
                    Apply(builder, definition, false);
                    builder.Rebuild();
                    var baseFrame = CaptureIdleFrame(builder.Texture);
                    Apply(builder, definition, true);
                    builder.Rebuild();
                    var promotionFrame = CaptureIdleFrame(builder.Texture);
                    var scale = ChooseScale(baseFrame, promotionFrame);
                    manifest.pairs[i] = CreateV4PairEntry(definition, scale, baseFrame, promotionFrame);
                    WriteFramePreview(baseFrame, manifest.pairs[i].basePreview, scale);
                    WriteFramePreview(promotionFrame, manifest.pairs[i].promotionPreview, scale);
                    WriteV4PairCell(sheet, font, manifest.pairs[i], BaseX, V4HeaderHeight + i * V4RowHeight, false);
                    WriteV4PairCell(sheet, font, manifest.pairs[i], PromotionX, V4HeaderHeight + i * V4RowHeight, true);
                }

                var birdPreview = CreatePrefabSupportPreview("Assets/PixelFantasy/PixelMonsters/Pack3/Bird/Bird.prefab");
                manifest.support[0] = CreateSupportEntry("bird_temporary_stand_in", "매 임시 대역 (Bird/앵무새형)", "Assets/PixelFantasy/PixelMonsters/Pack3/Bird/Bird.prefab", "Assets/PixelFantasy/PixelMonsters/Pack3/Bird/Bird.png", "Bird prefab / Bird.png", birdPreview, new[] { "vendor Bird prefab", "parrot-shaped temporary stand-in" }, "Not a final falcon claim; replace with a verified falcon asset at the Human Gate.");
                WriteSupportPreview(birdPreview, manifest.support[0].preview);
                WriteV4SupportCell(sheet, font, manifest.support[0], 220, V4SupportTop);
                UnityEngine.Object.DestroyImmediate(birdPreview);

                var wolfPreview = CreatePrefabSupportPreview("Assets/PixelFantasy/PixelMonsters/Pack2/Wolf/GreyWolf.prefab");
                manifest.support[1] = CreateSupportEntry("grey_wolf_support", "늑대 소환/지원", "Assets/PixelFantasy/PixelMonsters/Pack2/Wolf/GreyWolf.prefab", "Assets/PixelFantasy/PixelMonsters/Pack2/Wolf/GreyWolf.png", "GreyWolf prefab / GreyWolf.png", wolfPreview, new[] { "vendor GreyWolf prefab", "child summon/support visual" }, "Wolf remains a child summon/support visual, not a squad-slot owner.");
                WriteSupportPreview(wolfPreview, manifest.support[1].preview);
                WriteV4SupportCell(sheet, font, manifest.support[1], 690, V4SupportTop);
                UnityEngine.Object.DestroyImmediate(wolfPreview);

                var summonDefinition = new Definition("necromancer_skeleton_summon", "necromancer_skeleton_summon", "", "", "summoned skeleton/support", "Skeleton", "", "", "", "", "", "Skeleton", "", "", "", "", "", new[] { "pale cool bones" }, new[] { "pale cool bones" }, false, "");
                Apply(builder, summonDefinition, false);
                builder.Rebuild();
                var summonFrame = CaptureIdleFrame(builder.Texture);
                var summonPreview = CreatePreview(summonFrame, ChooseScale(summonFrame, summonFrame));
                manifest.support[2] = CreateSupportEntry("necromancer_skeleton_summon", "사령술사 소환 해골", "", "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Bonus/Character/Skeleton/SpriteSheet.png", "native Skeleton Body/Head/Arms/Eyes composition", summonPreview, new[] { "Skeleton body", "native skull/head/eyes", "minimal equipment" }, "Distinct from the allied Skeleton companion family by its pale bones and little/no equipment.");
                WriteSupportPreview(summonPreview, manifest.support[2].preview);
                WriteV4SupportCell(sheet, font, manifest.support[2], 1160, V4SupportTop);
                UnityEngine.Object.DestroyImmediate(summonPreview);
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
                NormalizeV4SheetBackground(sheet);
                sheet.Apply(false, false);
                File.WriteAllBytes(V4ContactSheetAssetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(font);
            }

            File.WriteAllText(V4ManifestAssetPath, JsonUtility.ToJson(manifest, true));
            return V4ContactSheetAssetPath;
        }

        [MenuItem("Lizzo/Art/Generate Companion Art V5 Contact Sheet")]
        public static string GenerateV5ContactSheet()
        {
            var collection = AssetDatabase.LoadAssetAtPath<SpriteCollection>(SpriteCollectionPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!collection) throw new InvalidOperationException("SpriteCollection asset is missing: " + SpriteCollectionPath);
            if (!sourceFont) throw new InvalidOperationException("Korean-capable Pretendard source font is missing: " + FontPath);
            ValidateDefinitions(collection, V5Definitions, 12);
            var v4Manifest = JsonUtility.FromJson<CompanionArtV4Manifest>(File.ReadAllText(V4ManifestAssetPath));
            if (v4Manifest == null || v4Manifest.pairs == null || v4Manifest.pairs.Length != 12 || v4Manifest.support == null || v4Manifest.support.Length != 3)
            {
                throw new InvalidOperationException("V4 manifest is not complete enough to preserve for V5.");
            }

            Directory.CreateDirectory(V5OutputDirectory);
            var font = CreateReviewFont(sourceFont);
            var sheet = CreateV5Sheet(font);
            var manifest = new CompanionArtV4Manifest
            {
                version = "V5", sourcePrefab = PrefabPath, sourceSpriteCollection = SpriteCollectionPath,
                sourceFont = FontPath, frame = "Idle_0", previewWidth = PreviewSize, previewHeight = PreviewSize,
                pairs = new CompanionArtV4PairEntry[V5Definitions.Length], support = new CompanionArtV4SupportEntry[v4Manifest.support.Length],
            };

            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                for (var i = 0; i < V5Definitions.Length; i++)
                {
                    var v4Pair = v4Manifest.pairs[i];
                    if (i != 0 && i != 8 && i != 11)
                    {
                        manifest.pairs[i] = CloneV4PairForV5(v4Pair);
                        File.Copy(v4Pair.basePreview, manifest.pairs[i].basePreview, true);
                        File.Copy(v4Pair.promotionPreview, manifest.pairs[i].promotionPreview, true);
                    }
                    else
                    {
                        var definition = V5Definitions[i];
                        ApplyV5(builder, definition, false);
                        builder.Rebuild();
                        var baseFrame = CaptureIdleFrame(builder.Texture);
                        ApplyV5(builder, definition, true);
                        builder.Rebuild();
                        var promotionFrame = CaptureIdleFrame(builder.Texture);
                        var scale = ChooseScale(baseFrame, promotionFrame);
                        manifest.pairs[i] = CreateV5PairEntry(definition, scale, baseFrame, promotionFrame);
                        WriteFramePreview(baseFrame, manifest.pairs[i].basePreview, scale);
                        WriteFramePreview(promotionFrame, manifest.pairs[i].promotionPreview, scale);
                        ApplyMetrics(manifest.pairs[i], GetPreviewMetrics(manifest.pairs[i].basePreview), GetPreviewMetrics(manifest.pairs[i].promotionPreview));
                    }
                    WriteV5PairCell(sheet, font, manifest.pairs[i], BaseX, V4HeaderHeight + i * V4RowHeight, false);
                    WriteV5PairCell(sheet, font, manifest.pairs[i], PromotionX, V4HeaderHeight + i * V4RowHeight, true);
                }

                for (var i = 0; i < v4Manifest.support.Length; i++)
                {
                    manifest.support[i] = CloneV4SupportForV5(v4Manifest.support[i]);
                    File.Copy(v4Manifest.support[i].preview, manifest.support[i].preview, true);
                }
                WriteV5SupportCell(sheet, font, manifest.support[0], 220, V4SupportTop);
                WriteV5SupportCell(sheet, font, manifest.support[1], 690, V4SupportTop);
                WriteV5SupportCell(sheet, font, manifest.support[2], 1160, V4SupportTop);
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
                NormalizeV5SheetBackground(sheet);
                sheet.Apply(false, false);
                File.WriteAllBytes(V5ContactSheetAssetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(font);
            }

            File.WriteAllText(V5ManifestAssetPath, JsonUtility.ToJson(manifest, true));
            return V5ContactSheetAssetPath;
        }

        [MenuItem("Lizzo/Art/Generate Companion Art V6 Contact Sheet")]
        public static string GenerateV6ContactSheet()
        {
            var collection = AssetDatabase.LoadAssetAtPath<SpriteCollection>(SpriteCollectionPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!collection) throw new InvalidOperationException("SpriteCollection asset is missing: " + SpriteCollectionPath);
            if (!sourceFont) throw new InvalidOperationException("Korean-capable Pretendard source font is missing: " + FontPath);

            var v5Manifest = JsonUtility.FromJson<CompanionArtV4Manifest>(File.ReadAllText(V5ManifestAssetPath));
            var candidateManifest = JsonUtility.FromJson<CompanionArtThreeFamilyCandidateManifest>(File.ReadAllText(CandidatesThreeFamiliesManifestAssetPath));
            if (v5Manifest == null || v5Manifest.pairs == null || v5Manifest.pairs.Length != 12 || v5Manifest.support == null || v5Manifest.support.Length != 3)
            {
                throw new InvalidOperationException("V5 manifest is not complete enough to preserve for V6.");
            }
            if (candidateManifest == null || candidateManifest.pairs == null || candidateManifest.pairs.Length != 9)
            {
                throw new InvalidOperationException("Three-family candidate manifest is not complete enough for V6 selections.");
            }

            var selectedCandidates = new Dictionary<string, CompanionArtThreeFamilyCandidateEntry>
            {
                ["shield_guard"] = candidateManifest.pairs.Single(i => i.optionId == "shield_B"),
                ["wolf_tamer"] = candidateManifest.pairs.Single(i => i.optionId == "wolf_B"),
                ["skeleton_bomber"] = candidateManifest.pairs.Single(i => i.optionId == "skeleton_C"),
            };
            foreach (var selected in selectedCandidates.Values)
            {
                if (!File.Exists(selected.basePreview) || !File.Exists(selected.promotionPreview))
                {
                    throw new InvalidOperationException("Selected candidate preview is missing: " + selected.optionId);
                }
                var source = v5Manifest.pairs.Single(i => i.baseId == selected.baseId);
                ValidateDefinitions(collection, new[] { DefinitionFromCandidate(source, selected) }, 1);
            }

            Directory.CreateDirectory(V6OutputDirectory);
            var font = CreateReviewFont(sourceFont);
            var sheet = CreateV6Sheet(font);
            var manifest = new CompanionArtV6Manifest
            {
                version = "V6",
                sourcePrefab = PrefabPath,
                sourceSpriteCollection = SpriteCollectionPath,
                sourceFont = FontPath,
                frame = "Idle_0",
                previewWidth = PreviewSize,
                previewHeight = PreviewSize,
                selectionProvenance = new[]
                {
                    "shield_guard/shield_captain=shield_B",
                    "wolf_tamer/beast_commander=wolf_B",
                    "skeleton_bomber/bone_artillery=skeleton_C",
                },
                pairs = new CompanionArtV4PairEntry[v5Manifest.pairs.Length],
                support = new CompanionArtV4SupportEntry[v5Manifest.support.Length],
            };

            try
            {
                for (var i = 0; i < v5Manifest.pairs.Length; i++)
                {
                    var source = v5Manifest.pairs[i];
                    if (selectedCandidates.TryGetValue(source.baseId, out var selected))
                    {
                        manifest.pairs[i] = CreateV6SelectedPairEntry(source, selected);
                        File.Copy(selected.basePreview, manifest.pairs[i].basePreview, true);
                        File.Copy(selected.promotionPreview, manifest.pairs[i].promotionPreview, true);
                    }
                    else
                    {
                        manifest.pairs[i] = CloneV4PairForV6(source);
                        File.Copy(source.basePreview, manifest.pairs[i].basePreview, true);
                        File.Copy(source.promotionPreview, manifest.pairs[i].promotionPreview, true);
                    }
                    WriteV5PairCell(sheet, font, manifest.pairs[i], BaseX, V4HeaderHeight + i * V4RowHeight, false);
                    WriteV5PairCell(sheet, font, manifest.pairs[i], PromotionX, V4HeaderHeight + i * V4RowHeight, true);
                }

                for (var i = 0; i < v5Manifest.support.Length; i++)
                {
                    manifest.support[i] = CloneV4SupportForV6(v5Manifest.support[i]);
                    File.Copy(v5Manifest.support[i].preview, manifest.support[i].preview, true);
                }
                WriteV5SupportCell(sheet, font, manifest.support[0], 220, V6SupportTop);
                WriteV5SupportCell(sheet, font, manifest.support[1], 690, V6SupportTop);
                WriteV5SupportCell(sheet, font, manifest.support[2], 1160, V6SupportTop);
            }
            finally
            {
                NormalizeV5SheetBackground(sheet);
                sheet.Apply(false, false);
                File.WriteAllBytes(V6ContactSheetAssetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(font);
            }

            File.WriteAllText(V6ManifestAssetPath, JsonUtility.ToJson(manifest, true));
            return V6ContactSheetAssetPath;
        }

        [MenuItem("Lizzo/Art/Generate Three-Family Companion Candidates")]
        public static string GenerateThreeFamilyCandidates()
        {
            var collection = AssetDatabase.LoadAssetAtPath<SpriteCollection>(SpriteCollectionPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!collection) throw new InvalidOperationException("SpriteCollection asset is missing: " + SpriteCollectionPath);
            if (!sourceFont) throw new InvalidOperationException("Korean-capable Pretendard source font is missing: " + FontPath);
            ValidateCandidateDefinitions(collection);
            Directory.CreateDirectory(CandidatesThreeFamiliesOutputDirectory);

            var availableFrames = CharacterBuilder.Layout.Keys.Where(i => i.StartsWith("Idle_", StringComparison.Ordinal)).OrderBy(i => i).ToArray();
            var renderableFrames = new[] { "Idle_0" };
            var font = CreateReviewFont(sourceFont);
            var sheet = CreateCandidateSheet(font);
            var manifest = new CompanionArtThreeFamilyCandidateManifest
            {
                version = "Candidates_ThreeFamilies_V1",
                sourcePrefab = PrefabPath,
                sourceSpriteCollection = SpriteCollectionPath,
                sourceFont = FontPath,
                availableIdleFrames = availableFrames,
                renderableIdleFrames = renderableFrames,
                previewWidth = PreviewSize,
                previewHeight = PreviewSize,
                pairs = new CompanionArtThreeFamilyCandidateEntry[CandidateDefinitions.Length],
            };

            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                for (var i = 0; i < CandidateDefinitions.Length; i++)
                {
                    var candidate = CandidateDefinitions[i];
                    ApplyCandidate(builder, candidate.Pair, candidate.Family, false);
                    builder.Rebuild();
                    var baseFrame = CaptureFrameByKey(builder.Texture, candidate.ReviewFrame);
                    ApplyCandidate(builder, candidate.Pair, candidate.Family, true);
                    builder.Rebuild();
                    var promotionFrame = CaptureFrameByKey(builder.Texture, candidate.ReviewFrame);
                    var scale = ChooseScale(baseFrame, promotionFrame);
                    var entry = CreateCandidateEntry(candidate, scale, baseFrame, promotionFrame);
                    WriteFramePreview(baseFrame, entry.basePreview, scale);
                    WriteFramePreview(promotionFrame, entry.promotionPreview, scale);
                    ApplyCandidateMetrics(entry, GetCandidatePreviewMetrics(entry.basePreview), GetCandidatePreviewMetrics(entry.promotionPreview));
                    manifest.pairs[i] = entry;
                    WriteCandidateCell(sheet, font, entry, BaseX, CandidateHeaderHeight + i * CandidateRowHeight, false);
                    WriteCandidateCell(sheet, font, entry, PromotionX, CandidateHeaderHeight + i * CandidateRowHeight, true);
                }
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
                NormalizeCandidateSheetBackground(sheet);
                sheet.Apply(false, false);
                File.WriteAllBytes(CandidatesThreeFamiliesContactSheetAssetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(font);
            }

            File.WriteAllText(CandidatesThreeFamiliesManifestAssetPath, JsonUtility.ToJson(manifest, true));
            return CandidatesThreeFamiliesContactSheetAssetPath;
        }

        [MenuItem("Lizzo/Art/Export V6 Companion Sprite Sheets")]
        public static string ExportV6FullSpriteSheets()
        {
            var sourceManifest = JsonUtility.FromJson<CompanionArtV6Manifest>(File.ReadAllText(V6ManifestAssetPath));
            if (sourceManifest == null || sourceManifest.pairs == null || sourceManifest.pairs.Length != 12 || sourceManifest.support == null || sourceManifest.support.Length != 3)
            {
                throw new InvalidOperationException("V6 manifest must contain exactly 12 pairs and 3 support entries.");
            }

            var expectedSelections = new[]
            {
                "shield_guard/shield_captain=shield_B",
                "wolf_tamer/beast_commander=wolf_B",
                "skeleton_bomber/bone_artillery=skeleton_C",
            };
            if (sourceManifest.selectionProvenance == null || !expectedSelections.SequenceEqual(sourceManifest.selectionProvenance))
            {
                throw new InvalidOperationException("V6 selection provenance does not match the approved shield_B/wolf_B/skeleton_C selections.");
            }

            Directory.CreateDirectory(CompanionSheetOutputDirectory);
            Directory.CreateDirectory(SummonSheetOutputDirectory);
            var companionEntries = new List<CompanionFullSpriteSheetEntry>(24);
            var summonSheet = SummonSheetOutputDirectory + "/necromancer_skeleton_summon_SpriteSheet.png";
            var summonLibrary = SummonSheetOutputDirectory + "/necromancer_skeleton_summon_SpriteLibrary.asset";
            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                foreach (var pair in sourceManifest.pairs)
                {
                    var definition = DefinitionFromPair(pair);
                    var baseSheet = CompanionSheetOutputDirectory + "/" + pair.baseId + "_SpriteSheet.png";
                    var baseLibrary = CompanionSheetOutputDirectory + "/" + pair.baseId + "_SpriteLibrary.asset";
                    ExportGeneratedCharacterSheet(builder, definition, false, baseSheet, baseLibrary);
                    companionEntries.Add(CreateCompanionSheetEntry(pair, definition, false, baseSheet, baseLibrary));

                    var promotionSheet = CompanionSheetOutputDirectory + "/" + pair.promotionId + "_SpriteSheet.png";
                    var promotionLibrary = CompanionSheetOutputDirectory + "/" + pair.promotionId + "_SpriteLibrary.asset";
                    ExportGeneratedCharacterSheet(builder, definition, true, promotionSheet, promotionLibrary);
                    companionEntries.Add(CreateCompanionSheetEntry(pair, definition, true, promotionSheet, promotionLibrary));
                }

                var summonDefinition = new Definition(
                    "necromancer_skeleton_summon", "necromancer_skeleton_summon", "", "", "summoned skeleton/support",
                    "Skeleton", "", "", "", "", "", "Skeleton", "", "", "", "", "",
                    new[] { "pale cool bones" }, new[] { "pale cool bones" }, false, "");
                ExportGeneratedCharacterSheet(builder, summonDefinition, false, summonSheet, summonLibrary);
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            var summonEntries = new List<SummonSpriteSheetEntry>(3);
            foreach (var support in sourceManifest.support)
            {
                var destinationSheet = SummonSheetOutputDirectory + "/" + support.id + "_SpriteSheet.png";
                var destinationLibrary = SummonSheetOutputDirectory + "/" + support.id + "_SpriteLibrary.asset";
                if (support.id == "necromancer_skeleton_summon")
                {
                    summonEntries.Add(new SummonSpriteSheetEntry
                    {
                        id = support.id, label = support.label, sourcePrefab = support.sourcePrefab, sourceArt = support.sourceArt,
                        sourceLabel = support.sourceLabel, limitation = support.limitation, sheetPath = summonSheet,
                        libraryPath = summonLibrary, width = 576, height = 928, categoryCount = 14, labelCount = 126,
                    });
                    continue;
                }

                var sourceLibrary = Path.ChangeExtension(support.sourceArt, ".asset").Replace('\\', '/');
                CopyVendorSheetAndCreateLibrary(support.sourceArt, sourceLibrary, destinationSheet, destinationLibrary);
                var counts = GetSpriteLibraryCounts(destinationLibrary);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(destinationSheet);
                if (!texture) throw new InvalidOperationException("Copied summon sheet failed to import: " + destinationSheet);
                summonEntries.Add(new SummonSpriteSheetEntry
                {
                    id = support.id, label = support.label, sourcePrefab = support.sourcePrefab, sourceArt = support.sourceArt,
                    sourceLabel = support.sourceLabel, limitation = support.limitation, sheetPath = destinationSheet,
                    libraryPath = destinationLibrary, width = texture.width, height = texture.height,
                    categoryCount = counts.categoryCount, labelCount = counts.labelCount,
                });
            }

            File.WriteAllText(CompanionSheetManifestAssetPath, JsonUtility.ToJson(new CompanionFullSpriteSheetManifest
            {
                exportVersion = "V6_FullSpriteSheets_V1",
                sourceManifest = V6ManifestAssetPath,
                sourceProvenance = string.Join(";", sourceManifest.selectionProvenance),
                sheetWidth = 576, sheetHeight = 928, categoryCount = 14, labelCount = 126,
                entries = companionEntries.ToArray(),
            }, true));
            File.WriteAllText(SummonSheetManifestAssetPath, JsonUtility.ToJson(new SummonSpriteSheetManifest
            {
                exportVersion = "V6_FullSpriteSheets_V1",
                sourceManifest = V6ManifestAssetPath,
                sheetWidth = 576, sheetHeight = 928,
                entries = summonEntries.ToArray(),
            }, true));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return CompanionSheetManifestAssetPath;
        }

        [MenuItem("Lizzo/Art/Export V6 Compact Companion Motion Sheets")]
        public static string ExportV6CompactCompanionSpriteSheets()
        {
            var sourceManifest = JsonUtility.FromJson<CompanionArtV6Manifest>(File.ReadAllText(V6ManifestAssetPath));
            if (sourceManifest == null || sourceManifest.pairs == null || sourceManifest.pairs.Length != 12)
            {
                throw new InvalidOperationException("V6 manifest must contain exactly 12 companion pairs.");
            }

            var expectedSelections = new[]
            {
                "shield_guard/shield_captain=shield_B",
                "wolf_tamer/beast_commander=wolf_B",
                "skeleton_bomber/bone_artillery=skeleton_C",
            };
            if (sourceManifest.selectionProvenance == null || !expectedSelections.SequenceEqual(sourceManifest.selectionProvenance))
            {
                throw new InvalidOperationException("V6 selection provenance does not match the approved shield_B/wolf_B/skeleton_C selections.");
            }

            Directory.CreateDirectory(CompanionSheetOutputDirectory);
            var entries = new List<CompanionFullSpriteSheetEntry>(24);
            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                foreach (var pair in sourceManifest.pairs)
                {
                    var definition = DefinitionFromPair(pair);
                    var attackMotion = GetV6AttackMotion(pair.baseId);
                    var baseSheet = CompanionSheetOutputDirectory + "/" + pair.baseId + "_SpriteSheet.png";
                    var baseLibrary = CompanionSheetOutputDirectory + "/" + pair.baseId + "_SpriteLibrary.asset";
                    var baseResult = ExportCompactCharacterSheet(builder, definition, false, attackMotion, baseSheet, baseLibrary);
                    entries.Add(CreateCompactCompanionSheetEntry(pair, definition, false, attackMotion, baseSheet, baseLibrary, baseResult));

                    var promotionSheet = CompanionSheetOutputDirectory + "/" + pair.promotionId + "_SpriteSheet.png";
                    var promotionLibrary = CompanionSheetOutputDirectory + "/" + pair.promotionId + "_SpriteLibrary.asset";
                    var promotionResult = ExportCompactCharacterSheet(builder, definition, true, attackMotion, promotionSheet, promotionLibrary);
                    entries.Add(CreateCompactCompanionSheetEntry(pair, definition, true, attackMotion, promotionSheet, promotionLibrary, promotionResult));
                }
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            File.WriteAllText(CompanionSheetManifestAssetPath, JsonUtility.ToJson(new CompanionFullSpriteSheetManifest
            {
                exportVersion = CompactCompanionExportVersion,
                sourceManifest = V6ManifestAssetPath,
                sourceProvenance = string.Join(";", sourceManifest.selectionProvenance),
                rowOrder = new[] { "Idle", "Run", "Attack", "Death" },
                sheetWidth = 576,
                sheetHeight = 256,
                categoryCount = 4,
                labelCount = 36,
                entries = entries.ToArray(),
            }, true));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return CompanionSheetManifestAssetPath;
        }

        private readonly struct CompactSheetResult
        {
            public readonly string[] EmptySourceSlots;

            public CompactSheetResult(string[] emptySourceSlots)
            {
                EmptySourceSlots = emptySourceSlots;
            }
        }

        private static string GetV6AttackMotion(string baseId)
        {
            return baseId switch
            {
                "shield_guard" => "Push",
                "sword_soldier" => "Slash",
                "cleric" => "Shot",
                "falcon_archer" => "Shot",
                "field_herbalist" => "Shot",
                "bombardier" => "Shot",
                "fire_mage" => "Shot",
                "lightning_mage" => "Shot",
                "wolf_tamer" => "Slash",
                "wraith_knight" => "Slash",
                "necromancer" => "Shot",
                "skeleton_bomber" => "Shot",
                _ => throw new InvalidOperationException("No compact attack motion mapping exists for " + baseId),
            };
        }

        private static CompanionFullSpriteSheetEntry CreateCompactCompanionSheetEntry(CompanionArtV4PairEntry pair, Definition definition, bool promotion, string attackMotion, string sheetPath, string libraryPath, CompactSheetResult result)
        {
            var entry = CreateCompanionSheetEntry(pair, definition, promotion, sheetPath, libraryPath);
            entry.sourceRows = new[] { "Idle", "Run", attackMotion, "Death" };
            entry.sourceAttackMotion = attackMotion;
            entry.emptySourceSlots = result.EmptySourceSlots;
            entry.width = 576;
            entry.height = 256;
            entry.categoryCount = 4;
            entry.labelCount = 36;
            return entry;
        }

        private static CompactSheetResult ExportCompactCharacterSheet(CharacterBuilder builder, Definition definition, bool promotion, string attackMotion, string sheetPath, string libraryPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(libraryPath)) AssetDatabase.DeleteAsset(libraryPath);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sheetPath)) AssetDatabase.DeleteAsset(sheetPath);
            if (File.Exists(sheetPath)) File.Delete(sheetPath);

            ApplyV5(builder, definition, promotion);
            builder.Rebuild(forceMerge: true);
            if (!builder.Texture || builder.Texture.width != 576 || builder.Texture.height != 928)
            {
                throw new InvalidOperationException("CharacterBuilder output must be 576x928 before compact cropping for " + sheetPath + ".");
            }

            var sourceRows = new[] { "Idle", "Run", attackMotion, "Death" };
            var compact = new Texture2D(576, 256, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            compact.SetPixels32(new Color32[576 * 256]);
            var emptySlots = new List<string>();
            try
            {
                for (var row = 0; row < sourceRows.Length; row++)
                {
                    var sourceRow = sourceRows[row];
                    var sourceLayout = CharacterBuilder.Layout[sourceRow + "_0"];
                    for (var frame = 0; frame < 9; frame++)
                    {
                        var layout = CharacterBuilder.Layout[sourceRow + "_" + frame];
                        var pixels = builder.Texture.GetPixels(layout[0], layout[1], layout[2], layout[3]);
                        if (!pixels.Any(i => i.a > 0f)) emptySlots.Add(sourceRow + "_" + frame);
                        compact.SetPixels(frame * 64, (sourceRows.Length - 1 - row) * 64, 64, 64, pixels);
                    }
                }
                compact.Apply(false, false);
                File.WriteAllBytes(sheetPath, compact.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(compact);
                builder.Rebuild(forceMerge: false);
            }

            AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
            ConfigureCompactSpriteSheet(sheetPath);
            CreateCompactSpriteLibrary(sheetPath, libraryPath);
            return new CompactSheetResult(emptySlots.ToArray());
        }

        private static void ConfigureCompactSpriteSheet(string sheetPath)
        {
            const string examplePath = "Assets/PixelFantasy/PixelHeroes/Common/Sprites/Example.png";
            var example = (TextureImporter)AssetImporter.GetAtPath(examplePath);
            var target = (TextureImporter)AssetImporter.GetAtPath(sheetPath);
            if (!example || !target) throw new InvalidOperationException("Example or compact sheet importer is missing.");
            var settings = new TextureImporterSettings();
            example.ReadTextureSettings(settings);
            target.SetTextureSettings(settings);
            target.textureType = TextureImporterType.Sprite;
            target.spriteImportMode = SpriteImportMode.Multiple;
            target.textureCompression = TextureImporterCompression.Uncompressed;
            target.filterMode = FilterMode.Point;
            target.mipmapEnabled = false;
            target.isReadable = true;
            target.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var targetData = factory.GetSpriteEditorDataProviderFromObject(target);
            targetData.InitSpriteEditorDataProvider();
            var rects = new List<SpriteRect>(36);
            var rowOrder = new[] { "Idle", "Run", "Attack", "Death" };
            for (var row = 0; row < rowOrder.Length; row++)
            {
                for (var frame = 0; frame < 9; frame++)
                {
                    rects.Add(new SpriteRect
                    {
                        name = rowOrder[row] + "_" + frame,
                        rect = new Rect(frame * 64, (rowOrder.Length - 1 - row) * 64, 64, 64),
                        pivot = new Vector2(0.5f, 0.125f),
                        alignment = SpriteAlignment.Custom,
                    });
                }
            }
            targetData.SetSpriteRects(rects.ToArray());
            targetData.Apply();
            target.SaveAndReimport();
        }

        private static void CreateCompactSpriteLibrary(string sheetPath, string libraryPath)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length != 36) throw new InvalidOperationException("Compact sheet sprite count is not 36: " + sheetPath);
            var library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            AssetDatabase.CreateAsset(library, libraryPath);
            foreach (var category in new[] { "Idle", "Run", "Attack", "Death" })
            {
                for (var frame = 0; frame < 9; frame++)
                {
                    var label = frame.ToString();
                    var sprite = sprites.Single(i => i.name == category + "_" + label);
                    library.AddCategoryLabel(sprite, category, label);
                }
            }
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private static int V4SupportTop => V4HeaderHeight + V4Definitions.Length * V4RowHeight + V4SupportHeaderGap;
        private static int V6SupportTop => V4SupportTop;

        private static Texture2D CreateV4Sheet(TMP_FontAsset font)
        {
            var height = V4SupportTop + V4SupportCellTopOffset + V4SupportCellHeight + 24;
            var sheet = new Texture2D(SheetWidth, height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            sheet.SetPixels32(Enumerable.Repeat(new Color32(248, 252, 255, 255), sheet.width * sheet.height).ToArray());
            ComposeLabel(sheet, font, "COMPANION PAIRS", BaseX - 50, 24, Navy);
            ComposeLabel(sheet, font, "BASE", BaseX - 50, 58, Navy);
            ComposeLabel(sheet, font, "PROMOTION", PromotionX - 50, 58, Navy);
            ComposeLabel(sheet, font, "SUMMON / SUPPORT", 50, V4SupportTop + 18, Navy);
            return sheet;
        }

        private static Texture2D CreateV5Sheet(TMP_FontAsset font)
        {
            var height = V4SupportTop + V4SupportCellTopOffset + V4SupportCellHeight + 24;
            var sheet = new Texture2D(V4SheetWidth, height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            sheet.SetPixels32(Enumerable.Repeat(new Color32(248, 252, 255, 255), sheet.width * sheet.height).ToArray());
            ComposeLabel(sheet, font, "COMPANION PAIRS V5", BaseX - 50, 24, Navy);
            ComposeLabel(sheet, font, "BASE", BaseX - 50, 58, Navy);
            ComposeLabel(sheet, font, "PROMOTION", PromotionX - 50, 58, Navy);
            ComposeLabel(sheet, font, "SUMMON / SUPPORT", 50, V4SupportTop + 18, Navy);
            return sheet;
        }

        private static Texture2D CreateV6Sheet(TMP_FontAsset font)
        {
            var height = V4SupportTop + V4SupportCellTopOffset + V4SupportCellHeight + 24;
            var sheet = new Texture2D(V4SheetWidth, height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            sheet.SetPixels32(Enumerable.Repeat(new Color32(248, 252, 255, 255), sheet.width * sheet.height).ToArray());
            ComposeLabel(sheet, font, "COMPANION PAIRS V6", BaseX - 50, 24, Navy);
            ComposeLabel(sheet, font, "BASE", BaseX - 50, 58, Navy);
            ComposeLabel(sheet, font, "PROMOTION", PromotionX - 50, 58, Navy);
            ComposeLabel(sheet, font, "SUMMON / SUPPORT", 50, V6SupportTop + 18, Navy);
            return sheet;
        }

        private static Texture2D CreateCandidateSheet(TMP_FontAsset font)
        {
            var height = CandidateHeaderHeight + CandidateDefinitions.Length * CandidateRowHeight + 24;
            var sheet = new Texture2D(V4SheetWidth, height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            sheet.SetPixels32(Enumerable.Repeat(new Color32(248, 252, 255, 255), sheet.width * sheet.height).ToArray());
            ComposeLabel(sheet, font, "THREE-FAMILY CANDIDATES", 50, 24, Navy);
            ComposeLabel(sheet, font, "BASE", BaseX - 50, 58, Navy);
            ComposeLabel(sheet, font, "PROMOTION", PromotionX - 50, 58, Navy);
            return sheet;
        }

        private static CompanionArtThreeFamilyCandidateEntry CreateCandidateEntry(CandidateDefinition candidate, int scale, Color[] baseFrame, Color[] promotionFrame)
        {
            var pair = candidate.Pair;
            var baseBounds = GetBounds(baseFrame);
            var promotionBounds = GetBounds(promotionFrame);
            return new CompanionArtThreeFamilyCandidateEntry
            {
                optionId = candidate.OptionId,
                family = candidate.Family,
                baseId = pair.BaseId,
                promotionId = pair.PromotionId,
                baseKoreanLabel = pair.BaseKoreanLabel,
                promotionKoreanLabel = pair.PromotionKoreanLabel,
                reviewFrame = candidate.ReviewFrame,
                directionEvidence = candidate.DirectionEvidence,
                baseBody = pair.Body,
                promotionBody = pair.PromotionBody,
                baseArmor = pair.Armor,
                promotionArmor = pair.PromotionArmor,
                baseHead = pair.Body,
                promotionHead = pair.PromotionBody,
                baseHelmet = pair.Helmet,
                promotionHelmet = pair.PromotionHelmet,
                baseWeapon = pair.Weapon,
                promotionWeapon = pair.PromotionWeapon,
                baseShield = pair.Shield,
                promotionShield = pair.PromotionShield,
                baseBack = pair.Back,
                promotionBack = pair.PromotionBack,
                basePalette = pair.BasePalette,
                promotionPalette = pair.PromotionPalette,
                basePreview = CandidatesThreeFamiliesOutputDirectory + "/" + candidate.OptionId + "_" + pair.BaseId + "_idle.png",
                promotionPreview = CandidatesThreeFamiliesOutputDirectory + "/" + candidate.OptionId + "_" + pair.PromotionId + "_idle.png",
                baseOpaquePixels = baseBounds.width * baseBounds.height,
                promotionOpaquePixels = promotionBounds.width * promotionBounds.height,
            };
        }

        private static void WriteCandidateCell(Texture2D sheet, TMP_FontAsset font, CompanionArtThreeFamilyCandidateEntry entry, int x, int cellTop, bool promotion)
        {
            var previewPath = promotion ? entry.promotionPreview : entry.basePreview;
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(previewPath)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid candidate preview: " + previewPath);
                }
                var cardTop = cellTop + CandidatePreviewTopOffset;
                FillTopRect(sheet, x - 10, cardTop - 10, CandidateCardSize, CandidateCardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, CandidateCardSize, CandidateCardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, BuildCandidateLabel(entry, promotion), x - 50, cellTop + CandidateLabelTopOffset, Navy);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static string BuildCandidateLabel(CompanionArtThreeFamilyCandidateEntry entry, bool promotion)
        {
            var id = promotion ? entry.promotionId : entry.baseId;
            var name = promotion ? entry.promotionKoreanLabel : entry.baseKoreanLabel;
            var armor = promotion ? entry.promotionArmor : entry.baseArmor;
            var helmet = promotion ? entry.promotionHelmet : entry.baseHelmet;
            var weapon = promotion ? entry.promotionWeapon : entry.baseWeapon;
            var shield = promotion ? entry.promotionShield : entry.baseShield;
            var back = promotion ? entry.promotionBack : entry.baseBack;
            return entry.optionId + " / " + entry.family + "\n" + name + " / " + id + "\n" + entry.reviewFrame + "\n" + "A:" + armor + " H:" + (string.IsNullOrEmpty(helmet) ? "none" : helmet) + "\n" + "W:" + (string.IsNullOrEmpty(weapon) ? "none" : weapon) + " S:" + (string.IsNullOrEmpty(shield) ? "none" : shield) + " B:" + (string.IsNullOrEmpty(back) ? "none" : back);
        }

        private static CompanionArtV4PairEntry CreateV4PairEntry(Definition definition, int scale, Color[] baseFrame, Color[] promotionFrame)
        {
            var baseBounds = GetBounds(baseFrame);
            var promotionBounds = GetBounds(promotionFrame);
            return new CompanionArtV4PairEntry
            {
                baseId = definition.BaseId,
                promotionId = definition.PromotionId,
                baseKoreanLabel = definition.BaseKoreanLabel,
                promotionKoreanLabel = definition.PromotionKoreanLabel,
                role = definition.Role,
                basePreview = V4OutputDirectory + "/" + definition.BaseId + "_idle.png",
                promotionPreview = V4OutputDirectory + "/" + definition.PromotionId + "_idle.png",
                baseBody = definition.Body,
                promotionBody = definition.PromotionBody,
                baseArmor = definition.Armor,
                promotionArmor = definition.PromotionArmor,
                baseHelmet = definition.Helmet,
                promotionHelmet = definition.PromotionHelmet,
                baseWeapon = definition.Weapon,
                promotionWeapon = definition.PromotionWeapon,
                baseShield = definition.Shield,
                promotionShield = definition.PromotionShield,
                baseBack = definition.Back,
                promotionBack = definition.PromotionBack,
                basePalette = definition.BasePalette,
                promotionPalette = definition.PromotionPalette,
                persistentIdentityCues = GetIdentityCues(definition.BaseId),
                promotionDelta = GetPromotionDelta(definition.BaseId),
                integerScale = scale,
                baseVisibleHeight = baseBounds.height * scale,
                promotionVisibleHeight = promotionBounds.height * scale,
                externalPropRequired = definition.ExternalPropRequired,
                externalPropReason = definition.ExternalPropReason,
            };
        }

        private static CompanionArtV4PairEntry CreateV5PairEntry(Definition definition, int scale, Color[] baseFrame, Color[] promotionFrame)
        {
            var baseBounds = GetBounds(baseFrame);
            var promotionBounds = GetBounds(promotionFrame);
            var baseMetrics = GetFrameMetrics(baseFrame, FrameSize, FrameSize);
            var promotionMetrics = GetFrameMetrics(promotionFrame, FrameSize, FrameSize);
            return new CompanionArtV4PairEntry
            {
                baseId = definition.BaseId, promotionId = definition.PromotionId,
                baseKoreanLabel = definition.BaseKoreanLabel, promotionKoreanLabel = definition.PromotionKoreanLabel,
                role = definition.Role,
                basePreview = V5OutputDirectory + "/" + definition.BaseId + "_idle.png",
                promotionPreview = V5OutputDirectory + "/" + definition.PromotionId + "_idle.png",
                baseBody = definition.Body, promotionBody = definition.PromotionBody,
                baseArmor = definition.Armor, promotionArmor = definition.PromotionArmor,
                baseHelmet = definition.Helmet, promotionHelmet = definition.PromotionHelmet,
                baseWeapon = definition.Weapon, promotionWeapon = definition.PromotionWeapon,
                baseShield = definition.Shield, promotionShield = definition.PromotionShield,
                baseBack = definition.Back, promotionBack = definition.PromotionBack,
                basePalette = definition.BasePalette, promotionPalette = definition.PromotionPalette,
                persistentIdentityCues = GetV5IdentityCues(definition.BaseId),
                promotionDelta = GetV5PromotionDelta(definition.BaseId),
                integerScale = scale,
                baseVisibleHeight = baseBounds.height * scale, promotionVisibleHeight = promotionBounds.height * scale,
                baseOpaquePixels = baseMetrics.opaquePixels, promotionOpaquePixels = promotionMetrics.opaquePixels,
                baseSideMass = baseMetrics.sideMass, promotionSideMass = promotionMetrics.sideMass,
                baseHeadCorePixels = baseMetrics.headCorePixels, promotionHeadCorePixels = promotionMetrics.headCorePixels,
                baseCentroidX1000 = baseMetrics.centroidX1000, promotionCentroidX1000 = promotionMetrics.centroidX1000,
                silhouetteDelta = Mathf.Abs(promotionMetrics.opaquePixels - baseMetrics.opaquePixels),
                externalPropRequired = definition.ExternalPropRequired, externalPropReason = definition.ExternalPropReason,
            };
        }

        private static CompanionArtV4PairEntry CloneV4PairForV5(CompanionArtV4PairEntry source)
        {
            return new CompanionArtV4PairEntry
            {
                baseId = source.baseId, promotionId = source.promotionId,
                baseKoreanLabel = source.baseKoreanLabel, promotionKoreanLabel = source.promotionKoreanLabel,
                role = source.role,
                basePreview = V5OutputDirectory + "/" + source.baseId + "_idle.png",
                promotionPreview = V5OutputDirectory + "/" + source.promotionId + "_idle.png",
                baseBody = source.baseBody, promotionBody = source.promotionBody,
                baseArmor = source.baseArmor, promotionArmor = source.promotionArmor,
                baseHelmet = source.baseHelmet, promotionHelmet = source.promotionHelmet,
                baseWeapon = source.baseWeapon, promotionWeapon = source.promotionWeapon,
                baseShield = source.baseShield, promotionShield = source.promotionShield,
                baseBack = source.baseBack, promotionBack = source.promotionBack,
                basePalette = source.basePalette?.ToArray(), promotionPalette = source.promotionPalette?.ToArray(),
                persistentIdentityCues = source.persistentIdentityCues?.ToArray(), promotionDelta = source.promotionDelta,
                integerScale = source.integerScale,
                baseVisibleHeight = source.baseVisibleHeight, promotionVisibleHeight = source.promotionVisibleHeight,
                baseOpaquePixels = source.baseOpaquePixels, promotionOpaquePixels = source.promotionOpaquePixels,
                baseSideMass = source.baseSideMass, promotionSideMass = source.promotionSideMass,
                baseHeadCorePixels = source.baseHeadCorePixels, promotionHeadCorePixels = source.promotionHeadCorePixels,
                baseCentroidX1000 = source.baseCentroidX1000, promotionCentroidX1000 = source.promotionCentroidX1000,
                silhouetteDelta = source.silhouetteDelta,
                externalPropRequired = source.externalPropRequired, externalPropReason = source.externalPropReason,
            };
        }

        private static CompanionArtV4PairEntry CloneV4PairForV6(CompanionArtV4PairEntry source)
        {
            var clone = CloneV4PairForV5(source);
            clone.basePreview = V6OutputDirectory + "/" + source.baseId + "_idle.png";
            clone.promotionPreview = V6OutputDirectory + "/" + source.promotionId + "_idle.png";
            return clone;
        }

        private static CompanionArtV4SupportEntry CloneV4SupportForV6(CompanionArtV4SupportEntry source)
        {
            var clone = CloneV4SupportForV5(source);
            clone.preview = V6OutputDirectory + "/" + source.id + "_idle.png";
            return clone;
        }

        private static Definition DefinitionFromCandidate(CompanionArtV4PairEntry source, CompanionArtThreeFamilyCandidateEntry candidate)
        {
            return new Definition(
                candidate.baseId,
                candidate.promotionId,
                candidate.baseKoreanLabel,
                candidate.promotionKoreanLabel,
                source.role,
                candidate.baseBody,
                candidate.baseArmor,
                candidate.baseHelmet,
                candidate.baseWeapon,
                candidate.baseShield,
                candidate.baseBack,
                candidate.promotionBody,
                candidate.promotionArmor,
                candidate.promotionHelmet,
                candidate.promotionWeapon,
                candidate.promotionShield,
                candidate.promotionBack,
                candidate.basePalette,
                candidate.promotionPalette,
                source.externalPropRequired,
                source.externalPropReason);
        }

        private static Definition DefinitionFromPair(CompanionArtV4PairEntry pair)
        {
            return new Definition(
                pair.baseId,
                pair.promotionId,
                pair.baseKoreanLabel,
                pair.promotionKoreanLabel,
                pair.role,
                pair.baseBody,
                pair.baseArmor,
                pair.baseHelmet,
                pair.baseWeapon,
                pair.baseShield,
                pair.baseBack,
                pair.promotionBody,
                pair.promotionArmor,
                pair.promotionHelmet,
                pair.promotionWeapon,
                pair.promotionShield,
                pair.promotionBack,
                pair.basePalette,
                pair.promotionPalette,
                pair.externalPropRequired,
                pair.externalPropReason);
        }

        private static string GetV6Selection(string baseId)
        {
            return baseId switch
            {
                "shield_guard" => "shield_B",
                "wolf_tamer" => "wolf_B",
                "skeleton_bomber" => "skeleton_C",
                _ => "V6 preserved",
            };
        }

        private static CompanionFullSpriteSheetEntry CreateCompanionSheetEntry(CompanionArtV4PairEntry pair, Definition definition, bool promotion, string sheetPath, string libraryPath)
        {
            return new CompanionFullSpriteSheetEntry
            {
                id = promotion ? pair.promotionId : pair.baseId,
                promotionOf = promotion ? pair.baseId : "",
                body = promotion ? definition.PromotionBody : definition.Body,
                armor = promotion ? definition.PromotionArmor : definition.Armor,
                helmet = promotion ? definition.PromotionHelmet : definition.Helmet,
                weapon = promotion ? definition.PromotionWeapon : definition.Weapon,
                shield = promotion ? definition.PromotionShield : definition.Shield,
                back = promotion ? definition.PromotionBack : definition.Back,
                sourceContactSheet = V6ContactSheetAssetPath,
                sourceSelection = GetV6Selection(pair.baseId),
                sheetPath = sheetPath,
                libraryPath = libraryPath,
                width = 576,
                height = 928,
                categoryCount = 14,
                labelCount = 126,
            };
        }

        private static void ExportGeneratedCharacterSheet(CharacterBuilder builder, Definition definition, bool promotion, string sheetPath, string libraryPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(libraryPath)) AssetDatabase.DeleteAsset(libraryPath);
            if (File.Exists(sheetPath)) File.Delete(sheetPath);

            ApplyV5(builder, definition, promotion);
            builder.Rebuild(forceMerge: true);
            if (!builder.Texture || builder.Texture.width != 576 || builder.Texture.height != 928)
            {
                throw new InvalidOperationException("CharacterBuilder output must be 576x928 for " + sheetPath + ".");
            }

            File.WriteAllBytes(sheetPath, builder.Texture.EncodeToPNG());
            builder.Rebuild(forceMerge: false);
            AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
            ConfigureGeneratedSpriteSheet(sheetPath);
            CreateCharacterBuilderSpriteLibrary(sheetPath, libraryPath);
        }

        private static void ConfigureGeneratedSpriteSheet(string sheetPath)
        {
            const string examplePath = "Assets/PixelFantasy/PixelHeroes/Common/Sprites/Example.png";
            var example = (TextureImporter)AssetImporter.GetAtPath(examplePath);
            var target = (TextureImporter)AssetImporter.GetAtPath(sheetPath);
            if (!example || !target) throw new InvalidOperationException("Example or generated sheet importer is missing.");

            var settings = new TextureImporterSettings();
            example.ReadTextureSettings(settings);
            target.SetTextureSettings(settings);
            target.textureType = TextureImporterType.Sprite;
            target.spriteImportMode = SpriteImportMode.Multiple;
            target.textureCompression = TextureImporterCompression.Uncompressed;
            target.filterMode = FilterMode.Point;
            target.mipmapEnabled = false;
            target.isReadable = true;
            target.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var targetData = factory.GetSpriteEditorDataProviderFromObject(target);
            targetData.InitSpriteEditorDataProvider();
            var spriteRects = new List<SpriteRect>(CharacterBuilder.Layout.Count);
            foreach (var pair in CharacterBuilder.Layout)
            {
                var layout = pair.Value;
                spriteRects.Add(new SpriteRect
                {
                    name = pair.Key,
                    rect = new Rect(layout[0], layout[1], layout[2], layout[3]),
                    pivot = new Vector2((float)layout[4] / layout[2], (float)layout[5] / layout[3]),
                    alignment = SpriteAlignment.Custom,
                });
            }
            targetData.SetSpriteRects(spriteRects.ToArray());
            targetData.Apply();
            target.SaveAndReimport();
        }

        private static void CreateCharacterBuilderSpriteLibrary(string sheetPath, string libraryPath)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length != CharacterBuilder.Layout.Count) throw new InvalidOperationException("Generated sheet sprite count is not 126: " + sheetPath);
            var library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            AssetDatabase.CreateAsset(library, libraryPath);
            foreach (var pair in CharacterBuilder.Layout)
            {
                var sprite = FindSpriteForLayout(sprites, pair.Value);
                if (!sprite) throw new InvalidOperationException("Generated sheet is missing layout sprite: " + pair.Key);
                var split = pair.Key.Split('_');
                library.AddCategoryLabel(sprite, split[0], split[1]);
            }
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private static void CopyVendorSheetAndCreateLibrary(string sourceSheet, string sourceLibraryPath, string destinationSheet, string destinationLibraryPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationLibraryPath)) AssetDatabase.DeleteAsset(destinationLibraryPath);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationSheet)) AssetDatabase.DeleteAsset(destinationSheet);
            if (!AssetDatabase.CopyAsset(sourceSheet, destinationSheet)) throw new InvalidOperationException("Could not copy summon sheet: " + sourceSheet);
            AssetDatabase.ImportAsset(destinationSheet, ImportAssetOptions.ForceUpdate);

            var sourceLibrary = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(sourceLibraryPath);
            var sourceSprites = AssetDatabase.LoadAllAssetsAtPath(sourceSheet).OfType<Sprite>().ToArray();
            var destinationSprites = AssetDatabase.LoadAllAssetsAtPath(destinationSheet).OfType<Sprite>().ToArray();
            if (!sourceLibrary || sourceSprites.Length == 0 || destinationSprites.Length == 0)
            {
                throw new InvalidOperationException("Summon source library or sprites are missing: " + sourceSheet);
            }

            var destinationLibrary = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            AssetDatabase.CreateAsset(destinationLibrary, destinationLibraryPath);
            foreach (var category in sourceLibrary.GetCategoryNames())
            {
                foreach (var label in sourceLibrary.GetCategoryLabelNames(category))
                {
                    var sourceSprite = sourceLibrary.GetSprite(category, label);
                    if (!sourceSprite) throw new InvalidOperationException("Summon source library label is empty: " + category + "/" + label);
                    var destinationSprite = destinationSprites.FirstOrDefault(i => SameRect(i.rect, sourceSprite.rect));
                    if (!destinationSprite) throw new InvalidOperationException("Summon destination sprite rect is missing: " + category + "/" + label);
                    destinationLibrary.AddCategoryLabel(destinationSprite, category, label);
                }
            }
            EditorUtility.SetDirty(destinationLibrary);
            AssetDatabase.SaveAssets();
        }

        private static (int categoryCount, int labelCount) GetSpriteLibraryCounts(string libraryPath)
        {
            var library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
            if (!library) throw new InvalidOperationException("SpriteLibrary asset is missing: " + libraryPath);
            var categories = library.GetCategoryNames().ToArray();
            var labels = categories.Sum(i => library.GetCategoryLabelNames(i).Count());
            return (categories.Length, labels);
        }

        private static Sprite FindSpriteForLayout(IEnumerable<Sprite> sprites, int[] layout)
        {
            return sprites.FirstOrDefault(i => Mathf.RoundToInt(i.rect.x) == layout[0] && Mathf.RoundToInt(i.rect.y) == layout[1] && Mathf.RoundToInt(i.rect.width) == layout[2] && Mathf.RoundToInt(i.rect.height) == layout[3]);
        }

        private static bool SameRect(Rect left, Rect right)
        {
            return Mathf.RoundToInt(left.x) == Mathf.RoundToInt(right.x) && Mathf.RoundToInt(left.y) == Mathf.RoundToInt(right.y) && Mathf.RoundToInt(left.width) == Mathf.RoundToInt(right.width) && Mathf.RoundToInt(left.height) == Mathf.RoundToInt(right.height);
        }

        private static CompanionArtV4PairEntry CreateV6SelectedPairEntry(CompanionArtV4PairEntry source, CompanionArtThreeFamilyCandidateEntry candidate)
        {
            var entry = CloneV4PairForV6(source);
            entry.baseBody = candidate.baseBody;
            entry.promotionBody = candidate.promotionBody;
            entry.baseArmor = candidate.baseArmor;
            entry.promotionArmor = candidate.promotionArmor;
            entry.baseHelmet = candidate.baseHelmet;
            entry.promotionHelmet = candidate.promotionHelmet;
            entry.baseWeapon = candidate.baseWeapon;
            entry.promotionWeapon = candidate.promotionWeapon;
            entry.baseShield = candidate.baseShield;
            entry.promotionShield = candidate.promotionShield;
            entry.baseBack = candidate.baseBack;
            entry.promotionBack = candidate.promotionBack;
            entry.basePalette = candidate.basePalette?.ToArray();
            entry.promotionPalette = candidate.promotionPalette?.ToArray();
            entry.baseOpaquePixels = candidate.baseOpaquePixels;
            entry.promotionOpaquePixels = candidate.promotionOpaquePixels;
            entry.baseSideMass = candidate.baseSideMass;
            entry.promotionSideMass = candidate.promotionSideMass;
            entry.baseHeadCorePixels = candidate.baseHeadCorePixels;
            entry.promotionHeadCorePixels = candidate.promotionHeadCorePixels;
            entry.baseCentroidX1000 = candidate.baseCentroidX1000;
            entry.promotionCentroidX1000 = candidate.promotionCentroidX1000;
            entry.silhouetteDelta = candidate.silhouetteDelta;
            return entry;
        }

        private static CompanionArtV4SupportEntry CloneV4SupportForV5(CompanionArtV4SupportEntry source)
        {
            return new CompanionArtV4SupportEntry
            {
                id = source.id, label = source.label, sourcePrefab = source.sourcePrefab,
                sourceArt = source.sourceArt, sourceLabel = source.sourceLabel,
                preview = V5OutputDirectory + "/" + source.id + "_idle.png",
                composition = source.composition?.ToArray(), limitation = source.limitation,
            };
        }

        private static void ApplyMetrics(CompanionArtV4PairEntry entry, (int opaquePixels, int sideMass, int headCorePixels, int centroidX1000) baseMetrics, (int opaquePixels, int sideMass, int headCorePixels, int centroidX1000) promotionMetrics)
        {
            entry.baseOpaquePixels = baseMetrics.opaquePixels;
            entry.promotionOpaquePixels = promotionMetrics.opaquePixels;
            entry.baseSideMass = baseMetrics.sideMass;
            entry.promotionSideMass = promotionMetrics.sideMass;
            entry.baseHeadCorePixels = baseMetrics.headCorePixels;
            entry.promotionHeadCorePixels = promotionMetrics.headCorePixels;
            entry.baseCentroidX1000 = baseMetrics.centroidX1000;
            entry.promotionCentroidX1000 = promotionMetrics.centroidX1000;
            entry.silhouetteDelta = Mathf.Abs(promotionMetrics.opaquePixels - baseMetrics.opaquePixels);
        }

        private static (int opaquePixels, int sideMass, int headCorePixels, int centroidX1000) GetPreviewMetrics(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(texture, File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid V5 preview: " + path);
                var pixels32 = texture.GetPixels32();
                var pixels = new Color[pixels32.Length];
                for (var i = 0; i < pixels32.Length; i++) pixels[i] = pixels32[i];
                return GetFrameMetrics(pixels, texture.width, texture.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static (int opaquePixels, int sideMass, int headCorePixels, int centroidX1000) GetFrameMetrics(Color[] pixels, int width, int height)
        {
            var opaque = 0;
            var left = 0;
            var right = 0;
            var head = 0;
            var weightedX = 0L;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[x + y * width].a <= 0) continue;
                    opaque++;
                    weightedX += x;
                    if (x < width / 3) left++;
                    if (x >= width * 2 / 3) right++;
                    if (x >= width / 3 && x < width * 2 / 3 && y >= height / 2) head++;
                }
            }
            return (opaque, Mathf.Max(left, right), head, opaque == 0 ? 0 : Mathf.RoundToInt((float)weightedX * 1000f / opaque));
        }

        private static string[] GetV5IdentityCues(string id)
        {
            return id switch
            {
                "shield_guard" => new[] { "full plate", "visible shield", "sword" },
                "wolf_tamer" => new[] { "matched head/orientation", "tamer outfit", "beast palette" },
                "skeleton_bomber" => new[] { "exposed Skeleton skull", "small backpack", "bone identity" },
                _ => GetIdentityCues(id),
            };
        }

        private static string GetV5PromotionDelta(string id)
        {
            return id switch
            {
                "shield_guard" => "same IronKnight full plate; CavalrymanHelmet and RoyalGreatShield make the promotion visibly heavier",
                "wolf_tamer" => "same GuardianTunic/head direction; LargeBackpack adds the commander silhouette",
                "skeleton_bomber" => "same exposed Skeleton skull; HeavyKnightArmor and LargeBackpack create a materially heavier artillery silhouette",
                _ => GetPromotionDelta(id),
            };
        }

        private static CompanionArtV4SupportEntry CreateSupportEntry(string id, string label, string sourcePrefab, string sourceArt, string sourceLabel, Texture2D preview, string[] composition, string limitation)
        {
            var previewPath = V4OutputDirectory + "/" + id + "_idle.png";
            return new CompanionArtV4SupportEntry
            {
                id = id,
                label = label,
                sourcePrefab = sourcePrefab,
                sourceArt = sourceArt,
                sourceLabel = sourceLabel,
                preview = previewPath,
                composition = composition,
                limitation = limitation,
            };
        }

        private static string[] GetIdentityCues(string id)
        {
            return id switch
            {
                "shield_guard" => new[] { "full plate", "sword", "shield" },
                "sword_soldier" => new[] { "allied blue/gold", "sword", "front melee" },
                "falcon_archer" => new[] { "ranger hood", "bow", "quiver" },
                "field_herbalist" => new[] { "travel outfit", "healing green", "backpack" },
                "bombardier" => new[] { "engineer armor", "storage backpack", "explosive support" },
                "wolf_tamer" => new[] { "tamer outfit", "beast palette", "owner silhouette" },
                "skeleton_bomber" => new[] { "exposed Skeleton", "practical armor", "storage backpack" },
                _ => new[] { "preserved V3 family identity" },
            };
        }

        private static string GetPromotionDelta(string id)
        {
            return id switch
            {
                "shield_guard" => "shared HeavyKnightArmor with upgraded helmet, sword, and KnightShield to RoyalGreatShield",
                "sword_soldier" => "shared BlueKnight armor with upgraded helmet and IronSword to Longsword",
                "falcon_archer" => "shared ArcherTunic with upgraded CaptainHelmet and Bow to LongBow",
                "field_herbalist" => "TravelerTunic/BanditBandana to PlagueDoctor and LargeBackpack",
                "bombardier" => "BulletHeadArmor to RocketArmour and SmallBackpack to LargeBackpack",
                "wolf_tamer" => "TravelerTunic to GuardianTunic and LargeBackpack command silhouette",
                "skeleton_bomber" => "exposed Skeleton skull preserved; Militiaman to BulletHead armor and SmallBackpack to LargeBackpack",
                _ => "preserved accepted V3 promotion definition",
            };
        }

        private static void WriteV4PairCell(Texture2D sheet, TMP_FontAsset font, CompanionArtV4PairEntry entry, int x, int cellTop, bool promotion)
        {
            var previewPath = promotion ? entry.promotionPreview : entry.basePreview;
            var label = promotion ? entry.promotionKoreanLabel : entry.baseKoreanLabel;
            var id = promotion ? entry.promotionId : entry.baseId;
            WriteV4Cell(sheet, font, previewPath, x, cellTop, label, id, entry.externalPropRequired);
        }

        private static void WriteV4Cell(Texture2D sheet, TMP_FontAsset font, string previewPath, int x, int cellTop, string koreanLabel, string id, bool externalPropRequired)
        {
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(previewPath)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid V4 preview: " + previewPath);
                }
                var cardTop = cellTop + V4PreviewTopOffset;
                FillTopRect(sheet, x - 10, cardTop - 10, V4CardSize, V4CardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, V4CardSize, V4CardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, koreanLabel, x - 50, cellTop + 8, Navy);
                ComposeLabel(sheet, font, id, x - 50, cellTop + 46, Navy);
                if (externalPropRequired) ComposeLabel(sheet, font, "외부 소품 필요", x - 50, cellTop + V4MarkerTopOffset, Orange);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void WriteV4SupportCell(Texture2D sheet, TMP_FontAsset font, CompanionArtV4SupportEntry entry, int x, int supportTop)
        {
            var cellTop = supportTop + V4SupportCellTopOffset;
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(entry.preview)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid V4 support preview: " + entry.preview);
                }
                var cardTop = cellTop + 110;
                FillTopRect(sheet, x - 10, cardTop - 10, V4CardSize, V4CardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, V4CardSize, V4CardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, entry.label, x - 50, cellTop + 8, Navy);
                ComposeLabel(sheet, font, entry.sourceLabel, x - 50, cellTop + 46, Navy);
                ComposeLabel(sheet, font, entry.id, x - 50, cellTop + 330, Orange);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void WriteV5PairCell(Texture2D sheet, TMP_FontAsset font, CompanionArtV4PairEntry entry, int x, int cellTop, bool promotion)
        {
            var previewPath = promotion ? entry.promotionPreview : entry.basePreview;
            var label = promotion ? entry.promotionKoreanLabel : entry.baseKoreanLabel;
            var id = promotion ? entry.promotionId : entry.baseId;
            WriteV5Cell(sheet, font, previewPath, x, cellTop, label, id, entry.externalPropRequired);
        }

        private static void WriteV5Cell(Texture2D sheet, TMP_FontAsset font, string previewPath, int x, int cellTop, string koreanLabel, string id, bool externalPropRequired)
        {
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(previewPath)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid V5 preview: " + previewPath);
                }
                var cardTop = cellTop + V4PreviewTopOffset;
                FillTopRect(sheet, x - 10, cardTop - 10, V4CardSize, V4CardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, V4CardSize, V4CardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, koreanLabel, x - 50, cellTop + 8, Navy);
                ComposeLabel(sheet, font, id, x - 50, cellTop + 46, Navy);
                if (externalPropRequired) ComposeLabel(sheet, font, "외부 소품 필요", x - 50, cellTop + V4MarkerTopOffset, Orange);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void WriteV5SupportCell(Texture2D sheet, TMP_FontAsset font, CompanionArtV4SupportEntry entry, int x, int supportTop)
        {
            var cellTop = supportTop + V4SupportCellTopOffset;
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(entry.preview)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid V5 support preview: " + entry.preview);
                }
                var cardTop = cellTop + 110;
                FillTopRect(sheet, x - 10, cardTop - 10, V4CardSize, V4CardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, V4CardSize, V4CardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, entry.label, x - 50, cellTop + 8, Navy);
                ComposeLabel(sheet, font, entry.sourceLabel, x - 50, cellTop + 46, Navy);
                ComposeLabel(sheet, font, entry.id, x - 50, cellTop + 330, Orange);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void WriteSupportPreview(Texture2D preview, string path)
        {
            File.WriteAllBytes(path, preview.EncodeToPNG());
        }

        private static void WriteFramePreview(Color[] frame, string path, int scale)
        {
            var preview = CreatePreview(frame, scale);
            try
            {
                File.WriteAllBytes(path, preview.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static Texture2D CreatePrefabSupportPreview(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var renderer = root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(i => i.sprite);
                if (!renderer || !renderer.sprite) throw new InvalidOperationException("Prefab has no authored SpriteRenderer: " + prefabPath);
                return CreatePreviewFromSprite(renderer.sprite);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Texture2D CreatePreviewFromSprite(Sprite sprite)
        {
            var rect = sprite.textureRect;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);
            var pixels = sprite.texture.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), width, height);
            var bounds = GetBounds(pixels, width, height);
            var scale = 1;
            while (scale < 6 && bounds.height * (scale + 1) <= 140 && bounds.width * (scale + 1) <= 176) scale++;
            return CreatePreviewFromPixels(pixels, width, height, scale);
        }

        private static void NormalizeV4SheetBackground(Texture2D sheet)
        {
            sheet.Apply(false, false);
            var pixels = sheet.GetPixels32();
            for (var y = 0; y < sheet.height; y++)
            {
                var top = sheet.height - 1 - y;
                for (var x = 0; x < sheet.width; x++)
                {
                    var index = x + y * sheet.width;
                    if (pixels[index].a != 0) continue;
                    var card = false;
                    for (var row = 0; row < V4Definitions.Length; row++)
                    {
                        var cardTop = V4HeaderHeight + row * V4RowHeight + V4PreviewTopOffset;
                        if (IsInsideV4Card(x, top, BaseX, cardTop) || IsInsideV4Card(x, top, PromotionX, cardTop))
                        {
                            card = true;
                            break;
                        }
                    }
                    if (!card)
                    {
                        var supportCardTop = V4SupportTop + V4SupportCellTopOffset + 110;
                        card = IsInsideV4Card(x, top, 220, supportCardTop) || IsInsideV4Card(x, top, 690, supportCardTop) || IsInsideV4Card(x, top, 1160, supportCardTop);
                    }
                    pixels[index] = card ? CardFill : new Color32(248, 252, 255, 255);
                }
            }
            sheet.SetPixels32(pixels);
        }

        private static void NormalizeV5SheetBackground(Texture2D sheet)
        {
            sheet.Apply(false, false);
            var pixels = sheet.GetPixels32();
            for (var y = 0; y < sheet.height; y++)
            {
                var top = sheet.height - 1 - y;
                for (var x = 0; x < sheet.width; x++)
                {
                    var index = x + y * sheet.width;
                    if (pixels[index].a != 0) continue;
                    var card = false;
                    for (var row = 0; row < V5Definitions.Length; row++)
                    {
                        var cardTop = V4HeaderHeight + row * V4RowHeight + V4PreviewTopOffset;
                        if (IsInsideV4Card(x, top, BaseX, cardTop) || IsInsideV4Card(x, top, PromotionX, cardTop))
                        {
                            card = true;
                            break;
                        }
                    }
                    if (!card)
                    {
                        var supportCardTop = V4SupportTop + V4SupportCellTopOffset + 110;
                        card = IsInsideV4Card(x, top, 220, supportCardTop) || IsInsideV4Card(x, top, 690, supportCardTop) || IsInsideV4Card(x, top, 1160, supportCardTop);
                    }
                    pixels[index] = card ? CardFill : new Color32(248, 252, 255, 255);
                }
            }
            sheet.SetPixels32(pixels);
        }

        private static void NormalizeCandidateSheetBackground(Texture2D sheet)
        {
            sheet.Apply(false, false);
            var pixels = sheet.GetPixels32();
            for (var y = 0; y < sheet.height; y++)
            {
                var top = sheet.height - 1 - y;
                for (var x = 0; x < sheet.width; x++)
                {
                    var index = x + y * sheet.width;
                    if (pixels[index].a != 0) continue;
                    var card = false;
                    for (var row = 0; row < CandidateDefinitions.Length; row++)
                    {
                        var cardTop = CandidateHeaderHeight + row * CandidateRowHeight + CandidatePreviewTopOffset;
                        if (IsInsideCandidateCard(x, top, BaseX, cardTop) || IsInsideCandidateCard(x, top, PromotionX, cardTop))
                        {
                            card = true;
                            break;
                        }
                    }
                    pixels[index] = card ? CardFill : new Color32(248, 252, 255, 255);
                }
            }
            sheet.SetPixels32(pixels);
        }

        private static bool IsInsideCandidateCard(int x, int top, int previewX, int cardTop)
        {
            var cardLeft = previewX - 10;
            return x >= cardLeft && x < cardLeft + CandidateCardSize && top >= cardTop - 10 && top < cardTop - 10 + CandidateCardSize;
        }

        private static bool IsInsideV4Card(int x, int top, int previewX, int cardTop)
        {
            var cardLeft = previewX - 10;
            return x >= cardLeft && x < cardLeft + V4CardSize && top >= cardTop - 10 && top < cardTop - 10 + V4CardSize;
        }

        private static Texture2D CreateV3Sheet(TMP_FontAsset font)
        {
            var height = V3HeaderHeight + Definitions.Length * V3RowHeight + 24;
            var sheet = new Texture2D(V3SheetWidth, height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            sheet.SetPixels32(Enumerable.Repeat(new Color32(248, 252, 255, 255), sheet.width * sheet.height).ToArray());
            ComposeLabel(sheet, font, "BASE", BaseX - 50, 24, Navy);
            ComposeLabel(sheet, font, "PROMOTION", PromotionX - 50, 24, Navy);
            return sheet;
        }

        private static void WriteV3Cell(Texture2D sheet, TMP_FontAsset font, string previewPath, int x, int cellTop, string koreanLabel, string id, bool externalPropRequired)
        {
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(previewPath)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid V2 preview for V3: " + previewPath);
                }

                var cardTop = cellTop + V3PreviewTopOffset;
                FillTopRect(sheet, x - 10, cardTop - 10, V3CardSize, V3CardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, V3CardSize, V3CardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, koreanLabel, x - 50, cellTop + V3LabelTopOffset, Navy);
                ComposeLabel(sheet, font, id, x - 50, cellTop + V3IdTopOffset, Navy);
                if (externalPropRequired) ComposeLabel(sheet, font, "외부 소품 필요", x - 50, cellTop + V3MarkerTopOffset, Orange);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void NormalizeV3SheetBackground(Texture2D sheet)
        {
            sheet.Apply(false, false);
            var pixels = sheet.GetPixels32();
            for (var y = 0; y < sheet.height; y++)
            {
                var top = sheet.height - 1 - y;
                for (var x = 0; x < sheet.width; x++)
                {
                    var index = x + y * sheet.width;
                    var color = pixels[index];
                    var card = false;
                    for (var row = 0; row < Definitions.Length; row++)
                    {
                        var cardTop = V3HeaderHeight + row * V3RowHeight + V3PreviewTopOffset;
                        if (IsInsideV3Card(x, top, BaseX, cardTop) || IsInsideV3Card(x, top, PromotionX, cardTop))
                        {
                            card = true;
                            break;
                        }
                    }

                    if (card && color.a == 0) pixels[index] = CardFill;
                    else if (color.a == 0) pixels[index] = new Color32(248, 252, 255, 255);
                }
            }
            sheet.SetPixels32(pixels);
        }

        private static bool IsInsideV3Card(int x, int top, int previewX, int cardTop)
        {
            var cardLeft = previewX - 10;
            return x >= cardLeft && x < cardLeft + V3CardSize && top >= cardTop - 10 && top < cardTop - 10 + V3CardSize;
        }

        private static CompanionArtManifestEntry CreateEntry(Definition definition, int scale, Color[] baseFrame, Color[] promotionFrame)
        {
            var baseBounds = GetBounds(baseFrame);
            var promotionBounds = GetBounds(promotionFrame);
            return new CompanionArtManifestEntry
            {
                baseId = definition.BaseId,
                promotionId = definition.PromotionId,
                baseKoreanLabel = definition.BaseKoreanLabel,
                promotionKoreanLabel = definition.PromotionKoreanLabel,
                role = definition.Role,
                basePreview = OutputDirectory + "/" + definition.BaseId + "_idle.png",
                promotionPreview = OutputDirectory + "/" + definition.PromotionId + "_idle.png",
                baseBody = definition.Body,
                promotionBody = definition.PromotionBody,
                baseArmor = definition.Armor,
                promotionArmor = definition.PromotionArmor,
                baseHelmet = definition.Helmet,
                promotionHelmet = definition.PromotionHelmet,
                baseWeapon = definition.Weapon,
                promotionWeapon = definition.PromotionWeapon,
                baseShield = definition.Shield,
                promotionShield = definition.PromotionShield,
                baseBack = definition.Back,
                promotionBack = definition.PromotionBack,
                basePalette = definition.BasePalette,
                promotionPalette = definition.PromotionPalette,
                integerScale = scale,
                baseVisibleHeight = baseBounds.height * scale,
                promotionVisibleHeight = promotionBounds.height * scale,
                externalPropRequired = definition.ExternalPropRequired,
                externalPropReason = definition.ExternalPropReason,
            };
        }

        private static void Apply(CharacterBuilder builder, Definition definition, bool promotion)
        {
            builder.Body = promotion ? definition.PromotionBody : definition.Body;
            builder.Head = builder.Body;
            builder.Ears = "";
            builder.Eyes = builder.Body;
            builder.Mouth = "";
            builder.Hair = "";
            builder.Armor = promotion ? definition.PromotionArmor : definition.Armor;
            builder.Helmet = promotion ? definition.PromotionHelmet : definition.Helmet;
            builder.Weapon = promotion ? definition.PromotionWeapon : definition.Weapon;
            builder.WeaponSecondary = "";
            builder.Firearm = "";
            builder.Shield = promotion ? definition.PromotionShield : definition.Shield;
            builder.Cape = "";
            builder.Back = promotion ? definition.PromotionBack : definition.Back;
            builder.Mask = "";
            builder.Horns = "";
        }

        private static void ApplyV5(CharacterBuilder builder, Definition definition, bool promotion)
        {
            Apply(builder, definition, promotion);
            if (definition.BaseId != "wolf_tamer") return;
            builder.Head = "Human";
            builder.Ears = "Human";
            builder.Eyes = "Human";
            builder.Hair = "Hair1#F3D255";
            builder.Helmet = "";
        }

        private static void ApplyCandidate(CharacterBuilder builder, Definition definition, string family, bool promotion)
        {
            Apply(builder, definition, promotion);
            if (family != "wolf") return;
            builder.Head = "Human";
            builder.Ears = "Human";
            builder.Eyes = "Human";
            builder.Hair = "Hair1#F3D255";
            builder.Helmet = "";
        }

        private static Color[] CaptureFrameByKey(Texture2D source, string frameKey)
        {
            if (!CharacterBuilder.Layout.TryGetValue(frameKey, out var layout)) throw new InvalidOperationException("Unknown review frame: " + frameKey);
            if (!source || source.width < layout[0] + FrameSize || source.height < layout[1] + FrameSize) throw new InvalidOperationException("CharacterBuilder output is smaller than " + frameKey + ".");
            return source.GetPixels(layout[0], layout[1], FrameSize, FrameSize);
        }

        private readonly struct CandidateMetrics
        {
            public readonly int OpaquePixels;
            public readonly int SideMass;
            public readonly int HeadCorePixels;
            public readonly int MinX;
            public readonly int MaxX;
            public readonly int CentroidX1000;

            public CandidateMetrics(int opaquePixels, int sideMass, int headCorePixels, int minX, int maxX, int centroidX1000)
            {
                OpaquePixels = opaquePixels;
                SideMass = sideMass;
                HeadCorePixels = headCorePixels;
                MinX = minX;
                MaxX = maxX;
                CentroidX1000 = centroidX1000;
            }
        }

        private static CandidateMetrics GetCandidateMetrics(Color[] pixels, int width, int height)
        {
            var opaque = 0;
            var left = 0;
            var right = 0;
            var head = 0;
            var minX = width;
            var maxX = -1;
            var weightedX = 0L;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[x + y * width].a <= 0) continue;
                    opaque++;
                    weightedX += x;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    if (x < width / 3) left++;
                    if (x >= width * 2 / 3) right++;
                    if (x >= width / 3 && x < width * 2 / 3 && y >= height / 2) head++;
                }
            }
            return new CandidateMetrics(opaque, Mathf.Max(left, right), head, minX, maxX, opaque == 0 ? 0 : Mathf.RoundToInt((float)weightedX * 1000f / opaque));
        }

        private static CandidateMetrics GetCandidatePreviewMetrics(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(texture, File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid candidate preview: " + path);
                var pixels32 = texture.GetPixels32();
                var pixels = new Color[pixels32.Length];
                for (var i = 0; i < pixels32.Length; i++) pixels[i] = pixels32[i];
                return GetCandidateMetrics(pixels, texture.width, texture.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ApplyCandidateMetrics(CompanionArtThreeFamilyCandidateEntry entry, CandidateMetrics baseMetrics, CandidateMetrics promotionMetrics)
        {
            entry.baseOpaquePixels = baseMetrics.OpaquePixels;
            entry.promotionOpaquePixels = promotionMetrics.OpaquePixels;
            entry.baseSideMass = baseMetrics.SideMass;
            entry.promotionSideMass = promotionMetrics.SideMass;
            entry.baseHeadCorePixels = baseMetrics.HeadCorePixels;
            entry.promotionHeadCorePixels = promotionMetrics.HeadCorePixels;
            entry.baseMinX = baseMetrics.MinX;
            entry.baseMaxX = baseMetrics.MaxX;
            entry.promotionMinX = promotionMetrics.MinX;
            entry.promotionMaxX = promotionMetrics.MaxX;
            entry.baseCentroidX1000 = baseMetrics.CentroidX1000;
            entry.promotionCentroidX1000 = promotionMetrics.CentroidX1000;
            entry.silhouetteDelta = Mathf.Abs(promotionMetrics.OpaquePixels - baseMetrics.OpaquePixels);
        }

        private static void ValidateCandidateDefinitions(SpriteCollection collection)
        {
            var layers = collection.Layers.ToDictionary(i => i.Name, StringComparer.Ordinal);
            var options = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in CandidateDefinitions)
            {
                if (!options.Add(candidate.OptionId)) throw new InvalidOperationException("Duplicate candidate option ID.");
                if (!candidate.ReviewFrame.StartsWith("Idle_", StringComparison.Ordinal) || !CharacterBuilder.Layout.ContainsKey(candidate.ReviewFrame)) throw new InvalidOperationException("Missing candidate review frame: " + candidate.ReviewFrame);
                ValidatePart(layers, "Body", candidate.Pair.Body);
                ValidatePart(layers, "Armor", candidate.Pair.Armor);
                ValidatePart(layers, "Helmet", candidate.Pair.Helmet);
                ValidatePart(layers, "Weapon", candidate.Pair.Weapon);
                ValidatePart(layers, "Shield", candidate.Pair.Shield);
                ValidatePart(layers, "Back", candidate.Pair.Back);
                ValidatePart(layers, "Body", candidate.Pair.PromotionBody);
                ValidatePart(layers, "Armor", candidate.Pair.PromotionArmor);
                ValidatePart(layers, "Helmet", candidate.Pair.PromotionHelmet);
                ValidatePart(layers, "Weapon", candidate.Pair.PromotionWeapon);
                ValidatePart(layers, "Shield", candidate.Pair.PromotionShield);
                ValidatePart(layers, "Back", candidate.Pair.PromotionBack);
            }
            if (CandidateDefinitions.Length != 9 || options.Count != 9) throw new InvalidOperationException("Expected exactly nine candidate rows.");
        }

        private static void ValidateDefinitions(SpriteCollection collection)
        {
            ValidateDefinitions(collection, Definitions, 12);
        }

        private static void ValidateDefinitions(SpriteCollection collection, Definition[] definitions, int expectedDefinitionCount)
        {
            var layers = collection.Layers.ToDictionary(i => i.Name, StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (!ids.Add(definition.BaseId) || !ids.Add(definition.PromotionId)) throw new InvalidOperationException("Duplicate companion ID.");
                ValidatePart(layers, "Body", definition.Body);
                ValidatePart(layers, "Armor", definition.Armor);
                ValidatePart(layers, "Helmet", definition.Helmet);
                ValidatePart(layers, "Weapon", definition.Weapon);
                ValidatePart(layers, "Shield", definition.Shield);
                ValidatePart(layers, "Back", definition.Back);
                ValidatePart(layers, "Body", definition.PromotionBody);
                ValidatePart(layers, "Armor", definition.PromotionArmor);
                ValidatePart(layers, "Helmet", definition.PromotionHelmet);
                ValidatePart(layers, "Weapon", definition.PromotionWeapon);
                ValidatePart(layers, "Shield", definition.PromotionShield);
                ValidatePart(layers, "Back", definition.PromotionBack);
            }
            if (definitions.Length != expectedDefinitionCount || ids.Count != expectedDefinitionCount * 2) throw new InvalidOperationException("Expected exactly 12 base and 12 promotion IDs.");
        }

        private static void ValidatePart(Dictionary<string, Layer> layers, string layerName, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var name = value.Split('#')[0].Split('/')[0];
            if (!layers.TryGetValue(layerName, out var layer) || !layer.Textures.Any(i => i.name == name)) throw new InvalidOperationException($"Missing PixelFantasy part {layerName}/{name}.");
        }

        private static TMP_FontAsset CreateReviewFont(Font sourceFont)
        {
            var font = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.hideFlags = HideFlags.HideAndDontSave;
            return font;
        }

        private static Color[] CaptureIdleFrame(Texture2D source)
        {
            if (!source || source.width < FrameSize || source.height < FrameY + FrameSize) throw new InvalidOperationException("CharacterBuilder output is smaller than Idle_0.");
            return source.GetPixels(0, FrameY, FrameSize, FrameSize);
        }

        private static (int minX, int minY, int maxX, int maxY, int width, int height) GetBounds(Color[] pixels)
        {
            return GetBounds(pixels, FrameSize, FrameSize);
        }

        private static (int minX, int minY, int maxX, int maxY, int width, int height) GetBounds(Color[] pixels, int width, int height)
        {
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[x + y * width].a <= 0) continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            if (maxX < 0) throw new InvalidOperationException("Idle_0 frame is empty.");
            return (minX, minY, maxX, maxY, maxX - minX + 1, maxY - minY + 1);
        }

        private static int ChooseScale(Color[] baseFrame, Color[] promotionFrame)
        {
            var baseBounds = GetBounds(baseFrame);
            var promotionBounds = GetBounds(promotionFrame);
            var maxHeight = Mathf.Max(baseBounds.height, promotionBounds.height);
            var maxWidth = Mathf.Max(baseBounds.width, promotionBounds.width);
            var best = 1;
            var bestScore = int.MaxValue;
            for (var scale = 1; scale <= 6; scale++)
            {
                var height = maxHeight * scale;
                var width = maxWidth * scale;
                if (width > 176 || height > 140) continue;
                var score = Mathf.Abs(height - 120) + (height < 90 ? 200 : 0);
                if (score < bestScore)
                {
                    best = scale;
                    bestScore = score;
                }
            }
            return best;
        }

        private static Texture2D CreatePreview(Color[] pixels, int scale)
        {
            return CreatePreviewFromPixels(pixels, FrameSize, FrameSize, scale);
        }

        private static Texture2D CreatePreviewFromPixels(Color[] pixels, int width, int height, int scale)
        {
            var bounds = GetBounds(pixels, width, height);
            var preview = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var result = new Color32[PreviewSize * PreviewSize];
            var originX = (PreviewSize - bounds.width * scale) / 2;
            var originY = FootBaseline;
            for (var y = bounds.minY; y <= bounds.maxY; y++)
            {
                for (var x = bounds.minX; x <= bounds.maxX; x++)
                {
                    var color = pixels[x + y * width];
                    if (color.a <= 0) continue;
                    for (var dy = 0; dy < scale; dy++)
                    {
                        for (var dx = 0; dx < scale; dx++) result[originX + (x - bounds.minX) * scale + dx + (originY + (y - bounds.minY) * scale + dy) * PreviewSize] = color;
                    }
                }
            }
            preview.SetPixels32(result);
            preview.Apply(false, false);
            return preview;
        }

        private static void WritePreview(Color[] pixels, string path, Texture2D sheet, int row, int x, TMP_FontAsset font, string koreanLabel, string id, int scale, bool externalPropRequired)
        {
            var preview = CreatePreview(pixels, scale);
            File.WriteAllBytes(path, preview.EncodeToPNG());
            var top = 150 + row * RowHeight;
            FillTopRect(sheet, x - 10, top - 10, PreviewSize + 20, PreviewSize + 20, CardFill);
            DrawTopBorder(sheet, x - 10, top - 10, PreviewSize + 20, PreviewSize + 20, CardBorder);
            CompositeTopLeft(sheet, preview, x, top);
            ComposeLabel(sheet, font, koreanLabel + "\n" + id, x - 50, top - 78, Navy);
            if (externalPropRequired) ComposeLabel(sheet, font, "외부 소품 필요", x - 50, top + PreviewSize + 14, Orange);
            UnityEngine.Object.DestroyImmediate(preview);
        }

        private static Texture2D CreateSheet(TMP_FontAsset font)
        {
            var sheet = new Texture2D(SheetWidth, 86 + Definitions.Length * RowHeight, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            sheet.SetPixels32(Enumerable.Repeat(new Color32(248, 252, 255, 255), sheet.width * sheet.height).ToArray());
            return sheet;
        }

        private static Texture2D RenderLabel(TMP_FontAsset font, string text, Color32 color)
        {
            var root = new GameObject("CompanionArtLabel", typeof(Canvas)) { hideFlags = HideFlags.HideAndDontSave };
            var cameraObject = new GameObject("CompanionArtLabelCamera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var canvas = root.GetComponent<Canvas>();
            var camera = cameraObject.GetComponent<Camera>();
            root.layer = LabelLayer;
            cameraObject.layer = LabelLayer;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.gameObject.AddComponent<CanvasScaler>();
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)) { hideFlags = HideFlags.HideAndDontSave };
            labelObject.layer = LabelLayer;
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 22;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = 22;
            label.color = color;
            label.text = text;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4, 0);
            rect.offsetMax = new Vector2(-4, 0);

            var renderTexture = new RenderTexture(LabelWidth, LabelHeight, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, antiAliasing = 1 };
            camera.targetTexture = renderTexture;
            camera.orthographic = true;
            camera.orthographicSize = LabelHeight / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << LabelLayer;
            camera.Render();
            var result = new Texture2D(LabelWidth, LabelHeight, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            result.ReadPixels(new Rect(0, 0, LabelWidth, LabelHeight), 0, 0);
            result.Apply(false, false);
            var labelPixels = result.GetPixels32();
            for (var i = 0; i < labelPixels.Length; i++)
            {
                if (labelPixels[i].r <= 2 && labelPixels[i].g <= 2 && labelPixels[i].b <= 2)
                {
                    labelPixels[i].a = 0;
                }
            }
            result.SetPixels32(labelPixels);
            result.Apply(false, false);
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(root);
            return result;
        }

        private static void ComposeLabel(Texture2D sheet, TMP_FontAsset font, string text, int x, int top, Color32 color)
        {
            var label = RenderLabel(font, text, color);
            CompositeTopLeft(sheet, label, x, top);
            UnityEngine.Object.DestroyImmediate(label);
        }

        private static void CompositeTopLeft(Texture2D sheet, Texture2D overlay, int x, int top)
        {
            sheet.Apply(false, false);
            var sheetPixels = sheet.GetPixels32();
            var overlayPixels = overlay.GetPixels32();
            var baseY = sheet.height - top - overlay.height;
            for (var yy = 0; yy < overlay.height; yy++)
            {
                var destinationY = baseY + yy;
                if (destinationY < 0 || destinationY >= sheet.height) continue;
                for (var xx = 0; xx < overlay.width; xx++)
                {
                    var destinationX = x + xx;
                    if (destinationX < 0 || destinationX >= sheet.width) continue;
                    var source = overlayPixels[xx + yy * overlay.width];
                    if (source.a == 0) continue;
                    var destinationIndex = destinationX + destinationY * sheet.width;
                    if (source.a == 255)
                    {
                        sheetPixels[destinationIndex] = source;
                        continue;
                    }
                    var sourceAlpha = source.a / 255f;
                    var destinationAlpha = sheetPixels[destinationIndex].a / 255f;
                    var outputAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
                    if (outputAlpha <= 0f) continue;
                    var destination = sheetPixels[destinationIndex];
                    sheetPixels[destinationIndex] = new Color32(
                        (byte)((source.r * sourceAlpha + destination.r * destinationAlpha * (1f - sourceAlpha)) / outputAlpha),
                        (byte)((source.g * sourceAlpha + destination.g * destinationAlpha * (1f - sourceAlpha)) / outputAlpha),
                        (byte)((source.b * sourceAlpha + destination.b * destinationAlpha * (1f - sourceAlpha)) / outputAlpha),
                        (byte)(outputAlpha * 255f));
                }
            }
            sheet.SetPixels32(sheetPixels);
        }

        private static void NormalizeSheetBackground(Texture2D sheet)
        {
            var pixels = sheet.GetPixels32();
            for (var y = 0; y < sheet.height; y++)
            {
                for (var x = 0; x < sheet.width; x++)
                {
                    var index = x + y * sheet.width;
                    var color = pixels[index];
                    var top = sheet.height - 1 - y;
                    var cardFill = false;
                    for (var row = 0; row < Definitions.Length; row++)
                    {
                        var cardTop = 150 + row * RowHeight;
                        if (IsInsideCard(x, top, BaseX, cardTop) || IsInsideCard(x, top, PromotionX, cardTop))
                        {
                            cardFill = true;
                            break;
                        }
                    }
                    if (cardFill && color.a == 0)
                    {
                        pixels[index] = CardFill;
                    }
                    else if (color.a == 0)
                    {
                        pixels[index] = new Color32(248, 252, 255, 255);
                    }
                }
            }
            sheet.SetPixels32(pixels);
        }

        private static bool IsInsideCard(int x, int top, int centerX, int cardTop)
        {
            var cardLeft = centerX - 10 - PreviewSize / 2;
            return x >= cardLeft && x < cardLeft + PreviewSize + 20 && top >= cardTop - 10 && top < cardTop - 10 + PreviewSize + 20;
        }

        private static void FillTopRect(Texture2D texture, int x, int top, int width, int height, Color32 color)
        {
            for (var yy = 0; yy < height; yy++)
            {
                for (var xx = 0; xx < width; xx++) SetTopPixel(texture, x + xx, top + yy, color);
            }
        }

        private static void DrawTopBorder(Texture2D texture, int x, int top, int width, int height, Color32 color)
        {
            for (var xx = 0; xx < width; xx++)
            {
                SetTopPixel(texture, x + xx, top, color);
                SetTopPixel(texture, x + xx, top + height - 1, color);
            }
            for (var yy = 0; yy < height; yy++)
            {
                SetTopPixel(texture, x, top + yy, color);
                SetTopPixel(texture, x + width - 1, top + yy, color);
            }
        }

        private static void SetTopPixel(Texture2D texture, int x, int top, Color32 color)
        {
            if (x < 0 || top < 0 || x >= texture.width || top >= texture.height) return;
            texture.SetPixel(x, texture.height - 1 - top, color);
        }
    }
}
