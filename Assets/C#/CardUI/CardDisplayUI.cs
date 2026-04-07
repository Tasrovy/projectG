using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 纯展示用的卡牌UI脚本，没有拖拽和选中逻辑
/// </summary>
public class CardDisplayUI : MonoBehaviour
{
    public Image baseImage;
    public Text nameText;
    public Text descriptionText;

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