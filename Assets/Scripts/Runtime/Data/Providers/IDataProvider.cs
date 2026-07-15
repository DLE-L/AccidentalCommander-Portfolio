using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lizzo.PV.Data
{
    public interface IDataProvider
    {
        bool IsInitialized { get; }
        RunTuningData RunTuning { get; }
        UniTask<DataLoadResult> InitializeAsync(CancellationToken cancellationToken = default);
        UnitData GetUnit(string id);
        SkillData GetSkill(string id);
        EnemyData GetEnemy(string id);
        EnemyData GetEnemyByTemplateId(int templateId);
        SynergyData GetSynergy(string id);
        int GetLevelExp(int level);
        float GetEffectiveSpawnSeconds(EnemyData enemyData);
        float GetStage1SpawnBudget(float elapsedSeconds);
    }
}