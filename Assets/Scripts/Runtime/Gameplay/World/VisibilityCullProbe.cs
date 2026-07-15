using UnityEngine;

namespace Lizzo.PV.Gameplay.World
{
    public sealed class VisibilityCullProbe : MonoBehaviour
    {
        public global::IVisibilityCullTarget Target { get; private set; }

        public void Bind(global::IVisibilityCullTarget target)
        {
            Target = target;
        }
    }
}
