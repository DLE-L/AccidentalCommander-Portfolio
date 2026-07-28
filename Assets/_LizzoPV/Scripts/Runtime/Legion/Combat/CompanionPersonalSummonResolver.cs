using System;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionPersonalSummonSetup
    {
        public readonly string SummonId;
        public readonly string OwnerUnitId;
        public readonly int CountableKillThreshold;
        public readonly int BaseActiveCap;
        public readonly int PromotedActiveCap;
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
            CountableKillThreshold = data.CountableKillThreshold;
            BaseActiveCap = data.BaseActiveCap;
            PromotedActiveCap = data.PromotedActiveCap;
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

        public int ResolveActiveCap(bool isPromoted)
        {
            return isPromoted ? PromotedActiveCap : BaseActiveCap;
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
            CompanionSummonData summon = _data.GetCompanionSummon(PersonalSkeletonId)
                ?? throw new InvalidOperationException("Necromancer personal skeleton summon missing.");

            if (summon.OwnerUnitId != baseUnitId
                || summon.SkillId != profile.SecondarySkillId
                || profile.SecondaryRuleId != "personal_thrall_countable_kills"
                || summon.CountableKillThreshold != 15
                || summon.BaseActiveCap != 1
                || summon.PromotedActiveCap != 2
                || summon.Hp != 18
                || summon.Damage != 4
                || summon.AttackInterval != 1.3f
                || summon.Range != 1.0f
                || summon.MoveSpeed != 2.7f
                || summon.AiScanInterval != 0.2f
                || summon.LifetimeRuleId != "battle_end_or_hp0"
                || summon.TargetRule != CombatTargetRule.Nearest
                || summon.Tags != "summon_object,companion_tag=false,no_family_tag"
                || summon.BossRuleId != "normal_target"
                || summon.StackRuleId != "separate_owner_cap"
                || summon.ResetRuleId != "battle_end"
                || summon.RemoteConfigKey != "rc_personal_skeleton_stats"
                || summon.DistinctFromSummonId != "UNIT_SYNERGY_SKELETON_01")
            {
                throw new InvalidOperationException("Necromancer personal skeleton summon data invalid.");
            }

            setup = new CompanionPersonalSummonSetup(summon);
            return true;
        }
    }

    public sealed class CountableKillThresholdState
    {
        private int _threshold;
        private int _activeCap;
        private int _pendingCountableKills;
        private int _activeCount;

        public int PendingCountableKills => _pendingCountableKills;
        public int ActiveCount => _activeCount;

        public void Configure(int threshold, int activeCap)
        {
            _threshold = threshold;
            _activeCap = activeCap;
            Reset();
        }

        public void Reconfigure(int threshold, int activeCap)
        {
            _threshold = threshold;
            _activeCap = activeCap;
            if (_activeCount > _activeCap)
                _activeCount = _activeCap;
        }

        public bool RecordKill(bool isCountable)
        {
            if (isCountable == false || _threshold <= 0 || _activeCap <= 0 || _activeCount >= _activeCap)
                return false;

            _pendingCountableKills++;
            if (_pendingCountableKills < _threshold)
                return false;

            _pendingCountableKills = 0;
            _activeCount++;
            return true;
        }

        public bool TryConsumeKill(bool isCountable)
        {
            if (isCountable == false || _threshold <= 0 || _activeCap <= 0)
                return false;

            _pendingCountableKills++;
            if (_pendingCountableKills < _threshold)
                return false;

            _pendingCountableKills = 0;
            return true;
        }

        public bool ReleaseOne()
        {
            if (_activeCount <= 0)
                return false;

            _activeCount--;
            return true;
        }

        public void Reset()
        {
            _pendingCountableKills = 0;
            _activeCount = 0;
        }
    }
}
