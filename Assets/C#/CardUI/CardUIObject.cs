using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardUIObject : MonoBehaviour
{
    public Image baseImage;
    public Sprite commonSprite;
    public Sprite ShengZhi;    // 默认/生纸卡图
    public Sprite JianZhi;     // 剪纸卡图
    public Sprite ShengZhang;  // 生长卡图
    public Text name;
    public Text description;
    bool beSet=false;

    void Start()
    {
        if(baseImage==null)
        {
            baseImage= this.GetComponentInChildren<Image>();

        }
        // 初始默认显示生纸
        if (baseImage.sprite == null)
        {
            baseImage.sprite = commonSprite;
        }

        if(!beSet)
        {
            SetCard(this.GetComponent<CardObject>()?.card);
        }
    }

    public void SetCard(Card card)
    {
        name.text = card.name;
        description.text = card.GetParsedDescription();

        // --- 新增逻辑：根据 broken 和 added 切换卡图 ---
        
        int brokenValue = 0;
        int addedValue = 0;

        // 将字符串安全转为整数（如果转换失败会返回 0）
        int.TryParse(card.broken, out brokenValue);
        int.TryParse(card.added, out addedValue);

        if (card.id.ToString()[0] == '1')
        {
            baseImage.sprite = commonSprite;
        }
        else if (brokenValue > 0)
        {
            // 如果 broken 大于 0，用剪纸卡图
            baseImage.sprite = JianZhi;
        }
        else if (addedValue > 0)
        {
            // 如果 added 大于 0，用生长卡图
            baseImage.sprite = ShengZhang;
        }
        else
        {
            // 否则用默认的生纸卡图
            baseImage.sprite = ShengZhi;
        }
        beSet= true;
    }
}