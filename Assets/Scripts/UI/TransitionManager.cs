using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : Singleton<TransitionManager>
{
    private CanvasGroup transitionGroup;
    private float transitionDuration = 0.5f;
    private float midWaitDuration = 0.2f;
    private bool isTransitioning = false;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        if (transitionGroup != null) return;

        // 动态创建一个覆盖全屏最顶层的Canvas和黑屏Image
        GameObject canvasGo = new GameObject("TransitionCanvas");
        DontDestroyOnLoad(canvasGo);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 保证在最上层

        GameObject imgGo = new GameObject("BlackScreen");
        imgGo.transform.SetParent(canvasGo.transform, false);
        Image img = imgGo.AddComponent<Image>();
        img.color = Color.black;
        
        RectTransform rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        transitionGroup = canvasGo.AddComponent<CanvasGroup>();
        transitionGroup.alpha = 0f;
        transitionGroup.blocksRaycasts = false;
    }

    public IEnumerator PlayTransition(Action onMidPoint = null)
    {
        if (transitionGroup == null) Initialize();

        if (isTransitioning) 
        {
            // 如果已在跑一个转场，直接执行回调，防止逻辑断卡
            onMidPoint?.Invoke();
            yield break;
        }

        isTransitioning = true;
        transitionGroup.blocksRaycasts = true; // 阻挡玩家鼠标输入

        // 1. 逐渐变黑
        float t = 0;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            transitionGroup.alpha = Mathf.Clamp01(t / transitionDuration);
            yield return null;
        }
        transitionGroup.alpha = 1f;

        // 2. 在完全转黑的正中央，执行切图或其他暗箱操作
        onMidPoint?.Invoke();

        // 保持黑屏小作停留
        yield return new WaitForSeconds(midWaitDuration);

        // 3. 逐渐恢复原明亮界面
        t = 0;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            transitionGroup.alpha = 1f - Mathf.Clamp01(t / transitionDuration);
            yield return null;
        }
        transitionGroup.alpha = 0f;
        
        transitionGroup.blocksRaycasts = false;
        isTransitioning = false;
    }
}