using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo/Presentation/Attack Hit Visual")]
    public class AttackHitVisualProfile : ScriptableObject
    {
        [SerializeField] private VfxWrapperInstance _hit;
        public VfxWrapperInstance Hit => _hit;
    }
}
