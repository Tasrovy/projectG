using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    [SerializeField] protected ShopMode startMode = ShopMode.Buy;
    [SerializeField] protected int slotsPerPage = 6;
    [Header("Buttons")]
    [SerializeField] private Button buyModeButton;
    [SerializeField] private Button sellModeButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button closeButton;
    [Header("Button Labels")]
    private Text buyModeButtonText;
    private Text sellModeButtonText;
    private Text confirmButtonText;
    private Text nextPageButtonText;
    private Text previousPageButtonText;
    private Text closeButtonText;
    [SerializeField] private string buyModeLabel = "购买";
    [SerializeField] private string sellModeLabel = "出售";
    [SerializeField] private string confirmBuyLabel = "购买";
    [SerializeField] private string confirmSellLabel = "出售";
    [SerializeField] private string nextPageLabel = "下一页";
    [SerializeField] private string previousPageLabel = "上一页";
    [SerializeField] private string closeLabel = "关闭";

    private readonly List<Transform> _slotTransforms = new List<Transform>();
    private readonly List<Card> _buyInventory = new List<Card>();
    private readonly HashSet<int> _purchasedSlotIndices = new HashSet<int>();

    private GameObject _blackOverlay;
    private ShopInventoryGenerator _inventoryGenerator;
    private ShopTransactionService _transactionService;
    private CardObject _selectedCardObject;
    private int _selectedSlotIndex = -1;
    private int _sellPageIndex;

    public ShopMode CurrentMode { get; private set; }

    protected virtual void Awake()
    {
        _inventoryGenerator = new ShopInventoryGenerator();
        _transactionService = new ShopTransactionService();
        EnsureOverlay();
        CacheSlots();
        ResolveButtonTexts();
        ApplyButtonLabels();
        BindButtons();
    }

    protected virtual void OnDestroy()
    {
        UnbindButtons();
    }

    protected virtual void OnEnable()
    {
        _transactionService = new ShopTransactionService();
        ResetSelection();
        GenerateBuyInventory();
        SwitchMode(startMode);
        UpdateButtonStates();
    }

    public void SelectCard(CardObject cardObj)
    {
        _selectedCardObject = cardObj;
        _selectedSlotIndex = cardObj != null ? _slotTransforms.IndexOf(cardObj.transform) : -1;

        if (DialogueUIAudio.Instance != null)
        {
            DialogueUIAudio.Instance.PlayCardClickAudio();
        }
    }

    public void ConfirmCurrentAction()
    {
        if (CurrentMode == ShopMode.Buy) ConfirmPurchase();
        else ConfirmSell();
    }

    public void ConfirmPurchase()
    {
        if (!HasValidSelection())
        {
            Debug.LogWarning("[ShopController] 尚未选中可购买的卡牌。");
            return;
        }

        UnpinDetail();

        Card selectedCard = _selectedCardObject.card;
        int price = _transactionService.GetBuyPrice(selectedCard);
        if (DataManager.Instance == null)
        {
            Debug.LogError("[ShopController] DataManager 未初始化。");
            return;
        }

        if (DataManager.Instance.MoneyNum < price)
        {
            Debug.LogWarning($"[ShopController] 购买失败，金额不足。需要 {price}，当前 {DataManager.Instance.MoneyNum}。");
            return;
        }

        if (!_transactionService.TryBuy(selectedCard))
        {
            Debug.LogWarning("[ShopController] 购买失败。");
            return;
        }

        if (_selectedSlotIndex >= 0)
        {
            _purchasedSlotIndices.Add(_selectedSlotIndex);
        }

        RefreshMoneyUI();
        RefreshCurrentMode();
        ResetSelection();
        UpdateButtonStates();
    }

    public void ConfirmSell()
    {
        if (!HasValidSelection())
        {
            Debug.LogWarning("[ShopController] 尚未选中可出售的卡牌。");
            return;
        }

        UnpinDetail();

        Card selectedCard = _selectedCardObject.card;
        if (!_transactionService.TrySell(selectedCard))
        {
            Debug.LogWarning("[ShopController] 出售失败。");
            return;
        }

        int sellPageCount = GetSellPageCount();
        if (_sellPageIndex >= sellPageCount)
        {
            _sellPageIndex = Mathf.Max(0, sellPageCount - 1);
        }

        RefreshMoneyUI();
        RefreshCurrentMode();
        ResetSelection();
        UpdateButtonStates();
    }

    public void SwitchToBuyMode()
    {
        SwitchMode(ShopMode.Buy);
    }

    public void SwitchToSellMode()
    {
        SwitchMode(ShopMode.Sell);
    }

    public void NextSellPage()
    {
        if (CurrentMode != ShopMode.Sell)
        {
            return;
        }

        int sellPageCount = GetSellPageCount();
        if (_sellPageIndex + 1 >= sellPageCount)
        {
            return;
        }

        _sellPageIndex++;
        RefreshCurrentMode();
        UpdateButtonStates();
    }

    public void PreviousSellPage()
    {
        if (CurrentMode != ShopMode.Sell || _sellPageIndex <= 0)
        {
            return;
        }

        _sellPageIndex--;
        RefreshCurrentMode();
        UpdateButtonStates();
    }

    public void RefreshCurrentMode()
    {
        RenderCurrentMode();
        UpdateButtonStates();
    }

    public void CloseShop()
    {
        UnpinDetail();
        ClearVisibleCards();
        ResetSelection();
        DialogueHandler.Instance.TriggerEndDayWithDeal();
    }

    protected virtual void SwitchMode(ShopMode mode)
    {
        CurrentMode = mode;
        ResetSelection();
        RenderCurrentMode();
        UpdateButtonStates();
    }

    private void BindButtons()
    {
        BindButton(buyModeButton, SwitchToBuyMode);
        BindButton(sellModeButton, SwitchToSellMode);
        BindButton(confirmButton, ConfirmCurrentAction);
        BindButton(nextPageButton, NextSellPage);
        BindButton(previousPageButton, PreviousSellPage);
        BindButton(closeButton, CloseShop);
    }

    private void ResolveButtonTexts()
    {
        if (buyModeButtonText == null && buyModeButton != null) buyModeButtonText = buyModeButton.GetComponentInChildren<Text>(true);
        if (sellModeButtonText == null && sellModeButton != null) sellModeButtonText = sellModeButton.GetComponentInChildren<Text>(true);
        if (confirmButtonText == null && confirmButton != null) confirmButtonText = confirmButton.GetComponentInChildren<Text>(true);
        if (nextPageButtonText == null && nextPageButton != null) nextPageButtonText = nextPageButton.GetComponentInChildren<Text>(true);
        if (previousPageButtonText == null && previousPageButton != null) previousPageButtonText = previousPageButton.GetComponentInChildren<Text>(true);
        if (closeButtonText == null && closeButton != null) closeButtonText = closeButton.GetComponentInChildren<Text>(true);
    }

    private void ApplyButtonLabels()
    {
        if (buyModeButtonText != null) buyModeButtonText.text = buyModeLabel;
        if (sellModeButtonText != null) sellModeButtonText.text = sellModeLabel;
        if (nextPageButtonText != null) nextPageButtonText.text = nextPageLabel;
        if (previousPageButtonText != null) previousPageButtonText.text = previousPageLabel;
        if (closeButtonText != null) closeButtonText.text = closeLabel;
        UpdateConfirmButtonLabel();
    }

    private void UnbindButtons()
    {
        if (buyModeButton != null) buyModeButton.onClick.RemoveListener(SwitchToBuyMode);
        if (sellModeButton != null) sellModeButton.onClick.RemoveListener(SwitchToSellMode);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmCurrentAction);
        if (nextPageButton != null) nextPageButton.onClick.RemoveListener(NextSellPage);
        if (previousPageButton != null) previousPageButton.onClick.RemoveListener(PreviousSellPage);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseShop);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        // button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    private void UpdateButtonStates()
    {
        if (buyModeButton != null) buyModeButton.interactable = CurrentMode != ShopMode.Buy;
        if (sellModeButton != null) sellModeButton.interactable = CurrentMode != ShopMode.Sell;

        bool isSellMode = CurrentMode == ShopMode.Sell;
        int sellPageCount = GetSellPageCount();

        if (previousPageButton != null)
        {
            previousPageButton.gameObject.SetActive(isSellMode);
            previousPageButton.interactable = isSellMode && _sellPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.gameObject.SetActive(isSellMode);
            nextPageButton.interactable = isSellMode && _sellPageIndex + 1 < sellPageCount;
        }

        UpdateConfirmButtonLabel();
    }

    private void UpdateConfirmButtonLabel()
    {
        if (confirmButtonText == null)
        {
            return;
        }

        confirmButtonText.text = CurrentMode == ShopMode.Sell ? confirmSellLabel : confirmBuyLabel;
    }

    private void RenderCurrentMode()
    {
        if (CurrentMode == ShopMode.Buy)
        {
            RenderBuyMode();
            return;
        }

        RenderSellMode();
    }

    private void RenderBuyMode()
    {
        for (int i = 0; i < _slotTransforms.Count; i++)
        {
            bool hasCard = i < _buyInventory.Count && !_purchasedSlotIndices.Contains(i);
            SetupSlot(_slotTransforms[i], hasCard ? _buyInventory[i] : null, hasCard ? _transactionService.GetBuyPrice(_buyInventory[i]) : 0);
        }
    }

    private void RenderSellMode()
    {
        int startIndex = _sellPageIndex * Mathf.Max(1, slotsPerPage);
        List<Card> handCards = CardManager.Instance != null ? CardManager.Instance.cardInHand : null;

        for (int i = 0; i < _slotTransforms.Count; i++)
        {
            int handIndex = startIndex + i;
            Card card = handCards != null && handIndex < handCards.Count ? handCards[handIndex] : null;
            SetupSlot(_slotTransforms[i], card, card != null ? _transactionService.GetSellPrice(card) : 0);
        }
    }

    private void GenerateBuyInventory()
    {
        _buyInventory.Clear();
        _purchasedSlotIndices.Clear();
        _buyInventory.AddRange(_inventoryGenerator.GenerateDailyShopCards(_slotTransforms.Count));
        _sellPageIndex = 0;
    }

    private void EnsureOverlay()
    {
        if (_blackOverlay != null)
        {
            return;
        }

        _blackOverlay = new GameObject("BackgroundOverlay");
        _blackOverlay.layer = LayerMask.NameToLayer("UI");
        _blackOverlay.transform.SetParent(transform, false);
        _blackOverlay.transform.SetAsFirstSibling();

        Image image = _blackOverlay.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.7f);
        image.raycastTarget = true;

        RectTransform rect = _blackOverlay.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(5000f, 5000f);
        rect.localScale = Vector3.one;
    }

    private void CacheSlots()
    {
        _slotTransforms.Clear();

        for (int i = 1; i <= slotsPerPage; i++)
        {
            string slotName = $"sellCard_{i}";
            Transform slot = transform.Find(slotName);
            if (slot == null)
            {
                CardObject[] allCardObjects = GetComponentsInChildren<CardObject>(true);
                foreach (CardObject cardObject in allCardObjects)
                {
                    if (cardObject.name == slotName)
                    {
                        slot = cardObject.transform;
                        break;
                    }
                }
            }

            if (slot == null)
            {
                Debug.LogError($"[ShopController] 未找到商店槽位 {slotName}。");
                continue;
            }

            _slotTransforms.Add(slot);
        }
    }

    private void SetupSlot(Transform slot, Card card, int price)
    {
        if (slot == null)
        {
            return;
        }

        CardObject cardObject = slot.GetComponent<CardObject>();
        if (cardObject == null)
        {
            Debug.LogError($"[ShopController] 槽位 {slot.name} 缺少 CardObject。");
            return;
        }

        if (card == null)
        {
            cardObject.card = null;
            slot.gameObject.SetActive(false);
            return;
        }

        cardObject.card = card;

        CardDisplayUI displayUI = slot.GetComponent<CardDisplayUI>();
        if (displayUI != null)
        {
            displayUI.Setup(card);
        }

        Transform priceTransform = slot.Find("price");
        if (priceTransform != null)
        {
            TextMeshProUGUI priceText = priceTransform.GetComponent<TextMeshProUGUI>();
            if (priceText != null)
            {
                priceText.text = price.ToString();
            }
        }

        Transform rareTransform = slot.Find("rare");
        if (rareTransform != null)
        {
            int rarity = (card.id / 1000) % 10;
            Sprite rareSprite = Resources.Load<Sprite>($"UI/Shop/{rarity}");
            if (rareSprite != null)
            {
                Image rareImage = rareTransform.GetComponent<Image>();
                if (rareImage != null)
                {
                    rareImage.sprite = rareSprite;
                }
                else
                {
                    SpriteRenderer spriteRenderer = rareTransform.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = rareSprite;
                    }
                }
            }
        }

        slot.gameObject.SetActive(true);
    }

    private void ClearVisibleCards()
    {
        foreach (Transform slot in _slotTransforms)
        {
            if (slot == null)
            {
                continue;
            }

            CardObject cardObject = slot.GetComponent<CardObject>();
            if (cardObject != null)
            {
                cardObject.card = null;
            }
        }
    }

    private void ResetSelection()
    {
        _selectedCardObject = null;
        _selectedSlotIndex = -1;
    }

    private bool HasValidSelection()
    {
        return _selectedCardObject != null && _selectedCardObject.card != null;
    }

    private void UnpinDetail()
    {
        if (_selectedCardObject != null)
            _selectedCardObject.UnpinDetail();
    }

    private int GetSellPageCount()
    {
        if (CardManager.Instance == null || CardManager.Instance.cardInHand == null || CardManager.Instance.cardInHand.Count == 0)
        {
            return 1;
        }

        return Mathf.CeilToInt(CardManager.Instance.cardInHand.Count / (float)Mathf.Max(1, slotsPerPage));
    }

    private static void RefreshMoneyUI()
    {
        UpdateMoney[] moneyUpdaters = FindObjectsOfType<UpdateMoney>(true);
        foreach (UpdateMoney updater in moneyUpdaters)
        {
            updater.UpdateText();
        }
    }
}
