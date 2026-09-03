using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Run Outcome World Feedback Profile", fileName = "RunOutcomeWorldFeedbackProfile")]
    public sealed class RunOutcomeWorldFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private VictoryWorldFeedback _victoryWorldFeedback; [SerializeField] private FailureWorldFeedback _failureWorldFeedback; [SerializeField] private AbandonedWorldFeedback _abandonedWorldFeedback;
        public VictoryWorldFeedback VictoryWorldFeedback=>_victoryWorldFeedback; public FailureWorldFeedback FailureWorldFeedback=>_failureWorldFeedback; public AbandonedWorldFeedback AbandonedWorldFeedback=>_abandonedWorldFeedback;
        public bool TryValidate(out string issue)=>_victoryWorldFeedback.TryValidate(out issue)&&_failureWorldFeedback.TryValidate(out issue)&&_abandonedWorldFeedback.TryValidate(out issue);
#if UNITY_EDITOR
        public void SetForEditor(VictoryWorldFeedback victory,FailureWorldFeedback failure,AbandonedWorldFeedback abandoned){_victoryWorldFeedback=victory;_failureWorldFeedback=failure;_abandonedWorldFeedback=abandoned;}
#endif
    }
}
