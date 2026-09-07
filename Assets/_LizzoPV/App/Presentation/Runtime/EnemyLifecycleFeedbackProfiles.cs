using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Enemy Spawn Feedback Profile", fileName = "EnemySpawnFeedbackProfile")]
    public sealed class EnemySpawnFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private AudioAssetId _spawnSfxId;

        public AudioAssetId SpawnSfxId => _spawnSfxId;

        public bool TryValidate(out string issue) => TryValidate(false, out issue);

        public bool TryValidate(bool requiresSpawnSfx, out string issue)
        {
            if (requiresSpawnSfx && !WorldFeedbackCoreValidation.Require(_spawnSfxId, nameof(SpawnSfxId), out issue))
            {
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(AudioAssetId spawnSfxId)
        {
            _spawnSfxId = spawnSfxId;
        }
#endif
    }

    [Serializable]
    public struct EnemySpawnFeedbackBinding
    {
        [SerializeField] private EnemyId _enemyId;
        [SerializeField] private EnemySpawnFeedbackProfileSO _profile;

        public EnemySpawnFeedbackBinding(EnemyId enemyId, EnemySpawnFeedbackProfileSO profile)
        {
            _enemyId = enemyId;
            _profile = profile;
        }

        public EnemyId EnemyId => _enemyId;
        public EnemySpawnFeedbackProfileSO Profile => _profile;
    }

    [Serializable]
    public struct EnemyDeathFeedbackBinding
    {
        [SerializeField] private EnemyId _enemyId;
        [SerializeField] private EnemyDeathFeedbackProfileSO _profile;

        public EnemyDeathFeedbackBinding(EnemyId enemyId, EnemyDeathFeedbackProfileSO profile)
        {
            _enemyId = enemyId;
            _profile = profile;
        }

        public EnemyId EnemyId => _enemyId;
        public EnemyDeathFeedbackProfileSO Profile => _profile;
    }
}
