#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>游戏功能模块配置文件</para>
    ///   <para>用于管理所有游戏功能模块的注册和注销</para>
    /// </summary>
    [Serializable]
    public sealed partial class GameFeatureModuleProfile : ScriptableObject, IGameFeatureModuleProfile
    {
        [SerializeField, HideInInspector, Tooltip("模块列表")] private List<GameFeatureModule> m_Modules = new List<GameFeatureModule>();
        [NonSerialized, Tooltip("菜单路径对照表")] private readonly Dictionary<string, GameFeatureModule> m_MenuPathLookup = new Dictionary<string, GameFeatureModule>();
        [NonSerialized, Tooltip("是否为脏")] public bool isDirty = true;

        public IReadOnlyCollection<IGameFeatureModule> Modules => m_Modules.AsReadOnly();
        /// <summary>
        ///   <para>菜单路径和功能模块对照表</para>
        /// </summary>
        public IReadOnlyDictionary<string, GameFeatureModule> MenuPathLookup => m_MenuPathLookup;


        private void OnEnable()
        {
            m_Modules.RemoveAll(module => module == null);
            RebuildMenuPathLookup();
        }
        
        /// <summary>
        ///   <para>重建菜单路径和功能模块对照表</para>
        /// </summary>
        private void RebuildMenuPathLookup()
        {
            m_MenuPathLookup.Clear();
            for (int i = 0; i < m_Modules.Count; i++)
            {
                var module = m_Modules[i];
                if (module == null) continue;
            
                string menuPath = GetMenuPathForModule(module);
                if (!string.IsNullOrEmpty(menuPath) && !m_MenuPathLookup.ContainsKey(menuPath))
                {
                    m_MenuPathLookup.Add(menuPath, module);
                }
            }
        }

        /// <summary>
        ///   <para>添加游戏功能模块</para>
        /// </summary>
        /// <param name="module">游戏功能模块实例</param>
        /// <param name="overrides">是否覆盖模块</param>
        public void Add(GameFeatureModule module, bool overrides = false)
        {
            if (module == null || (!overrides && Has(module))) return;
            if (overrides && Has(module))
            {
                Remove(module);
            }
            ValidateDependencies(module);
            InsertModuleWithDependencies(module);
            string menuPath = GetMenuPathForModule(module);
            if (!string.IsNullOrEmpty(menuPath))
            {
                if (m_MenuPathLookup.ContainsKey(menuPath))
                {
                    m_MenuPathLookup.Remove(menuPath);
                }
                m_MenuPathLookup[menuPath] = module;
            }
            isDirty = true;
        }

        /// <summary>
        ///   <para>移除游戏功能模块</para>
        /// </summary>
        /// <param name="module">游戏功能模块实例</param>
        /// <returns>
        ///   <para>是否成功移除</para>
        /// </returns>
        public bool Remove(GameFeatureModule module)
        {
            if (module == null) return false;

            CheckModuleDependents(module);
            
            string menuPath = GetMenuPathForModule(module);
            if (!string.IsNullOrEmpty(menuPath))
            {
                m_MenuPathLookup.Remove(menuPath);
            }
            
            isDirty = true;
            bool removed = m_Modules.Remove(module);

            return removed;
        }

        /// <summary>
        ///   <para>检查是否包含指定的模块</para>
        /// </summary>
        /// <param name="module">要检查的模块实例</param>
        /// <returns>
        ///   <para>是否包含</para>
        /// </returns>
        public bool Has(GameFeatureModule module)
        {
            return m_Modules.Contains(module);
        }

        /// <summary>
        ///   <para>检查是否包含指定类型的模块</para>
        /// </summary>
        /// <param name="type">功能模块类型</param>
        /// <returns>
        ///   <para>是否包含</para>
        /// </returns>
        public bool Has(Type type)
        {
            if (type == null || m_Modules == null || !typeof(GameFeatureModule).IsAssignableFrom(type)) return false;
            return m_Modules.Any(m => m?.GetType() == type);
        }

        /// <summary>
        ///   <para>验证模块的依赖关系</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateDependencies(GameFeatureModule module)
        {
            var visited = new HashSet<Type>();
            var stack = new Stack<Type>();
            
            CheckDependenciesRecursive(module.GetType(), visited, stack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDependenciesRecursive(Type moduleType, 
            ISet<Type> visited, Stack<Type> stack)
        {
            if (!visited.Add(moduleType))
            {
                if (stack.Contains(moduleType))
                {
                    throw new InvalidOperationException($"Circular dependency detected: {string.Join(" -> ", stack)}");
                }
                return;
            }

            stack.Push(moduleType);
            
            var dependencies = GetDependencies(moduleType);
            foreach (var dep in dependencies)
            {
                CheckDependenciesRecursive(dep, visited, stack);
            }
            
            stack.Pop();
        }

        /// <summary>
        ///   <para>插入模块</para>
        /// </summary>
        /// <param name="module">功能模块</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InsertModuleWithDependencies(GameFeatureModule module)
        {
            var dependencies = GetDependencies(module.GetType()).ToList();
            
            int insertIndex = 0;
            for (int i = 0; i < m_Modules.Count; i++)
            {
                var currentType = m_Modules[i].GetType();
                
                if (dependencies.Contains(currentType))
                {
                    insertIndex = i + 1;
                }
                
                if (dependencies.Count == 0 || !dependencies.Any(d => m_Modules.Any(m => m.GetType() == d)))
                {
                    break;
                }
            }
            
            m_Modules.Insert(insertIndex, module);
        }

        /// <summary>
        ///   <para>检查模块的依赖关系</para>
        /// </summary>
        /// <param name="module"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal void CheckModuleDependents(GameFeatureModule module)
        {
            var moduleType = module.GetType();

            foreach (var m in m_Modules)
            {
                if (m == null) continue;
                
                var dependencies = GetDependencies(moduleType);
                if (dependencies.Contains(moduleType))
                {
                    throw new InvalidOperationException($"Cannot unregister module {module.name} because it is required by {m.name}");
                }
            }
        }

        public IEnumerable<Type> GetDependencies(Type moduleType)
        {
            return moduleType?.GetCustomAttribute<GameFeatureAttribute>()?.Dependencies ?? Enumerable.Empty<Type>();
        }
        
        /// <summary>
        ///   <para>获取模块的菜单路径</para>
        /// </summary>
        /// <param name="module">功能模块</param>
        /// <returns>
        ///   <para>菜单路径</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetMenuPathForModule(GameFeatureModule module)
        {
            var attr = module.GetType().GetCustomAttribute<GameFeatureAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.MenuPath))
                return attr.MenuPath;
            
            if (module.Submodules != null && module.Submodules.Count > 0)
            {
                var firstSubmodule = module.Submodules.First();
                var submoduleAttr = firstSubmodule.GetType().GetCustomAttribute<GameFeatureSubmoduleAttribute>();
                if (submoduleAttr != null && !string.IsNullOrEmpty(submoduleAttr.MenuPath))
                    return submoduleAttr.MenuPath;
            }
        
            return null;
        }
    }
}

#endif