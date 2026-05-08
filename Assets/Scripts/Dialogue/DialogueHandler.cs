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
    private int lastCheckedDay = 1;

    // deal/special/doWork 对话进度（内存存储，每次启动重置）
    private Dictionary<int, int> _dealProgress = new Dictionary<int, int>();
    private Dictionary<int, int> _specialProgress = new Dictionary<int, int>();
    private int _doWorkI = 1;
    private int _doWorkJ = 1;

    // 失败对话标志：当前正在播放失败对话
    private bool _isPlayingFailedDialogue = false;

    // 延迟失败检定：记录本次过天流程应当在前置对话结束后触发的失败节点
    private string _deferredFailureNode = null;

    private Coroutine StartManagedCoroutine(IEnumerator routine)
    {
        if (routine == null)
        {
            return null;
        }

        if (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            return StartCoroutine(routine);
        }

        if (TransitionManager.Instance != null)
        {
            return TransitionManager.Instance.StartCoroutine(routine);
        }

        Debug.LogError("[DialogueHandler] 无法启动协程：DialogueHandler 未激活且 TransitionManager 不可用。");
        return null;
    }

    private static Transform FindAncestorByName(Transform start, string name)
    {
        Transform current = start;
        while (current != null)
        {
            if (current.name == name)
            {
                return current;
            }
            current = current.parent;
        }
        return null;
    }

    private GameObject ResolveTalkObject()
    {
        if (dialogueRunner != null)
        {
            Transform talkFromRunner = FindAncestorByName(dialogueRunner.transform, "talk");
            if (talkFromRunner != null)
            {
                return talkFromRunner.gameObject;
            }
        }

        Transform talkFromSelf = FindAncestorByName(transform, "talk");
        if (talkFromSelf != null)
        {
            return talkFromSelf.gameObject;
        }

        return GameObject.Find("talk");
    }

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
        

        characterHighlightManager = GetComponent<CharacterHighlightManager>();

        if (skipDialogueButton != null)
        {
            skipDialogueButton.onClick.RemoveListener(HandleSkipDialogueClicked);
            skipDialogueButton.onClick.AddListener(HandleSkipDialogueClicked);
            skipDialogueButton.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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
                GameObject talkObj = ResolveTalkObject();
                if (talkObj != null && talkObj.activeInHierarchy)
                {
                    lastCheckedDay = currentDay;
                    var daySO = DayManager.Instance.daySO;
                    if (daySO != null && currentDay - 2 < daySO.dayDatas.Count)
                    {
                        string morningNode = daySO.dayDatas[currentDay - 2].dailyDialog;
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
            StartManagedCoroutine(EndDialogueRoutine());
        }
        // 状态转为运行中：YarnSpinner内部真正启动时（可能要花几帧启动），才拉起UI
        else if (!wasDialogueRunning && isDialogueRunning)
        {
            wasDialogueRunning = true;
            ShowSkipButton();
        }
    }

    #region 对话呼出与日常结算

    /// <summary>
    /// 尝试获取失败对话节点。
    /// 如果当天存在 failedDialog 且对应属性未达标准，返回 true 并输出节点名。
    /// </summary>
    private bool TryGetFailedDialogue(out string failedNode)
    {
        failedNode = null;
        if (DayManager.Instance == null || DataManager.Instance == null) return false;

        var daySO = DayManager.Instance.daySO;
        int dayIndex = DayManager.Instance.GetDayNumber();
        // dayDatas 是0-indexed，dayDatas[k].day = k+1
        // dayNumber=N 时应读取 dayDatas[N-1] 才是当天的真实数据
        int arrayIndex = dayIndex - 1;
        if (daySO == null || arrayIndex < 0 || arrayIndex >= daySO.dayDatas.Count) return false;

        DayData today = daySO.dayDatas[arrayIndex];
        if (string.IsNullOrEmpty(today.failedDialog)) return false;

        int targetType = DayManager.Instance.TargetType;
        float playerValue;
        int targetValue;

        switch (targetType)
        {
            case 1: playerValue = DataManager.Instance.nature1; targetValue = today.target1; break;
            case 2: playerValue = DataManager.Instance.nature2; targetValue = today.target2; break;
            case 3: playerValue = DataManager.Instance.nature3; targetValue = today.target3; break;
            case 4:
                playerValue = DataManager.Instance.MoneyNum;
                targetValue = today.target4;
                break;
            default:
                return false;
        }

        if (playerValue >= targetValue) return false; // 属性达标，无事发生

        // 魅力值满足 targetCharm 也视为通过检定
        if (today.targetCharm > 0 && DataManager.Instance.GetCharm() >= today.targetCharm) return false;

        failedNode = today.failedDialog;
        return true;
    }

    public void StartDialogue(string yarnScript)
    {
        // 完整回溯出到底是谁点击/触发的：
        Debug.Log($"[DialogueHandler] 系统正在请求启动节点: {yarnScript}。当前天数为 {DayManager.Instance?.GetDayNumber()}。");

        if (dialogueRunner != null)
        {
            // 如果当前有对话正在运行，或者正在处理入场/离场中，或者队列里已经有积压任务
            if (dialogueRunner.IsDialogueRunning || wasDialogueRunning || isHandlingDialogueSequence || pendingDialogues.Count > 0)
            {
                Debug.LogWarning($"[DialogueHandler] 节点 '{yarnScript}' 被加入队列！原因: IsDialogueRunning={dialogueRunner.IsDialogueRunning}, wasDialogueRunning={wasDialogueRunning}, isHandlingDialogueSequence={isHandlingDialogueSequence}, pendingCount={pendingDialogues.Count}");
                pendingDialogues.Enqueue(yarnScript);
            }
            else
            {
                isHandlingDialogueSequence = true;
                StartManagedCoroutine(StartDialogueRoutine(yarnScript));
            }
        }
    }

    /// <summary>
    /// 尝试播放 deal，无论有没有可用的 deal，随后都会无缝拉起 special 检测并进入第二天！
    /// 提供给 Inspector 里的 Button OnClick 直接调用，参数填第二天早晨的场景名(匹配 SceneType 枚举)。
    /// </summary>
    public void TriggerEndDayWithDeal(string sceneTypeName = "Talk")
    {
        Debug.Log($"[DialogueHandler] TriggerEndDayWithDeal 被调用！sceneTypeName={sceneTypeName}, dialogueRunner={(dialogueRunner != null ? "OK" : "NULL")}, currentDay={DayManager.Instance?.GetDayNumber()}");

        // 记录失败检定结果，但不立刻触发 —— 等 deal 对话播完后再接着播失败对话
        TryGetFailedDialogue(out _deferredFailureNode);

        // 先设好一定会过天以及目的场景
        SetAdvanceDayAfterDialogue(true);
        SetNextSceneType(sceneTypeName);

        if (DayManager.Instance == null || dialogueRunner == null) 
        {
            StartManagedCoroutine(EndDialogueRoutine());
            return;
        }

        int currentDay = DayManager.Instance.GetDayNumber();
        bool foundDeal = false;

        for (int i = 1; i <= currentDay; i++)
        {
            if (!_dealProgress.ContainsKey(i)) _dealProgress[i] = 1;
            int j = _dealProgress[i];
            string yarnNode = $"deal{i}_{j}";
            
            if (dialogueRunner.YarnProject != null && dialogueRunner.YarnProject.NodeNames.Contains(yarnNode))
            {
                Debug.Log($"[DialogueHandler] 打工结束拦截并启动 deal 对话: {yarnNode}");
                _dealProgress[i] = j + 1;
                
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
            StartManagedCoroutine(EndDialogueRoutine());
        }
    }

    /// <summary>
    /// 打工环节专用：按 doWork{i}_{j} 顺序调用对话。
    /// 每次调用按进入次数顺序推进：第1次=doWork1_1，第2次=doWork1_2，以此类推；
    /// 当前 i 系列的 j 耗尽后自动切换到下一个 i 系列从 j=1 开始，不回绕。
    /// </summary>
    public void TriggerDoWorkDialogue()
    {
        if (dialogueRunner == null || dialogueRunner.YarnProject == null)
        {
            Debug.LogWarning("[DialogueHandler] TriggerDoWorkDialogue: dialogueRunner 或 YarnProject 为空。");
            return;
        }

        // 记录失败检定结果，但不立刻触发 —— 等打工对话播完后再接着播失败对话
        TryGetFailedDialogue(out _deferredFailureNode);

        string yarnNode = $"doWork{_doWorkI}_{_doWorkJ}";

        if (dialogueRunner.YarnProject.NodeNames.Contains(yarnNode))
        {
            // 当前节点存在，直接播放并推进 j
            Debug.Log($"[DialogueHandler] 打工对话启动（第 {_doWorkI}_{_doWorkJ} 节）: {yarnNode}");
            _doWorkJ++;
            StartDialogue(yarnNode);
        }
        else
        {
            // 当前 i 系列已耗尽，切换到下一个 i 系列
            _doWorkI++;
            _doWorkJ = 1;
            yarnNode = $"doWork{_doWorkI}_{_doWorkJ}";

            if (dialogueRunner.YarnProject.NodeNames.Contains(yarnNode))
            {
                Debug.Log($"[DialogueHandler] 切换到新系列，打工对话启动: {yarnNode}");
                _doWorkJ++;
                StartDialogue(yarnNode);
            }
            else
            {
                // 新 i 系列不存在，回绕到 doWork1_1
                Debug.Log($"[DialogueHandler] doWork{_doWorkI}_1 不存在，回绕到 doWork1_1。");
                _doWorkI = 1;
                _doWorkJ = 2; // 已播放 doWork1_1，下次从 _2 开始
                StartDialogue("doWork1_1");
            }
        }
    }

    /// <summary>
    /// 事件对话入口：由打出事件牌触发。
    /// 先播放 <paramref name="eventNodeName"/> 对应的事件对话；
    /// 结束后依次检测当天 special 对话（有则播放），最后过天并切换场景。
    /// </summary>
    /// <param name="eventNodeName">事件对话的 Yarn 节点名（即事件牌对应的文件名）。</param>
    /// <param name="sceneTypeName">过天后要切换到的场景名，默认 "Talk"。</param>
    public void TriggerEventDialogue(string eventNodeName, string sceneTypeName = "Talk")
    {
        Debug.Log($"[DialogueHandler] TriggerEventDialogue 被调用！eventNode={eventNodeName}, sceneTypeName={sceneTypeName}");

        // 记录失败检定结果，但不立刻触发 —— 等事件对话播完后再接着播失败对话
        TryGetFailedDialogue(out _deferredFailureNode);

        SetAdvanceDayAfterDialogue(true);
        SetNextSceneType(sceneTypeName);
        StartDialogue(eventNodeName);
    }

    #endregion

    /// <summary>
    /// 内部检索函数：获取当前天数下可用的 special 对话节点名。
    /// 每段 special 对话每三天可推进一步，i更小的系列优先。
    /// </summary>
    private string GetAvailableSpecialDialogue()
    {
        Debug.Log($"[DialogueHandler] 获取可用 special 对话。当前天数为 {DayManager.Instance?.GetDayNumber()}。");

        if (DayManager.Instance == null || dialogueRunner == null) return null;
        
        int currentDay = DayManager.Instance.GetDayNumber();
        
        // 遍历 special 系列的 i。设定一个安全上限 20（或100），找不到节点就不找了
        for (int i = 1; i <= 20; i++)
        {
            if (!_specialProgress.ContainsKey(i)) _specialProgress[i] = 1;
            int j = _specialProgress[i];
            string yarnNode = $"special{i}_{j}";
            
            // 如果存在第 i 系列的第 j 段对话
            if (dialogueRunner.YarnProject != null && dialogueRunner.YarnProject.NodeNames.Contains(yarnNode))
            {
                // special序列的判断条件：每三天触发一个（进度 j 对应第 j*3 天或以后才能触发）
                if (currentDay >= j * 3)
                {
                    Debug.Log($"[DialogueHandler] 睡前优先劫持！发现满足触发条件的 special 对话: {yarnNode} (系列 {i}，进度 {j})");
                    _specialProgress[i] = j + 1;
                    return yarnNode;
                }
            }
            else if (j == 1)
            {
                break;
            }
        }
        
        return null;
    }

    private IEnumerator StartDialogueRoutine(string yarnScript)
    {
        // 提前判断是否需要切换场景——必须在转场前判断，否则黑屏后再切会被玩家看到
        GameObject talkCheck = ResolveTalkObject();
        bool needsSceneSwitch = (talkCheck == null || !talkCheck.activeInHierarchy);

        if (needsSceneSwitch)
        {
            // 只有需要切场景时才播放转场，且把 SwitchToScene 放进 midPoint 回调，保证切换动作发生在黑屏正中央
            Debug.Log($"[DialogueHandler][StartDialogueRoutine] 当前场景无 talk，播放转场并在黑屏中切换到 Talk 场景...");
            yield return TransitionManager.Instance.PlayTransition(() =>
            {
                if (UISceneManager.Instance != null)
                    UISceneManager.Instance.SwitchToScene(SceneType.Talk);
            });
        }
        else
        {
            // 已在 Talk 场景，无需入场转场，直接进入
            Debug.Log($"[DialogueHandler][StartDialogueRoutine] 已在 Talk 场景，跳过入场转场，直接启动对话: {yarnScript}");
        }

        // 【强制阻塞】：等到场景内名叫 "talk" 的物体被激活后，才允许Yarn开始执行指令和加载物体
        GameObject talkObj = null;
        int waitFrames = 0;
        while (talkObj == null || !talkObj.activeInHierarchy)
        {
            talkObj = ResolveTalkObject();
            waitFrames++;
            if (waitFrames % 60 == 0)
            {
                Debug.LogWarning($"[DialogueHandler][StartDialogueRoutine] 已等待 {waitFrames} 帧，仍未找到激活的 'talk' 物体！节点: {yarnScript}");
            }
            yield return null;
        }
        Debug.Log($"[DialogueHandler][StartDialogueRoutine] 找到 talk，准备启动节点: {yarnScript}");

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
        // 尝试更新 UI 属性条
        if (PropertiesShow.Instance != null)
        {
            PropertiesShow.Instance.UpdatePropertiesShow();
        }

        // 1. 先播放离场转场动画，并在屏幕完全黑掉的瞬间去清除立绘和背景
        yield return TransitionManager.Instance.PlayTransition(() => 
        {
            if (characterHighlightManager != null)
            {
                characterHighlightManager.ClearVisualsOnTransitionMidpoint();
            }
        });

        // 失败对话流程结束：调用全局失败处理，跳过过天逻辑
        if (_isPlayingFailedDialogue)
        {
            _isPlayingFailedDialogue = false;
            isHandlingDialogueSequence = false;
            OnGameFailed();
            yield break;
        }

        // 延迟失败检定处理：在本次前置对话（打工/商店/约会）结束后，插入失败对话
        if (!string.IsNullOrEmpty(_deferredFailureNode))
        {
            string failedNode = _deferredFailureNode;
            _deferredFailureNode = null;
            _isPlayingFailedDialogue = true;
            // 取消过天（失败结局不应该过天）
            if (characterHighlightManager != null)
                characterHighlightManager.shouldAdvanceDayAfterDialogue = false;
            willSwitchScene = false;
            pendingDialogues.Clear(); // 清空 special 等其他待播项，失败优先
            pendingDialogues.Enqueue(failedNode);
            Debug.Log($"[DialogueHandler] 前置对话结束，延迟失败检定触发，即将播放失败对话: {failedNode}");
        }

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

            // 接下来要播 special/deal，此次 willSwitchScene 的目标交由 special 结束后的下一轮 EndDialogueRoutine 来执行
            // 所以这里先清掉，防止 StartDialogueRoutine 里的"主动补切 Talk"和后续真正的场景切换冲突
            willSwitchScene = false;

            // 此时 isHandlingDialogueSequence 仍为 true，StartDialogue 会把节点再次入队而非执行，造成死锁。
            // 直接启动 StartDialogueRoutine 协程，保持锁的持有状态，无缝衔接下一段对话。
            StartManagedCoroutine(StartDialogueRoutine(nextScript));
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
    /// 全局游戏失败处理函数。
    /// 当失败对话结束后自动调用，具体功能待后实现。
    /// </summary>
    public void OnGameFailed()
    {
        Debug.Log("[DialogueHandler] OnGameFailed 被触发，执行失败结算。");

        // 数据清零
        if (DataManager.Instance != null)
        {
            DataManager.Instance.nature1 = 0;
            DataManager.Instance.nature2 = 0;
            DataManager.Instance.nature3 = 0;
            DataManager.Instance.MoneyNum = 0;
            DataManager.Instance.extraCharm = 0;
        }

        // 清空手牌堆并重置保底计数
        if (CardManager.Instance != null)
        {
            CardManager.Instance.ClearAllCards();
            CardManager.Instance.consecutiveNonGiftCount = 0;
            CardManager.Instance.consecutiveNonFuncCount  = 0;
            CardManager.Instance.consecutiveNonEventCount = 0;
        }

        // 重置天数与目标类型（DayManager 是 DontDestroyOnLoad，必须手动重置）
        if (DayManager.Instance != null)
            DayManager.Instance.ResetToStart();

        // 跳转回 Begin 场景
        if (UISceneManager.Instance != null)
            UISceneManager.Instance.SwitchToScene(SceneType.Begin);
        else
            Debug.LogError("[DialogueHandler] OnGameFailed: UISceneManager.Instance 为空，无法跳转场景。");
    }

    /// <summary>
    /// 给外部（如直接回家的UI按钮）触发“过天检测”的方法。
    /// 如果触发了 Special，则进入对话流；如果没有，它会自己拉黑屏并跨天切场景。
    /// 可以直接给 UnityEvent 传递诸如 "Talk" 或 "DayMenu" 等字符串。
    /// </summary>
    public void TriggerEndDayDirectly(string sceneTypeName = "Talk")
    {
        // 失败检定
        if (TryGetFailedDialogue(out string failedNode))
        {
            Debug.Log($"[DialogueHandler] 失败检定未通过，播放失败对话: {failedNode}");
            _isPlayingFailedDialogue = true;
            StartDialogue(failedNode);
            return;
        }

        // 勾上过天的标记并排入要切换的后置场景
        SetAdvanceDayAfterDialogue(true);
        SetNextSceneType(sceneTypeName);
        
        // 然后，人为触发一次清场与过天检测漏斗
        StartManagedCoroutine(EndDialogueRoutine());
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
        StartManagedCoroutine(EndDialogueRoutine());
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
