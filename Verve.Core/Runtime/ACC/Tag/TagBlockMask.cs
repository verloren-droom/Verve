// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>标签阻塞信息；表示一个发起者对标签的阻塞计数。</para>
    /// </summary>
    internal readonly struct TagBlockInfo
    {
        /// <summary>
        ///   <para>标签标识；指示被阻塞的标签。</para>
        /// </summary>
        public readonly TagId tagId;
        /// <summary>
        ///   <para>发起者；用于区分独立的阻塞所有者。</para>
        /// </summary>
        public readonly object instigator;
        /// <summary>
        ///   <para>阻塞次数；需要匹配次数的解除操作才能清除。</para>
        /// </summary>
        public readonly int blockCount;

        /// <summary>
        ///   <para>创建标签阻塞快照。</para>
        /// </summary>
        /// <param name="tagId">被阻塞的标签。</param>
        /// <param name="instigator">持有阻塞的发起者。</param>
        /// <param name="blockCount">当前阻塞次数。</param>
        public TagBlockInfo(TagId tagId, object instigator, int blockCount)
        {
            this.tagId = tagId;
            this.instigator = instigator;
            this.blockCount = blockCount;
        }
    }

    /// <summary>
    ///   <para>标签阻塞记录；按发起者计数，仅保存有效阻塞。</para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TagBlockMask
    {
        /// <summary>
        ///   <para>单个标签的发起者计数。</para>
        /// </summary>
        private struct BlockEntry
        {
            /// <summary>
            ///   <para>发起者；拥有对应阻塞引用。</para>
            /// </summary>
            public object instigator;
            /// <summary>
            ///   <para>阻塞计数；为零时移除该条目。</para>
            /// </summary>
            public int blockCount;
        }
        
        /// <summary>
        ///   <para>初始标签容量；减少常见情况的扩容次数。</para>
        /// </summary>
        private const int INITIAL_CAPACITY = 4;

        /// <summary>
        ///   <para>标签到发起者计数列表的映射。</para>
        /// </summary>
        private Dictionary<int, List<BlockEntry>> m_TagBlocks;

        /// <summary>
        ///   <para>检查标签是否至少被一个发起者阻塞。</para>
        /// </summary>
        /// <param name="tagId">待检查标签。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTagBlocked(TagId tagId) => m_TagBlocks?.ContainsKey(tagId) == true;

        /// <summary>
        ///   <para>复制当前有效阻塞项到结果列表。</para>
        /// </summary>
        /// <param name="output">接收快照的列表；调用前内容会被清空。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetTagBlocks(List<TagBlockInfo> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (m_TagBlocks == null) return;

            foreach (var pair in m_TagBlocks)
            {
                var blocks = pair.Value;
                var tagId = new TagId(pair.Key);
                for (int i = 0; i < blocks.Count; i++)
                {
                    var entry = blocks[i];
                    output.Add(new TagBlockInfo(tagId, entry.instigator, entry.blockCount));
                }
            }
        }

        /// <summary>
        ///   <para>增加发起者对标签的阻塞计数。</para>
        /// </summary>
        /// <param name="tagId">要阻塞的标签。</param>
        /// <param name="instigator">阻塞发起者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BlockTag(TagId tagId, object instigator)
        {
            m_TagBlocks ??= new Dictionary<int, List<BlockEntry>>(INITIAL_CAPACITY);

            if (!m_TagBlocks.TryGetValue(tagId, out var blocks))
            {
                blocks = new List<BlockEntry>(2);
                m_TagBlocks[tagId] = blocks;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                var entry = blocks[i];
                if (entry.instigator == instigator)
                {
                    entry.blockCount = checked(entry.blockCount + 1);
                    blocks[i] = entry;
                    return;
                }
            }

            blocks.Add(new BlockEntry { instigator = instigator, blockCount = 1 });
        }

        /// <summary>
        ///   <para>减少发起者对标签的阻塞计数。</para>
        /// </summary>
        /// <param name="tagId">要解除阻塞的标签。</param>
        /// <param name="instigator">原阻塞发起者。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnblockTag(TagId tagId, object instigator)
        {
            if (m_TagBlocks == null || !m_TagBlocks.TryGetValue(tagId, out var blocks))
                return;

            for (int i = 0; i < blocks.Count; i++)
            {
                var entry = blocks[i];
                if (entry.instigator == instigator)
                {
                    entry.blockCount--;
                    if (entry.blockCount == 0)
                    {
                        blocks.RemoveAt(i);
                        if (blocks.Count == 0)
                            m_TagBlocks.Remove(tagId);
                    }
                    else
                    {
                        blocks[i] = entry;
                    }
                    return;
                }
            }
        }

        /// <summary>
        ///   <para>清除所有阻塞项并释放其引用。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            m_TagBlocks?.Clear();
            m_TagBlocks = null;
        }
    }
}