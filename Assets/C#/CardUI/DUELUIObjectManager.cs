using UnityEngine;
using UnityEngine.UI;

public class DUELUIObjectManager : Singleton<DUELUIObjectManager>
{
    private GameObject _duelUI;
    private CardDetailUI _cardDetailUI;
    private Canvas _canvas;
    private GameObject _canvasObj;

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
        InitializeUI();
    }

    /// <summary>
    /// 共享的 Canvas 获取/创建逻辑
    /// </summary>
    private Canvas GetOrCreateCanvas()
    {
        if (_canvas != null) return _canvas;

        CardUICanvas cardUIScript = Object.FindAnyObjectByType<CardUICanvas>();
        if (cardUIScript != null)
        {
            _canvas = cardUIScript.GetComponent<Canvas>();
            if (_canvas != null)
            {
                _canvasObj = _canvas.gameObject;
                Debug.Log("找到了 Canvas: " + _canvasObj.name);
                return _canvas;
            }
        }

        _canvasObj = new GameObject("Canvas");
        _canvas = _canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _canvasObj.AddComponent<GraphicRaycaster>();
        _canvasObj.layer = LayerMask.NameToLayer("UI");
        Debug.LogWarning("场景中未发现 CardUICanvas，已自动创建并配置。");

        return _canvas;
    }

    /// <summary>
    /// 核心初始化逻辑：加载资源、获取共享 Canvas、实例化
    /// </summary>
    private void InitializeUI()
    {
        if (_duelUI != null) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/DUELUI");
        if (prefab == null)
        {
            Debug.LogError("DUELUIObjectManager: 未能在 Resources/Prefabs/ 路径下找到 DUELUI 预制体！");
            return;
        }

        _duelUI = Instantiate(prefab, GetOrCreateCanvas().transform);
        _duelUI.name = "DUELUI";
    }

    private void InitializeCardDetailUI()
    {
        if (_cardDetailUI != null) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/CardDetailUI");
        if (prefab == null)
        {
            Debug.LogError("DUELUIObjectManager: 未能在 Resources/Prefabs/ 路径下找到 CardDetailUI 预制体！");
            return;
        }

        GameObject go = Instantiate(prefab, GetOrCreateCanvas().transform);
        go.name = "CardDetailUI";
        _cardDetailUI = go.GetComponent<CardDetailUI>();
        if (_cardDetailUI == null)
            Debug.LogError("CardDetailUI 预制体上缺少 CardDetailUI 组件！");
    }

    /// <summary>
    /// 获取 CardDetailUI 组件（懒加载）
    /// </summary>
    public CardDetailUI GetCardDetailUI()
    {
        if (_cardDetailUI == null)
            InitializeCardDetailUI();
        return _cardDetailUI;
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
        return DUELUI.transform.Find("EndFlightButton").GetComponent<Button>();
    }

    /// <summary>
    /// 获取卡牌区域：DUELUI -> CradZone
    /// </summary>
    public Transform GetCardZoneTransform()
    {
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
