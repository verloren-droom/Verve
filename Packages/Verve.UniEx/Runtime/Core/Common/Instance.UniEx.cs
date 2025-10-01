#if UNITY_5_3_OR_NEWER
    
namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Reflection;


    /// <summary>
    /// 组件单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DisallowMultipleComponent]
    public abstract class ComponentInstanceBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static bool s_IsInitialized;
        
        private static readonly Lazy<T> s_Lazy = new Lazy<T>(CreateInstance, LazyThreadSafetyMode.ExecutionAndPublication);
        
        public static T Instance
        {
            get
            {
                if (s_Lazy.IsValueCreated && s_Lazy.Value == null || (s_Lazy.Value is UnityEngine.Object obj && obj == null))
                {
                    var field = typeof(ComponentInstanceBase<T>).GetField(nameof(s_Lazy), BindingFlags.Static | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        field.SetValue(null, new Lazy<T>(CreateInstance, LazyThreadSafetyMode.ExecutionAndPublication));
                    }
                }
                
                if (Application.isPlaying && s_Lazy.IsValueCreated && s_Lazy.Value == null)
                {
                    typeof(T).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(s_Lazy.Value, null);
                }
                return s_Lazy.Value;
            }
        }

        private static T CreateInstance()
        {
            T instance = null;
            
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
                    else
                    {
                        DestroyImmediate(existingInstances[i].gameObject);
                    }
                }
            }
            else
            {
                instance = new GameObject(typeof(T).Name).AddComponent<T>();
            }
            
            if (!s_IsInitialized)
            {
                s_IsInitialized = true;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(instance?.gameObject);
                }
                
                if (instance is ComponentInstanceBase<T> instanceBase)
                {
                    instanceBase.OnInitialized();
                }
            }
            
            return instance;
        }


        /// <summary>
        /// 初始化（单例首次被创建后调用）
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}
    
#endif