using UnityEngine;

namespace Lizzo.PV.UI
{
    public readonly struct PauseCompanionPresentation
    {
        public PauseCompanionPresentation(Sprite icon, int currentCount)
        {
            Icon = icon;
            CurrentCount = Mathf.Max(0, currentCount);
        }

        public Sprite Icon { get; }
        public int CurrentCount { get; }
    }
}
