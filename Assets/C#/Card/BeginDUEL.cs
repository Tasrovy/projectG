using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DUEL : Singleton<DUEL>
{
    public GameObject cardPrefab;
    public Transform cardParent;
    public UnityEvent OnBeginDUEL = new UnityEvent();
    public UnityEvent OnEndDUEL = new UnityEvent();
    public List<GameObject> activeObjects = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
        OnBeginDUEL.AddListener(InitCardObject);
        OnEndDUEL.AddListener(DestroyCardObject);
    }
    
    public void Begin()
    {
        OnBeginDUEL?.Invoke();
    }

    public void End()
    {
        OnEndDUEL?.Invoke();
    }

    public void InitCardObject()
    {
        DestroyCardObject();
        int num = CardManager.Instance.cardInHand.Count;
        for (int i = 0; i < num; i++)
        {
            GameObject go = Instantiate(cardPrefab, cardParent);
            go.GetComponent<CardObject>().card =  CardManager.Instance.cardInHand[i];
            activeObjects.Add(go);
        }
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