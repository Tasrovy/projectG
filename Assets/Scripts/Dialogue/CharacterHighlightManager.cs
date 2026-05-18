using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using Yarn.Unity;

// 该脚本39行的 speakerName 和脚本 PlayernameHandler 的 defaultName 变量在11行的定义对应的都是角色默认名字，记得同步修改

public class CharacterHighlightManager : DialoguePresenterBase
{
    [System.Serializable]
    public class EmotionSprite
    {
        public string emotion; // 感情，例如 "happy", "sad"
        public Sprite sprite;  // 对应的差分图
    }

    [System.Serializable]
    public class Character
    {
        public string characterName;
        public Color normalColor = new(1f, 1f, 1f, 1f);
        public Color dimColor = new(0.5f, 0.5f, 0.5f, 1f);
        public List<EmotionSprite> emotionSprites;
    }

    [SerializeField]
    public List<Character> characters;
    private string currentSpeaker;
    [SerializeField]
    [HideInInspector] public string playerVariableName = "$MY_NAME";
    private int[] dialogueCompleteProperties = new int[4];

    public string defaultName = "Odara";

    [Header("UI Background (对话时显示，结束时隐藏)")]
    public GameObject dialogueBackground;

    // 对话 UI 射线拦截控制（自动从 LinePresenter / OptionsPresenter 获取）
    private readonly List<CanvasGroup> dialogueUICanvasGroups = new();

    private InMemoryVariableStorage variableStorage;

    private bool wasStorageReady = false;

    // 新增：控制对话结束后是否进入下一天的标志
    [HideInInspector] public bool shouldAdvanceDayAfterDialogue = false;

    private static void HidePortraitImage(Image image)
    {
        if (image == null) return;
        image.sprite = null;
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, 0f);
        image.enabled = false;
    }

    private static bool EnsureVisiblePortraitImage(Image image)
    {
        if (image == null || image.sprite == null)
        {
            if (image != null)
            {
                HidePortraitImage(image);
            }
            return false;
        }

        image.enabled = true;
        var c = image.color;
        if (c.a <= 0f)
        {
            image.color = new Color(c.r, c.g, c.b, 1f);
        }
        return true;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private void SetDialogueUIRaycasts(bool blocksRaycasts)
    {
        foreach (var cg in dialogueUICanvasGroups)
        {
            if (cg != null) cg.blocksRaycasts = blocksRaycasts;
        }
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

    private static Transform FindChildRecursive(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, name);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private GameObject ResolveTalkObject()
    {
        Transform talkFromSelf = FindAncestorByName(transform, "talk");
        if (talkFromSelf != null)
        {
            return talkFromSelf.gameObject;
        }

        return GameObject.Find("talk");
    }

    private void Start()
    {
        variableStorage = FindAnyObjectByType<InMemoryVariableStorage>();

        // 自动为 LinePresenter 和 OptionsPresenter 添加 CanvasGroup，初始关闭射线拦截
        dialogueUICanvasGroups.Clear();

        var linePresenter = GetComponentInChildren<Yarn.Unity.LinePresenter>(true);
        if (linePresenter == null)
            linePresenter = FindAnyObjectByType<Yarn.Unity.LinePresenter>();
        if (linePresenter != null)
            dialogueUICanvasGroups.Add(GetOrAddCanvasGroup(linePresenter.gameObject));

        // Options Presenter 上没有 OptionsListView 组件，改用名字定位
        GameObject optionsPresenterObj = null;
        Transform localOptionsPresenter = FindChildRecursive(transform, "Options Presenter");
        if (localOptionsPresenter != null)
            optionsPresenterObj = localOptionsPresenter.gameObject;
        if (optionsPresenterObj == null)
            optionsPresenterObj = GameObject.Find("Options Presenter");
        if (optionsPresenterObj != null)
            dialogueUICanvasGroups.Add(GetOrAddCanvasGroup(optionsPresenterObj));

        SetDialogueUIRaycasts(false);
    }

    private void Update()
    {
        // 确保 YarnSpinner 准备完毕后再抓取数据，避免报 SmartVariableEvaluator 错误
        if (!wasStorageReady && variableStorage != null)
        {
            var runner = FindAnyObjectByType<DialogueRunner>();
            // 只有当存在runner并且它的VariableStorage字段被正式初始化后才开始同步
            if (runner != null && runner.VariableStorage != null)
            {
                wasStorageReady = true;
            }
        }

        if (wasStorageReady)
        {
            SyncPlayerName();
        }
    }

    // 将 Yarn 变量里的名字实时同步给 characters 列表的第0项（玩家）
    private void SyncPlayerName()
    {
        if (characters != null && characters.Count > 0)
        {
            if (variableStorage == null)
                variableStorage = FindAnyObjectByType<InMemoryVariableStorage>();

            if (variableStorage != null)
            {
                try
                {
                    if (variableStorage.TryGetValue(playerVariableName, out string pName) && !string.IsNullOrEmpty(pName))
                    {
                        characters[0].characterName = pName;
                    }
                    else
                    {
                        characters[0].characterName = defaultName;
                    }
                }
                catch (System.InvalidOperationException)
                {
                    // 捕捉抛错，如果尚未准备好就不予更改，等待下一帧重试
                    characters[0].characterName = defaultName;
                }
            }
        }
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        SyncPlayerName(); // 开始执行对话前再确保一遍名字是最新同步的

        string speakerName = line.CharacterName;

        if (variableStorage == null) variableStorage = FindAnyObjectByType<InMemoryVariableStorage>();
        if (variableStorage != null && variableStorage.TryGetValue(playerVariableName, out string playerName) && !string.IsNullOrEmpty(playerName))
        {
            // 如果在 Yarn 中出现的名字是原本预设的名字、或者由于 Yarn 的 bug 没有正确替换的占位符，强制截获它。
            if (speakerName == defaultName || speakerName == "林奈" || speakerName == "Player" || speakerName == "{$MY_NAME}")
            {
                speakerName = playerName;
            }
        }

        // --- 强制接管修正名字UI的显示！ ---
        // 防止 Yarn 原生的 LinePresenter 会呈现未替换、被重置、或者是写死的林奈默认名
        var linePresenter = GetComponentInChildren<Yarn.Unity.LinePresenter>(true);
        if (linePresenter == null)
            linePresenter = FindAnyObjectByType<Yarn.Unity.LinePresenter>();
        if (linePresenter != null)
        {
            // characterNameText 的公开字段
            if (linePresenter.characterNameText != null)
            {
                linePresenter.characterNameText.text = speakerName;
            }
        }
        // ---------------------------------

        // --- 新增：解析当前对话行的标签 (Metadata) 并播放音效 ---
        if (line.Metadata != null)
        {
            var charControl = GetComponent<CharacterControl>();
            if (charControl != null)
            {
                foreach (var tag in line.Metadata)
                {
                    charControl.PlayAudioFromTag(tag);
                }
            }
        }
        // 当说话者改变时更新高亮状态，即使说话者为空（旁白）也需要更新，让所有人都变暗
        if (speakerName != currentSpeaker)
        {
            HightlightSpeaker(speakerName);
            currentSpeaker = string.IsNullOrEmpty(speakerName) ? "" : speakerName;
        }
        await YarnTask.CompletedTask;
    }

    private bool IsPlayerName(string speaker)
    {
        if (string.Equals(speaker, defaultName, System.StringComparison.OrdinalIgnoreCase)) return true;

        if (variableStorage == null) variableStorage = FindAnyObjectByType<InMemoryVariableStorage>();
        if (variableStorage != null)
        {
            try
            {
                if (variableStorage.TryGetValue(playerVariableName, out string pName) && !string.IsNullOrEmpty(pName))
                {
                    if (string.Equals(speaker, pName, System.StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch (System.InvalidOperationException)
            {
                // 静默捕捉，Yarn未完全初始化时不打断执行
            }
        }

        return false;
    }

    // 辅助方法：保证只在名叫 "talk" 的物体下寻找 Player 和 Character
    private GameObject GetCharacterObjectUnderTalk(string objName)
    {
        GameObject talkObj = ResolveTalkObject();
        if (talkObj != null && talkObj.activeInHierarchy)
        {
            Transform child = talkObj.transform.Find(objName);
            if (child != null)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private void HightlightSpeaker(string speaker)
    {
        // 尝试获取特定的游戏对象 (限定在talk子节点下)
        GameObject playerObj = GetCharacterObjectUnderTalk("Player");
        GameObject characterObj = GetCharacterObjectUnderTalk("Character");

        Image playerImage = playerObj != null ? playerObj.GetComponent<Image>() : null;
        Image characterImage = characterObj != null ? characterObj.GetComponent<Image>() : null;

        bool playerVisible = EnsureVisiblePortraitImage(playerImage);
        bool characterVisible = EnsureVisiblePortraitImage(characterImage);

        bool isPlayerSpeaking = !string.IsNullOrEmpty(speaker) && IsPlayerName(speaker);
        bool isCharacterSpeaking = !string.IsNullOrEmpty(speaker) && !isPlayerSpeaking;

        // 如果没有说话人 (如旁白)，则全部变暗。否则对应的人设为白，另一个人变暗。
        if (playerVisible)
        {
            playerImage.color = isPlayerSpeaking ? new Color(1f, 1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        if (characterVisible)
        {
            characterImage.color = isCharacterSpeaking ? new Color(1f, 1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    public override async YarnTask OnDialogueStartedAsync()
    {
        // 对话开始时，显示背景，并重新开启射线拦截
        if (dialogueBackground != null) dialogueBackground.SetActive(true);
        SetDialogueUIRaycasts(true);

        // 对话开始时，仅显示名为 "Player" 和 "Character" 的立绘物体 (限定在talk下)
        GameObject playerObj = GetCharacterObjectUnderTalk("Player");
        GameObject characterObj = GetCharacterObjectUnderTalk("Character");

        if (playerObj != null) playerObj.SetActive(true);
        if (characterObj != null) characterObj.SetActive(true);

        // 关键点：每次对话开始前，先把所有人变暗。这样能够解决刚开始没进发言时两人全亮的问题
        HightlightSpeaker("");
        await YarnTask.CompletedTask;
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        // 核心属性结算和环境清理保持，视觉清理移交到转场黑屏进行
        ApplyDialogueCompleteProperties();

        var charControl = GetComponent<CharacterControl>();
        if (charControl != null)
        {
            charControl.ResetPortraitPositionsAfterDialogue();
        }

        currentSpeaker = "";

        // 在转场之前，关闭对应的背景音乐和白噪音
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.StopWhiteNoise();
        }

        // 注意：已经将过天的逻辑（shouldAdvanceDayAfterDialogue判定）移交到了 DialogueHandler 内统一拦截！

        await YarnTask.CompletedTask;
    }

    /// <summary>
    /// 当离开对话的转场动画到达最黑的中间点时，由 DialogueHandler 呼叫。用来神不知鬼不觉地清理画面
    /// </summary>
    public void ClearVisualsOnTransitionMidpoint()
    {
        // 隐藏背景，并关闭射线拦截，避免对话 UI 阻挡其他界面点击
        if (dialogueBackground != null) dialogueBackground.SetActive(false);
        SetDialogueUIRaycasts(false);

        // 清空映射表
        var charControl = GetComponent<CharacterControl>();
        if (charControl != null)
        {
            charControl.objectToCharacterMap.Clear();
        }

        // 隐藏立绘并重置颜色
        GameObject playerObj = GetCharacterObjectUnderTalk("Player");
        GameObject characterObj = GetCharacterObjectUnderTalk("Character");

        if (playerObj != null)
        {
            var img = playerObj.GetComponent<Image>();
            HidePortraitImage(img);
            playerObj.SetActive(false);
        }

        if (characterObj != null)
        {
            var img = characterObj.GetComponent<Image>();
            HidePortraitImage(img);
            characterObj.SetActive(false);
        }
    }

    /// <summary>
    /// 提供给外部（如 UnityEvent 或其他脚本）调用，用于控制本次对话结束后是否自动进入下一天
    /// </summary>
    public void SetAdvanceDayAfterDialogue(bool advance)
    {
        shouldAdvanceDayAfterDialogue = advance;
    }

    public void SetDialogueCompleteProperties(int p1, int p2, int p3)
    {
        dialogueCompleteProperties[0] = p1;
        dialogueCompleteProperties[1] = p2;
        dialogueCompleteProperties[2] = p3;
    }

    public void SetDialogueCompleteMoney(int money)
    {
        dialogueCompleteProperties[3] = money;
    }

    private void ApplyDialogueCompleteProperties()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("DataManager instance is null, cannot apply dialogue complete properties.");
            return;
        }

        DataManager.Instance.Add(1, dialogueCompleteProperties[0]);
        DataManager.Instance.Add(2, dialogueCompleteProperties[1]);
        DataManager.Instance.Add(3, dialogueCompleteProperties[2]);
        DataManager.Instance.Add(4, dialogueCompleteProperties[3]);

        for (int i = 0; i < 4; i++)
        {
            dialogueCompleteProperties[i] = 0;
        }
    }
}
