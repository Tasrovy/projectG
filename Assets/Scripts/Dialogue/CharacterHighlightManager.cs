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
        public Color dimColor = new(0.6f, 0.6f, 0.6f, 1f);
        public List<EmotionSprite> emotionSprites;
    }

    [SerializeField]
    public List<Character> characters;
    private string currentSpeaker;
    [SerializeField]
    private string playerVariableName = "$my_name";

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
        if (!string.IsNullOrEmpty(speakerName) && speakerName != currentSpeaker)
        {
            HightlightSpeaker(speakerName);
            currentSpeaker = speakerName;
        }
        await YarnTask.CompletedTask;
    }

    private void HightlightSpeaker(string speaker)
    {
        foreach (var ch in characters)
        {
            if (ch.spriteRenderer == null) continue;
            if (string.Equals(ch.characterName, speaker, System.StringComparison.Ordinal))
            {
                ch.spriteRenderer.color = ch.normalColor;
            }
            else
            {
                ch.spriteRenderer.color = ch.dimColor;
            }
        }
    }

    public override async YarnTask OnDialogueStartedAsync()
    {
        await YarnTask.CompletedTask;
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        foreach (var ch in characters)
        {
            if (ch.spriteRenderer != null)
                ch.spriteRenderer.color = ch.dimColor;
        }
        currentSpeaker = "";
        await YarnTask.CompletedTask;
    }
}
