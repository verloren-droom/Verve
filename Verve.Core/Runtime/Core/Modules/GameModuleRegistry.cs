namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    ///   <para>模块注册表。</para>
    /// </summary>
    internal sealed class GameModuleRegistry
    {
        /// <summary>
        ///   <para>可赋值查询的解析状态。</para>
        /// </summary>
        private enum LookupState : byte
        {
            /// <summary>
            ///   <para>没有命中任何模块。</para>
            /// </summary>
            None = 0,
            /// <summary>
            ///   <para>成功命中唯一模块。</para>
            /// </summary>
            Unique = 1,
            /// <summary>
            ///   <para>命中了多个候选模块，结果不唯一。</para>
            /// </summary>
            Ambiguous = 2
        }

        /// <summary>
        ///   <para>内部模块解析结果。</para>
        /// </summary>
        private readonly struct ResolveResult
        {
            /// <summary>
            ///   <para>解析状态。</para>
            /// </summary>
            public readonly LookupState state;
            /// <summary>
            ///   <para>唯一命中时解析出的模块实例。</para>
            /// </summary>
            public readonly IGameModule module;
            /// <summary>
            ///   <para>歧义场景下的错误信息。</para>
            /// </summary>
            public readonly string error;

            /// <summary>
            ///   <para>创建解析结果。</para>
            /// </summary>
            /// <param name="state">状态。</param>
            /// <param name="module">模块。</param>
            /// <param name="error">错误。</param>
            public ResolveResult(LookupState state, IGameModule module, string error)
            {
                this.state = state;
                this.module = module;
                this.error = error;
            }
        }

        /// <summary>
        ///   <para>无依赖时复用的空句柄数组。</para>
        /// </summary>
        private static readonly RuntimeTypeHandle[] s_EmptyDependencyHandles = Array.Empty<RuntimeTypeHandle>();

        /// <summary>
        ///   <para>无模块时复用的空模块列表。</para>
        /// </summary>
        private static readonly ReadOnlyCollection<IGameModule> s_EmptyModuleList = Array.AsReadOnly(Array.Empty<IGameModule>());

        /// <summary>
        ///   <para>保护模块注册表全部可变状态的同步锁。</para>
        /// </summary>
        private readonly object m_Lock = new();

        /// <summary>
        ///   <para>当前已安装模块的顺序列表。</para>
        /// </summary>
        private readonly List<IGameModule> m_Modules = new(16);

        /// <summary>
        ///   <para>模块精确类型到实例的索引。</para>
        /// </summary>
        private readonly Dictionary<RuntimeTypeHandle, IGameModule> m_ModuleMap = new(16);

        /// <summary>
        ///   <para>模块精确类型到其依赖模块精确类型列表的映射。</para>
        /// </summary>
        private readonly Dictionary<RuntimeTypeHandle, RuntimeTypeHandle[]> m_ModuleDependencies = new(16);

        /// <summary>
        ///   <para>依赖反向索引。</para>
        /// </summary>
        private readonly Dictionary<RuntimeTypeHandle, HashSet<RuntimeTypeHandle>> m_ModuleDependents = new(16);

        /// <summary>
        ///   <para>按公开类型/基类查询时的解析缓存。</para>
        /// </summary>
        private readonly Dictionary<RuntimeTypeHandle, ResolveResult> m_AssignableLookupCache = new(16);

        /// <summary>
        ///   <para>预留安装模块的已解析依赖缓存。</para>
        /// </summary>
        private readonly Dictionary<IGameModule, RuntimeTypeHandle[]> m_ReservedDependencies = new(ReferenceEqualityComparer<IGameModule>.Instance);


        /// <summary>
        ///   <para>当前正在执行卸载流程的模块集合。</para>
        /// </summary>
        private readonly HashSet<IGameModule> m_UninstallingModules = new(ReferenceEqualityComparer<IGameModule>.Instance);

        /// <summary>
        ///   <para>模块列表缓存。</para>
        /// </summary>
        private IReadOnlyList<IGameModule> m_CachedModuleList;



        /// <summary>
        ///   <para>供外部复用的注册表同步根。</para>
        /// </summary>
        internal object SyncRoot => m_Lock;

        /// <summary>
        ///   <para>获取模块列表。</para>
        /// </summary>
        internal IReadOnlyList<IGameModule> ModuleList
        {
            get
            {
                lock (m_Lock)
                {
                    return GetModuleListNoLock();
                }
            }
        }

        /// <summary>
        ///   <para>将当前模块列表复制到目标列表中。</para>
        /// </summary>
        /// <param name="output">用于接收模块引用的列表。</param>
        internal void CopyModulesTo(List<IGameModule> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            lock (m_Lock)
            {
                output.Clear();
                output.AddRange(m_Modules);
            }
        }

        /// <summary>
        ///   <para>创建一个按依赖顺序排列的模块引用；结果保证依赖模块出现在依赖者之前。</para>
        /// </summary>
        internal List<IGameModule> CopyInstalledModules()
        {
            lock (m_Lock)
            {
                // 安装只允许依赖已经存在的模块，因此安装顺序本身就是拓扑顺序。
                return new List<IGameModule>(m_Modules);
            }
        }

        /// <summary>
        ///   <para>按泛型类型获取模块实例。</para>
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        internal T GetModule<T>()
            where T : class, IGameModule => GetModule(typeof(T)) as T;

        /// <summary>
        ///   <para>按运行时类型获取模块实例。</para>
        /// </summary>
        /// <param name="requestedType">要查询的模块类型或模块公开类型。</param>
        internal IGameModule GetModule(Type requestedType)
        {
            if (requestedType == null) return null;

            lock (m_Lock)
            {
                var resolved = ResolveCachedModuleNoLock(requestedType);
                if (resolved.state == LookupState.Ambiguous)
                {
                    throw new InvalidOperationException(resolved.error);
                }

                return resolved.module;
            }
        }

        /// <summary>
        ///   <para>按运行时类型尝试获取模块实例。</para>
        /// </summary>
        /// <param name="requestedType">要查询的模块类型或模块公开类型。</param>
        /// <param name="module">唯一匹配到的模块实例。</param>
        internal bool TryGetModule(Type requestedType, out IGameModule module)
        {
            module = null;
            if (requestedType == null)
            {
                return false;
            }

            lock (m_Lock)
            {
                var resolved = ResolveCachedModuleNoLock(requestedType);
                if (resolved.state != LookupState.Unique)
                {
                    return false;
                }

                module = resolved.module;
                return true;
            }
        }

        /// <summary>
        ///   <para>按模块安装时已经绑定的依赖图读取依赖模块。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="requestedType">请求的类型。</param>
        internal IGameModule GetDependency(IGameModule ownerModule, Type requestedType)
        {
            if (ownerModule == null) throw new ArgumentNullException(nameof(ownerModule));
            if (requestedType == null) return null;

            lock (m_Lock)
            {
                var resolved = ResolveBoundDependencyNoLock(ownerModule, requestedType);
                if (resolved.state == LookupState.Ambiguous)
                {
                    throw new InvalidOperationException(resolved.error);
                }

                return resolved.module;
            }
        }

        /// <summary>
        ///   <para>尝试按模块安装时已经绑定的依赖图读取依赖模块。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="requestedType">请求的类型。</param>
        /// <param name="module">模块。</param>
        internal bool TryGetDependency(IGameModule ownerModule, Type requestedType, out IGameModule module)
        {
            if (ownerModule == null) throw new ArgumentNullException(nameof(ownerModule));
            module = null;
            if (requestedType == null)
            {
                return false;
            }

            lock (m_Lock)
            {
                var resolved = ResolveBoundDependencyNoLock(ownerModule, requestedType);
                if (resolved.state != LookupState.Unique)
                {
                    return false;
                }

                module = resolved.module;
                return true;
            }
        }

        /// <summary>
        ///   <para>按请求类型解析模块，并复用可赋值查询缓存。</para>
        /// </summary>
        /// <param name="requestedType">请求的类型。</param>
        private ResolveResult ResolveCachedModuleNoLock(Type requestedType)
        {
            var requestedHandle = requestedType.TypeHandle;
            if (m_ModuleMap.TryGetValue(requestedHandle, out var exact))
            {
                return new ResolveResult(LookupState.Unique, exact, null);
            }

            if (m_AssignableLookupCache.TryGetValue(requestedHandle, out var cached))
            {
                return cached;
            }

            var resolved = ResolveModuleNoLock(requestedType, default, skipUninstallingModules: false);
            m_AssignableLookupCache[requestedHandle] = resolved;
            return resolved;
        }

        /// <summary>
        ///   <para>尝试按精确类型获取模块。</para>
        /// </summary>
        /// <param name="moduleType">模块的精确运行时类型。</param>
        /// <param name="module">命中的模块实例。</param>
        internal bool TryGetExactModule(Type moduleType, out IGameModule module)
        {
            module = null;
            if (moduleType == null) return false;

            lock (m_Lock)
            {
                return m_ModuleMap.TryGetValue(moduleType.TypeHandle, out module);
            }
        }

        /// <summary>
        ///   <para>为安装流程预占模块类型并解析依赖。</para>
        /// </summary>
        /// <param name="module">即将安装的模块实例。</param>
        internal void ReserveInstall(IGameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            lock (m_Lock)
            {
                var moduleType = module.GetType();
                var moduleTypeName = GameModuleUtility.GetTypeDisplayName(moduleType);

                if (IndexOfModuleReferenceNoLock(module) >= 0)
                {
                    throw new InvalidOperationException($"Module instance is already installed. type={moduleTypeName}");
                }

                if (m_ReservedDependencies.ContainsKey(module))
                {
                    throw new InvalidOperationException($"Module instance is already installing. type={moduleTypeName}");
                }

                if (m_UninstallingModules.Contains(module))
                {
                    throw new InvalidOperationException($"Module instance is already uninstalling. type={moduleTypeName}");
                }

                var moduleTypeHandle = moduleType.TypeHandle;
                if (IsModuleTypeChangingNoLock(moduleTypeHandle))
                {
                    throw new InvalidOperationException("Target module type is already changing.");
                }

                if (m_ModuleMap.ContainsKey(moduleTypeHandle))
                    throw new InvalidOperationException($"Module type is already installed: {moduleTypeName}. Uninstall it explicitly before installing another instance.");

                var dependencies = ResolveModuleDependenciesNoLock(module);
                ReserveDependenciesNoLock(module, dependencies);
            }
        }

        /// <summary>
        ///   <para>把模块写入已安装索引并结束安装中状态。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        internal void AddInstalledModule(IGameModule module)
        {
            if (module == null) return;

            lock (m_Lock)
            {
                if (!m_ReservedDependencies.TryGetValue(module, out var dependencies))
                {
                    var moduleName = GameModuleUtility.GetTypeDisplayName(module.GetType());
                    throw new InvalidOperationException(
                        $"Cannot add module to registry because its reserved dependency data is missing. type={moduleName}");
                }

                m_ReservedDependencies.Remove(module);
                AddInstalledModuleNoLock(module, dependencies);
            }
        }

        /// <summary>
        ///   <para>取消预留安装状态。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        internal void ReleaseReservedInstall(IGameModule module)
        {
            if (module == null) return;

            lock (m_Lock)
            {
                m_ReservedDependencies.Remove(module);
            }
        }

        /// <summary>
        ///   <para>开始卸载流程并标记模块进入卸载中状态。</para>
        /// </summary>
        /// <param name="module">要卸载的模块实例。</param>
        internal bool ReserveUninstall(IGameModule module)
        {
            if (module == null) return false;

            lock (m_Lock)
            {
                if (m_ReservedDependencies.ContainsKey(module) || m_UninstallingModules.Contains(module)) return false;

                var moduleTypeHandle = module.GetType().TypeHandle;
                if (!m_ModuleMap.TryGetValue(moduleTypeHandle, out var installed) || !ReferenceEquals(installed, module))
                {
                    return false;
                }

                if (TryGetBlockingDependentNoLock(moduleTypeHandle, out var dependent))
                {
                    throw new InvalidOperationException(
                        $"Cannot uninstall module {GameModuleUtility.GetTypeDisplayName(module.GetType())} because dependent module {GameModuleUtility.GetTypeDisplayName(dependent.GetType())} is still installed.");
                }

                m_UninstallingModules.Add(module);
                return true;
            }
        }

        /// <summary>
        ///   <para>判断模块是否正处于卸载中状态。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        internal bool IsUninstalling(IGameModule module)
        {
            if (module == null) return false;

            lock (m_Lock)
            {
                return m_UninstallingModules.Contains(module);
            }
        }

        /// <summary>
        ///   <para>从已安装索引中移除模块并结束卸载中状态。</para>
        /// </summary>
        /// <param name="module">已成功执行卸载回调的模块。</param>
        internal bool RemoveInstalledModule(IGameModule module)
        {
            if (module == null) return false;

            lock (m_Lock)
            {
                m_UninstallingModules.Remove(module);

                bool removed = false;
                var moduleTypeHandle = module.GetType().TypeHandle;
                if (m_ModuleMap.TryGetValue(moduleTypeHandle, out var installed) && ReferenceEquals(installed, module))
                {
                    m_ModuleMap.Remove(moduleTypeHandle);
                    removed = true;
                }

                int index = IndexOfModuleReferenceNoLock(module);
                if (index >= 0)
                {
                    m_Modules.RemoveAt(index);
                    removed = true;
                }

                if (removed)
                {
                    RemoveDependencyLinksNoLock(moduleTypeHandle);
                    MarkModulesChangedNoLock();
                }

                return removed;
            }
        }

        /// <summary>
        ///   <para>清理所有安装/卸载过程状态。</para>
        /// </summary>
        internal void ClearTransitionState()
        {
            lock (m_Lock)
            {
                m_UninstallingModules.Clear();
                m_ReservedDependencies.Clear();
            }
        }

        /// <summary>
        ///   <para>重置整个注册表。</para>
        /// </summary>
        internal void Reset()
        {
            lock (m_Lock)
            {
                m_Modules.Clear();
                m_ModuleMap.Clear();
                m_ModuleDependencies.Clear();
                m_ModuleDependents.Clear();
                m_AssignableLookupCache.Clear();
                m_UninstallingModules.Clear();
                m_ReservedDependencies.Clear();
                m_CachedModuleList = s_EmptyModuleList;
            }
        }

        /// <summary>
        ///   <para>检查指定精确类型是否已有模块处于安装中或卸载中状态。</para>
        /// </summary>
        /// <param name="moduleTypeHandle">模块类型句柄。</param>
        private bool IsModuleTypeChangingNoLock(RuntimeTypeHandle moduleTypeHandle)
        {
            foreach (var installingModule in m_ReservedDependencies.Keys)
            {
                if (installingModule?.GetType().TypeHandle.Equals(moduleTypeHandle) != true) continue;
                return true;
            }

            foreach (var uninstallingModule in m_UninstallingModules)
            {
                if (uninstallingModule?.GetType().TypeHandle.Equals(moduleTypeHandle) != true) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>把已解析的模块依赖登记到待安装缓存。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="dependencies">依赖。</param>
        private void ReserveDependenciesNoLock(IGameModule module, RuntimeTypeHandle[] dependencies) => m_ReservedDependencies.Add(module, dependencies);

        /// <summary>
        ///   <para>解析模块类型声明的依赖并转换为已安装模块的精确类型句柄列表。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private RuntimeTypeHandle[] ResolveModuleDependenciesNoLock(IGameModule module)
        {
            var dependencies = GameModuleDependencyUtility.GetDependencies(module.GetType());
            if (dependencies.Length == 0)
            {
                return s_EmptyDependencyHandles;
            }

            var moduleType = module.GetType();
            return ResolveDependencyHandlesNoLock(moduleType, moduleType.TypeHandle, dependencies);
        }

        /// <summary>
        ///   <para>在持锁状态下解析依赖列表。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="moduleTypeHandle">模块类型句柄。</param>
        /// <param name="dependencies">依赖。</param>
        private RuntimeTypeHandle[] ResolveDependencyHandlesNoLock(Type moduleType, RuntimeTypeHandle moduleTypeHandle, IReadOnlyList<Type> dependencies)
        {
            if (dependencies.Count == 0)
            {
                return s_EmptyDependencyHandles;
            }

            var resolvedTypes = new HashSet<RuntimeTypeHandle>();
            var resolvedHandles = new List<RuntimeTypeHandle>(dependencies.Count);

            for (int i = 0; i < dependencies.Count; i++)
            {
                var dependencyType = dependencies[i];
                var resolved = ResolveModuleNoLock(dependencyType, moduleTypeHandle, skipUninstallingModules: true);
                if (resolved.state == LookupState.Ambiguous)
                {
                    throw new InvalidOperationException(resolved.error);
                }

                if (resolved.state == LookupState.None || resolved.module == null)
                {
                    throw new InvalidOperationException($"Missing module dependency: {moduleType.FullName} -> {dependencyType.FullName}");
                }

                var resolvedHandle = resolved.module.GetType().TypeHandle;
                if (resolvedTypes.Add(resolvedHandle))
                {
                    resolvedHandles.Add(resolvedHandle);
                }
            }

            return resolvedHandles.Count == 0 ? s_EmptyDependencyHandles : resolvedHandles.ToArray();
        }

        /// <summary>
        ///   <para>只在指定模块已经绑定的依赖集合中解析请求类型。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="requestedType">请求的类型。</param>
        private ResolveResult ResolveBoundDependencyNoLock(IGameModule ownerModule, Type requestedType)
        {
            if (!TryGetBoundDependencyHandlesNoLock(ownerModule, out var dependencyHandles) ||
                dependencyHandles.Length == 0)
            {
                return new ResolveResult(LookupState.None, null, null);
            }

            IGameModule firstMatch = null;
            Type firstMatchType = null;
            for (int i = 0; i < dependencyHandles.Length; i++)
            {
                var dependencyHandle = dependencyHandles[i];
                if (!m_ModuleMap.TryGetValue(dependencyHandle, out var dependencyModule))
                {
                    continue;
                }

                if (m_UninstallingModules.Contains(dependencyModule))
                {
                    continue;
                }

                if (!requestedType.IsInstanceOfType(dependencyModule))
                {
                    continue;
                }

                if (firstMatch != null)
                {
                    return new ResolveResult(
                        LookupState.Ambiguous,
                        null,
                        $"Ambiguous bound dependency lookup: {ownerModule.GetType().FullName} -> {requestedType.FullName} " +
                        $"is matched by both {firstMatchType.FullName} and {dependencyModule.GetType().FullName}.");
                }

                firstMatch = dependencyModule;
                firstMatchType = dependencyModule.GetType();
            }

            return firstMatch != null
                ? new ResolveResult(LookupState.Unique, firstMatch, null)
                : new ResolveResult(LookupState.None, null, null);
        }

        /// <summary>
        ///   <para>获取模块安装时已经解析出的依赖句柄。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="dependencyHandles">依赖句柄。</param>
        private bool TryGetBoundDependencyHandlesNoLock(
            IGameModule ownerModule,
            out RuntimeTypeHandle[] dependencyHandles)
        {
            if (m_ReservedDependencies.TryGetValue(ownerModule, out dependencyHandles))
            {
                return true;
            }

            var ownerTypeHandle = ownerModule.GetType().TypeHandle;
            if (!m_ModuleMap.TryGetValue(ownerTypeHandle, out var installedModule) ||
                !ReferenceEquals(installedModule, ownerModule))
            {
                dependencyHandles = null;
                return false;
            }

            return m_ModuleDependencies.TryGetValue(ownerTypeHandle, out dependencyHandles);
        }

        /// <summary>
        ///   <para>在已安装模块集合中解析唯一模块。</para>
        /// </summary>
        /// <param name="requestedType">请求的模块类型或公开类型。</param>
        /// <param name="excludedTypeHandle">需要排除的模块精确类型，通常用于避免把自己解析成自己的依赖。</param>
        /// <param name="skipUninstallingModules">是否跳过已经进入卸载中状态的模块。</param>
        private ResolveResult ResolveModuleNoLock(Type requestedType, RuntimeTypeHandle excludedTypeHandle, bool skipUninstallingModules)
        {
            if (requestedType == null)
            {
                return new ResolveResult(LookupState.None, null, null);
            }

            var requestedHandle = requestedType.TypeHandle;
            if (!requestedHandle.Equals(excludedTypeHandle)
                && m_ModuleMap.TryGetValue(requestedHandle, out var exact)
                && (!skipUninstallingModules || !m_UninstallingModules.Contains(exact)))
            {
                return new ResolveResult(LookupState.Unique, exact, null);
            }

            IGameModule firstMatch = null;
            Type firstMatchType = null;

            for (int i = 0; i < m_Modules.Count; i++)
            {
                var candidate = m_Modules[i];
                if (skipUninstallingModules && m_UninstallingModules.Contains(candidate)) continue;

                var candidateType = candidate.GetType();
                var candidateTypeHandle = candidateType.TypeHandle;
                if (candidateTypeHandle.Equals(excludedTypeHandle)) continue;
                if (!requestedType.IsInstanceOfType(candidate)) continue;

                if (firstMatch != null)
                {
                    return new ResolveResult(
                        LookupState.Ambiguous,
                        null,
                        $"Ambiguous module lookup: {requestedType.FullName} is matched by both {firstMatchType.FullName} and {candidateType.FullName}.");
                }

                firstMatch = candidate;
                firstMatchType = candidateType;
            }

            return firstMatch != null
                ? new ResolveResult(LookupState.Unique, firstMatch, null)
                : new ResolveResult(LookupState.None, null, null);
        }

        /// <summary>
        ///   <para>检查当前模块是否仍被其他已安装模块依赖。</para>
        /// </summary>
        /// <param name="moduleTypeHandle">模块类型句柄。</param>
        /// <param name="dependent">依赖者。</param>
        private bool TryGetBlockingDependentNoLock(RuntimeTypeHandle moduleTypeHandle, out IGameModule dependent)
        {
            dependent = null;
            if (!m_ModuleDependents.TryGetValue(moduleTypeHandle, out var dependentHandles) ||
                dependentHandles.Count == 0)
            {
                return false;
            }

            foreach (var dependentHandle in dependentHandles)
            {
                if (!m_ModuleMap.TryGetValue(dependentHandle, out dependent)) continue;
                if (m_UninstallingModules.Contains(dependent)) continue;
                return true;
            }

            dependent = null;
            return false;
        }

        /// <summary>
        ///   <para>把模块写入已安装列表和索引。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="dependencies">依赖。</param>
        private void AddInstalledModuleNoLock(IGameModule module, RuntimeTypeHandle[] dependencies)
        {
            var moduleTypeHandle = module.GetType().TypeHandle;
            m_Modules.Add(module);
            m_ModuleMap[moduleTypeHandle] = module;
            RegisterDependencyLinksNoLock(moduleTypeHandle, dependencies);
            MarkModulesChangedNoLock();
        }

        /// <summary>
        ///   <para>登记模块的依赖关系和反向依赖关系。</para>
        /// </summary>
        /// <param name="moduleTypeHandle">模块类型句柄。</param>
        /// <param name="dependencies">依赖。</param>
        private void RegisterDependencyLinksNoLock(RuntimeTypeHandle moduleTypeHandle, RuntimeTypeHandle[] dependencies)
        {
            if (dependencies.Length == 0)
            {
                m_ModuleDependencies[moduleTypeHandle] = s_EmptyDependencyHandles;
                return;
            }

            m_ModuleDependencies[moduleTypeHandle] = dependencies;

            for (int i = 0; i < dependencies.Length; i++)
            {
                var dependencyHandle = dependencies[i];
                if (!m_ModuleDependents.TryGetValue(dependencyHandle, out var dependents))
                {
                    dependents = new HashSet<RuntimeTypeHandle>();
                    m_ModuleDependents[dependencyHandle] = dependents;
                }

                dependents.Add(moduleTypeHandle);
            }
        }

        /// <summary>
        ///   <para>移除模块的依赖关系和反向依赖关系。</para>
        /// </summary>
        /// <param name="moduleTypeHandle">模块类型句柄。</param>
        private void RemoveDependencyLinksNoLock(RuntimeTypeHandle moduleTypeHandle)
        {
            if (!m_ModuleDependencies.TryGetValue(moduleTypeHandle, out var dependencies))
            {
                return;
            }

            m_ModuleDependencies.Remove(moduleTypeHandle);
            if (dependencies.Length == 0) return;

            for (int i = 0; i < dependencies.Length; i++)
            {
                var dependencyHandle = dependencies[i];
                if (!m_ModuleDependents.TryGetValue(dependencyHandle, out var dependents)) continue;

                dependents.Remove(moduleTypeHandle);
                if (dependents.Count == 0)
                {
                    m_ModuleDependents.Remove(dependencyHandle);
                }
            }
        }

        /// <summary>
        ///   <para>获取当前模块列表缓存。</para>
        /// </summary>
        private IReadOnlyList<IGameModule> GetModuleListNoLock()
        {
            return m_CachedModuleList ??= m_Modules.Count == 0
                ? s_EmptyModuleList : Array.AsReadOnly(m_Modules.ToArray());
        }

        /// <summary>
        ///   <para>标记模块集合已变化。</para>
        /// </summary>
        private void MarkModulesChangedNoLock()
        {
            m_AssignableLookupCache.Clear();
            m_CachedModuleList = null;
        }

        /// <summary>
        ///   <para>按引用查找模块实例在安装顺序列表中的位置。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private int IndexOfModuleReferenceNoLock(IGameModule module)
        {
            if (module == null)
            {
                return -1;
            }

            for (int i = 0; i < m_Modules.Count; i++)
            {
                if (ReferenceEquals(m_Modules[i], module))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}