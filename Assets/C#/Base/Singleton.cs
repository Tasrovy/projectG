using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// 是否跨场景持久化。子类可以通过 override 返回 true 来保持不被销毁。
    /// 默认返回 false，即切换场景时自动销毁。
    /// </summary>
    protected virtual bool IsPersistent => false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting) return null;

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)FindFirstObjectByType(typeof(T));

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";
                        
                        // 如果子类要求持久化，则调用 DontDestroyOnLoad
                        // 这里通过访问刚刚生成的实例的 IsPersistent 来判断
                        if ((_instance as Singleton<T>).IsPersistent)
                        {
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            // 检查当前子类是否需要持久化
            if (IsPersistent)
            {
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}