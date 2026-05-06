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

    [Header("立绘大小参数")]
    [SerializeField, Min(1f)] private float portraitSizeType2Scale = 1.15f;
    [SerializeField, Min(1f)] private float portraitSizeType3Scale = 1.3f;

    private class PortraitSizeState
    {
        public RectTransform Rect;
        public Vector2 BaseAnchoredPos;
        public Vector3 BaseScale;
    }

    private readonly Dictionary<string, PortraitSizeState> portraitSizeStates = new Dictionary<string, PortraitSizeState>();
    private readonly Dictionary<string, Coroutine> activePortraitSizeCoroutines = new Dictionary<string, Coroutine>();

    private static void HidePortraitImage(Image image)
    {
        if (image == null) return;
        image.sprite = null;
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, 0f);
        image.enabled = false;
    }

    private static void ShowPortraitImage(Image image, Sprite sprite)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.enabled = true;
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, 1f);
    }

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
        Transform talkFromSelf = FindAncestorByName(transform, "talk");
        if (talkFromSelf != null)
        {
            return talkFromSelf.gameObject;
        }

        return GameObject.Find("talk");
    }

    // 辅助方法：从指定的 talk 节点下获取对应名字的子物体，而不是全局寻找
    private GameObject GetCharacterObjectUnderTalk(string objName)
    {
        GameObject talkObj = ResolveTalkObject();
        Debug.Log(talkObj != null ? $"[CharacterControl] 成功找到 'talk' 对象，准备在其下寻找 '{objName}'。" : "[CharacterControl] 未找到 'talk' 对象，无法在其下寻找角色物体！");
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
    private Image GetTargetImageByCharacterMap(string characterName)
    {
        // 遍历记录表，看看传入的故事角色名当前分配在了哪个物体（"Player" 还是 "Character"）
        foreach (var kvp in objectToCharacterMap)
        {
            if (string.Equals(kvp.Value, characterName, System.StringComparison.Ordinal))
            {
                GameObject targetObj = GetCharacterObjectUnderTalk(kvp.Key);
                if (targetObj != null)
                {
                    return targetObj.GetComponent<Image>();
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
            Image targetImage = GetTargetImageByCharacterMap(characterName);
            if (targetImage != null)
            {
                StartCoroutine(ShakeRoutine(targetImage.rectTransform, shakeDuration, shakeMagnitude, shakeType));
            }
            else
            {
                Debug.LogWarning($"[CharacterControl] 找不到名为'{ (IsPlayerName(characterName, manager) ? "Player" : "Character") }'的对象，或未赋予Image组件！");
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
                Image targetImage = GetTargetImageByCharacterMap(characterName);
                if (targetImage != null)
                {
                    if (ch.emotionSprites != null)
                    {
                        var targetState = ch.emotionSprites.Find(s => string.Equals(s.emotion, emotion, System.StringComparison.OrdinalIgnoreCase));
                        if (targetState != null && targetState.sprite != null)
                        {
                            ShowPortraitImage(targetImage, targetState.sprite);
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
                    Image img = targetObj.GetComponent<Image>();
                    if (img != null)
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
                            StartCoroutine(CrossfadeSpriteRoutine(img, newSprite, 0.4f));
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
            Image img = targetObj.GetComponent<Image>();
            if (img != null)
            {
                HidePortraitImage(img);
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterControl] 清除立绘失败：场景中不存在名为 '{objectName}' 的物体！");
        }
    }
    #endregion

    #region 立绘大小切换
    [YarnCommand("set_character_size")]
    public static void SetCharacterSizeStatic(string objectName, int sizeType, float yPoint, float duration = 0f)
    {
        var control = Object.FindAnyObjectByType<CharacterControl>();
        if (control != null)
        {
            control.SetCharacterSize(objectName, sizeType, yPoint, duration);
        }
    }

    public void SetCharacterSize(string objectName, int sizeType, float yPoint, float duration = 0f)
    {
        string normalizedObjectName = NormalizePortraitObjectName(objectName);
        if (string.IsNullOrEmpty(normalizedObjectName))
        {
            Debug.LogWarning($"[CharacterControl] 立绘大小切换失败：objectName 必须是 'Player' 或 'Character'，当前为: {objectName}");
            return;
        }

        GameObject targetObj = GetCharacterObjectUnderTalk(normalizedObjectName);
        if (targetObj == null)
        {
            Debug.LogWarning($"[CharacterControl] 立绘大小切换失败：未找到对象 {normalizedObjectName}");
            return;
        }

        RectTransform targetRect = targetObj.GetComponent<RectTransform>();
        if (targetRect == null)
        {
            Debug.LogWarning($"[CharacterControl] 立绘大小切换失败：对象 {normalizedObjectName} 缺少 RectTransform");
            return;
        }

        EnsurePortraitSizeState(normalizedObjectName, targetRect);
        PortraitSizeState state = portraitSizeStates[normalizedObjectName];

        float scaleFactor = GetScaleFactorByType(sizeType);
        if (scaleFactor < 0f)
        {
            Debug.LogWarning($"[CharacterControl] 立绘大小切换失败：sizeType 仅支持 1/2/3，当前为: {sizeType}");
            return;
        }

        Vector3 targetScale = new Vector3(
            state.BaseScale.x * scaleFactor,
            state.BaseScale.y * scaleFactor,
            state.BaseScale.z
        );

        Vector2 targetAnchoredPos = GetTargetAnchoredPosition(state, targetRect, sizeType, Mathf.Clamp01(yPoint), scaleFactor);
        float clampedDuration = Mathf.Max(0f, duration);

        if (activePortraitSizeCoroutines.TryGetValue(normalizedObjectName, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
        }

        activePortraitSizeCoroutines[normalizedObjectName] = StartCoroutine(
            AnimatePortraitSize(normalizedObjectName, targetRect, targetScale, targetAnchoredPos, clampedDuration)
        );
    }

    private static string NormalizePortraitObjectName(string objectName)
    {
        if (string.Equals(objectName, "Player", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Player";
        }
        if (string.Equals(objectName, "Character", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Character";
        }
        return string.Empty;
    }

    private void EnsurePortraitSizeState(string objectName, RectTransform rect)
    {
        if (!portraitSizeStates.TryGetValue(objectName, out PortraitSizeState state) || state.Rect != rect)
        {
            portraitSizeStates[objectName] = new PortraitSizeState
            {
                Rect = rect,
                BaseAnchoredPos = rect.anchoredPosition,
                BaseScale = rect.localScale,
            };
        }
    }

    private float GetScaleFactorByType(int sizeType)
    {
        switch (sizeType)
        {
            case 1:
                return 1f;
            case 2:
                return portraitSizeType2Scale;
            case 3:
                return portraitSizeType3Scale;
            default:
                return -1f;
        }
    }

    private static Vector2 GetTargetAnchoredPosition(PortraitSizeState state, RectTransform rect, int sizeType, float yPoint01, float scaleFactor)
    {
        if (sizeType == 1)
        {
            // 1档始终回到原始位置与原始大小
            return state.BaseAnchoredPos;
        }

        // yPoint: 上=0 下=1, 基于图片中轴线
        float localY = Mathf.Lerp(rect.rect.yMax, rect.rect.yMin, yPoint01);
        float scaledOffsetY = localY * (state.BaseScale.y * scaleFactor);

        // 让目标 y 点在放大结束后落到承载物体原始坐标点
        return state.BaseAnchoredPos - new Vector2(0f, scaledOffsetY);
    }

    private IEnumerator AnimatePortraitSize(string objectName, RectTransform rect, Vector3 targetScale, Vector2 targetAnchoredPos, float duration)
    {
        if (duration <= 0f)
        {
            rect.localScale = targetScale;
            rect.anchoredPosition = targetAnchoredPos;
            activePortraitSizeCoroutines.Remove(objectName);
            yield break;
        }

        Vector3 startScale = rect.localScale;
        Vector2 startPos = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            rect.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, t);
            yield return null;
        }

        rect.localScale = targetScale;
        rect.anchoredPosition = targetAnchoredPos;
        activePortraitSizeCoroutines.Remove(objectName);
    }
    #endregion

    #region 淡入淡出
    private IEnumerator CrossfadeSpriteRoutine(Image targetImage, Sprite newSprite, float duration)
    {
        if (targetImage == null || newSprite == null) yield break;

        // 如果要更替的图片就是现在的图片，直接跳过
        if (targetImage.sprite == newSprite && targetImage.enabled)
        {
            var currentColor = targetImage.color;
            targetImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
            yield break;
        }

        // 当前没有任何图片时，直接淡入即可
        if (targetImage.sprite == null || targetImage.enabled == false)
        {
            Color baseColor = targetImage.color;
            targetImage.enabled = true;
            targetImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            targetImage.sprite = newSprite;
            float targetAlpha = 1f;
            
            float elaps = 0f;
            while (elaps < duration)
            {
                elaps += Time.deltaTime;
                targetImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0f, targetAlpha, elaps / duration));
                yield return null;
            }
            targetImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, targetAlpha);
            yield break;
        }

        // --- 存在原图时，执行交叉淡入淡出 (Crossfade) ---
        Color originalColor = targetImage.color;
        RectTransform targetRect = targetImage.rectTransform;
        
        // 1. 创建临时对象，承载旧图片原地淡出
        GameObject tempObj = new GameObject("TempFadeOutImage", typeof(RectTransform), typeof(Image));
        RectTransform tempRect = tempObj.GetComponent<RectTransform>();
        tempRect.SetParent(targetRect.parent, false);
        tempRect.anchorMin = targetRect.anchorMin;
        tempRect.anchorMax = targetRect.anchorMax;
        tempRect.pivot = targetRect.pivot;
        tempRect.anchoredPosition = targetRect.anchoredPosition;
        tempRect.sizeDelta = targetRect.sizeDelta;
        tempRect.localScale = targetRect.localScale;
        tempRect.localRotation = targetRect.localRotation;

        int targetSiblingIndex = targetRect.GetSiblingIndex();
        tempRect.SetSiblingIndex(Mathf.Max(0, targetSiblingIndex - 1));
        
        Image tempImage = tempObj.GetComponent<Image>();
        tempImage.enabled = true;
        tempImage.sprite = targetImage.sprite;
        tempImage.color = originalColor;
        tempImage.material = targetImage.material;
        tempImage.preserveAspect = targetImage.preserveAspect;
        tempImage.raycastTarget = false;

        // 2. 将目标Image换上新图片，透明度设为0准备淡入
        targetImage.enabled = true;
        targetImage.sprite = newSprite;
        targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        float targetAlpha2 = originalColor.a > 0f ? originalColor.a : 1f;

        // 3. 开始执行渐变
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            tempImage.color = new Color(tempImage.color.r, tempImage.color.g, tempImage.color.b, Mathf.Lerp(originalColor.a, 0f, t));
            targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(0f, targetAlpha2, t));

            yield return null;
        }

        // 渐变结束清理
        targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha2);
        Destroy(tempObj);
    }
    #endregion

    #region 画面特写缩放
    private Vector2 originalTalkAnchoredPos;
    private Vector3 originalTalkScale;
    private bool isTalkZoomed = false;
    private Coroutine activeTalkZoomCoroutine;

    /// <summary>
    /// 通过仅缩放/移动背景 (talkBG) 制造「镜头推近」的错觉，立绘（Player/Character）与对话框保持原位不受影响。
    /// 
    /// Yarn 调用示例: <<camera_zoom 0.5 0.5 1.5 1.0>>
    /// 
    /// 参数释义：
    /// - anchorX / anchorY: 缩放锚点的归一化坐标 (0~1)
    ///   - anchorX: 0=左边缘, 1=右边缘, 0.5=水平居中
    ///   - anchorY: 0=上边缘, 1=下边缘, 0.5=垂直居中
    ///   - 锚点位置在缩放前后保持屏幕坐标不变（即「向该点推进」的效果）
    /// - targetScale: 放大倍数 (1.0为原始大小，1.5即为放大约1.5倍)
    /// - duration: 动画过渡时间 (秒)
    /// </summary>
    [YarnCommand("camera_zoom")]
    public static void ZoomArtStatic(float anchorX, float anchorY, float targetScale, float duration = 0f)
    {
        var control = Object.FindAnyObjectByType<CharacterControl>();
        if (control != null) control.ZoomArt(anchorX, anchorY, targetScale, duration);
    }

    public void ZoomArt(float anchorX, float anchorY, float targetScale, float duration = 0f)
    {
        // 只缩放/移动背景(talkBG)，立绘(Player/Character)和对话框完全不受影响
        GameObject bgObj = GetCharacterObjectUnderTalk("talkBG");
        if (bgObj == null)
        {
            Debug.LogWarning("[CharacterControl] 未找到 'talkBG' 对象，无法执行缩放！");
            return;
        }

        RectTransform rt = bgObj.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogWarning("[CharacterControl] 'talkBG' 对象缺少 RectTransform 组件！");
            return;
        }

        // 首次调用时记录 talkBG 的原始状态
        if (!isTalkZoomed)
        {
            originalTalkAnchoredPos = rt.anchoredPosition;
            originalTalkScale = rt.localScale;
            isTalkZoomed = true;
        }

        if (activeTalkZoomCoroutine != null) StopCoroutine(activeTalkZoomCoroutine);

        // 将归一化锚点坐标转换为本地空间坐标
        // anchorX: 0=左(xMin), 1=右(xMax)；anchorY: 0=上(yMax), 1=下(yMin)
        float localX = Mathf.Lerp(rt.rect.xMin, rt.rect.xMax, anchorX);
        float localY = Mathf.Lerp(rt.rect.yMax, rt.rect.yMin, anchorY);

        // 让锚点在缩放前后保持世界坐标不变：
        // newAnchoredPos = originalPos - localPoint * originalScale * (targetScale - 1)
        float offsetX = localX * originalTalkScale.x * (targetScale - 1f);
        float offsetY = localY * originalTalkScale.y * (targetScale - 1f);
        Vector2 targetPos = originalTalkAnchoredPos - new Vector2(offsetX, offsetY);

        Vector3 targetScaleVec = new Vector3(
            originalTalkScale.x * targetScale,
            originalTalkScale.y * targetScale,
            originalTalkScale.z
        );
        activeTalkZoomCoroutine = StartCoroutine(ZoomTalkRoutine(rt, targetPos, targetScaleVec, duration));
    }

    /// <summary>
    /// 恢复缩放/移动前状态
    /// Yarn 调用示例: <<reset_camera 1.0>>
    /// 参数 duration 为过渡时间(秒)
    /// </summary>
    [YarnCommand("reset_camera")]
    public static void ResetArtZoomStatic(float duration = 0f)
    {
        var control = Object.FindAnyObjectByType<CharacterControl>();
        if (control != null) control.ResetArtZoom(duration);
    }

    public void ResetArtZoom(float duration = 0f)
    {
        if (!isTalkZoomed) return;

        GameObject bgObj = GetCharacterObjectUnderTalk("talkBG");
        if (bgObj != null)
        {
            RectTransform rt = bgObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (activeTalkZoomCoroutine != null) StopCoroutine(activeTalkZoomCoroutine);
                activeTalkZoomCoroutine = StartCoroutine(ZoomTalkRoutine(rt, originalTalkAnchoredPos, originalTalkScale, duration));
            }
        }

        isTalkZoomed = false; // 状态恢复
    }

    private IEnumerator ZoomTalkRoutine(RectTransform rt, Vector2 targetPos, Vector3 targetScale, float duration)
    {
        Vector2 startPos = rt.anchoredPosition;
        Vector3 startScale = rt.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 缓动算法使得位移和缩放更加顺滑自然
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            rt.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        rt.anchoredPosition = targetPos;
        rt.localScale = targetScale;
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
            var backgroundObject = GetCharacterObjectUnderTalk("talkBG");
            if (backgroundObject == null)
            {
                Debug.LogError("[CharacterControl] UI Image named 'talkBG' was not found under 'Canvas/talk' object.");
                return;
            }

            Image backgroundImage = backgroundObject.GetComponent<Image>();
            if (backgroundImage == null)
            {
                Debug.LogError("[CharacterControl] 'talkBG' object does not have an Image component.");
                return;
            }

            Sprite newBackground = Resources.Load<Sprite>($"Background/{backgroundName}");
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

    [YarnCommand("stop_whitenoise")]
    public static void StopWhiteNoiseCommand()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopWhiteNoise();
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