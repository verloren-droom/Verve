// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>行动者；以索引和版本共同标识生命周期。</para>
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential), DebuggerDisplay("{ToString}")]
    public readonly struct Actor : IEquatable<Actor>, IComparable<Actor>
    {
        /// <summary>
        ///   <para>空行动者的内部编码。</para>
        /// </summary>
        private const long NONE_ID = -1L;
        /// <summary>
        ///   <para>索引字段的位掩码。</para>
        /// </summary>
        private const uint INDEX_MASK = 0x7FFFFFFFU;
        /// <summary>
        ///   <para>版本字段的位掩码。</para>
        /// </summary>
        private const uint VERSION_MASK = 0x7FFFFFFFU;
        /// <summary>
        ///   <para>有效行动者的最小版本。</para>
        /// </summary>
        private const uint MIN_VERSION = 1U;

        /// <summary>
        ///   <para>空行动者标识符。</para>
        /// </summary>
        public static readonly Actor none = new Actor(NONE_ID);
        /// <summary>
        ///   <para>行动者标识符（64位：高32位索引，低32位版本号）</para>
        /// </summary>
        public readonly long id;

        /// <summary>
        ///   <para>行动者索引。</para>
        /// </summary>
        public int Index { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (int)((uint)(id >> 32) & INDEX_MASK); }
        /// <summary>
        ///   <para>行动者版本。</para>
        /// </summary>
        public int Version { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (int)((uint)id & VERSION_MASK); }
        /// <summary>
        ///   <para>是否为空行动者。</para>
        /// </summary>
        public bool IsNone { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => id == NONE_ID; }

        /// <summary>
        ///   <para>从打包后的 64 位标识创建行动者。</para>
        /// </summary>
        /// <param name="id">高 32 位为索引、低 32 位为版本的标识。</param>
        public Actor(long id) => this.id = id;

        /// <summary>
        ///   <para>从索引和版本创建行动者。</para>
        /// </summary>
        /// <param name="index">非负索引。</param>
        /// <param name="version">大于零的版本。</param>
        public Actor(int index, int version)
        {
            if ((uint)index > INDEX_MASK || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if ((uint)version > VERSION_MASK || version < MIN_VERSION)
                throw new ArgumentOutOfRangeException(nameof(version));

            id = ((long)index << 32) | (uint)version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Actor left, Actor right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Actor left, Actor right) => !left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator long(Actor actor) => actor.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(Actor other) => id == other.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is Actor other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(Actor other) => id.CompareTo(other.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => id.GetHashCode();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => IsNone ? "Actor(None)" : $"Actor({Index}:{Version})";
    }
}
