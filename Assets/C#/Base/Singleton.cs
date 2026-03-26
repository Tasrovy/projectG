using UnityEngine;

/// <summary>
/// 懒汉模式单例基类 (MonoBehaviour)
/// </summary>
/// <typeparam name="T">继承了MonoBehaviour的子类类型</typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            // 在程序退出时，不再响应单例请求，防止在Hierarchy中生成“孤儿对象”
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] {typeof(T)} 实例在程序退出时被尝试访问，返回null。");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 1. 尝试在场景中查找已有的对象
                    _instance = (T)FindFirstObjectByType(typeof(T));

                    // 2. 如果场景中没有，则创建一个新的GameObject并挂载脚本
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";

                        // 3. 切换场景时不销毁 (可选)
                        // DontDestroyOnLoad(singletonObject);
                        
                        Debug.Log($"[Singleton] 自动创建了实例: {singletonObject.name}");
                    }
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// 当脚本实例被载入时调用
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            // 如果希望全局唯一且不随场景销毁，取消下面注释
            // DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] 场景中存在多个 {typeof(T)} 实例，正在销毁冗余对象。");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 程序退出标记
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        // 如果是手动销毁了当前实例，重置引用
        if (_instance == this)
        {
            _instance = null;
        }
    }
}