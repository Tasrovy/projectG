using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;
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

    private InMemoryVariableStorage variableStorage;

    private bool wasStorageReady = false;

    private void Start()
    {
        variableStorage = FindAnyObjectByType<InMemoryVariableStorage>();
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

        if (speakerName == defaultName)     // 如果说话者是默认名字，尝试从变量存储中获取玩家名字
        {
            if (variableStorage == null) variableStorage = FindAnyObjectByType<InMemoryVariableStorage>();
            if (variableStorage != null && variableStorage.TryGetValue(playerVariableName, out string playerName))
            {
                speakerName = playerName;
            }
        }
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

    private void HightlightSpeaker(string speaker)
    {
        // 尝试获取特定的游戏对象
        GameObject playerObj = GameObject.Find("Player");
        GameObject characterObj = GameObject.Find("Character");

        SpriteRenderer playerSr = playerObj != null ? playerObj.GetComponent<SpriteRenderer>() : null;
        SpriteRenderer characterSr = characterObj != null ? characterObj.GetComponent<SpriteRenderer>() : null;

        bool isPlayerSpeaking = !string.IsNullOrEmpty(speaker) && IsPlayerName(speaker);
        bool isCharacterSpeaking = !string.IsNullOrEmpty(speaker) && !isPlayerSpeaking;

        // 如果没有说话人 (如旁白)，则全部变暗。否则对应的人设为白，另一个人变暗。
        if (playerSr != null)
        {
            playerSr.color = isPlayerSpeaking ? new Color(1f, 1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        if (characterSr != null)
        {
            characterSr.color = isCharacterSpeaking ? new Color(1f, 1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    public override async YarnTask OnDialogueStartedAsync()
    {
        // 对话开始时，显示背景
        if (dialogueBackground != null) dialogueBackground.SetActive(true);

        // 对话开始时，仅显示名为 "Player" 和 "Character" 的立绘物体
        GameObject playerObj = GameObject.Find("Player");
        GameObject characterObj = GameObject.Find("Character");

        if (playerObj != null) playerObj.SetActive(true);
        if (characterObj != null) characterObj.SetActive(true);

        // 关键点：每次对话开始前，先把所有人变暗。这样能够解决刚开始没进发言时两人全亮的问题
        HightlightSpeaker(""); 
        await YarnTask.CompletedTask;
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        // 结束对话时，隐藏背景
        if (dialogueBackground != null) dialogueBackground.SetActive(false);

        // 结束对话时，清空映射表
        var charControl = GetComponent<CharacterControl>();
        if (charControl != null)
        {
            charControl.objectToCharacterMap.Clear();
        }

        GameObject playerObj = GameObject.Find("Player");
        GameObject characterObj = GameObject.Find("Character");

        if (playerObj != null)
        {
            var sr = playerObj.GetComponent<SpriteRenderer>();
            if (sr != null) 
            {
                sr.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 强制灰色
                sr.sprite = null; // 清空立绘
            }
            playerObj.SetActive(false);
        }

        if (characterObj != null)
        {
            var sr = characterObj.GetComponent<SpriteRenderer>();
            if (sr != null) 
            {
                sr.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 强制灰色
                sr.sprite = null; // 清空立绘
            }
            characterObj.SetActive(false);
        }

        ApplyDialogueCompleteProperties();

        currentSpeaker = "";

        // 在转场之前，关闭对应的背景音乐和白噪音
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.StopWhiteNoise();
        }

        // 在所有内部逻辑和属性加成都处理完毕后，推入下一天
        if (DayManager.Instance != null)
        {
            DayManager.Instance.NextDay();
        }
        else
        {
            Debug.LogWarning("DayManager instance not found. NextDay() was not called.");
        }

        await YarnTask.CompletedTask;
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
            Debug.LogError("DataManager instance is null, cannot apply dialogue complete properties.");
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
