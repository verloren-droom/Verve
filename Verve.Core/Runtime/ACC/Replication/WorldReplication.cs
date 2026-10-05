// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>世界复制层；拥有连接、状态和远端实体，仅在世界线程提交。</para>
    /// </summary>
    public sealed class WorldReplication
    {
        /// <summary>
        ///   <para>传输接管记录；防止跨世界重复接管或复用已释放连接。</para>
        /// </summary>
        private static readonly ConditionalWeakTable<IReplicationTransport, object> s_Transports = new();
        /// <summary>
        ///   <para>传输接管标记；所有条目共享同一值，避免无意义分配。</para>
        /// </summary>
        private static readonly object s_TransportMarker = new();
        
        /// <summary>
        ///   <para>所属世界。</para>
        /// </summary>
        internal readonly World World;
        /// <summary>
        ///   <para>创建时固定的配置。</para>
        /// </summary>
        internal readonly ReplicationOptions Options;
        /// <summary>
        ///   <para>冻结的组件字段。</para>
        /// </summary>
        internal readonly ReplicationComponent[] Components;
        /// <summary>
        ///   <para>框架拥有的连接。</para>
        /// </summary>
        private readonly List<ReplicationPeer> m_Peers = new();
        /// <summary>
        ///   <para>副本实体所有权；外部不能提前销毁连接拥有的实体。</para>
        /// </summary>
        private readonly HashSet<Actor> m_RemoteActors = new();
        /// <summary>
        ///   <para>共享候选缓冲区；避免每个连接重复查询标记。</para>
        /// </summary>
        private readonly List<Actor> m_ReplicationCandidates = new();
        
        /// <summary>
        ///   <para>更新或关闭期间的重入保护。</para>
        /// </summary>
        private bool m_Updating;
        /// <summary>
        ///   <para>权威端采样时钟。</para>
        /// </summary>
        private ulong m_Tick;
        /// <summary>
        ///   <para>连接 ID 分配序列。</para>
        /// </summary>
        private long m_NextPeer;
        /// <summary>
        ///   <para>发送计时器。</para>
        /// </summary>
        private float m_Elapsed;

        /// <summary>
        ///   <para>策略执行状态；阻止编码器和可见性策略进行结构变更。</para>
        /// </summary>
        internal bool IsReading { get; set; }
        /// <summary>
        ///   <para>当前连接数。</para>
        /// </summary>
        public int PeerCount => m_Peers.Count;

        /// <summary>
        ///   <para>创建世界专属复制层。</para>
        /// </summary>
        /// <param name="world">拥有者。</param>
        /// <param name="options">复制配置。</param>
        internal WorldReplication(World world, ReplicationOptions options)
        {
            World = world;
            Options = options;
            Components = options.Schema.Freeze();
        }

        /// <summary>
        ///   <para>接管新的可信连接；失败前所有权仍归调用方，成功后只能由框架释放。</para>
        /// </summary>
        /// <param name="transport">独占且尚未接管的传输。</param>
        public ReplicationPeer Attach(IReplicationTransport transport)
        {
            Validate();
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (m_Peers.Count == Options.MaxPeers || (Options.Role == ReplicationRole.Replica && m_Peers.Count != 0))
                throw new InvalidOperationException("Replication peer limit reached.");
            lock (s_Transports)
            {
                if (s_Transports.TryGetValue(transport, out _))
                    throw new InvalidOperationException("This transport has already been owned by replication.");
                s_Transports.Add(transport, s_TransportMarker);
            }
            try
            {
                var peer = new ReplicationPeer(this, checked(++m_NextPeer), transport);
                m_Peers.Add(peer);
                return peer;
            }
            catch
            {
                lock (s_Transports) s_Transports.Remove(transport);
                throw;
            }
        }

        /// <summary>
        ///   <para>移除连接并释放其远端实体；保留本地实体。</para>
        /// </summary>
        /// <param name="peer">当前世界拥有的连接。</param>
        public void Detach(ReplicationPeer peer)
        {
            Validate();
            if (peer == null) throw new ArgumentNullException(nameof(peer));
            if (peer.Owner != this) throw new ArgumentException("The peer belongs to another world.", nameof(peer));
            if (!m_Peers.Remove(peer)) return;
            m_Updating = true;
            try { peer.Close(); }
            finally { m_Updating = false; }
        }

        /// <summary>
        ///   <para>登记副本实体。</para>
        /// </summary>
        internal Actor CreateRemoteActor()
        {
            var actor = World.CreateActor();
            m_RemoteActors.Add(actor);
            return actor;
        }

        /// <summary>
        ///   <para>释放副本实体；先归还所有权，再执行世界清理。</para>
        /// </summary>
        /// <param name="actor">当前连接拥有的实体。</param>
        internal void DestroyRemoteActor(Actor actor)
        {
            if (!m_RemoteActors.Remove(actor))
                throw new InvalidOperationException("The replication layer does not own this actor.");
            World.DestroyActor(actor);
        }

        /// <summary>
        ///   <para>检查副本实体所有权。</para>
        /// </summary>
        /// <param name="actor">待检查实体。</param>
        internal bool Owns(Actor actor) => m_RemoteActors.Contains(actor);

        /// <summary>
        ///   <para>执行接收阶段；能力更新前提交完整批次。</para>
        /// </summary>
        /// <param name="delta">未缩放时间。</param>
        /// <param name="group">Tick 组。</param>
        internal void BeforeTick(float delta, TickGroup group)
        {
            if (group != TickGroup.Early || m_Peers.Count == 0) return;
            CheckDelta(delta);
            Update(false, delta, false);
        }

        /// <summary>
        ///   <para>执行发送阶段；能力更新完成后捕获一致视图。</para>
        /// </summary>
        /// <param name="delta">未缩放时间。</param>
        /// <param name="group">Tick 组。</param>
        internal void AfterTick(float delta, TickGroup group)
        {
            if (group != TickGroup.Late || Options.Role != ReplicationRole.Authority || m_Peers.Count == 0) return;
            CheckDelta(delta);
            m_Tick = checked(m_Tick + 1);
            m_Elapsed += delta;
            bool capture = m_Elapsed >= Options.SendInterval;
            if (capture)
            {
                m_Elapsed = 0;
                World.QueryActorsWithComponentNonAlloc<Replicated>(m_ReplicationCandidates);
            }
            Update(true, delta, capture);
        }

        /// <summary>
        ///   <para>推进独立连接；一个连接失败也继续处理其余连接。</para>
        /// </summary>
        /// <param name="sending">是否发送。</param>
        /// <param name="delta">未缩放时间。</param>
        /// <param name="capture">是否可以开始新状态。</param>
        private void Update(bool sending, float delta, bool capture)
        {
            m_Updating = true;
            List<Exception> errors = null;
            try
            {
                for (int i = 0; i < m_Peers.Count;)
                {
                    var peer = m_Peers[i];
                    try
                    {
                        if (sending) peer.Send(delta, capture, m_Tick, m_ReplicationCandidates);
                        else peer.Receive(delta);
                        i++;
                    }
                    catch (Exception error)
                    {
                        (errors ??= new List<Exception>()).Add(error);
                        m_Peers.RemoveAt(i);
                        try { peer.Close(); }
                        catch (Exception cleanup) { errors.Add(cleanup); }
                    }
                }
            }
            finally { m_Updating = false; IsReading = false; }
            if (errors != null) throw new AggregateException("Replication connection failed and was closed.", errors);
        }

        /// <summary>
        ///   <para>关闭所有连接；世界清空和释放共用此流程。</para>
        /// </summary>
        internal void Close()
        {
            if (m_Updating) throw new InvalidOperationException("Replication cannot be closed during an update.");
            m_Updating = true;
            List<Exception> errors = null;
            try
            {
                foreach (var peer in m_Peers)
                {
                    try { peer.Close(); }
                    catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                }
                m_Peers.Clear();
                m_RemoteActors.Clear();
                m_ReplicationCandidates.Clear();
                m_Elapsed = 0;
            }
            finally { m_Updating = false; }
            if (errors != null) throw new AggregateException("Closing replication connections failed.", errors);
        }

        /// <summary>
        ///   <para>验证线程、生命周期和重入边界。</para>
        /// </summary>
        private void Validate()
        {
            World.ValidateStructuralChange(true);
            if (m_Updating || World.Capabilities.IsUpdating) throw new InvalidOperationException("Change replication peers through World.Enqueue outside callbacks.");
        }

        /// <summary>
        ///   <para>拒绝会破坏超时计算的时间值。</para>
        /// </summary>
        /// <param name="delta">未缩放时间。</param>
        private static void CheckDelta(float delta)
        {
            if (delta < 0 || !Game.NumberUtility.IsFinite(delta)) throw new ArgumentOutOfRangeException(nameof(delta));
        }
    }
}
