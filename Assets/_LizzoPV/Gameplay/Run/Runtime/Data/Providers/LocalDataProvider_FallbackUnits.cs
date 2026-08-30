using UnityEngine;

namespace Lizzo.PV.Data
{
    public partial class LocalDataProvider
    {
        private void SeedFallbackUnits()
        {
            AddUnit("commander_01", "군단장", "commander_family", "commander", "commander_basic", string.Empty, string.Empty, 100, 18, 0, 0.8f, 2.0f, 1.0f, 0.0f, 2.2f, Color.white);
            AddUnit("shield_guard", "방패병", "shield_family", "tank,frontline,guard", "shield_push", string.Empty, "shield_captain", 80, 6, 0, 1.4f, 1.35f, 2.8f, 0.5f, 0.0f, new Color(0.25f, 0.55f, 1.0f, 1.0f));
            AddUnit("shield_captain", "방패대장", "shield_family", "tank,frontline,promoted,guard", "shield_captain_push", "shield_guard", string.Empty, 180, 12, 0, 1.6f, 1.8f, 2.6f, 0.9f, 0.0f, new Color(0.0f, 0.95f, 1.0f, 1.0f));
            AddUnit("sword_soldier", "검병", "sword_family", "melee,frontline,guard", "sword_front_slash", string.Empty, "sword_captain", 65, 12, 0, 1.0f, 1.55f, 3.0f, 0.0f, 0.0f, new Color(1.0f, 0.35f, 0.15f, 1.0f));
            AddUnit("cleric", "성직자", "cleric_family", "healer,support,guard", "cleric_heal", string.Empty, "light_guide", 55, 0, 10, 4.0f, 4.0f, 2.7f, 0.0f, 0.0f, new Color(0.25f, 1.0f, 0.45f, 1.0f));
            AddUnit("archer", "궁수", "ranged_family", "ranged,single_target", "archer_far_shot", string.Empty, "marksman", 45, 9, 0, 0.9f, 5.5f, 2.9f, 0.0f, 0.0f, new Color(1.0f, 0.9f, 0.2f, 1.0f));
        }

        private void AddUnit(
            string id,
            string displayName,
            string familyTags,
            string roleTags,
            string skillId,
            string promotionSource,
            string promotionResult,
            int hp,
            int attack,
            int heal,
            float cooldown,
            float range,
            float moveSpeed,
            float knockback,
            float absorbRange,
            Color color)
        {
            Units[id] = new UnitData
            {
                Id = id,
                DisplayName = displayName,
                FamilyTags = familyTags,
                RoleTags = roleTags,
                SkillId = skillId,
                PromotionSource = promotionSource,
                PromotionResult = promotionResult,
                Hp = hp,
                Attack = attack,
                Heal = heal,
                Cooldown = cooldown,
                Range = range,
                MoveSpeed = moveSpeed,
                Knockback = knockback,
                AbsorbRange = absorbRange,
                Color = color,
            };
        }
    }
}
