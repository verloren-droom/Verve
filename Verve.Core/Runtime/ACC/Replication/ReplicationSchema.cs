// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Buffers.Binary;

    /// <summary>
    ///   <para>组件编码；定长、确定性，负责定义跨进程的线格式。</para>
    /// </summary>
    /// <typeparam name="T">无托管引用的组件。</typeparam>
    public interface IReplicationCodec<T> where T : unmanaged, IComponent
    {
        /// <summary>
        ///   <para>编码字节数；创建后不变。</para>
        /// </summary>
        int Size { get; }
        /// <summary>
        ///   <para>写入所有目标字节；可在此进行量化。</para>
        /// </summary>
        /// <param name="destination">长度为 <see cref="Size"/> 的缓冲区。</param>
        /// <param name="value">组件值。</param>
        void Encode(Span<byte> destination, T value);
        /// <summary>
        ///   <para>解码并校验组件；非法值应抛出异常，不得修改世界。</para>
        /// </summary>
        /// <param name="source">长度为 <see cref="Size"/> 的输入。</param>
        T Decode(ReadOnlySpan<byte> source);
    }

    /// <summary>
    ///   <para>组件协议；显式 ID 和编码大小构成稳定 Schema，首次使用后禁止修改。</para>
    /// </summary>
    public sealed class ReplicationSchema
    {
        /// <summary>
        ///   <para>项目协议版本；编码语义变更时必须更新。</para>
        /// </summary>
        private readonly ulong m_Version;
        /// <summary>
        ///   <para>类型映射；仅用于注册冲突检查。</para>
        /// </summary>
        private readonly HashSet<Type> m_Types = new();
        /// <summary>
        ///   <para>协议字段；按显式 ID 排序。</para>
        /// </summary>
        private readonly SortedDictionary<uint, ReplicationComponent> m_Components = new();
        /// <summary>
        ///   <para>冻结后的字段数组。</para>
        /// </summary>
        private ReplicationComponent[] m_Frozen;
        /// <summary>
        ///   <para>协议指纹；覆盖版本、字段 ID 和编码大小。</para>
        /// </summary>
        internal ulong Fingerprint { get; private set; }

        /// <summary>
        ///   <para>创建组件协议。</para>
        /// </summary>
        /// <param name="version">项目内稳定的协议版本，不能为零。</param>
        public ReplicationSchema(ulong version)
        {
            if (version == 0) throw new ArgumentOutOfRangeException(nameof(version));
            m_Version = version;
        }

        /// <summary>
        ///   <para>登记组件；默认缓存数值字段布局，可传入自定义编码。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="id">稳定字段 ID，不能为零。</param>
        /// <param name="codec">无资源所有权的线程安全编码器；为空时自动编码数值字段。</param>
        public ReplicationSchema Register<T>(uint id, IReplicationCodec<T> codec = null) where T : unmanaged, IComponent
        {
            if (m_Frozen != null) throw new InvalidOperationException("Replication schema is frozen.");
            if (id == 0) throw new ArgumentOutOfRangeException(nameof(id));
            codec ??= new ReplicationCodec<T>();
            if (codec is IDisposable) throw new ArgumentException("Codecs must not own disposable resources.", nameof(codec));
            int size = codec.Size;
            if (size < 1 || size > 65536) throw new ArgumentOutOfRangeException(nameof(codec), "Codec size must be 1..65536.");
            if (m_Components.Count == 1024) throw new InvalidOperationException("At most 1024 replicated component types are supported.");
            if (m_Components.ContainsKey(id) || m_Types.Contains(typeof(T)))
                throw new InvalidOperationException("Duplicate replication component ID or type.");
            m_Components.Add(id, new ReplicationComponent<T>(id, size, codec));
            m_Types.Add(typeof(T));
            return this;
        }

        /// <summary>
        ///   <para>按小端字节追加协议指纹。</para>
        /// </summary>
        /// <param name="hash">当前指纹。</param>
        /// <param name="value">字段值。</param>
        private static ulong AppendFingerprint(ulong hash, ulong value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
            return Game.HashUtility.ComputeFnv1a64(bytes, hash);
        }

        /// <summary>
        ///   <para>冻结协议；字段数组由世界和连接共享。</para>
        /// </summary>
        internal ReplicationComponent[] Freeze()
        {
            if (m_Frozen != null) return m_Frozen;
            var components = new ReplicationComponent[m_Components.Count];
            m_Components.Values.CopyTo(components, 0);
            ulong hash = AppendFingerprint(Game.HashUtility.ComputeFnv1a64(ReadOnlySpan<byte>.Empty), m_Version);
            foreach (var component in components)
                hash = AppendFingerprint(AppendFingerprint(AppendFingerprint(hash, component.Id), (ulong)component.Size), component.Format);
            if (hash == 0) throw new InvalidOperationException("Schema fingerprint is zero; choose a different schema version.");
            Fingerprint = hash;
            return m_Frozen = components;
        }

    }

    /// <summary>
    ///   <para>复制字段；内部桥接强类型编码与连续字节列。</para>
    /// </summary>
    internal abstract class ReplicationComponent
    {
        /// <summary>
        ///   <para>字段 ID。</para>
        /// </summary>
        internal readonly uint Id;
        /// <summary>
        ///   <para>固定编码长度。</para>
        /// </summary>
        internal readonly int Size;
        /// <summary>
        ///   <para>自动编码格式指纹；自定义编码通过项目协议版本区分。</para>
        /// </summary>
        internal ulong Format;

        /// <summary>
        ///   <para>创建字段。</para>
        /// </summary>
        /// <param name="id">协议 ID。</param>
        /// <param name="size">编码长度。</param>
        protected ReplicationComponent(uint id, int size) { Id = id; Size = size; }

        /// <summary>
        ///   <para>捕获一列组件；仅读取已筛选的行动者。</para>
        /// </summary>
        /// <param name="world">源世界。</param>
        /// <param name="actors">可见行动者。</param>
        /// <param name="column">复用字节列。</param>
        /// <param name="bytes">连续编码区。</param>
        /// <param name="remaining">剩余状态字节预算。</param>
        internal abstract void Capture(World world, List<Actor> actors, Dictionary<long, int> column, ReplicationBuffer bytes, ref int remaining);

        /// <summary>
        ///   <para>创建连接独占的解码暂存。</para>
        /// </summary>
        internal abstract ReplicationBatch CreateBatch();
    }

    /// <summary>
    ///   <para>强类型复制字段；热路径不使用反射或装箱。</para>
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal sealed class ReplicationComponent<T> : ReplicationComponent where T : unmanaged, IComponent
    {
        /// <summary>
        ///   <para>创建时固定的编码策略。</para>
        /// </summary>
        private readonly IReplicationCodec<T> m_Codec;

        /// <summary>
        ///   <para>创建强类型字段。</para>
        /// </summary>
        /// <param name="id">协议 ID。</param>
        /// <param name="size">编码长度。</param>
        /// <param name="codec">编码器。</param>
        internal ReplicationComponent(uint id, int size, IReplicationCodec<T> codec) : base(id, size) {
            m_Codec = codec;
            Format = codec is ReplicationCodec<T> automatic ? automatic.Format : 0;
        }

        /// <inheritdoc />
        internal override void Capture(World world, List<Actor> actors, Dictionary<long, int> column, ReplicationBuffer bytes, ref int remaining)
        {
            var pool = world.Actors.GetPool<T>();
            if (pool == null) return;
            foreach (var actor in actors)
            {
                if (!pool.TryGet(actor, out var value)) continue;
                if (column.Count == 0) remaining -= 12;
                if (remaining < 8 + Size) throw new InvalidOperationException("Replication state exceeds MaxStateBytes.");
                remaining -= 8 + Size;
                column.Add(actor.id, bytes.Length);
                m_Codec.Encode(bytes.Append(Size), value);
            }
        }

        /// <inheritdoc />
        internal override ReplicationBatch CreateBatch() => new ReplicationBatch<T>(m_Codec);
    }
}
