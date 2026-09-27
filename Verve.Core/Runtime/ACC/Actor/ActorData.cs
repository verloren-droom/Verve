// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>行动者数据。</para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ActorData
    {
        /// <summary>
        ///   <para>版本号；区分复用同一槽位的行动者。</para>
        /// </summary>
        public int version;
        /// <summary>
        ///   <para>存活状态；标记槽位是否正在使用。</para>
        /// </summary>
        public bool isAlive;
        /// <summary>
        ///   <para>组件掩码；记录行动者拥有的组件类型。</para>
        /// </summary>
        public ComponentMask componentMask;
        /// <summary>
        ///   <para>能力列表；保存由管理器拥有的能力实例。</para>
        /// </summary>
        public List<CapabilityInstance> capabilities;
        /// <summary>
        ///   <para>标签阻塞；记录各发起者持有的阻塞计数。</para>
        /// </summary>
        public TagBlockMask tagBlocks;

        /// <summary>
        ///   <para>重置可复用槽位的数据。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            isAlive = false;
            componentMask.Clear();
            capabilities?.Clear();
            tagBlocks.Clear();
        }
    }
}
