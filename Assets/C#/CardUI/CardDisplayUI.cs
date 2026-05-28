using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 纯展示用的卡牌UI脚本，没有拖拽和选中逻辑
/// </summary>
public class CardDisplayUI : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image baseImage;
    public Text nameText;
    public Text descriptionText;
    public Image rare;
    public TMP_Text rareText;

    private Vector3 _originalScale;
    private bool _isSelected = false;
    private Card _card;

    void Awake()
    {
        _originalScale = transform.localScale;
    }

    void OnEnable()
    {
        _isSelected = false;
    }

    void Update()
    {
        transform.localScale = _isSelected ? _originalScale * 1.1f : _originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _isSelected = !_isSelected;

        // 如果当前变为选中，取消同级所有其他 CardDisplayUI 的选中状态
        if (_isSelected && transform.parent != null)
        {
            foreach (var sibling in transform.parent.GetComponentsInChildren<CardDisplayUI>())
            {
                if (sibling != this)
                    sibling._isSelected = false;
            }
        }

        // 点击时隐藏卡牌详情
        var detailUI = DUELUIObjectManager.Instance.GetCardDetailUI();
        if (detailUI != null)
            detailUI.Hide();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_card == null) return;

        var detailUI = DUELUIObjectManager.Instance.GetCardDetailUI();
        if (detailUI != null)
            detailUI.ShowAtCard(_card, baseImage != null ? baseImage.rectTransform : (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var detailUI = DUELUIObjectManager.Instance.GetCardDetailUI();
        if (detailUI != null && detailUI.CurrentCard == _card)
            detailUI.Hide();
    }

    [Header("卡图资源")]
    public Sprite giftSprite;
    public Sprite eventSprite;
    public Sprite funcSprite;
    public Sprite rare1;
    public Sprite rare2;
    public Sprite rare3;


    public void Setup(Card card)
    {
        if (card == null) return;

        _card = card;

        // 设置文本
        nameText.text = card.name;
        descriptionText.text = card.GetParsedDescription();

        // 设置图片（直接复用你之前的逻辑）
        int brokenValue = 0, addedValue = 0;
        int.TryParse(card.broken, out brokenValue);
        int.TryParse(card.added, out addedValue);

        if (card.id.ToString()[0] == '1') baseImage.sprite = giftSprite;
        if (card.id.ToString()[0] == '2') baseImage.sprite = funcSprite;
        if (card.id.ToString()[0] == '3') baseImage.sprite = eventSprite;
        if (card.id.ToString()[1] == '1')
        {
            rare.sprite = rare1;
            rareText.text = "普通";
        }
        if (card.id.ToString()[1] == '2')
        {
            rare.sprite = rare2;
            rareText.text = "罕见";
        }
        if (card.id.ToString()[1] == '3')
        {
            rare.sprite = rare3;
            rareText.text = "珍贵";
        }
    }
}