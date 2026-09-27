namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>模块运行时内部共用工具。</para>
    /// </summary>
    internal static class GameModuleUtility
    {
        /// <summary>
        ///   <para>无结果时统一返回的文本。</para>
        /// </summary>
        internal const string NoneText = "None";

        /// <summary>
        ///   <para>调试文本列表分隔符。</para>
        /// </summary>
        internal const string TextListSeparator = ", ";

        /// <summary>
        ///   <para>获取类型显示名称。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetTypeDisplayName(Type type) => type?.FullName ?? type?.Name ?? "Unknown";

        /// <summary>
        ///   <para>获取稳定类型名称。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetStableTypeName(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        }

        /// <summary>
        ///   <para>判断对象是否实现了任一受支持的 Tick 接口。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasTickInterfaces(object system) => system != null && HasTickInterfaces(system.GetType());

        /// <summary>
        ///   <para>判断类型是否实现了任一受支持的 Tick 接口。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasTickInterfaces(Type type)
        {
            if (type == null) return false;

            return
                typeof(IEarlyTick).IsAssignableFrom(type) ||
                typeof(IPhysicsTick).IsAssignableFrom(type) ||
                typeof(IGameplayTick).IsAssignableFrom(type) ||
                typeof(ILateTick).IsAssignableFrom(type);
        }

        /// <summary>
        ///   <para>获取可安装模块类型的校验错误；合法时返回空。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        internal static string GetModuleTypeError(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                return $"{type.FullName} must be a concrete non-generic {nameof(GameModule)} class.";
            }

            if (IsExternallyManagedType(type))
            {
                return GetExternallyManagedTypeError(type);
            }

            if (!typeof(GameModule).IsAssignableFrom(type))
            {
                return $"{type.FullName} must inherit {nameof(GameModule)}.";
            }

            if (HasTickInterfaces(type))
            {
                return GetTickTypeError(type);
            }

            return null;
        }

        /// <summary>
        ///   <para>获取模块公开类型的校验错误。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static string GetExposedTypeError(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (!typeof(IGameModule).IsAssignableFrom(type))
            {
                return $"{type.FullName} must implement {nameof(IGameModule)}.";
            }

            if (type.ContainsGenericParameters)
            {
                return $"{type.FullName} cannot be an open generic type.";
            }

            if (IsExternallyManagedType(type))
            {
                return GetExternallyManagedTypeError(type);
            }

            if (HasTickInterfaces(type))
            {
                return GetTickTypeError(type);
            }

            if (type.IsClass && !type.IsAbstract)
            {
                return $"{type.FullName} must be an interface or abstract {nameof(IGameModule)} type.";
            }

            return null;
        }

        /// <summary>
        ///   <para>获取模块依赖类型的校验错误。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        internal static string GetDependencyTypeError(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return type.IsClass && !type.IsAbstract
                ? GetModuleTypeError(type)
                : GetExposedTypeError(type);
        }

        /// <summary>
        ///   <para>校验给定模块实例满足可安装模块约束。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        internal static void ThrowIfInvalidModule(GameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (module.IsDisposed)
            {
                throw new ObjectDisposedException(GetTypeDisplayName(module.GetType()));
            }

            var error = GetModuleTypeError(module.GetType());
            if (error != null)
            {
                throw new InvalidOperationException(error);
            }
        }

        /// <summary>
        ///   <para>生成“模块/依赖类型直接实现 Tick 接口”错误文本。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static string GetTickTypeError(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return
                $"{type.FullName} cannot implement Tick interfaces directly. " +
                $"Register owned tick objects through {nameof(GameModuleContext)} instead. " +
                $"Supported Tick interfaces: {GetTickInterfaceNames()}.";
        }

        /// <summary>
        ///   <para>生成“模块类型已由外部系统托管”错误文本。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static string GetExternallyManagedTypeError(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return
                $"{type.FullName} cannot use a type whose lifetime is already managed elsewhere. " +
                $"Use a plain {nameof(GameModule)} and register any external runtime objects through {nameof(GameModuleContext)} instead. " +
                $"Externally managed types cannot be installed or exposed as game modules.";
        }

        /// <summary>
        ///   <para>判断类型是否已由外部系统托管。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsExternallyManagedType(Type type)
        {
#if UNITY_5_3_OR_NEWER
            return type != null && typeof(UnityEngine.Object).IsAssignableFrom(type);
#else
            return false;
#endif
        }

        /// <summary>
        ///   <para>返回框架支持的 Tick 接口名称列表。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetTickInterfaceNames()
        {
            return $"{nameof(IEarlyTick)}/{nameof(IPhysicsTick)}/{nameof(IGameplayTick)}/{nameof(ILateTick)}";
        }

        /// <summary>
        ///   <para>调用事件。</para>
        /// </summary>
        /// <param name="handlers">处理函数。</param>
        /// <param name="argument">事件参数。</param>
        /// <param name="eventName">事件名称。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        internal static void InvokeEvent<T>(Action<T> handlers, T argument, string eventName)
        {
            if (handlers == null) return;

            List<Exception> errors = null;
            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(argument);
                }
                catch (Exception ex)
                {
                    ExceptionUtility.Add(ref errors, new InvalidOperationException($"{eventName} listener failed.", ex));
                }
            }

            ExceptionUtility.ThrowIfAny(errors);
        }

        /// <summary>
        ///   <para>释放失败模块并汇总错误。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="operation">操作。</param>
        /// <param name="owner">所属容器。</param>
        internal static Exception DisposeModuleAndCreateFailure(IGameModule module, string operation, GameModules owner = null)
        {
            if (module == null) return null;

            try
            {
                if (owner != null)
                    ((GameModule)module).DisposeOwned(owner);
                else if (module is IDisposable disposable)
                    disposable.Dispose();

                return null;
            }
            catch (Exception ex)
            {
                return new InvalidOperationException(
                    $"Module {GetTypeDisplayName(module.GetType())} failed while {operation}.",
                    ex);
            }
        }

        /// <summary>
        ///   <para>创建模块。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        internal static GameModule CreateModule(Func<GameModule> factory) => CreateModuleImpl(factory, null);

        /// <summary>
        ///   <para>创建指定精确类型的模块。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="factory">模块创建工厂。</param>
        internal static GameModule CreateModuleForExactType(Type moduleType, Func<GameModule> factory)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            return CreateModuleImpl(factory, moduleType);
        }

        /// <summary>
        ///   <para>创建模块。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="expectedType">预期类型。</param>
        private static GameModule CreateModuleImpl(Func<GameModule> factory, Type expectedType)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            GameModule module = null;
            try
            {
                module = factory.Invoke();
                if (module != null && module.HasOwner)
                {
                    // 工厂不能交付借来的实例，连配置和失败清理也不能触碰它。
                    module = null;
                    throw new InvalidOperationException("Module factory returned an instance already owned by a container.");
                }
                if (module == null)
                {
                    throw expectedType == null
                        ? new InvalidOperationException("Module factory returned null.")
                        : new InvalidOperationException($"Module factory returned null. expected={GetTypeDisplayName(expectedType)}");
                }

                if (expectedType != null)
                {
                    var actualType = module.GetType();
                    if (actualType != expectedType)
                    {
                        throw new InvalidOperationException(
                            $"Module instance type mismatch. expected={GetTypeDisplayName(expectedType)}, actual={GetTypeDisplayName(actualType)}");
                    }
                }

                var result = module;
                module = null;
                return result;
            }
            catch (Exception ex)
            {
                var failure = ExceptionUtility.Combine(
                    ex,
                    DisposeModuleAndCreateFailure(
                        module,
                        expectedType == null
                            ? "disposing module after module factory failed"
                            : "disposing module after typed module factory failed"));
                ExceptionUtility.Rethrow(failure);
                throw;
            }
        }
    }
}