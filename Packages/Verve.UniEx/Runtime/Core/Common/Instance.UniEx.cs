#if UNITY_5_3_OR_NEWER
    
namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;


    /// <summary>
    /// 组件单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DisallowMultipleComponent]
    public abstract class ComponentInstanceBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static bool s_IsInitialized;
        
        private static readonly Lazy<T> s_Lazy = new Lazy<T>(() =>
        {
            T instance = null;
            bool isInitialized = false;
            
            T[] existingInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
            if (existingInstances.Length > 0)
            {
                instance = existingInstances[0];
                            
                for (int i = 1; i < existingInstances.Length; i++)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(existingInstances[i].gameObject);
                    }
                }
            }
            else
            {
                var obj = new GameObject(typeof(T).Name);
                instance = obj.AddComponent<T>();
            }
            
            if (!s_IsInitialized)
            {
                isInitialized = true;
                if (UnityEngine.Application.isPlaying)
                {
                    DontDestroyOnLoad(instance?.gameObject);
                }
                
                if (instance is ComponentInstanceBase<T> instanceBase)
                {
                    instanceBase.OnInitialized();
                }
            }
            
            s_IsInitialized = isInitialized;
            
            return instance;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        
        public static T Instance => s_Lazy.Value;
        

        /// <summary>
        /// 初始化（单例首次被创建后调用）
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}
    
#endif