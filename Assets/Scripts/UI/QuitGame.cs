using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitGame : MonoBehaviour
{
    // 可在 Inspector 中订阅的退出事件（可选）
    public UnityEvent OnQuit;

    // 将此方法绑定到 Button 的 OnClick 即可退出游戏
    public void Quit()
    {
        // 先触发订阅事件
        OnQuit?.Invoke();

        Application.Quit();
    }
}
