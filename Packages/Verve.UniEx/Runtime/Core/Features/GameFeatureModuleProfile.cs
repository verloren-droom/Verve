#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 游戏功能模块配置文件 - 管理所有游戏功能模块的注册和注销
    /// </summary>
    [Serializable]
    public sealed partial class GameFeatureModuleProfile : ScriptableObject, IGameFeatureModuleProfile
    {
        [SerializeField, HideInInspector, Tooltip("模块列表")] private List<GameFeatureModule> m_Modules = new List<GameFeatureModule>();
        [NonSerialized, Tooltip("菜单路径对照表")] private readonly Dictionary<string, GameFeatureModule> m_MenuPathLookup = new Dictionary<string, GameFeatureModule>();
        [NonSerialized, Tooltip("是否为脏")] public bool IsDirty = true;

        public IReadOnlyCollection<IGameFeatureModule> Modules => m_Modules.AsReadOnly();
        /// <summary> 菜单路径对照表 </summary>
        public IReadOnlyDictionary<string, GameFeatureModule> MenuPathLookup => m_MenuPathLookup;


        private void OnEnable()
        {
            m_Modules.RemoveAll(module => module == null);
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
        /// 添加游戏功能模块
        /// </summary>
        /// <param name="module">要添加的游戏功能模块实例</param>
        public void Add(GameFeatureModule module)
        {
            if (module == null || m_Modules.Contains(module)) return;
            ValidateDependencies(module);
            InsertModuleWithDependencies(module);
            string menuPath = GetMenuPathForModule(module);
            if (!string.IsNullOrEmpty(menuPath) && m_MenuPathLookup != null)
            {
                m_MenuPathLookup[menuPath] = module;
            }
            IsDirty = true;
        }

        /// <summary>
        /// 移除游戏功能模块
        /// </summary>
        /// <param name="module">要移除的游戏功能模块实例</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(GameFeatureModule module)
        {
            if (module == null) return false;

            CheckModuleDependents(module);
            
            string menuPath = GetMenuPathForModule(module);
            if (!string.IsNullOrEmpty(menuPath) && m_MenuPathLookup != null)
            {
                m_MenuPathLookup.Remove(menuPath);
            }
            
            IsDirty = true;
            bool removed = m_Modules.Remove(module);
            
#if UNITY_EDITOR
            if (removed && !Application.isPlaying)
            {
                if (UnityEditor.EditorUtility.IsPersistent(module) && UnityEditor.AssetDatabase.Contains(module))
                {
                    UnityEditor.AssetDatabase.RemoveObjectFromAsset(module);
                }

                UnityEngine.Object.DestroyImmediate(module, true);

                UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
            
            return removed;
        }

        /// <summary>
        /// 检查是否包含指定的模块
        /// </summary>
        /// <param name="module">要检查的模块实例</param>
        /// <returns>是否包含</returns>
        public bool Has(GameFeatureModule module)
        {
            return m_Modules.Contains(module);
        }

        /// <summary>
        /// 检查是否包含指定类型的模块
        /// </summary>
        /// <param name="type">要检查的模块类型</param>
        /// <returns>是否包含</returns>
        public bool Has(Type type)
        {
            if (type == null || !typeof(GameFeatureModule).IsAssignableFrom(type))
                return false;
            
            return m_Modules.Any(m => m?.GetType() == type);
        }

        private void ValidateDependencies(GameFeatureModule module)
        {
            var visited = new HashSet<Type>();
            var stack = new Stack<Type>();
            
            CheckDependenciesRecursive(module.GetType(), visited, stack);
        }

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
        /// 插入模块
        /// </summary>
        /// <param name="module"></param>
        private void InsertModuleWithDependencies(GameFeatureModule module)
        {
            var dependencies = module.GetDependencies().ToList();
            
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

        private void CheckModuleDependents(GameFeatureModule module)
        {
            var moduleType = module.GetType();

            foreach (var m in m_Modules)
            {
                if (m == null) continue;
                
                var dependencies = m.GetDependencies();
                if (dependencies.Contains(moduleType))
                {
                    throw new InvalidOperationException($"Cannot unregister module {module.name} because it is required by {m.name}");
                }
            }
        }

        public IEnumerable<Type> GetDependencies(Type moduleType)
        {
            var attr = moduleType.GetCustomAttribute<GameFeatureAttribute>();
            if (attr == null)
                return Enumerable.Empty<Type>();
        
            return attr.Dependencies ?? Enumerable.Empty<Type>();
        }
        
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