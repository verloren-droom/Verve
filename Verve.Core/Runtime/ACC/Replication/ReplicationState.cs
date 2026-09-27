// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>世界状态；每个连接复用两份状态进行增量比较。</para>
    /// </summary>
    internal sealed class ReplicationState
    {
        /// <summary>
        ///   <para>可见实体集合。</para>
        /// </summary>
        internal readonly HashSet<long> Entities = new();
        /// <summary>
        ///   <para>类型列；与冻结 Schema 顺序一致。</para>
        /// </summary>
        internal readonly Dictionary<long, int>[] Columns;
        /// <summary>
        ///   <para>各组件列共享的连续编码区；整份状态只保留一份有界容量。</para>
        /// </summary>
        internal readonly ReplicationBuffer Bytes;

        /// <summary>
        ///   <para>创建空状态。</para>
        /// </summary>
        /// <param name="count">字段数。</param>
        /// <param name="maximum">单份状态的字节预算。</param>
        internal ReplicationState(int count, int maximum)
        {
            Columns = new Dictionary<long, int>[count];
            for (int i = 0; i < count; i++) Columns[i] = new Dictionary<long, int>();
            Bytes = new ReplicationBuffer(maximum);
        }

        /// <summary>
        ///   <para>捕获可见状态；编码前检查整份状态预算。</para>
        /// </summary>
        /// <param name="world">源世界。</param>
        /// <param name="actors">已筛选的实体。</param>
        /// <param name="components">冻结字段。</param>
        /// <param name="maximum">字节预算。</param>
        internal void Capture(World world, List<Actor> actors, ReplicationComponent[] components, int maximum)
        {
            Entities.Clear();
            Bytes.Clear();
            int remaining = maximum - 12 - actors.Count * 8;
            if (remaining < 0) throw new InvalidOperationException("Replication state exceeds MaxStateBytes.");
            foreach (var actor in actors) Entities.Add(actor.id);
            for (int i = 0; i < components.Length; i++)
            {
                Columns[i].Clear();
                components[i].Capture(world, actors, Columns[i], Bytes, ref remaining);
            }
        }

        /// <summary>
        ///   <para>生成实体生命周期和组件列差异；没有变化时只保留三个零计数。</para>
        /// </summary>
        /// <param name="baseline">上一完整发送状态。</param>
        /// <param name="components">字段 Schema。</param>
        /// <param name="output">批次缓冲区。</param>
        internal void WriteDelta(ReplicationState baseline, ReplicationComponent[] components, ReplicationBuffer output)
        {
            output.Clear();
            WriteDifference(Entities, baseline.Entities, output);
            WriteDifference(baseline.Entities, Entities, output);
            int columnCountOffset = output.Length;
            output.WriteUInt(0);
            int columnCount = 0;
            for (int i = 0; i < components.Length; i++)
            {
                var current = Columns[i];
                var previous = baseline.Columns[i];
                int start = -1;
                int removed = 0;
                foreach (var entry in previous)
                {
                    if (!Entities.Contains(entry.Key) || current.ContainsKey(entry.Key)) continue;
                    if (start < 0)
                    {
                        start = output.Length;
                        output.WriteUInt(components[i].Id);
                        output.WriteUInt(0);
                    }
                    output.WriteId(entry.Key);
                    removed++;
                }
                int updatedOffset = -1;
                int updated = 0;
                foreach (var entry in current)
                {
                    var bytes = Bytes.Span.Slice(entry.Value, components[i].Size);
                    if (previous.TryGetValue(entry.Key, out int offset) &&
                        bytes.SequenceEqual(baseline.Bytes.Span.Slice(offset, components[i].Size))) continue;
                    if (start < 0)
                    {
                        start = output.Length;
                        output.WriteUInt(components[i].Id);
                        output.WriteUInt(0);
                    }
                    if (updatedOffset < 0)
                    {
                        updatedOffset = output.Length;
                        output.WriteUInt(0);
                    }
                    output.WriteId(entry.Key);
                    bytes.CopyTo(output.Append(bytes.Length));
                    updated++;
                }
                if (start >= 0)
                {
                    output.SetCount(start + 4, removed);
                    if (updatedOffset < 0) output.WriteUInt(0);
                    else output.SetCount(updatedOffset, updated);
                    columnCount++;
                }
            }
            output.SetCount(columnCountOffset, columnCount);
        }

        /// <summary>
        ///   <para>写入仅存在于左集合的实体。</para>
        /// </summary>
        /// <param name="left">目标集合。</param>
        /// <param name="right">已知集合。</param>
        /// <param name="output">输出批次。</param>
        private static void WriteDifference(HashSet<long> left, HashSet<long> right, ReplicationBuffer output)
        {
            int offset = output.Length;
            output.WriteUInt(0);
            int count = 0;
            foreach (long id in left)
            {
                if (right.Contains(id)) continue;
                output.WriteId(id);
                count++;
            }
            output.SetCount(offset, count);
        }
    }
}