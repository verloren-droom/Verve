#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Reflection;
    using System.Collections;
    using UnityEngine.Profiling;
    using UnityEngine.Assertions;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 游戏功能运行器 - 负责遍历已注册模块并调用功能模块其中的子模块的生命周期
    /// </summary>
    [DisallowMultipleComponent, ExecuteAlways, DefaultExecutionOrder(-1000), AddComponentMenu("Verve/Game Features Runner")]
    public sealed class GameFeaturesRunner : ComponentInstanceBase<GameFeaturesRunner>
    {
        [SerializeField, Tooltip("需启动模块")] private List<GameFeatureModule> m_Modules = new List<GameFeatureModule>();
        [NonSerialized, Tooltip("菜单路径对照表")] private readonly Dictionary<string, GameFeatureModule> m_MenuPathLookup = new Dictionary<string, GameFeatureModule>();

        [Tooltip("功能模块管理")] public GameFeatureModuleProfile ModuleProfile;
        [Tooltip("功能组件管理")] public GameFeatureComponentProfile ComponentProfile;
        
        private IGameFeatureContext m_Context;

        #region 存储子模块

        private IGameFeatureSubmodule[] m_AllSubmodules = Array.Empty<IGameFeatureSubmodule>();
        private ITickableGameFeatureSubmodule[] m_TickableSubmodules = Array.Empty<ITickableGameFeatureSubmodule>();
        
        public IReadOnlyCollection<IGameFeatureSubmodule> AllSubmodules => m_AllSubmodules;

        #endregion

        #region 模块生命周期委托事件缓存
        
        private delegate void StartupDelegate(IGameFeatureSubmodule submodule);
        private delegate void ShutdownDelegate(IGameFeatureSubmodule submodule);
        private delegate void TickDelegate(ITickableGameFeatureSubmodule tickable, in IGameFeatureContext context);
        
        private static readonly StartupDelegate s_StartupCache = static submodule => submodule?.Startup();
        private static readonly ShutdownDelegate s_ShutdownCache = static submodule => submodule?.Shutdown();
        private static readonly TickDelegate s_TickCache = static (ITickableGameFeatureSubmodule submodule, in IGameFeatureContext context) => submodule?.Tick(context);

        #endregion

        private Coroutine m_StartupCoroutine;
        private bool m_IsRunning;
        private readonly HashSet<GameFeatureModule> m_PendingModulesToAdd = new HashSet<GameFeatureModule>();
        private readonly HashSet<GameFeatureModule> m_PendingModulesToRemove = new HashSet<GameFeatureModule>();
        
        public Action<GameFeatureModule> OnModuleAdded;
        public Action<GameFeatureModule> OnModuleRemoved;

        private void Awake()
        {
            Assert.IsNotNull(ModuleProfile, "Game feature modules data is null.");
            // m_Modules = ModuleProfile.Modules.OfType<GameFeatureModule>().ToList();
            m_Modules.RemoveAll(module => module == null);
        }
        
        private void OnEnable()
        {
            if (m_StartupCoroutine != null)
            {
                StopCoroutine(m_StartupCoroutine);
            }
            m_StartupCoroutine = StartCoroutine(StartupAllModules());
            m_IsRunning = true;
        }

        private void Update()
        {
            m_Context = GameFeatureContext.Default;
            
            if (m_PendingModulesToAdd.Count > 0)
            {
                foreach (var module in m_PendingModulesToAdd)
                {
                    if (module != null && !m_Modules.Contains(module) && ModuleProfile.Has(module))
                    {
                        m_Modules.Add(module);
                        StartupModule(module);
                    }
                }
                m_PendingModulesToAdd.Clear();
            }
            
            if (m_PendingModulesToRemove.Count > 0)
            {
                foreach (var module in m_PendingModulesToRemove)
                {
                    if (module != null && m_Modules.Contains(module))
                    {
                        m_Modules.Remove(module);
                        ShutdownModule(module);
                    }
                }
                m_PendingModulesToRemove.Clear();
            }
            
            if (ModuleProfile.IsDirty || ComponentProfile.IsDirty)
            {
                RefreshAllModules();
                ModuleProfile.IsDirty = false;
                ComponentProfile.IsDirty = false;
            }
            
            TickAllModules();
        }

        private void OnDisable()
        {
            m_IsRunning = false;
            OnModuleAdded = null;
            OnModuleRemoved = null;
            ShutdownAllModules();
        }

        private IEnumerator StartupAllModules()
        {
            Profiler.BeginSample("GameFeaturesRunner.StartupAllModules");
            
            var tickableList = new List<ITickableGameFeatureSubmodule>();
            var asyncStartups = new List<IEnumerator>();
            var allSubmodulesList = new List<IGameFeatureSubmodule>();
            
            var initializedModules = new HashSet<Type>();
            var modulesToInitialize = new Queue<IGameFeatureModule>();
            
            foreach (var module in m_Modules)
            {
                if (module.IsActive && CheckDependencies(module))
                {
                    modulesToInitialize.Enqueue(module);
                }
            }
            
            while (modulesToInitialize.Count > 0)
            {
                var module = modulesToInitialize.Dequeue();
                var moduleType = module.GetType();
                
                var dependencies = module.GetDependencies();
                bool allDependenciesInitialized = true;
                
                foreach (var dep in dependencies)
                {
                    if (!initializedModules.Contains(dep))
                    {
                        allDependenciesInitialized = false;
                        break;
                    }
                }
                
                if (!allDependenciesInitialized)
                {
                    modulesToInitialize.Enqueue(module);
                    continue;
                }
                
                foreach (var sub in module.Submodules)
                {
                    if (!sub.IsEnabled || !CheckDependencies(sub))
                        continue;
                    
                    BindComponentToSubmodule(sub);
                    if (sub is GameFeatureSubmodule featureSubmodule)
                    {
                        var startupCoroutine = featureSubmodule.StartupCoroutine();
                        asyncStartups.Add(startupCoroutine);
                    }
                    else
                    {
                        s_StartupCache(sub);
                    }
                    
                    allSubmodulesList.Add(sub);
                    
                    if (sub is ITickableGameFeatureSubmodule tickable)
                        tickableList.Add(tickable);
                }
                
                initializedModules.Add(moduleType);
            }
            
            foreach (var startup in asyncStartups)
            {
                while (startup.MoveNext())
                {
                    yield return startup.Current;
                }
            }
            
            m_AllSubmodules = allSubmodulesList.ToArray();
            m_TickableSubmodules = tickableList.ToArray();
            
            Profiler.EndSample();
        }

        private void TickAllModules()
        {
            if (m_TickableSubmodules.Length == 0) return;
            
            Profiler.BeginSample("GameFeaturesRunner.TickAllModules");
            
            var context = m_Context;
            
            for (int i = 0; i < m_TickableSubmodules.Length; i++)
            {
                if (m_TickableSubmodules[i].IsEnabled)
                    s_TickCache(m_TickableSubmodules[i], context);
            }
            
            Profiler.EndSample();
        }

        private void ShutdownAllModules()
        {
            if (m_AllSubmodules.Length == 0) return;
            
            Profiler.BeginSample("GameFeaturesRunner.ShutdownAllModules");
            
            for (int i = m_AllSubmodules.Length - 1; i >= 0; i--)
            {
                var sub = m_AllSubmodules[i];
                if (sub?.IsEnabled == true)
                    s_ShutdownCache(sub);
            }
            
            m_TickableSubmodules = Array.Empty<ITickableGameFeatureSubmodule>();
            m_AllSubmodules = Array.Empty<IGameFeatureSubmodule>();
            
            Profiler.EndSample();
        }
              
        /// <summary>
        /// 刷新所有模块
        /// </summary>
        private void RefreshAllModules()
        {
            ShutdownAllModules();
            if (m_StartupCoroutine != null)
            {
                StopCoroutine(m_StartupCoroutine);
            }
            m_StartupCoroutine = StartCoroutine(StartupAllModules());
        }

        /// <summary>
        /// 启动单个模块及其子模块
        /// </summary>
        private void StartupModule(GameFeatureModule module)
        {
            if (module == null || !module.IsActive || !CheckDependencies(module)) return;
            
            Profiler.BeginSample("GameFeaturesRunner.StartupModule");
            
            var allSubmodulesList = m_AllSubmodules.ToList();
            var tickableList = m_TickableSubmodules.ToList();
            var asyncStartups = new List<IEnumerator>();
            
            foreach (var sub in module.Submodules)
            {
                if (!sub.IsEnabled || !CheckDependencies(sub))
                    continue;
                
                BindComponentToSubmodule(sub);
                if (sub is GameFeatureSubmodule m)
                {
                    var startupCoroutine = m.StartupCoroutine();
                    asyncStartups.Add(startupCoroutine);
                }
                else
                {
                    s_StartupCache(sub);
                }
                
                allSubmodulesList.Add(sub);
                
                if (sub is ITickableGameFeatureSubmodule tickable)
                    tickableList.Add(tickable);
            }
            
            if (m_IsRunning && asyncStartups.Count > 0)
            {
                StartCoroutine(WaitForAsyncStartups(asyncStartups, allSubmodulesList, tickableList));
            }
            else
            {
                m_AllSubmodules = allSubmodulesList.ToArray();
                m_TickableSubmodules = tickableList.ToArray();
            }
            
            Profiler.EndSample();
        }
        
        private IEnumerator WaitForAsyncStartups(List<IEnumerator> asyncStartups, List<IGameFeatureSubmodule> allSubmodulesList, List<ITickableGameFeatureSubmodule> tickableList)
        {
            foreach (var startup in asyncStartups)
            {
                while (startup.MoveNext())
                {
                    yield return startup.Current;
                }
            }
            
            m_AllSubmodules = allSubmodulesList.ToArray();
            m_TickableSubmodules = tickableList.ToArray();
        }

        /// <summary>
        /// 关闭单个模块及其子模块
        /// </summary>
        private void ShutdownModule(GameFeatureModule module)
        {
            if (module == null) return;
            
            Profiler.BeginSample("GameFeaturesRunner.ShutdownModule");
            
            var allSubmodulesList = m_AllSubmodules.ToList();
            var tickableList = m_TickableSubmodules.ToList();
            
            foreach (var sub in module.Submodules)
            {
                if (sub?.IsEnabled == true)
                {
                    s_ShutdownCache(sub);
                    
                    allSubmodulesList.Remove(sub);
                    
                    if (sub is ITickableGameFeatureSubmodule tickable)
                    {
                        tickableList.Remove(tickable);
                    }
                }
            }
            
            m_AllSubmodules = allSubmodulesList.ToArray();
            m_TickableSubmodules = tickableList.ToArray();
            
            Profiler.EndSample();
        }

        /// <summary>
        /// 绑定组件到子模块中
        /// </summary>
        private void BindComponentToSubmodule(IGameFeatureSubmodule sub)
        {
            if (ComponentProfile == null) return;
            
            if (sub is IComponentGameFeatureSubmodule componentSub)
            {
                var componentType = componentSub.ComponentType;
                if (componentType != null && ComponentProfile.TryGet(componentType, out var component))
                {
                    componentSub.SetComponent(component);
                }
            }
        }

        /// <summary>
        /// 检查依赖是否满足
        /// </summary>
        private bool CheckDependencies(object target)
        {
#if !UNITY_EDITOR
            if (GameFeaturesSettings.GetOrCreateSettings().SkipRuntimeDependencyChecks) 
                return true;
#endif
            
            var attr = target.GetType().GetCustomAttribute<GameFeatureAttribute>();
            if (attr == null || attr.Dependencies.Length == 0)
                return true;

            foreach (Type requiredType in attr.Dependencies)
            {
                bool found = false;
                
                if (typeof(IGameFeatureModule).IsAssignableFrom(requiredType))
                {
                    foreach (var module in m_Modules)
                    {
                        if (requiredType.IsInstanceOfType(module) && module.IsActive)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                
                if (!found)
                {
                    Debug.LogWarning($"{target.GetType()} requires {requiredType} which is not active or not found.");
                    return false;
                }
            }
            return true;
        }
        
        private void AddModule(GameFeatureModule module)
        {
            if (module == null || m_Modules.Contains(module) || !ModuleProfile.Has(module)) return;
            
            m_PendingModulesToAdd.Add(module);
            m_PendingModulesToRemove.Remove(module);
            OnModuleAdded?.Invoke(module);
        }
        
        /// <summary>
        /// 添加模块
        /// </summary>
        internal bool AddModule(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath) || ModuleProfile == null)
                return false;
            

            if (ModuleProfile.MenuPathLookup.TryGetValue(menuPath, out var module))
            {
                AddModule(module);
                return true;
            }

            return false;
        }

        private void RemoveModule(GameFeatureModule module)
        {
            if (module == null || !m_Modules.Contains(module)) return;
            
            m_PendingModulesToRemove.Add(module);
            m_PendingModulesToAdd.Remove(module);
            OnModuleRemoved?.Invoke(module);
        }
        
        /// <summary>
        /// 移除模块
        /// </summary>
        internal bool RemoveModule(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return false;
            
            if (ModuleProfile.MenuPathLookup.TryGetValue(menuPath, out var module))
            {
                RemoveModule(module);
                return true;
            }

            return false;
        }

        private void SetModuleActive(GameFeatureModule module, bool isActive)
        {
            if (module == null || !m_Modules.Contains(module)) return;
            
            module.IsActive = isActive;
        }

        /// <summary>
        /// 激活或禁用功能模块
        /// </summary>
        internal bool SetModuleActive(string menuPath, bool isActive)
        {
            if (string.IsNullOrEmpty(menuPath))
                return false;
            
            if (ModuleProfile.MenuPathLookup.TryGetValue(menuPath, out var module))
            {
                SetModuleActive(module, isActive);
                return true;
            }

            return false;
        }
        
        private bool GetModuleActive(GameFeatureModule module)
        {
            if (module == null || !m_Modules.Contains(module)) return false;
            
            return module.IsActive;
        }
        
        /// <summary>
        /// 获取模块是否激活
        /// </summary>
        internal bool GetModuleActive(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return false;
            
            if (ModuleProfile.MenuPathLookup.TryGetValue(menuPath, out var module))
            {
                return GetModuleActive(module);
            }

            return false;
        }
    }
}

#endif