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

                        Debug.Log($"[Singleton] {typeof(T).Name}.Instance getter → 场景中未找到，已动态创建 (GameObject: {singletonObject.name})");

                        // 3. 处理持久化逻辑
                        var singletonComponent = _instance as Singleton<T>;
                        if (singletonComponent != null && singletonComponent.IsPersistent)
                        {
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                    else
                    {
                        Debug.Log($"[Singleton] {typeof(T).Name}.Instance getter → 场景中找到已有实例 (GameObject: {_instance.gameObject.name})");
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
        // 在被销毁之前先记录 GameObject 是否还活着
        if (gameObject == null)
        {
            Debug.LogError($"[Singleton] {typeof(T).Name}.Awake() → gameObject 为 null！组件可能已从对象上移除");
            return;
        }

        Debug.Log($"[Singleton] {typeof(T).Name}.Awake() 开始 (GameObject: {gameObject.name}, activeInHierarchy: {gameObject.activeInHierarchy}, _instance: {(_instance == null ? "null" : _instance.name)})");

        if (_instance == null)
        {
            _instance = this as T;

            if (IsPersistent)
            {
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            Debug.Log($"[Singleton] {typeof(T).Name}.Awake() → _instance 注册成功 (GameObject: {gameObject.name}, IsPersistent: {IsPersistent})");
        }
        else if (_instance != this)
        {
            // 如果已经存在实例且不是自己，销毁重复的
            Debug.LogError($"[Singleton] {typeof(T).Name} 检测到重复实例！\n" +
                $"  当前实例 (this): {this.name} (GameObject: {gameObject.name})\n" +
                $"  已存在的 _instance: {_instance.name} (GameObject: {_instance.gameObject.name})\n" +
                $"  → 即将销毁 {gameObject.name}（连同其上的所有组件）");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[Singleton] {typeof(T).Name}.Awake() → _instance 已被提前设置为 this (GameObject: {gameObject.name})，跳过注册");
        }

        Debug.Log($"[Singleton] {typeof(T).Name}.Awake() 结束 (GameObject: {gameObject.name})");
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
            Debug.Log($"[Singleton] {typeof(T).Name}.OnDestroy() → _instance 被清空 (GameObject: {gameObject.name})");
            _instance = null;
        }
        else if (_instance != null)
        {
            Debug.Log($"[Singleton] {typeof(T).Name}.OnDestroy() → _instance 不是 this，不清空 (this: {gameObject.name}, _instance: {_instance.gameObject.name})");
        }
    }
}