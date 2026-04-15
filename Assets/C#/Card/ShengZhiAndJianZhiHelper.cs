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
        int target = _targetNum;
        if (target <= 0)
        {
            if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
            return;
        }

        JianZhi(target);
    }

    private void JianZhi(int timesLeft)
    {
        CardActionResolver.Instance.StartEffectSelection(
            onConfirm: (selectedCard) => 
            {
                selectedCard.OnBroken();
                CardManager.Instance.BreakCard(selectedCard);

                int remaining = timesLeft - 1;
                if (remaining > 0)
                {
                    JianZhi(remaining);
                }
                else
                {
                    if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
                }
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
                DUEL.Instance.UpdateCardData();
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

    public void ShengZhangTo(int num)
    {
        Debug.Log($"[Helper] 发起生长至选牌，目标数值: {num}，剩余次数: 1");

        CardActionResolver.Instance.StartEffectSelection(
            onConfirm: (selectedCard) =>
            {
                if (selectedCard.GetNatureById(1) != 0 && selectedCard.GetNatureById(1) < num)
                {
                    selectedCard.AddTo(1, num);
                }

                if (selectedCard.GetNatureById(2) != 0 && selectedCard.GetNatureById(2) < num)
                {
                    selectedCard.AddTo(2, num);
                }

                if (selectedCard.GetNatureById(3) != 0 && selectedCard.GetNatureById(3) < num)
                {
                    selectedCard.AddTo(3, num);
                }

                CardManager.Instance.NotifyDeckOrHandChanged();
                DUEL.Instance.UpdateCardData();

                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
            },
            onCancel: () =>
            {
                Debug.Log("[Helper] 生长至被取消，准备退回打出的卡牌。");
                RestoreCallerCard();
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(false);
            }
        );
    }

    private void RestoreCallerCard()
    {
        CardEffect effect = CardEffect.Instance;
        Card caller = effect != null ? effect.CallerCard : null;

        if (effect != null && caller != null)
        {
            effect.MarkConditionFailed(caller);
        }

        if (caller != null)
        {
            Card restoredCard = new Card();
            restoredCard.InitCard(caller); 
            CardManager.Instance.AddCardInHand(restoredCard);
        }
    }

    public void RestoreCallerCardOnInvalidTarget()
    {
        RestoreCallerCard();
    }
}
