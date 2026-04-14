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
        if (!_deckService.ImplementCardSetFromInitialHandExcel(initialHandExcelPath))
        {
            Debug.LogWarning($"[InitialHandService] 无法按配置构建牌堆，跳过初始手牌应用: {initialHandExcelPath}");
            return;
        }
        Debug.Log($"[InitialHandService] 已按配置构建牌堆，不再根据表生成初始手牌: {initialHandExcelPath}");
    }
}
