using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardShopping : MonoBehaviour
{
    private GameObject blackOverlay;
    private Transform[] sellCards = new Transform[6];

    [Tooltip("概率值，从0到100")]
    [Range(0, 100)][SerializeField][Header("不稀有")]
    private int CardChance_1 = 60;
    [Range(0, 100)][SerializeField][Header("一般")]
    private int CardChance_2 = 30;
    [Range(0, 100)][SerializeField][Header("稀有")]
    private int CardChance_3 = 10;

    private CardObject selectedCard;

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
        selectedCard = null;
        
        // 确保所有商品卡牌位每次打开都处于激活可见状态，因为有的可能上次被买走隐藏了
        for (int i = 0; i < sellCards.Length; i++)
        {
            if (sellCards[i] != null) 
            {
                sellCards[i].gameObject.SetActive(true);
            }
        }

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

        // 决定6个卡位的类型要求，保证三种类型至少各出现一次
        List<int> requiredTypes = new List<int> { 1, 2, 3, 0, 0, 0 }; 
        // 0代表任意类型
        
        // 打乱类型要求
        for (int i = 0; i < requiredTypes.Count; i++)
        {
            int temp = requiredTypes[i];
            int randomIndex = Random.Range(i, requiredTypes.Count);
            requiredTypes[i] = requiredTypes[randomIndex];
            requiredTypes[randomIndex] = temp;
        }

        for (int i = 0; i < 6 && pool.Count > 0; i++)
        {
            CardData selectedData = PopRandomWeighted(pool, requiredTypes[i]);
            if (selectedData != null)
            {
                AssignCardTo(sellCards[i], selectedData);
            }
        }
    }

    private CardData PopRandomWeighted(List<CardData> pool, int requiredType)
    {
        // 1. 筛选符合类型要求的卡牌
        List<CardData> validCards = new List<CardData>();
        foreach (var card in pool)
        {
            int cardType = card.id / 10000;
            if (requiredType == 0 || cardType == requiredType)
            {
                validCards.Add(card);
            }
        }

        // 如果没有符合类型的卡牌（比如某类型卡牌已经被抽完），则降级为不限制类型
        if (validCards.Count == 0 && requiredType != 0)
        {
            validCards.AddRange(pool);
        }

        if (validCards.Count == 0) return null;

        // 2. 根据稀有度计算权重
        // 稀有度：(id / 1000) % 10，假设1为普通, 2为稀有, 3为史诗
        // 权重可以自己调整，这里假设 1: 60, 2: 30, 3: 10
        int totalWeight = 0;
        List<int> weights = new List<int>();

        foreach (var card in validCards)
        {
            int rarity = (card.id / 1000) % 10;
            int weight = 10; // 默认权重
            if (rarity == 1) weight = CardChance_1;
            else if (rarity == 2) weight = CardChance_2;
            else if (rarity == 3) weight = CardChance_3;

            weights.Add(weight);
            totalWeight += weight;
        }

        // 3. 随机抽取
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        int selectedIndex = 0;

        for (int i = 0; i < validCards.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue < currentWeight)
            {
                selectedIndex = i;
                break;
            }
        }

        CardData selectedData = validCards[selectedIndex];
        pool.Remove(selectedData);
        return selectedData;
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
        selectedCard = null;
        for (int i = 0; i < sellCards.Length; i++)
        {
            if (sellCards[i] != null)
            {
                var c = sellCards[i].GetComponent<CardObject>();
                if (c != null) c.card = null;
            }
        }
    }

    #region 按钮与事件功能
    /// <summary>
    /// 被 CardObject 点击时调用，记录当前玩家选中的商店卡牌
    /// </summary>
    public void SelectCard(CardObject cardObj)
    {
        selectedCard = cardObj;
        Debug.Log($"[CardShopping] 当前选中了商店卡牌: {(cardObj != null && cardObj.card != null ? cardObj.card.name : "null")}");
    }

    /// <summary>
    /// 确认功能（供 UnityEvent 调用）
    /// 点击后：最近选中的卡牌消失，并加入手牌
    /// </summary>
    public void ConfirmPurchase()
    {
        if (selectedCard != null && selectedCard.card != null)
        {
            if (CardManager.Instance != null && CardManager.Instance.cardInHand != null)
            {
                // 将选中的卡牌实体数据加入手牌库
                CardManager.Instance.cardInHand.Add(selectedCard.card);
                Debug.Log($"[CardShopping] 购买确认！卡牌 {selectedCard.card.name} 获取并纳入手中。");
                
                // 隐藏买走的卡牌
                selectedCard.gameObject.SetActive(false);
                
                // 购买成功后清空当前选中项，必须重选才能再次购买
                selectedCard = null;
            }
            else
            {
                Debug.LogError("[CardShopping] 找不到 CardManager 或 cardInHand 列表被置空！");
            }
        }
        else
        {
            Debug.LogWarning("[CardShopping] 尚未选中任何卡牌或卡牌数据为空！");
        }
    }
    #endregion
}
