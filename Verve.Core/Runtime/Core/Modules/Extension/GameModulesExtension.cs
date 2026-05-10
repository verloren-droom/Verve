namespace Verve
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Collections.Generic;


    /// <summary>
    ///   <para>模块容器扩展类</para>
    /// </summary>
    public static class GameModulesExtension
    {
        /// <summary>
        ///   <para>通过程序集异步安装模块</para>
        /// </summary>
        /// <param name="assemblyData">程序集字节数据</param>
        /// <param name="pdbData">可选调试符号字节数据</param>
        /// <param name="moduleTypeNames">要安装的模块类型名；为空或长度为 0 时安装程序集内所有可安装模块</param>
        public static ValueTask InstallAsync(this GameModules self, byte[] assemblyData, byte[] pdbData = null, params string[] moduleTypeNames)
        {
#if !ENABLE_IL2CPP
            throw new PlatformNotSupportedException("Installing modules from assembly bytes is only supported on IL2CPP runtime.");
#else
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (assemblyData == null) throw new ArgumentNullException(nameof(assemblyData));
            if (assemblyData.Length == 0) throw new ArgumentException("Assembly data cannot be empty.", nameof(assemblyData));

            self.ThrowIfNotAlive();

            var assembly = pdbData == null || pdbData.Length == 0
                ? Assembly.Load(assemblyData)
                : Assembly.Load(assemblyData, pdbData);

            var moduleTypes = ResolveAssemblyModuleTypes(assembly, moduleTypeNames);

            if (moduleTypes.Length == 1)
            {
                return self.InstallAsync(() => (GameModule)Activator.CreateInstance(moduleTypes[0]));
            }

            var manifest = new GameModuleManifest();
            foreach (var moduleType in moduleTypes)
            {
                manifest.Add(moduleType, () => (GameModule)Activator.CreateInstance(moduleType));
            }

            return self.InstallFromManifestAsync(manifest);
#endif
        }

#if ENABLE_IL2CPP
        /// <summary>
        ///   <para>解析程序集内要安装的模块类型</para>
        /// </summary>
        private static Type[] ResolveAssemblyModuleTypes(Assembly assembly, string[] typeNames)
        {
            var assemblyTypes = assembly.GetTypes();
            if (typeNames == null || typeNames.Length == 0)
            {
                var moduleTypes = new List<Type>(assemblyTypes.Length);
                foreach (var type in assemblyTypes)
                {
                    if (GetAssemblyModuleTypeError(type) != null)
                    {
                        continue;
                    }

                    moduleTypes.Add(type);
                }

                if (moduleTypes.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Assembly '{assembly.FullName}' does not contain any installable {nameof(GameModule)} type.");
                }

                return moduleTypes.ToArray();
            }

            var resolvedTypes = new Type[typeNames.Length];
            for (int i = 0; i < typeNames.Length; i++)
            {
                var typeName = typeNames[i]?.Trim();
                if (string.IsNullOrEmpty(typeName))
                {
                    throw new ArgumentException($"Module type name cannot be empty. index={i}", nameof(typeNames));
                }

                Type matchedType = null;
                foreach (var type in assemblyTypes)
                {
                    if (!(string.Equals(type.FullName, typeName, StringComparison.Ordinal) ||
                          string.Equals(type.Name, typeName, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    if (matchedType != null)
                    {
                        throw new InvalidOperationException(
                            $"Type name '{typeName}' matches multiple types in assembly '{assembly.FullName}'. " +
                            "Use a full type name.");
                    }

                    matchedType = type;
                }

                if (matchedType == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to find module type '{typeName}' in assembly '{assembly.FullName}'.");
                }

                var error = GetAssemblyModuleTypeError(matchedType);
                if (error != null)
                {
                    throw new InvalidOperationException(error);
                }

                resolvedTypes[i] = matchedType;
            }

            return resolvedTypes;
        }

        private static string GetAssemblyModuleTypeError(Type moduleType)
        {
            var error = GameModuleUtility.GetModuleTypeError(moduleType);
            if (error != null)
            {
                return error;
            }

            if (moduleType.GetConstructor(Type.EmptyTypes) == null)
            {
                return
                    $"{GameModuleUtility.GetTypeDisplayName(moduleType)} must declare a public parameterless constructor to be installed from assembly data.";
            }

            return null;
        }
#endif
    }
}