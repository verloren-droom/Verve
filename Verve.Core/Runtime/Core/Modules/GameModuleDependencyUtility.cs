namespace Verve
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>模块依赖声明工具。</para>
    /// </summary>
    internal static class GameModuleDependencyUtility
    {
        /// <summary>
        ///   <para>缓存锁。</para>
        /// </summary>
        private static readonly object s_CacheLock = new();
        /// <summary>
        ///   <para>依赖缓存。</para>
        /// </summary>
        private static readonly Dictionary<RuntimeTypeHandle, Type[]> s_DependenciesCache = new();

        /// <summary>
        ///   <para>读取并规范化模块类型声明的依赖列表。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        internal static Type[] GetDependencies(Type moduleType)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));

            var moduleTypeHandle = moduleType.TypeHandle;
            lock (s_CacheLock)
            {
                if (s_DependenciesCache.TryGetValue(moduleTypeHandle, out var cachedDependencies))
                {
                    return cachedDependencies;
                }
            }

            var attributes = (GameModuleDependencyAttribute[])Attribute.GetCustomAttributes(
                moduleType,
                typeof(GameModuleDependencyAttribute),
                inherit: true);
            var dependencies = new List<Type>();
            if (attributes != null)
            {
                for (int i = 0; i < attributes.Length; i++)
                {
                    var declaredDependencies = attributes[i]?.dependencies;
                    if (declaredDependencies == null || declaredDependencies.Length == 0) continue;
                    dependencies.AddRange(declaredDependencies);
                }
            }

            var normalizedDependencies = NormalizeDependencies(moduleType, dependencies);
            lock (s_CacheLock)
            {
                s_DependenciesCache[moduleTypeHandle] = normalizedDependencies;
            }

            return normalizedDependencies;
        }

        /// <summary>
        ///   <para>判断请求类型是否可由模块显式声明的依赖解析。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="requestedDependencyType">请求的依赖类型。</param>
        internal static bool CanResolveDeclaredDependency(Type moduleType, Type requestedDependencyType)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            if (requestedDependencyType == null) throw new ArgumentNullException(nameof(requestedDependencyType));

            var error = GameModuleUtility.GetDependencyTypeError(requestedDependencyType);
            if (error != null)
            {
                throw new InvalidOperationException(error);
            }

            var dependencies = GetDependencies(moduleType);
            for (int i = 0; i < dependencies.Length; i++)
            {
                var dependencyType = dependencies[i];
                if (dependencyType == requestedDependencyType || requestedDependencyType.IsAssignableFrom(dependencyType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>规范化模块依赖列表。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="dependencies">依赖。</param>
        private static Type[] NormalizeDependencies(Type moduleType, IReadOnlyList<Type> dependencies)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            if (dependencies == null || dependencies.Count == 0)
            {
                return Array.Empty<Type>();
            }

            var uniqueDependencies = new List<Type>(dependencies.Count);
            var visited = new HashSet<RuntimeTypeHandle>();

            for (int i = 0; i < dependencies.Count; i++)
            {
                var dependencyType = dependencies[i];
                if (dependencyType == null)
                {
                    throw new InvalidOperationException($"Module dependency type cannot be null. module={moduleType.FullName}");
                }

                if (dependencyType == moduleType)
                {
                    throw new InvalidOperationException($"Module cannot depend on itself: {moduleType.FullName}");
                }

                var error = GameModuleUtility.GetDependencyTypeError(dependencyType);
                if (error != null)
                {
                    throw new InvalidOperationException(error);
                }

                if (!visited.Add(dependencyType.TypeHandle))
                {
                    throw new InvalidOperationException(
                        $"Duplicate module dependency detected. module={moduleType.FullName}, dependency={dependencyType.FullName}");
                }

                uniqueDependencies.Add(dependencyType);
            }

            return uniqueDependencies.Count == 0 ? Array.Empty<Type>() : uniqueDependencies.ToArray();
        }
    }
}