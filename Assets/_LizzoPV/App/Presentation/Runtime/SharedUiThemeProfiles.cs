using System;
using System.Collections.Generic;
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

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/Typography Profile", fileName = "TypographyProfile")]
    public sealed class TypographyProfileSO : ScriptableObject
    {
        [SerializeField] private List<TypographyProfileBinding> _bindings = new List<TypographyProfileBinding>();

        public IReadOnlyList<TypographyProfileBinding> Bindings => _bindings;

        public bool TryGet(TypographyRole role, out TypographyProfileBinding binding)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].Role == role)
                {
                    binding = _bindings[i];
                    return true;
                }
            }

            binding = default;
            return false;
        }

        public bool TryValidate(out string issue)
        {
            var seen = new HashSet<TypographyRole>();
            for (int i = 0; i < _bindings.Count; i++)
            {
                TypographyProfileBinding binding = _bindings[i];
                if (!seen.Add(binding.Role))
                {
                    issue = $"Duplicate TypographyRole binding: {binding.Role}.";
                    return false;
                }

                if (binding.FontAsset == null)
                {
                    issue = $"TypographyRole {binding.Role} requires a FontAsset.";
                    return false;
                }
            }

            foreach (TypographyRole role in Enum.GetValues(typeof(TypographyRole)))
            {
                if (!seen.Contains(role))
                {
                    issue = $"Missing required TypographyRole binding: {role}.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(IEnumerable<TypographyProfileBinding> bindings)
        {
            _bindings = Copy(bindings);
        }
#endif

        private static List<T> Copy<T>(IEnumerable<T> source)
        {
            return source == null ? new List<T>() : new List<T>(source);
        }
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

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/Color Palette", fileName = "ColorPalette")]
    public sealed class ColorPaletteSO : ScriptableObject
    {
        [SerializeField] private List<ColorPaletteEntry> _entries = new List<ColorPaletteEntry>();

        public IReadOnlyList<ColorPaletteEntry> Entries => _entries;

        public bool TryGet(ColorRole role, out Color color)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Role == role)
                {
                    color = _entries[i].Color;
                    return true;
                }
            }

            color = default;
            return false;
        }

        public bool TryValidate(out string issue)
        {
            var seen = new HashSet<ColorRole>();
            for (int i = 0; i < _entries.Count; i++)
            {
                ColorRole role = _entries[i].Role;
                if (role.IsNone)
                {
                    issue = $"Color palette entry {i} requires a non-empty ColorRole.";
                    return false;
                }

                if (!seen.Add(role))
                {
                    issue = $"Duplicate ColorRole entry: {role}.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(IEnumerable<ColorPaletteEntry> entries)
        {
            _entries = entries == null ? new List<ColorPaletteEntry>() : new List<ColorPaletteEntry>(entries);
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/Control Style Profile", fileName = "ControlStyleProfile")]
    public sealed class ControlStyleProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _normalSpriteId;
        [SerializeField] private SpriteAssetId _pressedSpriteId;
        [SerializeField] private SpriteAssetId _selectedSpriteId;
        [SerializeField] private SpriteAssetId _disabledSpriteId;
        [SerializeField] private SpriteAssetId _lockedSpriteId;
        [SerializeField] private SpriteAssetId _processingSpriteId;

        public SpriteAssetId NormalSpriteId => _normalSpriteId;
        public SpriteAssetId PressedSpriteId => _pressedSpriteId;
        public SpriteAssetId SelectedSpriteId => _selectedSpriteId;
        public SpriteAssetId DisabledSpriteId => _disabledSpriteId;
        public SpriteAssetId LockedSpriteId => _lockedSpriteId;
        public SpriteAssetId ProcessingSpriteId => _processingSpriteId;

        public bool TryGet(ControlVisualState state, out SpriteAssetId spriteId)
        {
            switch (state)
            {
                case ControlVisualState.Normal:
                    spriteId = _normalSpriteId;
                    break;
                case ControlVisualState.Pressed:
                    spriteId = _pressedSpriteId;
                    break;
                case ControlVisualState.Selected:
                    spriteId = _selectedSpriteId;
                    break;
                case ControlVisualState.Disabled:
                    spriteId = _disabledSpriteId;
                    break;
                case ControlVisualState.Locked:
                    spriteId = _lockedSpriteId;
                    break;
                case ControlVisualState.Processing:
                    spriteId = _processingSpriteId;
                    break;
                default:
                    spriteId = SpriteAssetId.None;
                    return false;
            }

            return !spriteId.IsNone;
        }

        public bool TryValidate(ControlStyleRole role, out string issue)
        {
            if (!Require(_normalSpriteId, nameof(NormalSpriteId), out issue)
                || !Require(_pressedSpriteId, nameof(PressedSpriteId), out issue)
                || !Require(_disabledSpriteId, nameof(DisabledSpriteId), out issue))
            {
                return false;
            }

            switch (role)
            {
                case ControlStyleRole.PrimaryButton:
                case ControlStyleRole.DestructiveButton:
                    return Require(_processingSpriteId, nameof(ProcessingSpriteId), out issue)
                           && RequireNone(_selectedSpriteId, nameof(SelectedSpriteId), role, out issue)
                           && RequireNone(_lockedSpriteId, nameof(LockedSpriteId), role, out issue);
                case ControlStyleRole.IconButton:
                    return Require(_selectedSpriteId, nameof(SelectedSpriteId), out issue)
                           && Require(_lockedSpriteId, nameof(LockedSpriteId), out issue)
                           && Require(_processingSpriteId, nameof(ProcessingSpriteId), out issue);
                case ControlStyleRole.ChoiceCard:
                    return Require(_selectedSpriteId, nameof(SelectedSpriteId), out issue)
                           && RequireNone(_lockedSpriteId, nameof(LockedSpriteId), role, out issue)
                           && RequireNone(_processingSpriteId, nameof(ProcessingSpriteId), role, out issue);
                default:
                    issue = $"Unsupported ControlStyleRole: {role}.";
                    return false;
            }
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId normalSpriteId,
            SpriteAssetId pressedSpriteId,
            SpriteAssetId selectedSpriteId,
            SpriteAssetId disabledSpriteId,
            SpriteAssetId lockedSpriteId,
            SpriteAssetId processingSpriteId)
        {
            _normalSpriteId = normalSpriteId;
            _pressedSpriteId = pressedSpriteId;
            _selectedSpriteId = selectedSpriteId;
            _disabledSpriteId = disabledSpriteId;
            _lockedSpriteId = lockedSpriteId;
            _processingSpriteId = processingSpriteId;
        }
#endif

        private static bool Require(SpriteAssetId id, string fieldName, out string issue)
        {
            if (id.IsNone)
            {
                issue = $"Required Control Style Sprite Asset ID {fieldName} must be positive.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool RequireNone(
            SpriteAssetId id,
            string fieldName,
            ControlStyleRole role,
            out string issue)
        {
            if (!id.IsNone)
            {
                issue = $"ControlStyleRole {role} does not allow {fieldName}.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
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

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/Control Style Profile Set", fileName = "ControlStyleProfileSet")]
    public sealed class ControlStyleProfileSetSO : ScriptableObject
    {
        [SerializeField] private List<ControlStyleProfileBinding> _bindings = new List<ControlStyleProfileBinding>();

        public IReadOnlyList<ControlStyleProfileBinding> Bindings => _bindings;

        public bool TryGet(ControlStyleRole role, out ControlStyleProfileSO profile)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].Role == role)
                {
                    profile = _bindings[i].Profile;
                    return profile != null;
                }
            }

            profile = null;
            return false;
        }

        public bool TryValidate(out string issue)
        {
            var seen = new HashSet<ControlStyleRole>();
            for (int i = 0; i < _bindings.Count; i++)
            {
                ControlStyleProfileBinding binding = _bindings[i];
                if (!seen.Add(binding.Role))
                {
                    issue = $"Duplicate ControlStyleRole binding: {binding.Role}.";
                    return false;
                }

                if (binding.Profile == null)
                {
                    issue = $"ControlStyleRole {binding.Role} requires a Profile.";
                    return false;
                }

                if (!binding.Profile.TryValidate(binding.Role, out issue))
                {
                    issue = $"ControlStyleRole {binding.Role}: {issue}";
                    return false;
                }
            }

            foreach (ControlStyleRole role in Enum.GetValues(typeof(ControlStyleRole)))
            {
                if (!seen.Contains(role))
                {
                    issue = $"Missing required ControlStyleRole binding: {role}.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(IEnumerable<ControlStyleProfileBinding> bindings)
        {
            _bindings = bindings == null ? new List<ControlStyleProfileBinding>() : new List<ControlStyleProfileBinding>(bindings);
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/UI Theme Profile", fileName = "UiThemeProfile")]
    public sealed class UiThemeProfileSO : ScriptableObject
    {
        [SerializeField] private TypographyProfileSO _typographyProfile;
        [SerializeField] private ColorPaletteSO _colorPalette;
        [SerializeField] private ControlStyleProfileSetSO _controlStyleProfileSet;

        public TypographyProfileSO TypographyProfile => _typographyProfile;
        public ColorPaletteSO ColorPalette => _colorPalette;
        public ControlStyleProfileSetSO ControlStyleProfileSet => _controlStyleProfileSet;

        public bool TryValidate(out string issue)
        {
            if (_typographyProfile == null || _colorPalette == null || _controlStyleProfileSet == null)
            {
                issue = "UiThemeProfile requires TypographyProfile, ColorPalette, and ControlStyleProfileSet.";
                return false;
            }

            return _typographyProfile.TryValidate(out issue)
                   && _colorPalette.TryValidate(out issue)
                   && _controlStyleProfileSet.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            TypographyProfileSO typographyProfile,
            ColorPaletteSO colorPalette,
            ControlStyleProfileSetSO controlStyleProfileSet)
        {
            _typographyProfile = typographyProfile;
            _colorPalette = colorPalette;
            _controlStyleProfileSet = controlStyleProfileSet;
        }
#endif
    }
}
