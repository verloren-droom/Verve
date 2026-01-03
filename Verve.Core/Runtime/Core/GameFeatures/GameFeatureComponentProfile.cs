#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>游戏功能组件配置文件</para>
    ///   <para>用于管理游戏功能组件，提供添加、移除、获取功能组件等</para>
    /// </summary>
    [Serializable]
    public sealed partial class GameFeatureComponentProfile : ScriptableObject, IGameFeatureComponentProfile
    {
        [SerializeField, HideInInspector, Tooltip("已加载游戏功能组件")] private List<GameFeatureComponent> m_Components = new List<GameFeatureComponent>();
        [NonSerialized, Tooltip("是否为脏")] public bool isDirty = true;

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
        
        /// <summary>
        ///   <para>添加组件</para>
        /// </summary>
        /// <param name="overrides">是否覆盖</param>
        /// <typeparam name="T">功能组件</typeparam>
        /// <returns>
        ///   <para>添加的组件实例</para>
        /// </returns>
        public T Add<T>(bool overrides = false)
            where T : GameFeatureComponent
            =>(T)Add(typeof(T), overrides);

        /// <summary>
        ///   <para>添加组件</para>
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <param name="overrides">是否覆盖</param>
        public void Add(GameFeatureComponent component, bool overrides = false)
        {
            if (component == null || (Has(component.GetType()) && !overrides))
                throw new InvalidOperationException("Game Feature already exists in the profile");
            
            if (overrides && Has(component.GetType()))
            {
                Remove(component.GetType());
            }
            
            m_Components.Add(component);
            isDirty = true;
        }
        
        /// <summary>
        ///   <para>添加组件</para>
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <param name="overrides">是否覆盖</param>
        /// <returns>
        ///   <para>添加的组件实例</para>
        /// </returns>
        public GameFeatureComponent Add(Type type, bool overrides = false)
        {
            if (Has(type) && !overrides)
                throw new InvalidOperationException("Game Feature already exists in the profile");

            if (overrides && Has(type))
            {
                Remove(type);
            }
            
            var component = CreateInstance(type) as GameFeatureComponent;
#if UNITY_EDITOR
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            component.name = type.Name;
#endif
            m_Components.Add(component);
            isDirty = true;
            
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
        
        /// <summary>
        ///   <para>检查特定组件是否存在</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>
        ///   <para>是否存在</para>
        /// </returns>
        public bool Has<T>() where T : GameFeatureComponent => Has(typeof(T));
        
        /// <summary>
        ///   <para>检查特定组件是否存在</para>
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <returns>
        ///   <para>是否存在</para>
        /// </returns>
        public bool Has(Type type)
        {
            if (!typeof(GameFeatureComponent).IsAssignableFrom(type) || m_Components == null || type == null) return false;
            
            for (int i = 0; i < m_Components.Count; i++)
            {
                if (m_Components[i]?.GetType() == type)
                    return true;
            }

            return false;
        }
        
        /// <summary>
        ///   <para>移除特定组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void Remove<T>() where T : GameFeatureComponent => Remove(typeof(T));
        
        /// <summary>
        ///   <para>移除特定组件</para>
        /// </summary>
        /// <param name="type">组件类型</param>
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
        
        /// <summary>
        ///   <para>获取特定组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>
        ///   <para>组件实例</para>
        /// </returns>
        public T Get<T>()where T : GameFeatureComponent => (T)Get(typeof(T));
        
        /// <summary>
        ///   <para>获取特定组件</para>
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <returns>
        ///   <para>组件实例</para>
        /// </returns>
        public GameFeatureComponent Get(Type type)
        {
            for (int i = 0; i < m_Components.Count; i++)
            {
                if (m_Components[i]?.GetType() == type)
                    return m_Components[i];
            }

            return null;
        }
        
        /// <summary>
        ///   <para>尝试获取特定组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件实例</param>
        /// <returns>
        ///   <para>是否存在</para>
        /// </returns>
        public bool TryGet<T>(out T component) where T : GameFeatureComponent
        {
            component = Get<T>();
            return component != null;
        }

        /// <summary>
        ///   <para>尝试获取特定组件</para>
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <param name="component">组件实例</param>
        /// <returns>
        ///   <para>是否存在</para>
        /// </returns>
        public bool TryGet(Type type, out GameFeatureComponent component)
        {
            component = Get(type);
            return component != null;
        }

        /// <summary>
        ///   <para>尝试获取特定组件的子类</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件实例</param>
        /// <param name="type">组件类型</param>
        /// <returns>
        ///   <para>是否存在</para>
        /// </returns>
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