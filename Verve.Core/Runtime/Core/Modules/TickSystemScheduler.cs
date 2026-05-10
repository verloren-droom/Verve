namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;


    /// <summary>
    ///   <para>当前线程是否正处于 Tick 回调执行中</para>
    /// </summary>
    internal static class TickExecutionScope
    {
        [ThreadStatic] private static int s_Depth;

        public static bool IsActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_Depth != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scope Enter()
        {
            s_Depth++;
            return default;
        }

        public readonly struct Scope : IDisposable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                s_Depth--;
            }
        }
    }

    /// <summary>
    ///   <para>更新系统调度器接口</para>
    /// </summary>
    public interface ITickSystemScheduler : IDisposable
    {
        /// <summary>
        ///   <para>添加更新系统</para>
        /// </summary>
        /// <param name="system">系统实例</param>
        /// <param name="runInBackground">是否后台运行</param>
        void AddSystem(object system, bool runInBackground);
        /// <summary>
        ///   <para>移除更新系统</para>
        /// </summary>
        /// <param name="system">系统实例</param>
        bool RemoveSystem(object system);
        /// <summary>
        ///   <para>执行更新</para>
        /// </summary>
        /// <param name="deltaTime">间隔时间</param>
        /// <param name="group">更新组</param>
        void Tick(float deltaTime, TickGroup group);
        /// <summary>
        ///   <para>等待调度器进入空闲状态</para>
        /// </summary>
        void WaitForIdle();
    }

    /// <summary>
    ///   <para>内置更新系统调度器</para>
    /// </summary>
    internal sealed class TickSystemScheduler : ITickSystemScheduler
    {
        /// <summary>
        ///   <para>各阶段列表和缓存数组的默认初始容量</para>
        /// </summary>
        private const int k_DefaultCapacity = 16;

        /// <summary>
        ///   <para>后台 Tick 批次等待超时的默认毫秒数</para>
        /// </summary>
        private const int k_DefaultBackgroundWaitMilliseconds = 5000;

        private enum DisposeState : byte
        {
            Alive = 0,
            Disposing = 1,
            Disposed = 2,
        }

        /// <summary>
        ///   <para>保护所有 Tick 列表增删操作的同步锁</para>
        /// </summary>
        private readonly object m_SystemsLock = new();

        /// <summary>
        ///   <para>保护后台批次状态的同步锁</para>
        /// </summary>
        private readonly object m_BackgroundBatchLock = new();

#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
        /// <summary>
        ///   <para>早期更新阶段主线程执行列表、后台执行列表以及后台工作项列表</para>
        /// </summary>
        private readonly List<IEarlyTick> m_EarlyMain = new(k_DefaultCapacity);
        private readonly List<IEarlyTick> m_EarlyBg = new(k_DefaultCapacity);
        private readonly List<IWorkItem> m_EarlyBgWork = new(k_DefaultCapacity);

        /// <summary>
        ///   <para>早期更新阶段 Tick 前复制出的只读缓冲区</para>
        /// </summary>
        private IEarlyTick[] m_EarlyMainBuffer;
        private IWorkItem[] m_EarlyBgWorkBuffer;
#endif

        /// <summary>
        ///   <para>物理更新阶段主线程执行列表、后台执行列表以及后台工作项列表</para>
        /// </summary>
        private readonly List<IPhysicsTick> m_PhysicsMain = new(k_DefaultCapacity);
        private readonly List<IPhysicsTick> m_PhysicsBg = new(k_DefaultCapacity);
        private readonly List<IWorkItem> m_PhysicsBgWork = new(k_DefaultCapacity);

        /// <summary>
        ///   <para>物理更新阶段 Tick 前复制出的只读缓冲区</para>
        /// </summary>
        private IPhysicsTick[] m_PhysicsMainBuffer;
        private IWorkItem[] m_PhysicsBgWorkBuffer;

        /// <summary>
        ///   <para>游戏逻辑更新阶段主线程执行列表、后台执行列表以及后台工作项列表</para>
        /// </summary>
        private readonly List<IGameplayTick> m_GameplayMain = new(k_DefaultCapacity);
        private readonly List<IGameplayTick> m_GameplayBg = new(k_DefaultCapacity);
        private readonly List<IWorkItem> m_GameplayBgWork = new(k_DefaultCapacity);

        /// <summary>
        ///   <para>游戏逻辑更新阶段 Tick 前复制出的只读缓冲区</para>
        /// </summary>
        private IGameplayTick[] m_GameplayMainBuffer;
        private IWorkItem[] m_GameplayBgWorkBuffer;

        /// <summary>
        ///   <para>延迟更新阶段主线程执行列表、后台执行列表以及后台工作项列表</para>
        /// </summary>
        private readonly List<ILateTick> m_LateMain = new(k_DefaultCapacity);
        private readonly List<ILateTick> m_LateBg = new(k_DefaultCapacity);
        private readonly List<IWorkItem> m_LateBgWork = new(k_DefaultCapacity);

        /// <summary>
        ///   <para>延迟更新阶段 Tick 前复制出的只读缓冲区</para>
        /// </summary>
        private ILateTick[] m_LateMainBuffer;
        private IWorkItem[] m_LateBgWorkBuffer;

        /// <summary>
        ///   <para>后台等待时间（毫秒）</para>
        /// </summary>
        private readonly int m_BackgroundWaitMilliseconds;

        /// <summary>
        ///   <para>线程池后台工作入口</para>
        /// </summary>
        private static readonly WaitCallback s_BatchWorkerCallback = static state => ((BackgroundTickBatch)state).ExecuteQueuedWork();

#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
        /// <summary>
        ///   <para>早期更新阶段主线程调用入口</para>
        /// </summary>
        private static readonly Action<IEarlyTick, float> s_EarlyTickInvoker = static (system, deltaTime) => system.EarlyTick(deltaTime);
#endif

        /// <summary>
        ///   <para>物理更新阶段主线程调用入口</para>
        /// </summary>
        private static readonly Action<IPhysicsTick, float> s_PhysicsTickInvoker = static (system, deltaTime) => system.PhysicsTick(deltaTime);

        /// <summary>
        ///   <para>逻辑更新阶段主线程调用入口</para>
        /// </summary>
        private static readonly Action<IGameplayTick, float> s_GameplayTickInvoker = static (system, deltaTime) => system.GameplayTick(deltaTime);

        /// <summary>
        ///   <para>延迟更新阶段主线程调用入口</para>
        /// </summary>
        private static readonly Action<ILateTick, float> s_LateTickInvoker = static (system, deltaTime) => system.LateTick(deltaTime);

        /// <summary>
        ///   <para>当前仍未完全结束的后台批次</para>
        /// </summary>
        private BackgroundTickBatch m_ActiveBackgroundBatch;

        /// <summary>
        ///   <para>调度器释放状态</para>
        /// </summary>
        private int m_DisposeState;

        /// <summary>
        ///   <para>防止同一调度器发生重入 Tick 的门闩</para>
        /// </summary>
        private int m_TickGate;

        /// <summary>
        ///   <para>后台 Tick 工作项接口</para>
        /// </summary>
        private interface IWorkItem
        {
            void Execute(float deltaTime);
        }

        /// <summary>
        ///   <para>一次后台 Tick 批次</para>
        /// </summary>
        private sealed class BackgroundTickBatch : IDisposable
        {
            private readonly IWorkItem[] m_WorkItems;
            private readonly int m_WorkItemCount;
            private readonly float m_DeltaTime;
            private readonly ManualResetEventSlim m_CompletedSignal = new(false);
            private readonly object m_ExceptionLock = new();
            private List<Exception> m_Exceptions;
            private int m_NextIndex;
            private int m_RemainingItems;

            public BackgroundTickBatch(IWorkItem[] workItems, int workItemCount, float deltaTime)
            {
                m_WorkItems = workItems ?? throw new ArgumentNullException(nameof(workItems));
                m_WorkItemCount = workItemCount;
                m_DeltaTime = deltaTime;
                m_RemainingItems = workItemCount;
            }

            public bool IsCompleted
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Volatile.Read(ref m_RemainingItems) == 0;
            }

            public void Schedule()
            {
                if (m_WorkItemCount <= 0)
                {
                    m_CompletedSignal.Set();
                    return;
                }

                int workerCount = Math.Min(m_WorkItemCount, Math.Max(Environment.ProcessorCount, 1));
                for (int i = 0; i < workerCount; i++)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(s_BatchWorkerCallback, this);
                }
            }

            public bool Wait(int millisecondsTimeout)
            {
                return IsCompleted || m_CompletedSignal.Wait(millisecondsTimeout);
            }

            public void ExecuteQueuedWork()
            {
                while (true)
                {
                    int index = Interlocked.Increment(ref m_NextIndex) - 1;
                    if (index >= m_WorkItemCount)
                    {
                        return;
                    }

                    try
                    {
                        using var scope = TickExecutionScope.Enter();
                        m_WorkItems[index]?.Execute(m_DeltaTime);
                    }
                    catch (Exception ex)
                    {
                        AddException(ex);
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref m_RemainingItems) == 0)
                        {
                            m_CompletedSignal.Set();
                        }
                    }
                }
            }

            public List<Exception> GetExceptionsCopy()
            {
                lock (m_ExceptionLock)
                {
                    if (m_Exceptions == null || m_Exceptions.Count == 0)
                    {
                        return null;
                    }

                    return new List<Exception>(m_Exceptions);
                }
            }

            public void Dispose()
            {
                m_CompletedSignal.Dispose();
            }

            private void AddException(Exception exception)
            {
                if (exception == null) return;

                lock (m_ExceptionLock)
                {
                    m_Exceptions ??= new List<Exception>(2);
                    m_Exceptions.Add(exception);
                }
            }
        }

        /// <summary>
        ///   <para>调度器构造函数</para>
        /// </summary>
        /// <param name="bgWaitMilliseconds">后台等待时间（毫秒）</param>
        public TickSystemScheduler(int bgWaitMilliseconds = k_DefaultBackgroundWaitMilliseconds)
        {
            m_BackgroundWaitMilliseconds = Math.Max(bgWaitMilliseconds, 0);
        }

        /// <summary>
        ///   <para>按 <see cref="ITickOrder"/> 约定获取系统顺序值</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetTickOrder(object system)
        {
            return system is ITickOrder ordered ? ordered.TickOrder : 0;
        }

        /// <summary>
        ///   <para>在已按 TickOrder 排序的列表中查找插入位置</para>
        /// </summary>
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
        ///   <para>把 Tick 对象按顺序插入主线程列表</para>
        /// </summary>
        private static void InsertOrdered<T>(List<T> list, T item)
        {
            list.Insert(FindInsertIndexByTickOrder(list, GetTickOrder(item)), item);
        }

        /// <summary>
        ///   <para>把 Tick 对象和其后台工作项按顺序插入后台列表</para>
        /// </summary>
        private static void InsertOrdered<T>(List<T> list, List<IWorkItem> workItems, T item, IWorkItem workItem)
        {
            int index = FindInsertIndexByTickOrder(list, GetTickOrder(item));
            list.Insert(index, item);
            workItems.Insert(index, workItem);
        }

        /// <summary>
        ///   <para>按引用查找对象在列表中的位置</para>
        /// </summary>
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
        ///   <para>把当前执行列表复制到缓冲区</para>
        /// </summary>
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
        ///   <para>向指定 Tick 分组中添加或迁移一个系统</para>
        /// </summary>
        private static bool AddTick<T>(List<T> main, List<T> bg, List<IWorkItem> bgWork, T system, bool runInBackground, IWorkItem backgroundWorkItem)
            where T : class
        {
            int mainIndex = FindIndexByReference(main, system);
            if (mainIndex >= 0)
            {
                if (!runInBackground) return false;
                main.RemoveAt(mainIndex);
            }

            int bgIndex = FindIndexByReference(bg, system);
            if (bgIndex >= 0)
            {
                if (runInBackground) return false;
                bg.RemoveAt(bgIndex);
                bgWork.RemoveAt(bgIndex);
            }

            if (runInBackground)
            {
                if (backgroundWorkItem == null) throw new ArgumentNullException(nameof(backgroundWorkItem));
                InsertOrdered(bg, bgWork, system, backgroundWorkItem);
                return true;
            }

            InsertOrdered(main, system);
            return true;
        }

        /// <summary>
        ///   <para>从指定 Tick 分组中按引用移除一个系统</para>
        /// </summary>
        private static bool RemoveTick<T>(List<T> main, List<T> bg, List<IWorkItem> bgWork, T system)
            where T : class
        {
            int index = FindIndexByReference(main, system);
            if (index >= 0)
            {
                main.RemoveAt(index);
                return true;
            }

            index = FindIndexByReference(bg, system);
            if (index < 0) return false;

            bg.RemoveAt(index);
            bgWork.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///   <para>复制某个 Tick 分组的主线程列表和后台工作项列表</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PrepareTickBuffers<TTick>(
            List<TTick> main,
            ref TTick[] mainBuffer,
            List<IWorkItem> backgroundWork,
            ref IWorkItem[] backgroundBuffer,
            out TTick[] mainFrame,
            out int mainCount,
            out IWorkItem[] backgroundFrame,
            out int backgroundCount)
        {
            lock (m_SystemsLock)
            {
                CopyBuffer(main, ref mainBuffer, out mainCount);
                CopyBuffer(backgroundWork, ref backgroundBuffer, out backgroundCount);
                mainFrame = mainBuffer;
                backgroundFrame = backgroundBuffer;
            }
        }

        /// <summary>
        ///   <para>使所有持久缓冲区失效</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateTickBuffersNoLock()
        {
#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
            m_EarlyMainBuffer = null;
            m_EarlyBgWorkBuffer = null;
#endif
            m_PhysicsMainBuffer = null;
            m_PhysicsBgWorkBuffer = null;
            m_GameplayMainBuffer = null;
            m_GameplayBgWorkBuffer = null;
            m_LateMainBuffer = null;
            m_LateBgWorkBuffer = null;
        }

        /// <summary>
        ///   <para>提取并回收已经完成的后台批次</para>
        /// </summary>
        private BackgroundTickBatch TakeCompletedBackgroundBatch()
        {
            lock (m_BackgroundBatchLock)
            {
                var active = m_ActiveBackgroundBatch;
                if (active == null || !active.IsCompleted)
                {
                    return null;
                }

                m_ActiveBackgroundBatch = null;
                return active;
            }
        }

        /// <summary>
        ///   <para>读取当前未完成的后台批次</para>
        /// </summary>
        private BackgroundTickBatch GetActiveBackgroundBatch()
        {
            lock (m_BackgroundBatchLock)
            {
                return m_ActiveBackgroundBatch;
            }
        }

        /// <summary>
        ///   <para>处理已完成的后台批次，并在需要时把延迟异常提升到主线程</para>
        /// </summary>
        private static void FinalizeCompletedBackgroundBatch(BackgroundTickBatch batch, bool delayed)
        {
            if (batch == null) return;

            try
            {
                var exceptions = batch.GetExceptionsCopy();
                if (exceptions == null || exceptions.Count == 0)
                {
                    return;
                }

                if (exceptions.Count == 1)
                {
                    if (delayed)
                    {
                        throw new InvalidOperationException("Previous background tick completed after timing out and reported a delayed exception.", exceptions[0]);
                    }

                    throw exceptions[0];
                }

                if (delayed)
                {
                    throw new AggregateException("Previous background tick completed after timing out and reported delayed exceptions.", exceptions);
                }

                throw new AggregateException(exceptions);
            }
            finally
            {
                batch.Dispose();
            }
        }

        /// <summary>
        ///   <para>在开始新一帧后台调度前，确保没有旧批次仍在运行</para>
        /// </summary>
        private void EnsureNoRunningBackgroundBatch()
        {
            FinalizeCompletedBackgroundBatch(TakeCompletedBackgroundBatch(), delayed: true);

            var active = GetActiveBackgroundBatch();
            if (active == null)
            {
                return;
            }

            if (!active.IsCompleted)
            {
                throw new TimeoutException("Previous background tick is still running.");
            }

            FinalizeCompletedBackgroundBatch(TakeCompletedBackgroundBatch(), delayed: true);
        }

        /// <summary>
        ///   <para>调度后台 Tick 工作项</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BackgroundTickBatch ScheduleBackground(IWorkItem[] workItems, int count, float deltaTime)
        {
            if (count == 0) return null;

            var batch = new BackgroundTickBatch(workItems, count, deltaTime);
            lock (m_BackgroundBatchLock)
            {
                if (m_ActiveBackgroundBatch != null)
                {
                    batch.Dispose();
                    throw new InvalidOperationException("Background tick batch state is invalid.");
                }

                m_ActiveBackgroundBatch = batch;
            }

            batch.Schedule();
            return batch;
        }

        /// <summary>
        ///   <para>等待当前后台 Tick 批次结束</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitForBackground(BackgroundTickBatch batch)
        {
            if (batch == null) return;

            if (!batch.Wait(m_BackgroundWaitMilliseconds))
            {
                throw new TimeoutException($"Background tick timed out after {m_BackgroundWaitMilliseconds}ms.");
            }

            lock (m_BackgroundBatchLock)
            {
                if (ReferenceEquals(m_ActiveBackgroundBatch, batch))
                {
                    m_ActiveBackgroundBatch = null;
                }
            }

            FinalizeCompletedBackgroundBatch(batch, delayed: false);
        }

        /// <summary>
        ///   <para>执行一个 Tick 阶段的完整流程：生成执行副本、调度后台、执行主线程、等待后台</para>
        /// </summary>
        private void ExecuteTickPhase<TTick>(
            float deltaTime,
            List<TTick> main,
            ref TTick[] mainBuffer,
            List<IWorkItem> backgroundWork,
            ref IWorkItem[] backgroundBuffer,
            Action<TTick, float> tickInvoker)
            where TTick : class
        {
            if (tickInvoker == null) throw new ArgumentNullException(nameof(tickInvoker));

            PrepareTickBuffers(
                main,
                ref mainBuffer,
                backgroundWork,
                ref backgroundBuffer,
                out var mainFrame,
                out var mainCount,
                out var backgroundFrame,
                out var backgroundCount);

            BackgroundTickBatch backgroundBatch = null;
            Exception failure = null;

            try
            {
                backgroundBatch = ScheduleBackground(backgroundFrame, backgroundCount, deltaTime);
                for (int i = 0; i < mainCount; i++)
                {
                    using var scope = TickExecutionScope.Enter();
                    tickInvoker(mainFrame[i], deltaTime);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            try
            {
                WaitForBackground(backgroundBatch);
            }
            catch (Exception ex)
            {
                failure = GameModuleUtility.CombineErrors(failure, ex);
            }

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        ///   <para>向调度器注册一个 Tick 系统</para>
        /// </summary>
        /// <param name="system">实现任一 Tick 接口的对象</param>
        /// <param name="runInBackground">是否让对象在后台线程执行</param>
        public void AddSystem(object system, bool runInBackground)
        {
            if (system == null) return;
            if (Volatile.Read(ref m_DisposeState) != (int)DisposeState.Alive) throw new ObjectDisposedException(nameof(TickSystemScheduler));
            if (system is ITickSystemScheduler) throw new InvalidOperationException($"{nameof(ITickSystemScheduler)} cannot be added to scheduler.");
            if (!GameModuleUtility.HasTickInterfaces(system))
            {
                throw new InvalidOperationException(
                    $"Tick system must implement {GameModuleUtility.GetTickInterfaceNames()}. type={GameModuleUtility.GetTypeDisplayName(system.GetType())}");
            }
#if UNITY_5_3_OR_NEWER
            if (runInBackground && system is UnityEngine.Object) throw new InvalidOperationException($"{nameof(UnityEngine.Object)} cannot run in background.");
#endif

            lock (m_SystemsLock)
            {
                bool changed = false;
#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
                if (system is IEarlyTick early)
                {
                    changed |= AddTick(m_EarlyMain, m_EarlyBg, m_EarlyBgWork, early, runInBackground,
                        runInBackground ? new EarlyTickWorkItem(early) : null);
                }
#endif
                if (system is IPhysicsTick physics)
                {
                    changed |= AddTick(m_PhysicsMain, m_PhysicsBg, m_PhysicsBgWork, physics, runInBackground,
                        runInBackground ? new PhysicsTickWorkItem(physics) : null);
                }

                if (system is IGameplayTick gameplay)
                {
                    changed |= AddTick(m_GameplayMain, m_GameplayBg, m_GameplayBgWork, gameplay, runInBackground,
                        runInBackground ? new GameplayTickWorkItem(gameplay) : null);
                }

                if (system is ILateTick late)
                {
                    changed |= AddTick(m_LateMain, m_LateBg, m_LateBgWork, late, runInBackground,
                        runInBackground ? new LateTickWorkItem(late) : null);
                }

                if (changed)
                {
                    InvalidateTickBuffersNoLock();
                }
            }
        }

        /// <summary>
        ///   <para>从调度器移除一个 Tick 系统</para>
        /// </summary>
        /// <param name="system">要移除的系统实例</param>
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
#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
                if (system is IEarlyTick early) removed |= RemoveTick(m_EarlyMain, m_EarlyBg, m_EarlyBgWork, early);
#endif
                if (system is IPhysicsTick physics) removed |= RemoveTick(m_PhysicsMain, m_PhysicsBg, m_PhysicsBgWork, physics);
                if (system is IGameplayTick gameplay) removed |= RemoveTick(m_GameplayMain, m_GameplayBg, m_GameplayBgWork, gameplay);
                if (system is ILateTick late) removed |= RemoveTick(m_LateMain, m_LateBg, m_LateBgWork, late);
                if (removed)
                {
                    InvalidateTickBuffersNoLock();
                }
                return removed;
            }
        }

        /// <summary>
        ///   <para>执行指定阶段的 Tick</para>
        /// </summary>
        /// <param name="deltaTime">当前阶段的时间步长</param>
        /// <param name="group">要执行的 Tick 分组</param>
        public void Tick(float deltaTime, TickGroup group)
        {
            if (Volatile.Read(ref m_DisposeState) != (int)DisposeState.Alive) return;
            if (Interlocked.Exchange(ref m_TickGate, 1) != 0) throw new InvalidOperationException("Re-entrant tick is not supported.");

            try
            {
                EnsureNoRunningBackgroundBatch();

                switch (group)
                {
#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
                    case TickGroup.Early:
                        ExecuteTickPhase(
                            deltaTime,
                            m_EarlyMain,
                            ref m_EarlyMainBuffer,
                            m_EarlyBgWork,
                            ref m_EarlyBgWorkBuffer,
                            s_EarlyTickInvoker);
                        break;
#endif
                    case TickGroup.Physics:
                        ExecuteTickPhase(
                            deltaTime,
                            m_PhysicsMain,
                            ref m_PhysicsMainBuffer,
                            m_PhysicsBgWork,
                            ref m_PhysicsBgWorkBuffer,
                            s_PhysicsTickInvoker);
                        break;

                    case TickGroup.Gameplay:
                        ExecuteTickPhase(
                            deltaTime,
                            m_GameplayMain,
                            ref m_GameplayMainBuffer,
                            m_GameplayBgWork,
                            ref m_GameplayBgWorkBuffer,
                            s_GameplayTickInvoker);
                        break;

                    case TickGroup.Late:
                        ExecuteTickPhase(
                            deltaTime,
                            m_LateMain,
                            ref m_LateMainBuffer,
                            m_LateBgWork,
                            ref m_LateBgWorkBuffer,
                            s_LateTickInvoker);
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

        public void WaitForIdle()
        {
            while (true)
            {
                var completed = TakeCompletedBackgroundBatch();
                if (completed != null)
                {
                    FinalizeCompletedBackgroundBatch(completed, delayed: true);
                    continue;
                }

                var active = GetActiveBackgroundBatch();
                if (active == null)
                {
                    return;
                }

                if (!active.Wait(m_BackgroundWaitMilliseconds))
                {
                    throw new TimeoutException($"Background tick is still running after {m_BackgroundWaitMilliseconds}ms.");
                }
            }
        }

        /// <summary>
        ///   <para>释放调度器</para>
        /// </summary>
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
#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
                    m_EarlyMain.Clear();
                    m_EarlyBg.Clear();
                    m_EarlyBgWork.Clear();
#endif
                    m_PhysicsMain.Clear();
                    m_PhysicsBg.Clear();
                    m_PhysicsBgWork.Clear();

                    m_GameplayMain.Clear();
                    m_GameplayBg.Clear();
                    m_GameplayBgWork.Clear();

                    m_LateMain.Clear();
                    m_LateBg.Clear();
                    m_LateBgWork.Clear();

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

#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
        /// <summary>
        ///   <para>早期更新阶段后台工作项</para>
        /// </summary>
        private sealed class EarlyTickWorkItem : IWorkItem
        {
            private readonly IEarlyTick m_System;

            public EarlyTickWorkItem(IEarlyTick system)
            {
                m_System = system;
            }

            public void Execute(float deltaTime)
            {
                m_System.EarlyTick(deltaTime);
            }
        }
#endif

        /// <summary>
        ///   <para>物理逻辑更新阶段后台工作项</para>
        /// </summary>
        private sealed class PhysicsTickWorkItem : IWorkItem
        {
            private readonly IPhysicsTick m_System;

            public PhysicsTickWorkItem(IPhysicsTick system)
            {
                m_System = system;
            }

            public void Execute(float deltaTime)
            {
                m_System.PhysicsTick(deltaTime);
            }
        }

        /// <summary>
        ///   <para>游戏逻辑更新阶段后台工作项</para>
        /// </summary>
        private sealed class GameplayTickWorkItem : IWorkItem
        {
            private readonly IGameplayTick m_System;

            public GameplayTickWorkItem(IGameplayTick system)
            {
                m_System = system;
            }

            public void Execute(float deltaTime)
            {
                m_System.GameplayTick(deltaTime);
            }
        }

        /// <summary>
        ///   <para>延迟更新阶段后台工作项</para>
        /// </summary>
        private sealed class LateTickWorkItem : IWorkItem
        {
            private readonly ILateTick m_System;

            public LateTickWorkItem(ILateTick system)
            {
                m_System = system;
            }

            public void Execute(float deltaTime)
            {
                m_System.LateTick(deltaTime);
            }
        }
    }
}