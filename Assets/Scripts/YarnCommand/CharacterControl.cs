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
    [Header("抖动幅度")] private float shakeMagnitude = 40.0f;

    // 辅助方法：判断名字是否为玩家
    private bool IsPlayerName(string characterName, CharacterHighlightManager manager)
    {
        if (manager == null) return false;
        
        // 允许直接用 "Player" 代指玩家
        if (string.Equals(characterName, "Player", System.StringComparison.OrdinalIgnoreCase)) 
            return true;

        // 判断是否为默认名("Odara"等)
        if (string.Equals(characterName, manager.defaultName, System.StringComparison.OrdinalIgnoreCase)) 
            return true;
        
        // 判断是否为玩家游戏内自定义名(比如 "a")
        var storage = FindAnyObjectByType<InMemoryVariableStorage>();
        if (storage != null && storage.TryGetValue(manager.playerVariableName, out string pName))
        {
            if (string.Equals(characterName, pName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }

    // 新增：保存当前物体上挂载的角色名称字典
    public Dictionary<string, string> objectToCharacterMap = new Dictionary<string, string>();

    // 辅助方法：从指定的 talk 节点下获取对应名字的子物体，而不是全局寻找
    private GameObject GetCharacterObjectUnderTalk(string objName)
    {
        GameObject talkObj = GameObject.Find("talk");
        if (talkObj != null && talkObj.activeInHierarchy)
        {
            Transform child = talkObj.transform.Find(objName);
            if (child != null)
            {
                return child.gameObject;
            }
        }
        return null; // talk不活跃或找不到时返回null
    }

    // 辅助方法：根据输入的人物名字找它被挂载在了哪一个物体上
    private SpriteRenderer GetTargetRendererByCharacterMap(string characterName)
    {
        // 遍历记录表，看看传入的故事角色名当前分配在了哪个物体（"Player" 还是 "Character"）
        foreach (var kvp in objectToCharacterMap)
        {
            if (string.Equals(kvp.Value, characterName, System.StringComparison.Ordinal))
            {
                GameObject targetObj = GetCharacterObjectUnderTalk(kvp.Key);
                if (targetObj != null)
                {
                    return targetObj.GetComponent<SpriteRenderer>();
                }
            }
        }
        return null;
    }

    #region 立绘抖动
    [YarnCommand("set_character_shake")]
    public static void SetCharacterShakeStatic(string characterName, string shakeType)
    {
        var control = Object.FindAnyObjectByType<CharacterControl>();
        if (control != null) control.SetCharacterShake(characterName, shakeType);
    }

    public void SetCharacterShake(string characterName, string shakeType)
    {
        var manager = GetComponent<CharacterHighlightManager>();
        if (manager != null)
        {
            SpriteRenderer targetRenderer = GetTargetRendererByCharacterMap(characterName);
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
    public static void SetCharacterSpriteStatic(string characterName, string emotion)
    {
        var control = Object.FindAnyObjectByType<CharacterControl>();
        if (control != null) control.SetCharacterSprite(characterName, emotion);
    }

    public void SetCharacterSprite(string characterName, string emotion)
    {
        ChangeCharacterSprite(characterName, emotion);
    }

    public void ChangeCharacterSprite(string characterName, string emotion)
    {
        var manager = GetComponentInParent<CharacterHighlightManager>();
        if (manager != null)
        {
            CharacterHighlightManager.Character ch = null;
            if (IsPlayerName(characterName, manager) && manager.characters != null && manager.characters.Count > 0)
            {
                ch = manager.characters[0];
            }
            else
            {
                ch = manager.characters.Find(c => string.Equals(c.characterName, characterName, System.StringComparison.OrdinalIgnoreCase));
            }

            if (ch != null)
            {
                SpriteRenderer targetRenderer = GetTargetRendererByCharacterMap(characterName);
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
                    Debug.LogWarning($"[CharacterControl] 角色 '{characterName}' 当前未挂载在任何立绘物体上！");
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
    public static void SetCharacterPersonStatic(string objectName, string characterName, string emotion)
    {
        var control = Object.FindAnyObjectByType<CharacterControl>();
        if (control != null) control.SetCharacterPerson(objectName, characterName, emotion);
    }

    public void SetCharacterPerson(string objectName, string characterName, string emotion)
    {
        if (objectName != "Player" && objectName != "Character")
        {
            Debug.LogWarning($"[CharacterControl] objectName 必须是 'Player' 或 'Character'，当前为: {objectName}");
            return;
        }

        var manager = GetComponent<CharacterHighlightManager>();
        if (manager != null)
        {
            CharacterHighlightManager.Character targetConfig = null;
            if (IsPlayerName(characterName, manager) && manager.characters != null && manager.characters.Count > 0)
            {
                targetConfig = manager.characters[0];
            }
            else
            {
                targetConfig = manager.characters.Find(c => string.Equals(c.characterName, characterName, System.StringComparison.OrdinalIgnoreCase));
            }

            if (targetConfig != null)
            {
                // 将该角色名映射记录到这个游戏对象上
                objectToCharacterMap[objectName] = characterName;

                GameObject targetObj = GetCharacterObjectUnderTalk(objectName);
                if (targetObj != null)
                {
                    SpriteRenderer sr = targetObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // 2. 找到对应 emotion 的差分图
                        Sprite newSprite = null;
                        if (targetConfig.emotionSprites != null)
                        {
                            var targetState = targetConfig.emotionSprites.Find(s => string.Equals(s.emotion, emotion, System.StringComparison.OrdinalIgnoreCase));
                            if (targetState != null && targetState.sprite != null)
                            {
                                newSprite = targetState.sprite;
                            }
                            else
                            {
                                Debug.LogWarning($"[CharacterControl] 未找到 '{characterName}' 的感情 '{emotion}' 的差分图！");
                                // 备用方案：如果没找到特定的emotion，尝试使用第一张图
                                if (targetConfig.emotionSprites.Count > 0)
                                {
                                    newSprite = targetConfig.emotionSprites[0].sprite;
                                }
                            }
                        }

                        // 3. 执行渐变切换
                        if (newSprite != null)
                        {
                            StartCoroutine(CrossfadeSpriteRoutine(sr, newSprite, 0.4f));
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[CharacterControl] 场景中不存在名为 '{objectName}' 的物体！");
                }
            }
            else
            {
                Debug.LogWarning($"[CharacterControl] 未在管理器中找到名为 '{characterName}' 的角色配置！");
            }
        }
    }
    #endregion

    #region 清除立绘
    [YarnCommand("clear_character_person")]
    public static void ClearCharacterPersonStatic(string objectName)
    {
        var control = Object.FindAnyObjectByType<CharacterControl>();
        if (control != null) control.ClearCharacterPerson(objectName);
    }

    public void ClearCharacterPerson(string objectName)
    {
        if (objectName != "Player" && objectName != "Character")
        {
            Debug.LogWarning($"[CharacterControl] 参数 objectName 必须是 'Player' 或 'Character'，当前为: {objectName}");
            return;
        }

        // 清除映射
        if (objectToCharacterMap.ContainsKey(objectName))
        {
            objectToCharacterMap.Remove(objectName);
        }

        GameObject targetObj = GetCharacterObjectUnderTalk(objectName);
        if (targetObj != null)
        {
            SpriteRenderer sr = targetObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = null;
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterControl] 清除立绘失败：场景中不存在名为 '{objectName}' 的物体！");
        }
    }
    #endregion

    #region 淡入淡出
    private IEnumerator CrossfadeSpriteRoutine(SpriteRenderer targetSR, Sprite newSprite, float duration)
    {
        if (targetSR == null || newSprite == null) yield break;

        // 如果要更替的图片就是现在的图片，直接跳过
        if (targetSR.sprite == newSprite) yield break;

        // 当前没有任何图片时，直接淡入即可
        if (targetSR.sprite == null)
        {
            Color baseColor = targetSR.color;
            targetSR.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            targetSR.sprite = newSprite;
            
            float elaps = 0f;
            while (elaps < duration)
            {
                elaps += Time.deltaTime;
                targetSR.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0f, baseColor.a, elaps / duration));
                yield return null;
            }
            targetSR.color = baseColor;
            yield break;
        }

        // --- 存在原图时，执行交叉淡入淡出 (Crossfade) ---
        Color originalColor = targetSR.color;
        
        // 1. 创建临时对象，承载旧图片原地淡出
        GameObject tempObj = new GameObject("TempFadeOutSprite");
        tempObj.transform.SetParent(targetSR.transform.parent);
        tempObj.transform.localPosition = targetSR.transform.localPosition;
        tempObj.transform.localScale = targetSR.transform.localScale;
        
        SpriteRenderer tempSR = tempObj.AddComponent<SpriteRenderer>();
        tempSR.sprite = targetSR.sprite;
        tempSR.color = originalColor;
        tempSR.sortingLayerID = targetSR.sortingLayerID;
        tempSR.sortingOrder = targetSR.sortingOrder - 1; // 调整一点层级避免Z-fighting闪烁

        // 2. 将目标SR换上新图片，透明度设为0准备淡入
        targetSR.sprite = newSprite;
        targetSR.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        // 3. 开始执行渐变
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            tempSR.color = new Color(tempSR.color.r, tempSR.color.g, tempSR.color.b, Mathf.Lerp(originalColor.a, 0f, t));
            targetSR.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(0f, originalColor.a, t));

            yield return null;
        }

        // 渐变结束清理
        targetSR.color = originalColor;
        Destroy(tempObj);
    }
    #endregion

    #region 场景切换
    [YarnCommand("set_background")]
    public static IEnumerator SetBackgroundStatic(string backgroundName)
    {
        var control = Object.FindAnyObjectByType<CharacterControl>();
        if (control != null) 
        {
            return control.SetBackground(backgroundName);
        }
        return DummyCoroutine();
    }

    private static IEnumerator DummyCoroutine() { yield break; }

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
                Debug.LogWarning($"[CharacterControl] Background sprite not found at Resources/Background/{backgroundName}.");
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
        float speed = 35f; // 控制抖动的平滑频率

        while (elapsed < duration)
        {
            float x = 0f;
            float y = 0f;

            // 使用 Sin 曲线代替完全随机，让抖动不那么“刺眼/激烈”
            if (shakeType == "up_down")
            {
                y = Mathf.Sin(elapsed * speed) * magnitude;
            }
            else if (shakeType == "left_right")
            {
                x = Mathf.Sin(elapsed * speed) * magnitude;
            }
            else
            {
                // 如果传入其他的，默认全方向抖动
                x = Mathf.Sin(elapsed * speed) * magnitude;
                y = Mathf.Cos(elapsed * speed * 1.2f) * magnitude;
            }

            targetTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetTransform.localPosition = originalPos;
    }

    #region 音频功能
    /// <summary>
    /// 独立封装的路径处理功能：将下划线风格的文件参数变更为资源文件夹路径
    /// 格式示例：folderA_folderB_filename -> folderA/folderB/filename
    /// </summary>
    private static string FormatAudioPath(string rawParam)
    {
        if (string.IsNullOrWhiteSpace(rawParam)) return string.Empty;
        return rawParam.Replace("_", "/");
    }

    [YarnCommand("play_bgm")]
    public static void PlayBGMCommand(string audioParam)
    {
        if (AudioManager.Instance != null)
        {
            if (string.IsNullOrEmpty(audioParam))
            {
                AudioManager.Instance.StopBGM();
            }
            else
            {
                // AudioManager内部已经配了前缀 "Sound/bgm/"
                // FormatAudioPath 把 level1_theme 转换成 level1/theme
                AudioManager.Instance.PlayBGM(FormatAudioPath(audioParam));
            }
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 找不到 AudioManager 实例！");
        }
    }

    [YarnCommand("stop_bgm")]
    public static void StopBGMCommand()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 找不到 AudioManager 实例！");
        }
    }

    [YarnCommand("play_whitenoise")]
    public static void PlayWhiteNoiseCommand(string audioParam)
    {
        if (AudioManager.Instance != null)
        {
            if (string.IsNullOrEmpty(audioParam))
            {
                AudioManager.Instance.StopWhiteNoise();
            }
            else
            {
                // AudioManager内部已经配了前缀 "Sound/Whitenoise/"
                AudioManager.Instance.PlayWhiteNoise(FormatAudioPath(audioParam));
            }
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 找不到 AudioManager 实例！");
        }
    }

    /// <summary>
    /// 读取标识符并播放音效
    /// 格式要求：sfx_文件夹1_文件夹2_文件名（例如 #sfx_Characters_Player_laugh）
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
            // 提取 sfx_ 之后的所有内容
            string rawParam = tag.Substring(4);
            if (!string.IsNullOrEmpty(rawParam))
            {
                if (AudioManager.Instance != null)
                {
                    // 按照封装的函数将下划线替换成路径，AudioManager层基准为 "Sound/"
                    string path = FormatAudioPath(rawParam);
                    AudioManager.Instance.PlaySound(path);
                }
                else
                {
                    Debug.LogWarning("[CharacterControl] 找不到 AudioManager 实例！");
                }
            }
        }
    }
    #endregion
}