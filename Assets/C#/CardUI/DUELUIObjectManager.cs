using UnityEngine;
using UnityEngine.UI;

public class DUELUIObjectManager : Singleton<DUELUIObjectManager>
{
    private GameObject _duelUI;

    /// <summary>
    /// 对外公开的 DUELUI 引用。
    /// 使用属性封装，确保在被访问时如果尚未初始化，则立即触发初始化。
    /// </summary>
    public GameObject DUELUI
    {
        get
        {
            if (_duelUI == null)
            {
                InitializeUI();
            }
            return _duelUI;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        // 尝试在自己的 Awake 中初始化
        InitializeUI();
    }

    /// <summary>
    /// 核心初始化逻辑：加载资源、寻找/创建画布、实例化
    /// </summary>
    private void InitializeUI()
    {
        // 如果已经实例化过了，直接返回
        if (_duelUI != null) return;

        // 1. 加载 Prefab
        GameObject prefab = Resources.Load<GameObject>("Prefabs/DUELUI");
        if (prefab == null)
        {
            Debug.LogError("DUELUIObjectManager: 未能在 Resources/Prefabs/ 路径下找到 DUELUI 预制体！");
            return;
        }

        // 2. 寻找场景中的 Canvas（画布）
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        
        // 如果场景里没有 Canvas，则自动创建一个（保证 UI 能显示）
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.layer = LayerMask.NameToLayer("UI");
            Debug.LogWarning("场景中未发现 Canvas，已自动创建。");
        }

        // 3. 实例化并设置为 Canvas 的子物体
        _duelUI = Instantiate(prefab, canvas.transform);
        
        // 4. 重构命名并重置 UI 布局坐标
        _duelUI.name = "DUELUI";
    }

    /// <summary>
    /// 激活/显示整个 DUEL UI 界面
    /// </summary>
    public void ShowUI()
    {
        DUELUI.SetActive(true);
    }

    /// <summary>
    /// 关闭/隐藏整个 DUEL UI 界面
    /// </summary>
    public void HideUI()
    {
        DUELUI.SetActive(false);
    }

    // --- 以下接口内部统一使用大写的 DUELUI 属性，以确保安全访问 ---

    /// <summary>
    /// 获取提交按钮：DUELUI -> SubmitButton -> Submit
    /// </summary>
    public Button GetSubmitButton()
    {
        return DUELUI.transform.Find("SubmitButton/Submit").GetComponent<Button>();
    }

    /// <summary>
    /// 获取取消按钮：DUELUI -> SubmitButton -> Cancel
    /// </summary>
    public Button GetCancelButton()
    {
        return DUELUI.transform.Find("SubmitButton/Cancel").GetComponent<Button>();
    }

    /// <summary>
    /// 获取结束战斗按钮：DUELUI -> EndFlightButton
    /// </summary>
    public Button GetEndFightButton()
    {
        // 路径依据你的结构图：EndFlightButton
        return DUELUI.transform.Find("EndFlightButton").GetComponent<Button>();
    }

    /// <summary>
    /// 获取卡牌区域：DUELUI -> CradZone
    /// </summary>
    public Transform GetCardZoneTransform()
    {
        // 路径依据你的结构图拼写：CradZone
        return DUELUI.transform.Find("CradZone");
    }

    /// <summary>
    /// 获取卡组对象：DUELUI -> CardSet
    /// </summary>
    public GameObject GetCardSetGameObject()
    {
        return DUELUI.transform.Find("CardSet").gameObject;
    }

    /// <summary>
    /// 获取战斗对话根节点：DUELUI -> BattleDialog
    /// </summary>
    public RectTransform GetBattleDialogRoot()
    {
        Transform t = DUELUI.transform.Find("BattleDialog");
        return t != null ? t.GetComponent<RectTransform>() : null;
    }

    /// <summary>
    /// 获取立绘 Image：DUELUI -> BattleDialog -> Portrait
    /// </summary>
    public Image GetBattlePortraitImage()
    {
        Transform t = DUELUI.transform.Find("BattleDialog/Portrait");
        return t != null ? t.GetComponent<Image>() : null;
    }

    /// <summary>
    /// 获取对话框根节点：DUELUI -> BattleDialog -> Bubble
    /// </summary>
    public GameObject GetBattleDialogBubble()
    {
        Transform t = DUELUI.transform.Find("BattleDialog/Bubble");
        return t != null ? t.gameObject : null;
    }

    /// <summary>
    /// 获取对话文本：DUELUI -> BattleDialog -> Bubble -> Text
    /// </summary>
    public Text GetBattleDialogText()
    {
        Transform t = DUELUI.transform.Find("BattleDialog/Bubble/Text");
        return t != null ? t.GetComponent<Text>() : null;
    }

    /// <summary>
    /// 获取对话框透明度组件（若缺失则自动补一个）
    /// </summary>
    public CanvasGroup GetBattleDialogCanvasGroup()
    {
        GameObject bubble = GetBattleDialogBubble();
        if (bubble == null) return null;

        CanvasGroup group = bubble.GetComponent<CanvasGroup>();
        if (group == null) group = bubble.AddComponent<CanvasGroup>();
        return group;
    }
}
