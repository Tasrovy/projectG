using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISceneManager : MonoBehaviour
{
    [Header("互斥场景节点 (只能同时显示一个)")]
    [SerializeField] private GameObject beginRoot;     // 初始场景
    [SerializeField] private GameObject talkRoot;      // 对话场景
    [SerializeField] private GameObject selectRoot;    // 选择场景
    [SerializeField] private GameObject shopRoot;      // 商店场景
    [SerializeField] private GameObject cardFightRoot; // 卡牌战斗场景
    [SerializeField] private GameObject endRoot;       // 结束场景

    [Header("常驻显示节点 (一直显示)")]
    [SerializeField] private GameObject[] alwaysShowRoots; // 常驻显示的GameObjects

    public SceneType testChange;

    // 单例模式，以便全局调用
    public static UISceneManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureAlwaysShowActive();       // 确保常驻节点处于激活状态
        SwitchToScene(SceneType.Begin); // 默认切换到初始场景
    }

    private void Update()
    {
#if UNITY_EDITOR
        
        if(Input.GetKeyDown(KeyCode.C))
        {
            SwitchToScene(testChange);
        }

#endif
    }

    /// <summary>
    /// 开启所有常驻显示的节点
    /// </summary>
    private void EnsureAlwaysShowActive()
    {
        if (alwaysShowRoots == null) return;
        foreach(var root in alwaysShowRoots)
        {
            if (root != null) root.SetActive(true);
        }
    }

    /// <summary>
    /// 切换到指定的场景
    /// </summary>
    public void SwitchToScene(SceneType sceneType)
    {
        // 隐藏所有的互斥场景节点
        DeactivateAllMutexRoots();

        // 开启目标场景
        switch (sceneType)
        {
            case SceneType.Begin:
                if (beginRoot != null) beginRoot.SetActive(true);
                break;
            case SceneType.Talk:
                if (talkRoot != null) talkRoot.SetActive(true);
                break;
            case SceneType.Select:
                if (selectRoot != null) selectRoot.SetActive(true);
                break;
            case SceneType.Shop:
                if (shopRoot != null) shopRoot.SetActive(true);
                break;
            case SceneType.CardFight:
                if (cardFightRoot != null) cardFightRoot.SetActive(true);
                break;
            case SceneType.End:
                if (endRoot != null) endRoot.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// 隐藏所有的互斥场景节点，常驻节点不受影响
    /// </summary>
    private void DeactivateAllMutexRoots()
    {
        if (beginRoot != null) beginRoot.SetActive(false);
        if (talkRoot != null) talkRoot.SetActive(false);
        if (selectRoot != null) selectRoot.SetActive(false);
        if (shopRoot != null) shopRoot.SetActive(false);
        if (cardFightRoot != null) cardFightRoot.SetActive(false);
        if (endRoot != null) endRoot.SetActive(false);
    }
}

/// <summary>
/// 场景类型枚举
/// </summary>
public enum SceneType
{
    Begin,
    Talk,
    Select,
    Shop,
    CardFight,
    End
}
