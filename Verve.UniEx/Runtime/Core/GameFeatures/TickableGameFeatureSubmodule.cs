#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;

    
    /// <summary>
    ///   <para>可更新游戏功能子模块基类</para>
    /// </summary>
    /// <example>
    /// <code>
    /// [Serializable, GameFeatureSubmodule(typeof(MyGameFeature))]
    /// public class MyGameFeatureSubmodule : TickableGameFeatureSubmodule
    /// {
    ///     protected override IEnumerator OnStartup()
    ///     {
    ///         // 启动逻辑
    ///         yield break;
    ///     }
    ///     protected override void OnTick(in GameFeatureContext ctx)
    ///     {
    ///         // 更新逻辑
    ///     }
    ///     protected override void OnShutdown()
    ///     {
    ///         // 停止逻辑
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class TickableGameFeatureSubmodule : GameFeatureSubmodule, ITickableGameFeatureSubmodule
    {
        private readonly Action<TickableGameFeatureSubmodule, GameFeatureContext> s_UpdateCache
            = static (s, ctx) => s.VirtualOnTick(in ctx);

        void ITickableGameFeatureSubmodule.Tick(in IGameFeatureContext context)
        {
            if (context is GameFeatureContext ctx)
            {
                s_UpdateCache(this, ctx);
            }
        }

        private void VirtualOnTick(in GameFeatureContext context)
            => OnTick(in context);
        
    
        /// <summary>
        ///   <para>每帧更新</para>
        /// </summary>
        /// <param name="ctx"> 游戏功能上下文 </param>
        protected virtual void OnTick(in GameFeatureContext ctx) { }
    }
    
    
    /// <summary>
    ///   <para>可更新游戏功能子模块基类</para>
    /// </summary>
    /// <typeparam name="T">游戏组件类型</typeparam>
    [Serializable]
    public abstract class TickableGameFeatureSubmodule<T> : TickableGameFeatureSubmodule, IComponentGameFeatureSubmodule
        where T : GameFeatureComponent
    {
        [SerializeField, HideInInspector, Tooltip("绑定的模块组件")] private T m_Component;
        
        
        /// <summary>
        ///   <para>绑定的模块组件</para>
        /// </summary>
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
        
        Type IComponentGameFeatureSubmodule.ComponentType => typeof(T);
        
        IGameFeatureComponent IComponentGameFeatureSubmodule.GetComponent()
        {
            return m_Component;
        }
        
        void IComponentGameFeatureSubmodule.SetComponent(IGameFeatureComponent component) 
            => m_Component = component as T;
    }
}

#endif