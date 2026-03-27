using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Data;
using ExcelDataReader;
using Random = System.Random;

public class CardData
{
    public int id;
    public int cardProperty1;
    public int cardProperty2;
    public int cardProperty3;
    public string cardName;
    public string cardDescription;
    public string effect;
}

public class CardManager : Singleton<CardManager>
{
    // 存储所有的卡牌配置数据
    public List<CardData> cardDatas = new List<CardData>();
    
    public List<Card> cardSet = new List<Card>();
    public List<Card> cardInHand = new List<Card>();

    [Header("Excel文件名 (如: Cards.xlsx)")]
    public string cardExcelPath = "Cards.xlsx";
    private Random rng = new Random();

    protected override void Awake()
    {
        base.Awake();
        LoadAllCards();
    }

    public void DrawCard(int num)
    {
        for (int i = 0; i < num; i++)
        {
            if (cardSet.Count <= 0)
            {
                Debug.LogWarning("牌堆已空，无法继续抽牌！");
                // 这里可以扩展：如果弃牌堆有牌，则将弃牌堆放入牌堆并洗牌
                // TryRefillDeckFromDiscard(); 
                break;
            }

            // 1. 从牌堆顶部取出一张牌 (通常取 index 0)
            Card drawnCard = cardSet[0];
            cardSet.RemoveAt(0);

            // 2. 加入手牌
            cardInHand.Add(drawnCard);
            
            // 3. TODO: 触发UI表现
            // UIManager.Instance.OnCardDrawn(drawnCard);
        }
        
        Debug.Log($"当前手牌数: {cardInHand.Count}，牌堆剩余: {cardSet.Count}");
    }

    public void ShuffleDeck()
    {
        int n = cardSet.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card value = cardSet[k];
            cardSet[k] = cardSet[n];
            cardSet[n] = value;
        }
        Debug.Log("牌堆已重新洗牌。");
    }
    
    public void LoadAllCards()
    {
        cardDatas.Clear();

        DataSet dataSet = ExcelLoader.Instance.ReadExcel(cardExcelPath);

        if (dataSet == null || dataSet.Tables.Count == 0)
        {
            Debug.LogError($"[CardManager] 无法加载 Excel: {cardExcelPath}");
            return;
        }

        DataTable table = dataSet.Tables[0];

        foreach (DataRow row in table.Rows)
        {
            if (row["id"] == System.DBNull.Value || string.IsNullOrWhiteSpace(row["id"].ToString()))
                continue;

            try
            {
                CardData data = new CardData();

                data.id = int.Parse(row["id"].ToString());
                data.cardProperty1 = int.Parse(row["cardProperty1"].ToString());
                data.cardProperty2 = int.Parse(row["cardProperty2"].ToString());
                data.cardProperty3 = int.Parse(row["cardProperty3"].ToString());
                
                data.cardName = row["cardName"].ToString();
                data.cardDescription = row["cardDescription"].ToString();
                data.effect = row["effect"].ToString();

                cardDatas.Add(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardManager] 解析行失败: {e.Message}");
            }
        }

        Debug.Log($"[CardManager] 成功加载 {cardDatas.Count} 张卡牌数据。");
    }

    /// <summary>
    /// 根据 ID 获取卡牌配置
    /// </summary>
    public CardData GetCardDataById(int id)
    {
        return cardDatas.Find(c => c.id == id);
    }

    public void GenCard(CardData data)
    {
        Card card = new Card();
        card.InitCard(data);
        cardSet.Add(card);
    }
    
    public void  AddToCardSet(Card card)
    {
        if (cardInHand.Contains(card))
        {
            cardInHand.Remove(card);
            cardSet.Add(card);
        }
        
    }
}
