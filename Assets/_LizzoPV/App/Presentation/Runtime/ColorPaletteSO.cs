using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
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
}
