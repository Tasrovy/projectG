using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class DialogueHandler : MonoBehaviour
{
    public static DialogueHandler Instance { get; private set; }
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private Button skipDialogueButton;


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
                    var daySO = DayManager.Instance.daySO;
                    if (daySO != null && currentDay < daySO.dayDatas.Count)
                    {
                        string morningNode = daySO.dayDatas[currentDay].dailyDialog;
                        if (!string.IsNullOrEmpty(morningNode))
                        {
                            // 触发当天早晨固定对话
                            StartDialogue(morningNode);

                            // 对话结束后自动转入 Select 场景
                            SetNextSceneType("Select");
                        }
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

    #region 对话呼出与日常结算

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
    /// 【安全一键连招】：强烈推荐过天结算专用！
    /// 尝试播放 deal，无论有没有可用的 deal，随后都会无缝拉起 special 检测并进入第二天！
    /// 提供给 Inspector 里的 Button OnClick 直接调用，参数填第二天早晨的场景名(匹配 SceneType 枚举)。
    /// </summary>
    public void TriggerEndDayWithDeal(string sceneTypeName = "Talk")
    {
        // 先设好一定会过天以及目的场景
        SetAdvanceDayAfterDialogue(true);
        SetNextSceneType(sceneTypeName);

        if (DayManager.Instance == null || dialogueRunner == null) 
        {
            StartCoroutine(EndDialogueRoutine());
            return;
        }

        int currentDay = DayManager.Instance.GetDayNumber();
        bool foundDeal = false;

        for (int i = 1; i <= currentDay; i++)
        {
            int j = PlayerPrefs.GetInt($"DealProgress_{i}", 1);
            string yarnNode = $"deal{i}_{j}";
            
            if (dialogueRunner.YarnProject != null && dialogueRunner.YarnProject.NodeNames.Contains(yarnNode))
            {
                Debug.Log($"[DialogueHandler] 打工结束拦截并启动 deal 对话: {yarnNode}");
                PlayerPrefs.SetInt($"DealProgress_{i}", j + 1);
                PlayerPrefs.Save();
                
                // 【有deal时】：立刻播放！播完后Update函数会自动拉起EndDialogueRoutine去找special并过天。
                StartDialogue(yarnNode);
                foundDeal = true;
                break;
            }
        }
        
        if (!foundDeal)
        {
            // 【没deal时】：直接拉起黑屏，跑去检测今晚有没有special，都没有就安静切去第二天。
            Debug.Log($"[DialogueHandler] 今晚没有 deal，直接拉起过天与 special 检测。");
            StartCoroutine(EndDialogueRoutine());
        }
    }

    #endregion

    /// <summary>
    /// 内部检索函数：获取当前天数下可用的 special 对话节点名。
    /// 每段 special 对话每三天可推进一步，i更小的系列优先。
    /// </summary>
    private string GetAvailableSpecialDialogue()
    {
        if (DayManager.Instance == null || dialogueRunner == null) return null;
        
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
                    Debug.Log($"[DialogueHandler] 睡前优先劫持！发现满足触发条件的 special 对话: {yarnNode} (系列 {i}，进度 {j})");
                    
                    PlayerPrefs.SetInt($"SpecialProgress_{i}", j + 1);
                    PlayerPrefs.Save();
                    
                    return yarnNode; // 将此 Node 返回供黑屏播放队列使用
                }
            }
            else if (j == 1)
            {
                // 如果这个系列的第 1 篇都没写，意味着往后更大的 i 也没写了（特殊对话顺序按i编写），可以直接结束以节省开销
                break;
            }
        }
        
        return null;
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

    #region 场景切换相关

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

    #endregion

    private IEnumerator EndDialogueRoutine()
    {
        // 1. 先播放离场转场动画，并在屏幕完全黑掉的瞬间去清除立绘和背景
        yield return TransitionManager.Instance.PlayTransition(() => 
        {
            if (characterHighlightManager != null)
            {
                characterHighlightManager.ClearVisualsOnTransitionMidpoint();
            }
        });

        // =================== 修改点：拦截过天与 Special 对话 ===================
        // 【重要】：在此时趁着转场完全黑屏的时刻，我们才来判定这一天是否要“跨向第二天”！
        // 如果是过天的流程，必须在结算这一天之前先拽出来晚间的 special 对话进入待办队列！
        if (characterHighlightManager != null && characterHighlightManager.shouldAdvanceDayAfterDialogue)
        {
            string specialNode = GetAvailableSpecialDialogue(); // 去找今天有没有彩蛋对话
            if (!string.IsNullOrEmpty(specialNode))
            {
                // **这就是正巧碰上了有 special 的日子！**
                // 如果发现 special 节点，我们将它硬塞入待办最前面（或尾部）。
                pendingDialogues.Enqueue(specialNode);
                
                // 【绝不能在这里做 DayManager.NextDay()】
                // 而是直接略过：让 shouldAdvanceDayAfterDialogue 保持为 true！
                // 等这段 Special 对话完全结束了，系统会再次发起黑屏转场，再进入到这层判断。
            }
            else
            {
                // 没有 Special 对话了，那就彻彻底底地进行跨天运算！
                if (DayManager.Instance != null)
                {
                    DayManager.Instance.NextDay();
                }
                
                // 结算安全完毕，复位标志位
                characterHighlightManager.shouldAdvanceDayAfterDialogue = false; 
            }
        }
        // ====================================================================

        // 2. 黑屏后第一步：检查是否有排队等候的对话
        if (pendingDialogues.Count > 0)
        {
            string nextScript = pendingDialogues.Dequeue();
            
            // 直接开启下一个对话（调用了自身完整带强制黑屏加载的StartDialogue，所以没问题）
            StartDialogue(nextScript);
            
            // 【为什么要停下立刻 yield break？】
            // 因为如果是去播 special 对话，你绝不可以说“继续正常的向下进行切第二天的场景”，
            // 不然屏幕就会把你切到明天早晨然后才开始播昨天晚上的 special！
            // 所以 yield break 叫停后，新对话演完时会产生新一轮的转场并在那时安稳切换场景！
            yield break;
        }

        // 3. 所有排备流程结束，此时若是需要换场景才会真正切换。
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

    #region 强制过天与数值属性设置

    /// <summary>
    /// 给外部（如直接回家的UI按钮）触发“过天检测”的方法。
    /// 如果触发了 Special，则进入对话流；如果没有，它会自己拉黑屏并跨天切场景。
    /// 可以直接给 UnityEvent 传递诸如 "Talk" 或 "DayMenu" 等字符串。
    /// </summary>
    public void TriggerEndDayDirectly(string sceneTypeName = "Talk")
    {
        // 勾上过天的标记并排入要切换的后置场景
        SetAdvanceDayAfterDialogue(true);
        SetNextSceneType(sceneTypeName);
        
        // 然后，人为触发一次清场与过天检测漏斗
        StartCoroutine(EndDialogueRoutine());
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

    #endregion

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
