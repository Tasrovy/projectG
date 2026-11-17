#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Yarn.Unity;

// Test-only automated input. Executes real transaction and card-selection entry points.
public class FlowPlaytestDriver : MonoBehaviour
{
    public static string Result = "Not started";
    float started, next; string previous; int days; bool began;
    bool workshopChecked;
    public static int WorkshopExitsVerified;
    void Start() { started=Time.realtimeSinceStartup; Result="Running"; WorkshopExitsVerified=0; }
    void Update()
    {
        if(Time.realtimeSinceStartup < next)return;
        next=Time.realtimeSinceStartup+0.08f;
        try
        {
            if(Time.realtimeSinceStartup-started>120)throw new Exception("测试超时: "+previous);
            if(!began)
            {
                UISceneManager.Instance.SwitchToScene(SceneType.Talk);
                if(DialogueHandler.Instance==null)return;
                var r=FindObjectsByType<DialogueRunner>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
                r.VariableStorage.SetValue("$MY_NAME","测试主角");
                began=true;GameFlowController.Ensure().BeginRun();return;
            }
            var f=GameFlowController.Instance;
            string current=f.State+":"+f.StageDay;
            if(current!=previous){Debug.Log("[FlowPlaytest] "+current);previous=current;}
            if(f.State=="ConfigError")throw new Exception("流程进入ConfigError，查看Console");
            if(f.State=="Completed" || f.State=="Failed")
            {
                Result=f.State+"; finished player days="+days;Debug.Log("[FlowPlaytest] "+Result);enabled=false;return;
            }
            var runner=FindObjectsByType<DialogueRunner>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(r=>r.IsDialogueRunning);
            if(runner!=null){runner.RequestHurryUpLine();runner.RequestNextLine();return;}
            if(f.State=="PlayerAction")
            {
                if(!workshopChecked) { VerifyWorkshopExit();workshopChecked=true; }
                days++;f.FinishDay();return;
            }
            if(f.State!="TutorialAction")return;
            var cm=CardManager.Instance;
            if(GameFlowController.Allows("Buy"))
            { if(!new ShopTransactionService().TryBuy(new CardFromData(cm.cardDatas.First()).Value))throw new Exception("购买失败");return; }
            if(GameFlowController.Allows("Sell"))
            { if(!new ShopTransactionService().TrySell(cm.cardInHand.First()))throw new Exception("出售失败");return; }
            var resolver=CardActionResolver.Instance;
            var card=resolver.currentMode==CardPlayMode.EffectSelect
                ? cm.cardInHand.FirstOrDefault(c=>CardIdUtility.GetCardType(c.id)==1)
                : cm.cardInHand.FirstOrDefault(GameFlowController.AllowsCard);
            if(card==null)throw new Exception("教学没有可用卡牌");
            typeof(CardActionResolver).GetMethod("HandleSubmit",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(resolver,new object[]{card});
        }
        catch(Exception e){Result="ERROR: "+e.GetBaseException().Message;Debug.LogError("[FlowPlaytest] "+Result);enabled=false;}
    }
    void VerifyWorkshopExit()
    {
        int calendarDay=DayManager.Instance.dayNumber;
        foreach(var destination in new[]{SceneType.Talk,SceneType.Shop,SceneType.Select})
        {
            UISceneManager.Instance.SwitchToScene(SceneType.CardFight);
            DUEL.Instance.Begin();
            var manager=DUELUIObjectManager.Instance;
            DeckViewerUI.Instance.ShowHand();
            var card=CardManager.Instance.cardInHand.FirstOrDefault();
            if(card!=null)manager.GetCardDetailUI().Show(card);
            UISceneManager.Instance.SwitchToScene(destination);
            if(manager.DUELUI.activeSelf || (DeckViewerUI.Instance.viewerPanel!=null && DeckViewerUI.Instance.viewerPanel.activeSelf)
                || manager.GetCardDetailUI().CurrentCard!=null || manager.GetSubmitButton().gameObject.activeSelf
                || manager.GetCancelButton().gameObject.activeSelf)throw new Exception("手工台UI离场残留:"+destination);
            if(DayManager.Instance.dayNumber!=calendarDay)throw new Exception("UI清理意外过天");
            WorkshopExitsVerified++;
        }
        Debug.Log("[FlowPlaytest] workshop exits verified: "+WorkshopExitsVerified);
    }
    class CardFromData { public Card Value;public CardFromData(CardData d){Value=new Card();Value.InitCard(d);} }
}
#endif
