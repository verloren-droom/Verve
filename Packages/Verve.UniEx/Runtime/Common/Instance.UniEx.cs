#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx
{
    using UnityEngine;
    
    
    /// <summary>
    /// 组件单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DisallowMultipleComponent]
    public abstract class ComponentInstanceBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T m_Instance;
        private static bool m_IsInitialized;
        
        public static T Instance
        {
            get
            {
                // m_Instance ??= GameObject.FindObjectOfType<T>();
                if (m_Instance == null)
                {
                    T[] existingInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
                    if (existingInstances.Length > 0)
                    {
                        m_Instance = existingInstances[0];
                                
                        for (int i = 1; i < existingInstances.Length; i++)
                        {
                            Destroy(existingInstances[i].gameObject);
                        }
                    }
                    else
                    {
                        var obj = new GameObject(typeof(T).Name);
                        m_Instance = obj.AddComponent<T>();
                    }
                }
                if (!m_IsInitialized)
                {
                    m_IsInitialized = true;
                    DontDestroyOnLoad(m_Instance.gameObject);
                    (m_Instance as ComponentInstanceBase<T>).OnInitialized();
                }
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
    
#endif