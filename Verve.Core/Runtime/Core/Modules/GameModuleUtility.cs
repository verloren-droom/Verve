namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    

    /// <summary>
    ///   <para>模块运行时内部共用工具</para>
    /// </summary>
    internal static class GameModuleUtility
    {
        /// <summary>
        ///   <para>无结果时统一返回的文本</para>
        /// </summary>
        internal const string NoneText = "None";

        /// <summary>
        ///   <para>调试文本列表分隔符</para>
        /// </summary>
        internal const string TextListSeparator = ", ";

        /// <summary>
        ///   <para>按对象引用比较</para>
        /// </summary>
        internal sealed class ReferenceComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(T obj)
            {
                return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetTypeDisplayName(Type type)
        {
            return type?.FullName ?? type?.Name ?? "Unknown";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetStableTypeName(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        }

        /// <summary>
        ///   <para>判断对象是否实现了任一受支持的 Tick 接口</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasTickInterfaces(object system)
        {
            return system != null && HasTickInterfaces(system.GetType());
        }

        /// <summary>
        ///   <para>判断类型是否实现了任一受支持的 Tick 接口</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasTickInterfaces(Type type)
        {
            if (type == null) return false;

            return
#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
                typeof(IEarlyTick).IsAssignableFrom(type) ||
#endif
                typeof(IPhysicsTick).IsAssignableFrom(type) ||
                typeof(IGameplayTick).IsAssignableFrom(type) ||
                typeof(ILateTick).IsAssignableFrom(type);
        }

        /// <summary>
        ///   <para>获取可安装模块类型的校验错误；合法时返回空</para>
        /// </summary>
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
        ///   <para>获取模块公开类型的校验错误</para>
        /// </summary>
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
        ///   <para>获取模块依赖类型的校验错误</para>
        /// </summary>
        internal static string GetDependencyTypeError(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return type.IsClass && !type.IsAbstract
                ? GetModuleTypeError(type)
                : GetExposedTypeError(type);
        }

        /// <summary>
        ///   <para>校验给定模块实例满足可安装模块约束</para>
        /// </summary>
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
        ///   <para>生成“模块/依赖类型直接实现 Tick 接口”错误文本</para>
        /// </summary>
        private static string GetTickTypeError(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return
                $"{type.FullName} cannot implement Tick interfaces directly. " +
                $"Register owned tick objects through {nameof(GameModuleContext)} instead. " +
                $"Supported Tick interfaces: {GetTickInterfaceNames()}.";
        }

        /// <summary>
        ///   <para>生成“模块类型已由外部系统托管”错误文本</para>
        /// </summary>
        private static string GetExternallyManagedTypeError(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return
                $"{type.FullName} cannot use a type whose lifetime is already managed elsewhere. " +
                $"Use a plain {nameof(GameModule)} and register any external runtime objects through {nameof(GameModuleContext)} instead. " +
                $"Externally managed types cannot be installed or exposed as game modules.";
        }

        /// <summary>
        ///   <para>判断类型是否已由外部系统托管</para>
        /// </summary>
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
        ///   <para>返回框架支持的 Tick 接口名称列表</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetTickInterfaceNames()
        {
#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
            return $"{nameof(IEarlyTick)}/{nameof(IPhysicsTick)}/{nameof(IGameplayTick)}/{nameof(ILateTick)}";
#else
            return $"{nameof(IPhysicsTick)}/{nameof(IGameplayTick)}/{nameof(ILateTick)}";
#endif
        }

        internal static void AddError(ref List<Exception> errors, Exception error)
        {
            if (error == null) return;

            errors ??= new List<Exception>();
            if (error is AggregateException aggregate)
            {
                errors.AddRange(aggregate.Flatten().InnerExceptions);
                return;
            }

            errors.Add(error);
        }

        internal static Exception CombineErrors(Exception primary, Exception secondary)
        {
            if (primary == null) return secondary;
            if (secondary == null) return primary;

            var errors = new List<Exception>(4);
            AddError(ref errors, primary);
            AddError(ref errors, secondary);
            return ToCombinedError(errors);
        }

        internal static Exception ToCombinedError(List<Exception> errors)
        {
            if (errors == null || errors.Count == 0) return null;
            return errors.Count == 1 ? errors[0] : new AggregateException(errors);
        }

        internal static void ThrowIfErrors(List<Exception> errors)
        {
            if (errors == null || errors.Count == 0) return;
            if (errors.Count == 1) throw errors[0];
            throw new AggregateException(errors);
        }

        internal static void InvokeEvent(Action handlers, string eventName)
        {
            if (handlers == null) return;

            List<Exception> errors = null;
            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action)handler)?.Invoke();
                }
                catch (Exception ex)
                {
                    AddError(ref errors, new InvalidOperationException($"{eventName} listener failed.", ex));
                }
            }

            ThrowIfErrors(errors);
        }

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
                    AddError(ref errors, new InvalidOperationException($"{eventName} listener failed.", ex));
                }
            }

            ThrowIfErrors(errors);
        }

        internal static Exception DisposeModuleAndCreateFailure(IGameModule module, string operation)
        {
            if (module == null) return null;

            try
            {
                if (module is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                return null;
            }
            catch (Exception ex)
            {
                return new InvalidOperationException(
                    $"Module {GetTypeDisplayName(module.GetType())} failed while {operation}.",
                    ex);
            }
        }

        internal static GameModule CreateModule(Func<GameModule> factory)
        {
            return CreateModuleImpl(factory, null);
        }

        internal static GameModule CreateModuleForExactType(Type moduleType, Func<GameModule> factory)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            return CreateModuleImpl(factory, moduleType);
        }

        private static GameModule CreateModuleImpl(Func<GameModule> factory, Type expectedType)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            GameModule module = null;
            try
            {
                module = factory.Invoke();
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
                var failure = CombineErrors(
                    ex,
                    DisposeModuleAndCreateFailure(
                        module,
                        expectedType == null
                            ? "disposing module after module factory failed"
                            : "disposing module after typed module factory failed"));
                ExceptionDispatchInfo.Capture(failure).Throw();
                throw;
            }
        }
    }
}