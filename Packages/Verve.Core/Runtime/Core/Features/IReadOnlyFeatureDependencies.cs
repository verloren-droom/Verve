namespace Verve
{
    using System;
    using System.Collections.Generic;
    

    /// <summary>
    /// 只读功能依赖项
    /// </summary>
    public interface IReadOnlyFeatureDependencies : IDisposable
    {
        IGameFeature this[string featureName] { get; }
        bool TryGet<TFeature>(out TFeature feature) where TFeature : class, IGameFeature;
        TFeature Get<TFeature>() where TFeature : class, IGameFeature;
    }
    
    
    public sealed class FeatureDependencies : IReadOnlyFeatureDependencies
    {
        private readonly IReadOnlyDictionary<string, IGameFeature> m_Source;
        private readonly Dictionary<Type, WeakReference<IGameFeature>> m_Cache = new Dictionary<Type, WeakReference<IGameFeature>>();
    
        public FeatureDependencies(IReadOnlyDictionary<string, IGameFeature> source)
        {
            m_Source = source;
        }

        public IGameFeature this[string featureName] => m_Source[featureName];

        public bool TryGet<TFeature>(out TFeature feature) where TFeature : class, IGameFeature
        {
            feature = Get<TFeature>();
            return feature != null;
        }

        public TFeature Get<TFeature>() where TFeature : class, IGameFeature
        {
            if (m_Cache.TryGetValue(typeof(TFeature), out var cached) && cached.TryGetTarget(out var cachedFeature))
                return (TFeature)cachedFeature;
            
            foreach (var feature in m_Source.Values)
            {
                if (feature is TFeature result)
                {
                    m_Cache[typeof(TFeature)] = new WeakReference<IGameFeature>(result);
                    return result;
                }
            }
            return null;
        }
        
        public void Dispose()
        {
            m_Cache.Clear();
        }
    }

}