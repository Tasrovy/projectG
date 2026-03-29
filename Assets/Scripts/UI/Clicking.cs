using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Clicking : MonoBehaviour, IPointerClickHandler
{
    [Header("在此添加需要执行的【函数调用】")]
    [Tooltip("只要这里检测到被点击（或调用TriggerActions），列表里的函数就会依次执行。")]
    public UnityEvent onClickActions;

    // ----- 如果它被挂在 UI 控件上（如Image, Image透明区域） -----
    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerActions();
    }

    // ----- 如果它被挂在有Collider的 2D/3D 物理对象上 -----
    private void OnMouseDown()
    {
        TriggerActions();
    }

    /// <summary>
    /// 其他脚本如果想主动触发它，可以直接调用这个方法
    /// </summary>
    public void TriggerActions()
    {
        if (onClickActions != null)
        {
            onClickActions.Invoke();
        }
    }

    // =========================================================================
    // 💡 适配器：突破 Unity 面板 UnityEvent 最多只支持传 1 个参数的限制！
    // 用法：由于面板不支持填传多个int的函数，您可以把当前这个 gameObject 拖给面板，
    // 选择下面这些 Call_ 开头的扩展方法，通过【特定格式的字符串】就能一口气传多个值给你对应的系统了。
    // =========================================================================

    /// <summary>
    /// 开始对话（参数：节点名称字符串）
    /// 例如填：main_start
    /// </summary>
    public void Call_StartDialogue(string nodeName)
    {
        if (DialogueHandler.Instance != null)
            DialogueHandler.Instance.StartDialogue(nodeName);
        else
            Debug.LogWarning("[Clicking] 场景中未找到 DialogueHandler.Instance！");
    }

    /// <summary>
    /// 切换场景（参数：SceneType 的名字）
    /// 例如填：Begin 或 Room1
    /// </summary>
    public void Call_SwitchToScene(string sceneTypeName)
    {
        if (System.Enum.TryParse(sceneTypeName, true, out SceneType parsedScene))
        {
            if (UISceneManager.Instance != null)
                UISceneManager.Instance.SwitchToScene(parsedScene);
        }
        else
        {
            Debug.LogWarning($"[Clicking] 无法将字符串 {sceneTypeName} 解析成 SceneType!");
        }
    }

    /// <summary>
    /// 设置对话完成后给多少属性三连（参数：p1,p2,p3用英文逗号分隔）
    /// 例如填：10,20,-5
    /// </summary>
    public void Call_SetDialogueProperties(string valuesSplitByComma)
    {
        string[] parts = valuesSplitByComma.Split(',');
        if (parts.Length >= 3)
        {
            int p1 = int.Parse(parts[0].Trim());
            int p2 = int.Parse(parts[1].Trim());
            int p3 = int.Parse(parts[2].Trim());
            if (DialogueHandler.Instance != null)
                DialogueHandler.Instance.SetDialogueProperties(p1, p2, p3);
        }
        else
        {
            Debug.LogWarning("[Clicking] 属性格式错误！应为三位数用英文逗号隔开，类似: 10,-5,3");
        }
    }

    /// <summary>
    /// 设置对话完成后的金钱掉落（参数：min,max用英文逗号分隔）
    /// 例如填：10,50
    /// </summary>
    public void Call_SetDialogueMoney(string valuesSplitByComma)
    {
        string[] parts = valuesSplitByComma.Split(',');
        if (parts.Length >= 2)
        {
            int min = int.Parse(parts[0].Trim());
            int max = int.Parse(parts[1].Trim());
            if (DialogueHandler.Instance != null)
                DialogueHandler.Instance.SetDialogueMoney(min, max);
        }
        else
        {
            Debug.LogWarning("[Clicking] 金钱范围格式错误！应为两位数用英文逗号隔开，类似: 10,50");
        }
    }
}
