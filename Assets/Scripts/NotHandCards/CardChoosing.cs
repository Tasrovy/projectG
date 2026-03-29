using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardChoosing : MonoBehaviour
{
    private Transform card1;
    private Transform card2;
    private Transform card3;

    private Vector3 targetPos1;
    private Vector3 targetPos2; // 新增：保存卡牌2（中间基准）的位置
    private Vector3 targetPos3;

    private GameObject blackOverlay;
    private CardObject selectedCard;

    private void Awake()
    {
        // 动态创建并设置最底层的半透明黑幕
        blackOverlay = new GameObject("BackgroundOverlay");
        blackOverlay.layer = LayerMask.NameToLayer("UI"); // 必须设置所在层级为 UI 才能在 Canvas 内部渲染出来
        blackOverlay.transform.SetParent(this.transform, false);
        blackOverlay.transform.SetAsFirstSibling(); // 移动到子节点的第一位，即渲染最底层
        
        Image img = blackOverlay.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f); // 半透明黑幕
        img.raycastTarget = true; // 拦截并吸收底层UI的所有点击事件
        
        RectTransform rect = blackOverlay.GetComponent<RectTransform>();
        // 强制设置超大尺寸居中，以防当前挂载物体锚点为0导致无法覆盖全屏
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(5000f, 5000f);
        rect.localScale = Vector3.one;

        // 获取卡牌引用
        card1 = transform.Find("card_1");
        card2 = transform.Find("card_2");
        card3 = transform.Find("card_3");
        if (card1 == null || card2 == null || card3 == null)
        {
            Debug.LogError("[CardChoosing] 未能在当前物体下找到 card_1/2/3 子物体！");
        }

        // 记录 Inspector 调好的位置作为目标位置
        if (card1 != null) targetPos1 = card1.localPosition;
        if (card2 != null) targetPos2 = card2.localPosition; // 保存中间基准点
        if (card3 != null) targetPos3 = card3.localPosition;
    }

    private void OnEnable()
    {
        selectedCard = null; // 每次打开界面时，重置之前选中的状态

        /* 暂时假设 cardUISum 恒定保持 true，保留代码备用
        // 当脚本被启用时，也要确保兄弟卡牌组合激活以显示内容
        Transform cardUISum = transform.parent != null ? transform.parent.Find("cardUISum") : null;
        if (cardUISum != null)
        {
            cardUISum.gameObject.SetActive(true);
        }
        */

        LoadRandomCards();
        StartCoroutine(PlaySlideAnimation());
    }

    /// <summary>
    /// 功能1：从 CardManager 随机加载三个不重复的数据到物体上
    /// </summary>
    private void LoadRandomCards()
    {
        if (CardManager.Instance == null)
        {
            Debug.LogError("[CardChoosing] CardManager.Instance 是空的！可能是执行顺序问题。");
            return;
        }
        if (CardManager.Instance.cardDatas == null || CardManager.Instance.cardDatas.Count == 0)
        {
            Debug.LogError("[CardChoosing] CardManager.Instance.cardDatas 为空或为0！牌库没有加载成功。");
            return;
        }
        
        List<CardData> pool = new List<CardData>(CardManager.Instance.cardDatas);
        if (pool.Count < 3)
        {
            Debug.LogWarning($"[CardChoosing] 牌库数据不足3张！当前数量: {pool.Count}");
            return;
        }

        AssignCardTo(card1, PopRandom(pool));
        AssignCardTo(card2, PopRandom(pool));
        AssignCardTo(card3, PopRandom(pool));
    }

    private void AssignCardTo(Transform cardTransform, CardData data)
    {
        if (cardTransform == null)
        {
            Debug.LogError("[AssignCardTo] 传进来的 Transform 为空！");
            return;
        }

        CardObject cardObj = cardTransform.GetComponent<CardObject>();
        
        if (cardObj != null)
        {
            if (cardObj.card == null) cardObj.card = new Card();
            cardObj.card.InitCard(data);
            
            Debug.Log($"[AssignCardTo] 给物体 {cardTransform.name} 赋予了卡牌: {cardObj.card.name} / ID: {cardObj.card.id}");
        }
        else
        {
            Debug.LogError($"[AssignCardTo] 物体 {cardTransform.name} 身上没找到 CardObject 脚本！请检查组件挂载。");
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
    /// 功能3：卡牌从中间向两侧平移，动画逐渐变慢停在原位
    /// </summary>
    private IEnumerator PlaySlideAnimation()
    {
        float duration = 0.6f;
        float elapsed = 0f;
        
        // 如果此节点启用了自动布局（如HorizontalLayoutGroup），暂时禁用它，否则卡牌位置会被强制覆盖无法滑动
        LayoutGroup layoutGroup = GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        // 获取中心位置：以 card2 为中心基准发起平移，更准确
        float centerX = card2 != null ? targetPos2.x : 0f;
        
        Vector3 start1 = card1 != null ? new Vector3(centerX, targetPos1.y, targetPos1.z) : Vector3.zero;
        Vector3 start3 = card3 != null ? new Vector3(centerX, targetPos3.y, targetPos3.z) : Vector3.zero;

        // 初始移动到中间
        if (card1 != null) card1.localPosition = start1;
        if (card3 != null) card3.localPosition = start3;

        while (elapsed < duration)
        {
            // 使用 unscaledDeltaTime 防止弹窗时游戏暂停导致动画卡死
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float curve = 1f - Mathf.Pow(1f - t, 3f);

            if (card1 != null) card1.localPosition = Vector3.LerpUnclamped(start1, targetPos1, curve);
            if (card3 != null) card3.localPosition = Vector3.LerpUnclamped(start3, targetPos3, curve);

            yield return null;
        }

        // 保证停在绝对精度的目标位置
        if (card1 != null) card1.localPosition = targetPos1;
        if (card3 != null) card3.localPosition = targetPos3;

        // 动画播完，如果之前有布局组件则恢复其掌管
        if (layoutGroup != null) layoutGroup.enabled = true;

        Debug.Log("[CardChoosing] 卡牌滑动动画完成，已停在目标位置。");
    }

    #region 按钮与事件功能
    /// <summary>
    /// 被 CardObject 点击时调用，记录当前玩家选中的卡牌
    /// </summary>
    public void SelectCard(CardObject cardObj)
    {
        selectedCard = cardObj;
        Debug.Log($"[CardChoosing] 当前选中了卡牌: {(cardObj != null && cardObj.card != null ? cardObj.card.name : "null")}");
        // 如果你需要做UI上的高亮描边显示特效等，可以在这里附加逻辑
    }

    /// <summary>
    /// 确认功能（供 Inspector 中的 Button UnityEvent 调用）
    /// </summary>
    public void Confirm()
    {
        if (selectedCard != null && selectedCard.card != null)
        {
            if (CardManager.Instance != null && CardManager.Instance.cardInHand != null)
            {
                CardManager.Instance.cardInHand.Add(selectedCard.card);
                Debug.Log($"[CardChoosing] 已确认！卡牌 {selectedCard.card.name} 纳入手中。");
            }
            ClosePage();
        }
        else
        {
            Debug.LogWarning("[CardChoosing] 暂未点击选中任何卡牌！");
        }
    }

    /// <summary>
    /// 跳过功能（供 Inspector 中的 Button UnityEvent 调用）
    /// </summary>
    public void Skip()
    {
        Debug.Log("[CardChoosing] 玩家跳过了此次选卡。");
        ClosePage();
    }

    /// <summary>
    /// 关闭界面并清空所有子物体的数据
    /// 此脚本依靠所在物体 gameObject.SetActive(true) 进行重启(OnEnable)
    /// </summary>
    private void ClosePage()
    {
        ClearCardsData();
        /* 暂时假设 cardUISum 恒定保持 true，保留代码备用
        // 隐藏同级的模型挂载点
        Transform cardUISum = transform.parent != null ? transform.parent.Find("cardUISum") : null;
        if (cardUISum != null) cardUISum.gameObject.SetActive(false);
        */

        // 隐藏自身，黑幕等所有子物体均会自动隐藏
        gameObject.SetActive(false); 
    }

    /// <summary>
    /// 将 3 个实体上的数据指针拔掉，防残留
    /// </summary>
    private void ClearCardsData()
    {
        selectedCard = null;
        if (card1 != null) { var c = card1.GetComponent<CardObject>(); if (c != null) c.card = null; }
        if (card2 != null) { var c = card2.GetComponent<CardObject>(); if (c != null) c.card = null; }
        if (card3 != null) { var c = card3.GetComponent<CardObject>(); if (c != null) c.card = null; }
    }
    #endregion
}
