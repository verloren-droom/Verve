namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>ACC 世界管理模块，负责世界的创建、切换、销毁和 Tick 调度。</para>
    /// </summary>
    [Serializable, GameModule("ACC 世界管理模块")]
    public sealed class WorldManagerModule : GameModule, IWorldManager
    {
        /// <summary>
        ///   <para>仅负责将 Core Tick 转发给所属模块的活跃世界。</para>
        /// </summary>
        private sealed class WorldTickSystem :
            IEarlyTick,
            IPhysicsTick,
            IGameplayTick,
            ILateTick
        {
            /// <summary>
            ///   <para>所属世界管理模块。</para>
            /// </summary>
            private readonly WorldManagerModule m_Owner;
            
            /// <summary>
            ///   <para>创建世界 Tick 转发器。</para>
            /// </summary>
            /// <param name="owner">所属模块。</param>
            public WorldTickSystem(WorldManagerModule owner)
            {
                m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            /// <inheritdoc />
            public void EarlyTick(float deltaTime)
            {
                m_Owner.TickActiveWorld(deltaTime, TickGroup.Early);
            }

            /// <inheritdoc />
            public void PhysicsTick(float deltaTime)
            {
                m_Owner.TickActiveWorld(deltaTime, TickGroup.Physics);
            }

            /// <inheritdoc />
            public void GameplayTick(float deltaTime)
            {
                m_Owner.TickActiveWorld(deltaTime, TickGroup.Gameplay);
            }

            /// <inheritdoc />
            public void LateTick(float deltaTime)
            {
                m_Owner.TickActiveWorld(deltaTime, TickGroup.Late);
            }
        }
        
        /// <summary>
        ///   <para>按名称拥有的世界。</para>
        /// </summary>
        private readonly Dictionary<string, World> m_Worlds = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        ///   <para>保护世界集合和活跃世界。</para>
        /// </summary>
        private readonly object m_WorldLock = new();
        
        /// <summary>
        ///   <para>当前活跃世界。</para>
        /// </summary>
        private volatile World m_ActiveWorld;

        /// <summary>
        ///   <para>世界数量。</para>
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (m_WorldLock)
                {
                    return m_Worlds.Count;
                }
            }
        }

        /// <summary>
        ///   <para>当前所有世界。</para>
        /// </summary>
        public IReadOnlyCollection<World> Worlds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (m_WorldLock)
                {
                    if (m_Worlds.Count == 0)
                    {
                        return Array.Empty<World>();
                    }

                    var worlds = new World[m_Worlds.Count];
                    m_Worlds.Values.CopyTo(worlds, 0);
                    return worlds;
                }
            }
        }

        /// <summary>
        ///   <para>当前活跃世界；没有可用世界时返回 <c>null</c>。</para>
        /// </summary>
        public World Active
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (m_WorldLock)
                {
                    return GetActiveWorldNoLock();
                }
            }
        }

        /// <summary>
        ///   <para>模块安装时登记其拥有的世界 Tick 对象。</para>
        /// </summary>
        protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
        {
            context.AddTickSystem(new WorldTickSystem(this));
            return default;
        }

        /// <summary>
        ///   <para>模块卸载时释放其拥有的全部世界。</para>
        /// </summary>
        protected override ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
        {
            DestroyAllInternal();
            return default;
        }

        /// <inheritdoc />
        protected override void OnDispose()
        {
            DestroyAllInternal();
        }
        
        /// <summary>
        ///   <para>创建世界，或激活同名的现有世界。</para>
        /// </summary>
        public World Create(
            string worldName,
            ActorManagerOptions? actorManagerOptions = null,
            ReplicationOptions replicationOptions = null)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(WorldManagerModule)}.{nameof(Create)}");
            if (string.IsNullOrWhiteSpace(worldName))
            {
                throw new ArgumentException("World name cannot be null or empty.", nameof(worldName));
            }

            lock (m_WorldLock)
            {
                if (m_Worlds.TryGetValue(worldName, out var existingWorld))
                {
                    if (!existingWorld.IsDisposed)
                    {
                        m_ActiveWorld = existingWorld;
                        return existingWorld;
                    }

                    m_Worlds.Remove(worldName);
                }

                var world = new World(worldName, actorManagerOptions, replicationOptions);
                m_Worlds.Add(worldName, world);
                if (m_ActiveWorld == null || m_ActiveWorld.IsDisposed)
                {
                    m_ActiveWorld = world;
                }

                return world;
            }
        }

        /// <summary>
        ///   <para>判断指定世界是否存在且可用。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName))
            {
                return false;
            }

            lock (m_WorldLock)
            {
                return m_Worlds.TryGetValue(worldName, out var world) && !world.IsDisposed;
            }
        }

        /// <summary>
        ///   <para>获取指定世界；不存在时返回 <c>null</c>。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public World Get(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName))
            {
                return null;
            }

            lock (m_WorldLock)
            {
                return m_Worlds.TryGetValue(worldName, out var world) && !world.IsDisposed ? world : null;
            }
        }

        /// <summary>
        ///   <para>切换当前活跃世界。</para>
        /// </summary>
        public bool SetActive(string worldName)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(WorldManagerModule)}.{nameof(SetActive)}");
            if (string.IsNullOrWhiteSpace(worldName))
            {
                return false;
            }

            lock (m_WorldLock)
            {
                if (!m_Worlds.TryGetValue(worldName, out var world) || world.IsDisposed)
                {
                    return false;
                }

                m_ActiveWorld = world;
                return true;
            }
        }

        /// <summary>
        ///   <para>销毁指定世界。若它是当前活跃世界，会自动选择下一个可用世界。</para>
        /// </summary>
        public void Destroy(string worldName)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(WorldManagerModule)}.{nameof(Destroy)}");
            if (string.IsNullOrWhiteSpace(worldName))
            {
                return;
            }

            World worldToDispose;
            lock (m_WorldLock)
            {
                if (!m_Worlds.TryGetValue(worldName, out worldToDispose))
                {
                    return;
                }
            }

            worldToDispose.Dispose();

            lock (m_WorldLock)
            {
                if (m_Worlds.TryGetValue(worldName, out var current) && ReferenceEquals(current, worldToDispose))
                    m_Worlds.Remove(worldName);
                if (ReferenceEquals(m_ActiveWorld, worldToDispose))
                    m_ActiveWorld = FindFirstAvailableWorldNoLock();
            }
        }

        /// <summary>
        ///   <para>销毁全部世界。</para>
        /// </summary>
        public void DestroyAll()
        {
            Game.ThrowIfNotOnMainThread($"{nameof(WorldManagerModule)}.{nameof(DestroyAll)}");
            DestroyAllInternal();
        }

        /// <summary>
        ///   <para>更新当前活跃世界。</para>
        /// </summary>
        /// <param name="deltaTime">本次 Tick 间隔。</param>
        /// <param name="tickGroup">当前 Tick 组。</param>
        private void TickActiveWorld(float deltaTime, TickGroup tickGroup)
        {
            World world;
            lock (m_WorldLock)
            {
                world = GetActiveWorldNoLock();
            }

            if (world == null)
            {
                return;
            }

            try
            {
                world.Tick(deltaTime, tickGroup);
            }
            catch (Exception ex)
            {
                Game.LogError($"World {tickGroup} update error ({world.Name}): {ex}");
            }
        }
        /// <summary>
        ///   <para>在已持有世界锁时获取有效活跃世界。</para>
        /// </summary>
        private World GetActiveWorldNoLock()
        {
            var active = m_ActiveWorld;
            if (active != null && !active.IsDisposed)
            {
                return active;
            }

            m_ActiveWorld = FindFirstAvailableWorldNoLock();
            return m_ActiveWorld;
        }
        /// <summary>
        ///   <para>在已持有世界锁时查找第一个有效世界。</para>
        /// </summary>
        private World FindFirstAvailableWorldNoLock()
        {
            foreach (var world in m_Worlds.Values)
            {
                if (world != null && !world.IsDisposed)
                {
                    return world;
                }
            }

            return null;
        }
        /// <summary>
        ///   <para>释放并清空模块拥有的全部世界。</para>
        /// </summary>
        private void DestroyAllInternal()
        {
            KeyValuePair<string, World>[] worlds;
            lock (m_WorldLock)
            {
                worlds = new KeyValuePair<string, World>[m_Worlds.Count];
                var index = 0;
                foreach (var pair in m_Worlds)
                    worlds[index++] = pair;
            }

            List<Exception> errors = null;
            for (int i = 0; i < worlds.Length; i++)
            {
                var world = worlds[i].Value;
                if (world == null || world.IsDisposed)
                {
                    continue;
                }

                try
                {
                    world.Dispose();
                }
                catch (Exception ex)
                {
                    errors ??= new List<Exception>();
                    errors.Add(ex);
                }
            }

            lock (m_WorldLock)
            {
                for (int i = 0; i < worlds.Length; i++)
                {
                    var pair = worlds[i];
                    if (m_Worlds.TryGetValue(pair.Key, out var current) &&
                        ReferenceEquals(current, pair.Value) &&
                        (current == null || current.IsDisposed))
                        m_Worlds.Remove(pair.Key);
                }

                if (m_ActiveWorld == null || m_ActiveWorld.IsDisposed)
                    m_ActiveWorld = FindFirstAvailableWorldNoLock();
            }

            if (errors == null || errors.Count == 0)
            {
                return;
            }

            if (errors.Count == 1)
            {
                throw errors[0];
            }

            throw new AggregateException(errors);
        }
    }
}