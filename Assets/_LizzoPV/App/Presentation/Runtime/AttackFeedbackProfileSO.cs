using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Attack Feedback Profile", fileName = "AttackFeedbackProfile")]
    public sealed class AttackFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private CastFeedback _castFeedback; [SerializeField] private ProjectileFeedback _projectileFeedback; [SerializeField] private AreaFeedback _areaFeedback;
        [SerializeField] private FieldFeedback _fieldFeedback; [SerializeField] private ChainFeedback _chainFeedback; [SerializeField] private ProxyFeedback _proxyFeedback;
        [SerializeField] private ReturningProjectileFeedback _returningProjectileFeedback; [SerializeField] private SelfFeedback _selfFeedback; [SerializeField] private ImpactFeedback _impactFeedback;
        public CastFeedback CastFeedback=>_castFeedback; public ProjectileFeedback ProjectileFeedback=>_projectileFeedback; public AreaFeedback AreaFeedback=>_areaFeedback;
        public FieldFeedback FieldFeedback=>_fieldFeedback; public ChainFeedback ChainFeedback=>_chainFeedback; public ProxyFeedback ProxyFeedback=>_proxyFeedback;
        public ReturningProjectileFeedback ReturningProjectileFeedback=>_returningProjectileFeedback; public SelfFeedback SelfFeedback=>_selfFeedback; public ImpactFeedback ImpactFeedback=>_impactFeedback;
        public bool TryValidate(out string issue)
        {
            bool any=_castFeedback.IsConfigured||_projectileFeedback.IsConfigured||_areaFeedback.IsConfigured||_fieldFeedback.IsConfigured||_chainFeedback.IsConfigured||_proxyFeedback.IsConfigured||_returningProjectileFeedback.IsConfigured||_selfFeedback.IsConfigured||_impactFeedback.IsConfigured;
            if(!any){issue="Attack feedback profile must configure at least one delivery group.";return false;}
            return _castFeedback.TryValidate(out issue)&&_projectileFeedback.TryValidate(out issue)&&_areaFeedback.TryValidate(out issue)&&_fieldFeedback.TryValidate(out issue)&&_chainFeedback.TryValidate(out issue)&&_proxyFeedback.TryValidate(out issue)&&_returningProjectileFeedback.TryValidate(out issue)&&_selfFeedback.TryValidate(out issue)&&_impactFeedback.TryValidate(out issue);
        }
#if UNITY_EDITOR
        public void SetForEditor(CastFeedback cast,ProjectileFeedback projectile,AreaFeedback area,FieldFeedback field,ChainFeedback chain,ProxyFeedback proxy,ReturningProjectileFeedback returning,SelfFeedback self,ImpactFeedback impact)
        { _castFeedback=cast;_projectileFeedback=projectile;_areaFeedback=area;_fieldFeedback=field;_chainFeedback=chain;_proxyFeedback=proxy;_returningProjectileFeedback=returning;_selfFeedback=self;_impactFeedback=impact; }
#endif
    }
}
