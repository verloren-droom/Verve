#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para>组件实例基类。</para>
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    [DisallowMultipleComponent]
    public abstract class ComponentInstanceBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        ///   <para>实例。</para>
        /// </summary>
        private static T s_Instance;

        /// <summary>
        ///   <para>单例实例。</para>
        /// </summary>
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = CreateInstance();
                }

                return s_Instance;
            }
        }

        /// <summary>
        ///   <para>创建实例。</para>
        /// </summary>
        /// <returns>
        ///   <para>实例</para>
        /// </returns>
        private static T CreateInstance()
        {
#if UNITY_2022_2_OR_NEWER
            T[] existingInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            T[] existingInstances = FindObjectsOfType<T>();
#endif
            if (existingInstances.Length > 1)
                throw new InvalidOperationException($"Multiple singleton components found: {typeof(T).FullName}.");
            var instance = existingInstances.Length == 1
                ? existingInstances[0]
                : new GameObject(typeof(T).Name).AddComponent<T>();

            if (Application.isPlaying && instance != null)
            {
                DontDestroyOnLoad(instance.gameObject);
            }

            if (instance is ComponentInstanceBase<T> instanceBase)
            {
                instanceBase.OnInitialized();
            }

            return instance;
        }

        /// <summary>
        ///   <para>清除被 Unity 销毁的单例引用，允许下一次访问重新创建实例。</para>
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (!ReferenceEquals(s_Instance, this))
            {
                return;
            }

            s_Instance = null;
        }

        /// <summary>
        ///   <para>单例初始化；仅在单例首次被创建后调用。</para>
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}

#endif
