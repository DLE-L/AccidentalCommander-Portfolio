using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Lizzo.PV.Data
{
    public interface IDataProvider
    {
        bool IsInitialized { get; }
        RunTuningData RunTuning { get; }
        IReadOnlyList<CompanionRosterData> CompanionRoster { get; }
        IReadOnlyList<CompanionCardLocalizationData> CompanionCardLocalizations { get; }
        IReadOnlyList<PassiveData> Passives { get; }
        IReadOnlyList<CompanionCombatProfileData> CompanionCombatProfiles { get; }
        IReadOnlyList<CombatEffectData> CombatEffects { get; }
        IReadOnlyList<CompanionSummonData> CompanionSummons { get; }
        IReadOnlyList<SynergyDamageData> SynergyDamages { get; }
        IReadOnlyList<SynergyEffectData> SynergyEffects { get; }
        IReadOnlyList<SynergySummonData> SynergySummons { get; }
        UniTask<DataLoadResult> InitializeAsync(CancellationToken cancellationToken = default);
        UnitData GetUnit(string id);
        CompanionRosterData GetCompanionRoster(string unitId);
        CompanionCardLocalizationData GetCompanionCardLocalization(string unitId);
        PassiveData GetPassive(string passiveId);
        CompanionPromotionData GetCompanionPromotion(string profileId);
        CompanionCombatProfileData GetCompanionCombatProfile(string unitId);
        CombatEffectData GetCombatEffect(string effectId);
        CompanionSummonData GetCompanionSummon(string summonId);
        SynergyDamageData GetSynergyDamage(string damageId);
        SynergyEffectData GetSynergyEffect(string effectId);
        SynergySummonData GetSynergySummon(string summonId);
        SkillData GetSkill(string id);
        EnemyData GetEnemy(string id);
        EnemyData GetEnemyByTemplateId(int templateId);
        SynergyData GetSynergy(string id);
        int GetLevelExp(int level);
        float GetEffectiveSpawnSeconds(EnemyData enemyData);
        float GetStage1SpawnBudget(float elapsedSeconds);
    }
}
