namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;


    /// <summary>
    ///   <para>游戏模块描述符</para>
    /// </summary>
    [Serializable]
    public sealed class GameModuleDescriptor
    {
        /// <summary>
        ///   <para>执行清单安装时使用的模块创建工厂</para>
        /// </summary>
        private readonly Func<GameModule> m_Factory;

        /// <summary>
        ///   <para>模块的精确运行时类型</para>
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        ///   <para>模块类型句柄</para>
        /// </summary>
        internal RuntimeTypeHandle ModuleTypeHandle { get; }

        /// <summary>
        ///   <para>模块类型显示名</para>
        /// </summary>
        internal string ModuleTypeName { get; }

        /// <summary>
        ///   <para>创建模块描述符</para>
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <param name="factory">模块创建工厂</param>
        public GameModuleDescriptor(Type moduleType, Func<GameModule> factory)
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
            m_Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        ///   <para>通过描述符创建模块实例并校验精确运行时类型</para>
        /// </summary>
        internal GameModule CreateModule()
        {
            return GameModuleUtility.CreateModuleForExactType(ModuleType, m_Factory);
        }

        public override string ToString()
        {
            return ModuleTypeName;
        }
    }

    /// <summary>
    ///   <para>模块清单的安装顺序</para>
    /// </summary>
    public enum GameModuleInstallOrder : byte
    {
        /// <summary>
        ///   <para>按模块依赖关系自动整理安装顺序</para>
        /// </summary>
        Dependency = 0,
        /// <summary>
        ///   <para>按清单声明顺序安装；依赖必须已经出现在更早的条目中</para>
        /// </summary>
        Declared = 1,
    }

    /// <summary>
    ///   <para>游戏模块清单</para>
    /// </summary>
    [Serializable]
    public sealed class GameModuleManifest
    {
        /// <summary>
        ///   <para>拓扑排序中的访问状态</para>
        /// </summary>
        private enum VisitState : byte
        {
            Visiting = 0,
            Done = 1,
        }

        /// <summary>
        ///   <para>一次清单安装中准备好的模块项</para>
        /// </summary>
        internal readonly struct InstallItem
        {
            public readonly GameModule module;
            public readonly Type moduleType;
            public readonly RuntimeTypeHandle moduleTypeHandle;
            public readonly Type[] dependencies;

            public InstallItem(GameModule module, Type[] dependencies)
            {
                this.module = module ?? throw new ArgumentNullException(nameof(module));
                moduleType = module.GetType();
                moduleTypeHandle = moduleType.TypeHandle;
                this.dependencies = dependencies ?? Array.Empty<Type>();
            }
        }

        /// <summary>
        ///   <para>非递归拓扑排序时使用的访问栈帧</para>
        /// </summary>
        [Serializable]
        private struct VisitFrame
        {
            /// <summary>
            ///   <para>当前处理中的模块项索引</para>
            /// </summary>
            public int itemIndex;
            /// <summary>
            ///   <para>当前已处理到的依赖索引</para>
            /// </summary>
            public int dependencyIndex;
            /// <summary>
            ///   <para>当前模块项是否已经进入访问流程</para>
            /// </summary>
            public bool entered;
        }

        /// <summary>
        ///   <para>清单中的模块描述符列表</para>
        /// </summary>
        private readonly List<GameModuleDescriptor> m_Descriptors = new();

        /// <summary>
        ///   <para>清单描述符列表引用</para>
        /// </summary>
        private readonly IReadOnlyList<GameModuleDescriptor> m_DescriptorList;

        /// <summary>
        ///   <para>模块类型句柄到描述符</para>
        /// </summary>
        private readonly Dictionary<RuntimeTypeHandle, GameModuleDescriptor> m_ByType = new();

        /// <summary>
        ///   <para>清单安装顺序</para>
        /// </summary>
        private readonly GameModuleInstallOrder m_InstallOrder;
        
        /// <summary>
        ///   <para>清单内模块描述符</para>
        /// </summary>
        public IReadOnlyList<GameModuleDescriptor> Descriptors => m_DescriptorList;
        
        /// <summary>
        ///   <para>清单安装顺序</para>
        /// </summary>
        public GameModuleInstallOrder InstallOrder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_InstallOrder;
        }

        /// <summary>
        ///   <para>清单模块数量</para>
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Descriptors.Count;
        }

        public GameModuleManifest(GameModuleInstallOrder installOrder = GameModuleInstallOrder.Dependency)
        {
            m_DescriptorList = m_Descriptors.AsReadOnly();
            m_InstallOrder = installOrder;
        }

        /// <summary>
        ///   <para>添加模块描述符</para>
        /// </summary>
        /// <param name="factory">模块创建工厂</param>
        /// <param name="replace">是否替换同精确类型下已有的描述符</param>
        public GameModuleDescriptor Add<T>(Func<T> factory, bool replace = false)
            where T : GameModule
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var descriptor = new GameModuleDescriptor(typeof(T), factory);
            Add(descriptor, replace);
            return descriptor;
        }

        /// <summary>
        ///   <para>添加模块描述符并使用无参构造函数创建模块实例</para>
        /// </summary>
        /// <param name="replace">是否替换同精确类型下已有的描述符</param>
        public GameModuleDescriptor Add<T>(bool replace = false)
            where T : GameModule, new()
        {
            var descriptor = new GameModuleDescriptor(typeof(T), static () => new T());
            Add(descriptor, replace);
            return descriptor;
        }

        /// <summary>
        ///   <para>添加模块描述符</para>
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <param name="factory">模块创建工厂</param>
        /// <param name="replace">是否替换已存在描述符</param>
        public GameModuleDescriptor Add(Type moduleType, Func<GameModule> factory, bool replace = false)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var descriptor = new GameModuleDescriptor(moduleType, factory);
            Add(descriptor, replace);
            return descriptor;
        }

        /// <summary>
        ///   <para>把模块描述符加入清单</para>
        /// </summary>
        /// <param name="descriptor">模块描述符</param>
        /// <param name="replace">是否替换已存在描述符</param>
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
        ///   <para>移除指定泛型类型对应的模块描述符</para>
        /// </summary>
        public bool Remove<T>() where T : GameModule => Remove(typeof(T));

        /// <summary>
        ///   <para>检查清单是否包含指定泛型模块类型</para>
        /// </summary>
        public bool Contains<T>() where T : GameModule => Contains(typeof(T));

        /// <summary>
        ///   <para>移除指定类型对应的模块描述符</para>
        /// </summary>
        /// <param name="moduleType">模块类型</param>
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
        ///   <para>检查清单是否包含指定模块类型</para>
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        public bool Contains(Type moduleType)
        {
            if (moduleType == null) return false;
            return m_ByType.ContainsKey(moduleType.TypeHandle);
        }

        /// <summary>
        ///   <para>尝试获取指定类型对应的模块描述符</para>
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <param name="descriptor">模块描述符</param>
        public bool TryGetDescriptor(Type moduleType, out GameModuleDescriptor descriptor)
        {
            descriptor = null;
            if (moduleType == null) return false;
            return m_ByType.TryGetValue(moduleType.TypeHandle, out descriptor);
        }

        /// <summary>
        ///   <para>尝试获取指定泛型类型对应的模块描述符</para>
        /// </summary>
        public bool TryGetDescriptor<T>(out GameModuleDescriptor descriptor)
            where T : GameModule
        {
            return TryGetDescriptor(typeof(T), out descriptor);
        }

        /// <summary>
        ///   <para>清空清单中的全部模块描述符</para>
        /// </summary>
        public void Clear()
        {
            if (m_Descriptors.Count == 0) return;
            m_Descriptors.Clear();
            m_ByType.Clear();
        }

        /// <summary>
        ///   <para>创建一次清单安装所需的模块项，并按当前清单策略决定最终顺序</para>
        /// </summary>
        internal IReadOnlyList<InstallItem> CreateInstallItems()
        {
            if (m_Descriptors.Count == 0)
            {
                return Array.Empty<InstallItem>();
            }

            var installItems = CreatePreparedInstallItems();

            try
            {
                return InstallOrder == GameModuleInstallOrder.Declared
                    ? ValidateDeclaredInstallItems(installItems)
                    : OrderInstallItems(installItems);
            }
            catch (Exception ex)
            {
                var failure = GameModuleUtility.CombineErrors(ex, DisposePreparedItems(installItems));
                ExceptionDispatchInfo.Capture(failure).Throw();
                throw;
            }
        }

        /// <summary>
        ///   <para>创建本次清单安装所需的全部安装项</para>
        /// </summary>
        private List<InstallItem> CreatePreparedInstallItems()
        {
            var installItems = new List<InstallItem>(m_Descriptors.Count);
            try
            {
                for (int i = 0; i < m_Descriptors.Count; i++)
                {
                    installItems.Add(CreateInstallItem(m_Descriptors[i]));
                }
            }
            catch (Exception ex)
            {
                var failure = GameModuleUtility.CombineErrors(ex, DisposePreparedItems(installItems));
                ExceptionDispatchInfo.Capture(failure).Throw();
                throw;
            }

            return installItems;
        }

        /// <summary>
        ///   <para>创建单个安装项并读取其声明的依赖</para>
        /// </summary>
        private static InstallItem CreateInstallItem(GameModuleDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            GameModule module = null;
            try
            {
                module = descriptor.CreateModule();
                var entry = new InstallItem(module, GameModuleDependencyUtility.GetDependencies(module));
                module = null;
                return entry;
            }
            catch (Exception ex)
            {
                var failure = GameModuleUtility.CombineErrors(
                    ex,
                    GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after manifest item creation failed"));
                ExceptionDispatchInfo.Capture(failure).Throw();
                throw;
            }
        }

        /// <summary>
        ///   <para>按依赖关系整理安装项顺序</para>
        /// </summary>
        private static InstallItem[] OrderInstallItems(List<InstallItem> installItems)
        {
            var orderedItems = new List<InstallItem>(installItems.Count);
            var visitState = new Dictionary<RuntimeTypeHandle, VisitState>(installItems.Count);
            var stack = new List<VisitFrame>(installItems.Count);
            var exactTypeIndexMap = CreateExactTypeIndexMap(installItems);

            for (int i = 0; i < installItems.Count; i++)
            {
                AppendOrderedItemsFrom(i, installItems, exactTypeIndexMap, orderedItems, visitState, stack);
            }

            return orderedItems.ToArray();
        }

        /// <summary>
        ///   <para>校验当前声明顺序是否已经满足依赖要求，并直接返回当前顺序</para>
        /// </summary>
        private static InstallItem[] ValidateDeclaredInstallItems(List<InstallItem> installItems)
        {
            if (installItems.Count == 0)
            {
                return Array.Empty<InstallItem>();
            }

            var exactTypeIndexMap = CreateExactTypeIndexMap(installItems);

            for (int i = 0; i < installItems.Count; i++)
            {
                var item = installItems[i];
                for (int j = 0; j < item.dependencies.Length; j++)
                {
                    if (!TryFindDependencyItemIndex(
                            item.moduleType,
                            item.moduleTypeHandle,
                            item.dependencies[j],
                            installItems,
                            exactTypeIndexMap,
                            i,
                            out _,
                            out var error))
                    {
                        throw new InvalidOperationException(
                            $"{nameof(GameModuleManifest)} declared install order is invalid at {item.moduleType.FullName}. {error}");
                    }
                }

            }

            return installItems.ToArray();
        }

        /// <summary>
        ///   <para>从指定根安装项开始追加依赖有序结果</para>
        /// </summary>
        private static void AppendOrderedItemsFrom(
            int rootIndex,
            List<InstallItem> installItems,
            Dictionary<RuntimeTypeHandle, int> exactTypeIndexMap,
            List<InstallItem> orderedItems,
            Dictionary<RuntimeTypeHandle, VisitState> visitState,
            List<VisitFrame> stack)
        {
            var rootEntry = installItems[rootIndex];
            var rootHandle = rootEntry.moduleTypeHandle;
            if (visitState.TryGetValue(rootHandle, out var rootState))
            {
                if (rootState == VisitState.Visiting)
                {
                    throw new InvalidOperationException($"Module dependency cycle detected at {rootEntry.moduleType.FullName}.");
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
                var item = installItems[frame.itemIndex];
                var moduleType = item.moduleType;
                var moduleHandle = item.moduleTypeHandle;

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

                var dependencies = item.dependencies;
                if (frame.dependencyIndex < dependencies.Length)
                {
                    var dependencyType = dependencies[frame.dependencyIndex];
                    frame.dependencyIndex++;
                    stack[topIndex] = frame;

                    if (!TryFindDependencyItemIndex(
                            moduleType,
                            moduleHandle,
                            dependencyType,
                            installItems,
                            exactTypeIndexMap,
                            installItems.Count,
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
                orderedItems.Add(item);
                stack.RemoveAt(topIndex);
            }
        }

        /// <summary>
        ///   <para>构建“精确模块类型 -> 清单索引”的查找表</para>
        /// </summary>
        private static Dictionary<RuntimeTypeHandle, int> CreateExactTypeIndexMap(List<InstallItem> installItems)
        {
            var map = new Dictionary<RuntimeTypeHandle, int>(installItems.Count);

            for (int i = 0; i < installItems.Count; i++)
            {
                map[installItems[i].moduleTypeHandle] = i;
            }

            return map;
        }

        /// <summary>
        ///   <para>解析某个依赖类型对应的安装项索引</para>
        /// </summary>
        private static bool TryFindDependencyItemIndex(
            Type moduleType,
            RuntimeTypeHandle moduleTypeHandle,
            Type dependencyType,
            List<InstallItem> installItems,
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
                itemIndex < installItems.Count &&
                !installItems[itemIndex].moduleTypeHandle.Equals(moduleTypeHandle))
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

            for (int i = 0; i < installItems.Count; i++)
            {
                var candidateItem = installItems[i];
                var candidateType = candidateItem.moduleType;
                if (candidateItem.moduleTypeHandle.Equals(moduleTypeHandle)) continue;
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

        /// <summary>
        ///   <para>释放已经创建但尚未交给容器安装流程的模块项</para>
        /// </summary>
        private static Exception DisposePreparedItems(List<InstallItem> installItems)
        {
            if (installItems.Count == 0) return null;

            List<Exception> errors = null;
            for (int i = installItems.Count - 1; i >= 0; i--)
            {
                GameModuleUtility.AddError(
                    ref errors,
                    GameModuleUtility.DisposeModuleAndCreateFailure(
                        installItems[i].module,
                        "disposing module after manifest item creation failed"));
            }

            return GameModuleUtility.ToCombinedError(errors);
        }
    }
}