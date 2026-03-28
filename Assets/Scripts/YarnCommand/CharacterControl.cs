using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class CharacterControl : MonoBehaviour
{
    [Header("抖动参数")]
    [SerializeField]
    [Header("抖动时长")] private float shakeDuration = 0.5f;
    [SerializeField] 
    [Header("抖动幅度")] private float shakeMagnitude = 0.1f;

    // 辅助方法：判断名字是否为玩家
    private bool IsPlayerName(string characterName, CharacterHighlightManager manager)
    {
        if (manager == null) return false;
        
        // 判断是否为默认名("Player")
        if (string.Equals(characterName, manager.defaultName, System.StringComparison.OrdinalIgnoreCase)) 
            return true;
        
        // 判断是否为玩家游戏内自定义名
        var storage = FindAnyObjectByType<InMemoryVariableStorage>();
        if (storage != null && storage.TryGetValue(manager.playerVariableName, out string pName))
        {
            if (string.Equals(characterName, pName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }

    // 辅助方法：统一获取固定名称的 GameObject 上的 SpriteRenderer
    private SpriteRenderer GetTargetRenderer(string characterName, CharacterHighlightManager manager)
    {
        string targetObjectName = IsPlayerName(characterName, manager) ? "Player" : "Character";
        GameObject targetObj = GameObject.Find(targetObjectName);
        if (targetObj != null)
        {
            return targetObj.GetComponent<SpriteRenderer>();
        }
        return null;
    }

    #region 立绘抖动
    [YarnCommand("set_character_shake")]
    public void SetCharacterShake(string characterName, string shakeType)
    {
        var manager = GetComponent<CharacterHighlightManager>();
        if (manager != null)
        {
            SpriteRenderer targetRenderer = GetTargetRenderer(characterName, manager);
            if (targetRenderer != null)
            {
                StartCoroutine(ShakeRoutine(targetRenderer.transform, shakeDuration, shakeMagnitude, shakeType));
            }
            else
            {
                Debug.LogWarning($"[CharacterControl] 找不到名为'{ (IsPlayerName(characterName, manager) ? "Player" : "Character") }'的对象，或未赋予SpriteRenderer！");
            }
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 未在父物体找到 CharacterHighlightManager 组件！");
        }
    }
    #endregion

    #region 差分切换
    [YarnCommand("set_character_sprite")]
    public void SetCharacterSprite(string characterName, string emotion)
    {
        ChangeCharacterSprite(characterName, emotion);
    }

    public void ChangeCharacterSprite(string characterName, string emotion)
    {
        var manager = GetComponentInParent<CharacterHighlightManager>();
        if (manager != null)
        {
            var ch = manager.characters.Find(c => string.Equals(c.characterName, characterName, System.StringComparison.Ordinal));
            if (ch != null)
            {
                SpriteRenderer targetRenderer = GetTargetRenderer(characterName, manager);
                if (targetRenderer != null)
                {
                    if (ch.emotionSprites != null)
                    {
                        var targetState = ch.emotionSprites.Find(s => string.Equals(s.emotion, emotion, System.StringComparison.OrdinalIgnoreCase));
                        if (targetState != null && targetState.sprite != null)
                        {
                            targetRenderer.sprite = targetState.sprite;
                        }
                        else
                        {
                            Debug.LogWarning($"[CharacterControl] 未找到感情 '{emotion}' 的差分精灵图，或图片未赋值！");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[CharacterControl] 找不到名为'{ (IsPlayerName(characterName, manager) ? "Player" : "Character") }'的对象，或未设SpriteRenderer！");
                }
            }
            else
            {
                Debug.LogWarning($"[CharacterControl] 未在管理器中找到名为 '{characterName}' 的角色配置！");
            }
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 未在父物体找到 CharacterHighlightManager 组件！");
        }
    }
    #endregion

    #region 立绘切换
    [YarnCommand("set_character_person")]
    public void SetCharacterPerson(string characterName)
    {
        var manager = GetComponent<CharacterHighlightManager>();
        if (manager != null)
        {
            var targetConfig = manager.characters.Find(c => string.Equals(c.characterName, characterName, System.StringComparison.Ordinal));
            if (targetConfig != null)
            {
                GameObject characterObj = GameObject.Find("Character");
                if (characterObj != null)
                {
                    SpriteRenderer sr = characterObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // 1. 将该配置绑定到"Character"的SpriteRenderer
                        targetConfig.spriteRenderer = sr;
                        
                        // 2. 将表情默认修换为列表第一项
                        if (targetConfig.emotionSprites != null && targetConfig.emotionSprites.Count > 0)
                        {
                            sr.sprite = targetConfig.emotionSprites[0].sprite;
                        }

                        // 为了能正确控制亮暗，可以在此将原先占用"Character"的其它实例的spriteRenderer置空
                        foreach (var otherConfig in manager.characters)
                        {
                            if (otherConfig != targetConfig && !IsPlayerName(otherConfig.characterName, manager))
                            {
                                otherConfig.spriteRenderer = null;
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[CharacterControl] 场景中不存在名为 'Character' 的物体！");
                }
            }
            else
            {
                Debug.LogWarning($"[CharacterControl] 未在管理器中找到名为 '{characterName}' 的角色配置！");
            }
        }
    }
    #endregion

    #region 场景切换
    [YarnCommand("set_background")]
    public IEnumerator SetBackground(string backgroundName)
    {
        if (string.IsNullOrWhiteSpace(backgroundName))
        {
            Debug.LogError("[CharacterControl] Background name is null or empty.");
            yield break;
        }

        // YarnSpinner 会自动等待这个携程(IEnumerator)全屏完全淡入淡出结束才会走下一句话 
        yield return TransitionManager.Instance.PlayTransition(() => 
        {
            // 在屏幕完全黑掉的回调瞬间，进行切图操作：
            var backgroundObject = GameObject.Find("Background");
            if (backgroundObject == null)
            {
                Debug.LogError("[CharacterControl] UI Image named 'Background' was not found in scene.");
                return;
            }

            Image backgroundImage = backgroundObject.GetComponent<Image>();
            if (backgroundImage == null)
            {
                Debug.LogError("[CharacterControl] 'Background' object does not have an Image component.");
                return;
            }

            Sprite newBackground = Resources.Load<Sprite>($"Background/art/{backgroundName}");
            if (newBackground == null)
            {
                Debug.LogError($"[CharacterControl] Background sprite not found at Resources/Background/{backgroundName}.");
                return;
            }

            backgroundImage.sprite = newBackground;
        });
    }
    #endregion

    private IEnumerator ShakeRoutine(Transform targetTransform, float duration, float magnitude, string shakeType)
    {
        Vector3 originalPos = targetTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = 0f;
            float y = 0f;

            if (shakeType == "up_down")
            {
                y = Random.Range(-1f, 1f) * magnitude;
            }
            else if (shakeType == "left_right")
            {
                x = Random.Range(-1f, 1f) * magnitude;
            }
            else
            {
                // 如果传入其他的，默认全方向抖动
                x = Random.Range(-1f, 1f) * magnitude;
                y = Random.Range(-1f, 1f) * magnitude;
            }

            targetTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetTransform.localPosition = originalPos;
    }

    #region 音频功能
    [YarnCommand("play_bgm")]
    public void PlayBGMCommand(string audioPath)
    {
        if (AudioManager.Instance != null)
        {
            if (string.IsNullOrEmpty(audioPath))
            {
                AudioManager.Instance.StopBGM();
            }
            else
            {
                // 内部方法已含有直接切换（自动Stop上一个）的效果，并且会自动加上 "bgm/" 前缀
                AudioManager.Instance.PlayBGM(audioPath);
            }
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 找不到 AudioManager 实例！");
        }
    }

    [YarnCommand("play_whitenoise")]
    public void PlayWhiteNoiseCommand(string audioPath)
    {
        if (AudioManager.Instance != null)
        {
            if (string.IsNullOrEmpty(audioPath))
            {
                AudioManager.Instance.StopWhiteNoise();
            }
            else
            {
                // 内部方法会自动加上 "Whitenoise/" 前缀
                AudioManager.Instance.PlayWhiteNoise(audioPath);
            }
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 找不到 AudioManager 实例！");
        }
    }

    /// <summary>
    /// 读取标识符并播放音效
    /// 格式要求：sfx_charactername_soundtype
    /// </summary>
    public void PlayAudioFromTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        // 去除可能的 '#' 符号
        if (tag.StartsWith("#"))
        {
            tag = tag.Substring(1);
        }

        // 以 sfx_ 开头则认为是音效标签
        if (tag.StartsWith("sfx_"))
        {
            string[] parts = tag.Split('_');
            if (parts.Length >= 3)
            {
                // parts[0] 是 "sfx"
                string characterName = parts[1];
                // 提取具体的音效名字 (考虑到音效名字本身可能带下划线，将其余部分还原)
                string soundType = tag.Substring(4 + characterName.Length + 1);
                
                if (AudioManager.Instance != null)
                {
                    // 拼接路径： Characters/角色名/音效名
                    string path = $"Characters/{characterName}/{soundType}";
                    AudioManager.Instance.PlaySound(path);
                }
                else
                {
                    Debug.LogWarning("[CharacterControl] 找不到 AudioManager 实例！");
                }
            }
            else
            {
                Debug.LogWarning($"[CharacterControl] 音效标签格式错误，应为 sfx_人物_音效，当前为: {tag}");
            }
        }
    }
    #endregion
}
