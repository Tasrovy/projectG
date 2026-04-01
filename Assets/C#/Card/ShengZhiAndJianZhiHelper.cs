using UnityEngine;
using UnityEngine.Events;

public class ShengZhiAndJianZhiHelper : Singleton<ShengZhiAndJianZhiHelper>
{
    private int _targetNum; 

    public void SetNum(int num)
    {
        _targetNum = num;
    }

    public void ShengZhi()
    {
        CardSelector.Instance.StartSelection(
            onConfirm: (selectedCard) => 
            {
                Card templateSnapshot = new Card();
                templateSnapshot.InitCard(selectedCard);
                int copyNum = _targetNum; 

                DayManager.Instance.GetNextDayEvent().AddListener(() => 
                {
                    for (int i = 0; i < copyNum; i++)
                    {
                        Card newCard = new Card();
                        newCard.InitCard(templateSnapshot); 
                        newCard.OnAdded(); 
                        CardManager.Instance.AddCardInHand(newCard); 
                    }
                });
                
                // 告诉特效管理器：操作成功完成，继续下一个特效
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
            },
            onCancel: () => 
            {
                RestoreCallerCard(); 
                // 告诉特效管理器：操作取消，中断特效链
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(false);
            }
        );
    }

    public void JianZhi()
    {
        CardSelector.Instance.StartSelection(
            onConfirm: (selectedCard) => 
            {
                selectedCard.OnBroken();
                CardManager.Instance.BreakCard(selectedCard);
                
                // 告诉特效管理器：操作成功完成
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
            },
            onCancel: () => 
            {
                RestoreCallerCard(); 
                // 告诉特效管理器：操作取消
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(false);
            }
        );
    }

    /// <summary>
    /// 【新增】：开始生长（增加属性）流程。支持连续多次选牌。
    /// </summary>
    public void ShengZhang(int amount, int timesLeft)
    {
        Debug.Log($"[Helper] 发起生长选牌，每次增加: {amount}，剩余次数: {timesLeft}");

        CardSelector.Instance.StartSelection(
            onConfirm: (selectedCard) => 
            {
                // 1. 给选中的卡牌增加属性
                selectedCard.Add(1, amount);
                selectedCard.Add(2, amount);
                selectedCard.Add(3, amount);
                
                Debug.Log($"[Helper] 成功将 {selectedCard.name} 的属性增加了 {amount}。");
                
                // 通知UI刷新手牌数据展示
                CardManager.Instance.NotifyDeckOrHandChanged();

                // 2. 检查是否还需要继续选牌
                int remaining = timesLeft - 1;
                if (remaining > 0)
                {
                    Debug.Log($"[Helper] 还有 {remaining} 次机会，再次唤起选牌UI...");
                    ShengZhang(amount, remaining); // 递归调用，再次开启UI
                }
                else
                {
                    // 次数全部用尽，正式通知特效管理器继续往下走
                    Debug.Log($"[Helper] 生长次数全部用完，结束本次技能。");
                    if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
                }
            },
            onCancel: () => 
            {
                Debug.Log("[Helper] 生长被取消，准备退回打出的卡牌。");
                RestoreCallerCard(); 
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(false);
            }
        );
    }

    private void RestoreCallerCard()
    {
        Card caller = CardEffect.Instance.CallerCard;
        if (caller != null)
        {
            Card restoredCard = new Card();
            restoredCard.InitCard(caller); 
            CardManager.Instance.AddCardInHand(restoredCard);
        }
    }
}