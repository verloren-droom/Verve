// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Threading;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>组件类型注册表；为运行时掩码提供进程内索引。</para>
    /// </summary>
    /// <remarks>该索引只用于当前进程内存布局，不能写入网络包或存档。</remarks>
    internal static class ComponentTypeRegistry
    {
        /// <summary>
        ///   <para>下一个可分配的组件类型标识。</para>
        /// </summary>
        private static int s_NextTypeId = 1;
        /// <summary>
        ///   <para>组件类型到进程内标识的映射。</para>
        /// </summary>
        private static readonly Dictionary<Type, ComponentTypeId> s_TypeToId = new(128);
        /// <summary>
        ///   <para>进程内标识到组件类型的映射。</para>
        /// </summary>
        private static readonly Dictionary<int, Type> s_IdToType = new(128);
        /// <summary>
        ///   <para>保护组件类型注册表的锁。</para>
        /// </summary>
        private static readonly object s_Lock = new();
        /// <summary>
        ///   <para>TypeCount；已注册的组件类型数量。</para>
        /// </summary>
        public static int TypeCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { lock (s_Lock) return s_TypeToId.Count; }
        }

        /// <summary>
        ///   <para>获取或注册组件类型标识。</para>
        /// </summary>
        /// <param name="componentType">值类型组件。</param>
        public static ComponentTypeId GetTypeId(Type componentType)
        {
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));
            if (!typeof(IComponent).IsAssignableFrom(componentType))
                throw new ArgumentException($"Type {componentType.Name} must implement IComponent");
            if (!componentType.IsValueType)
                throw new ArgumentException($"Component {componentType.Name} must be a struct");

            lock (s_Lock)
            {
                if (s_TypeToId.TryGetValue(componentType, out var typeId))
                    return typeId;

                var id = Interlocked.Increment(ref s_NextTypeId) - 1;
                typeId = new ComponentTypeId(id);

                s_TypeToId[componentType] = typeId;
                s_IdToType[id] = componentType;

                return typeId;
            }
        }

        /// <summary>
        ///   <para>获取组件类型；标识不存在时返回 <see langword="null"/>。</para>
        /// </summary>
        /// <param name="typeId">进程内组件类型标识。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType(int typeId)
        {
            lock (s_Lock) { return s_IdToType.TryGetValue(typeId, out var type) ? type : null; }
        }

        /// <summary>
        ///   <para>预先注册组件类型。</para>
        /// </summary>
        /// <param name="componentTypes">待注册的组件类型。</param>
        public static void PreRegister(params Type[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0) return;

            lock (s_Lock)
            {
                foreach (var type in componentTypes)
                {
                    if (type != null && !s_TypeToId.ContainsKey(type))
                        GetTypeId(type);
                }
            }
        }

        /// <summary>
        ///   <para>清空调试环境中的类型映射。</para>
        /// </summary>
        [Conditional("DEBUG"), Conditional("UNITY_EDITOR")]
        public static void Clear()
        {
            lock (s_Lock)
            {
                s_TypeToId.Clear();
                s_IdToType.Clear();
                s_NextTypeId = 1;
            }
        }
    }

    /// <summary>
    ///   <para>组件类型注册表。</para>
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    internal static class ComponentTypeRegistry<T> where T : struct, IComponent
    {
        /// <summary>
        ///   <para>组件类型的惰性注册标识。</para>
        /// </summary>
        public static readonly ComponentTypeId id = ComponentTypeRegistry.GetTypeId(typeof(T));
    }
}
