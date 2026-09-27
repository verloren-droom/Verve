// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>组件池接口；定义组件存储的生命周期和容量操作。</para>
    /// </summary>
    internal interface IComponentPool : IDisposable
    {
        /// <summary>
        ///   <para>TypeId；组件的进程内类型标识。</para>
        /// </summary>
        int TypeId { get; }

        /// <summary>
        ///   <para>Count；当前存储的组件数量。</para>
        /// </summary>
        int Count { get; }

        /// <summary>
        ///   <para>Capacity；当前稠密存储容量。</para>
        /// </summary>
        int Capacity { get; }

        /// <summary>
        ///   <para>判断行动者是否拥有该组件。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        bool Has(Actor actor);

        /// <summary>
        ///   <para>移除行动者的组件。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        bool Remove(Actor actor);

        /// <summary>
        ///   <para>移除池中所有组件。</para>
        /// </summary>
        void Clear();

        /// <summary>
        ///   <para>确保池容量至少达到指定值。</para>
        /// </summary>
        /// <param name="capacity">所需容量。</param>
        void EnsureCapacity(int capacity);
    }

    /// <summary>
    ///   <para>组件池。</para>
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    internal sealed class ComponentPool<T> : IComponentPool where T : struct, IComponent
    {
        /// <summary>
        ///   <para>组件池条目；保存组件数据和行动者版本。</para>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct Entry
        {
            /// <summary>
            ///   <para>组件数据。</para>
            /// </summary>
            public T value;
            /// <summary>
            ///   <para>拥有该槽位的行动者版本。</para>
            /// </summary>
            public int actorVersion;
        }

        /// <summary>
        ///   <para>是否启用并发访问保护。</para>
        /// </summary>
        private readonly bool m_EnableStorageThreadSafety;

        /// <summary>
        ///   <para>组件及拥有者版本的稠密存储。</para>
        /// </summary>
        private Entry[] m_Entries;
        /// <summary>
        ///   <para>行动者索引到稠密槽位的反向映射。</para>
        /// </summary>
        private int[] m_Sparse;
        /// <summary>
        ///   <para>稠密槽位到行动者索引的映射。</para>
        /// </summary>
        private int[] m_Dense;
        /// <summary>
        ///   <para>有效槽位数量；组件始终紧密排列在数组前段。</para>
        /// </summary>
        private int m_Count;
        /// <summary>
        ///   <para>当前组件数据容量。</para>
        /// </summary>
        private int m_Capacity;
        /// <summary>
        ///   <para>保护池数据及其索引。</para>
        /// </summary>
        private SpinLock m_Lock;
        /// <summary>
        ///   <para>TypeId；组件的进程内类型标识。</para>
        /// </summary>
        public ComponentTypeId TypeId => ComponentTypeRegistry<T>.id;
        /// <inheritdoc />
        int IComponentPool.TypeId => TypeId;
        /// <inheritdoc />
        public int Count => m_Count;
        /// <inheritdoc />
        public int Capacity => m_Capacity;

        /// <summary>
        ///   <para>创建组件池并分配初始存储。</para>
        /// </summary>
        /// <param name="initialCapacity">初始容量。</param>
        /// <param name="enableStorageThreadSafety">是否启用并发保护。</param>
        public ComponentPool(int initialCapacity = 64, bool enableStorageThreadSafety = true)
        {
            m_EnableStorageThreadSafety = enableStorageThreadSafety;
            m_Capacity = Math.Max(initialCapacity, 16);
            m_Entries = new Entry[m_Capacity];
            m_Sparse = new int[m_Capacity];
            m_Dense = new int[m_Capacity];
            m_Sparse.AsSpan().Fill(-1);
            m_Lock = new SpinLock(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);
                m_Entries = null;
                m_Sparse = null;
                m_Dense = null;
                m_Count = 0;
                m_Capacity = 0;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        ///   <para>抛出行动者索引不存在的异常。</para>
        /// </summary>
        /// <param name="actor">未找到的行动者。</param>
        private static void ThrowActorNotFound(Actor actor) =>
            throw new InvalidOperationException($"Actor {actor} not found");

        /// <summary>
        ///   <para>抛出行动者没有组件的异常。</para>
        /// </summary>
        /// <param name="actor">未持有组件的行动者。</param>
        private static void ThrowComponentNotFound(Actor actor) =>
            throw new InvalidOperationException($"Component not found for actor {actor}");

        /// <summary>
        ///   <para>抛出组件槽位版本不匹配的异常。</para>
        /// </summary>
        /// <param name="actor">版本无效的行动者。</param>
        private static void ThrowComponentInvalid(Actor actor) =>
            throw new InvalidOperationException($"Component not valid for actor {actor}");

        /// <summary>
        ///   <para>添加组件；已有组件时返回其引用。</para>
        /// </summary>
        /// <param name="actor">组件所属行动者。</param>
        public ref T Add(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                EnsureSparseCapacity(actorIndex + 1);

                int existingDenseIndex = m_Sparse[actorIndex];
                if (existingDenseIndex >= 0 && existingDenseIndex < m_Count && m_Dense[existingDenseIndex] == actorIndex)
                {
                    ref var existing = ref m_Entries[existingDenseIndex];
                    if (existing.actorVersion == actor.Version)
                        return ref existing.value;

                    existing.value = default;
                    existing.actorVersion = actor.Version;
                    return ref existing.value;
                }

                if (m_Count >= m_Capacity)
                    Resize(m_Capacity * 2);

                int denseIndex = m_Count++;
                m_Sparse[actorIndex] = denseIndex;
                m_Dense[denseIndex] = actorIndex;

                ref var entry = ref m_Entries[denseIndex];
                entry.value = default;
                entry.actorVersion = actor.Version;

                return ref entry.value;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        ///   <para>获取行动者组件的可写引用。</para>
        /// </summary>
        /// <param name="actor">组件所属行动者。</param>
        public ref T Get(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length)
                    ThrowActorNotFound(actor);

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count)
                    ThrowComponentNotFound(actor);

                ref var entry = ref m_Entries[denseIndex];
                if (entry.actorVersion != actor.Version)
                    ThrowComponentInvalid(actor);

                return ref entry.value;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        ///   <para>尝试获取行动者组件副本。</para>
        /// </summary>
        /// <param name="actor">组件所属行动者。</param>
        /// <param name="component">获取到的组件；失败时为默认值。</param>
        public bool TryGet(Actor actor, out T component)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);

                component = default;
                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length) return false;

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count) return false;

                ref var entry = ref m_Entries[denseIndex];
                if (entry.actorVersion != actor.Version) return false;

                component = entry.value;
                return true;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length) return false;

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count) return false;

                ref var entry = ref m_Entries[denseIndex];
                return entry.actorVersion == actor.Version;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <inheritdoc />
        public bool Remove(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length) return false;

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count) return false;

                ref var entry = ref m_Entries[denseIndex];
                if (entry.actorVersion != actor.Version) return false;

                int lastIndex = --m_Count;
                if (denseIndex != lastIndex)
                {
                    m_Entries[denseIndex] = m_Entries[lastIndex];
                    int movedActorIndex = m_Dense[lastIndex];
                    m_Dense[denseIndex] = movedActorIndex;
                    m_Sparse[movedActorIndex] = denseIndex;
                }

                m_Sparse[actorIndex] = -1;
                m_Entries[lastIndex] = default;

                return true;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        ///   <para>覆盖行动者已有组件数据。</para>
        /// </summary>
        /// <param name="actor">组件所属行动者。</param>
        /// <param name="component">新的组件数据。</param>
        public void Set(Actor actor, in T component)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length)
                    ThrowActorNotFound(actor);

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count)
                    ThrowComponentNotFound(actor);

                ref var entry = ref m_Entries[denseIndex];
                if (entry.actorVersion != actor.Version)
                    ThrowComponentInvalid(actor);

                entry.value = component;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);

                if (m_Count > 0)
                {
                    for (int i = 0; i < m_Count; i++)
                    {
                        int actorIndex = m_Dense[i];
                        m_Sparse[actorIndex] = -1;
                    }

                    Array.Clear(m_Entries, 0, m_Count);
                    Array.Clear(m_Dense, 0, m_Count);

                    m_Count = 0;
                }
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <inheritdoc />
        public void EnsureCapacity(int capacity)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);
                if (capacity > m_Capacity) Resize(capacity);
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        ///   <para>复制稠密存储中的行动者；开销仅与匹配数量相关。</para>
        /// </summary>
        /// <param name="result">接收行动者的列表。</param>
        internal void CopyActors(List<Actor> result)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_Lock.Enter(ref lockTaken);
                for (int i = 0; i < m_Count; i++)
                    result.Add(new Actor(m_Dense[i], m_Entries[i].actorVersion));
            }
            finally { if (lockTaken) m_Lock.Exit(); }
        }

        /// <summary>
        ///   <para>扩展稠密数组并保留现有组件。</para>
        /// </summary>
        /// <param name="newCapacity">新的容量。</param>
        private void Resize(int newCapacity)
        {
            newCapacity = Math.Max(newCapacity, 16);
            if (newCapacity == m_Capacity) return;

            Array.Resize(ref m_Entries, newCapacity);
            Array.Resize(ref m_Dense, newCapacity);
            m_Capacity = newCapacity;
        }

        /// <summary>
        ///   <para>扩展稀疏索引并初始化新增项。</para>
        /// </summary>
        /// <param name="requiredCapacity">所需索引容量。</param>
        private void EnsureSparseCapacity(int requiredCapacity)
        {
            if (requiredCapacity <= m_Sparse.Length) return;

            int oldLength = m_Sparse.Length;
            int newCapacity = Math.Max(oldLength * 2, requiredCapacity);
            Array.Resize(ref m_Sparse, newCapacity);

            m_Sparse.AsSpan(oldLength).Fill(-1);
        }
    }
}
