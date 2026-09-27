namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>模块容器句柄；持有并释放容器。</para>
    /// </summary>
    [Serializable]
    public sealed class GameModulesHandle : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///   <para>模块容器。</para>
        /// </summary>
        private GameModules m_Modules;

        /// <summary>
        ///   <para>创建模块容器句柄。</para>
        /// </summary>
        /// <param name="id">标识。</param>
        /// <param name="modules">模块。</param>
        internal GameModulesHandle(int id, GameModules modules)
        {
            Id = id;
            m_Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        }

        /// <summary>
        ///   <para>模块容器句柄编号。</para>
        /// </summary>
        public int Id { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        ///   <para>当前句柄是否已经失效。</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_Modules) == null;
        }

        /// <summary>
        ///   <para>当前持有的模块容器。</para>
        /// </summary>
        public GameModules Modules
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!TryGetModules(out var modules))
                {
                    throw new ObjectDisposedException(nameof(GameModulesHandle));
                }

                return modules;
            }
        }

        /// <summary>
        ///   <para>尝试获取当前持有的模块容器。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        public bool TryGetModules(out GameModules modules)
        {
            modules = Volatile.Read(ref m_Modules);
            if (modules == null || modules.IsDisposedOrDisposing)
            {
                modules = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public void Dispose() => Game.DestroyModules(this);

        /// <inheritdoc />
        public ValueTask DisposeAsync() => Game.DestroyModulesAsync(this);

        /// <summary>
        ///   <para>异步释放该句柄持有的模块容器。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        public ValueTask DisposeAsync(CancellationToken ct) => Game.DestroyModulesAsync(this, ct);

        /// <summary>
        ///   <para>尝试移交并移除容器引用。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        internal bool TryDetachModules(out GameModules modules)
        {
            while (true)
            {
                var current = Volatile.Read(ref m_Modules);
                if (current == null)
                {
                    modules = null;
                    return false;
                }

                if (Interlocked.CompareExchange(ref m_Modules, null, current) == current)
                {
                    modules = current;
                    return true;
                }
            }
        }

        /// <summary>
        ///   <para>尝试移交并移除容器引用。</para>
        /// </summary>
        /// <param name="expectedModules">预期模块。</param>
        internal bool TryDetachModules(GameModules expectedModules)
        {
            if (expectedModules == null)
            {
                return false;
            }

            while (true)
            {
                var current = Volatile.Read(ref m_Modules);
                if (!ReferenceEquals(current, expectedModules))
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref m_Modules, null, current) == current)
                {
                    return true;
                }
            }
        }

        /// <summary>
        ///   <para>绑定模块。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        internal void AttachModules(GameModules modules)
        {
            if (modules == null) throw new ArgumentNullException(nameof(modules));
            if (Interlocked.CompareExchange(ref m_Modules, modules, null) != null)
            {
                throw new InvalidOperationException($"{nameof(GameModulesHandle)} already owns a modules container.");
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{nameof(GameModulesHandle)}#{Id}";
    }
}