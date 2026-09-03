using System;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [Serializable]
    public struct TypographyProfileBinding
    {
        [SerializeField] private TypographyRole _role;
        [SerializeField] private TMP_FontAsset _fontAsset;
        [SerializeField] private Material _fontMaterial;

        public TypographyProfileBinding(TypographyRole role, TMP_FontAsset fontAsset, Material fontMaterial = null)
        {
            _role = role;
            _fontAsset = fontAsset;
            _fontMaterial = fontMaterial;
        }

        public TypographyRole Role => _role;
        public TMP_FontAsset FontAsset => _fontAsset;
        public Material FontMaterial => _fontMaterial != null ? _fontMaterial : _fontAsset != null ? _fontAsset.material : null;
    }

    [Serializable]
    public struct ColorPaletteEntry
    {
        [SerializeField] private ColorRole _role;
        [SerializeField] private Color _color;

        public ColorPaletteEntry(ColorRole role, Color color)
        {
            _role = role;
            _color = color;
        }

        public ColorRole Role => _role;
        public Color Color => _color;
    }

    [Serializable]
    public struct ControlStyleProfileBinding
    {
        [SerializeField] private ControlStyleRole _role;
        [SerializeField] private ControlStyleProfileSO _profile;

        public ControlStyleProfileBinding(ControlStyleRole role, ControlStyleProfileSO profile)
        {
            _role = role;
            _profile = profile;
        }

        public ControlStyleRole Role => _role;
        public ControlStyleProfileSO Profile => _profile;
    }

}
