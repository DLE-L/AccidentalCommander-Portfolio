using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo/Presentation/Cleric Light Visuals")]
    public sealed class ClericLightVisualProfile : Lizzo.PV.Gameplay.Presentation.AttackHitVisualProfile
    {
        [SerializeField] private ClericLightEffectView _outbound;
        [SerializeField] private ClericLightEffectView _return;
        public ClericLightEffectView Outbound => _outbound;
        public ClericLightEffectView Return => _return;
    }
}
