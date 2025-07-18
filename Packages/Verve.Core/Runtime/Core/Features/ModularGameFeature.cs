namespace Verve
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 支持子模块的游戏功能基类
    /// </summary>
    [System.Serializable]
    public abstract class ModularGameFeature<T> : GameFeature where T : class, IGameFeatureSubmodule
    {
        private readonly Dictionary<string, T> m_Submodules = 
            new Dictionary<string, T>(StringComparer.Ordinal);

        ~ModularGameFeature() => ((IGameFeature)this).Unload();
        
        
        /// <summary> 注册子模块 </summary>
        public void RegisterSubmodule(T submodule)
        {
            if (submodule == null) return;
            
            string name = submodule.ModuleName;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Submodule must have a valid name");
            
            // if (m_Submodules.ContainsKey(name))
            //     throw new InvalidOperationException($"Submodule '{name}' already registered");
            
            m_Submodules[name] = submodule;
        }
        
        /// <summary> 获取子模块 </summary>
        public T GetSubmodule(string name)
        {
            if (m_Submodules.TryGetValue(name, out var submodule))
            {
                return submodule;
            }
            return default;
        }

        /// <summary>
        /// 获取子模块
        /// </summary>
        public TSubmodule GetSubmodule<TSubmodule>() where TSubmodule : class, T
        {
            return m_Submodules.Values.FirstOrDefault(x => x is TSubmodule) as TSubmodule;
        }
        
        public T GetSubmodule(Type type)
        {
            return m_Submodules.Values.FirstOrDefault(x => x.GetType() == type);
        }

        /// <summary> 获取所有子模块 </summary>
        public IEnumerable<T> GetAllSubmodules() => m_Submodules.Values;
        
        protected sealed override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            base.OnLoad(dependencies);
            
            OnBeforeSubmodulesLoaded(dependencies);
            foreach (var submodule in m_Submodules.Values)
            {
                submodule.OnModuleLoaded(dependencies);
            }
            OnAfterSubmodulesLoaded(dependencies);
        }
        
        protected virtual void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies) { }
        protected virtual void OnAfterSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies) { }
        
        protected override void OnUnload()
        {
            foreach (var submodule in m_Submodules.Values)
            {
                submodule.OnModuleUnloaded();
            }
            
            base.OnUnload();
            m_Submodules.Clear();
        }
    }
    
    public abstract class ModularGameFeature : ModularGameFeature<IGameFeatureSubmodule>
    {
        
    }
}