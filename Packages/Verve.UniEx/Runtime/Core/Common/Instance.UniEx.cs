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
        private static T s_Instance;
        private static bool s_IsInitialized;
        
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    T[] existingInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
                    if (existingInstances.Length > 0)
                    {
                        s_Instance = existingInstances[0];
                                
                        for (int i = 1; i < existingInstances.Length; i++)
                        {
                            Destroy(existingInstances[i].gameObject);
                        }
                    }
                    else
                    {
                        var obj = new GameObject(typeof(T).Name);
                        s_Instance = obj.AddComponent<T>();
                    }
                }
                if (!s_IsInitialized)
                {
                    s_IsInitialized = true;
                    if (UnityEngine.Application.isPlaying)
                    {
                        DontDestroyOnLoad(s_Instance?.gameObject);
                    }
                    (s_Instance as ComponentInstanceBase<T>).OnInitialized();
                }
                return s_Instance;
            }
            private set => s_Instance = value;
        }
        
        
        /// <summary>
        /// 初始化（单例首次被创建后调用）
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}
    
#endif