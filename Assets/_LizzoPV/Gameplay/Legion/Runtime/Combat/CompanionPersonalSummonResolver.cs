using System;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionPersonalSummonSetup
    {
        public readonly string SummonId;
        public readonly string OwnerUnitId;
        public readonly int Hp;
        public readonly int Damage;
        public readonly float AttackInterval;
        public readonly float Range;
        public readonly float MoveSpeed;
        public readonly float AiScanInterval;
        public readonly string LifetimeRuleId;
        public readonly CombatTargetRule TargetRule;
        public readonly string Tags;
        public readonly string BossRuleId;
        public readonly string StackRuleId;
        public readonly string ResetRuleId;
        public readonly string RemoteConfigKey;
        public readonly string DistinctFromSummonId;

        public CompanionPersonalSummonSetup(CompanionSummonData data)
        {
            SummonId = data.Id;
            OwnerUnitId = data.OwnerUnitId;
            Hp = data.Hp;
            Damage = data.Damage;
            AttackInterval = data.AttackInterval;
            Range = data.Range;
            MoveSpeed = data.MoveSpeed;
            AiScanInterval = data.AiScanInterval;
            LifetimeRuleId = data.LifetimeRuleId;
            TargetRule = data.TargetRule;
            Tags = data.Tags;
            BossRuleId = data.BossRuleId;
            StackRuleId = data.StackRuleId;
            ResetRuleId = data.ResetRuleId;
            RemoteConfigKey = data.RemoteConfigKey;
            DistinctFromSummonId = data.DistinctFromSummonId;
        }

    }

    public sealed class CompanionPersonalSummonResolver
    {
        private const string NecromancerId = "necromancer";
        private const string PersonalSkeletonId = "UNIT_PERSONAL_SKELETON_01";

        private readonly IDataProvider _data;

        public CompanionPersonalSummonResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, out CompanionPersonalSummonSetup setup)
        {
            if (baseUnitId != NecromancerId)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId)
                ?? throw new InvalidOperationException("Necromancer profile missing.");
            CompanionRosterData roster = _data.GetCompanionRoster(baseUnitId)
                ?? throw new InvalidOperationException("Necromancer roster contract missing.");
            CompanionSummonData summon = _data.GetCompanionSummon(PersonalSkeletonId)
                ?? throw new InvalidOperationException("Dark Ritualist undead summon missing.");

            if (summon.OwnerUnitId != baseUnitId
                || roster.PromotionAction != CompanionPromotionActionKind.CursedDeathUndeadRitual
                || roster.PromotionContractStage != CompanionCombatContractStage.RuntimeConnected
                || profile.PromotionProfileId != "dark_ritualist"
                || summon.SkillId != "skill_dark_ritualist_ritual"
                || summon.Hp != 18
                || summon.Damage != 4
                || summon.AttackInterval != 1.3f
                || summon.Range != 1.0f
                || summon.MoveSpeed != 2.7f
                || summon.AiScanInterval != 0.2f
                || summon.LifetimeRuleId != "timed_group_or_hp0"
                || summon.TargetRule != CombatTargetRule.Nearest
                || summon.Tags != "summon_object,companion_tag=false,no_family_tag"
                || summon.BossRuleId != "normal_target"
                || summon.StackRuleId != "single_temporary_group"
                || summon.ResetRuleId != "battle_end"
                || summon.RemoteConfigKey != "rc_dark_ritualist_undead_stats"
                || summon.DistinctFromSummonId != "UNIT_SYNERGY_SKELETON_01")
            {
                throw new InvalidOperationException("Dark Ritualist undead summon data invalid.");
            }

            setup = new CompanionPersonalSummonSetup(summon);
            return true;
        }
    }

}
