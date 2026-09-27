namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>游戏模块描述符。</para>
    /// </summary>
    [Serializable]
    public sealed class GameModuleDescriptor
    {
        /// <summary>
        ///   <para>执行清单安装时使用的模块创建工厂。</para>
        /// </summary>
        private readonly Func<GameModule> m_Factory;
        /// <summary>
        ///   <para>条目配置委托；在框架接管和工厂配置后调用。</para>
        /// </summary>
        internal Action<GameModule> Configure { get; }

        /// <summary>
        ///   <para>模块的精确运行时类型。</para>
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        ///   <para>模块类型句柄。</para>
        /// </summary>
        internal RuntimeTypeHandle ModuleTypeHandle { get; }

        /// <summary>
        ///   <para>模块类型显示名。</para>
        /// </summary>
        internal string ModuleTypeName { get; }

        /// <summary>
        ///   <para>模块类型声明的依赖列表。</para>
        /// </summary>
        internal Type[] DependencyTypes { get; }

        /// <summary>
        ///   <para>创建模块描述符。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="configure">接管后执行的条目配置。</param>
        public GameModuleDescriptor(Type moduleType, Func<GameModule> factory = null, Action<GameModule> configure = null)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));

            var error = GameModuleUtility.GetModuleTypeError(moduleType);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(moduleType));
            }

            ModuleType = moduleType;
            ModuleTypeHandle = moduleType.TypeHandle;
            ModuleTypeName = GameModuleUtility.GetTypeDisplayName(moduleType);
            DependencyTypes = GameModuleDependencyUtility.GetDependencies(moduleType);
            m_Factory = factory;
            Configure = configure;
        }

        /// <summary>
        ///   <para>通过描述符创建模块实例并校验精确运行时类型。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        internal GameModule CreateModule(IGameModuleFactory factory) => GameModuleUtility.CreateModuleForExactType(ModuleType, m_Factory ?? (() => factory.Create(ModuleType)));

        /// <inheritdoc />
        public override string ToString() => ModuleTypeName;
    }

    /// <summary>
    ///   <para>模块清单的安装顺序。</para>
    /// </summary>
    public enum GameModuleInstallOrder : byte
    {
        /// <summary>
        ///   <para>按模块依赖关系自动整理安装顺序。</para>
        /// </summary>
        Dependency = 0,
        /// <summary>
        ///   <para>按清单声明顺序安装；依赖必须已经出现在更早的条目中。</para>
        /// </summary>
        Declared = 1,
    }
    
    /// <summary>
    ///   <para>游戏模块清单。</para>
    /// </summary>
    [Serializable]
    public sealed class GameModuleManifest
    {
        /// <summary>
        ///   <para>拓扑排序中的访问状态。</para>
        /// </summary>
        private enum VisitState : byte
        {
            /// <summary>
            ///   <para>访问中。</para>
            /// </summary>
            Visiting = 0,
            /// <summary>
            ///   <para>完成。</para>
            /// </summary>
            Done = 1,
        }

        /// <summary>
        ///   <para>非递归拓扑排序时使用的访问栈帧。</para>
        /// </summary>
        [Serializable]
        private struct VisitFrame
        {
            /// <summary>
            ///   <para>当前处理中的模块项索引。</para>
            /// </summary>
            public int itemIndex;
            /// <summary>
            ///   <para>当前已处理到的依赖索引。</para>
            /// </summary>
            public int dependencyIndex;
            /// <summary>
            ///   <para>当前模块项是否已经进入访问流程。</para>
            /// </summary>
            public bool entered;
        }

        /// <summary>
        ///   <para>清单中的模块描述符列表。</para>
        /// </summary>
        private readonly List<GameModuleDescriptor> m_Descriptors = new();

        /// <summary>
        ///   <para>清单描述符列表引用。</para>
        /// </summary>
        private readonly IReadOnlyList<GameModuleDescriptor> m_DescriptorList;

        /// <summary>
        ///   <para>模块类型句柄到描述符。</para>
        /// </summary>
        private readonly Dictionary<RuntimeTypeHandle, GameModuleDescriptor> m_ByType = new();

        /// <summary>
        ///   <para>清单安装顺序。</para>
        /// </summary>
        private readonly GameModuleInstallOrder m_InstallOrder;
        
        /// <summary>
        ///   <para>清单内模块描述符。</para>
        /// </summary>
        public IReadOnlyList<GameModuleDescriptor> Descriptors => m_DescriptorList;
        
        /// <summary>
        ///   <para>清单安装顺序。</para>
        /// </summary>
        public GameModuleInstallOrder InstallOrder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_InstallOrder;
        }

        /// <summary>
        ///   <para>清单模块数量。</para>
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Descriptors.Count;
        }

        /// <summary>
        ///   <para>创建模块清单。</para>
        /// </summary>
        /// <param name="installOrder">安装顺序。</param>
        public GameModuleManifest(GameModuleInstallOrder installOrder = GameModuleInstallOrder.Dependency)
        {
            m_DescriptorList = m_Descriptors.AsReadOnly();
            m_InstallOrder = installOrder;
        }

        /// <summary>
        ///   <para>添加模块描述符。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="replace">是否替换同精确类型下已有的描述符。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        public GameModuleDescriptor Add<T>(Func<T> factory, bool replace = false)
            where T : GameModule
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var descriptor = new GameModuleDescriptor(typeof(T), factory);
            Add(descriptor, replace);
            return descriptor;
        }

        /// <summary>
        ///   <para>添加模块描述符，安装时使用所属容器的创建策略。</para>
        /// </summary>
        /// <param name="replace">是否替换同精确类型下已有的描述符。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        public GameModuleDescriptor Add<T>(bool replace = false)
            where T : GameModule => Add(typeof(T), replace: replace);

        /// <summary>
        ///   <para>添加模块描述符。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="replace">是否替换已存在描述符。</param>
        public GameModuleDescriptor Add(Type moduleType, Func<GameModule> factory = null, bool replace = false)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            var descriptor = new GameModuleDescriptor(moduleType, factory);
            Add(descriptor, replace);
            return descriptor;
        }

        /// <summary>
        ///   <para>把模块描述符加入清单。</para>
        /// </summary>
        /// <param name="descriptor">模块描述符。</param>
        /// <param name="replace">是否替换已存在描述符。</param>
        public void Add(GameModuleDescriptor descriptor, bool replace = false)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            var handle = descriptor.ModuleTypeHandle;
            if (m_ByType.TryGetValue(handle, out var existing))
            {
                if (!replace)
                {
                    throw new InvalidOperationException($"Module descriptor already exists: {descriptor.ModuleTypeName}");
                }

                int existingIndex = m_Descriptors.IndexOf(existing);
                if (existingIndex < 0)
                {
                    throw new InvalidOperationException($"{nameof(GameModuleManifest)} descriptor state is inconsistent.");
                }

                m_Descriptors[existingIndex] = descriptor;
                m_ByType[handle] = descriptor;
                return;
            }

            m_ByType.Add(handle, descriptor);
            m_Descriptors.Add(descriptor);
        }

        /// <summary>
        ///   <para>移除指定泛型类型对应的模块描述符。</para>
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        public bool Remove<T>() where T : GameModule => Remove(typeof(T));

        /// <summary>
        ///   <para>检查清单是否包含指定泛型模块类型。</para>
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        public bool Contains<T>() where T : GameModule => Contains(typeof(T));

        /// <summary>
        ///   <para>移除指定类型对应的模块描述符。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        public bool Remove(Type moduleType)
        {
            if (moduleType == null) return false;
            var handle = moduleType.TypeHandle;
            if (!m_ByType.TryGetValue(handle, out var descriptor)) return false;

            int descriptorIndex = m_Descriptors.IndexOf(descriptor);
            if (descriptorIndex < 0)
            {
                throw new InvalidOperationException($"{nameof(GameModuleManifest)} descriptor state is inconsistent.");
            }

            m_ByType.Remove(handle);
            m_Descriptors.RemoveAt(descriptorIndex);
            return true;
        }

        /// <summary>
        ///   <para>检查清单是否包含指定模块类型。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        public bool Contains(Type moduleType)
        {
            if (moduleType == null) return false;
            return m_ByType.ContainsKey(moduleType.TypeHandle);
        }

        /// <summary>
        ///   <para>尝试获取指定类型对应的模块描述符。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="descriptor">模块描述符。</param>
        public bool TryGetDescriptor(Type moduleType, out GameModuleDescriptor descriptor)
        {
            descriptor = null;
            if (moduleType == null) return false;
            return m_ByType.TryGetValue(moduleType.TypeHandle, out descriptor);
        }

        /// <summary>
        ///   <para>尝试获取指定泛型类型对应的模块描述符。</para>
        /// </summary>
        /// <param name="descriptor">模块描述符。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        public bool TryGetDescriptor<T>(out GameModuleDescriptor descriptor)
            where T : GameModule => TryGetDescriptor(typeof(T), out descriptor);

        /// <summary>
        ///   <para>清空清单中的全部模块描述符。</para>
        /// </summary>
        public void Clear()
        {
            if (m_Descriptors.Count == 0) return;
            m_Descriptors.Clear();
            m_ByType.Clear();
        }

        /// <summary>
        ///   <para>只整理声明；每个实例在即将安装时才创建，创建成功即交给容器。</para>
        /// </summary>
        internal IReadOnlyList<GameModuleDescriptor> GetInstallDescriptors()
        {
            return InstallOrder == GameModuleInstallOrder.Declared
                ? ValidateDeclaredDescriptors(m_Descriptors)
                : OrderDescriptors(m_Descriptors);
        }

        /// <summary>
        ///   <para>按依赖关系整理描述符顺序。</para>
        /// </summary>
        /// <param name="descriptors">描述符。</param>
        private static GameModuleDescriptor[] OrderDescriptors(List<GameModuleDescriptor> descriptors)
        {
            var orderedDescriptors = new List<GameModuleDescriptor>(descriptors.Count);
            var visitState = new Dictionary<RuntimeTypeHandle, VisitState>(descriptors.Count);
            var stack = new List<VisitFrame>(descriptors.Count);
            var exactTypeIndexMap = CreateExactTypeIndexMap(descriptors);

            for (int i = 0; i < descriptors.Count; i++)
            {
                AppendOrderedDescriptorsFrom(i, descriptors, exactTypeIndexMap, orderedDescriptors, visitState, stack);
            }

            return orderedDescriptors.ToArray();
        }

        /// <summary>
        ///   <para>校验当前声明顺序是否已经满足依赖要求，并直接返回当前顺序。</para>
        /// </summary>
        /// <param name="descriptors">描述符。</param>
        private static GameModuleDescriptor[] ValidateDeclaredDescriptors(List<GameModuleDescriptor> descriptors)
        {
            if (descriptors.Count == 0)
            {
                return Array.Empty<GameModuleDescriptor>();
            }

            var exactTypeIndexMap = CreateExactTypeIndexMap(descriptors);

            for (int i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                var dependencies = descriptor.DependencyTypes;
                for (int j = 0; j < dependencies.Length; j++)
                {
                    if (!TryFindDependencyItemIndex(
                            descriptor.ModuleType,
                            descriptor.ModuleTypeHandle,
                            dependencies[j],
                            descriptors,
                            exactTypeIndexMap,
                            i,
                            out _,
                            out var error))
                    {
                        throw new InvalidOperationException(
                            $"{nameof(GameModuleManifest)} declared install order is invalid at {descriptor.ModuleType.FullName}. {error}");
                    }
                }

            }

            return descriptors.ToArray();
        }

        /// <summary>
        ///   <para>从指定根描述符开始追加依赖有序结果。</para>
        /// </summary>
        /// <param name="rootIndex">根节点索引。</param>
        /// <param name="descriptors">描述符。</param>
        /// <param name="exactTypeIndexMap">精确类型到索引的映射。</param>
        /// <param name="orderedDescriptors">已排序描述符。</param>
        /// <param name="visitState">访问状态。</param>
        /// <param name="stack">栈。</param>
        private static void AppendOrderedDescriptorsFrom(
            int rootIndex,
            List<GameModuleDescriptor> descriptors,
            Dictionary<RuntimeTypeHandle, int> exactTypeIndexMap,
            List<GameModuleDescriptor> orderedDescriptors,
            Dictionary<RuntimeTypeHandle, VisitState> visitState,
            List<VisitFrame> stack)
        {
            var rootEntry = descriptors[rootIndex];
            var rootHandle = rootEntry.ModuleTypeHandle;
            if (visitState.TryGetValue(rootHandle, out var rootState))
            {
                if (rootState == VisitState.Visiting)
                {
                    throw new InvalidOperationException($"Module dependency cycle detected at {rootEntry.ModuleType.FullName}.");
                }

                if (rootState == VisitState.Done)
                {
                    return;
                }
            }

            stack.Clear();
            stack.Add(new VisitFrame { itemIndex = rootIndex, dependencyIndex = 0, entered = false });

            while (stack.Count > 0)
            {
                int topIndex = stack.Count - 1;
                var frame = stack[topIndex];
                var descriptor = descriptors[frame.itemIndex];
                var moduleType = descriptor.ModuleType;
                var moduleHandle = descriptor.ModuleTypeHandle;

                if (!frame.entered)
                {
                    if (visitState.TryGetValue(moduleHandle, out var state))
                    {
                        if (state == VisitState.Visiting)
                        {
                            throw new InvalidOperationException($"Module dependency cycle detected at {moduleType.FullName}.");
                        }

                        if (state == VisitState.Done)
                        {
                            stack.RemoveAt(topIndex);
                            continue;
                        }
                    }

                    visitState[moduleHandle] = VisitState.Visiting;
                    frame.entered = true;
                    stack[topIndex] = frame;
                }

                var dependencies = descriptor.DependencyTypes;
                if (frame.dependencyIndex < dependencies.Length)
                {
                    var dependencyType = dependencies[frame.dependencyIndex];
                    frame.dependencyIndex++;
                    stack[topIndex] = frame;

                    if (!TryFindDependencyItemIndex(
                            moduleType,
                            moduleHandle,
                            dependencyType,
                            descriptors,
                            exactTypeIndexMap,
                            descriptors.Count,
                            out var dependencyItemIndex,
                            out var error))
                    {
                        throw new InvalidOperationException(error);
                    }

                    if (dependencyItemIndex < 0)
                    {
                        continue;
                    }

                    stack.Add(new VisitFrame { itemIndex = dependencyItemIndex, dependencyIndex = 0, entered = false });
                    continue;
                }

                visitState[moduleHandle] = VisitState.Done;
                orderedDescriptors.Add(descriptor);
                stack.RemoveAt(topIndex);
            }
        }

        /// <summary>
        ///   <para>构建“精确模块类型 -> 清单索引”的查找表。</para>
        /// </summary>
        /// <param name="descriptors">描述符。</param>
        private static Dictionary<RuntimeTypeHandle, int> CreateExactTypeIndexMap(List<GameModuleDescriptor> descriptors)
        {
            var map = new Dictionary<RuntimeTypeHandle, int>(descriptors.Count);

            for (int i = 0; i < descriptors.Count; i++)
            {
                map[descriptors[i].ModuleTypeHandle] = i;
            }

            return map;
        }

        /// <summary>
        ///   <para>解析某个依赖类型对应的安装项索引。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="moduleTypeHandle">模块类型句柄。</param>
        /// <param name="dependencyType">依赖类型。</param>
        /// <param name="descriptors">描述符。</param>
        /// <param name="exactTypeIndexMap">精确类型到索引的映射。</param>
        /// <param name="searchCount">搜索数量。</param>
        /// <param name="itemIndex">项索引。</param>
        /// <param name="error">错误。</param>
        private static bool TryFindDependencyItemIndex(
            Type moduleType,
            RuntimeTypeHandle moduleTypeHandle,
            Type dependencyType,
            List<GameModuleDescriptor> descriptors,
            Dictionary<RuntimeTypeHandle, int> exactTypeIndexMap,
            int searchCount,
            out int itemIndex,
            out string error)
        {
            itemIndex = -1;
            error = null;

            if (dependencyType == null)
            {
                error = $"Module dependency type cannot be null. module={GameModuleUtility.GetTypeDisplayName(moduleType)}";
                return false;
            }

            if (exactTypeIndexMap.TryGetValue(dependencyType.TypeHandle, out itemIndex) &&
                itemIndex >= 0 &&
                itemIndex < descriptors.Count &&
                !descriptors[itemIndex].ModuleTypeHandle.Equals(moduleTypeHandle))
            {
                if (itemIndex < searchCount)
                {
                    return true;
                }

                error =
                    $"Module dependency is declared later in the same manifest: {moduleType.FullName} -> " +
                    $"{dependencyType.FullName} at index {itemIndex}.";
                itemIndex = -1;
                return false;
            }

            Type firstMatchType = null;
            itemIndex = -1;

            for (int i = 0; i < descriptors.Count; i++)
            {
                var candidateItem = descriptors[i];
                var candidateType = candidateItem.ModuleType;
                if (candidateItem.ModuleTypeHandle.Equals(moduleTypeHandle)) continue;
                if (!dependencyType.IsAssignableFrom(candidateType)) continue;

                if (itemIndex >= 0)
                {
                    error =
                        $"Ambiguous module dependency in manifest: {moduleType.FullName} -> {dependencyType.FullName} " +
                        $"matched by both {firstMatchType.FullName} and {candidateType.FullName}.";
                    itemIndex = -1;
                    return false;
                }

                itemIndex = i;
                firstMatchType = candidateType;
            }

            if (itemIndex < 0)
            {
                return true;
            }

            if (itemIndex >= searchCount)
            {
                error =
                    $"Module dependency is declared later in the same manifest: {moduleType.FullName} -> " +
                    $"{dependencyType.FullName} matched by {firstMatchType.FullName} at index {itemIndex}.";
                itemIndex = -1;
                return false;
            }

            return true;
        }
    }
}