namespace Verve
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 支持子模块的游戏功能基类
    /// </summary>
    [System.Serializable]
    public abstract class ModularGameFeature : GameFeature
    {
        private readonly Dictionary<string, IGameFeatureSubmodule> m_Submodules = 
            new Dictionary<string, IGameFeatureSubmodule>(StringComparer.Ordinal);

        ~ModularGameFeature() => ((IGameFeature)this).Unload();
        
        
        /// <summary> 注册子模块 </summary>
        public void RegisterSubmodule(IGameFeatureSubmodule submodule)
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
        public TSubmodule GetSubmodule<TSubmodule>(string name) where TSubmodule : class, IGameFeatureSubmodule
        {
            if (m_Submodules.TryGetValue(name, out var submodule))
            {
                return submodule as TSubmodule;
            }
            return null;
        }

        /// <summary>
        /// 获取子模块
        /// </summary>
        public TSubmodule GetSubmodule<TSubmodule>() where TSubmodule : class, IGameFeatureSubmodule
        {
            return m_Submodules.Values.FirstOrDefault(x => x is TSubmodule) as TSubmodule;
        }
        
        public IGameFeatureSubmodule GetSubmodule(Type type)
        {
            return m_Submodules.Values.FirstOrDefault(x => x.GetType() == type);
        }

        /// <summary> 获取所有子模块 </summary>
        public IEnumerable<IGameFeatureSubmodule> GetAllSubmodules() => m_Submodules.Values;
        
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            base.OnLoad(dependencies);
            
            foreach (var submodule in m_Submodules.Values)
            {
                submodule.OnModuleLoaded(dependencies);
            }
        }
        
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
}