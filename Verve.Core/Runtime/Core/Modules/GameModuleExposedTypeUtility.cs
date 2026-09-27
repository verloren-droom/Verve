namespace Verve
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>模块公开类型工具类。</para>
    /// </summary>
    internal static class GameModuleExposedTypeUtility
    {
        /// <summary>
        ///   <para>模块类型到公开类型列表的缓存。</para>
        /// </summary>
        private static readonly Dictionary<RuntimeTypeHandle, Type[]> s_ExposedTypeCache = new();

        /// <summary>
        ///   <para>保护公开类型缓存并发访问的锁。</para>
        /// </summary>
        private static readonly object s_CacheLock = new();

        /// <summary>
        ///   <para>获取模块对外公开的抽象类型列表。</para>
        /// </summary>
        /// <param name="moduleType">目标模块类型。</param>
        private static Type[] GetExposedTypes(Type moduleType)
        {
            if (moduleType == null || !typeof(IGameModule).IsAssignableFrom(moduleType))
            {
                return Array.Empty<Type>();
            }

            var handle = moduleType.TypeHandle;
            lock (s_CacheLock)
            {
                if (s_ExposedTypeCache.TryGetValue(handle, out var cached))
                {
                    return cached;
                }

                var created = BuildExposedTypes(moduleType);
                s_ExposedTypeCache[handle] = created;
                return created;
            }
        }

        /// <summary>
        ///   <para>获取适合调试输出的公开类型文本。</para>
        /// </summary>
        /// <param name="moduleType">目标模块类型。</param>
        internal static string GetExposedTypesText(Type moduleType)
        {
            var exposedTypes = GetExposedTypes(moduleType);
            if (exposedTypes.Length == 0)
            {
                return GameModuleUtility.NoneText;
            }

            var parts = new string[exposedTypes.Length];
            for (int i = 0; i < exposedTypes.Length; i++)
            {
                parts[i] = GameModuleUtility.GetTypeDisplayName(exposedTypes[i]);
            }

            return string.Join(GameModuleUtility.TextListSeparator, parts);
        }

        /// <summary>
        ///   <para>构建模块的抽象公开类型列表。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        private static Type[] BuildExposedTypes(Type moduleType)
        {
            var results = new List<Type>(4);
            var seen = new HashSet<RuntimeTypeHandle>();

            var interfaces = moduleType.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                var exposedType = interfaces[i];
                if (!IsExposedType(moduleType, exposedType)) continue;
                if (seen.Add(exposedType.TypeHandle))
                {
                    results.Add(exposedType);
                }
            }

            for (var baseType = moduleType.BaseType; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
            {
                if (!IsExposedType(moduleType, baseType)) continue;
                if (seen.Add(baseType.TypeHandle))
                {
                    results.Add(baseType);
                }
            }

            if (results.Count == 0)
            {
                return Array.Empty<Type>();
            }

            results.Sort(static (left, right) => string.CompareOrdinal(left?.FullName, right?.FullName));
            return results.ToArray();
        }

        /// <summary>
        ///   <para>判断给定类型是否可以作为模块公开类型对外暴露。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="exposedType">公开类型。</param>
        private static bool IsExposedType(Type moduleType, Type exposedType)
        {
            if (exposedType == null || exposedType == moduleType)
            {
                return false;
            }

            if (exposedType == typeof(IGameModule) || exposedType == typeof(GameModule))
            {
                return false;
            }

            if (exposedType.IsGenericTypeDefinition || exposedType.ContainsGenericParameters)
            {
                return false;
            }

            if (!typeof(IGameModule).IsAssignableFrom(exposedType))
            {
                return false;
            }

            return exposedType.IsInterface || exposedType.IsAbstract;
        }
    }
}