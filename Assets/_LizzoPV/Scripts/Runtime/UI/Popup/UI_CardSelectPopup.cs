using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Telemetry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_CardSelectPopup : global::UI_Base
    {
        private const string CardPrefabAddress = "UI_SelectCardItem.prefab";
        private const float CardSelectionRevealSeconds = 0.18f;

        [Header("Modal")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _inputBlocker;

        [Header("Card Selection")]
        [SerializeField] private Transform _cardList;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _selectionGuideText;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private TMP_Text _refreshLabelText;
        [SerializeField] private TMP_Text _refreshRemainingCountText;

        private readonly List<UI_SelectCardItem> _items = new List<UI_SelectCardItem>(3);

        private CardData[] _cards = Array.Empty<CardData>();
        private IPrefabFactory _factory;
        private PartyService _party;
        private Action _closeRequested;
        private bool _isConfigured;
        private bool _isSelecting;

        public bool Configure(IPrefabFactory factory, PartyService party, Action closeRequested)
        {
            if (factory == null)
            {
                Debug.LogError("[UI_CardSelectPopup] Card factory is required.", this);
                return false;
            }

            if (party == null)
            {
                Debug.LogError("[UI_CardSelectPopup] PartyService is required.", this);
                return false;
            }

            _factory = factory;
            _party = party;
            _closeRequested = closeRequested;
            _isConfigured = true;
            return ValidateAuthoredReferences();
        }

        public override bool Init()
        {
            if (base.Init() == false)
                return false;

            if (ValidateAuthoredReferences() == false)
                return false;

            ApplyRaycastPolicy();
            ApplyKoreanLabels();
            RegisterRefreshListener();
            ApplyRefreshState();
            return true;
        }

        private void OnEnable()
        {
            _isSelecting = false;
            if (_isConfigured == false || _init == false)
                return;

            RegisterRefreshListener();
            ApplyKoreanLabels();
            PopulateGrid();
            ApplyRefreshState();
        }

        private void OnDisable()
        {
            UnregisterRefreshListener();
            _isSelecting = false;
        }

        private void OnDestroy()
        {
            UnregisterRefreshListener();
        }

        public void SelectCard(UI_SelectCardItem selectedItem, CardData cardData)
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
                UI_SelectCardItem candidate = _items[candidateIndex];
                if (candidate == null || !candidate.EditorAutomationCanSelect)
                    continue;

                selectedIndex = candidateIndex;
                candidate.OnClickItem();
                return true;
            }

            return false;
        }
#endif

        private void ApplyKoreanLabels()
        {
            if (_titleText == null || _selectionGuideText == null)
            {
                Debug.LogError("[UI_CardSelectPopup] Required authored label references are missing.", this);
                return;
            }

            _titleText.text = "카드 선택";
            _selectionGuideText.text = "카드 1장을 선택하세요";
            _refreshLabelText.text = "새로고침";
            _titleText.raycastTarget = false;
            _selectionGuideText.raycastTarget = false;
            _refreshLabelText.raycastTarget = false;
            _refreshRemainingCountText.raycastTarget = false;
        }

        private void PopulateGrid()
        {
            PopulateGrid(FixedCardPool.GetNextLevelUpCards());
        }

        private void PopulateGrid(CardData[] cards)
        {
            ClearGridItems();
            _cards = cards ?? Array.Empty<CardData>();
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
                GameObject go = _factory.Spawn(CardPrefabAddress, _cardList, pooled: true);
                UI_SelectCardItem item = go == null ? null : go.GetComponent<UI_SelectCardItem>();
                if (item == null)
                {
                    Debug.LogError("[UI_CardSelectPopup] UI_SelectCardItem prefab is missing its authored component.", go);
                    if (go != null)
                        _factory.Release(go);
                    continue;
                }

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

            ApplyRefreshState();
        }

        private void OnRefreshClicked()
        {
            if (_isSelecting)
                return;

            if (FixedCardPool.TryRefreshCards(_cards, out CardData[] refreshedCards) == false)
            {
                ApplyRefreshState();
                return;
            }

            PopulateGrid(refreshedCards);
        }

        private async UniTaskVoid PlayCardSelectionAsync(UI_SelectCardItem selectedItem, CardData cardData)
        {
            _isSelecting = true;
            ApplyRefreshState();

            for (int i = 0; i < _items.Count; i++)
            {
                UI_SelectCardItem item = _items[i];
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

        private void RegisterRefreshListener()
        {
            if (_refreshButton == null)
                return;

            _refreshButton.onClick.RemoveListener(OnRefreshClicked);
            _refreshButton.onClick.AddListener(OnRefreshClicked);
        }

        private void UnregisterRefreshListener()
        {
            if (_refreshButton != null)
                _refreshButton.onClick.RemoveListener(OnRefreshClicked);
        }

        private UI_SelectCardItem FindCardItem(CardKind kind)
        {
            for (int i = 0; i < _items.Count && i < _cards.Length; i++)
            {
                if (_cards[i].Kind == kind)
                    return _items[i];
            }

            return null;
        }

        private void ClearGridItems()
        {
            if (_cardList == null || _factory == null)
                return;

            for (int i = _cardList.childCount - 1; i >= 0; i--)
            {
                GameObject child = _cardList.GetChild(i).gameObject;
                child.SetActive(false);
                _factory.Release(child);
            }

            _items.Clear();
            _cards = Array.Empty<CardData>();
        }

        private void ApplyRaycastPolicy()
        {
            _inputBlocker.raycastTarget = true;

            Graphic[] refreshGraphics = _refreshButton.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < refreshGraphics.Length; i++)
                refreshGraphics[i].raycastTarget = refreshGraphics[i].gameObject == _refreshButton.gameObject;
        }

        private void ApplyRefreshState()
        {
            if (_refreshButton == null || _refreshRemainingCountText == null)
                return;

            _refreshButton.gameObject.SetActive(true);
            _refreshButton.interactable = _isSelecting == false && FixedCardPool.RemainingRefreshCount > 0;
            _refreshRemainingCountText.text = $"{FixedCardPool.RemainingRefreshCount}/{FixedCardPool.MaxRefreshCount}";
        }

        private bool ValidateAuthoredReferences()
        {
            if (_canvasGroup == null
                || _inputBlocker == null
                || _cardList == null
                || _titleText == null
                || _selectionGuideText == null
                || _refreshButton == null
                || _refreshLabelText == null
                || _refreshRemainingCountText == null)
            {
                Debug.LogError("[UI_CardSelectPopup] Required authored references are missing.", this);
                return false;
            }

            return true;
        }

        private static void ValidateCardCount(CardData[] cards)
        {
            int count = cards == null ? 0 : cards.Length;
            if (count != FixedCardPool.CardOptionCount)
                Debug.LogError($"Card option count mismatch. expected={FixedCardPool.CardOptionCount}, actual={count}");
        }

        private static string BuildCardOptionsText(CardData[] cards)
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

        private static void LogCardBuckets(CardData[] cards)
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
}
