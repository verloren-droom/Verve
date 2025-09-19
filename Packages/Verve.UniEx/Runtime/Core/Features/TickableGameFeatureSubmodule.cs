#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;

    
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
        
    
        protected virtual void OnTick(in GameFeatureContext ctx) { }
    }
    
    
    [Serializable]
    public abstract class TickableGameFeatureSubmodule<T> : TickableGameFeatureSubmodule, IComponentGameFeatureSubmodule
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