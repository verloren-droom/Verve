#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;
    

    /// <summary>
    /// 游戏功能组件配置文件 - 用于管理游戏功能组件，提供添加、移除、获取功能组件等
    /// </summary>
    [Serializable]
    public sealed partial class GameFeatureComponentProfile : ScriptableObject, IGameFeatureComponentProfile
    {
        [SerializeField, HideInInspector, Tooltip("已加载游戏功能组件")] private List<GameFeatureComponent> m_Components = new List<GameFeatureComponent>();
        [NonSerialized, Tooltip("是否为脏")] public bool IsDirty = true;

        public IReadOnlyCollection<IGameFeatureComponent> Components => m_Components.AsReadOnly();

        
        private void OnEnable()
        {
            m_Components.RemoveAll(feature => feature == null);
        }
        
        private void OnDisable()
        {
            if (m_Components == null) return;
            
            for (int i = 0; i < m_Components.Count; i++)
            {
                m_Components[i]?.Release();
            }
        }
        
        public T Add<T>(bool overrides = false)
            where T : GameFeatureComponent
        {
            return (T)Add(typeof(T), overrides);
        }
        
        /// <summary>
        /// 添加组件
        /// </summary>
        /// <param name="type"></param>
        /// <param name="overrides"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public GameFeatureComponent Add(Type type, bool overrides = false)
        {
            if (Has(type))
                throw new InvalidOperationException("Game Feature already exists in the profile");

            var component = CreateInstance(type) as GameFeatureComponent;
#if UNITY_EDITOR
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            component.name = type.Name;
#endif
            m_Components.Add(component);
            IsDirty = true;
            
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(this))
            {
               UnityEditor.AssetDatabase.AddObjectToAsset(component, this);
            }

            if (UnityEditor.EditorUtility.IsPersistent(this))
            {
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
#endif
            return component;
        }
        
        public bool Has<T>() where T : GameFeatureComponent => Has(typeof(T));
        
        /// <summary>
        /// 检查特定组件是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Has(Type type)
        {
            if (!typeof(GameFeatureComponent).IsAssignableFrom(type) || m_Components == null) return false;
            
            for (int i = 0; i < m_Components.Count; i++)
            {
                if (m_Components[i]?.GetType() == type)
                    return true;
            }

            return false;
        }
        
        public void Remove<T>() where T : GameFeatureComponent => Remove(typeof(T));
        
        /// <summary>
        /// 移除特定组件
        /// </summary>
        /// <param name="type"></param>
        public void Remove(Type type)
        {
            if (!typeof(GameFeatureComponent).IsAssignableFrom(type) || m_Components == null) return;

            int toRemove = -1;

            for (int i = 0; i < m_Components.Count; i++)
            {
                if (m_Components[i]?.GetType() == type)
                {
                    toRemove = i;
                    break;
                }
            }

            if (toRemove >= 0)
            {
                var component = m_Components[toRemove];
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (UnityEditor.EditorUtility.IsPersistent(component) && UnityEditor.AssetDatabase.Contains(component))
                    {
                        UnityEditor.AssetDatabase.RemoveObjectFromAsset(component);
                    }

                    UnityEngine.Object.DestroyImmediate(component, true);

                    UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
                m_Components.Remove(component);
            }
        }
        
        public T Get<T>()where T : GameFeatureComponent => (T)Get(typeof(T));
        
        public GameFeatureComponent Get(Type type)
        {
            for (int i = 0; i < m_Components.Count; i++)
            {
                if (m_Components[i]?.GetType() == type)
                    return m_Components[i];
            }

            return null;
        }
        
        public bool TryGet<T>(out T component) where T : GameFeatureComponent
        {
            component = Get<T>();
            return component != null;
        }

        public bool TryGet(Type type, out GameFeatureComponent component)
        {
            component = Get(type);
            return component != null;
        }

        public bool TryGetSubclassOf<T>(Type type, out T component)
            where T : GameFeatureComponent
        {
            component = null;
            for (int i = 0; i < m_Components.Count; i++)
            {
                if (m_Components[i].GetType().IsSubclassOf(type))
                {
                    component = (T)m_Components[i];
                    return true;
                }
            }
            return false;
        }
    }
}

#endif