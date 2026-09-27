namespace Verve
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>引用比较器；按实例身份比较，避免值相等影响所有权判断。</para>
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        /// <summary>
        ///   <para>实例。</para>
        /// </summary>
        public static readonly ReferenceEqualityComparer<T> Instance = new();

        /// <summary>
        ///   <para>创建引用相等性比较器。</para>
        /// </summary>
        private ReferenceEqualityComparer() { }

        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }
}