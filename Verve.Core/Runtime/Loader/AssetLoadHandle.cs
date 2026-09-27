namespace Verve
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>资源加载句柄；释放时归还加载器持有的资源引用。</para>
    /// </summary>
    /// <remarks>可交由 <see cref="AssetLoadScope"/> 批量托管。</remarks>
    /// <typeparam name="T">Unity 资源结果类型。</typeparam>
    public sealed class AssetLoadHandle<T> : IDisposable
        where T : UnityEngine.Object
    {
        /// <summary>
        ///   <para>释放回调。</para>
        /// </summary>
        private Action m_ReleaseAction;
        /// <summary>
        ///   <para>结果。</para>
        /// </summary>
        private T m_Result;
        /// <summary>
        ///   <para>是否已释放。</para>
        /// </summary>
        private int m_IsReleased;

        /// <summary>
        ///   <para>加载得到的资源实例。</para>
        /// </summary>
        public T Result => !IsReleased ? m_Result : throw new ObjectDisposedException(GetType().Name);

        /// <summary>
        ///   <para>当前句柄是否已经释放。</para>
        /// </summary>
        public bool IsReleased { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Volatile.Read(ref m_IsReleased) != 0; }

        /// <summary>
        ///   <para>创建资源加载句柄。</para>
        /// </summary>
        /// <param name="result">资源结果。</param>
        /// <param name="releaseAction">释放回调。</param>
        internal AssetLoadHandle(T result = default, Action releaseAction = null)
        {
            m_Result = result;
            m_ReleaseAction = releaseAction;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_IsReleased, 1) != 0)
            {
                return;
            }

            m_Result = null;
            Interlocked.Exchange(ref m_ReleaseAction, null)?.Invoke();
        }

        /// <summary>
        ///   <para>转交唯一释放权；原句柄立即失效，其后 <see cref="Dispose"/> 不影响新所有者。</para>
        /// </summary>
        internal Action TransferRelease()
        {
            if (Interlocked.Exchange(ref m_IsReleased, 1) != 0)
                throw new ObjectDisposedException(GetType().Name);
            m_Result = null;
            return Interlocked.Exchange(ref m_ReleaseAction, null);
        }
    }
}
