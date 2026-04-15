using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 纯展示用的卡牌UI脚本，没有拖拽和选中逻辑
/// </summary>
public class CardDisplayUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image baseImage;
    public Text nameText;
    public Text descriptionText;

    private Vector3 _originalScale;
    private bool _isHovered = false;

    void Awake()
    {
        _originalScale = transform.localScale;
    }

    void Update()
    {
        if (_isHovered && !Input.GetMouseButton(0))
        {
            transform.localScale = _originalScale * 1.1f;
        }
        else
        {
            transform.localScale = _originalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => _isHovered = false;

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