namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;

    
    /// <summary>
    /// 异步任务状态
    /// </summary>
    public enum VTaskStatus : byte
    {
        Pending = 0,
        Succeeded = 1,
        Faulted = 2,
        Canceled = 3
    }

    /// <summary>
    /// 轻量级异步任务
    /// </summary>
    [AsyncMethodBuilder(typeof(VTaskMethodBuilder))]
    public readonly struct VTask : IEquatable<VTask>
    {
        internal readonly IVTaskSource m_Source;
        internal readonly short m_Token;
        private readonly bool m_Completed;

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTask(IVTaskSource source, short token)
        {
            m_Source = source;
            m_Token = token;
            m_Completed = false;
        }

        private VTask(bool completed)
        {
            m_Source = null;
            m_Token = 0;
            m_Completed = completed;
        }

        /// <summary>已完成任务</summary>
        public static VTask CompletedTask => new(true);

        /// <summary>任务状态</summary>
        public VTaskStatus Status
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_Completed) return VTaskStatus.Succeeded;
                if (m_Source == null) return VTaskStatus.Pending;
                return m_Source.GetStatus(m_Token);
            }
        }

        /// <summary>任务是否完成</summary>
        public bool IsCompleted
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Completed || (m_Source?.GetStatus(m_Token) > VTaskStatus.Pending);
        }

        /// <summary>任务是否成功完成</summary>
        public bool IsCompletedSuccessfully
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Status == VTaskStatus.Succeeded;
        }

        /// <summary>任务是否被取消</summary>
        public bool IsCanceled
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Status == VTaskStatus.Canceled;
        }

        /// <summary>任务是否出现异常</summary>
        public bool IsFaulted
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Status == VTaskStatus.Faulted;
        }

        /// <summary>获取任务结果（阻塞）</summary>
        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
            if (m_Completed) return;
            if (m_Source == null) throw new InvalidOperationException("Task not initialized");
            m_Source.GetResult(m_Token);
        }

        /// <summary>获取任务Awaiter</summary>
        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTaskAwaiter GetAwaiter() => new(this);

        /// <summary>配置任务Awaiter</summary>
        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTask ConfigureAwait(bool continueOnCapturedContext) => this;

        public bool Equals(VTask other) => 
            m_Source == other.m_Source && m_Token == other.m_Token && m_Completed == other.m_Completed;

        public override bool Equals(object obj) => obj is VTask task && Equals(task);
        public override int GetHashCode() => HashCode.Combine(m_Source, m_Token, m_Completed);
        public static bool operator ==(VTask left, VTask right) => left.Equals(right);
        public static bool operator !=(VTask left, VTask right) => !left.Equals(right);

        #region 静态方法

        /// <summary>创建延迟任务</summary>
        public static VTask Delay(int millisecondsDelay, CancellationToken cancellationToken = default) 
            => Delay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);

        /// <summary>创建延迟任务</summary>
        public static VTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            if (delay <= TimeSpan.Zero) return CompletedTask;
            if (cancellationToken.IsCancellationRequested) return FromCanceled(cancellationToken);
            
            var source = new DelayVTaskSource();
            source.Start(delay, cancellationToken);
            return new VTask(source, 0);
        }

        /// <summary>创建被取消的任务</summary>
        public static VTask FromCanceled(CancellationToken cancellationToken)
        {
            var source = new AsyncVTaskSource();
            source.SetException(new OperationCanceledException(cancellationToken));
            return new VTask(source, 0);
        }

        /// <summary>创建异常任务</summary>
        public static VTask FromException(Exception exception)
        {
            var source = new AsyncVTaskSource();
            source.SetException(exception);
            return new VTask(source, 0);
        }

        /// <summary>运行异步方法</summary>
        public static VTask Run(Func<VTask> function) => Task.Run(async () => await function()).AsVTask();

        /// <summary>等待所有任务完成</summary>
        public static VTask WhenAll(params VTask[] tasks)
        {
            if (tasks == null || tasks.Length == 0) return CompletedTask;
            var source = new WhenAllVTaskSource(tasks);
            return new VTask(source, 0);
        }

        /// <summary>等待所有任务完成</summary>
        public static VTask WhenAll(IEnumerable<VTask> tasks) => WhenAll(tasks as VTask[] ?? new List<VTask>(tasks).ToArray());

        /// <summary>等待任意一个任务完成</summary>
        public static VTask<int> WhenAny(params VTask[] tasks)
        {
            if (tasks == null || tasks.Length == 0) 
                throw new ArgumentException("Tasks cannot be null or empty");
            var source = new WhenAnyVTaskSource(tasks);
            return new VTask<int>(source, 0);
        }

        /// <summary>等待任意一个任务完成</summary>
        public static VTask<int> WhenAny(IEnumerable<VTask> tasks) => WhenAny(tasks as VTask[] ?? new List<VTask>(tasks).ToArray());

        /// <summary>让出当前线程执行权</summary>
        public static VTask Yield()
        {
            var source = new YieldVTaskSource();
            return new VTask(source, 0);
        }

        #endregion
    }

    /// <summary>
    /// 泛型异步任务
    /// </summary>
    [AsyncMethodBuilder(typeof(VTaskMethodBuilder<>))]
    public readonly struct VTask<TResult> : IEquatable<VTask<TResult>>
    {
        internal readonly IVTaskSource<TResult> m_Source;
        internal readonly short m_Token;
        private readonly TResult m_Result;
        private readonly bool m_Completed;

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTask(IVTaskSource<TResult> source, short token)
        {
            m_Source = source;
            m_Token = token;
            m_Result = default;
            m_Completed = false;
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTask(TResult result)
        {
            m_Source = null;
            m_Token = 0;
            m_Result = result;
            m_Completed = true;
        }

        /// <summary>任务状态</summary>
        public VTaskStatus Status
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_Completed) return VTaskStatus.Succeeded;
                if (m_Source == null) return VTaskStatus.Pending;
                return m_Source.GetStatus(m_Token);
            }
        }

        /// <summary>任务是否完成</summary>
        public bool IsCompleted
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Completed || (m_Source?.GetStatus(m_Token) > VTaskStatus.Pending);
        }

        /// <summary>任务是否成功完成</summary>
        public bool IsCompletedSuccessfully
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Status == VTaskStatus.Succeeded;
        }

        /// <summary>任务是否被取消</summary>
        public bool IsCanceled
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Status == VTaskStatus.Canceled;
        }

        /// <summary>任务是否出现异常</summary>
        public bool IsFaulted
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Status == VTaskStatus.Faulted;
        }

        /// <summary>获取任务结果（阻塞）</summary>
        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult GetResult()
        {
            if (m_Completed) return m_Result;
            if (m_Source == null) throw new InvalidOperationException("Task not initialized");
            return m_Source.GetResult(m_Token);
        }

        /// <summary>获取任务Awaiter</summary>
        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTaskAwaiter<TResult> GetAwaiter() => new(this);

        /// <summary>配置任务Awaiter</summary>
        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTask<TResult> ConfigureAwait(bool continueOnCapturedContext) => this;

        public bool Equals(VTask<TResult> other) => 
            m_Source == other.m_Source && m_Token == other.m_Token && m_Completed == other.m_Completed;

        public override bool Equals(object obj) => obj is VTask<TResult> task && Equals(task);
        public override int GetHashCode() => HashCode.Combine(m_Source, m_Token, m_Completed);
        public static bool operator ==(VTask<TResult> left, VTask<TResult> right) => left.Equals(right);
        public static bool operator !=(VTask<TResult> left, VTask<TResult> right) => !left.Equals(right);

        #region 静态方法

        /// <summary>创建被取消的任务</summary>
        public static VTask<TResult> FromCanceled(CancellationToken cancellationToken)
        {
            var source = new AsyncVTaskSource<TResult>();
            source.SetException(new OperationCanceledException(cancellationToken));
            return new VTask<TResult>(source, 0);
        }

        /// <summary>创建异常任务</summary>
        public static VTask<TResult> FromException(Exception exception)
        {
            var source = new AsyncVTaskSource<TResult>();
            source.SetException(exception);
            return new VTask<TResult>(source, 0);
        }

        /// <summary>创建已完成的任务</summary>
        public static VTask<TResult> FromResult(TResult result) => new(result);

        /// <summary>运行异步方法</summary>
        public static VTask<TResult> Run(Func<VTask<TResult>> function) => Task.Run(async () => await function()).AsVTask();

        /// <summary>等待所有任务完成</summary>
        public static VTask<TResult[]> WhenAll(params VTask<TResult>[] tasks)
        {
            if (tasks == null || tasks.Length == 0) 
                return new VTask<TResult[]>(Array.Empty<TResult>());
            var source = new WhenAllVTaskSource<TResult>(tasks);
            return new VTask<TResult[]>(source, 0);
        }

        /// <summary>等待所有任务完成</summary>
        public static VTask<TResult[]> WhenAll(IEnumerable<VTask<TResult>> tasks) => 
            WhenAll(tasks as VTask<TResult>[] ?? new List<VTask<TResult>>(tasks).ToArray());

        /// <summary>等待任意一个任务完成</summary>
        public static VTask<VTask<TResult>> WhenAny(params VTask<TResult>[] tasks)
        {
            if (tasks == null || tasks.Length == 0) 
                throw new ArgumentException("Tasks cannot be null or empty");
            var source = new WhenAnyVTaskSource<TResult>(tasks);
            return new VTask<VTask<TResult>>(source, 0);
        }

        /// <summary>等待任意一个任务完成</summary>
        public static VTask<VTask<TResult>> WhenAny(IEnumerable<VTask<TResult>> tasks) => 
            WhenAny(tasks as VTask<TResult>[] ?? new List<VTask<TResult>>(tasks).ToArray());

        #endregion
    }

    #region 接口定义

    /// <summary>任务源接口</summary>
    public interface IVTaskSource
    {
        VTaskStatus GetStatus(short token);
        void GetResult(short token);
        void OnCompleted(Action<object> continuation, object state, short token);
    }

    /// <summary>泛型任务源接口</summary>
    public interface IVTaskSource<out TResult>
    {
        VTaskStatus GetStatus(short token);
        TResult GetResult(short token);
        void OnCompleted(Action<object> continuation, object state, short token);
    }

    #endregion

    #region Awaiter实现

    /// <summary>任务Awaiter</summary>
    public readonly struct VTaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly VTask m_Task;

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTaskAwaiter(VTask task) => m_Task = task;

        public bool IsCompleted => m_Task.IsCompleted;

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() => m_Task.GetResult();

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) => 
            m_Task.m_Source?.OnCompleted(s => ((Action)s)(), continuation, m_Task.m_Token);

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
    }

    /// <summary>泛型任务Awaiter</summary>
    public readonly struct VTaskAwaiter<TResult> : ICriticalNotifyCompletion
    {
        private readonly VTask<TResult> m_Task;

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VTaskAwaiter(VTask<TResult> task) => m_Task = task;

        public bool IsCompleted => m_Task.IsCompleted;

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult GetResult() => m_Task.GetResult();

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) => 
            m_Task.m_Source?.OnCompleted(s => ((Action)s)(), continuation, m_Task.m_Token);

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
    }

    #endregion

    #region 方法构建器

    /// <summary>VTask方法构建器</summary>
    public struct VTaskMethodBuilder
    {
        private AsyncVTaskSource m_Source;
        private bool m_HasResult;

        public VTask Task
        {
            [DebuggerHidden]
            get => m_HasResult ? VTask.CompletedTask : new VTask(m_Source, 0);
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VTaskMethodBuilder Create() => new();

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (m_Source == null)
            {
                m_HasResult = true;
                return;
            }
            m_Source.SetResult();
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (m_Source == null) m_Source = new AsyncVTaskSource();
            m_Source.SetException(exception);
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (m_Source == null) m_Source = new AsyncVTaskSource();
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (m_Source == null) m_Source = new AsyncVTaskSource();
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }

    
    /// <summary>泛型VTask方法构建器</summary>
    public struct VTaskMethodBuilder<TResult>
    {
        private AsyncVTaskSource<TResult> m_Source;
        private TResult m_Result;
        private bool m_HasResult;

        public VTask<TResult> Task
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_HasResult ? new VTask<TResult>(m_Result) : new VTask<TResult>(m_Source, 0);
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VTaskMethodBuilder<TResult> Create() => new();

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(TResult result)
        {
            if (m_Source == null)
            {
                m_Result = result;
                m_HasResult = true;
                return;
            }
            m_Source.SetResult(result);
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (m_Source == null) m_Source = new AsyncVTaskSource<TResult>();
            m_Source.SetException(exception);
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (m_Source == null) m_Source = new AsyncVTaskSource<TResult>();
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (m_Source == null) m_Source = new AsyncVTaskSource<TResult>();
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }

    #endregion

    #region 任务源实现

    /// <summary>异步任务源</summary>
    internal class AsyncVTaskSource : IVTaskSource
    {
        private VTaskStatus m_Status;
        private ExceptionDispatchInfo m_Exception;
        private Action<object> m_Continuation;
        private object m_ContinuationState;
        private readonly object m_Lock = new object();

        public VTaskStatus GetStatus(short token) => m_Status;

        public void GetResult(short token)
        {
            if (m_Status == VTaskStatus.Faulted)
                m_Exception?.Throw();
            else if (m_Status == VTaskStatus.Canceled)
                throw new OperationCanceledException();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (m_Lock)
            {
                if (m_Status > VTaskStatus.Pending)
                {
                    continuation(state);
                }
                else
                {
                    m_Continuation = continuation;
                    m_ContinuationState = state;
                }
            }
        }

        public void SetResult()
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;
                m_Status = VTaskStatus.Succeeded;
                InvokeContinuation();
            }
        }

        public void SetException(Exception exception)
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;
                m_Status = exception is OperationCanceledException 
                    ? VTaskStatus.Canceled 
                    : VTaskStatus.Faulted;
                
                m_Exception = ExceptionDispatchInfo.Capture(exception);
                InvokeContinuation();
            }
        }

        private void InvokeContinuation()
        {
            var cont = m_Continuation;
            if (cont != null)
            {
                var state = m_ContinuationState;
                m_Continuation = null;
                m_ContinuationState = null;
                cont(state);
            }
        }
    }

    
    /// <summary>泛型异步任务源</summary>
    internal class AsyncVTaskSource<TResult> : IVTaskSource<TResult>
    {
        private VTaskStatus m_Status;
        private TResult m_Result;
        private ExceptionDispatchInfo m_Exception;
        private Action<object> m_Continuation;
        private object m_ContinuationState;
        private readonly object m_Lock = new object();

        public VTaskStatus GetStatus(short token) => m_Status;

        public TResult GetResult(short token)
        {
            if (m_Status == VTaskStatus.Faulted)
                m_Exception?.Throw();
            else if (m_Status == VTaskStatus.Canceled)
                throw new OperationCanceledException();

            return m_Result;
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (m_Lock)
            {
                if (m_Status > VTaskStatus.Pending)
                {
                    continuation(state);
                }
                else
                {
                    m_Continuation = continuation;
                    m_ContinuationState = state;
                }
            }
        }

        public void SetResult(TResult result)
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;
                m_Status = VTaskStatus.Succeeded;
                m_Result = result;
                InvokeContinuation();
            }
        }

        public void SetException(Exception exception)
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;
                m_Status = exception is OperationCanceledException 
                    ? VTaskStatus.Canceled 
                    : VTaskStatus.Faulted;
                
                m_Exception = ExceptionDispatchInfo.Capture(exception);
                InvokeContinuation();
            }
        }

        private void InvokeContinuation()
        {
            var cont = m_Continuation;
            if (cont != null)
            {
                var state = m_ContinuationState;
                m_Continuation = null;
                m_ContinuationState = null;
                cont(state);
            }
        }
    }

    /// <summary>延迟任务源</summary>
    internal class DelayVTaskSource : IVTaskSource
    {
        private VTaskStatus m_Status;
        private Action<object> m_Continuation;
        private object m_ContinuationState;
        private Timer m_Timer;
        private CancellationTokenRegistration m_CancellationRegistration;
        private readonly object m_Lock = new object();

        public VTaskStatus GetStatus(short token) => m_Status;

        public void GetResult(short token)
        {
            if (m_Status == VTaskStatus.Canceled)
                throw new OperationCanceledException();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (m_Lock)
            {
                if (m_Status > VTaskStatus.Pending)
                {
                    continuation(state);
                }
                else
                {
                    m_Continuation = continuation;
                    m_ContinuationState = state;
                }
            }
        }

        public void Start(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
            {
                m_CancellationRegistration = cancellationToken.Register(() => SetCanceled());
            }
            
            m_Timer = new Timer(OnTimerCallback, null, delay, Timeout.InfiniteTimeSpan);
        }

        private void OnTimerCallback(object state)
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;
                m_Status = VTaskStatus.Succeeded;
                Cleanup();
                InvokeContinuation();
            }
        }

        private void SetCanceled()
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;
                m_Status = VTaskStatus.Canceled;
                Cleanup();
                InvokeContinuation();
            }
        }

        private void Cleanup()
        {
            m_Timer?.Dispose();
            m_Timer = null;
            m_CancellationRegistration.Dispose();
        }

        private void InvokeContinuation()
        {
            var cont = m_Continuation;
            if (cont != null)
            {
                var state = m_ContinuationState;
                m_Continuation = null;
                m_ContinuationState = null;
                cont(state);
            }
        }
    }

    
    /// <summary>让出执行权任务源</summary>
    internal class YieldVTaskSource : IVTaskSource
    {
        private VTaskStatus m_Status;
        private Action<object> m_Continuation;
        private object m_ContinuationState;

        public YieldVTaskSource()
        {
            // 在下一帧完成
            Task.Run(() =>
            {
                m_Status = VTaskStatus.Succeeded;
                InvokeContinuation();
            });
        }

        public VTaskStatus GetStatus(short token) => m_Status;
        public void GetResult(short token) { }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (m_Status > VTaskStatus.Pending)
            {
                continuation(state);
            }
            else
            {
                m_Continuation = continuation;
                m_ContinuationState = state;
            }
        }

        private void InvokeContinuation()
        {
            var cont = m_Continuation;
            if (cont != null)
            {
                var state = m_ContinuationState;
                m_Continuation = null;
                m_ContinuationState = null;
                cont(state);
            }
        }
    }

    
    /// <summary>WhenAll任务源</summary>
    internal class WhenAllVTaskSource : IVTaskSource
    {
        private readonly VTask[] m_Tasks;
        private VTaskStatus m_Status;
        private int m_CompletedCount;
        private Action<object> m_Continuation;
        private object m_ContinuationState;
        private readonly object m_Lock = new object();

        public WhenAllVTaskSource(VTask[] tasks)
        {
            m_Tasks = tasks;
            m_Status = VTaskStatus.Pending;
            StartWaiting();
        }

        public VTaskStatus GetStatus(short token) => m_Status;

        public void GetResult(short token)
        {
            if (m_Status == VTaskStatus.Faulted)
            {
                foreach (var task in m_Tasks)
                {
                    if (task.IsFaulted)
                    {
                        task.GetResult(); // 抛出异常
                    }
                }
            }
            else if (m_Status == VTaskStatus.Canceled)
            {
                throw new OperationCanceledException();
            }
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (m_Lock)
            {
                if (m_Status > VTaskStatus.Pending)
                {
                    continuation(state);
                }
                else
                {
                    m_Continuation = continuation;
                    m_ContinuationState = state;
                }
            }
        }

        private void StartWaiting()
        {
            for (int i = 0; i < m_Tasks.Length; i++)
            {
                var task = m_Tasks[i];
                if (task.IsCompleted)
                {
                    OnTaskCompleted();
                }
                else
                {
                    task.GetAwaiter().OnCompleted(OnTaskCompleted);
                }
            }
        }

        private void OnTaskCompleted()
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;

                m_CompletedCount++;
                if (m_CompletedCount == m_Tasks.Length)
                {
                    bool hasException = false;
                    bool hasCanceled = false;

                    foreach (var task in m_Tasks)
                    {
                        if (task.IsFaulted) hasException = true;
                        else if (task.IsCanceled) hasCanceled = true;
                    }

                    if (hasException) m_Status = VTaskStatus.Faulted;
                    else if (hasCanceled) m_Status = VTaskStatus.Canceled;
                    else m_Status = VTaskStatus.Succeeded;

                    InvokeContinuation();
                }
            }
        }

        private void InvokeContinuation()
        {
            var cont = m_Continuation;
            if (cont != null)
            {
                var state = m_ContinuationState;
                m_Continuation = null;
                m_ContinuationState = null;
                cont(state);
            }
        }
    }

    /// <summary>WhenAll泛型任务源</summary>
    internal class WhenAllVTaskSource<TResult> : IVTaskSource<TResult[]>
    {
        private readonly VTask<TResult>[] m_Tasks;
        private VTaskStatus m_Status;
        private int m_CompletedCount;
        private Action<object> m_Continuation;
        private object m_ContinuationState;
        private readonly object m_Lock = new object();

        public WhenAllVTaskSource(VTask<TResult>[] tasks)
        {
            m_Tasks = tasks;
            m_Status = VTaskStatus.Pending;
            StartWaiting();
        }

        public VTaskStatus GetStatus(short token) => m_Status;

        public TResult[] GetResult(short token)
        {
            if (m_Status == VTaskStatus.Faulted)
            {
                foreach (var task in m_Tasks)
                {
                    if (task.IsFaulted)
                    {
                        task.GetResult(); // 抛出异常
                    }
                }
            }
            else if (m_Status == VTaskStatus.Canceled)
            {
                throw new OperationCanceledException();
            }

            var results = new TResult[m_Tasks.Length];
            for (int i = 0; i < m_Tasks.Length; i++)
            {
                results[i] = m_Tasks[i].GetResult();
            }
            return results;
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (m_Lock)
            {
                if (m_Status > VTaskStatus.Pending)
                {
                    continuation(state);
                }
                else
                {
                    m_Continuation = continuation;
                    m_ContinuationState = state;
                }
            }
        }

        private void StartWaiting()
        {
            for (int i = 0; i < m_Tasks.Length; i++)
            {
                var task = m_Tasks[i];
                if (task.IsCompleted)
                {
                    OnTaskCompleted();
                }
                else
                {
                    task.GetAwaiter().OnCompleted(OnTaskCompleted);
                }
            }
        }

        private void OnTaskCompleted()
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;

                m_CompletedCount++;
                if (m_CompletedCount == m_Tasks.Length)
                {
                    bool hasException = false;
                    bool hasCanceled = false;

                    foreach (var task in m_Tasks)
                    {
                        if (task.IsFaulted) hasException = true;
                        else if (task.IsCanceled) hasCanceled = true;
                    }

                    if (hasException) m_Status = VTaskStatus.Faulted;
                    else if (hasCanceled) m_Status = VTaskStatus.Canceled;
                    else m_Status = VTaskStatus.Succeeded;

                    InvokeContinuation();
                }
            }
        }

        private void InvokeContinuation()
        {
            var cont = m_Continuation;
            if (cont != null)
            {
                var state = m_ContinuationState;
                m_Continuation = null;
                m_ContinuationState = null;
                cont(state);
            }
        }
    }

    /// <summary>WhenAny任务源</summary>
    internal class WhenAnyVTaskSource : IVTaskSource<int>
    {
        private readonly VTask[] m_Tasks;
        private VTaskStatus m_Status;
        private int m_CompletedIndex = -1;
        private Action<object> m_Continuation;
        private object m_ContinuationState;
        private readonly object m_Lock = new object();

        public WhenAnyVTaskSource(VTask[] tasks)
        {
            m_Tasks = tasks;
            m_Status = VTaskStatus.Pending;
            StartWaiting();
        }

        public VTaskStatus GetStatus(short token) => m_Status;

        public int GetResult(short token)
        {
            if (m_Status == VTaskStatus.Canceled)
                throw new OperationCanceledException();
            
            return m_CompletedIndex;
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (m_Lock)
            {
                if (m_Status > VTaskStatus.Pending)
                {
                    continuation(state);
                }
                else
                {
                    m_Continuation = continuation;
                    m_ContinuationState = state;
                }
            }
        }

        private void StartWaiting()
        {
            for (int i = 0; i < m_Tasks.Length; i++)
            {
                var task = m_Tasks[i];
                var index = i;
                if (task.IsCompleted)
                {
                    OnTaskCompleted(index);
                }
                else
                {
                    task.GetAwaiter().OnCompleted(() => OnTaskCompleted(index));
                }
            }
        }

        private void OnTaskCompleted(int index)
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;

                m_CompletedIndex = index;
                m_Status = VTaskStatus.Succeeded;
                InvokeContinuation();
            }
        }

        private void InvokeContinuation()
        {
            var cont = m_Continuation;
            if (cont != null)
            {
                var state = m_ContinuationState;
                m_Continuation = null;
                m_ContinuationState = null;
                cont(state);
            }
        }
    }

    /// <summary>WhenAny泛型任务源</summary>
    internal class WhenAnyVTaskSource<TResult> : IVTaskSource<VTask<TResult>>
    {
        private readonly VTask<TResult>[] m_Tasks;
        private VTaskStatus m_Status;
        private int m_CompletedIndex = -1;
        private Action<object> m_Continuation;
        private object m_ContinuationState;
        private readonly object m_Lock = new object();

        
        public WhenAnyVTaskSource(VTask<TResult>[] tasks)
        {
            m_Tasks = tasks;
            m_Status = VTaskStatus.Pending;
            StartWaiting();
        }

        public VTaskStatus GetStatus(short token) => m_Status;

        public VTask<TResult> GetResult(short token)
        {
            if (m_Status == VTaskStatus.Canceled)
                throw new OperationCanceledException();
            
            return m_Tasks[m_CompletedIndex];
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (m_Lock)
            {
                if (m_Status > VTaskStatus.Pending)
                {
                    continuation(state);
                }
                else
                {
                    m_Continuation = continuation;
                    m_ContinuationState = state;
                }
            }
        }

        private void StartWaiting()
        {
            for (int i = 0; i < m_Tasks.Length; i++)
            {
                var task = m_Tasks[i];
                var index = i;
                if (task.IsCompleted)
                {
                    OnTaskCompleted(index);
                }
                else
                {
                    task.GetAwaiter().OnCompleted(() => OnTaskCompleted(index));
                }
            }
        }

        private void OnTaskCompleted(int index)
        {
            lock (m_Lock)
            {
                if (m_Status != VTaskStatus.Pending) return;

                m_CompletedIndex = index;
                m_Status = VTaskStatus.Succeeded;
                InvokeContinuation();
            }
        }

        private void InvokeContinuation()
        {
            var cont = m_Continuation;
            if (cont != null)
            {
                var state = m_ContinuationState;
                m_Continuation = null;
                m_ContinuationState = null;
                cont(state);
            }
        }
    }

    #endregion

    #region 扩展方法

    /// <summary>任务扩展方法</summary>
    public static class VTaskExtensions
    {
        /// <summary>转换为 System.Threading.Tasks.Task</summary>
        public static Task AsTask(this VTask self)
        {
            var tcs = new TaskCompletionSource<object>();
            
            if (self.IsCompleted)
            {
                HandleCompleted(self, tcs);
            }
            else
            {
                self.GetAwaiter().OnCompleted(() => HandleCompleted(self, tcs));
            }
            
            return tcs.Task;
        }

        /// <summary>转换为 System.Threading.Tasks.Task</summary>
        public static Task<TResult> AsTask<TResult>(this VTask<TResult> self)
        {
            var tcs = new TaskCompletionSource<TResult>();
            
            if (self.IsCompleted)
            {
                HandleCompleted(self, tcs);
            }
            else
            {
                self.GetAwaiter().OnCompleted(() => HandleCompleted(self, tcs));
            }
            
            return tcs.Task;
        }

        /// <summary>从 System.Threading.Tasks.Task 转换为 VTask</summary>
        public static VTask AsVTask(this Task task)
        {
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                    return VTask.FromException(task.Exception.InnerException ?? task.Exception);
                if (task.IsCanceled)
                    return VTask.FromCanceled(CancellationToken.None);
                return VTask.CompletedTask;
            }

            var source = new AsyncVTaskSource();
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    source.SetException(t.Exception.InnerException ?? t.Exception);
                else if (t.IsCanceled)
                    source.SetException(new OperationCanceledException());
                else
                    source.SetResult();
            }, TaskContinuationOptions.ExecuteSynchronously);

            return new VTask(source, 0);
        }

        /// <summary>从 System.Threading.Tasks.Task 转换为 VTask</summary>
        public static VTask<TResult> AsVTask<TResult>(this Task<TResult> task)
        {
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                    return VTask<TResult>.FromException(task.Exception.InnerException ?? task.Exception);
                if (task.IsCanceled)
                    return VTask<TResult>.FromCanceled(CancellationToken.None);
                return VTask<TResult>.FromResult(task.Result);
            }

            var source = new AsyncVTaskSource<TResult>();
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    source.SetException(t.Exception.InnerException ?? t.Exception);
                else if (t.IsCanceled)
                    source.SetException(new OperationCanceledException());
                else
                    source.SetResult(t.Result);
            }, TaskContinuationOptions.ExecuteSynchronously);

            return new VTask<TResult>(source, 0);
        }

        private static void HandleCompleted(VTask task, TaskCompletionSource<object> tcs)
        {
            switch (task.Status)
            {
                case VTaskStatus.Succeeded:
                    tcs.SetResult(null);
                    break;
                case VTaskStatus.Canceled:
                    tcs.SetCanceled();
                    break;
                case VTaskStatus.Faulted:
                    try
                    {
                        task.GetResult();
                        tcs.SetException(new InvalidOperationException("Unexpected successful result in faulted state"));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    break;
                default:
                    tcs.SetException(new InvalidOperationException("Invalid task state"));
                    break;
            }
        }

        private static void HandleCompleted<TResult>(VTask<TResult> task, TaskCompletionSource<TResult> tcs)
        {
            switch (task.Status)
            {
                case VTaskStatus.Succeeded:
                    tcs.SetResult(task.GetResult());
                    break;
                case VTaskStatus.Canceled:
                    tcs.SetCanceled();
                    break;
                case VTaskStatus.Faulted:
                    try
                    {
                        task.GetResult();
                        tcs.SetException(new InvalidOperationException("Unexpected successful result in faulted state"));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    break;
                default:
                    tcs.SetException(new InvalidOperationException("Invalid task state"));
                    break;
            }
        }
    }

    #endregion
}