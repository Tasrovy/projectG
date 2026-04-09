using UnityEngine;

public class InitialHandService
{
    private readonly DeckService _deckService;

    public InitialHandService(DeckService deckService)
    {
        _deckService = deckService;
    }

    public void ApplyInitialHandFromExcel(
        bool useInitialHandFromExcel,
        string initialHandExcelPath,
        System.Collections.Generic.List<Card> cardInHand,
        System.Func<int, CardData> getCardDataById)
    {
        if (!useInitialHandFromExcel) return;
        if (string.IsNullOrWhiteSpace(initialHandExcelPath)) return;

        InitialHandSO initialHandSO = ExcelLoader.Instance.ReadInitialHandExcel(initialHandExcelPath);
        if (initialHandSO == null || initialHandSO.entries == null || initialHandSO.entries.Count == 0)
        {
            Debug.LogWarning($"[InitialHandService] 初始手牌配置为空或读取失败: {initialHandExcelPath}");
            return;
        }

        cardInHand.Clear();

        foreach (InitialHandEntry entry in initialHandSO.entries)
        {
            if (entry == null || entry.id == 0) continue;

            int count = entry.num <= 0 ? 1 : entry.num;
            for (int i = 0; i < count; i++)
            {
                if (_deckService.TryTakeCardById(entry.id, out Card cardFromDeck))
                {
                    cardInHand.Add(cardFromDeck);
                }
                else
                {
                    CardData data = getCardDataById(entry.id);
                    if (data == null)
                    {
                        Debug.LogWarning($"[InitialHandService] 初始手牌ID不存在: {entry.id}");
                        break;
                    }

                    Card fallbackCard = new Card();
                    fallbackCard.InitCard(data);
                    cardInHand.Add(fallbackCard);
                }
            }
        }

        Debug.Log($"[InitialHandService] 已应用初始手牌配置: {initialHandExcelPath}, 手牌数量={cardInHand.Count}");
    }
}
