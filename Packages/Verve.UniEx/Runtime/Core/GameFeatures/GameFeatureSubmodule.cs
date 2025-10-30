#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Collections;
    
    
    /// <summary>
    ///   <para>游戏功能子模块基类</para>
    /// </summary>
    /// <example>
    /// <code>
    /// [Serializable, GameFeatureSubmodule(typeof(MyGameFeature))]
    /// public class MyGameFeatureSubmodule : GameFeatureSubmodule
    /// {
    ///     protected override IEnumerator OnStartup()
    ///     {
    ///         // 启动逻辑
    ///         yield break;
    ///     }
    ///     protected override void OnShutdown()
    ///     {
    ///         // 停止逻辑
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class GameFeatureSubmodule : IGameFeatureSubmodule
    {
        public virtual bool IsEnabled { get; set; } = true;
    
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
            ((IGameFeatureSubmodule)this).Dispose();
        }
    
        /// <summary>
        ///   <para>启动子模块</para>
        ///   <para>如果需要进行异步初始化，请返回一个IEnumerator；如果只进行同步初始化，请返回null或空方法</para>
        /// </summary>
        protected virtual IEnumerator OnStartup() { yield break; }
    
        protected virtual void OnShutdown() { }
    
        /// <summary>
        ///   <para>异步启动协程</para>
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
        
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   <para>释放资源</para>
        /// </summary>
        protected virtual void Dispose(bool disposing) { }
    }
    
    
    /// <summary>
    ///   <para>模块组件子模块基类</para>
    /// </summary>
    /// <typeparam name="T">游戏组件类型</typeparam>
    [Serializable]
    public abstract class GameFeatureSubmodule<T> : GameFeatureSubmodule, IComponentGameFeatureSubmodule
        where T : GameFeatureComponent
    {
        [SerializeField, HideInInspector, Tooltip("绑定的模块组件")] private T m_Component;
        
        /// <summary>
        ///   <para>绑定的模块组件</para>
        /// </summary>
        protected T Component 
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