using System.Collections.Generic;
using UnityEngine;

public class CardRepositoryService
{
    private readonly List<CardData> _cardDatas;
    private readonly List<string> _cardExcelPaths;

    public CardRepositoryService(List<CardData> cardDatas, List<string> cardExcelPaths)
    {
        _cardDatas = cardDatas;
        _cardExcelPaths = cardExcelPaths;
    }

    public void LoadAllCards()
    {
        _cardDatas.Clear();

        foreach (string path in _cardExcelPaths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;

            CardDatabaseSO databaseSO = ExcelLoader.Instance.ReadExcel(path);
            if (databaseSO == null || databaseSO.allCards.Count == 0)
            {
                Debug.LogWarning($"[CardRepository] 无法加载数据或数据为空，已跳过文件: {path}");
                continue;
            }

            _cardDatas.AddRange(databaseSO.allCards);
        }

        Debug.Log($"[CardRepository] 所有表格加载完毕，共加载了 {_cardDatas.Count} 张卡牌数据。");
    }

    public CardData GetCardDataById(int id)
    {
        return _cardDatas.Find(c => c.id == id);
    }
}
