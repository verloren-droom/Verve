// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>组件掩码；位图，用于快速组件匹配。</para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentMask : IEquatable<ComponentMask>
    {
        /// <summary>
        ///   <para>按升序枚举掩码中已设置的组件类型标识。</para>
        /// </summary>
        public ref struct Enumerator
        {
            /// <summary>
            ///   <para>正在枚举的掩码快照。</para>
            /// </summary>
            private readonly ComponentMask m_Mask;
            /// <summary>
            ///   <para>当前位块索引。</para>
            /// </summary>
            private int m_BlockIndex;
            /// <summary>
            ///   <para>当前位索引。</para>
            /// </summary>
            private int m_BitIndex;
            /// <summary>
            ///   <para>当前位块缓存。</para>
            /// </summary>
            private ulong m_CurrentBlock;
            /// <summary>
            ///   <para>当前组件类型标识。</para>
            /// </summary>
            private int m_Current;
            /// <summary>
            ///   <para>当前已设置的组件类型标识。</para>
            /// </summary>
            public int Current => m_Current;

            /// <summary>
            ///   <para>创建掩码枚举器。</para>
            /// </summary>
            /// <param name="mask">要枚举的掩码。</param>
            public Enumerator(ComponentMask mask)
            {
                m_Mask = mask;
                m_BlockIndex = -1;
                m_BitIndex = -1;
                m_CurrentBlock = 0;
                m_Current = -1;
            }
            /// <summary>
            ///   <para>移动到下一个已设置位。</para>
            /// </summary>
            public bool MoveNext()
            {
                while (true)
                {
                    if (m_BitIndex < 0)
                    {
                        m_BlockIndex++;
                        if (m_BlockIndex >= m_Mask.m_BlockCount) return false;

                        m_CurrentBlock = m_Mask.GetBlock(m_BlockIndex);
                        m_BitIndex = 0;

                        if (m_CurrentBlock == 0)
                        {
                            m_BitIndex = -1;
                            continue;
                        }
                    }

                    while (m_BitIndex < BITS_PER_BLOCK)
                    {
                        if ((m_CurrentBlock & (1UL << m_BitIndex)) != 0)
                        {
                            m_Current = m_BlockIndex * BITS_PER_BLOCK + m_BitIndex;
                            m_BitIndex++;
                            return true;
                        }
                        m_BitIndex++;
                    }

                    m_BitIndex = -1;
                }
            }
        }

        /// <summary>
        ///   <para>每个掩码块包含的位数。</para>
        /// </summary>
        private const int BITS_PER_BLOCK = 64;
        /// <summary>
        ///   <para>掩码的最小块数。</para>
        /// </summary>
        private const int INIT_BLOCK_COUNT = 4;
        /// <summary>
        ///   <para>内联存储的块数。</para>
        /// </summary>
        private const int MAX_INLINE_BLOCKS = 4;

        /// <summary>
        ///   <para>第一个内联位块。</para>
        /// </summary>
        private ulong m_InlineBlock0;
        /// <summary>
        ///   <para>第二个内联位块。</para>
        /// </summary>
        private ulong m_InlineBlock1;
        /// <summary>
        ///   <para>第三个内联位块。</para>
        /// </summary>
        private ulong m_InlineBlock2;
        /// <summary>
        ///   <para>第四个内联位块。</para>
        /// </summary>
        private ulong m_InlineBlock3;
        /// <summary>
        ///   <para>超出内联范围时存储额外位块的数组。</para>
        /// </summary>
        private ulong[] m_Blocks;
        /// <summary>
        ///   <para>当前分配的位块数。</para>
        /// </summary>
        private int m_BlockCount;

        /// <summary>
        ///   <para>掩码是否为空。</para>
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_InlineBlock0 != 0 || m_InlineBlock1 != 0 ||
                    m_InlineBlock2 != 0 || m_InlineBlock3 != 0)
                    return false;

                if (m_Blocks != null)
                {
                    for (int i = MAX_INLINE_BLOCKS; i < m_BlockCount; i++)
                        if (m_Blocks[i] != 0) return false;
                }
                return true;
            }
        }

        /// <summary>
        ///   <para>创建组件掩码；容量按位数计。</para>
        /// </summary>
        /// <param name="initialCapacity">预留的组件类型位数。</param>
        public ComponentMask(int initialCapacity = 64)
        {
            int blockCount = Math.Max((initialCapacity + BITS_PER_BLOCK - 1) / BITS_PER_BLOCK, INIT_BLOCK_COUNT);
            m_InlineBlock0 = m_InlineBlock1 = m_InlineBlock2 = m_InlineBlock3 = 0;
            m_Blocks = blockCount > MAX_INLINE_BLOCKS ? new ulong[blockCount] : null;
            m_BlockCount = blockCount;
        }

        /// <summary>
        ///   <para>设置指定组件类型在掩码中的存在状态。</para>
        /// </summary>
        /// <param name="componentTypeId">组件类型标识。</param>
        /// <param name="value"><see langword="true"/> 表示设置，<see langword="false"/> 表示清除。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int componentTypeId, bool value)
        {
            if (componentTypeId < 0) return;
            EnsureCapacity(componentTypeId + 1);

            int blockIndex = componentTypeId / BITS_PER_BLOCK;
            int bitIndex = componentTypeId % BITS_PER_BLOCK;
            ulong mask = 1UL << bitIndex;

            ulong block = GetBlock(blockIndex);
            if (value) block |= mask;
            else block &= ~mask;
            SetBlock(blockIndex, block);
        }

        /// <summary>
        ///   <para>获取指定组件类型在掩码中的存在状态。</para>
        /// </summary>
        /// <param name="componentTypeId">组件类型标识。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int componentTypeId)
        {
            if (componentTypeId < 0) return false;

            int blockIndex = componentTypeId / BITS_PER_BLOCK;
            if (blockIndex >= m_BlockCount) return false;

            int bitIndex = componentTypeId % BITS_PER_BLOCK;
            return (GetBlock(blockIndex) & (1UL << bitIndex)) != 0;
        }

        /// <summary>
        ///   <para>添加指定组件类型到掩码。</para>
        /// </summary>
        /// <param name="componentTypeId">组件类型标识。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int componentTypeId) => Set(componentTypeId, true);

        /// <summary>
        ///   <para>从掩码中移除指定组件类型。</para>
        /// </summary>
        /// <param name="componentTypeId">组件类型标识。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int componentTypeId) => Set(componentTypeId, false);

        /// <summary>
        ///   <para>清空掩码中的所有组件标记。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            m_InlineBlock0 = m_InlineBlock1 = m_InlineBlock2 = m_InlineBlock3 = 0;
            if (m_Blocks != null)
                Array.Clear(m_Blocks, 0, m_BlockCount);
        }

        /// <summary>
        ///   <para>判断当前掩码是否包含另一个掩码的全部组件。</para>
        /// </summary>
        /// <param name="other">要匹配的组件掩码。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsAll(in ComponentMask other)
        {
            int minBlocks = Math.Min(m_BlockCount, other.m_BlockCount);

            for (int i = 0; i < minBlocks; i++)
            {
                ulong otherBlock = other.GetBlock(i);
                if ((GetBlock(i) & otherBlock) != otherBlock)
                    return false;
            }

            for (int i = minBlocks; i < other.m_BlockCount; i++)
                if (other.GetBlock(i) != 0)
                    return false;

            return true;
        }

        /// <summary>
        ///   <para>判断当前掩码与另一个掩码是否存在任意交集。</para>
        /// </summary>
        /// <param name="other">要比较的组件掩码。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsAny(in ComponentMask other)
        {
            int minBlocks = Math.Min(m_BlockCount, other.m_BlockCount);
            for (int i = 0; i < minBlocks; i++)
                if ((GetBlock(i) & other.GetBlock(i)) != 0)
                    return true;

            return false;
        }

        /// <summary>
        ///   <para>判断当前掩码与另一个掩码是否完全无交集。</para>
        /// </summary>
        /// <param name="other">要比较的组件掩码。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsNone(in ComponentMask other) => !ContainsAny(other);

        /// <summary>
        ///   <para>创建当前掩码的副本。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentMask Clone()
        {
            var clone = new ComponentMask();
            clone.m_InlineBlock0 = m_InlineBlock0;
            clone.m_InlineBlock1 = m_InlineBlock1;
            clone.m_InlineBlock2 = m_InlineBlock2;
            clone.m_InlineBlock3 = m_InlineBlock3;
            clone.m_BlockCount = m_BlockCount;

            if (m_Blocks != null)
            {
                clone.m_Blocks = new ulong[m_Blocks.Length];
                Array.Copy(m_Blocks, clone.m_Blocks, m_BlockCount);
            }

            return clone;
        }

        /// <summary>
        ///   <para>读取指定位块。</para>
        /// </summary>
        /// <param name="blockIndex">位块索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetBlock(int blockIndex)
        {
            if (blockIndex < MAX_INLINE_BLOCKS)
            {
                switch (blockIndex)
                {
                    case 0: return m_InlineBlock0;
                    case 1: return m_InlineBlock1;
                    case 2: return m_InlineBlock2;
                    case 3: return m_InlineBlock3;
                }
            }
            return m_Blocks[blockIndex];
        }

        /// <summary>
        ///   <para>写入指定位块。</para>
        /// </summary>
        /// <param name="blockIndex">位块索引。</param>
        /// <param name="value">新的位块数据。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBlock(int blockIndex, ulong value)
        {
            if (blockIndex < MAX_INLINE_BLOCKS)
            {
                switch (blockIndex)
                {
                    case 0: m_InlineBlock0 = value; break;
                    case 1: m_InlineBlock1 = value; break;
                    case 2: m_InlineBlock2 = value; break;
                    case 3: m_InlineBlock3 = value; break;
                }
            }
            else
            {
                m_Blocks[blockIndex] = value;
            }
        }

        /// <summary>
        ///   <para>扩展掩码容量以容纳指定位数。</para>
        /// </summary>
        /// <param name="requiredBits">所需的总位数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int requiredBits)
        {
            int requiredBlocks = (requiredBits + BITS_PER_BLOCK - 1) / BITS_PER_BLOCK;

            if (m_BlockCount < requiredBlocks)
            {
                int newBlockCount = Math.Max(m_BlockCount * 2, requiredBlocks);

                if (m_Blocks == null && newBlockCount > MAX_INLINE_BLOCKS)
                {
                    m_Blocks = new ulong[newBlockCount];
                    m_Blocks[0] = m_InlineBlock0;
                    m_Blocks[1] = m_InlineBlock1;
                    m_Blocks[2] = m_InlineBlock2;
                    m_Blocks[3] = m_InlineBlock3;
                }
                else if (m_Blocks != null)
                {
                    Array.Resize(ref m_Blocks, newBlockCount);
                }

                m_BlockCount = newBlockCount;
            }
        }

        public bool Equals(ComponentMask other)
        {
            if (m_InlineBlock0 != other.m_InlineBlock0 || m_InlineBlock1 != other.m_InlineBlock1 ||
                m_InlineBlock2 != other.m_InlineBlock2 || m_InlineBlock3 != other.m_InlineBlock3)
                return false;

            int maxBlocks = Math.Max(m_BlockCount, other.m_BlockCount);
            for (int i = MAX_INLINE_BLOCKS; i < maxBlocks; i++)
            {
                ulong left = i < m_BlockCount && m_Blocks != null ? m_Blocks[i] : 0;
                ulong right = i < other.m_BlockCount && other.m_Blocks != null ? other.m_Blocks[i] : 0;
                if (left != right) return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ComponentMask other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + m_InlineBlock0.GetHashCode();
            hash = hash * 31 + m_InlineBlock1.GetHashCode();
            hash = hash * 31 + m_InlineBlock2.GetHashCode();
            hash = hash * 31 + m_InlineBlock3.GetHashCode();

            if (m_Blocks != null)
            {
                for (int i = MAX_INLINE_BLOCKS; i < m_BlockCount; i++)
                    if (m_Blocks[i] != 0)
                        hash = hash * 31 + m_Blocks[i].GetHashCode();
            }

            return hash;
        }

        /// <summary>
        ///   <para>获取已设置位的枚举器。</para>
        /// </summary>
        public Enumerator GetEnumerator() => new(this);
    }
}