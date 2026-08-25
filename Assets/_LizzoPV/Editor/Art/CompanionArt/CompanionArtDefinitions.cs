namespace Lizzo.PV.EditorTools.Art.Companions
{
    public static partial class CompanionArtContactSheetGenerator
    {
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

        private static readonly Definition[] V6BaseDefinitions =
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

        private static readonly Definition[] V6Definitions =
        {
            new("shield_guard", "shield_captain", "방패병", "방패대장", "shield/sword/front tank", "Human", "IronKnight#2E6FB7", "IronKnightHelmet#F3D255", "IronSword#F8FCFF", "GuardianShield#F3D255", "", "Human", "IronKnight#2E6FB7", "CavalrymanHelmet#F3D255", "Longsword#F8FCFF", "RoyalGreatShield#F3D255", "", new[] { "allied blue", "steel", "gold" }, new[] { "allied blue", "steel", "gold" }, false, ""),
            V6BaseDefinitions[1],
            V6BaseDefinitions[2],
            V6BaseDefinitions[3],
            V6BaseDefinitions[4],
            V6BaseDefinitions[5],
            V6BaseDefinitions[6],
            V6BaseDefinitions[7],
            new("wolf_tamer", "beast_commander", "늑대 조련사", "야수 지휘관", "beast owner/rear", "Human", "GuardianTunic#2E6FB7", "", "HunterKnife#D4E9F8", "", "", "Human", "GuardianTunic#0C2E44", "", "HunterKnife#D4E9F8", "", "LargeBackpack#D4E9F8", new[] { "allied blue", "beast brown/teal" }, new[] { "allied blue", "beast brown/teal" }, false, "GreyWolf is reviewed separately as a child summon/support visual, not a squad-slot owner."),
            V6BaseDefinitions[9],
            V6BaseDefinitions[10],
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












    }
}
