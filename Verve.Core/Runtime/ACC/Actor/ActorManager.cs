// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Buffers;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>行动者管理器；拥有行动者、组件存储和实体版本。</para>
    /// </summary>
    [Serializable]
    public sealed class ActorManager
    {
        /// <summary>
        ///   <para>进程内行动者版本序列；避免跨世界和清空后的旧句柄复活。</para>
        /// </summary>
        private static long s_LastVersion;

        /// <summary>
        ///   <para>保护行动者和组件池映射的读写锁。</para>
        /// </summary>
        private readonly ReaderWriterLockSlim m_ActorLock;
        /// <summary>
        ///   <para>组件类型到其稀疏集存储的映射。</para>
        /// </summary>
        private readonly Dictionary<int, IComponentPool> m_ComponentPools;
        /// <summary>
        ///   <para>是否启用行动者数据的并发访问保护。</para>
        /// </summary>
        private readonly bool m_EnableStorageThreadSafety;

        /// <summary>
        ///   <para>当前槽位容量。</para>
        /// </summary>
        private int m_Capacity;
        /// <summary>
        ///   <para>空闲槽位数量。</para>
        /// </summary>
        private int m_FreeCount;
        /// <summary>
        ///   <para>可用槽位索引栈。</para>
        /// </summary>
        private int[] m_FreeList;
        /// <summary>
        ///   <para>扩容比例。</para>
        /// </summary>
        private float m_GrowthFactor;
        /// <summary>
        ///   <para>按槽位存储的行动者状态。</para>
        /// </summary>
        private ActorData[] m_Actors;
        /// <summary>
        ///   <para>保护空闲槽位分配的轻量锁。</para>
        /// </summary>
        private SpinLock m_PoolLock;
        /// <summary>
        ///   <para>释放状态；阻止对象释放后的重复使用。</para>
        /// </summary>
        private bool m_IsDisposed;

        /// <summary>
        ///   <para>当前容量；可容纳的最大行动者数量。</para>
        /// </summary>
        public int Capacity => m_Capacity;
        /// <summary>
        ///   <para>存活行动者数量。</para>
        /// </summary>
        public int AliveActorCount { get; private set; }
        /// <summary>
        ///   <para>空闲槽位数量。</para>
        /// </summary>
        public int FreeActorCount => m_FreeCount;
        /// <summary>
        ///   <para>是否启用存储访问锁；不改变能力调度线程。</para>
        /// </summary>
        public bool EnableStorageThreadSafety => m_EnableStorageThreadSafety;

        /// <summary>
        ///   <para>创建行动者和组件存储。</para>
        /// </summary>
        /// <param name="options">容量与并发配置。</param>
        internal ActorManager(ActorManagerOptions options)
        {
            m_Capacity = options.initialCapacity;
            m_GrowthFactor = options.growthFactor;
            m_EnableStorageThreadSafety = options.enableStorageThreadSafety;

            m_Actors = new ActorData[m_Capacity];
            m_FreeList = new int[m_Capacity];

            m_ActorLock = m_EnableStorageThreadSafety
                ? new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion)
                : null;

            m_PoolLock = new SpinLock(false);
            m_ComponentPools = new Dictionary<int, IComponentPool>(128);

            for (int i = 0; i < m_Capacity; i++)
                m_FreeList[i] = i;

            m_FreeCount = m_Capacity;
        }

        /// <summary>
        ///   <para>释放世界拥有的行动者存储。</para>
        /// </summary>
        internal void Dispose()
        {
            if (m_IsDisposed) return;

            Clear();

            m_ActorLock?.EnterWriteLock();
            try
            {
                bool lockTaken = false;
                try
                {
                    if (m_EnableStorageThreadSafety) m_PoolLock.Enter(ref lockTaken);
                    foreach (var pool in m_ComponentPools.Values)
                        pool.Dispose();
                    m_ComponentPools.Clear();
                }
                finally
                {
                    if (lockTaken) m_PoolLock.Exit();
                }
            }
            finally
            {
                if (m_ActorLock != null)
                {
                    m_ActorLock.ExitWriteLock();
                    m_ActorLock.Dispose();
                }
            }

            m_IsDisposed = true;
        }

        /// <summary>
        ///   <para>预留不会重复的行动者版本范围。</para>
        /// </summary>
        /// <param name="count">预留数量。</param>
        private static int ReserveVersions(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            while (true)
            {
                long current = Volatile.Read(ref s_LastVersion);
                long end = current + count;
                if (end > int.MaxValue)
                    throw new InvalidOperationException("The process has exhausted its actor identity range.");
                if (Interlocked.CompareExchange(ref s_LastVersion, end, current) == current)
                    return (int)(current + 1);
            }
        }

        /// <summary>
        ///   <para>创建容量耗尽异常。</para>
        /// </summary>
        private static void ThrowCapacityExceeded() =>
            throw new InvalidOperationException("Actor capacity exceeded");

        /// <summary>
        ///   <para>创建行动者无效异常。</para>
        /// </summary>
        /// <param name="actor">无效的行动者。</param>
        private static void ThrowActorNotAlive(Actor actor) =>
            throw new InvalidOperationException($"Actor {actor} is not alive");

        /// <summary>
        ///   <para>创建组件缺失异常。</para>
        /// </summary>
        /// <typeparam name="T">缺失的组件类型。</typeparam>
        /// <param name="actor">未持有组件的行动者。</param>
        private static void ThrowComponentNotFound<T>(Actor actor) =>
            throw new InvalidOperationException($"Component {typeof(T).Name} not found for actor {actor}");

        /// <summary>
        ///   <para>分配一个行动者槽位。</para>
        /// </summary>
        internal Actor CreateActor()
        {
            if (m_IsDisposed) throw new ObjectDisposedException(nameof(ActorManager));
            int version = ReserveVersions(1);
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (m_FreeCount == 0)
                {
                    int newCapacity = (int)(m_Capacity * m_GrowthFactor);
                    Resize(Math.Min(newCapacity, ActorManagerOptions.MAX_CAPACITY));
                    if (m_FreeCount == 0) ThrowCapacityExceeded();
                }

                int index = m_FreeList[--m_FreeCount];
                ref var actorData = ref m_Actors[index];

                actorData.version = version;
                actorData.isAlive = true;
                actorData.componentMask.Clear();

                var actor = new Actor(index, actorData.version);
                AliveActorCount++;

                return actor;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>批量分配行动者槽位。</para>
        /// </summary>
        /// <param name="count">要创建的数量。</param>
        internal Actor[] CreateActors(int count)
        {
            if (m_IsDisposed) throw new ObjectDisposedException(nameof(ActorManager));
            if (count < 0 || count > ActorManagerOptions.MAX_CAPACITY) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return Array.Empty<Actor>();
            int version = ReserveVersions(count);
            var actors = new Actor[count];

            m_ActorLock?.EnterWriteLock();
            try
            {
                if (m_FreeCount < count)
                {
                    int newCapacity = Math.Min(
                        Math.Max((int)(m_Capacity * m_GrowthFactor), m_Capacity + count),
                        ActorManagerOptions.MAX_CAPACITY);
                    Resize(newCapacity);
                    if (m_FreeCount < count) ThrowCapacityExceeded();
                }

                for (int i = 0; i < count; i++)
                {
                    int index = m_FreeList[--m_FreeCount];
                    ref var actorData = ref m_Actors[index];

                    actorData.version = version + i;
                    actorData.isAlive = true;
                    actorData.componentMask.Clear();

                    actors[i] = new Actor(index, actorData.version);
                    AliveActorCount++;
                }
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }

            return actors;
        }

        /// <summary>
        ///   <para>回收行动者槽位及其组件数据。</para>
        /// </summary>
        /// <param name="actor">待销毁行动者。</param>
        internal void DestroyActor(Actor actor)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return;

                ref var actorData = ref m_Actors[actor.Index];

                foreach (var typeId in actorData.componentMask)
                    RemoveComponentByTypeIdUnsafe(actor, typeId);

                actorData.capabilities?.Clear();
                actorData.Reset();
                m_FreeList[m_FreeCount++] = actor.Index;
                AliveActorCount--;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>检查行动者是否存活且版本匹配。</para>
        /// </summary>
        /// <param name="actor">待检查行动者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsActorAlive(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try { return IsActorAliveUnsafe(actor); }
            finally { m_ActorLock?.ExitReadLock(); }
        }

        /// <summary>
        ///   <para>将当前存活行动者复制到结果列表。</para>
        /// </summary>
        /// <param name="output">接收结果的列表；调用前内容会被清空。</param>
        internal void GetAliveActors(List<Actor> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            m_ActorLock?.EnterReadLock();
            try
            {
                for (int i = 0; i < m_Capacity; i++)
                {
                    ref var actorData = ref m_Actors[i];
                    if (!actorData.isAlive) continue;
                    output.Add(new Actor(i, actorData.version));
                }
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        /// <summary>
        ///   <para>检查行动者是否拥有指定组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="actor">目标行动者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasComponent<T>(Actor actor)
            where T : struct, IComponent
            => HasComponentByTypeId(actor, ComponentTypeRegistry<T>.id);

        /// <summary>
        ///   <para>按组件类型标识检查行动者是否拥有组件。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="componentTypeId">组件类型标识。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasComponentByTypeId(Actor actor, int componentTypeId)
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                return IsActorAliveUnsafe(actor) &&
                       m_Actors[actor.Index].componentMask.Get(componentTypeId);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        /// <summary>
        ///   <para>获取行动者的组件掩码副本。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentMask GetComponentMask(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try { return IsActorAliveUnsafe(actor) ? m_Actors[actor.Index].componentMask : default; }
            finally { m_ActorLock?.ExitReadLock(); }
        }

        /// <summary>
        ///   <para>获取行动者的标签阻塞掩码副本。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TagBlockMask GetTagBlockMask(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try { return IsActorAliveUnsafe(actor) ? m_Actors[actor.Index].tagBlocks : default; }
            finally { m_ActorLock?.ExitReadLock(); }
        }

        /// <summary>
        ///   <para>获取行动者拥有的能力实例列表。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal List<CapabilityInstance> GetCapabilities(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try { return IsActorAliveUnsafe(actor) ? m_Actors[actor.Index].capabilities : null; }
            finally { m_ActorLock?.ExitReadLock(); }
        }

        /// <summary>
        ///   <para>登记由管理器拥有的能力实例。</para>
        /// </summary>
        /// <param name="actor">能力所属行动者。</param>
        /// <param name="instance">能力实例及其调度信息。</param>
        internal bool AddCapabilityInstance(Actor actor, CapabilityInstance instance)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return false;

                ref var actorData = ref m_Actors[actor.Index];
                actorData.capabilities ??= new List<CapabilityInstance>(16);
                actorData.capabilities.Add(instance);
                return true;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>从行动者移除指定能力实例。</para>
        /// </summary>
        /// <param name="actor">能力所属行动者。</param>
        /// <param name="capability">要移除的能力。</param>
        internal bool RemoveCapabilityInstance(Actor actor, Capability capability)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return false;

                ref var actorData = ref m_Actors[actor.Index];
                if (actorData.capabilities == null) return false;

                for (int i = 0; i < actorData.capabilities.Count; i++)
                {
                    if (actorData.capabilities[i].capability == capability)
                    {
                        actorData.capabilities.RemoveAt(i);
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>为行动者登记一个标签阻塞引用。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="tagId">被阻塞的标签。</param>
        /// <param name="instigator">阻塞引用的所有者。</param>
        internal void BlockTag(Actor actor, TagId tagId, object instigator)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return;
                ref var actorData = ref m_Actors[actor.Index];
                actorData.tagBlocks.BlockTag(tagId, instigator);
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>释放发起者持有的一个标签阻塞引用。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="tagId">要解除阻塞的标签。</param>
        /// <param name="instigator">原阻塞引用的所有者。</param>
        internal void UnblockTag(Actor actor, TagId tagId, object instigator)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return;
                ref var actorData = ref m_Actors[actor.Index];
                actorData.tagBlocks.UnblockTag(tagId, instigator);
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>检查行动者的标签是否处于阻塞状态。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="tagId">待检查的标签。</param>
        internal bool IsTagBlocked(Actor actor, TagId tagId)
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return false;
                ref var actorData = ref m_Actors[actor.Index];
                return actorData.tagBlocks.IsTagBlocked(tagId);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        /// <summary>
        ///   <para>添加或获取行动者的组件引用。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="actor">目标行动者。</param>
        internal ref T AddComponent<T>(Actor actor) where T : struct, IComponent
        {
            var typeId = ComponentTypeRegistry<T>.id;

            m_ActorLock?.EnterUpgradeableReadLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) ThrowActorNotAlive(actor);

                var pool = GetOrCreatePool<T>();

                m_ActorLock?.EnterWriteLock();
                try
                {
                    ref var actorData = ref m_Actors[actor.Index];
                    if (pool.Has(actor))
                    {
                        actorData.componentMask.Add(typeId);
                        return ref pool.Get(actor);
                    }

                    ref var component = ref pool.Add(actor);
                    actorData.componentMask.Add(typeId);

                    return ref component;
                }
                finally
                {
                    m_ActorLock?.ExitWriteLock();
                }
            }
            finally
            {
                m_ActorLock?.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        ///   <para>获取行动者的组件引用；行动者或组件无效时抛出异常。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="actor">目标行动者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref T GetComponent<T>(Actor actor) where T : struct, IComponent
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) ThrowActorNotAlive(actor);

                var pool = GetPool<T>();
                if (pool == null) ThrowComponentNotFound<T>(actor);

                return ref pool.Get(actor);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        /// <summary>
        ///   <para>尝试获取行动者的组件数据。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="actor">目标行动者。</param>
        /// <param name="component">接收组件数据。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetComponent<T>(Actor actor, out T component) where T : struct, IComponent
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                component = default;
                if (!IsActorAliveUnsafe(actor)) return false;

                var pool = GetPool<T>();
                return pool != null && pool.TryGet(actor, out component);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        /// <summary>
        ///   <para>移除行动者的指定组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="actor">目标行动者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool RemoveComponent<T>(Actor actor) where T : struct, IComponent
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return false;
                return RemoveComponentByTypeIdUnsafe(actor, ComponentTypeRegistry<T>.id);
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>写入行动者组件；组件不存在时添加实例。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="actor">目标行动者。</param>
        /// <param name="component">要写入的数据。</param>
        internal void SetComponent<T>(Actor actor, in T component) where T : struct, IComponent
        {
            var typeId = ComponentTypeRegistry<T>.id;

            m_ActorLock?.EnterUpgradeableReadLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) ThrowActorNotAlive(actor);

                var pool = GetOrCreatePool<T>();

                if (pool.Has(actor))
                {
                    pool.Set(actor, component);
                }
                else
                {
                    m_ActorLock?.EnterWriteLock();
                    try
                    {
                        ref var comp = ref pool.Add(actor);
                        comp = component;

                        ref var actorData = ref m_Actors[actor.Index];
                        actorData.componentMask.Add(typeId);
                    }
                    finally
                    {
                        m_ActorLock?.ExitWriteLock();
                    }
                }
            }
            finally
            {
                m_ActorLock?.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        ///   <para>查询拥有指定组件的存活行动者。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        internal IReadOnlyList<Actor> QueryActorsWithComponent<T>() where T : struct, IComponent
        {
            var result = new List<Actor>();
            QueryActorsWithComponentNonAlloc<T>(result);
            return result;
        }

        /// <summary>
        ///   <para>查询同时拥有所有指定组件的存活行动者。</para>
        /// </summary>
        /// <param name="componentTypes">用于筛选的组件类型。</param>
        internal IReadOnlyList<Actor> QueryActorsWithComponents(params Type[] componentTypes)
        {
            var result = new List<Actor>();
            QueryActorsWithComponentsNonAlloc(componentTypes, result);
            return result;
        }

        /// <summary>
        ///   <para>将拥有指定组件的行动者写入复用列表。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="result">接收结果的列表；调用前会清空。</param>
        internal void QueryActorsWithComponentNonAlloc<T>(List<Actor> result) where T : struct, IComponent
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();

            m_ActorLock?.EnterReadLock();
            try
            {
                GetPool<T>()?.CopyActors(result);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        /// <summary>
        ///   <para>将同时拥有所有指定组件的行动者写入复用列表。</para>
        /// </summary>
        /// <param name="componentTypes">用于筛选的组件类型。</param>
        /// <param name="result">接收结果的列表；调用前会清空。</param>
        internal void QueryActorsWithComponentsNonAlloc(Type[] componentTypes, List<Actor> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            if (componentTypes == null) throw new ArgumentNullException(nameof(componentTypes));
            if (componentTypes.Length == 0) return;

            m_ActorLock?.EnterReadLock();
            int[] ids = null;
            try
            {
                ids = ArrayPool<int>.Shared.Rent(componentTypes.Length);
                int idCount = 0;
                for (int k = 0; k < componentTypes.Length; k++)
                {
                    var type = componentTypes[k];
                    if (type == null) throw new ArgumentException("Component types cannot contain null.", nameof(componentTypes));
                    if (!typeof(IComponent).IsAssignableFrom(type))
                        throw new ArgumentException($"Type {type.Name} must implement IComponent");
                    if (!type.IsValueType)
                        throw new ArgumentException($"Component {type.Name} must be a struct");
                    ids[idCount++] = ComponentTypeRegistry.GetTypeId(type);
                }
                for (int i = 0; i < m_Capacity; i++)
                {
                    ref var actorData = ref m_Actors[i];
                    if (!actorData.isAlive) continue;

                    bool allMatch = true;
                    for (int j = 0; j < idCount; j++)
                    {
                        if (!actorData.componentMask.Get(ids[j]))
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch)
                    {
                        result.Add(new Actor(i, actorData.version));
                    }
                }
            }
            finally
            {
                if (ids != null) ArrayPool<int>.Shared.Return(ids, true);
                m_ActorLock?.ExitReadLock();
            }
        }

        /// <summary>
        ///   <para>清除全部行动者与组件数据，并保留已分配容量。</para>
        /// </summary>
        internal void Clear()
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                bool poolLockTaken = false;
                try
                {
                    if (m_EnableStorageThreadSafety) m_PoolLock.Enter(ref poolLockTaken);
                    foreach (var pool in m_ComponentPools.Values)
                        pool.Clear();
                }
                finally
                {
                    if (poolLockTaken) m_PoolLock.Exit();
                }

                for (int i = 0; i < m_Capacity; i++)
                    m_Actors[i] = default;

                for (int i = 0; i < m_Capacity; i++)
                    m_FreeList[i] = i;

                m_FreeCount = m_Capacity;
                AliveActorCount = 0;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>在调用方已持有锁时校验行动者句柄。</para>
        /// </summary>
        /// <param name="actor">待校验的行动者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsActorAliveUnsafe(Actor actor)
        {
            if (actor.Index < 0 || actor.Index >= m_Capacity) return false;
            ref var actorData = ref m_Actors[actor.Index];
            return actorData.isAlive && actorData.version == actor.Version;
        }

        /// <summary>
        ///   <para>在调用方已持有行动者写锁时按类型 ID 移除组件。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="typeId">组件类型标识。</param>
        private bool RemoveComponentByTypeIdUnsafe(Actor actor, int typeId)
        {
            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_PoolLock.Enter(ref lockTaken);
                if (!m_ComponentPools.TryGetValue(typeId, out var pool)) return false;
                if (!pool.Remove(actor)) return false;
            }
            finally
            {
                if (lockTaken) m_PoolLock.Exit();
            }

            ref var actorData = ref m_Actors[actor.Index];
            actorData.componentMask.Remove(typeId);
            return true;
        }

        /// <summary>
        ///   <para>获取或创建组件类型对应的稀疏存储池。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ComponentPool<T> GetOrCreatePool<T>() where T : struct, IComponent
        {
            var typeId = ComponentTypeRegistry<T>.id;

            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_PoolLock.Enter(ref lockTaken);
                if (!m_ComponentPools.TryGetValue(typeId, out var poolObj))
                {
                    var pool = new ComponentPool<T>(Math.Max(AliveActorCount / 4, 64), m_EnableStorageThreadSafety);
                    m_ComponentPools[typeId] = pool;
                    return pool;
                }

                return (ComponentPool<T>)poolObj;
            }
            finally
            {
                if (lockTaken) m_PoolLock.Exit();
            }
        }

        /// <summary>
        ///   <para>获取组件类型对应的存储池；尚未创建时返回 <see langword="null"/>。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentPool<T> GetPool<T>() where T : struct, IComponent
        {
            var typeId = ComponentTypeRegistry<T>.id;

            bool lockTaken = false;
            try
            {
                if (m_EnableStorageThreadSafety) m_PoolLock.Enter(ref lockTaken);
                return m_ComponentPools.TryGetValue(typeId, out var poolObj)
                    ? (ComponentPool<T>)poolObj
                    : null;
            }
            finally
            {
                if (lockTaken) m_PoolLock.Exit();
            }
        }

        /// <summary>
        ///   <para>扩展行动者槽位及空闲索引存储。</para>
        /// </summary>
        /// <param name="newCapacity">目标容量。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int newCapacity)
        {
            if (newCapacity <= m_Capacity || newCapacity > ActorManagerOptions.MAX_CAPACITY) return;

            int oldCapacity = m_Capacity;
            m_Capacity = newCapacity;

            Array.Resize(ref m_Actors, newCapacity);
            Array.Resize(ref m_FreeList, newCapacity);

            for (int i = oldCapacity; i < newCapacity; i++)
                m_FreeList[m_FreeCount++] = i;
        }
    }
}