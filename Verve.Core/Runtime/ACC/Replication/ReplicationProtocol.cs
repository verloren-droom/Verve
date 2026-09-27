// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Buffers.Binary;
    using System.IO;

    /// <summary>
    ///   <para>复制包头；48 字节小端序，与 Rust ACC v2 保持一致。</para>
    /// </summary>
    internal readonly struct ReplicationHeader
    {
        /// <summary>
        ///   <para>固定包头长度。</para>
        /// </summary>
        internal const int Size = 48;
        /// <summary>
        ///   <para>协议标记；小端字节为 VACC。</para>
        /// </summary>
        private const uint Magic = 0x43434156;
        /// <summary>
        ///   <para>是否为首次完整状态。</para>
        /// </summary>
        internal readonly bool Full;
        /// <summary>
        ///   <para>组件协议指纹。</para>
        /// </summary>
        internal readonly ulong Schema;
        /// <summary>
        ///   <para>当前批次序列；会话内不回绕。</para>
        /// </summary>
        internal readonly ulong Sequence;
        /// <summary>
        ///   <para>依赖的上一批次。</para>
        /// </summary>
        internal readonly ulong Baseline;
        /// <summary>
        ///   <para>权威端采样 Tick。</para>
        /// </summary>
        internal readonly ulong Tick;
        /// <summary>
        ///   <para>批次总字节数。</para>
        /// </summary>
        internal readonly int Total;
        /// <summary>
        ///   <para>分片偏移。</para>
        /// </summary>
        internal readonly int Offset;

        /// <summary>
        ///   <para>创建批次包头。</para>
        /// </summary>
        /// <param name="schema">协议指纹。</param>
        /// <param name="sequence">批次序列。</param>
        /// <param name="baseline">依赖基线。</param>
        /// <param name="tick">采样 Tick。</param>
        /// <param name="total">批次字节数。</param>
        /// <param name="offset">分片偏移。</param>
        internal ReplicationHeader(ulong schema, ulong sequence, ulong baseline, ulong tick, int total, int offset)
        {
            Full = baseline == 0;
            Schema = schema;
            Sequence = sequence;
            Baseline = baseline;
            Tick = tick;
            Total = total;
            Offset = offset;
        }

        /// <summary>
        ///   <para>解析完整帧包头；拒绝旧协议、未知标志和越界分片。</para>
        /// </summary>
        /// <param name="packet">完整传输帧。</param>
        /// <param name="maxStateBytes">批次上限。</param>
        internal static ReplicationHeader Read(ReadOnlySpan<byte> packet, int maxStateBytes)
        {
            if (packet.Length <= Size || BinaryPrimitives.ReadUInt32LittleEndian(packet) != Magic ||
                packet[4] != 2 || (packet[5] != 1 && packet[5] != 2) ||
                BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(6)) != 0)
                throw new InvalidDataException("Invalid ACC v2 replication header.");
            ulong schema = BinaryPrimitives.ReadUInt64LittleEndian(packet.Slice(8));
            ulong sequence = BinaryPrimitives.ReadUInt64LittleEndian(packet.Slice(16));
            ulong baseline = BinaryPrimitives.ReadUInt64LittleEndian(packet.Slice(24));
            ulong tick = BinaryPrimitives.ReadUInt64LittleEndian(packet.Slice(32));
            uint total = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(40));
            uint offset = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(44));
            if (schema == 0 || sequence == 0 || tick == 0 || baseline == ulong.MaxValue || sequence != baseline + 1 ||
                (packet[5] == 1) != (baseline == 0) || total < 12 || total > maxStateBytes ||
                offset >= total || packet.Length - Size > total - offset)
                throw new InvalidDataException("Invalid replication sequence or fragment range.");
            return new ReplicationHeader(schema, sequence, baseline, tick, (int)total, (int)offset);
        }

        /// <summary>
        ///   <para>写入固定包头。</para>
        /// </summary>
        /// <param name="packet">长度至少为 <see cref="Size"/> 的目标。</param>
        internal void Write(Span<byte> packet)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(packet, Magic);
            packet[4] = 2;
            packet[5] = Full ? (byte)1 : (byte)2;
            BinaryPrimitives.WriteUInt16LittleEndian(packet.Slice(6), 0);
            BinaryPrimitives.WriteUInt64LittleEndian(packet.Slice(8), Schema);
            BinaryPrimitives.WriteUInt64LittleEndian(packet.Slice(16), Sequence);
            BinaryPrimitives.WriteUInt64LittleEndian(packet.Slice(24), Baseline);
            BinaryPrimitives.WriteUInt64LittleEndian(packet.Slice(32), Tick);
            BinaryPrimitives.WriteUInt32LittleEndian(packet.Slice(40), (uint)Total);
            BinaryPrimitives.WriteUInt32LittleEndian(packet.Slice(44), (uint)Offset);
        }
    }

    /// <summary>
    ///   <para>复制缓冲区；按需增长到固定上限，由连接复用。</para>
    /// </summary>
    internal sealed class ReplicationBuffer
    {
        /// <summary>
        ///   <para>容量上限。</para>
        /// </summary>
        private readonly int m_Maximum;
        /// <summary>
        ///   <para>连续字节存储。</para>
        /// </summary>
        private byte[] m_Bytes = Array.Empty<byte>();
        /// <summary>
        ///   <para>有效字节数。</para>
        /// </summary>
        internal int Length { get; private set; }
        /// <summary>
        ///   <para>有效只读数据。</para>
        /// </summary>
        internal ReadOnlySpan<byte> Span => m_Bytes.AsSpan(0, Length);

        /// <summary>
        ///   <para>创建有界缓冲区。</para>
        /// </summary>
        /// <param name="maximum">容量上限。</param>
        internal ReplicationBuffer(int maximum) => m_Maximum = maximum;

        /// <summary>
        ///   <para>追加字节区域；超过上限立即报错。</para>
        /// </summary>
        /// <param name="size">追加长度。</param>
        internal Span<byte> Append(int size)
        {
            if (size < 0 || size > m_Maximum - Length) throw new InvalidDataException("Replication state exceeds its byte limit.");
            int end = Length + size;
            if (end > m_Bytes.Length)
                Array.Resize(ref m_Bytes, Math.Min(m_Maximum, Math.Max(end, Math.Max(256, m_Bytes.Length * 2))));
            var span = m_Bytes.AsSpan(Length, size);
            Length = end;
            return span;
        }

        /// <summary>
        ///   <para>写入 32 位无符号整数。</para>
        /// </summary>
        /// <param name="value">整数。</param>
        internal void WriteUInt(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Append(4), value);
        /// <summary>
        ///   <para>写入实体标识。</para>
        /// </summary>
        /// <param name="id">实体标识。</param>
        internal void WriteId(long id) => BinaryPrimitives.WriteInt64LittleEndian(Append(8), id);
        /// <summary>
        ///   <para>完成预留计数。</para>
        /// </summary>
        /// <param name="offset">计数位置。</param>
        /// <param name="count">计数。</param>
        internal void SetCount(int offset, int count) => BinaryPrimitives.WriteInt32LittleEndian(m_Bytes.AsSpan(offset, 4), count);
        /// <summary>
        ///   <para>清空长度并保留容量。</para>
        /// </summary>
        internal void Clear() => Length = 0;
    }

    /// <summary>
    ///   <para>复制读取器；每次读取都验证边界，不返回错误默认值。</para>
    /// </summary>
    internal ref struct ReplicationReader
    {
        /// <summary>
        ///   <para>尚未读取的字节。</para>
        /// </summary>
        private ReadOnlySpan<byte> m_Remaining;
        /// <summary>
        ///   <para>剩余字节数。</para>
        /// </summary>
        internal int Remaining => m_Remaining.Length;
        /// <summary>
        ///   <para>创建读取器。</para>
        /// </summary>
        /// <param name="bytes">完整批次。</param>
        internal ReplicationReader(ReadOnlySpan<byte> bytes) => m_Remaining = bytes;
        /// <summary>
        ///   <para>读取区域。</para>
        /// </summary>
        /// <param name="size">区域长度。</param>
        internal ReadOnlySpan<byte> Read(int size)
        {
            if (size < 0 || size > m_Remaining.Length) throw new InvalidDataException("Truncated replication state.");
            var result = m_Remaining.Slice(0, size);
            m_Remaining = m_Remaining.Slice(size);
            return result;
        }
        /// <summary>
        ///   <para>读取字段 ID。</para>
        /// </summary>
        internal uint ReadUInt() => BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
        /// <summary>
        ///   <para>读取实体 ID。</para>
        /// </summary>
        internal long ReadId()
        {
            long id = BinaryPrimitives.ReadInt64LittleEndian(Read(8));
            if (id <= 0) throw new InvalidDataException("Replication entity IDs must be positive.");
            return id;
        }
        /// <summary>
        ///   <para>读取有界计数；先验证剩余字节，避免畸形计数触发大分配。</para>
        /// </summary>
        /// <param name="stride">每项最小长度。</param>
        /// <param name="maximum">最大项数。</param>
        internal int ReadCount(int stride, int maximum)
        {
            uint count = ReadUInt();
            if (count > maximum || count > Remaining / stride) throw new InvalidDataException("Invalid replication count.");
            return (int)count;
        }
    }
}
