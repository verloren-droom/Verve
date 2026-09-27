namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>Tick 执行范围；记录当前线程的回调嵌套深度。</para>
    /// </summary>
    internal static class TickExecutionScope
    {
        /// <summary>
        ///   <para>回调嵌套深度。</para>
        /// </summary>
        [ThreadStatic] private static int s_Depth;

        /// <summary>
        ///   <para>当前线程是否正在执行 Tick。</para>
        /// </summary>
        public static bool IsActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_Depth != 0;
        }

        /// <summary>
        ///   <para>进入作用域。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scope Enter()
        {
            s_Depth++;
            return default;
        }

        /// <summary>
        ///   <para>Tick 执行句柄；释放时退出当前范围。</para>
        /// </summary>
        public readonly struct Scope : IDisposable
        {
            /// <inheritdoc />
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => s_Depth--;
        }
    }

    /// <summary>
    ///   <para>GameLoop 主线程 Tick 系统调度器接口。</para>
    /// </summary>
    public interface IGameLoopTickSystemScheduler : IDisposable
    {
        /// <summary>
        ///   <para>添加主线程 Tick 系统。</para>
        /// </summary>
        /// <param name="system">系统实例。</param>
        void AddSystem(object system);

        /// <summary>
        ///   <para>移除主线程 Tick 系统。</para>
        /// </summary>
        /// <param name="system">系统实例。</param>
        bool RemoveSystem(object system);

        /// <summary>
        ///   <para>执行指定 GameLoop 阶段的主线程 Tick。</para>
        /// </summary>
        /// <param name="deltaTime">间隔时间。</param>
        /// <param name="group">更新组。</param>
        void Tick(float deltaTime, TickGroup group);

        /// <summary>
        ///   <para>等待 GameLoop Tick 调度器进入空闲状态。</para>
        /// </summary>
        void WaitForIdle();
    }
    
    /// <summary>
    ///   <para>默认 GameLoop 主线程 Tick 系统调度器。</para>
    /// </summary>
    internal sealed class GameLoopTickSystemScheduler : IGameLoopTickSystemScheduler
    {
        /// <summary>
        ///   <para>默认容量。</para>
        /// </summary>
        private const int k_DefaultCapacity = 16;

        /// <summary>
        ///   <para>释放状态。</para>
        /// </summary>
        private enum DisposeState : byte
        {
            /// <summary>
            ///   <para>存活。</para>
            /// </summary>
            Alive = 0,
            /// <summary>
            ///   <para>释放中。</para>
            /// </summary>
            Disposing = 1,
            /// <summary>
            ///   <para>已释放。</para>
            /// </summary>
            Disposed = 2,
        }

        /// <summary>
        ///   <para>系统锁。</para>
        /// </summary>
        private readonly object m_SystemsLock = new();

        /// <summary>
        ///   <para>早期更新系统。</para>
        /// </summary>
        private readonly List<IEarlyTick> m_Early = new(k_DefaultCapacity);
        /// <summary>
        ///   <para>早期更新快照。</para>
        /// </summary>
        private IEarlyTick[] m_EarlyBuffer;

        /// <summary>
        ///   <para>物理更新系统。</para>
        /// </summary>
        private readonly List<IPhysicsTick> m_Physics = new(k_DefaultCapacity);
        /// <summary>
        ///   <para>游戏逻辑更新系统。</para>
        /// </summary>
        private readonly List<IGameplayTick> m_Gameplay = new(k_DefaultCapacity);
        /// <summary>
        ///   <para>后期更新系统。</para>
        /// </summary>
        private readonly List<ILateTick> m_Late = new(k_DefaultCapacity);

        /// <summary>
        ///   <para>物理更新快照。</para>
        /// </summary>
        private IPhysicsTick[] m_PhysicsBuffer;
        /// <summary>
        ///   <para>游戏逻辑更新快照。</para>
        /// </summary>
        private IGameplayTick[] m_GameplayBuffer;
        /// <summary>
        ///   <para>后期更新快照。</para>
        /// </summary>
        private ILateTick[] m_LateBuffer;

        /// <summary>
        ///   <para>早期 Tick 调用器。</para>
        /// </summary>
        private static readonly Action<IEarlyTick, float> s_EarlyTickInvoker = static (system, deltaTime) => system.EarlyTick(deltaTime);
        /// <summary>
        ///   <para>物理 Tick 调用器。</para>
        /// </summary>
        private static readonly Action<IPhysicsTick, float> s_PhysicsTickInvoker = static (system, deltaTime) => system.PhysicsTick(deltaTime);
        /// <summary>
        ///   <para>游戏逻辑 Tick 调用器。</para>
        /// </summary>
        private static readonly Action<IGameplayTick, float> s_GameplayTickInvoker = static (system, deltaTime) => system.GameplayTick(deltaTime);
        /// <summary>
        ///   <para>后期 Tick 调用器。</para>
        /// </summary>
        private static readonly Action<ILateTick, float> s_LateTickInvoker = static (system, deltaTime) => system.LateTick(deltaTime);

        /// <summary>
        ///   <para>释放状态。</para>
        /// </summary>
        private int m_DisposeState;
        /// <summary>
        ///   <para>Tick 互斥门。</para>
        /// </summary>
        private int m_TickGate;

        /// <summary>
        ///   <para>获取 Tick 顺序。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetTickOrder(object system) => system is ITickOrder ordered ? ordered.TickOrder : 0;

        /// <summary>
        ///   <para>按 Tick 顺序查找插入位置。</para>
        /// </summary>
        /// <param name="list">列表。</param>
        /// <param name="order">顺序。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        private static int FindInsertIndexByTickOrder<T>(List<T> list, int order)
        {
            int lo = 0;
            int hi = list.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                int midOrder = GetTickOrder(list[mid]);
                if (midOrder <= order) lo = mid + 1;
                else hi = mid;
            }

            return lo;
        }

        /// <summary>
        ///   <para>添加 Tick。</para>
        /// </summary>
        /// <param name="list">列表。</param>
        /// <param name="system">系统。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        private static bool AddTick<T>(List<T> list, T system)
            where T : class
        {
            if (FindIndexByReference(list, system) >= 0)
            {
                return false;
            }

            list.Insert(FindInsertIndexByTickOrder(list, GetTickOrder(system)), system);
            return true;
        }

        /// <summary>
        ///   <para>移除 Tick。</para>
        /// </summary>
        /// <param name="list">列表。</param>
        /// <param name="system">系统。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        private static bool RemoveTick<T>(List<T> list, T system)
            where T : class
        {
            int index = FindIndexByReference(list, system);
            if (index < 0)
            {
                return false;
            }

            list.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///   <para>按引用查找索引。</para>
        /// </summary>
        /// <param name="list">列表。</param>
        /// <param name="target">目标。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindIndexByReference<T>(List<T> list, T target)
            where T : class
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], target)) return i;
            }

            return -1;
        }

        /// <summary>
        ///   <para>复制调度快照。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="buffer">缓冲区。</param>
        /// <param name="count">数量。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        private static void CopyBuffer<T>(List<T> source, ref T[] buffer, out int count)
        {
            count = source?.Count ?? 0;
            if (count == 0) return;

            if (buffer == null || buffer.Length < count)
            {
                int nextSize = buffer == null ? k_DefaultCapacity : buffer.Length * 2;
                if (nextSize < count) nextSize = count;
                buffer = new T[nextSize];
            }

            source.CopyTo(0, buffer, 0, count);
        }

        /// <summary>
        ///   <para>准备 Tick 缓冲区。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="buffer">缓冲区。</param>
        /// <param name="frame">帧号。</param>
        /// <param name="count">数量。</param>
        /// <typeparam name="TTick">目标类型。</typeparam>
        private void PrepareTickBuffer<TTick>(
            List<TTick> source,
            ref TTick[] buffer,
            out TTick[] frame,
            out int count)
        {
            lock (m_SystemsLock)
            {
                CopyBuffer(source, ref buffer, out count);
                frame = buffer;
            }
        }

        /// <summary>
        ///   <para>标记 Tick 快照失效。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateTickBuffersNoLock()
        {
            m_EarlyBuffer = null;
            m_PhysicsBuffer = null;
            m_GameplayBuffer = null;
            m_LateBuffer = null;
        }

        /// <summary>
        ///   <para>执行 Tick 阶段。</para>
        /// </summary>
        /// <param name="deltaTime">本次更新的时间间隔（秒）。</param>
        /// <param name="systems">系统。</param>
        /// <param name="buffer">缓冲区。</param>
        /// <param name="tickInvoker">Tick 调用器。</param>
        /// <typeparam name="TTick">目标类型。</typeparam>
        private void ExecuteTickPhase<TTick>(
            float deltaTime,
            List<TTick> systems,
            ref TTick[] buffer,
            Action<TTick, float> tickInvoker)
            where TTick : class
        {
            if (tickInvoker == null) throw new ArgumentNullException(nameof(tickInvoker));

            PrepareTickBuffer(systems, ref buffer, out var frame, out var count);
            for (int i = 0; i < count; i++)
            {
                using var scope = TickExecutionScope.Enter();
                tickInvoker(frame[i], deltaTime);
            }
        }

        /// <inheritdoc />
        public void AddSystem(object system)
        {
            if (system == null) return;
            if (Volatile.Read(ref m_DisposeState) != (int)DisposeState.Alive) throw new ObjectDisposedException(nameof(GameLoopTickSystemScheduler));
            if (system is IGameLoopTickSystemScheduler) throw new InvalidOperationException($"{nameof(IGameLoopTickSystemScheduler)} cannot be added to scheduler.");
            if (!GameModuleUtility.HasTickInterfaces(system))
            {
                throw new InvalidOperationException(
                    $"Tick system must implement {GameModuleUtility.GetTickInterfaceNames()}. type={GameModuleUtility.GetTypeDisplayName(system.GetType())}");
            }

            lock (m_SystemsLock)
            {
                bool changed = false;
                if (system is IEarlyTick early) changed |= AddTick(m_Early, early);
                if (system is IPhysicsTick physics) changed |= AddTick(m_Physics, physics);
                if (system is IGameplayTick gameplay) changed |= AddTick(m_Gameplay, gameplay);
                if (system is ILateTick late) changed |= AddTick(m_Late, late);
                if (changed)
                {
                    InvalidateTickBuffersNoLock();
                }
            }
        }

        /// <inheritdoc />
        public bool RemoveSystem(object system)
        {
            if (system == null) return false;
            if (Volatile.Read(ref m_DisposeState) != (int)DisposeState.Alive) return false;
            if (system is Type || system is RuntimeTypeHandle)
            {
                throw new InvalidOperationException("RemoveSystem only accepts a previously registered system instance.");
            }

            lock (m_SystemsLock)
            {
                bool removed = false;
                if (system is IEarlyTick early) removed |= RemoveTick(m_Early, early);
                if (system is IPhysicsTick physics) removed |= RemoveTick(m_Physics, physics);
                if (system is IGameplayTick gameplay) removed |= RemoveTick(m_Gameplay, gameplay);
                if (system is ILateTick late) removed |= RemoveTick(m_Late, late);
                if (removed)
                {
                    InvalidateTickBuffersNoLock();
                }

                return removed;
            }
        }

        /// <inheritdoc />
        public void Tick(float deltaTime, TickGroup group)
        {
            if (Volatile.Read(ref m_DisposeState) != (int)DisposeState.Alive) return;
            if (Interlocked.Exchange(ref m_TickGate, 1) != 0) throw new InvalidOperationException("Re-entrant tick is not supported.");

            try
            {
                switch (group)
                {
                    case TickGroup.Early:
                        ExecuteTickPhase(deltaTime, m_Early, ref m_EarlyBuffer, s_EarlyTickInvoker);
                        break;
                    case TickGroup.Physics:
                        ExecuteTickPhase(deltaTime, m_Physics, ref m_PhysicsBuffer, s_PhysicsTickInvoker);
                        break;
                    case TickGroup.Gameplay:
                        ExecuteTickPhase(deltaTime, m_Gameplay, ref m_GameplayBuffer, s_GameplayTickInvoker);
                        break;
                    case TickGroup.Late:
                        ExecuteTickPhase(deltaTime, m_Late, ref m_LateBuffer, s_LateTickInvoker);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(group), group, null);
                }
            }
            finally
            {
                Volatile.Write(ref m_TickGate, 0);
            }
        }

        /// <inheritdoc />
        public void WaitForIdle()
        {
            if (TickExecutionScope.IsActive)
            {
                throw new InvalidOperationException("Cannot wait for the GameLoop tick scheduler from within a Tick callback.");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var observedState = Interlocked.CompareExchange(
                ref m_DisposeState,
                (int)DisposeState.Disposing,
                (int)DisposeState.Alive);
            if (observedState == (int)DisposeState.Disposed ||
                observedState == (int)DisposeState.Disposing)
            {
                return;
            }

            try
            {
                WaitForIdle();

                lock (m_SystemsLock)
                {
                    m_Early.Clear();
                    m_Physics.Clear();
                    m_Gameplay.Clear();
                    m_Late.Clear();
                    InvalidateTickBuffersNoLock();
                }

                Volatile.Write(ref m_TickGate, 0);
                Volatile.Write(ref m_DisposeState, (int)DisposeState.Disposed);
            }
            catch
            {
                Volatile.Write(ref m_DisposeState, (int)DisposeState.Alive);
                throw;
            }
        }
    }
}