using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class LobbyBottomNavigationView : MonoBehaviour
    {
        const int TabCount = 5;
        const float NormalCellHeight = 200f;
        const float SelectedCellHeight = 218f;
        const float NormalIconSize = 116f;
        const float SelectedIconSize = 128f;
        const float NormalLabelSize = 36f;
        const float SelectedLabelSize = 40f;
        const float SelectedScale = 1.08f;

        [Serializable]
        sealed class TabBinding
        {
            [SerializeField] Button _button;
            [SerializeField] RectTransform _cell;
            [SerializeField] GameObject _normal;
            [SerializeField] GameObject _selected;
            [SerializeField] RectTransform _icon;
            [SerializeField] Image _iconImage;
            [SerializeField] TMP_Text _label;

            public Button Button => _button;
            public RectTransform Cell => _cell;
            public GameObject Normal => _normal;
            public GameObject Selected => _selected;
            public RectTransform Icon => _icon;
            public Image IconImage => _iconImage;
            public TMP_Text Label => _label;
        }

        sealed class Item
        {
            public TabBinding Binding;
            public int OriginalSiblingIndex;
        }

        [SerializeField] TabBinding[] _tabBindings;

        Item[] _items;

        public bool Configure()
        {
            if (_items != null)
                return true;

            if (_tabBindings == null || _tabBindings.Length != TabCount)
            {
                Debug.LogError("[LobbyBottomNavigationView] Exactly five explicit serialized tab bindings are required.", this);
                return false;
            }

            HashSet<Button> buttons = new HashSet<Button>();
            HashSet<RectTransform> cells = new HashSet<RectTransform>();
            Item[] items = new Item[TabCount];
            for (int i = 0; i < TabCount; i++)
            {
                TabBinding binding = _tabBindings[i];
                if (binding == null || binding.Button == null || binding.Cell == null ||
                    binding.Normal == null || binding.Selected == null || binding.Icon == null ||
                    binding.IconImage == null || binding.Label == null ||
                    binding.IconImage.rectTransform != binding.Icon ||
                    buttons.Contains(binding.Button) || cells.Contains(binding.Cell))
                {
                    Debug.LogError("[LobbyBottomNavigationView] Each explicit tab binding must be complete and unique.", this);
                    return false;
                }

                buttons.Add(binding.Button);
                cells.Add(binding.Cell);
                items[i] = new Item
                {
                    Binding = binding,
                    OriginalSiblingIndex = binding.Cell.GetSiblingIndex(),
                };
            }

            _items = items;
            return true;
        }

        public void SetSelected(Button selectedButton)
        {
            if (Configure() == false)
                return;

            int selectedIndex = -1;
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].Binding.Button == selectedButton)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex < 0)
            {
                Debug.LogError("[LobbyBottomNavigationView] Selected Button is not part of the authored bottom navigation.", this);
                return;
            }

            for (int i = 0; i < _items.Length; i++)
                ApplyState(_items[i].Binding, i == selectedIndex);

            for (int i = 0; i < _items.Length; i++)
            {
                if (i != selectedIndex && _items[i].Binding.Cell.GetSiblingIndex() != _items[i].OriginalSiblingIndex)
                    _items[i].Binding.Cell.SetSiblingIndex(_items[i].OriginalSiblingIndex);
            }

            _items[selectedIndex].Binding.Cell.SetAsLastSibling();
        }

        static void ApplyState(TabBinding binding, bool selected)
        {
            binding.Normal.SetActive(!selected);
            binding.Selected.SetActive(selected);
            binding.Cell.sizeDelta = new Vector2(binding.Cell.sizeDelta.x, selected ? SelectedCellHeight : NormalCellHeight);
            binding.Cell.localScale = selected ? Vector3.one * SelectedScale : Vector3.one;
            binding.Icon.sizeDelta = Vector2.one * (selected ? SelectedIconSize : NormalIconSize);
            binding.Icon.gameObject.SetActive(true);
            binding.IconImage.enabled = true;
            binding.IconImage.preserveAspect = true;
            binding.Label.gameObject.SetActive(true);
            binding.Label.enabled = true;
            binding.Label.fontSize = selected ? SelectedLabelSize : NormalLabelSize;
            binding.Label.fontStyle |= FontStyles.Bold;
        }
    }
}
