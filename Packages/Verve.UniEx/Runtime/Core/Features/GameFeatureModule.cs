#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 游戏功能模块基类 - 用于管理创建功能子模块，仅负责创建或注册子模块
    /// </summary>
    [Serializable]
    public class GameFeatureModule : ScriptableObject, IGameFeatureModule
    {
        [SerializeField, Tooltip("功能子模块类型名列表")] private List<string> m_SubmoduleTypeNames = new List<string>();
        [SerializeField, Tooltip("是否激活")] private bool m_IsActive = true;
        [NonSerialized, Tooltip("功能子模块列表")] private List<IGameFeatureSubmodule> m_Submodules;
        [NonSerialized, Tooltip("是否为脏")] public bool isDirty = true;

        public bool IsActive { get => m_IsActive; set => m_IsActive = value; }

        public IReadOnlyCollection<IGameFeatureSubmodule> Submodules
        {
            get
            {
                if (m_Submodules == null || isDirty)
                {
                    RebuildSubmodules();
                }
                return m_Submodules.AsReadOnly();
            }
        }

        
        protected virtual void OnEnable()
        {
            m_SubmoduleTypeNames.RemoveAll(string.IsNullOrEmpty);
        }
        
        protected virtual void OnDisable()
        {
            m_Submodules?.Clear();
        }

        /// <summary>
        /// 重新构建子模块实例列表
        /// </summary>
        private void RebuildSubmodules()
        {
            if (m_Submodules == null)
                m_Submodules = new List<IGameFeatureSubmodule>(m_SubmoduleTypeNames.Count);
            else
                m_Submodules.Clear();
                        
            m_Submodules.Capacity = Math.Max(m_Submodules.Capacity, m_SubmoduleTypeNames.Count);

            for (int i = 0; i < m_SubmoduleTypeNames.Count; i++)
            {
                var typeName = m_SubmoduleTypeNames[i];
                var type = Type.GetType(typeName);
                if (type != null && typeof(IGameFeatureSubmodule).IsAssignableFrom(type))
                {
                    var submodule = (IGameFeatureSubmodule)Activator.CreateInstance(type);
                    m_Submodules.Add(submodule);
                }
            }
            isDirty = false;
        }

        /// <summary>
        /// 添加子模块
        /// </summary>
        public virtual void Add(Type type, bool overrides = false)
        {
            if (type == null || !typeof(IGameFeatureSubmodule).IsAssignableFrom(type))
                return;
            
            string typeName = type.AssemblyQualifiedName;
            
            if (Has(type) && !overrides) 
                return;
            
            if (overrides)
            {
                Remove(type);
            }
        
            m_SubmoduleTypeNames.Add(typeName);
            isDirty = true;
        }

        public void Add<T>(bool overrides = false)
            where T : class, IGameFeatureSubmodule
            => Add(typeof(T), overrides);

        /// <summary>
        /// 移除子模块
        /// </summary>
        public virtual bool Remove(System.Type type)
        {
            if (type == null || !typeof(IGameFeatureSubmodule).IsAssignableFrom(type))
                return false;
            
            string typeName = type.AssemblyQualifiedName;
        
            var removed = m_SubmoduleTypeNames.Remove(typeName);
            if (removed)
            {
                isDirty = true;
            }
        
            return removed;
        }

        public bool Remove<T>()
            where T : class, IGameFeatureSubmodule
            => Remove(typeof(T));

        /// <summary>
        /// 获取指定类型的子模块
        /// </summary>
        public IGameFeatureSubmodule Get(System.Type type)
        {
            if (type == null || !typeof(IGameFeatureSubmodule).IsAssignableFrom(type))
                return null;

            if (Has(type))
            {
                if (m_Submodules != null)
                {
                    for (int i = 0; i < m_Submodules.Count; i++)
                    {
                        if (m_Submodules[i]?.GetType() == type)
                            return m_Submodules[i];
                    }
                }
                
                return (IGameFeatureSubmodule)Activator.CreateInstance(type);
            }

            return null;
        }

        public T Get<T>()
            where T : class, IGameFeatureSubmodule
            => (T)Get(typeof(T));
        
        public bool Has(Type type)
        {
            if (type == null || !typeof(IGameFeatureSubmodule).IsAssignableFrom(type))
                return false;

            return m_SubmoduleTypeNames.Contains(type.AssemblyQualifiedName);
        }

        /// <summary>
        /// 检查是否有指定类型的子模块
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool Has<T>()
            where T : IGameFeatureSubmodule
            => Has(typeof(T));

        public void Clear()
        {
            m_SubmoduleTypeNames?.Clear();
            m_Submodules?.Clear();
            isDirty = true;
        }

        public void Dispose()
        {
            Dispose(true);
            Clear();
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}

#endif