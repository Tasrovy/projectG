using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardShopping : MonoBehaviour
{
    private GameObject blackOverlay;
    private Transform[] sellCards = new Transform[6];

    private void Awake()
    {
        // 1. 动态创建并设置最底层的半透明黑幕
        blackOverlay = new GameObject("BackgroundOverlay");
        blackOverlay.layer = LayerMask.NameToLayer("UI");
        blackOverlay.transform.SetParent(this.transform, false);
        blackOverlay.transform.SetAsFirstSibling(); // 移动到最底层
        
        Image img = blackOverlay.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f); // 半透明黑色
        img.raycastTarget = true; // 拦截点击事件，防止穿透
        
        RectTransform rect = blackOverlay.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(5000f, 5000f);
        rect.localScale = Vector3.one;

        // 2. 获取六个商品卡牌的引用 (sellCard_1 到 sellCard_6)
        CardObject[] allCardObjs = GetComponentsInChildren<CardObject>(true);
        foreach (var cObj in allCardObjs)
        {
            if (cObj.name == "sellCard_1") sellCards[0] = cObj.transform;
            if (cObj.name == "sellCard_2") sellCards[1] = cObj.transform;
            if (cObj.name == "sellCard_3") sellCards[2] = cObj.transform;
            if (cObj.name == "sellCard_4") sellCards[3] = cObj.transform;
            if (cObj.name == "sellCard_5") sellCards[4] = cObj.transform;
            if (cObj.name == "sellCard_6") sellCards[5] = cObj.transform;
        }

        for (int i = 0; i < sellCards.Length; i++)
        {
            if (sellCards[i] == null)
            {
                Debug.LogError($"[CardShopping] 未能在当前物体下找到 sellCard_{i + 1} 子物体！");
            }
        }
    }

    private void OnEnable()
    {
        LoadRandomCards();
    }

    /// <summary>
    /// 从 CardManager 随机加载6个不重复的数据到物体上
    /// </summary>
    private void LoadRandomCards()
    {
        if (CardManager.Instance == null || CardManager.Instance.cardDatas == null || CardManager.Instance.cardDatas.Count == 0)
        {
            Debug.LogError("[CardShopping] 牌库数据为空或 CardManager 尚未初始化！");
            return;
        }
        
        List<CardData> pool = new List<CardData>(CardManager.Instance.cardDatas);
        if (pool.Count < 6)
        {
            Debug.LogWarning($"[CardShopping] 牌库数据不足6张！当前数量: {pool.Count}");
        }

        for (int i = 0; i < 6; i++)
        {
            if (pool.Count > 0)
            {
                AssignCardTo(sellCards[i], PopRandom(pool));
            }
        }
    }

    private void AssignCardTo(Transform cardTransform, CardData data)
    {
        if (cardTransform == null) return;

        CardObject cardObj = cardTransform.GetComponent<CardObject>();
        
        if (cardObj != null)
        {
            if (cardObj.card == null) cardObj.card = new Card();
            cardObj.card.InitCard(data);
            
            // 查找直属子物体 Price 并设置其 TextMeshPro 的值为 sale
            Transform priceTransform = cardTransform.Find("Price");
            if (priceTransform != null)
            {
                TextMeshProUGUI priceText = priceTransform.GetComponent<TextMeshProUGUI>();
                if (priceText != null)
                {
                    priceText.text = data.sale.ToString();
                }
                else
                {
                    Debug.LogWarning($"[CardShopping] 找不到物体 {cardTransform.name}/Price 身上的 TextMeshProUGUI 组件！");
                }
            }
            else
            {
                Debug.LogWarning($"[CardShopping] 找不到物体 {cardTransform.name} 的直属子物体 'Price'！");
            }

            Debug.Log($"[CardShopping] 给物体 {cardTransform.name} 赋予了商品: {data.name} 价格: {data.sale}");
        }
        else
        {
            Debug.LogError($"[CardShopping] 物体 {cardTransform.name} 身上没找到 CardObject 脚本！");
        }
    }

    private CardData PopRandom(List<CardData> pool)
    {
        int index = Random.Range(0, pool.Count);
        CardData data = pool[index];
        pool.RemoveAt(index);
        return data;
    }

    /// <summary>
    /// 供 UnityEvent 调用的关闭商店按钮
    /// </summary>
    public void CloseShop()
    {
        ClearCardsData();
        gameObject.SetActive(false); 
    }

    /// <summary>
    /// 清除旧数据防止残留
    /// </summary>
    private void ClearCardsData()
    {
        for (int i = 0; i < sellCards.Length; i++)
        {
            if (sellCards[i] != null)
            {
                var c = sellCards[i].GetComponent<CardObject>();
                if (c != null) c.card = null;
            }
        }
    }
}
