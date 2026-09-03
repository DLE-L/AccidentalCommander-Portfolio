using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Run Result Presentation Set", fileName = "RunResultPresentationSet")]
    public sealed class RunResultPresentationSetSO : ScriptableObject
    {
        [SerializeField] private RunResultSharedPresentationProfileSO _sharedProfile;
        [SerializeField] private RunResultPresentationProfileSO _victoryProfile;
        [SerializeField] private RunResultPresentationProfileSO _failureProfile;
        [SerializeField] private RunResultPresentationProfileSO _abandonedProfile;

        public RunResultSharedPresentationProfileSO SharedProfile => _sharedProfile;
        public RunResultPresentationProfileSO VictoryProfile => _victoryProfile;
        public RunResultPresentationProfileSO FailureProfile => _failureProfile;
        public RunResultPresentationProfileSO AbandonedProfile => _abandonedProfile;

        public bool TryValidate(out string issue)
        {
            if (_sharedProfile == null || _victoryProfile == null || _failureProfile == null || _abandonedProfile == null)
            {
                issue = "RunResultPresentationSet requires Shared, Victory, Failure, and Abandoned Profiles.";
                return false;
            }
            return _sharedProfile.TryValidate(out issue)
                && _victoryProfile.TryValidate(out issue)
                && _failureProfile.TryValidate(out issue)
                && _abandonedProfile.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(RunResultSharedPresentationProfileSO sharedProfile,
            RunResultPresentationProfileSO victoryProfile, RunResultPresentationProfileSO failureProfile,
            RunResultPresentationProfileSO abandonedProfile)
        {
            _sharedProfile = sharedProfile;
            _victoryProfile = victoryProfile;
            _failureProfile = failureProfile;
            _abandonedProfile = abandonedProfile;
        }
#endif
    }
}
