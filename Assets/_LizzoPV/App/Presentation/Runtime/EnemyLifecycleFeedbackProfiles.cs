using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Enemy Spawn Feedback Profile", fileName = "EnemySpawnFeedbackProfile")]
    public sealed class EnemySpawnFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private VfxAssetId _spawnMarkerVfxId;
        [SerializeField] private MotionAssetId _spawnMotionId;
        [SerializeField] private VfxAssetId _spawnVfxId;
        [SerializeField] private AudioAssetId _spawnSfxId;
        [SerializeField] private AudioAssetId _activeSfxId;
        [SerializeField] private MotionAssetId _visualRevealMotionId;

        public VfxAssetId SpawnMarkerVfxId => _spawnMarkerVfxId;
        public MotionAssetId SpawnMotionId => _spawnMotionId;
        public VfxAssetId SpawnVfxId => _spawnVfxId;
        public AudioAssetId SpawnSfxId => _spawnSfxId;
        public AudioAssetId ActiveSfxId => _activeSfxId;
        public MotionAssetId VisualRevealMotionId => _visualRevealMotionId;

        public bool TryValidate(out string issue) => TryValidate(false, out issue);

        public bool TryValidate(bool requiresSpawnSfx, out string issue)
        {
            if (!WorldFeedbackCoreValidation.Require(_spawnMarkerVfxId, nameof(SpawnMarkerVfxId), out issue)
                || !WorldFeedbackCoreValidation.Require(_spawnMotionId, nameof(SpawnMotionId), out issue)
                || !WorldFeedbackCoreValidation.Require(_spawnVfxId, nameof(SpawnVfxId), out issue)
                || !WorldFeedbackCoreValidation.Require(_visualRevealMotionId, nameof(VisualRevealMotionId), out issue))
            {
                return false;
            }

            if (requiresSpawnSfx && !WorldFeedbackCoreValidation.Require(_spawnSfxId, nameof(SpawnSfxId), out issue))
            {
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            VfxAssetId spawnMarkerVfxId,
            MotionAssetId spawnMotionId,
            VfxAssetId spawnVfxId,
            AudioAssetId spawnSfxId,
            AudioAssetId activeSfxId,
            MotionAssetId visualRevealMotionId)
        {
            _spawnMarkerVfxId = spawnMarkerVfxId;
            _spawnMotionId = spawnMotionId;
            _spawnVfxId = spawnVfxId;
            _spawnSfxId = spawnSfxId;
            _activeSfxId = activeSfxId;
            _visualRevealMotionId = visualRevealMotionId;
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
