using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 纯展示用的卡牌UI脚本，没有拖拽和选中逻辑
/// </summary>
public class CardDisplayUI : MonoBehaviour, IPointerDownHandler
{
    public Image baseImage;
    public Text nameText;
    public Text descriptionText;

    private Vector3 _originalScale;
    private bool _isSelected = false;

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
    }

    [Header("卡图资源")] 
    public Sprite commonSprite;
    public Sprite ShengZhi;
    public Sprite JianZhi;
    public Sprite ShengZhang;

    public void Setup(Card card)
    {
        if (card == null) return;

        // 设置文本
        nameText.text = card.name;
        descriptionText.text = card.GetParsedDescription();

        // 设置图片（直接复用你之前的逻辑）
        int brokenValue = 0, addedValue = 0;
        int.TryParse(card.broken, out brokenValue);
        int.TryParse(card.added, out addedValue);

        if (card.id.ToString()[0] == '1') baseImage.sprite = commonSprite;
        else if (brokenValue > 0) baseImage.sprite = JianZhi;
        else if (addedValue > 0) baseImage.sprite = ShengZhang;
        else baseImage.sprite = ShengZhi;
    }
}