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
    [HideInInspector] public string playerVariableName = "$MY_NAME";

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

        var storage = FindAnyObjectByType<InMemoryVariableStorage>();
        if (storage != null && storage.TryGetValue(playerVariableName, out string pName) && !string.IsNullOrEmpty(pName))
        {
            if (string.Equals(speaker, pName, System.StringComparison.OrdinalIgnoreCase)) return true;
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
        GameObject playerObj = GameObject.Find("Player");
        GameObject characterObj = GameObject.Find("Character");

        if (playerObj != null)
        {
            var sr = playerObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 强制灰色
            playerObj.SetActive(false);
        }

        if (characterObj != null)
        {
            var sr = characterObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 强制灰色
            characterObj.SetActive(false);
        }

        ApplyDialogueCompleteProperties();

        currentSpeaker = "";
        await YarnTask.CompletedTask;
    }

    private void ApplyDialogueCompleteProperties()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager instance is null, cannot apply dialogue complete properties.");
            return;
        }

        if (Properties.Instance != null)
        {
            DataManager.Instance.Add(1, Properties.Instance.property1);
            DataManager.Instance.Add(2, Properties.Instance.property2);
            DataManager.Instance.Add(3, Properties.Instance.property3);
            DataManager.Instance.Add(4, Properties.Instance.money);

            // 清空，防止下次不带属性的对话误加
            Properties.Instance.ClearProperties();
        }
    }
}
