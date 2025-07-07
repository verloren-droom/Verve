namespace Verve
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    /// 游戏功能运行时
    /// </summary>
    [Serializable]
    public sealed class GameFeaturesRuntime
    {
        [Serializable]
        private sealed class FeatureInfo
        {
            public IGameFeature Feature;
            public FeatureState State = FeatureState.Unloaded;
            public IGameFeatureData Data;
            public int ReferenceCount;
            public HashSet<string> Dependents = new HashSet<string>();
        }

        
        private readonly Dictionary<string, FeatureInfo> m_Features = 
            new Dictionary<string, FeatureInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> m_Dependencies = 
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        
        
        /// <summary> 注册功能数据 </summary>
        public void RegisterFeatureData(IGameFeatureData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            
            string name = data.FeatureName;
            if (m_Features.ContainsKey(name)) return;

            FeatureInfo info = new FeatureInfo
            {
                // Feature = data.CreateFeature(),
                Data = data,
                State = FeatureState.Unloaded
            };
            
            m_Features[name] = info;
            
            foreach (string dependency in data.Dependencies)
            {
                if (dependency == name) continue;
                
                if (!m_Dependencies.TryGetValue(name, out var deps))
                {
                    deps = new List<string>();
                    m_Dependencies[name] = deps;
                }
                
                if (!deps.Contains(dependency))
                    deps.Add(dependency);
                
                if (!m_Features.TryGetValue(dependency, out var depInfo))
                {
                    depInfo = new FeatureInfo {
                        Dependents = new HashSet<string>()
                    };
                    m_Features[dependency] = depInfo;
                }
                depInfo.Dependents.Add(name);
            }
        }

        /// <summary> 加载功能及其依赖 </summary>
        public void LoadFeature([NotNull] string featureName, Action<bool> onComplete = null)
        {
            if (!m_Features.TryGetValue(featureName, out var info) || info.State != FeatureState.Unloaded)
            {
                onComplete?.Invoke(false);
                return;
            }

            if (m_Dependencies.TryGetValue(featureName, out var dependencies))
            {
                foreach (string dependency in dependencies)
                {
                    if (!m_Features.TryGetValue(dependency, out var depInfo))
                        throw new GameFeatureMissingDependencyException(featureName, dependency);
                    depInfo.ReferenceCount++;
                }
            }

            try
            {
                info.Feature = info.Data.CreateFeature();
                info.Feature.Load();
                info.State = FeatureState.Loaded;
                
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                info.State = FeatureState.Unloaded;
                onComplete?.Invoke(false);
                throw new GameFeatureLoadException(featureName, e.Message);
            }
        }
        
        /// <summary> 激活功能 </summary>
        public void ActivateFeature([NotNull] string featureName)
        {
            if (!m_Features.TryGetValue(featureName, out var info) || info.State != FeatureState.Loaded) return;

            try
            {
                info.Feature.Activate();
                info.State = FeatureState.Active;
            }
            catch (Exception e)
            {
                info.State = FeatureState.Loaded;
                throw new GameFeatureActivateException(featureName, e.Message);
            }
        }
        
        /// <summary> 停用功能 </summary>
        public void DeactivateFeature([NotNull] string featureName)
        {
            if (!m_Features.TryGetValue(featureName, out var info) || info.State != FeatureState.Active) return;

            try
            {
                info.Feature?.Deactivate();
                info.State = FeatureState.Loaded;
            }
            catch (Exception e)
            {
                info.State = FeatureState.Active;
                throw new GameFeatureDeactivateException(featureName, e.Message);
            }
        }
        
        /// <summary> 卸载功能 </summary>
        public void UnloadFeature([NotNull] string featureName)
        {
            if (!m_Features.TryGetValue(featureName, out var info) || 
                info.State < FeatureState.Loaded) 
                return;
            
            try
            {
                if (info.State == FeatureState.Active)
                    DeactivateFeature(featureName);
    
                if (info.ReferenceCount > 0)
                    throw new GameFeatureHasActiveReferencesException(featureName, info.ReferenceCount);
    
                info.State = FeatureState.Unloading;
                
                if (m_Dependencies.TryGetValue(featureName, out var dependencies))
                {
                    for (int i = dependencies.Count - 1; i >= 0; i--)
                    {
                        if (m_Features.TryGetValue(dependencies[i], out var depInfo))
                        {
                            depInfo.ReferenceCount--;
                        }
                    }
                }
                
                info.Feature?.Unload();
                info.State = FeatureState.Unloaded;
                
                m_Features.Remove(featureName);
                m_Dependencies.Remove(featureName);
            }
            catch (Exception e)
            {
                info.State = FeatureState.Loaded;
                throw new GameFeatureUnloadException(featureName, e.Message);
            }
        }

        /// <summary> 获取指定功能的依赖项 </summary>
        public IReadOnlyList<string> GetDependencies([NotNull] string featureName)
        {
            return m_Dependencies.TryGetValue(featureName, out var deps) ? 
                deps.AsReadOnly() : Array.Empty<string>();
        }
        
        /// <summary> 获取依赖指定功能的所有功能 </summary>
        public IReadOnlyCollection<string> GetDependents([NotNull] string featureName)
        {
            return m_Features.TryGetValue(featureName, out var info) ? 
                info.Dependents : Array.Empty<string>();
        }
        
        /// <summary> 获取功能状态 </summary>
        public FeatureState GetFeatureState([NotNull] string featureName) => m_Features.TryGetValue(featureName, out var info) ? info.State : FeatureState.Unloaded;

        /// <summary> 获取已注册的功能列表 </summary>
        public IEnumerable<string> GetRegisteredFeatures() => m_Features.Keys;

        /// <summary> 获取指定类型的已激活功能实例 </summary>
        public IGameFeature GetFeature([NotNull] string featureName)
        {
            if (m_Features.TryGetValue(featureName, out var info))
            {
                if (info.State == FeatureState.Active)
                    return info.Feature;
                throw new GameFeatureNotActiveException(featureName, info.State);
            }
            return null;
        }

        /// <summary> 尝试获取指定类型的已激活功能实例 </summary>
        public bool TryGetFeature([NotNull] string featureName, out IGameFeature feature)
        {
            feature = GetFeature(featureName);
            return feature != null;
        }
        
        /// <summary> 获取指定类型的已激活功能实例 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TFeature GetFeature<TFeature>() where TFeature : class, IGameFeature
            => m_Features?.FirstOrDefault(x 
                => x.Value.Feature is TFeature && x.Value.State == FeatureState.Active).Value.Feature as TFeature;

        /// <summary> 尝试获取指定类型的已激活功能实例 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetFeature<TFeature>(out IGameFeature feature) where TFeature : class, IGameFeature
        {
            feature = GetFeature<TFeature>();
            return feature != null;
        }
        
        /// <summary> 清理所有功能（按依赖顺序） </summary>
        public void Cleanup()
        {
            // 获取拓扑排序的卸载顺序
            var unloadOrder = GetTopologicalUnloadOrder();
            foreach (var featureName in unloadOrder)
            {
                UnloadFeature(featureName);
            }
            
            m_Features.Clear();
            m_Dependencies.Clear();
        }

        /// <summary> 获取拓扑卸载顺序 </summary>
        private List<string> GetTopologicalUnloadOrder()
        {
            var result = new List<string>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var featureName in m_Features.Keys)
            {
                if (!visited.Contains(featureName))
                {
                    TopologicalVisit(featureName, visited, visiting, result);
                }
            }
            
            result.Reverse();
            return result;
        }

        private void TopologicalVisit(
            string featureName, 
            HashSet<string> visited,
            HashSet<string> visiting,
            List<string> result)
        {
            if (visited.Contains(featureName)) return;
            if (visiting.Contains(featureName))
                throw new InvalidOperationException($"Circular dependency detected: {featureName}");

            visiting.Add(featureName);
            
            if (m_Dependencies.TryGetValue(featureName, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (m_Features.ContainsKey(dep))
                        TopologicalVisit(dep, visited, visiting, result);
                }
            }
            
            visiting.Remove(featureName);
            visited.Add(featureName);
            result.Add(featureName);
        }
    }
}