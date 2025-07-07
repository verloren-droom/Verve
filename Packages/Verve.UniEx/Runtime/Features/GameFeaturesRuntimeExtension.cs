#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using Verve;
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;
    
    
    /// <summary>
    /// 游戏功能运行时扩展
    /// </summary>
    public static class GameFeaturesRuntimeExtension
    {
        private static readonly Dictionary<string, GameFeatureAsset> m_FeatureAssets = 
            new Dictionary<string, GameFeatureAsset>();


        /// <summary> 注册功能资产 </summary>
        public static void RegisterFeatureAsset(this GameFeaturesRuntime self, GameFeatureAsset asset)
        {
            if (asset == null) return;
            
            string featureName = asset.FeatureName;
            if (string.IsNullOrWhiteSpace(featureName)) return;
            
            if (!m_FeatureAssets.ContainsKey(featureName))
            {
                self.RegisterFeatureData(asset);
                m_FeatureAssets.Add(featureName, asset);
            }
        }
        
        public static Coroutine LoadFeatureAsync(this MonoBehaviour self, string featureName, Action<bool> onComplete = null)
        {
            return self.StartCoroutine(GameFeaturesSystem.Runtime.LoadFeatureAsync(featureName, onComplete));
        }
        
        public static Coroutine UnloadFeatureAsync(this MonoBehaviour self, string featureName)
        {
            return self.StartCoroutine(GameFeaturesSystem.Runtime.UnloadFeatureAsync(featureName));
        }

        /// <summary> 异步加载功能 </summary>
        public static IEnumerator LoadFeatureAsync(this GameFeaturesRuntime self, [System.Diagnostics.CodeAnalysis.NotNull] string featureName, Action<bool> onComplete = null)
        {
            if (!self.GetRegisteredFeatures().Contains(featureName))
                throw new GameFeatureNotRegisteredException(featureName);
            
            var state = self.GetFeatureState(featureName);
            if (state != FeatureState.Unloaded)
                yield break;
            
            // var dependencies = self.GetDependencies(featureName).ToList();
            // foreach (var dependency in dependencies)
            // {
            //     yield return self.LoadFeatureAsync(dependency);
            // }

            if (self.GetFeature(featureName) is GameFeatureComponent component)
            {
                if (!component.gameObject.scene.IsValid())
                {
                    Object.Instantiate(component);
                }
            }

            self.LoadFeature(featureName, onComplete);
        }

        /// <summary> 异步卸载功能 </summary>
        public static IEnumerator UnloadFeatureAsync(this GameFeaturesRuntime self, [System.Diagnostics.CodeAnalysis.NotNull] string featureName)
        {
            if (!self.TryGetFeature(featureName, out var feature) || !UnityEngine.Application.isPlaying) yield break;
            
            self.DeactivateFeature(featureName);
            
            // var dependencies = self.GetDependencies(featureName).Reverse().ToList();
            // foreach (var dependency in dependencies)
            // {
            //     yield return self.UnloadFeatureAsync(dependency);
            // }
            
            self.UnloadFeature(featureName);

            if (feature is GameFeatureComponent component
                && component != null && component.gameObject != null
                && component.gameObject.scene.IsValid())
            {
                yield return new WaitForEndOfFrame();
                if (UnityEngine.Application.isPlaying)
                    Object.Destroy(component.gameObject);
                else
                    Object.DestroyImmediate(component.gameObject);
            }
            else if (feature is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            while (self.GetFeatureState(featureName) != FeatureState.Unloaded)
            {
                yield return null;
            }
        }
        
        /// <summary> 获取已注册的功能资产 </summary>
        public static IReadOnlyCollection<GameFeatureAsset> GetFeatureAssets(this GameFeaturesRuntime _) => m_FeatureAssets.Values;
        
        /// <summary> 获取已注册的功能组件 </summary>
        public static T GetFeatureComponent<T>(this GameFeaturesRuntime self) where T : GameFeatureComponent
        {
            return self.GetFeature<T>();
        }
        
        /// <summary> 获取已注册的功能组件 </summary>
        public static T GetFeatureComponent<T>(this MonoBehaviour self) where T : GameFeatureComponent
        {
            return GameFeaturesSystem.Runtime.GetFeature<T>();
        }
    }
}

#endif