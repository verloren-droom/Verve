#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Collections;
    
    
    [Serializable]
    public abstract class GameFeatureSubmodule : IGameFeatureSubmodule
    {
        public bool IsEnabled { get; set; } = true;
    
        [NonSerialized] private bool m_IsStarting;
        [NonSerialized] private bool m_StartCompleted;
    
        protected GameFeatureSubmodule() { }
        ~GameFeatureSubmodule() => ((IGameFeatureSubmodule)this).Shutdown();
    
        void IGameFeatureSubmodule.Startup()
        {
            if (m_IsStarting || m_StartCompleted) 
                return;
            
            m_IsStarting = true;
        
            OnStartup();
        
            m_StartCompleted = true;
            m_IsStarting = false;
        }
    
        void IGameFeatureSubmodule.Shutdown()
        {
            OnShutdown();
            GC.SuppressFinalize(this);
        }
    
        /// <summary>
        /// 启动子模块
        /// </summary>
        /// <remarks>
        /// 如果需要进行异步初始化，请返回一个IEnumerator；如果只进行同步初始化，请返回null或空方法
        /// </remarks>
        protected virtual IEnumerator OnStartup() { yield break; }
    
        protected virtual void OnShutdown() { }
    
        /// <summary>
        /// 异步启动协程
        /// </summary>
        internal IEnumerator StartupCoroutine()
        {
            if (m_IsStarting || m_StartCompleted) 
                yield break;
            
            m_IsStarting = true;
        
            var startupCoroutine = OnStartup();
        
            if (startupCoroutine != null)
            {
                while (startupCoroutine.MoveNext())
                {
                    yield return startupCoroutine.Current;
                }
            }
            else
            {
                m_StartCompleted = true;
            }
        
            m_IsStarting = false;
        }
    }
    
    
    [Serializable]
    public abstract class GameFeatureSubmodule<T> : GameFeatureSubmodule, IComponentGameFeatureSubmodule
        where T : GameFeatureComponent
    {
        [SerializeField, HideInInspector, Tooltip("绑定的模块组件")] private T m_Component;
        
        /// <summary> 绑定的模块组件 </summary>
        public T Component 
        { 
            get
            {
#if UNITY_EDITOR
                if (m_Component == null)
                {
                    try
                    {
                        m_Component = UnityEngine.ScriptableObject.CreateInstance<T>();
                    }
                    catch { }
                }
#endif
                return m_Component;
            }
            set => m_Component = value;
        }
        

        Type IComponentGameFeatureSubmodule.ComponentType
            => typeof(T);

        IGameFeatureComponent IComponentGameFeatureSubmodule.GetComponent()
        {
            return m_Component;
        }
        
        void IComponentGameFeatureSubmodule.SetComponent(IGameFeatureComponent component) 
            => m_Component = component as T;
    }
}

#endif