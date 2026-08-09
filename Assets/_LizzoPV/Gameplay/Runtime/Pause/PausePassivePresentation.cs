using UnityEngine;

namespace Lizzo.PV.UI
{
    public readonly struct PausePassivePresentation
    {
        public PausePassivePresentation(Sprite icon)
            : this(icon, 0)
        {
        }

        public PausePassivePresentation(Sprite icon, int level)
        {
            Icon = icon;
            Level = Mathf.Max(0, level);
        }

        public Sprite Icon { get; }
        public int Level { get; }
    }
}
