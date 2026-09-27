// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;

    /// <summary>
    ///   <para>行动者存储配置；指定容量和访问锁。</para>
    /// </summary>
    [Serializable]
    public readonly struct ActorManagerOptions
    {
        /// <summary>
        ///   <para>最小容量；用于限制过小的存储池。</para>
        /// </summary>
        public const int MIN_CAPACITY = 64;
        /// <summary>
        ///   <para>最大容量；限制行动者存储的增长上限。</para>
        /// </summary>
        public const int MAX_CAPACITY = 2 * 1024 * 1024;

        /// <summary>
        ///   <para>初始容量（受最小/最大容量限制）</para>
        /// </summary>
        public readonly int initialCapacity;
        /// <summary>
        ///   <para>存储访问锁；不保护返回的组件引用，也不改变能力调度线程。</para>
        /// </summary>
        public readonly bool enableStorageThreadSafety;
        /// <summary>
        ///   <para>容量增长因子（至少为 1.0）</para>
        /// </summary>
        public readonly float growthFactor;

        /// <summary>
        ///   <para>创建存储配置。</para>
        /// </summary>
        /// <param name="initialCapacity">初始容量。</param>
        /// <param name="enableStorageThreadSafety">是否启用存储访问锁。</param>
        /// <param name="growthFactor">容量增长因子。</param>
        public ActorManagerOptions(
            int initialCapacity,
            bool enableStorageThreadSafety,
            float growthFactor)
        {
            if (float.IsNaN(growthFactor) || float.IsInfinity(growthFactor))
                throw new ArgumentOutOfRangeException(nameof(growthFactor));
            this.initialCapacity = Math.Clamp(initialCapacity, MIN_CAPACITY, MAX_CAPACITY);
            this.enableStorageThreadSafety = enableStorageThreadSafety;
            this.growthFactor = Math.Max(growthFactor, 1.0f);
        }

        /// <summary>
        ///   <para>默认配置；容量 1024，启用存储访问锁，增长因子 2。</para>
        /// </summary>
        internal static ActorManagerOptions Default => new(
            initialCapacity: 1024,
            enableStorageThreadSafety: true,
            growthFactor: 2.0f);
    }
}
