using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DUEL : Singleton<DUEL>
{
    public GameObject cardPrefab;
    public Transform cardParent;
    public UnityEvent OnBeginDUEL = new UnityEvent();
    public UnityEvent OnEndDUEL = new UnityEvent();
    public List<GameObject> activeObjects = new List<GameObject>();
    public Button endButton;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("创建决斗单例");
        OnBeginDUEL.AddListener(InitCardObject);
        OnEndDUEL.AddListener(DestroyCardObject);
        endButton.onClick.AddListener(End);
        endButton.gameObject.SetActive(false);
    }
    
    public void Begin()
    {
        endButton.gameObject.SetActive(true);
        Debug.Log("准备开始决斗");
        OnBeginDUEL?.Invoke();
    }

    public void End()
    {
        OnEndDUEL?.Invoke();
        Debug.Log("结束决斗");
        endButton.gameObject.SetActive(false);
        //测试用
        Begin();
    }

    public void InitCardObject()
    {
        // 1. 清理activeObjects中的null引用
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i] == null)
            {
                activeObjects.RemoveAt(i);
            }
        }

        // 2. 创建从卡牌到现有物体的映射
        Dictionary<Card, GameObject> cardToObjectMap = new Dictionary<Card, GameObject>();

        // 遍历现有物体，构建映射
        for (int i = 0; i < activeObjects.Count; i++)
        {
            GameObject obj = activeObjects[i];
            if (obj != null)
            {
                CardObject cardObject = obj.GetComponent<CardObject>();
                if (cardObject != null && cardObject.card != null)
                {
                    cardToObjectMap[cardObject.card] = obj;
                }
            }
        }

        // 3. 处理手牌中的每张卡牌
        List<GameObject> newActiveObjects = new List<GameObject>();
        List<Card> currentHandCards = CardManager.Instance.cardInHand;

        for (int i = 0; i < currentHandCards.Count; i++)
        {
            Card card = currentHandCards[i];

            // 如果已存在该卡牌的物体，重用
            if (cardToObjectMap.TryGetValue(card, out GameObject existingObject))
            {
                newActiveObjects.Add(existingObject);
                // 从映射中移除，避免后续重复使用
                cardToObjectMap.Remove(card);
                Debug.Log($"[DUEL] 保留卡牌物体: {card.name} (ID:{card.id})");
            }
            else
            {
                // 创建新物体
                GameObject go = Instantiate(cardPrefab, cardParent);
                CardObject cardObj = go.GetComponent<CardObject>();
                if (cardObj != null)
                {
                    cardObj.SetCard(card);
                }
                newActiveObjects.Add(go);
                Debug.Log($"[DUEL] 创建新卡牌物体: {card.name} (ID:{card.id})");
            }
        }

        // 4. 销毁剩余的、不再需要的物体
        foreach (var kvp in cardToObjectMap)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
                Debug.Log($"[DUEL] 销毁卡牌物体: {kvp.Key.name} (ID:{kvp.Key.id})");
            }
        }

        // 5. 更新活动物体列表
        activeObjects = newActiveObjects;

        Debug.Log($"[DUEL] 卡牌物体更新完成: 手牌{currentHandCards.Count}张，物体{activeObjects.Count}个");
    }

    public void DestroyCardObject()
    {
        if (activeObjects == null || activeObjects.Count == 0) return;

        // 1. 遍历列表并摧毁物体
        for (int i = 0; i < activeObjects.Count; i++)
        {
            if (activeObjects[i] != null)
            {
                Destroy(activeObjects[i]);
            }
        }

        // 2. 核心：清空列表内容
        activeObjects.Clear();
        
        Debug.Log("对决结束：所有卡牌物体已清理。");
    }
}