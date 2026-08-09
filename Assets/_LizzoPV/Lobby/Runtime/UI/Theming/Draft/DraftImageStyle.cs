using System;
using UnityEngine;

namespace Lizzo.PV.UI.Theming.Draft
{
    [Serializable]
    public struct DraftImageStyle
    {
        [SerializeField] Sprite _sprite;

        public Sprite Sprite => _sprite;
    }
}
