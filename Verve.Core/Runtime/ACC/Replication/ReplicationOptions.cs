// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;

    /// <summary>
    ///   <para>复制标记；权威端仅发布持有此组件的行动者。</para>
    /// </summary>
    public struct Replicated : IComponent { }

    /// <summary>
    ///   <para>复制角色；一个世界只有一个状态权威来源。</para>
    /// </summary>
    public enum ReplicationRole
    {
        /// <summary>
        ///   <para>权威端；向多个连接发布状态。</para>
        /// </summary>
        Authority,
        /// <summary>
        ///   <para>副本端；从一个可信连接接收状态。</para>
        /// </summary>
        Replica,
    }

    /// <summary>
    ///   <para>复制传输；提供可靠、有序、保持帧边界的连接。</para>
    /// </summary>
    public interface IReplicationTransport : IDisposable
    {
        /// <summary>
        ///   <para>尝试发送；队列满返回假且不接管数据，断线抛出异常。</para>
        /// </summary>
        /// <param name="packet">借用的完整帧；返回前完成复制或传输。</param>
        bool TrySend(ReadOnlySpan<byte> packet);

        /// <summary>
        ///   <para>非阻塞接收；无帧返回零，断线或缓冲区不足抛出异常。</para>
        /// </summary>
        /// <param name="buffer">框架复用的接收缓冲区。</param>
        int Receive(Span<byte> buffer);
    }

    /// <summary>
    ///   <para>可见性策略；按连接筛选行动者，默认发布全部复制标记。</para>
    /// </summary>
    public interface IReplicationInterest
    {
        /// <summary>
        ///   <para>判断行动者对连接是否可见；仅在世界线程调用，不得修改世界。</para>
        /// </summary>
        /// <param name="world">权威世界。</param>
        /// <param name="actor">候选行动者。</param>
        /// <param name="peer">目标连接。</param>
        bool IsRelevant(World world, Actor actor, ReplicationPeer peer);
    }

    /// <summary>
    ///   <para>复制配置；创建时固定协议、角色、预算和可见性策略。</para>
    /// </summary>
    public sealed class ReplicationOptions
    {
        /// <summary>
        ///   <para>组件协议；首次使用后冻结。</para>
        /// </summary>
        public ReplicationSchema Schema { get; }
        /// <summary>
        ///   <para>世界的复制角色。</para>
        /// </summary>
        public ReplicationRole Role { get; }
        /// <summary>
        ///   <para>状态间隔；使用未缩放时间，零表示每次 Late 更新。</para>
        /// </summary>
        public float SendInterval { get; }
        /// <summary>
        ///   <para>单帧字节上限；包含复制包头。</para>
        /// </summary>
        public int MaxPacketBytes { get; }
        /// <summary>
        ///   <para>单个状态或增量的字节上限。</para>
        /// </summary>
        public int MaxStateBytes { get; }
        /// <summary>
        ///   <para>每个连接的行动者上限。</para>
        /// </summary>
        public int MaxActors { get; }
        /// <summary>
        ///   <para>每个连接每次 Tick 的帧预算。</para>
        /// </summary>
        public int PacketsPerTick { get; }
        /// <summary>
        ///   <para>世界接管的连接上限。</para>
        /// </summary>
        public int MaxPeers { get; }
        /// <summary>
        ///   <para>批次传输时限；背压或不完整输入不能无限占用资源。</para>
        /// </summary>
        public float TransferTimeout { get; }
        /// <summary>
        ///   <para>可见性策略；为空时发布全部标记行动者。</para>
        /// </summary>
        public IReplicationInterest Interest { get; }

        /// <summary>
        ///   <para>创建复制配置。</para>
        /// </summary>
        /// <param name="schema">组件协议。</param>
        /// <param name="role">世界角色。</param>
        /// <param name="sendInterval">发送间隔（秒）。</param>
        /// <param name="maxPacketBytes">单帧上限，256 至 262144 字节。</param>
        /// <param name="maxStateBytes">批次上限，12 字节至 64 MiB。</param>
        /// <param name="maxActors">连接行动者上限。</param>
        /// <param name="packetsPerTick">每次 Tick 的帧预算。</param>
        /// <param name="maxPeers">最大连接数。</param>
        /// <param name="transferTimeout">批次超时（秒）。</param>
        /// <param name="interest">无资源所有权的可见性策略。</param>
        public ReplicationOptions(ReplicationSchema schema, ReplicationRole role,
            float sendInterval = 0.05f, int maxPacketBytes = 65536, int maxStateBytes = 8 * 1024 * 1024,
            int maxActors = 65536, int packetsPerTick = 64, int maxPeers = 128,
            float transferTimeout = 10f, IReplicationInterest interest = null)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            if (role != ReplicationRole.Authority && role != ReplicationRole.Replica)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (float.IsNaN(sendInterval) || float.IsInfinity(sendInterval) || sendInterval < 0)
                throw new ArgumentOutOfRangeException(nameof(sendInterval));
            if (float.IsNaN(transferTimeout) || float.IsInfinity(transferTimeout) || transferTimeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(transferTimeout));
            if (maxPacketBytes < 256 || maxPacketBytes > 262144) throw new ArgumentOutOfRangeException(nameof(maxPacketBytes));
            if (maxStateBytes < 12 || maxStateBytes > 64 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(maxStateBytes));
            if (maxActors < 1 || maxActors > ActorManagerOptions.MAX_CAPACITY) throw new ArgumentOutOfRangeException(nameof(maxActors));
            if (packetsPerTick < 1 || packetsPerTick > 4096) throw new ArgumentOutOfRangeException(nameof(packetsPerTick));
            if (maxPeers < 1 || maxPeers > 65536) throw new ArgumentOutOfRangeException(nameof(maxPeers));
            if (interest is IDisposable) throw new ArgumentException("Interest policies must not own disposable resources.", nameof(interest));
            Role = role;
            SendInterval = sendInterval;
            MaxPacketBytes = maxPacketBytes;
            MaxStateBytes = maxStateBytes;
            MaxActors = maxActors;
            PacketsPerTick = packetsPerTick;
            MaxPeers = maxPeers;
            TransferTimeout = transferTimeout;
            Interest = interest;
        }
    }
}
