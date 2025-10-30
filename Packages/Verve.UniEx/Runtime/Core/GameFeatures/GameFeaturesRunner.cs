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
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>游戏功能运行器</para>
    ///   <para>负责遍历已注册模块并调用功能模块其中的子模块的生命周期</para>
    /// </summary>
    [DisallowMultipleComponent, ExecuteAlways, DefaultExecutionOrder(-1000), AddComponentMenu("Verve/Game Features Runner")]
    public sealed class GameFeaturesRunner : ComponentInstanceBase<GameFeaturesRunner>
    {
        [SerializeField, HideInInspector, Tooltip("需启动模块")] private List<GameFeatureModule> m_Modules = new List<GameFeatureModule>();
        [SerializeField, HideInInspector, Tooltip("功能模块管理")] private GameFeatureModuleProfile m_ModuleProfile;
        [SerializeField, HideInInspector, Tooltip("功能组件管理")] private GameFeatureComponentProfile m_ComponentProfile;
        [SerializeField, HideInInspector, Tooltip("是否在运行时跳过依赖检查")] public bool skipRuntimeDependencyChecks;
        
        private IGameFeatureContext m_Context;
        
        /// <summary>
        ///   <para>游戏功能组件管理器</para>
        /// </summary>
        public GameFeatureComponentProfile ComponentProfile => m_ComponentProfile;
        /// <summary>
        ///   <para>游戏功能模块管理器</para>
        /// </summary>
        public GameFeatureModuleProfile ModuleProfile => m_ModuleProfile;

        #region 存储子模块

        private readonly List<IGameFeatureSubmodule> m_AllSubmodules = new List<IGameFeatureSubmodule>();
        private readonly List<ITickableGameFeatureSubmodule> m_ActiveTickableSubmodules = new List<ITickableGameFeatureSubmodule>();
        private readonly List<IDrawableSubmodule> m_ActiveDrawableSubmodules = new List<IDrawableSubmodule>();
        
        private readonly Dictionary<GameFeatureModule, bool> m_ModuleActiveStates = new Dictionary<GameFeatureModule, bool>();
        private readonly Dictionary<IGameFeatureSubmodule, bool> m_SubmoduleEnabledStates = new Dictionary<IGameFeatureSubmodule, bool>();
        
        public IReadOnlyCollection<IGameFeatureSubmodule> AllSubmodules => m_AllSubmodules;

        #endregion

        #region 子模块生命周期委托事件缓存
        
        private delegate void StartupDelegate(IGameFeatureSubmodule submodule);
        private delegate void ShutdownDelegate(IGameFeatureSubmodule submodule);
        private delegate void TickDelegate(ITickableGameFeatureSubmodule tickable, in IGameFeatureContext context);
        private delegate void DrawGUIDelegate(IDrawableSubmodule drawable);
        
        private static readonly StartupDelegate s_StartupCache = static submodule => submodule?.Startup();
        private static readonly ShutdownDelegate s_ShutdownCache = static submodule => submodule?.Shutdown();
        private static readonly TickDelegate s_TickCache = static (ITickableGameFeatureSubmodule submodule, in IGameFeatureContext context) => submodule?.Tick(context);
        private static readonly DrawGUIDelegate s_DrawGUIDelegate = static submodule => submodule?.DrawGUI();
        private static readonly DrawGUIDelegate s_DrawGizmosDelegate = static submodule => submodule?.DrawGizmos();

        #endregion

        #region 数据缓存

        private static readonly Dictionary<Type, Type[]> s_DependencyCache = new Dictionary<Type, Type[]>();
        private static readonly Dictionary<Type, GameFeatureAttribute> s_AttributeCache = new Dictionary<Type, GameFeatureAttribute>();
        private static readonly Dictionary<Type, MethodInfo[]> s_ModuleSubmoduleMethodsCache = new Dictionary<Type, MethodInfo[]>();

        #endregion

        #region 模块事件

        public Action<GameFeatureModule> OnModuleAdded;
        public Action<GameFeatureModule> OnModuleRemoved;

        #endregion

        #region 性能分析
        
        private static readonly CustomSampler s_StartupSampler = CustomSampler.Create("GameFeaturesRunner.Startup");
        private static readonly CustomSampler s_TickSampler = CustomSampler.Create("GameFeaturesRunner.Tick");
        private static readonly CustomSampler s_DrawGUISampler = CustomSampler.Create("GameFeaturesRunner.DrawGUI");
        private static readonly CustomSampler s_DrawGizmosSampler = CustomSampler.Create("GameFeaturesRunner.DrawGizmos");
        private static readonly CustomSampler s_ShutdownSampler = CustomSampler.Create("GameFeaturesRunner.Shutdown");
        private static readonly CustomSampler s_RebuildSampler = CustomSampler.Create("GameFeaturesRunner.Rebuild");
        
        #endregion
        
        private readonly HashSet<GameFeatureModule> m_PendingModulesToAdd = new HashSet<GameFeatureModule>();
        private readonly HashSet<GameFeatureModule> m_PendingModulesToRemove = new HashSet<GameFeatureModule>();
        private Coroutine m_StartupCoroutine;
        private bool m_IsRunning;
        private bool m_NeedsRebuild;
        private int m_LastModuleCount;
        private int m_LastSubmoduleCount;
        
        
        private void Awake()
        {
            m_Modules.RemoveAll(module => module == null);
            m_LastModuleCount = m_Modules.Count;
            
            int estimatedSubmodules = m_Modules.Sum(m => m.Submodules.Count);
            m_AllSubmodules.Capacity = Math.Max(m_AllSubmodules.Capacity, estimatedSubmodules);
            m_ActiveTickableSubmodules.Capacity = Math.Max(m_ActiveTickableSubmodules.Capacity, estimatedSubmodules / 2);
            m_ActiveDrawableSubmodules.Capacity = Math.Max(m_ActiveDrawableSubmodules.Capacity, estimatedSubmodules / 3);
        }
        
        private void OnEnable()
        {
            // Assert.IsNotNull(m_ModuleProfile, "Game feature modules profile is null!");
            // if (m_IsRunning) return;
            if (m_StartupCoroutine != null)
            {
                StopCoroutine(m_StartupCoroutine);
            }
            m_StartupCoroutine = StartCoroutine(StartupAllModules());
            m_IsRunning = true;
        }

        private void Update()
        {
            if (m_ModuleProfile == null || m_ComponentProfile == null) return;
            // Assert.IsNotNull(m_ModuleProfile, "Game feature modules profile is null!");
            m_Context = GameFeatureContext.Default;
            
            if (m_Modules.Count != m_LastModuleCount)
            {
                m_NeedsRebuild = true;
                m_LastModuleCount = m_Modules.Count;
            }
            
            CheckModuleActiveStateChanges();

            if (m_PendingModulesToAdd.Count > 0 || m_PendingModulesToRemove.Count > 0)
            {
                ProcessPendingModules();
            }
            
            if (m_ModuleProfile.isDirty || m_ComponentProfile.isDirty)
            {
                RefreshAllModules();
                m_ModuleProfile.isDirty = false;
                m_ComponentProfile.isDirty = false;
            }
            
            if (m_NeedsRebuild)
            {
                RebuildSubmodules();
                m_NeedsRebuild = false;
            }
            
            TickAllModules();
        }

        private void OnGUI()
        {
            DrawGUIAllModules();
        }

        private void OnDrawGizmos()
        {
            DrawGizmosAllModules();
        }

        private void OnDisable()
        {
            m_IsRunning = false;
            OnModuleAdded = null;
            OnModuleRemoved = null;
            if (m_StartupCoroutine != null)
            {
                StopCoroutine(m_StartupCoroutine);
                m_StartupCoroutine = null;
            }
            ShutdownAllModules();
        }
        
        /// <summary>
        ///   <para>设置配置文件</para>
        /// </summary>
        /// <param name="moduleProfile">模块配置</param>
        /// <param name="componentProfile">组件配置</param>
        public void SetProfiles(GameFeatureModuleProfile moduleProfile, GameFeatureComponentProfile componentProfile)
        {
            m_ModuleProfile = moduleProfile;
            m_ComponentProfile = componentProfile;
            
            if (moduleProfile != null && componentProfile != null)
            {
                RefreshAllModules();
            }
        }
        
        /// <summary>
        ///   <para>处理待添加/移除的模块</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessPendingModules()
        {
            if (m_PendingModulesToRemove.Count > 0)
            {
                foreach (var module in m_PendingModulesToRemove)
                {
                    if (module != null && m_Modules.Contains(module))
                    {
                        m_Modules.Remove(module);
                        ShutdownModule(module);
                        OnModuleRemoved?.Invoke(module);
                    }
                }
                m_PendingModulesToRemove.Clear();
                m_NeedsRebuild = true;
            }
            
            if (m_PendingModulesToAdd.Count > 0)
            {
                AddModulesWithDependencies();
                
                // BUG: 这里暂时移除对依赖关系的处理
                // foreach (var module in m_PendingModulesToAdd)
                // {
                //     if (m_PendingModulesToAdd.Contains(module) && 
                //         !m_Modules.Contains(module))
                //     {
                //         m_Modules.Add(module);
                //         StartupModule(module);
                //         OnModuleAdded?.Invoke(module);
                //     }
                // }

                m_PendingModulesToAdd.Clear();
                m_NeedsRebuild = true;
            }
        }
        
        /// <summary>
        ///   <para>检查模块激活状态变化</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckModuleActiveStateChanges()
        {
            bool needsUpdate = false;
            
            foreach (var module in m_Modules)
            {
                if (module == null) continue;
                
                bool wasActive = m_ModuleActiveStates.TryGetValue(module, out var active) && active;
                bool isActive = module.IsActive;
                
                if (wasActive != isActive)
                {
                    m_ModuleActiveStates[module] = isActive;
                    needsUpdate = true;
                    
                    if (isActive)
                    {
                        StartupModule(module);
                    }
                    else
                    {
                        ShutdownModule(module);
                    }
                }
            }
            
            if (!needsUpdate)
            {
                foreach (var submodule in m_AllSubmodules)
                {
                    if (submodule == null) continue;
                    
                    bool wasEnabled = m_SubmoduleEnabledStates.TryGetValue(submodule, out var enabled) && enabled;
                    bool isEnabled = submodule.IsEnabled;
                    
                    if (wasEnabled != isEnabled)
                    {
                        m_SubmoduleEnabledStates[submodule] = isEnabled;
                        needsUpdate = true;
                        
                        if (isEnabled)
                        {
                            s_StartupCache(submodule);
                        }
                        else
                        {
                            s_ShutdownCache(submodule);
                        }
                    }
                }
            }
            
            if (needsUpdate)
            {
                m_NeedsRebuild = true;
            }
        }

        /// <summary>
        ///   <para>重新构建子模块列表</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RebuildSubmodules()
        {
            s_RebuildSampler.Begin();
            
            m_ActiveTickableSubmodules.Clear();
            m_ActiveDrawableSubmodules.Clear();
            
            foreach (var module in m_Modules)
            {
                if (module != null)
                {
                    m_ModuleActiveStates[module] = module.IsActive;
                }
            }
            
            foreach (var submodule in m_AllSubmodules)
            {
                if (submodule == null) continue;
                
                m_SubmoduleEnabledStates[submodule] = submodule.IsEnabled;
                
                if (submodule.IsEnabled && IsParentModuleActive(submodule))
                {
                    if (submodule is ITickableGameFeatureSubmodule tickable)
                        m_ActiveTickableSubmodules.Add(tickable);
                    
                    if (submodule is IDrawableSubmodule drawable)
                        m_ActiveDrawableSubmodules.Add(drawable);
                }
            }
            
            s_RebuildSampler.End();
        }
        
        /// <summary>
        ///   <para>检查子模块的父模块是否激活</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsParentModuleActive(IGameFeatureSubmodule submodule)
        {
            foreach (var module in m_Modules)
            {
                if (module != null && module.IsActive && module.Submodules.Contains(submodule))
                {
                    return true;
                }
            }
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddModulesWithDependencies()
        {
            if (m_ModuleProfile.Modules == null) return;
            
            foreach (GameFeatureModule module in m_ModuleProfile.Modules)
            {
                if (m_PendingModulesToAdd.Contains(module) && 
                    !m_Modules.Contains(module))
                {
                    AddDependenciesIfNeeded(module);
                    
                    m_Modules.Add(module);
                    StartupModule(module);
                    OnModuleAdded?.Invoke(module);
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddDependenciesIfNeeded(GameFeatureModule module)
        {
            var dependencies = m_ModuleProfile.GetDependencies(module.GetType());
            foreach (var dependencyType in dependencies)
            {
                bool dependencyExists = m_Modules.Any(existingModule => 
                    existingModule != null && existingModule.GetType() == dependencyType);
                
                if (!dependencyExists)
                {
                    var dependencyModule = m_ModuleProfile.Modules.FirstOrDefault(
                        m => m is GameFeatureModule gameModule && 
                             gameModule.GetType() == dependencyType) as GameFeatureModule;
                    
                    if (dependencyModule != null && !m_Modules.Contains(dependencyModule))
                    {
                        AddDependenciesIfNeeded(dependencyModule);
                        m_Modules.Add(dependencyModule);
                        StartupModule(dependencyModule);
                    }
                }
            }
        }

        /// <summary>
        ///   <para>启动所有模块</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerator StartupAllModules()
        {
            s_StartupSampler.Begin();
            
            m_AllSubmodules.Clear();
            m_ActiveTickableSubmodules.Clear();
            m_ActiveDrawableSubmodules.Clear();
            m_ModuleActiveStates.Clear();
            m_SubmoduleEnabledStates.Clear();
            
            var asyncStartups = new List<IEnumerator>();
            
            var initializedModules = new HashSet<Type>();
            var modulesToInitialize = new Queue<GameFeatureModule>();
            
            foreach (var module in m_Modules)
            {
                if (module == null) continue;
                
                m_ModuleActiveStates[module] = module.IsActive;
                
                if (module.IsActive && CheckDependencies(module))
                {
                    modulesToInitialize.Enqueue(module);
                }
            }
            
            while (modulesToInitialize.Count > 0)
            {
                var module = modulesToInitialize.Dequeue();
                var moduleType = module.GetType();
                
                var dependencies = m_ModuleProfile.GetDependencies(moduleType);
                bool allDependenciesInitialized = dependencies.All(initializedModules.Contains);
                
                if (!allDependenciesInitialized)
                {
                    modulesToInitialize.Enqueue(module);
                    continue;
                }
                
                foreach (var submodule in module.Submodules)
                {
                    if (submodule == null || !submodule.IsEnabled || !CheckDependencies(submodule))
                        continue;
                    
                    BindComponentToSubmodule(submodule);
                    
                    if (submodule is GameFeatureSubmodule featureSubmodule)
                    {
                        var startupCoroutine = featureSubmodule.StartupCoroutine();
                        if (startupCoroutine != null)
                        {
                            asyncStartups.Add(startupCoroutine);
                        }
                        else
                        {
                            s_StartupCache(submodule);
                        }
                    }
                    else
                    {
                        s_StartupCache(submodule);
                    }
                    
                    m_AllSubmodules.Add(submodule);
                    m_SubmoduleEnabledStates[submodule] = submodule.IsEnabled;
                    
                    if (submodule is ITickableGameFeatureSubmodule tickable && submodule.IsEnabled)
                        m_ActiveTickableSubmodules.Add(tickable);
                    if (submodule is IDrawableSubmodule drawable && submodule.IsEnabled)
                        m_ActiveDrawableSubmodules.Add(drawable);
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
            
            s_StartupSampler.End();
        }

        /// <summary>
        ///   <para>更新所有模块</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TickAllModules()
        {
            if (m_ActiveTickableSubmodules.Count == 0) return;
            
            s_TickSampler.Begin();
            
            var context = m_Context;
            
            for (int i = 0; i < m_ActiveTickableSubmodules.Count; i++)
            {
                s_TickCache(m_ActiveTickableSubmodules[i], context);
            }
            
            s_TickSampler.End();
        }

        /// <summary>
        ///   <para>绘制所有模块GUI</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawGUIAllModules()
        {
            if (m_ActiveDrawableSubmodules.Count == 0) return;
            
            s_DrawGUISampler.Begin();
            for (int i = 0; i < m_ActiveDrawableSubmodules.Count; i++)
            {
                s_DrawGUIDelegate(m_ActiveDrawableSubmodules[i]);
            }
            s_DrawGUISampler.End();
        }
        
        /// <summary>
        ///   <para>绘制所有模块Gizmos</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawGizmosAllModules()
        {
            if (m_ActiveDrawableSubmodules.Count == 0) return;
            
            s_DrawGizmosSampler.Begin();
            for (int i = 0; i < m_ActiveDrawableSubmodules.Count; i++)
            {
                s_DrawGizmosDelegate(m_ActiveDrawableSubmodules[i]);
            }
            s_DrawGizmosSampler.End();
        }
        
        /// <summary>
        ///   <para>关闭所有模块</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShutdownAllModules()
        {
            if (m_AllSubmodules.Count == 0) return;
            
            s_ShutdownSampler.Begin();
            
            for (int i = m_AllSubmodules.Count - 1; i >= 0; i--)
            {
                var sub = m_AllSubmodules[i];
                if (sub?.IsEnabled == true)
                    s_ShutdownCache(sub);
            }
            
            m_AllSubmodules.Clear();
            m_ActiveTickableSubmodules.Clear();
            m_ActiveDrawableSubmodules.Clear();
            m_ModuleActiveStates.Clear();
            m_SubmoduleEnabledStates.Clear();
            
            s_ShutdownSampler.End();
        }
              
        /// <summary>
        ///   <para>刷新所有模块</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        ///   <para>启动所有模块及其子模块</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartupModule(GameFeatureModule module)
        {
            if (module == null || !module.IsActive || !CheckDependencies(module)) return;
            
            s_StartupSampler.Begin();
            
            var asyncStartups = new List<IEnumerator>();
            
            foreach (var sub in module.Submodules)
            {
                if (sub == null || !sub.IsEnabled || !CheckDependencies(sub))
                    continue;
                
                BindComponentToSubmodule(sub);
                
                if (sub is GameFeatureSubmodule m)
                {
                    var startupCoroutine = m.StartupCoroutine();
                    if (startupCoroutine != null)
                    {
                        asyncStartups.Add(startupCoroutine);
                    }
                    else
                    {
                        s_StartupCache(sub);
                    }
                }
                else
                {
                    s_StartupCache(sub);
                }
                
                m_AllSubmodules.Add(sub);
                m_SubmoduleEnabledStates[sub] = sub.IsEnabled;
                
                if (sub is ITickableGameFeatureSubmodule tickable && sub.IsEnabled)
                    m_ActiveTickableSubmodules.Add(tickable);
                if (sub is IDrawableSubmodule drawable && sub.IsEnabled)
                    m_ActiveDrawableSubmodules.Add(drawable);
            }
            
            if (m_IsRunning && asyncStartups.Count > 0)
            {
                StartCoroutine(WaitForAsyncStartups(asyncStartups));
            }
            
            s_StartupSampler.End();
        }
        
        /// <summary>
        ///   <para>等待所有异步启动完成</para>
        /// </summary>
        /// <param name="asyncStartups">异步启动列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerator WaitForAsyncStartups(List<IEnumerator> asyncStartups)
        {
            foreach (var startup in asyncStartups)
            {
                while (startup.MoveNext())
                {
                    yield return startup.Current;
                }
            }
            
            RebuildSubmodules();
        }

        /// <summary>
        ///   <para>关闭单个模块及其子模块</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShutdownModule(GameFeatureModule module)
        {
            if (module == null) return;
            
            s_ShutdownSampler.Begin();
            
            for (int i = m_AllSubmodules.Count - 1; i >= 0; i--)
            {
                var sub = m_AllSubmodules[i];
                if (sub != null && module.Submodules.Contains(sub))
                {
                    if (sub.IsEnabled)
                    {
                        s_ShutdownCache(sub);
                    }
                    
                    m_AllSubmodules.RemoveAt(i);
                    m_SubmoduleEnabledStates.Remove(sub);
                    
                    if (sub is ITickableGameFeatureSubmodule tickable)
                    {
                        m_ActiveTickableSubmodules.Remove(tickable);
                    }
                    if (sub is IDrawableSubmodule drawable)
                    {
                        m_ActiveDrawableSubmodules.Remove(drawable);
                    }
                }
            }
            
            s_ShutdownSampler.End();
        }

        /// <summary>
        ///   <para>绑定组件到子模块中</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BindComponentToSubmodule(IGameFeatureSubmodule sub)
        {
            if (m_ComponentProfile == null) return;
            
            if (sub is IComponentGameFeatureSubmodule componentSub)
            {
                var componentType = componentSub.ComponentType;
                if (componentType != null && m_ComponentProfile.TryGet(componentType, out var component))
                {
                    componentSub.SetComponent(component);
                }
            }
        }

        /// <summary>
        ///   <para>检查依赖是否满足</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckDependencies(object target)
        {
#if !UNITY_EDITOR
            if (skipRuntimeDependencyChecks) 
                return true;
#endif
            
            Type targetType = target.GetType();
            
            if (!s_AttributeCache.TryGetValue(targetType, out GameFeatureAttribute attr))
            {
                attr = targetType.GetCustomAttribute<GameFeatureAttribute>();
                s_AttributeCache[targetType] = attr;
            }
            
            if (attr == null || attr.Dependencies.Length == 0)
                return true;

            if (!s_DependencyCache.TryGetValue(targetType, out Type[] dependencies))
            {
                dependencies = attr.Dependencies;
                s_DependencyCache[targetType] = dependencies;
            }

            foreach (Type requiredType in dependencies)
            {
                bool found = false;
                
                if (typeof(IGameFeatureModule).IsAssignableFrom(requiredType))
                {
                    foreach (var module in m_Modules)
                    {
                        if (module != null && requiredType.IsInstanceOfType(module) && module.IsActive)
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
        
        /// <summary>
        ///   <para>添加模块</para>
        /// </summary>
        /// <param name="module">模块资源</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddModule(GameFeatureModule module)
        {
            if (module == null || m_Modules.Contains(module)) return;
            // if (!m_ModuleProfile.Has(module)) return;
            
            m_PendingModulesToAdd.Add(module);
            m_PendingModulesToRemove.Remove(module);
        }
        
        /// <summary>
        ///   <para>添加模块</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool AddModule(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath) || m_ModuleProfile == null)
                return false;
            
            if (m_ModuleProfile.MenuPathLookup.TryGetValue(menuPath, out var module))
            {
                AddModule(module);
                return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>移除模块</para>
        /// </summary>
        /// <param name="module">功能模块</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveModule(GameFeatureModule module)
        {
            if (module == null || !m_Modules.Contains(module)) return;
            
            m_PendingModulesToRemove.Add(module);
            m_PendingModulesToAdd.Remove(module);
        }
        
        /// <summary>
        ///   <para>移除模块</para>
        /// </summary>
        /// <param name="menuPath">模块菜单路径</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool RemoveModule(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath)) return false;
            
            if (m_ModuleProfile.MenuPathLookup.TryGetValue(menuPath, out var module))
            {
                RemoveModule(module);
                return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>激活或禁用功能模块</para>
        /// </summary>
        /// <param name="module">功能模块</param>
        /// <param name="isActive">是否激活</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetModuleActive(GameFeatureModule module, bool isActive)
        {
            if (module == null || !m_Modules.Contains(module)) return;
            
            if (module.IsActive != isActive)
            {
                module.IsActive = isActive;
                m_NeedsRebuild = true;
            }
        }

        /// <summary>
        ///   <para>激活或禁用功能模块</para>
        /// </summary>
        /// <param name="menuPath">模块菜单路径</param>
        /// <param name="isActive">是否激活</param>
        /// <returns>
        ///   <para>是否成功</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool SetModuleActive(string menuPath, bool isActive)
        {
            if (string.IsNullOrEmpty(menuPath))
                return false;
            
            if (m_ModuleProfile.MenuPathLookup.TryGetValue(menuPath, out var module))
            {
                SetModuleActive(module, isActive);
                return true;
            }

            return false;
        }
        
        /// <summary>
        ///   <para>获取模块是否激活</para>
        /// </summary>
        /// <param name="module"功能模块></param>
        /// <returns>
        ///   <para>模块是否激活</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool GetModuleActive(GameFeatureModule module)
        {
            if (module == null || !m_Modules.Contains(module)) return false;
            
            return module.IsActive;
        }
        
        /// <summary>
        ///   <para>获取模块是否激活</para>
        /// </summary>
        /// <param name="menuPath">模块菜单路径</param>
        /// <returns>
        ///   <para>模块是否激活</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool GetModuleActive(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return false;
            
            if (m_ModuleProfile.MenuPathLookup.TryGetValue(menuPath, out var module))
            {
                return GetModuleActive(module);
            }

            return false;
        }
    }
}

#endif