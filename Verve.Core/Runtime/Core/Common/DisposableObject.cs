namespace Verve
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>资源释放基类；通过原子标记保证只释放一次。</para>
    /// </summary>
    public abstract class DisposableObject : IDisposable
    {
        /// <summary>
        ///   <para>释放标记。</para>
        /// </summary>
        [NonSerialized] private int m_Disposed;

        /// <summary>
        ///   <para>对象是否已经进入释放状态。</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_Disposed) != 0;
        }

        /// <summary>
        ///   <para>释放派生类持有的资源。</para>
        /// </summary>
        protected virtual void OnDispose() { }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_Disposed, 1) == 0) OnDispose();
        }

        /// <summary>
        ///   <para>已释放时抛出异常。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        protected void ThrowIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
        }
    }
}