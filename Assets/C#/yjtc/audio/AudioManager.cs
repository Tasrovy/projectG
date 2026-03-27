using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    #region Singleton
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new("AudioManager");
                _instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    #endregion

    #region 对象池
    [System.Serializable]
    public class AudioSourcePool
    {
        private Queue<AudioSource> availableSources = new();
        private List<AudioSource> allSources = new();
        private Transform poolParent;

        public AudioSourcePool(Transform parent, int initialSize = 5)
        {

            poolParent = parent;
            InitializePool(initialSize);

        }

        /// <summary>
        /// 初始化对象池
        /// </summary>
        private void InitializePool(int size)
        {
            for (int i = 0; i < size; i++)
            {
                CreateNewAudioSource();
            }
        }

        /// <summary>
        /// 创建新的AudioSource
        /// </summary>
        private AudioSource CreateNewAudioSource()
        {
            GameObject go = new("AudioSource_Pooled");
            go.transform.SetParent(poolParent);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D音效

            availableSources.Enqueue(source);
            allSources.Add(source);

            return source;
        }

        /// <summary>
        /// 从对象池获取AudioSource
        /// </summary>
        public AudioSource Get()
        {
            if (availableSources.Count == 0)
            {
                // 如果没有可用对象，创建新的
                CreateNewAudioSource();
                //Debug.LogWarning("AudioSource池已耗尽，创建新的AudioSource");
            }

            AudioSource source = availableSources.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }

        /// <summary>
        /// 将AudioSource返回到对象池
        /// </summary>
        public void Return(AudioSource source)
        {
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);

            if (!availableSources.Contains(source))
            {
                availableSources.Enqueue(source);
            }
        }

        /// <summary>
        /// 清理对象池
        /// </summary>
        public void Clear()
        {
            foreach (var source in allSources)
            {
                if (source != null)
                    Destroy(source.gameObject);
            }
            availableSources.Clear();
            allSources.Clear();
        }
    }
    #endregion

    [Header("音频源属性")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource singleSource;
    private AudioSourcePool soundEffectPool;

    [Header("音高配置")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;
    float lowMinPitch = 0.45f;
    float lowMaxPitch = 0.6f;

    private System.Random rand = new();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioManager();
    }

    /// <summary>
    /// 初始化音频管理器
    /// </summary>
    private void InitializeAudioManager()
    {
        // 初始化BGM和Single音频源
        if (bgmSource == null)
        {
            GameObject bgmGo = new("BGM_Source");
            bgmGo.transform.SetParent(transform);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
        }

        if (singleSource == null)
        {
            GameObject singleGo = new("Single_Source");
            singleGo.transform.SetParent(transform);
            singleSource = singleGo.AddComponent<AudioSource>();
            singleSource.loop = false;
            singleSource.spatialBlend = 0f;
        }

        // 初始化音效对象池
        soundEffectPool = new AudioSourcePool(transform, 10);
    }

    #region 播放控制
    /// <summary>
    /// 设置音高随机范围
    /// </summary>
    public void SetPitchRange(float min, float max)
    {
        minPitch = Mathf.Clamp(min, 0.5f, 2f);
        maxPitch = Mathf.Clamp(max, 0.5f, 2f);
    }
    public void setLowPitchRange(float min,float max)
    {
        lowMaxPitch= Mathf.Clamp(min, 0.2f, 0.7f);
        lowMinPitch= Mathf.Clamp(max, 0.2f, 0.7f);
    }

    /// <summary>
    /// 播放BGM
    /// </summary>
    public void PlayBGM(string name, bool randomPitch = false)
    {
        StartCoroutine(LoadAndPlayBGM(name, randomPitch));
    }

    /// <summary>
    /// 播放单次音效
    /// </summary>
    public void PlaySingle(string name, bool randomPitch = false)
    {
        StartCoroutine(LoadAndPlaySingle(name, randomPitch));
    }
    public void PlaySingleLow(string name, bool randomPitch = false)
    {
        StartCoroutine(LoadAndPlaySingle(name, true,true));
    }

    /// <summary>
    /// 播放音效（使用对象池，支持重叠）
    /// </summary>
    public void PlaySound(string name, bool randomPitch = true)
    {
        StartCoroutine(LoadAndPlaySound(name, true));
    }

    public void PlaySoundLow(string name, bool randomPitch = true)
    {
        StartCoroutine(LoadAndPlaySound(name, randomPitch,true));
    }

    /// <summary>
    /// 停止BGM
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }

    /// <summary>
    /// 停止单次音效
    /// </summary>
    public void StopSingle()
    {
        if (singleSource != null && singleSource.isPlaying)
            singleSource.Stop();
    }
    #endregion

    #region 协程方法
    private IEnumerator LoadAndPlayBGM(string fileName, bool randomPitch)
    {
        fileName = SplitName(fileName);
        ResourceRequest request = Resources.LoadAsync<AudioClip>("Sound/" + fileName);
        yield return request;

        AudioClip clip = request.asset as AudioClip;
        if (clip != null)
        {
            if (randomPitch)
                bgmSource.pitch = GetRandomPitch();
            else
                bgmSource.pitch = 1.0f;

            bgmSource.clip = clip;
            bgmSource.Play();
        }
        else
        {
            Debug.LogError($"BGM加载失败: Sound/{fileName}");
        }
    }

    private IEnumerator LoadAndPlaySingle(string fileName, bool randomPitch,bool lowRandom=false)
    {
        fileName = SplitName(fileName);
        ResourceRequest request = Resources.LoadAsync<AudioClip>("Sound/" + fileName);
        yield return request;

        AudioClip clip = request.asset as AudioClip;
        if (clip != null)
        {
            if (randomPitch)
                singleSource.pitch = GetRandomPitch();
            else
                singleSource.pitch = 1.0f;
            if(lowRandom)
                singleSource.pitch=getLowRandomPitch();

                singleSource.clip = clip;
            singleSource.Play();
        }
        else
        {
            Debug.LogError($"音效加载失败: Sound/{fileName}");
        }
    }

    private IEnumerator LoadAndPlaySound(string fileName, bool randomPitch,bool lowRandom=false)
    {
        fileName = SplitName(fileName);
        ResourceRequest request = Resources.LoadAsync<AudioClip>("Sound/" + fileName);
        yield return request;

        AudioClip clip = request.asset as AudioClip;
        if (clip != null)
        {
            // 从对象池获取AudioSource
            AudioSource poolSource = soundEffectPool.Get();

            if (randomPitch)
                poolSource.pitch = GetRandomPitch();
            else
                poolSource.pitch = 1.0f;
            if(lowRandom)
                poolSource.pitch = getLowRandomPitch();
            //print(poolSource.pitch);

            poolSource.PlayOneShot(clip);

            // 播放完成后返回到对象池
            StartCoroutine(ReturnToPoolAfterPlay(poolSource, clip.length));
        }
        else
        {
            Debug.LogError($"音效加载失败: Sound/{fileName}");
        }
    }

    /// <summary>
    /// 播放完成后将AudioSource返回到对象池
    /// </summary>
    private IEnumerator ReturnToPoolAfterPlay(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration + 0.1f); // 额外等待0.1秒确保播放完成

        if (source != null && !source.isPlaying)
        {
            soundEffectPool.Return(source);
        }
    }
    #endregion

    #region 辅助方法
    private string SplitName(string name)
    {
        string[] strs = name.Split(',');
        return strs[rand.Next(0, strs.Length)].Trim();
    }

    private float GetRandomPitch()
    {
        return (float)(rand.NextDouble() * (maxPitch - minPitch) + minPitch);
    }
    private float getLowRandomPitch()
    {
        return (float)(rand.NextDouble() * (lowMaxPitch - lowMinPitch) + lowMinPitch);
    }
    #endregion

    /// <summary>
    /// 清理对象池（在场景切换或游戏结束时调用）
    /// </summary>
    public void ClearPool()
    {
        soundEffectPool?.Clear();
    }

    private void OnDestroy()
    {
        ClearPool();
    }
}
