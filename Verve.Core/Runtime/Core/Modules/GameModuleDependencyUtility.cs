namespace Verve
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    ///   <para>模块依赖声明工具</para>
    /// </summary>
    internal static class GameModuleDependencyUtility
    {
        /// <summary>
        ///   <para>读取并规范化模块声明的依赖列表</para>
        /// </summary>
        internal static Type[] GetDependencies(IGameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            var moduleType = module.GetType();
            if (module is not IGameModuleDependencies dependencyProvider)
            {
                return Array.Empty<Type>();
            }

            var dependencies = dependencyProvider.Dependencies;
            if (dependencies == null || dependencies.Count == 0)
            {
                return Array.Empty<Type>();
            }

            return NormalizeDependencies(moduleType, dependencies);
        }

        /// <summary>
        ///   <para>尝试创建临时模块实例并读取其依赖列表，随后释放实例</para>
        /// </summary>
        internal static bool TryInspectDependencies(
            Type moduleType,
            string fields,
            out Type[] dependencyTypes,
            out string error)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));

            dependencyTypes = Array.Empty<Type>();
            error = null;

            GameModule module = null;
            bool success = false;
            try
            {
                module = GameModuleManifestAsset.CreateModuleInstance(moduleType, fields);
                dependencyTypes = GetDependencies(module);
                success = true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                if (module != null && !TryDisposeInspectionModule(module, out var disposeError))
                {
                    error = string.IsNullOrWhiteSpace(error)
                        ? disposeError
                        : $"{error} {disposeError}";
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        ///   <para>规范化模块依赖列表</para>
        /// </summary>
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

        private static bool TryDisposeInspectionModule(GameModule module, out string error)
        {
            error = null;
            try
            {
                module.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                error =
                    $"Module {GameModuleUtility.GetTypeDisplayName(module.GetType())} failed while disposing after dependency inspection. " +
                    $"{ex.Message}";
                return false;
            }
        }
    }
}