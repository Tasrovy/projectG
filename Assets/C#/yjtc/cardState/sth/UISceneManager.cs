using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
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
    [SerializeField] private GameObject afterclassRoot;// 放学三选一场景    
    [SerializeField] private GameObject workRoot;      // 打工场景
    [SerializeField] private GameObject namingRoot;    // 命名场景

    [Header("常驻显示节点 (一直显示)")]
    [SerializeField] private GameObject[] alwaysShowRoots; // 常驻显示的GameObjects

    [Header("BGM 设置")]
    [Tooltip("配置每个场景对应的BGM文件夹（资源需放在 Resources/Sound/bgm/ 下）")]
    public List<SceneFolderConfig> sceneBGMConfigs = new List<SceneFolderConfig>();

    private Dictionary<SceneType, string> bgmFolderDictionary = new Dictionary<SceneType, string>();
    private Coroutine currentBgmCoroutine;

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

        // 初始化字典
        foreach (var config in sceneBGMConfigs)
        {
            if (!bgmFolderDictionary.ContainsKey(config.sceneType))
            {
                bgmFolderDictionary.Add(config.sceneType, config.folderName);
            }
        }
    }

    private void Start()
    {
        SwitchToScene(SceneType.Begin); // 默认切换到初始场景，内部会自动处理常驻节点的显隐
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
    /// 专门为了给 UnityEvent（面板上）使用而增加的 string 类型重载！
    /// 因为 UnityEvent 不支持直接选 Enum 类型。可以在面板里填入 "Talk" 或者 "Shop"
    /// </summary>
    public void SwitchToScene(string sceneTypeName)
    {
        if (System.Enum.TryParse(sceneTypeName, true, out SceneType parsedScene))
        {
            SwitchToScene(parsedScene);
        }
        else
        {
            Debug.LogWarning($"[UISceneManager] 面板输入的场景名 {sceneTypeName} 解析失败！请检查拼写。");
        }
    }

    /// <summary>
    /// 切换到指定的场景 (原本的 Enum 版本，供纯代码调用)
    /// </summary>
    public void SwitchToScene(SceneType sceneType)
    {
        // 隐藏所有的互斥场景节点
        DeactivateAllMutexRoots();

        bool shouldShowAlwaysRoots = (sceneType != SceneType.Begin && sceneType != SceneType.Naming && sceneType != SceneType.Talk);
        if (alwaysShowRoots != null)
        {
            foreach(var root in alwaysShowRoots)
            {
                if (root != null) root.SetActive(shouldShowAlwaysRoots);
            }
        }

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
            case SceneType.AfterClass:
                if (afterclassRoot != null) afterclassRoot.SetActive(true);
                break;
            case SceneType.Work:
                if (workRoot != null) workRoot.SetActive(true);
                break;
            case SceneType.Naming:
                if (namingRoot != null) namingRoot.SetActive(true);
                break;
        }

        // 处理场景切换后的 BGM 逻辑
        PlayBGMForScene(sceneType);
    }

    /// <summary>
    /// 播放该场景对应的 BGM 列表
    /// </summary>
    private void PlayBGMForScene(SceneType sceneType)
    {
        if (currentBgmCoroutine != null)
        {
            StopCoroutine(currentBgmCoroutine);
            currentBgmCoroutine = null;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        if (bgmFolderDictionary.TryGetValue(sceneType, out string folderName) && !string.IsNullOrEmpty(folderName))
        {
            currentBgmCoroutine = StartCoroutine(BgmRoutine(folderName));
        }
    }

    private IEnumerator BgmRoutine(string folderName)
    {
        // 动态加载该文件夹下所有的音频（注意：需要将音频放在 Resources/Sound/bgm/ 对应文件夹下）
        AudioClip[] clips = Resources.LoadAll<AudioClip>($"Sound/bgm/{folderName}");

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[UISceneManager] 未在该文件夹找到音频文件: Sound/bgm/{folderName}");
            yield break;
        }

        while (true)
        {
            // 随机挑一首歌
            int randomIndex = Random.Range(0, clips.Length);
            AudioClip clip = clips[randomIndex];

            if (clip != null && AudioManager.Instance != null)
            {
                // 使用 AudioManager 播放这首 BGM（相对路径：文件夹名/音频名）
                AudioManager.Instance.PlayBGM($"{folderName}/{clip.name}");
                
                // 等待这首 BGM 播放完
                // 为了防止AudioManager的loop导致末尾重复播放，可稍微提前0.05秒切歌
                yield return new WaitForSeconds(clip.length - 0.05f);
            }
            else
            {
                yield return null;
            }
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
        if (afterclassRoot != null) afterclassRoot.SetActive(false);
        if (workRoot != null) workRoot.SetActive(false);
        if (namingRoot != null) namingRoot.SetActive(false);
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
    AfterClass,
    Work,
    Naming,
    End
}

[System.Serializable]
public struct SceneFolderConfig
{
    public SceneType sceneType;
    public string folderName; // Unity 面板填入对应的文件夹名称
}
