using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Enemy Attack Feedback Profile", fileName = "EnemyAttackFeedbackProfile")]
    public sealed class EnemyAttackFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private WindupFeedback _windupFeedback; [SerializeField] private TelegraphFeedback _telegraphFeedback;
        [SerializeField] private EnemyProjectileFeedback _enemyProjectileFeedback; [SerializeField] private EnemyImpactFeedback _enemyImpactFeedback;
        [SerializeField] private PersistentHazardFeedback _persistentHazardFeedback;
        public WindupFeedback WindupFeedback=>_windupFeedback; public TelegraphFeedback TelegraphFeedback=>_telegraphFeedback;
        public EnemyProjectileFeedback EnemyProjectileFeedback=>_enemyProjectileFeedback; public EnemyImpactFeedback EnemyImpactFeedback=>_enemyImpactFeedback;
        public PersistentHazardFeedback PersistentHazardFeedback=>_persistentHazardFeedback;
        public bool TryValidate(out string issue)
        {
            bool any=_windupFeedback.IsConfigured||_telegraphFeedback.IsConfigured||_enemyProjectileFeedback.IsConfigured||_enemyImpactFeedback.IsConfigured||_persistentHazardFeedback.IsConfigured;
            if(!any){issue="Enemy attack feedback profile must configure at least one delivery group.";return false;}
            return _windupFeedback.TryValidate(out issue)&&_telegraphFeedback.TryValidate(out issue)&&_enemyProjectileFeedback.TryValidate(out issue)&&_enemyImpactFeedback.TryValidate(out issue)&&_persistentHazardFeedback.TryValidate(out issue);
        }
#if UNITY_EDITOR
        public void SetForEditor(WindupFeedback windup,TelegraphFeedback telegraph,EnemyProjectileFeedback projectile,EnemyImpactFeedback impact,PersistentHazardFeedback hazard)
        { _windupFeedback=windup;_telegraphFeedback=telegraph;_enemyProjectileFeedback=projectile;_enemyImpactFeedback=impact;_persistentHazardFeedback=hazard; }
#endif
    }
}
