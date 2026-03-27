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
        public SpriteRenderer spriteRenderer;
        public Color normalColor = new(1f, 1f, 1f, 1f);
        public Color dimColor = new(0.5f, 0.5f, 0.5f, 1f);
        public List<EmotionSprite> emotionSprites;
    }

    [SerializeField]
    public List<Character> characters;
    private string currentSpeaker;
    [SerializeField]
    private string playerVariableName = "$MY_NAME";
    private int[] dialogueCompleteProperties = new int[4];

    public string defaultName = "Player";

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speakerName = line.CharacterName;

        if (speakerName == defaultName)     // 如果说话者是默认名字，尝试从变量存储中获取玩家名字
        {
            var storage = FindAnyObjectByType<InMemoryVariableStorage>();
            if (storage != null && storage.TryGetValue(playerVariableName, out string playerName))
            {
                speakerName = playerName;
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

    private void HightlightSpeaker(string speaker)
    {
        if (characters == null) return;

        // 尝试获取玩家在游戏内的自定义名
        string customPlayerName = defaultName;
        var storage = FindAnyObjectByType<InMemoryVariableStorage>();
        if (storage != null && storage.TryGetValue(playerVariableName, out string pName) && !string.IsNullOrEmpty(pName))
        {
            customPlayerName = pName;
        }

        foreach (var ch in characters)
        {
            if (ch == null || ch.spriteRenderer == null) continue;

            // 1. 常规匹配：直接字符名称对比
            bool isSpeaking = string.Equals(ch.characterName, speaker, System.StringComparison.Ordinal);
            
            // 2. 特殊匹配：如果列表里的角色名是"Player"（即立绘物体的恒定名），并且当前说话的是玩家的自定义名，则同样视为该角色在说话
            if (!isSpeaking && ch.characterName == defaultName && speaker == customPlayerName)
            {
                isSpeaking = true;
            }

            if (isSpeaking)
            {
                // 强制写死：说话时设为纯白（全亮）
                ch.spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                // 强制写死：未说话时设为灰色（变暗），不使用面板上的 dimColor，避免配置错误导致不现形或全亮
                ch.spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
    }

    public override async YarnTask OnDialogueStartedAsync()
    {
        // 对话开始时，将所有配置的立绘物体显示出来
        if (characters != null)
        {
            foreach (var ch in characters)
            {
                if (ch != null && ch.spriteRenderer != null)
                {
                    ch.spriteRenderer.gameObject.SetActive(true);
                }
            }
        }

        // 关键点：每次对话开始前，先把所有人变暗。这样能够解决刚开始没进发言时两人全亮的问题
        HightlightSpeaker(""); 
        await YarnTask.CompletedTask;
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        if (characters != null)
        {
            foreach (var ch in characters)
            {
                if (ch != null && ch.spriteRenderer != null)
                {
                    ch.spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 强制灰色
                    // 对话结束时隐藏立绘
                    ch.spriteRenderer.gameObject.SetActive(false);
                }
            }
        }

        ApplyDialogueCompleteProperties();

        currentSpeaker = "";
        await YarnTask.CompletedTask;
    }

    public void SetDialogueCompleteProperties(int[] values)
    {
        if (values == null || values.Length != 4)
        {
            Debug.LogError("SetDialogueCompleteProperties requires an int array with exactly 4 values.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            dialogueCompleteProperties[i] = values[i];
        }
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
