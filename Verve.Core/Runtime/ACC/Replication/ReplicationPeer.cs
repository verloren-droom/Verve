// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    ///   <para>复制连接；基线、分片和远端实体由 <see cref="WorldReplication"/> 独占管理。</para>
    /// </summary>
    public sealed class ReplicationPeer
    {
        /// <summary>
        ///   <para>所属复制层。</para>
        /// </summary>
        internal readonly WorldReplication Owner;
        /// <summary>
        ///   <para>当前接管的传输。</para>
        /// </summary>
        private IReplicationTransport m_Transport;
        /// <summary>
        ///   <para>复用的单帧存储。</para>
        /// </summary>
        private byte[] m_Packet;
        /// <summary>
        ///   <para>正在发送或接收的完整批次。</para>
        /// </summary>
        private ReplicationBuffer m_State;
        /// <summary>
        ///   <para>上一完整发送状态。</para>
        /// </summary>
        private ReplicationState m_Baseline;
        /// <summary>
        ///   <para>本次捕获状态；背压时保持不变。</para>
        /// </summary>
        private ReplicationState m_Capture;
        /// <summary>
        ///   <para>本连接可见的候选实体。</para>
        /// </summary>
        private List<Actor> m_Visible;
        /// <summary>
        ///   <para>连接独占的远端实体。</para>
        /// </summary>
        private Dictionary<long, Actor> m_RemoteActors;
        /// <summary>
        ///   <para>预检后的实体集合。</para>
        /// </summary>
        private HashSet<long> m_CandidateIds;
        /// <summary>
        ///   <para>当前字段的去重集合。</para>
        /// </summary>
        private HashSet<long> m_Seen;
        /// <summary>
        ///   <para>待创建的实体。</para>
        /// </summary>
        private List<long> m_Spawned;
        /// <summary>
        ///   <para>待销毁的实体。</para>
        /// </summary>
        private List<long> m_Despawned;
        /// <summary>
        ///   <para>强类型组件暂存。</para>
        /// </summary>
        private ReplicationBatch[] m_Batches;
        /// <summary>
        ///   <para>当前接收批次的固定头部。</para>
        /// </summary>
        private ReplicationHeader m_Incoming;
        /// <summary>
        ///   <para>已发送的批次偏移。</para>
        /// </summary>
        private int m_Offset;
        /// <summary>
        ///   <para>捕获状态的 Tick。</para>
        /// </summary>
        private ulong m_CaptureTick;
        /// <summary>
        ///   <para>当前批次的累计传输时间。</para>
        /// </summary>
        private float m_TransferElapsed;

        /// <summary>
        ///   <para>连接 ID；世界内单调分配，不是身份凭证。</para>
        /// </summary>
        public long Id { get; }
        /// <summary>
        ///   <para>最后完整发送或应用的批次。</para>
        /// </summary>
        public ulong Sequence { get; private set; }
        /// <summary>
        ///   <para>最后完整批次的权威采样 Tick。</para>
        /// </summary>
        public ulong Tick { get; private set; }
        /// <summary>
        ///   <para>连接是否已经由框架释放。</para>
        /// </summary>
        public bool IsClosed { get; private set; }
        /// <summary>
        ///   <para>当前拥有的远端实体数量。</para>
        /// </summary>
        public int RemoteActorCount => m_RemoteActors?.Count ?? 0;

        /// <summary>
        ///   <para>创建尚未接管传输的连接。</para>
        /// </summary>
        /// <param name="owner">所属复制层。</param>
        /// <param name="id">连接 ID。</param>
        /// <param name="transport">待接管传输。</param>
        internal ReplicationPeer(WorldReplication owner, long id, IReplicationTransport transport)
        {
            Owner = owner;
            Id = id;
            m_Transport = transport;
            m_Packet = new byte[owner.Options.MaxPacketBytes];
            m_State = new ReplicationBuffer(owner.Options.MaxStateBytes);
            if (owner.Options.Role == ReplicationRole.Authority)
            {
                m_Baseline = new ReplicationState(owner.Components.Length, owner.Options.MaxStateBytes);
                m_Capture = new ReplicationState(owner.Components.Length, owner.Options.MaxStateBytes);
                m_Visible = new List<Actor>();
            }
            else
            {
                m_RemoteActors = new Dictionary<long, Actor>();
                m_CandidateIds = new HashSet<long>();
                m_Seen = new HashSet<long>();
                m_Spawned = new List<long>();
                m_Despawned = new List<long>();
                m_Batches = new ReplicationBatch[owner.Components.Length];
                for (int i = 0; i < m_Batches.Length; i++) m_Batches[i] = owner.Components[i].CreateBatch();
            }
        }

        /// <summary>
        ///   <para>查询远端实体对应的本地行动者；只能在世界线程调用。</para>
        /// </summary>
        /// <param name="networkId">来自权威端的稳定实体 ID。</param>
        /// <param name="actor">映射结果。</param>
        public bool TryGetActor(long networkId, out Actor actor)
        {
            Owner.World.ValidateStructuralChange();
            actor = Actor.none;
            return !IsClosed && m_RemoteActors != null && m_RemoteActors.TryGetValue(networkId, out actor) && Owner.World.IsActorAlive(actor);
        }

        /// <summary>
        ///   <para>生成增量并有界发送；完整接管后才切换基线。</para>
        /// </summary>
        /// <param name="delta">未缩放时间。</param>
        /// <param name="capture">是否开始新状态。</param>
        /// <param name="tick">权威时钟。</param>
        /// <param name="candidates">持有复制标记的实体。</param>
        internal void Send(float delta, bool capture, ulong tick, List<Actor> candidates)
        {
            if (m_State.Length != 0) CheckTimeout(delta);
            if (m_State.Length == 0 && capture)
            {
                m_Visible.Clear();
                Owner.IsReading = true;
                try
                {
                    foreach (var actor in candidates)
                    {
                        if (Owner.Options.Interest != null && !Owner.Options.Interest.IsRelevant(Owner.World, actor, this)) continue;
                        if (m_Visible.Count == Owner.Options.MaxActors) throw new InvalidOperationException("Replication actor limit exceeded.");
                        m_Visible.Add(actor);
                    }
                    m_Capture.Capture(Owner.World, m_Visible, Owner.Components, Owner.Options.MaxStateBytes);
                }
                finally { Owner.IsReading = false; }
                m_Capture.WriteDelta(m_Baseline, Owner.Components, m_State);
                if (Sequence != 0 && m_State.Length == 12)
                {
                    m_State.Clear();
                    return;
                }
                m_CaptureTick = tick;
                m_TransferElapsed = 0;
            }
            if (m_State.Length == 0) return;
            ulong next = checked(Sequence + 1);
            for (int i = 0; i < Owner.Options.PacketsPerTick; i++)
            {
                int length = Math.Min(m_Packet.Length - ReplicationHeader.Size, m_State.Length - m_Offset);
                var header = new ReplicationHeader(Owner.Options.Schema.Fingerprint, next, Sequence,
                    m_CaptureTick, m_State.Length, m_Offset);
                header.Write(m_Packet);
                m_State.Span.Slice(m_Offset, length).CopyTo(m_Packet.AsSpan(ReplicationHeader.Size));
                if (!m_Transport.TrySend(m_Packet.AsSpan(0, ReplicationHeader.Size + length))) return;
                m_Offset += length;
                if (m_Offset != m_State.Length) continue;
                (m_Baseline, m_Capture) = (m_Capture, m_Baseline);
                Sequence = next;
                Tick = m_CaptureTick;
                m_Offset = 0;
                m_State.Clear();
                return;
            }
        }

        /// <summary>
        ///   <para>组装可靠有序分片；未知基线、重复和乱序帧作为协议错误关闭连接。</para>
        /// </summary>
        /// <param name="delta">未缩放时间。</param>
        internal void Receive(float delta)
        {
            if (Owner.Options.Role == ReplicationRole.Replica && m_State.Length != 0) CheckTimeout(delta);
            for (int i = 0; i < Owner.Options.PacketsPerTick; i++)
            {
                int count = m_Transport.Receive(m_Packet);
                if (count == 0) return;
                if (count < 0 || count > m_Packet.Length) throw new InvalidDataException("Invalid transport frame length.");
                if (Owner.Options.Role != ReplicationRole.Replica)
                    throw new InvalidDataException("An authority cannot receive replicated state.");
                var packet = m_Packet.AsSpan(0, count);
                var header = ReplicationHeader.Read(packet, Owner.Options.MaxStateBytes);
                if (header.schema != Owner.Options.Schema.Fingerprint || header.baseline != Sequence ||
                    header.offset != m_State.Length || header.tick <= Tick)
                    throw new InvalidDataException("Replication schema, baseline, tick or fragment order mismatch.");
                if (m_State.Length == 0)
                {
                    m_Incoming = header;
                    m_TransferElapsed = 0;
                }
                else if (header.total != m_Incoming.total || header.tick != m_Incoming.tick)
                    throw new InvalidDataException("Replication fragment metadata changed within a batch.");
                packet.Slice(ReplicationHeader.Size).CopyTo(m_State.Append(count - ReplicationHeader.Size));
                if (m_State.Length != header.total) continue;
                Owner.IsReading = true;
                try { Decode(); }
                finally { Owner.IsReading = false; }
                Apply();
                Sequence = header.sequence;
                Tick = header.tick;
                m_State.Clear();
            }
        }

        /// <summary>
        ///   <para>完整预检并解码；该阶段不创建或修改世界资源。</para>
        /// </summary>
        private void Decode()
        {
            m_CandidateIds.Clear();
            m_Spawned.Clear();
            m_Despawned.Clear();
            foreach (var pair in m_RemoteActors)
            {
                if (!Owner.World.IsActorAlive(pair.Value)) throw new InvalidOperationException("A replicated actor was destroyed outside its owner.");
                m_CandidateIds.Add(pair.Key);
            }
            foreach (var batch in m_Batches) batch.Clear();
            var reader = new ReplicationReader(m_State.Span);
            int spawned = reader.ReadCount(8, Owner.Options.MaxActors);
            for (int i = 0; i < spawned; i++)
            {
                long id = reader.ReadId();
                if (!m_CandidateIds.Add(id)) throw new InvalidDataException("Duplicate entity spawn.");
                m_Spawned.Add(id);
            }
            int despawned = reader.ReadCount(8, Owner.Options.MaxActors);
            for (int i = 0; i < despawned; i++)
            {
                long id = reader.ReadId();
                if (!m_RemoteActors.ContainsKey(id) || !m_CandidateIds.Remove(id)) throw new InvalidDataException("Unknown or duplicate entity despawn.");
                m_Despawned.Add(id);
            }
            if (m_CandidateIds.Count > Owner.Options.MaxActors ||
                (long)Owner.World.Actors.AliveActorCount + spawned - despawned > ActorManagerOptions.MAX_CAPACITY)
                throw new InvalidDataException("Replication actor capacity exceeded.");
            int columns = reader.ReadCount(12, Owner.Components.Length);
            uint previousType = 0;
            int componentIndex = 0;
            for (int i = 0; i < columns; i++)
            {
                uint id = reader.ReadUInt();
                if (id <= previousType) throw new InvalidDataException("Component columns must have unique ascending IDs.");
                previousType = id;
                while (componentIndex < Owner.Components.Length && Owner.Components[componentIndex].Id < id) componentIndex++;
                if (componentIndex == Owner.Components.Length || Owner.Components[componentIndex].Id != id)
                    throw new InvalidDataException("Unknown replication component.");
                var batch = m_Batches[componentIndex];
                m_Seen.Clear();
                int removed = reader.ReadCount(8, Owner.Options.MaxActors);
                for (int j = 0; j < removed; j++)
                {
                    long entity = reader.ReadId();
                    if (!m_CandidateIds.Contains(entity) || !batch.Members.Contains(entity) || !m_Seen.Add(entity))
                        throw new InvalidDataException("Invalid component removal.");
                    batch.Removed.Add(entity);
                }
                int size = Owner.Components[componentIndex].Size;
                int updated = reader.ReadCount(8 + size, Owner.Options.MaxActors);
                if (removed == 0 && updated == 0) throw new InvalidDataException("Empty component column.");
                for (int j = 0; j < updated; j++)
                {
                    long entity = reader.ReadId();
                    if (!m_CandidateIds.Contains(entity) || !m_Seen.Add(entity))
                        throw new InvalidDataException("Unknown entity or duplicate component update.");
                    batch.Updated.Add(entity);
                    batch.Decode(reader.Read(size));
                }
            }
            if (reader.Remaining != 0) throw new InvalidDataException("Trailing replication data.");
        }

        /// <summary>
        ///   <para>提交已校验批次；生命周期回调异常时由复制层关闭连接并清理其全部实体。</para>
        /// </summary>
        private void Apply()
        {
            foreach (long id in m_Despawned)
            {
                var actor = m_RemoteActors[id];
                m_RemoteActors.Remove(id);
                Owner.DestroyRemoteActor(actor);
                foreach (var batch in m_Batches) batch.Members.Remove(id);
            }
            foreach (long id in m_Spawned) m_RemoteActors.Add(id, Owner.CreateRemoteActor());
            foreach (var batch in m_Batches)
            {
                batch.Apply(Owner.World, m_RemoteActors);
                batch.Clear();
            }
        }

        /// <summary>
        ///   <para>检查整个批次的时限；少量持续输入不会重置超时。</para>
        /// </summary>
        /// <param name="delta">未缩放时间。</param>
        private void CheckTimeout(float delta)
        {
            m_TransferElapsed += delta;
            if (m_TransferElapsed >= Owner.Options.TransferTimeout) throw new TimeoutException("Replication batch timed out.");
        }

        /// <summary>
        ///   <para>释放传输和远端实体；所有清理均执行并保留异常。</para>
        /// </summary>
        internal void Close()
        {
            if (IsClosed) return;
            IsClosed = true;
            List<Exception> errors = null;
            var transport = m_Transport;
            m_Transport = null;
            try { transport.Dispose(); }
            catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
            if (m_RemoteActors != null)
                foreach (var actor in m_RemoteActors.Values)
                {
                    try { Owner.DestroyRemoteActor(actor); }
                    catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                }
            m_Packet = null;
            m_State = null;
            m_Baseline = null;
            m_Capture = null;
            m_Visible = null;
            m_RemoteActors = null;
            m_CandidateIds = null;
            m_Seen = null;
            m_Spawned = null;
            m_Despawned = null;
            m_Batches = null;
            if (errors != null) throw new AggregateException("Replication peer cleanup failed.", errors);
        }
    }
}