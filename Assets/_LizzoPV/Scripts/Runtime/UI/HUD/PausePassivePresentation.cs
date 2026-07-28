using UnityEngine;

namespace Lizzo.PV.UI
{
    public readonly struct PausePassivePresentation
    {
        public PausePassivePresentation(Sprite icon)
        {
            Icon = icon;
        }

        public Sprite Icon { get; }
    }
}
