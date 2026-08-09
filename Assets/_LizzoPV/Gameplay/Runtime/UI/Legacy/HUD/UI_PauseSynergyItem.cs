using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_PauseSynergyItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;

        public bool Validate()
        {
            if (_nameText == null)
            {
                Debug.LogError("[UI_PauseSynergyItem] NameText is required.", this);
                return false;
            }

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].raycastTarget)
                {
                    Debug.LogError("[UI_PauseSynergyItem] Decorative graphics must not raycast.", graphics[i]);
                    return false;
                }
            }

            return true;
        }

        public bool Present(PauseSynergyPresentation presentation)
        {
            if (!Validate() || string.IsNullOrWhiteSpace(presentation.DisplayName))
                return false;

            _nameText.text = presentation.DisplayName;
            _nameText.raycastTarget = false;
            return true;
        }

        public void Clear()
        {
            if (_nameText != null)
                _nameText.text = string.Empty;
        }
    }
}
