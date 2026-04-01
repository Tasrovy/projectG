using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class DialogueHandler : MonoBehaviour
{
    [System.Serializable]
    public class DayDialogueConfig
    {
        public int dayNumber;
        public string yarnNode;
    }

    public static DialogueHandler Instance { get; private set; }
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private Button skipDialogueButton;

    [Header("按天数自动触发的对话配置")]
    [SerializeField] private List<DayDialogueConfig> dayDialoguesConfig = new List<DayDialogueConfig>();

    private CharacterHighlightManager characterHighlightManager;
    private bool wasDialogueRunning;
    private bool willSwitchScene;
    private SceneType nextSceneType;

    // 对话队列管理
    private Queue<string> pendingDialogues = new Queue<string>();
    private bool isHandlingDialogueSequence = false;
    private int lastCheckedDay = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        

        characterHighlightManager = GetComponent<CharacterHighlightManager>();

        if (skipDialogueButton != null)
        {
            skipDialogueButton.onClick.RemoveListener(HandleSkipDialogueClicked);
            skipDialogueButton.onClick.AddListener(HandleSkipDialogueClicked);
            skipDialogueButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (dialogueRunner == null)
        {
            return;
        }

        // 检测天数是否发生变化，并自动加入队列
        if (DayManager.Instance != null)
        {
            int currentDay = DayManager.Instance.GetDayNumber();
            if (currentDay > 0 && currentDay != lastCheckedDay) // 防止默认0天时无意义判定
            {
                // 【锁】：必须要在 talk 能检测到时才能试图开始天数事件检测，否则等待（不更新 lastCheckedDay）
                GameObject talkObj = GameObject.Find("talk");
                if (talkObj != null && talkObj.activeInHierarchy)
                {
                    lastCheckedDay = currentDay;
                    var config = dayDialoguesConfig.Find(c => c.dayNumber == currentDay);
                    if (config != null && !string.IsNullOrEmpty(config.yarnNode))
                    {
                        StartDialogue(config.yarnNode);
                    }
                }
            }
        }

        bool isDialogueRunning = dialogueRunner.IsDialogueRunning;
        
        // 状态转为结束：当YarnSpinner内部真正结束时执行离场
        if (wasDialogueRunning && !isDialogueRunning)
        {
            HideSkipButton();
            wasDialogueRunning = false;
            StartCoroutine(EndDialogueRoutine());
        }
        // 状态转为运行中：YarnSpinner内部真正启动时（可能要花几帧启动），才拉起UI
        else if (!wasDialogueRunning && isDialogueRunning)
        {
            wasDialogueRunning = true;
            ShowSkipButton();
        }
    }

    public void StartDialogue(string yarnScript)
    {
        // 完整回溯出到底是谁点击/触发的：
        Debug.Log($"[DialogueHandler] 系统正在请求启动节点: {yarnScript}。调用者堆栈为：\n" + System.Environment.StackTrace);

        if (dialogueRunner != null)
        {
            // 如果当前有对话正在运行，或者正在处理入场/离场中，或者队列里已经有积压任务
            if (dialogueRunner.IsDialogueRunning || wasDialogueRunning || isHandlingDialogueSequence || pendingDialogues.Count > 0)
            {
                pendingDialogues.Enqueue(yarnScript);
            }
            else
            {
                isHandlingDialogueSequence = true;
                StartCoroutine(StartDialogueRoutine(yarnScript));
            }
        }
    }

    private IEnumerator StartDialogueRoutine(string yarnScript)
    {
        // 保证顺序：先执行转场黑屏
        yield return TransitionManager.Instance.PlayTransition();
        
        // 【强制阻塞】：等到场景内名叫 "talk" 的物体被激活后，才允许Yarn开始执行指令和加载物体
        GameObject talkObj = null;
        while (talkObj == null || !talkObj.activeInHierarchy)
        {
            talkObj = GameObject.Find("talk");
            yield return null;
        }

        // 向YarnSpinner发送开始指令。不再人为提前去抢状态或显示按钮
        // 接下来由Update自动完美捕捉起跑的瞬间！
        dialogueRunner.StartDialogue(yarnScript);
    }

    /// <summary>
    /// 提供给UnityEvent调用的函数，传入SceneType（由枚举名转换）
    /// 并保存起来用于对话结束后的场景切换
    /// </summary>
    public void SetNextSceneType(string sceneTypeName)
    {
        if (System.Enum.TryParse(sceneTypeName, true, out SceneType parsedScene))
        {
            nextSceneType = parsedScene;
            willSwitchScene = true;
        }
        else
        {
            Debug.LogWarning($"[DialogueHandler] Cannot parse SceneType from string: {sceneTypeName}");
        }
    }

    /// <summary>
    /// 提供给代码或支持Enum的UnityEvent使用
    /// </summary>
    public void SetNextSceneByEnum(SceneType sceneType)
    {
        nextSceneType = sceneType;
        willSwitchScene = true;
    }

    private IEnumerator EndDialogueRoutine()
    {
        // 先播放离场转场动画，并在屏幕完全黑掉的瞬间去清除立绘和背景
        yield return TransitionManager.Instance.PlayTransition(() => 
        {
            if (characterHighlightManager != null)
            {
                characterHighlightManager.ClearVisualsOnTransitionMidpoint();
            }
        });

        // 黑屏后第一步：检查是否有排队等候的对话
        if (pendingDialogues.Count > 0)
        {
            string nextScript = pendingDialogues.Dequeue();
            // 直接开启下一个对话（不需要切场景或重置序列状态）
            dialogueRunner.StartDialogue(nextScript);
            yield break;
        }

        // 黑屏/转场彻底结束后，检查是否有存储的待切场景
        if (willSwitchScene)
        {
            willSwitchScene = false;   // 状态复位
            
            // 调用存在的 UISceneManager 切场景接口
            if (UISceneManager.Instance != null)
            {
                UISceneManager.Instance.SwitchToScene(nextSceneType);
            }
            else
            {
                Debug.LogError("UISceneManager.Instance is null, cannot switch scene.");
            }
        }

        // 所有流程结束，释放锁
        isHandlingDialogueSequence = false;
    }

    public void SetDialogueProperties(int p1, int p2, int p3)
    {
        if (characterHighlightManager == null)
        {
            characterHighlightManager = GetComponent<CharacterHighlightManager>();
        }

        if (characterHighlightManager != null)
        {
            characterHighlightManager.SetDialogueCompleteProperties(p1, p2, p3);
        }
        else
        {
            Debug.LogError("CharacterHighlightManager not found on the same GameObject.");
        }
    }

    public void SetDialogueMoney(int money)
    {
        if (characterHighlightManager == null)
        {
            characterHighlightManager = GetComponent<CharacterHighlightManager>();
        }

        if (characterHighlightManager != null)
        {
            characterHighlightManager.SetDialogueCompleteMoney(money);
        }
        else
        {
            Debug.LogError("CharacterHighlightManager not found on the same GameObject.");
        }
    }

    /// <summary>
    /// 提供给外部（如 UnityEvent 或其他脚本）调用，用于控制本次对话结束后是否自动进入下一天。
    /// 勾选/传 true 代表对话结束后会执行 DayManager.Instance.NextDay()。
    /// </summary>
    public void SetAdvanceDayAfterDialogue(bool advance)
    {
        if (characterHighlightManager == null)
        {
            characterHighlightManager = GetComponent<CharacterHighlightManager>();
        }
        
        if (characterHighlightManager != null)
        {
            characterHighlightManager.SetAdvanceDayAfterDialogue(advance);
        }
    }

    private async void HandleSkipDialogueClicked()
    {
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
        {
            HideSkipButton();
            return;
        }

        if (skipDialogueButton != null)
        {
            skipDialogueButton.interactable = false;
        }

        await dialogueRunner.Stop();

        HideSkipButton();
        if (skipDialogueButton != null)
        {
            skipDialogueButton.interactable = true;
        }
        wasDialogueRunning = false;

        // 如果玩家因为跳过而人工强停对话结束，那么同样触发离场的转场
        StartCoroutine(EndDialogueRoutine());
    }

    private void ShowSkipButton()
    {
        if (skipDialogueButton != null)
        {
            skipDialogueButton.gameObject.SetActive(true);
        }
    }

    private void HideSkipButton()
    {
        if (skipDialogueButton != null)
        {
            skipDialogueButton.gameObject.SetActive(false);
        }
    }
}
