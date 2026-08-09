using UnityEngine;

namespace Lizzo.PV.UI.Theming.Draft
{
    [CreateAssetMenu(fileName = "PauseOverlayStyleSet", menuName = "Lizzo/UI/Draft/Pause Overlay Style Set")]
    public sealed class PauseOverlayStyleSet : ScriptableObject
    {
        [SerializeField] DraftImageStyle _panel;
        [SerializeField] DraftImageStyle _continueButton;

        public DraftImageStyle Panel => _panel;
        public DraftImageStyle ContinueButton => _continueButton;
    }
}
