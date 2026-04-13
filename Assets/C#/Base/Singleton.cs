using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// 是否跨场景持久化。子类可以通过 override 返回 true 来保持不被销毁。
    /// </summary>
    protected virtual bool IsPersistent => false;

    public static T Instance
    {
        get
        {
            // 如果程序正在退出，不再创建新单例（防止留在场景中导致报错）
            if (_applicationIsQuitting)
            {
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 1. 尝试在场景中查找已有的对象
                    _instance = (T)Object.FindFirstObjectByType(typeof(T));

                    // 2. 如果场景中没有，则动态创建一个
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";

                        // 3. 处理持久化逻辑
                        // 通过强转访问基类定义的 IsPersistent 属性
                        var singletonComponent = _instance as Singleton<T>;
                        if (singletonComponent != null && singletonComponent.IsPersistent)
                        {
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// 确保手动拖到场景中的对象也能正常工作
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            if (IsPersistent)
            {
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (_instance != this)
        {
            // 如果已经存在实例且不是自己，销毁重复的
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        // 只有当销毁的是当前记录的实例时才清空引用
        if (_instance == this)
        {
            _instance = null;
        }
    }
}