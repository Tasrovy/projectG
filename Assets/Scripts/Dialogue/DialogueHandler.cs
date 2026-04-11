using System.Linq;
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

        void Start()
    {
        Yarn.Unity.InMemoryVariableStorage storage = FindAnyObjectByType<Yarn.Unity.InMemoryVariableStorage>();
        if (storage != null && PlayerPrefs.HasKey("PLAYER_CUSTOM_NAME"))
        {
            storage.SetValue("$MY_NAME", PlayerPrefs.GetString("PLAYER_CUSTOM_NAME"));
        }
    }
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

    /// <summary>
    /// StartDialogue 的变体：用于触发 deal 系列对话
    /// 自动根据天数 i 和进度 j 查找合适的 deal{i}_{j} 节点。
    /// i 表示最早可触发的天数，j每触发一次推进。i越小越优先。 
    /// </summary>
    public void StartDialogue_deal()
    {
        if (DayManager.Instance == null || dialogueRunner == null) return;
        
        int currentDay = DayManager.Instance.GetDayNumber();
        
        // 遍历所有的系列 i，i最高不应该超过当前天数，因为i代表这个系列触发的最早天数
        for (int i = 1; i <= currentDay; i++)
        {
            // 从 PlayerPrefs 拿到该系列当前的进度 j，默认为 1
            int j = PlayerPrefs.GetInt($"DealProgress_{i}", 1);
            string yarnNode = $"deal{i}_{j}";
            
            // 使用 Yarn Spinner 提供的 NodeExists 完美替代“从文件夹枚举读取”，
            // 只要你写了这层对话且编译没报错，就会返回 true。
            if (dialogueRunner.YarnProject != null && dialogueRunner.YarnProject.NodeNames.Contains(yarnNode))
            {
                Debug.Log($"[DialogueHandler] 发现并触发优先级最高的 deal 对话: {yarnNode} (系列 {i}，进度 {j})");
                
                // 将该系列的进度推进 1
                PlayerPrefs.SetInt($"DealProgress_{i}", j + 1);
                PlayerPrefs.Save();
                
                StartDialogue(yarnNode);
                return; // 触发后立即返回，保证较小i的优先，且单次只触发一段
            }
        }
        
        Debug.Log($"[DialogueHandler] 商店或交互触发失败，当前没有任何满足条件的 deal 系列对话。");
    }

    /// <summary>
    /// StartDialogue 的变体：用于触发 special 系列对话
    /// 每段 special 对话每三天可推进一次，i更小的系列优先。
    /// </summary>
    public void StartDialogue_special()
    {
        if (DayManager.Instance == null || dialogueRunner == null) return;
        
        int currentDay = DayManager.Instance.GetDayNumber();
        
        // 遍历 special 系列的 i。设定一个安全上限 20（或100），找不到节点就不找了
        for (int i = 1; i <= 20; i++)
        {
            int j = PlayerPrefs.GetInt($"SpecialProgress_{i}", 1);
            string yarnNode = $"special{i}_{j}";
            
            // 如果存在第 i 系列的第 j 段对话
            if (dialogueRunner.YarnProject != null && dialogueRunner.YarnProject.NodeNames.Contains(yarnNode))
            {
                // special序列的判断条件：每三天触发一个（进度 j 对应第 j*3 天或以后才能触发）
                if (currentDay >= j * 3)
                {
                    Debug.Log($"[DialogueHandler] 发现可触发的 special 对话: {yarnNode} (系列 {i}，进度 {j}，当前天数 {currentDay} >= {j*3})");
                    
                    PlayerPrefs.SetInt($"SpecialProgress_{i}", j + 1);
                    PlayerPrefs.Save();
                    
                    StartDialogue(yarnNode);
                    return; // 触发后立即返回
                }
            }
            else if (j == 1)
            {
                // 如果这个系列的第 1 篇都没写，意味着往后更大的 i 也没写了（特殊对话顺序按i编写），可以直接结束以节省开销
                break;
            }
        }
        
        Debug.Log($"[DialogueHandler] 特殊事件触发失败，当前没有满足天数(j*3)或进度的 special 对话。");
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
        // =================强制劫持与数据同步点=================
        // 不论这是第几个场景的 VariableStorage，我们都在它开启对话前强行修正！
        if (PlayerPrefs.HasKey("PLAYER_CUSTOM_NAME"))
        {
            string savedName = PlayerPrefs.GetString("PLAYER_CUSTOM_NAME");
            if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
            {
                dialogueRunner.VariableStorage.SetValue("$MY_NAME", savedName);
            }
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
