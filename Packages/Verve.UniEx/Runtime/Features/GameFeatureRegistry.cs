#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using Verve;
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Collections.Generic;


    /// <summary>
    /// 游戏功能注册表
    /// </summary>
    // [CreateAssetMenu(fileName = "New GameFeatureRegistry", menuName = "Verve/Features/Feature Registry", order = 0)]
    [DisallowMultipleComponent, AddComponentMenu("Verve/Features/GameFeatureRegistry")]
    public sealed partial class GameFeatureRegistry : MonoBehaviour
    {
        [SerializeField, Tooltip("注册游戏功能资源")] private GameFeatureAsset[] m_Features = Array.Empty<GameFeatureAsset>();
        [SerializeField, Tooltip("是否自动注册功能")] private bool m_AutoRegister = true;
        [SerializeField, Tooltip("是否持久化")] private bool m_IsPersistent = true;
        
        private List<GameFeatureAsset> m_SortedFeatures;
        
        public IReadOnlyCollection<GameFeatureAsset> FeatureAssets => m_Features;
        public IReadOnlyList<GameFeatureAsset> SortedFeatures => m_SortedFeatures;

        private void Awake()
        {
            if (m_IsPersistent && UnityEngine.Application.isPlaying)
                DontDestroyOnLoad(this);
        }

        private void OnEnable()
        {
            if (m_AutoRegister)
                RegisterAll();
        }

        private void OnDisable()
        {
            if (UnityEngine.Application.isPlaying)
            {
                UnregisterAll();
            }
        }

        public void RegisterAll()
        {
            m_SortedFeatures = SortByDependencies(m_Features);
            
            foreach (var feat in m_SortedFeatures)
            {
                GameFeaturesSystem.Runtime.RegisterFeatureData(feat);
                
                if (feat.AutoLoad)
                {
                    GameFeaturesSystem.Runtime.LoadFeature(feat.FeatureName);
                    if (feat.AutoActivate)
                        GameFeaturesSystem.Runtime.ActivateFeature(feat.FeatureName);
                }
            }
        }
        
        public void UnregisterAll()
        {
            if (m_SortedFeatures == null) return;
            
            for (int i = m_SortedFeatures.Count - 1; i >= 0; i--)
            {
                var feature = m_SortedFeatures[i];
                if (feature != null)
                    GameFeaturesSystem.Runtime.UnloadFeature(feature.FeatureName);
            }
            
            m_SortedFeatures = null;
        }

        private List<GameFeatureAsset> SortByDependencies(
            IEnumerable<GameFeatureAsset> features)
        {
            var graph = new Dictionary<GameFeatureAsset, List<GameFeatureAsset>>();
            var nodeMap = new Dictionary<string, GameFeatureAsset>();
            var result = new List<GameFeatureAsset>();

            foreach (var feat in features)
            {
                if (feat == null) continue;
                nodeMap[feat.FeatureName] = feat;
                graph[feat] = new List<GameFeatureAsset>();
            }

            foreach (var feat in features)
            {
                if (feat == null) continue;
                
                foreach (var depName in feat.Dependencies)
                {
                    if (nodeMap.TryGetValue(depName, out var dependency))
                        graph[feat].Add(dependency);
                }
            }

            var visited = new HashSet<GameFeatureAsset>();
            var stack = new HashSet<GameFeatureAsset>();
            
            foreach (var feat in graph.Keys)
            {
                if (!visited.Contains(feat))
                    TopologicalSort(feat, graph, visited, stack, result);
            }
            
            return result;
        }

        private void TopologicalSort(
            GameFeatureAsset feature,
            Dictionary<GameFeatureAsset, List<GameFeatureAsset>> graph,
            HashSet<GameFeatureAsset> visited,
            HashSet<GameFeatureAsset> stack,
            List<GameFeatureAsset> result)
        {
            if (stack.Contains(feature))
                throw new InvalidOperationException($"Circular dependency: {feature.FeatureName}");
            
            if (visited.Contains(feature)) return;
            
            visited.Add(feature);
            stack.Add(feature);
            
            foreach (var dep in graph[feature])
                TopologicalSort(dep, graph, visited, stack, result);
            
            stack.Remove(feature);
            result.Add(feature);
        }
    }
}

#endif