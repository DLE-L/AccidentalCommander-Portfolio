using Lizzo.PV.Data;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private void SeedFallbackSkills()
        {
            AddSkill("commander_basic", "군단장 기본 공격", "single_target", 18, 0.8f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f);
            AddSkill("shield_push", "전방 짧은 밀침", "single_target_knockback", 6, 1.4f, 1.35f, 0.5f, 0.0f, 0.0f, 0.0f);
            AddSkill("shield_captain_push", "넓은 밀침 + 보호막 1회", "area_knockback", 12, 1.6f, 1.8f, 0.9f, 0.0f, 0.0f, 0.0f);
            AddSkill("sword_front_slash", "가까운 적 전방 베기", "front_slash", 12, 1.0f, 1.55f, 0.0f, 60.0f, 0.0f, 0.0f);
            AddSkill("cleric_heal", "가장 낮은 체력 대상 회복", "heal_lowest", 10, 4.0f, 4.0f, 0.0f, 0.0f, 0.0f, 0.0f);
            AddSkill("archer_far_shot", "멀리 있는 적 단일 화살", "farthest_target", 9, 0.9f, 5.5f, 0.0f, 0.0f, 0.0f, 0.0f);
            AddSkill("guard_squad_shield", "방패 진형 전개", "radial_shield_push", 12, 12.0f, 4.0f, 1.8f, 0.0f, 0.6f, 4.0f);
        }

        private void AddSkill(
            string id,
            string displayName,
            string skillKind,
            int power,
            float cooldown,
            float range,
            float knockback,
            float angle,
            float duration,
            float width)
        {
            Skills[id] = new SkillData
            {
                Id = id,
                DisplayName = displayName,
                SkillKind = skillKind,
                Power = power,
                Cooldown = cooldown,
                Range = range,
                Knockback = knockback,
                Angle = angle,
                Duration = duration,
                Width = width,
            };
        }
    }
}
