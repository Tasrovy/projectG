using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISceneManager : MonoBehaviour
{
    [Header("场景根节点")]
    [SerializeField] private GameObject beginRoot;   // 开始场景
    [SerializeField] private GameObject talkRoot;    // 对话场景
    [SerializeField] private GameObject selectRoot;  // 选择场景
    [SerializeField] private GameObject shopRoot;    // 商店场景
    [SerializeField] private GameObject cardFightRoot; // 卡牌战斗场景
    [SerializeField] private GameObject endRoot;     // 结束场景

    public SceneType testChange;

    // 单例模式，方便全局调用
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
        SwitchToScene(SceneType.Begin); // 默认切换到开始场景
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
    /// 切换到指定场景
    /// </summary>
    public void SwitchToScene(SceneType sceneType)
    {
        // 先失活所有场景根节点
        DeactivateAllRoots();

        // 激活目标场景
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
    /// 失活所有场景根节点
    /// </summary>
    private void DeactivateAllRoots()
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
