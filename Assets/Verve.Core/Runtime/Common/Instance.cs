namespace Verve
{
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif


    /// <summary>
    /// 单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class InstanceBase<T> where T : class, new()
    {
        private static T m_Instance;

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new T();
                    (m_Instance as InstanceBase<T>).OnInitialized();
                }
                return m_Instance;
            }
            private set => m_Instance = value;
        }

        protected InstanceBase() { }
        
        /// <summary>
        /// 初始化（单例首次被创建后调用）
        /// </summary>
        protected virtual void OnInitialized() { }
    }
    
    
#if UNITY_5_3_OR_NEWER
    /// <summary>
    /// 组件单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DisallowMultipleComponent]
    public abstract class ComponentInstanceBase<T> : MonoBehaviour where T : MonoBehaviour
#else
    public abstract class ComponentInstanceBase<T> where T : class, new()
#endif
    {
        private static T m_Instance;
        
        public static T Instance
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                m_Instance ??= GameObject.FindObjectOfType<T>();
                if (m_Instance == null)
                {
                    var obj = new GameObject(typeof(T).Name);
                    m_Instance = obj.AddComponent<T>();
                    DontDestroyOnLoad(obj);
                    (m_Instance as ComponentInstanceBase<T>).OnInitialized();
                }
#else
                if (m_Instance == null)
                {
                    m_Instance = new T();
                    (m_Instance as InstanceBase<T>).OnInitialized();
                }
#endif
                return m_Instance;
            }
            private set => m_Instance = value;
        }
        
        /// <summary>
        /// 初始化（单例首次被创建后调用）
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}