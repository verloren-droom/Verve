#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using Verve;
    using System.Linq;
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    
    
    public static class GameFeatureExtension
    {
        private static readonly Dictionary<string, GameObject> m_FeatureInstances = new Dictionary<string, GameObject>();


        public static T CreateFeatureInstance<T>(this IGameFeatureData self, GameObject prefab = null) 
            where T : GameFeatureComponent
        {
            if (m_FeatureInstances.TryGetValue(self.FeatureName, out var existing))
            {
                if (existing != null)
                {
                    return existing.GetComponent<T>();
                }
                m_FeatureInstances.Remove(self.FeatureName);
            }

            GameObject instance = prefab != null ? GameObject.Instantiate(prefab) : new GameObject($"[Feature] {self.FeatureName}");

            if (self is GameFeatureAsset asset && asset.IsPersistent)
            {
                GameObject.DontDestroyOnLoad(instance);
            }
            // SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());

            var component = instance.GetComponent<T>() ?? instance.AddComponent<T>();
            m_FeatureInstances[self.FeatureName] = instance;
            return component;
        }
    }
}

#endif