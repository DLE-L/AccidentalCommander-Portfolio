using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Audio Profile", fileName = "GameplayAudioPresentationProfile")]
    public sealed class GameplayAudioPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private AudioAssetId _gameplayBgmId;
        [SerializeField] private AudioAssetId _bossBgmId;
        [SerializeField] private AudioAssetId _stageAmbienceLoopSfxId;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.18f;
        [SerializeField, Range(0f, 1f)] private float _ambienceVolume = 0.12f;
        [SerializeField, Min(0f)] private float _gameplayBossCrossFadeSeconds;
        [SerializeField, Min(0f)] private float _resultFadeSeconds;

        public AudioAssetId GameplayBgmId => _gameplayBgmId;
        public AudioAssetId BossBgmId => _bossBgmId;
        public AudioAssetId StageAmbienceLoopSfxId => _stageAmbienceLoopSfxId;
        public float BgmVolume => _bgmVolume;
        public float AmbienceVolume => _ambienceVolume;
        public float GameplayBossCrossFadeSeconds => _gameplayBossCrossFadeSeconds;
        public float ResultFadeSeconds => _resultFadeSeconds;

        public bool TryValidate(out string issue)
        {
            if (!(_bgmVolume >= 0f && _bgmVolume <= 1f)
                || !(_ambienceVolume >= 0f && _ambienceVolume <= 1f))
            {
                issue = "BGM and ambience volumes must be between zero and one.";
                return false;
            }
            if (!GameplayCoreProfileValidation.Require(_gameplayBgmId, nameof(GameplayBgmId), out issue)
                || !GameplayCoreProfileValidation.Require(_bossBgmId, nameof(BossBgmId), out issue))
                return false;
            if (_gameplayBossCrossFadeSeconds < 0f)
            {
                issue = "GameplayBossCrossFadeSeconds cannot be negative.";
                return false;
            }
            if (_resultFadeSeconds < 0f)
            {
                issue = "ResultFadeSeconds cannot be negative.";
                return false;
            }
            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(AudioAssetId gameplayBgmId, AudioAssetId bossBgmId,
            AudioAssetId stageAmbienceLoopSfxId, float gameplayBossCrossFadeSeconds, float resultFadeSeconds)
        {
            _gameplayBgmId = gameplayBgmId;
            _bossBgmId = bossBgmId;
            _stageAmbienceLoopSfxId = stageAmbienceLoopSfxId;
            _gameplayBossCrossFadeSeconds = gameplayBossCrossFadeSeconds;
            _resultFadeSeconds = resultFadeSeconds;
        }
#endif
    }
}
