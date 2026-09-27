// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>能力类型注册表；为运行时能力查询提供进程内索引。</para>
    /// </summary>
    /// <remarks>该索引只用于当前进程，网络协议应使用独立的稳定 ID。</remarks>
    internal static class CapabilityTypeRegistry
    {
        /// <summary>
        ///   <para>下一个可分配的能力类型 ID。</para>
        /// </summary>
        private static int s_NextTypeId = 1;
        /// <summary>
        ///   <para>能力类型到进程内标识的映射。</para>
        /// </summary>
        private static readonly Dictionary<Type, CapabilityTypeId> s_TypeToId = new(256);
        /// <summary>
        ///   <para>进程内标识到能力类型的映射。</para>
        /// </summary>
        private static readonly Dictionary<int, Type> s_IdToType = new(256);
        /// <summary>
        ///   <para>保护类型映射及 ID 分配的锁。</para>
        /// </summary>
        private static readonly object s_Lock = new();
        /// <summary>
        ///   <para>当前已注册的能力类型数量。</para>
        /// </summary>
        public static int TypeCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { lock (s_Lock) return s_TypeToId.Count; }
        }
        /// <summary>
        ///   <para>获取或注册能力类型的进程内标识。</para>
        /// </summary>
        /// <param name="capabilityType">能力类型。</param>
        public static CapabilityTypeId GetTypeId(Type capabilityType)
        {
            if (capabilityType == null) throw new ArgumentNullException(nameof(capabilityType));
            if (!typeof(Capability).IsAssignableFrom(capabilityType))
                throw new ArgumentException($"Type {capabilityType.Name} must inherit from Capability");

            lock (s_Lock)
            {
                if (s_TypeToId.TryGetValue(capabilityType, out var typeId))
                    return typeId;

                var id = Interlocked.Increment(ref s_NextTypeId) - 1;
                typeId = new CapabilityTypeId(id);

                s_TypeToId[capabilityType] = typeId;
                s_IdToType[id] = capabilityType;

                return typeId;
            }
        }

        /// <summary>
        ///   <para>按进程内标识获取能力类型；未注册时返回 <see langword="null"/>。</para>
        /// </summary>
        /// <param name="typeId">能力类型标识。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType(int typeId)
        {
            lock (s_Lock) { return s_IdToType.TryGetValue(typeId, out var type) ? type : null; }
        }
    }

    /// <summary>
    ///   <para>能力类型注册表。</para>
    /// </summary>
    /// <typeparam name="T">能力类型</typeparam>
    internal static class CapabilityTypeRegistry<T> where T : Capability
    {
        /// <summary>
        ///   <para>泛型能力类型对应的缓存标识。</para>
        /// </summary>
        public static readonly CapabilityTypeId id = CapabilityTypeRegistry.GetTypeId(typeof(T));
    }
}
