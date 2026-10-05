// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    /// <summary>
    ///   <para>能力实例。</para>
    /// </summary>
    internal struct CapabilityInstance
    {
        /// <summary>
        ///   <para>由能力管理器拥有的能力对象。</para>
        /// </summary>
        public Capability capability;
        /// <summary>
        ///   <para>能力所属的更新阶段标识。</para>
        /// </summary>
        public int tickGroup;
        /// <summary>
        ///   <para>能力在阶段内的执行顺序。</para>
        /// </summary>
        public int tickOrder;
    }
}