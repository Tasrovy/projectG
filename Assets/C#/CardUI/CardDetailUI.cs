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
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text textText;


    [Header("Prompt (ScrollView)")]
    [SerializeField] private PromptItemUI promptItemPrefab;
    [SerializeField] private Transform promptContent;

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

        // 面板背景不拦截射线，避免挡住卡牌导致闪烁
        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

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

        SetText(descriptionText, CurrentCard.GetParsedDescription());
        SetText(textText, CurrentCard.text);
        
        BuildPromptItems(CurrentCard);

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
