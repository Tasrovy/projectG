using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class End : MonoBehaviour
{
    /// <summary>
    /// 退出游戏事件（可挂载给UI Button或其他UnityEvent）
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}
