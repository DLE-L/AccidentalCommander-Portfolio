using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class UI_SkillSelectPopup : UI_Base
{
    [SerializeField]
    [FormerlySerializedAs("_grid")]
    Transform _cardList;

    const float CardSelectionRevealSeconds = 0.18f;

    readonly List<UI_SkillCardItem> _items = new List<UI_SkillCardItem>();
    CardData[] _cards = Array.Empty<CardData>();
    bool _isSelecting;
    bool _isConfigured;
    IPrefabFactory _factory;
    PartyService _party;
    Action _closeRequested;

public bool Configure(IPrefabFactory factory, PartyService party, Action closeRequested)
    {
        if (factory == null)
        {
            Debug.LogError("[SkillSelectPopup] Card factory is required.", this);
            return false;
        }

        if (party == null)
        {
            Debug.LogError("[SkillSelectPopup] PartyService is required.", this);
            return false;
        }

        _factory = factory;
        _party = party;
        _closeRequested = closeRequested;
        _isConfigured = true;
        return true;
    }


    void OnEnable()
    {
        _isSelecting = false;
        ApplyKoreanLabels();
        PopulateGrid();
    }

    void OnDisable()
    {
        _isSelecting = false;
    }

    void ApplyKoreanLabels()
    {
        SetText("Title", "카드 선택");
        SetText("Comment", "카드를 하나 선택하세요");
        SetText("LevelUpTitle", "레벨업!");
        SetText("BeforeLevel", string.Empty);
        SetText("AfterLevel", string.Empty);
        SetText("RefreshButtonText", "새로고침");
        SetText("AdRefreshButtonText", "광고 리롤");
    }

    void SetText(string objectName, string text)
    {
        TMP_Text target = Utils.FindChild<TMP_Text>(gameObject, objectName, true);
        if (target != null)
            target.text = text;
    }

    void PopulateGrid()
    {
        if (_isConfigured == false || _cardList == null)
            return;

        ClearGridItems();
        _cards = FixedCardPool.GetNextLevelUpCards();
        ValidateCardCount(_cards);
        P0Telemetry.Log(
            P0Telemetry.CardOptionsShow,
            P0Telemetry.RunTimeSecondsParameter,
            $"level_up={FixedCardPool.CurrentLevelUpCount}",
            $"card_count={_cards.Length}",
            $"cards={BuildCardOptionsText(_cards)}");
        LogCardBuckets(_cards);

        for (int i = 0; i < _cards.Length; i++)
        {
            GameObject go = _factory.Spawn("SkillCardItem.prefab", pooled: true);
            UI_SkillCardItem item = go == null ? null : go.GetComponent<UI_SkillCardItem>();
            if (item == null)
            {
                Debug.LogError("[SkillSelectPopup] SkillCardItem prefab is missing its authored component.", go);
                if (go != null)
                    _factory.Release(go);
                continue;
            }
            item.transform.SetParent(_cardList, false);
            if (item.Configure(SelectCard, _party) == false)
            {
                _factory.Release(go);
                continue;
            }

            item.Init();
            item.SetInfo(_cards[i]);
            item.ApplySelectionVisual(selected: false, faded: FixedCardPool.IsTutorialOffRouteCard(_cards[i]));
            _items.Add(item);
        }
    }

#if UNITY_EDITOR
    public bool EditorAutomationTrySelectCard(int preferredIndex, out int selectedIndex)
    {
        selectedIndex = -1;
        if (!isActiveAndEnabled || _isSelecting || _items.Count == 0)
            return false;

        int itemCount = _items.Count;
        int startIndex = Mathf.Abs(preferredIndex) % itemCount;
        for (int offset = 0; offset < itemCount; offset++)
        {
            int candidateIndex = (startIndex + offset) % itemCount;
            UI_SkillCardItem candidate = _items[candidateIndex];
            if (candidate == null || !candidate.EditorAutomationCanSelect)
                continue;

            selectedIndex = candidateIndex;
            candidate.OnClickItem();
            return true;
        }

        return false;
    }
#endif

    public void SelectCard(UI_SkillCardItem selectedItem, CardData cardData)
    {
        if (_isSelecting)
            return;

        if (FixedCardPool.TryGetTutorialRequiredCardData(_cards, out CardData requiredCard)
            && cardData.Kind != requiredCard.Kind)
        {
            cardData = requiredCard;
            selectedItem = FindCardItem(requiredCard.Kind) ?? selectedItem;
        }

        PlayCardSelectionAsync(selectedItem, cardData).Forget();
    }

    UI_SkillCardItem FindCardItem(CardKind kind)
    {
        for (int i = 0; i < _items.Count && i < _cards.Length; i++)
        {
            if (_cards[i].Kind == kind)
                return _items[i];
        }

        return null;
    }

    async UniTaskVoid PlayCardSelectionAsync(UI_SkillCardItem selectedItem, CardData cardData)
    {
        _isSelecting = true;

        for (int i = 0; i < _items.Count; i++)
        {
            UI_SkillCardItem item = _items[i];
            if (item == null)
                continue;

            bool selected = item == selectedItem;
            item.ApplySelectionVisual(selected, faded: selected == false);
        }

        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(CardSelectionRevealSeconds),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        FixedCardPool.Select(cardData);
        _closeRequested?.Invoke();
    }

    void ClearGridItems()
    {
        for (int i = _cardList.childCount - 1; i >= 0; i--)
        {
            GameObject child = _cardList.GetChild(i).gameObject;
            child.SetActive(false);
            _factory.Release(child);
        }

        _items.Clear();
        _cards = Array.Empty<CardData>();
    }

    static void ValidateCardCount(CardData[] cards)
    {
        int count = cards == null ? 0 : cards.Length;
        if (count != FixedCardPool.CardOptionCount)
            Debug.LogError($"Card option count mismatch. expected={FixedCardPool.CardOptionCount}, actual={count}");
    }

    static string BuildCardOptionsText(CardData[] cards)
    {
        if (cards == null || cards.Length == 0)
            return "none";

        System.Text.StringBuilder builder = new System.Text.StringBuilder(96);
        for (int i = 0; i < cards.Length; i++)
        {
            if (i > 0)
                builder.Append(';');

            builder.Append(cards[i].Kind);
            builder.Append(':');
            builder.Append(cards[i].Highlight);
            builder.Append(':');
            builder.Append(CardPresentation.GetTypeId(cards[i]));
        }

        return builder.ToString();
    }

    static void LogCardBuckets(CardData[] cards)
    {
        if (cards == null || cards.Length == 0)
            return;

        int squadCount = 0;
        int passiveCount = 0;
        int utilityCount = 0;
        System.Text.StringBuilder bucketBuilder = new System.Text.StringBuilder(96);

        for (int i = 0; i < cards.Length; i++)
        {
            string bucket = CardPresentation.GetBucketId(cards[i]);
            if (bucket == "squad")
                squadCount++;
            else if (bucket == "passive")
                passiveCount++;
            else
                utilityCount++;

            if (bucketBuilder.Length > 0)
                bucketBuilder.Append(';');

            bucketBuilder.Append(cards[i].Kind);
            bucketBuilder.Append(':');
            bucketBuilder.Append(bucket);

            P0Telemetry.Log(
                P0Telemetry.CardTypeSeen,
                P0Telemetry.RunTimeSecondsParameter,
                $"level_up={FixedCardPool.CurrentLevelUpCount}",
                $"card={cards[i].Kind}",
                $"card_type={CardPresentation.GetTypeId(cards[i])}",
                $"bucket={bucket}",
                $"highlight={cards[i].Highlight}");
        }

        P0Telemetry.Log(
            P0Telemetry.CardOfferBucketLog,
            P0Telemetry.RunTimeSecondsParameter,
            $"level_up={FixedCardPool.CurrentLevelUpCount}",
            $"squad={squadCount}",
            $"passive={passiveCount}",
            $"utility={utilityCount}",
            $"buckets={bucketBuilder}");
    }
}
