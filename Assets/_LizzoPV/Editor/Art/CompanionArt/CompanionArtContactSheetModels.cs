using System;

namespace Lizzo.PV.EditorTools.Art.Companions
{
    [Serializable]
    public sealed class CompanionArtPairEntry
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
    public sealed class CompanionArtSupportEntry
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
        public CompanionArtPairEntry[] pairs;
        public CompanionArtSupportEntry[] support;
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

}
