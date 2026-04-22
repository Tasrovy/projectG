using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardShopping : MonoBehaviour
{
    private GameObject blackOverlay;
    private Transform[] sellCards = new Transform[6];

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
    /// 从 CardManager 根据 DayManager 当天配置的稀有度概率加载6个不重复的数据到物体上
    /// </summary>
    private void LoadRandomCards()
    {
        if (CardManager.Instance == null || CardManager.Instance.cardDatas == null || CardManager.Instance.cardDatas.Count < 6)
        {
            Debug.LogError("[CardShopping] 牌库数据异常或不足6张！");
            return;
        }

        // 获取当天的权重配置
        int dayNum = DayManager.Instance.dayNumber;
        var dayData = DayManager.Instance.daySO.dayDatas[dayNum];
        float prob1 = dayData.probRarity1;
        float prob2 = dayData.probRarity2;
        float prob3 = dayData.probRarity3;

        // 根据千位稀有度分类卡池
        List<CardData> pool1 = new List<CardData>();
        List<CardData> pool2 = new List<CardData>();
        List<CardData> pool3 = new List<CardData>();

        foreach (var data in CardManager.Instance.cardDatas)
        {
            int rarity = (data.id / 1000) % 10;
            if (rarity == 1) pool1.Add(data);
            else if (rarity == 2) pool2.Add(data);
            else if (rarity == 3) pool3.Add(data);
        }

        // 保底机制：连续4次未获得礼物牌时，强制第一个卡位为礼物牌（id万位为1）
        bool pityActive = CardManager.Instance.consecutiveNonGiftCount >= 4;
        CardData forcedGift = null;
        if (pityActive && CardManager.Instance.giftCards != null && CardManager.Instance.giftCards.Count > 0)
        {
            int gIdx = Random.Range(0, CardManager.Instance.giftCards.Count);
            forcedGift = CardManager.Instance.giftCards[gIdx];
            pool1.RemoveAll(d => d.id == forcedGift.id);
            pool2.RemoveAll(d => d.id == forcedGift.id);
            pool3.RemoveAll(d => d.id == forcedGift.id);
            Debug.Log($"[CardShopping] 保底触发！强制插入礼物牌: {forcedGift.name}");
        }

        // 保底触发时 forcedGift 绕过了 PopWeightedRandom，需在此处单独更新计数
        if (forcedGift != null)
        {
            CardManager.Instance.consecutiveNonGiftCount = 0;
            Debug.Log("[CardShopping] 保底礼物牌直接插入，计数重置为0。");
        }

        for (int i = 0; i < sellCards.Length; i++)
        {
            CardData selectedData = (i == 0 && forcedGift != null)
                ? forcedGift
                : PopWeightedRandom(pool1, pool2, pool3, prob1, prob2, prob3);
            if (selectedData == null) continue;
            AssignCardTo(sellCards[i], selectedData);
        }
        Debug.Log($"[CardShopping] 本轮生成完毕，保底计数: {CardManager.Instance.consecutiveNonGiftCount}");
    }

    /// <summary>
    /// 按权重随机从三个卡池中抽取一张卡并移除防止重复（与 CardChoosing 共用同一逻辑）
    /// </summary>
    private CardData PopWeightedRandom(List<CardData> p1, List<CardData> p2, List<CardData> p3, float w1, float w2, float w3)
    {
        // 如果某个卡池空了，那么抽到它的实际概率直接降为0
        float curW1 = p1.Count > 0 ? w1 : 0f;
        float curW2 = p2.Count > 0 ? w2 : 0f;
        float curW3 = p3.Count > 0 ? w3 : 0f;

        float totalWeight = curW1 + curW2 + curW3;

        if (totalWeight <= 0f)
        {
            Debug.LogError("[CardShopping] 严重错误：符合条件的卡牌库存全空了！");
            return null;
        }

        // 轮盘法进行权重随机
        float randomVal = Random.Range(0f, totalWeight);
        List<CardData> selectedPool = null;

        if (randomVal < curW1)
        {
            selectedPool = p1;
        }
        else if (randomVal < curW1 + curW2)
        {
            selectedPool = p2;
        }
        else
        {
            selectedPool = p3;
        }

        // 从确定的池子中等概率随机抽取一张卡，并剔除
        int index = Random.Range(0, selectedPool.Count);
        CardData data = selectedPool[index];
        selectedPool.RemoveAt(index);

        // 无论何种原因抽到礼物牌都重置保底计数，否则+1
        if (data.id / 10000 == 1)
            CardManager.Instance.consecutiveNonGiftCount = 0;
        else
            CardManager.Instance.consecutiveNonGiftCount++;

        return data;
    }

    private void AssignCardTo(Transform cardTransform, CardData data)
    {
        if (cardTransform == null) return;

        CardObject cardObj = cardTransform.GetComponent<CardObject>();
        
        if (cardObj != null)
        {
            if (cardObj.card == null) cardObj.card = new Card();
            cardObj.card.InitCard(data);

            CardDisplayUI displayUI = cardTransform.GetComponent<CardDisplayUI>();
            if (displayUI != null) displayUI.Setup(cardObj.card);

            // 查找直属子物体 Price 并设置其 TextMeshPro 的值为 sale
            Transform priceTransform = cardTransform.Find("price");
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

            // 查找直属子物体 rare 并根据稀有度加载图片
            int rarity = (data.id / 1000) % 10;
            Transform rareTransform = cardTransform.Find("rare");
            if (rareTransform != null)
            {
                Sprite rareSprite = Resources.Load<Sprite>($"UI/Shop/{rarity}");
                if (rareSprite != null)
                {
                    // 兼容 UI Image 和 SpriteRenderer 两种情况
                    Image rareImage = rareTransform.GetComponent<Image>();
                    if (rareImage != null)
                    {
                        rareImage.sprite = rareSprite;
                    }
                    else
                    {
                        SpriteRenderer sr = rareTransform.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = rareSprite;
                        }
                        else
                        {
                            Debug.LogWarning($"[CardShopping] {cardTransform.name}/rare 既没有 Image 也没有 SpriteRenderer 组件！");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[CardShopping] 找不到路径 Resources/UI/Shop/{rarity} 下的图片资源！");
                }
            }
            else
            {
                Debug.LogWarning($"[CardShopping] 找不到物体 {cardTransform.name} 的直属子物体！");
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
        DialogueUIAudio.Instance.PlayCardClickAudio();
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
                int price = selectedCard.card.sale;

                // 金钱检测
                if (DataManager.Instance != null && DataManager.Instance.MoneyNum < price)
                {
                    Debug.LogWarning($"[CardShopping] 购买失败！金钱不足。需要: {price}，当前金钱: {DataManager.Instance.MoneyNum}");
                    return;
                }

                // 扣除金钱
                if (DataManager.Instance != null)
                {
                    // 扣除对应的卡牌售价(-price)
                    DataManager.Instance.Add(4, -price);
                }

                // 将选中的卡牌实体数据加入手牌库
                CardManager.Instance.cardInHand.Add(selectedCard.card);
                Debug.Log($"[CardShopping] 购买确认！卡牌 {selectedCard.card.name} 获取并纳入手中，花费: {price}。");
                
                // 隐藏买走的卡牌
                selectedCard.gameObject.SetActive(false);
                
                // 购买成功后清空当前选中项，必须重选才能再次购买
                selectedCard = null;

                // 查找并更新所有带有 UpdateMoney 组件的 UI 文本
                UpdateMoney[] moneyUpdaters = FindObjectsOfType<UpdateMoney>(true);
                foreach (var updater in moneyUpdaters)
                {
                    updater.UpdateText();
                }
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
