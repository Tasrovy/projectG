using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CardDetailUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool hideOnAwake = true;

    [Header("Main Display")]
    [SerializeField] private Image baseImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text idText;
    [SerializeField] private Text descriptionText;

    [Header("Detail Text")]
    [SerializeField] private Text natureText;
    [SerializeField] private Text saleText;
    [SerializeField] private Text madeText;
    [SerializeField] private Text brokenText;
    [SerializeField] private Text addedText;
    [SerializeField] private Text buffText;
    [SerializeField] private Text triggerText;
    [SerializeField] private Text nextTurnText;

    [Header("Prompt (ScrollView)")]
    [SerializeField] private PromptItemUI promptItemPrefab;
    [SerializeField] private Transform promptContent;

    [Header("Card Sprites")]
    [SerializeField] private Sprite commonSprite;
    [SerializeField] private Sprite shengZhiSprite;
    [SerializeField] private Sprite jianZhiSprite;
    [SerializeField] private Sprite shengZhangSprite;

    public Card CurrentCard { get; private set; }
    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    private PromptItemSO _promptItemSO;
    private bool _promptLoaded;

    private void Awake()
    {
        EnsurePromptLoaded();
        if (panelRoot == null)
            panelRoot = gameObject;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (hideOnAwake)
            Hide();
    }

    public void Show(Card card)
    {
        if (card == null)
        {
            Hide();
            return;
        }

        CurrentCard = card;
        Refresh();
        SetVisible(true);
    }

    public void Show(CardObject cardObject)
    {
        Show(cardObject != null ? cardObject.card : null);
    }

    public void Show(CardUIObject cardUIObject)
    {
        Show(cardUIObject != null ? cardUIObject.Card : null);
    }

    public void Toggle(Card card)
    {
        if (IsVisible && ReferenceEquals(CurrentCard, card))
        {
            Hide();
            return;
        }

        Show(card);
    }

    public void Hide()
    {
        CurrentCard = null;
        ClearPromptItems();
        SetVisible(false);
    }

    public void Refresh()
    {
        if (CurrentCard == null) return;

        SetText(nameText, CurrentCard.name);
        SetText(idText, CurrentCard.id.ToString());
        SetText(descriptionText, CurrentCard.GetParsedDescription());
        SetText(natureText, BuildNatureText(CurrentCard));
        SetText(saleText, CurrentCard.sale.ToString());
        SetText(madeText, FormatOptional(CurrentCard.made));
        SetText(brokenText, FormatOptional(CurrentCard.broken));
        SetText(addedText, FormatOptional(CurrentCard.added));
        SetText(buffText, FormatOptional(CurrentCard.buff));
        SetText(triggerText, FormatOptional(CurrentCard.trigger));
        SetText(nextTurnText, FormatOptional(CurrentCard.nextTurn));
        BuildPromptItems(CurrentCard);

        if (baseImage != null)
            baseImage.sprite = ResolveCardSprite(CurrentCard);
    }

    private void SetVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
            target.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string FormatOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string BuildNatureText(Card card)
    {
        StringBuilder builder = new StringBuilder();
        AppendNature(builder, "Nature 1", card.nature1);
        AppendNature(builder, "Nature 2", card.nature2);
        AppendNature(builder, "Nature 3", card.nature3);
        return builder.Length > 0 ? builder.ToString() : "-";
    }

    private static void AppendNature(StringBuilder builder, string label, int value)
    {
        if (value == 0) return;

        if (builder.Length > 0)
            builder.AppendLine();

        builder.Append(label);
        builder.Append(": ");
        builder.Append(value);
    }

    private void BuildPromptItems(Card card)
    {
        ClearPromptItems();

        if (string.IsNullOrWhiteSpace(card.prompt)) return;
        if (promptItemPrefab == null || promptContent == null) return;

        EnsurePromptLoaded();
        if (_promptItemSO == null || _promptItemSO.allItems == null) return;

        string[] ids = card.prompt.Split(',');

        foreach (string idStr in ids)
        {
            if (!int.TryParse(idStr.Trim(), out int promptId)) continue;

            PromptItem item = _promptItemSO.allItems.Find(p => p.id == promptId);
            if (item == null) continue;

            PromptItemUI ui = Instantiate(promptItemPrefab, promptContent);
            ui.SetPromptItem(item);
        }
    }

    private void ClearPromptItems()
    {
        if (promptContent == null) return;

        for (int i = promptContent.childCount - 1; i >= 0; i--)
        {
            Destroy(promptContent.GetChild(i).gameObject);
        }
    }

    private void EnsurePromptLoaded()
    {
        if (_promptLoaded) return;
        _promptLoaded = true;

        _promptItemSO = ExcelLoader.Instance.ReadPromptExcel("prompt.xlsx");
    }

    private Sprite ResolveCardSprite(Card card)
    {
        int cardType = GetCardType(card.id);
        int brokenValue = ParseInt(card.broken);
        int addedValue = ParseInt(card.added);

        if (cardType == 1 && commonSprite != null)
            return commonSprite;

        if (brokenValue > 0 && jianZhiSprite != null)
            return jianZhiSprite;

        if (addedValue > 0 && shengZhangSprite != null)
            return shengZhangSprite;

        return shengZhiSprite != null ? shengZhiSprite : baseImage != null ? baseImage.sprite : null;
    }

    private static int GetCardType(int id)
    {
        string value = Math.Abs(id).ToString();
        return value.Length > 0 && int.TryParse(value[0].ToString(), out int type) ? type : 0;
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, out int result) ? result : 0;
    }
}
