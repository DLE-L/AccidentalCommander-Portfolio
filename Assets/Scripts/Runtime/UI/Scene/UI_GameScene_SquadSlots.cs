using System.Collections.Generic;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Telemetry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UI_GameScene
{
    [Header("Squad")]
    [SerializeField] private RectTransform _squadSlotHudRoot;
    [SerializeField] private RectTransform _allySlotRailRoot;
    [SerializeField] private RectTransform _passiveSlotRailRoot;

    private const int ALLY_SLOT_COUNT = 7;
    private static readonly Color EMPTY_SLOT_COLOR = new Color(0.10f, 0.12f, 0.17f, 0.86f);
    private static readonly Color EMPTY_SLOT_OUTLINE = new Color(0.29f, 0.33f, 0.43f, 0.95f);
    private static readonly Color PASSIVE_SLOT_COLOR = new Color(0.34f, 0.78f, 0.42f, 0.95f);
    private static readonly Color PASSIVE_SLOT_OUTLINE = new Color(0.76f, 1.0f, 0.42f, 0.95f);

    private readonly List<HudRailSlotItem> _allySlotHudItems = new List<HudRailSlotItem>(ALLY_SLOT_COUNT);
    private readonly List<SquadSlotPresentationSet.Entry> _allySlotHudEntries = new List<SquadSlotPresentationSet.Entry>(ALLY_SLOT_COUNT);
    private readonly List<HudRailSlotItem> _passiveSlotHudItems = new List<HudRailSlotItem>(CardEffectRuntime.PassiveSlotCap);
    private readonly string[] _passiveSlotLabels = new string[CardEffectRuntime.PassiveSlotCap];

    private bool _squadSlotHudInitialized;
    private bool _squadSlotHudErrorReported;
    private int _lastSquadSlotHudHash = int.MinValue;

    private void InitializeSquadSlotHud()
    {
        if (_party == null || _squadSlotHudInitialized || _squadSlotHudErrorReported)
            return;

        ResolveSquadSlotReferences();
        if (_squadSlotHudRoot == null || _allySlotRailRoot == null || _passiveSlotRailRoot == null)
        {
            Debug.LogError("[HUD] UI_GameScene is missing authored squad rail references. Runtime UI creation is disabled.", this);
            _squadSlotHudErrorReported = true;
            return;
        }

        if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false || catalog.SquadSlots == null)
        {
            Debug.LogError($"{nameof(PresentationCatalogProvider)} requires a {nameof(SquadSlotPresentationSet)} for squad HUD.", this);
            _squadSlotHudErrorReported = true;
            return;
        }

        IReadOnlyList<SquadSlotState> snapshot = _party.GetSquadSlotSnapshot();
        for (int i = 0; i < ALLY_SLOT_COUNT; i++)
        {
            if (i >= snapshot.Count || catalog.SquadSlots.TryGetEntry(snapshot[i].SlotId, out SquadSlotPresentationSet.Entry entry) == false)
            {
                Debug.LogError("[HUD] Squad data does not match authored ally slots.", this);
                _squadSlotHudErrorReported = true;
                return;
            }

            HudRailSlotItem item = ResolveRailSlot(_allySlotRailRoot, i);
            if (item == null)
            {
                _squadSlotHudErrorReported = true;
                return;
            }

            _allySlotHudEntries.Add(entry);
            _allySlotHudItems.Add(item);
        }

        for (int i = 0; i < CardEffectRuntime.PassiveSlotCap; i++)
        {
            HudRailSlotItem item = ResolveRailSlot(_passiveSlotRailRoot, i);
            if (item == null)
            {
                _squadSlotHudErrorReported = true;
                return;
            }

            _passiveSlotHudItems.Add(item);
        }

        _squadSlotHudInitialized = true;
        _lastSquadSlotHudHash = int.MinValue;
        UpdateSquadSlotHud();
        P0Telemetry.Log(P0Telemetry.BattleHudView, "loadout_slot_hud=shown", $"ally_slots={_allySlotHudItems.Count}", $"passive_slots={_passiveSlotHudItems.Count}");
    }

    private void ResolveSquadSlotReferences()
    {
        _squadSlotHudRoot ??= Utils.FindChild<RectTransform>(gameObject, "SquadSlotHudRoot", true);
        _allySlotRailRoot ??= _squadSlotHudRoot != null ? Utils.FindChild<RectTransform>(_squadSlotHudRoot.gameObject, "AllySlotRail", true) : null;
        _passiveSlotRailRoot ??= _squadSlotHudRoot != null ? Utils.FindChild<RectTransform>(_squadSlotHudRoot.gameObject, "PassiveSlotRail", true) : null;
    }

    private void UpdateSquadSlotHud()
    {
        if (_party == null)
            return;

        InitializeSquadSlotHud();
        if (_squadSlotHudInitialized == false)
            return;

        IReadOnlyList<SquadSlotState> snapshot = _party.GetSquadSlotSnapshot();
        int hash = BuildSquadSlotHudHash(snapshot, CardEffectRuntime.PassiveSlotStateHash);
        if (hash == _lastSquadSlotHudHash)
            return;

        _lastSquadSlotHudHash = hash;
        int allyDisplayIndex = 0;
        for (int i = 0; i < snapshot.Count && allyDisplayIndex < _allySlotHudItems.Count; i++)
        {
            SquadSlotState state = snapshot[i];
            if (state.IsActive == false)
                continue;

            _allySlotHudItems[allyDisplayIndex].ApplyAlly(state, _allySlotHudEntries[i]);
            allyDisplayIndex++;
        }

        for (int i = allyDisplayIndex; i < _allySlotHudItems.Count; i++)
            _allySlotHudItems[i].ApplyEmpty();

        int passiveCount = CardEffectRuntime.FillPassiveSlotLabels(_passiveSlotLabels);
        for (int i = 0; i < _passiveSlotHudItems.Count; i++)
        {
            if (i < passiveCount)
                _passiveSlotHudItems[i].ApplyPassive(_passiveSlotLabels[i]);
            else
                _passiveSlotHudItems[i].ApplyEmpty();
        }
    }

    private HudRailSlotItem ResolveRailSlot(RectTransform railRoot, int index)
    {
        Transform slot = railRoot.Find($"Slot_{index:00}");
        Image background = slot != null ? slot.GetComponent<Image>() : null;
        Outline outline = slot != null ? slot.GetComponent<Outline>() : null;
        TMP_Text text = slot != null ? Utils.FindChild<TMP_Text>(slot.gameObject, "Text", true) : null;
        if (background != null && outline != null && text != null)
            return new HudRailSlotItem(background, outline, text);

        Debug.LogError($"[HUD] Authored rail is missing Slot_{index:00} components.", this);
        return null;
    }

    private static int BuildSquadSlotHudHash(IReadOnlyList<SquadSlotState> snapshot, int passiveHash)
    {
        int hash = 17;
        for (int i = 0; i < snapshot.Count; i++)
        {
            unchecked
            {
                hash = hash * 31 + snapshot[i].CurrentCount;
                hash = hash * 31 + snapshot[i].MaxCount;
                hash = hash * 31 + (snapshot[i].IsPromoted ? 1 : 0);
            }
        }

        unchecked
        {
            hash = hash * 31 + passiveHash;
        }

        return hash;
    }

    private sealed class HudRailSlotItem
    {
        private readonly Image _background;
        private readonly Outline _outline;
        private readonly TMP_Text _text;

        public HudRailSlotItem(Image background, Outline outline, TMP_Text text)
        {
            _background = background;
            _outline = outline;
            _text = text;
        }

        public void ApplyAlly(SquadSlotState state, SquadSlotPresentationSet.Entry allyEntry)
        {
            if (state.IsActive == false)
            {
                ApplyEmpty();
                return;
            }

            Color accent = allyEntry != null ? allyEntry.AccentColor : Color.white;
            _background.color = accent;
            _outline.enabled = true;
            _outline.effectColor = state.IsPromoted ? Color.white : accent;
            _text.text = BuildAllySlotText(state, allyEntry);
            _text.color = Color.white;
        }

        public void ApplyPassive(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                ApplyEmpty();
                return;
            }

            _background.color = PASSIVE_SLOT_COLOR;
            _outline.enabled = true;
            _outline.effectColor = PASSIVE_SLOT_OUTLINE;
            _text.text = label;
            _text.color = Color.white;
        }

        public void ApplyEmpty()
        {
            _background.color = EMPTY_SLOT_COLOR;
            _outline.enabled = true;
            _outline.effectColor = EMPTY_SLOT_OUTLINE;
            _text.text = string.Empty;
        }

        private static string BuildAllySlotText(SquadSlotState state, SquadSlotPresentationSet.Entry entry)
        {
            if (state.IsPromoted)
                return "진";

            string label = entry != null && string.IsNullOrWhiteSpace(entry.ShortLabel) == false
                ? entry.ShortLabel
                : state.DisplayName;
            int count = Mathf.Clamp(state.CurrentCount, 0, Mathf.Max(1, state.MaxCount));
            return $"{label}{count}";
        }
    }
}
