namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;


    /// <summary>
    ///   <para>显式持有 <see cref="GameModules"/> 容器生命周期的句柄</para>
    /// </summary>
    [Serializable]
    public sealed class GameModulesHandle : IDisposable, IAsyncDisposable
    {
        private GameModules m_Modules;

        internal GameModulesHandle(int id, GameModules modules)
        {
            Id = id;
            m_Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        }

        /// <summary>
        ///   <para>模块容器句柄编号</para>
        /// </summary>
        public int Id { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        ///   <para>当前句柄是否已经失效</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_Modules) == null;
        }

        /// <summary>
        ///   <para>当前持有的模块容器</para>
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
        ///   <para>尝试获取当前持有的模块容器</para>
        /// </summary>
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

        /// <summary>
        ///   <para>释放该句柄持有的模块容器</para>
        /// </summary>
        public void Dispose()
        {
            Game.DestroyModules(this);
        }

        /// <summary>
        ///   <para>异步释放该句柄持有的模块容器</para>
        /// </summary>
        public ValueTask DisposeAsync()
        {
            return Game.DestroyModulesAsync(this);
        }

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

        internal void AttachModules(GameModules modules)
        {
            if (modules == null) throw new ArgumentNullException(nameof(modules));
            if (Interlocked.CompareExchange(ref m_Modules, modules, null) != null)
            {
                throw new InvalidOperationException($"{nameof(GameModulesHandle)} already owns a modules container.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"{nameof(GameModulesHandle)}#{Id}";
        }
    }
}