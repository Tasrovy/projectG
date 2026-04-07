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
        // 【修改点】：调用 CardActionResolver
        CardActionResolver.Instance.StartEffectSelection(
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
                
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
            },
            onCancel: () => 
            {
                RestoreCallerCard(); 
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(false);
            }
        );
    }

    public void JianZhi()
    {
        // 【修改点】：调用 CardActionResolver
        CardActionResolver.Instance.StartEffectSelection(
            onConfirm: (selectedCard) => 
            {
                selectedCard.OnBroken();
                CardManager.Instance.BreakCard(selectedCard);
                
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
            },
            onCancel: () => 
            {
                RestoreCallerCard(); 
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(false);
            }
        );
    }

    public void ShengZhang(int amount, int timesLeft)
    {
        Debug.Log($"[Helper] 发起生长选牌，每次增加: {amount}，剩余次数: {timesLeft}");

        // 【修改点】：调用 CardActionResolver
        CardActionResolver.Instance.StartEffectSelection(
            onConfirm: (selectedCard) => 
            {
                selectedCard.Add(1, amount);
                selectedCard.Add(2, amount);
                selectedCard.Add(3, amount);
                
                CardManager.Instance.NotifyDeckOrHandChanged();

                int remaining = timesLeft - 1;
                if (remaining > 0)
                {
                    Debug.Log($"[Helper] 还有 {remaining} 次机会，再次唤起选牌UI...");
                    // 递归调用，Resolver 中的状态保存机制完美支持这种做法
                    ShengZhang(amount, remaining); 
                }
                else
                {
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