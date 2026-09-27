#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    ///   <para>协程转换扩展；适配 <see cref="System.Threading.Tasks.Task"/>、<see cref="System.Threading.Tasks.ValueTask"/> 与 Unity 等待机制。</para>
    /// </summary>
    public static class CoroutineExtension
    {
#if UNITY_2023_1_OR_NEWER
        /// <summary>
        ///   <para>转换为可等待操作。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        public static async Awaitable AsAwaitable(this Task self, Action onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
        {
            Game.ThrowIfNotOnMainThread(nameof(AsAwaitable));
            try
            {
                await self.WaitWithCancellationAsync(token);
                onComplete?.Invoke();
            }
            catch (Exception error)
            {
                onError?.Invoke(error);
                throw;
            }
        }

        /// <summary>
        ///   <para>转换为可等待操作。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        public static Awaitable AsAwaitable(this ValueTask self, Action onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
            => self.AsTask().AsAwaitable(onComplete, onError, token);
#endif

        /// <summary>
        ///   <para>转换为协程迭代器。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        public static IEnumerator AsIEnumerator(this Task self, Action onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            while (!self.IsCompleted && !token.IsCancellationRequested) yield return null;
            Exception failure = null;
            try
            {
                token.ThrowIfCancellationRequested();
                self.GetAwaiter().GetResult();
            }
            catch (Exception error) { failure = error; }
            if (failure != null)
            {
                if (onError == null) ExceptionUtility.Rethrow(failure);
                else onError(failure);
            }
            else onComplete?.Invoke();
        }

        /// <summary>
        ///   <para>转换为协程迭代器。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        public static IEnumerator AsIEnumerator(this ValueTask self, Action onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
            => self.AsTask().AsIEnumerator(onComplete, onError, token);

        /// <summary>
        ///   <para>启动协程。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        public static CoroutineOperation AsCoroutine(this Task self, Action onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
            => new(ct => self.AsIEnumerator(onComplete, onError, ct), token);

        /// <summary>
        ///   <para>启动协程。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        public static CoroutineOperation AsCoroutine(this ValueTask self, Action onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
            => self.AsTask().AsCoroutine(onComplete, onError, token);

#if UNITY_2023_1_OR_NEWER
        /// <summary>
        ///   <para>转换为可等待操作。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static async Awaitable<T> AsAwaitable<T>(this Task<T> self, Action<T> onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
        {
            Game.ThrowIfNotOnMainThread(nameof(AsAwaitable));
            try
            {
                var result = await self.WaitWithCancellationAsync(token);
                onComplete?.Invoke(result);
                return result;
            }
            catch (Exception error)
            {
                onError?.Invoke(error);
                throw;
            }
        }

        /// <summary>
        ///   <para>转换为可等待操作。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static Awaitable<T> AsAwaitable<T>(this ValueTask<T> self, Action<T> onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
            => self.AsTask().AsAwaitable(onComplete, onError, token);
#endif

        /// <summary>
        ///   <para>转换为协程迭代器。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static IEnumerator AsIEnumerator<T>(this Task<T> self, Action<T> onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            while (!self.IsCompleted && !token.IsCancellationRequested) yield return null;
            Exception failure = null;
            T result = default;
            try
            {
                token.ThrowIfCancellationRequested();
                result = self.GetAwaiter().GetResult();
            }
            catch (Exception error) { failure = error; }
            if (failure != null)
            {
                if (onError == null) ExceptionUtility.Rethrow(failure);
                else onError(failure);
            }
            else onComplete?.Invoke(result);
        }

        /// <summary>
        ///   <para>转换为协程迭代器。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static IEnumerator AsIEnumerator<T>(this ValueTask<T> self, Action<T> onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
            => self.AsTask().AsIEnumerator(onComplete, onError, token);

        /// <summary>
        ///   <para>启动协程。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static CoroutineOperation AsCoroutine<T>(this Task<T> self, Action<T> onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
            => new(ct => self.AsIEnumerator(onComplete, onError, ct), token);

        /// <summary>
        ///   <para>启动协程。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <param name="onError">错误回调。</param>
        /// <param name="token">取消令牌。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static CoroutineOperation AsCoroutine<T>(this ValueTask<T> self, Action<T> onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
            => self.AsTask().AsCoroutine(onComplete, onError, token);

    }

    /// <summary>
    ///   <para>协程操作句柄；拥有协程与取消源，结束后不再运行。</para>
    /// </summary>
    public sealed class CoroutineOperation : DisposableObject
    {
        /// <summary>
        ///   <para>执行器。</para>
        /// </summary>
        private readonly CoroutineRunner m_Runner;
        /// <summary>
        ///   <para>取消源。</para>
        /// </summary>
        private CancellationTokenSource m_Cancellation;
        /// <summary>
        ///   <para>协程迭代器；停止时显式释放，不依赖 Unity 调用迭代器清理。</para>
        /// </summary>
        private IEnumerator m_Routine;
        /// <summary>
        ///   <para>协程。</para>
        /// </summary>
        public Coroutine Coroutine { get; private set; }
        /// <summary>
        ///   <para>取消令牌。</para>
        /// </summary>
        public CancellationToken CancellationToken { get; }
        /// <summary>
        ///   <para>是否正在运行。</para>
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        ///   <para>是否已请求取消。</para>
        /// </summary>
        public bool IsCancellationRequested => CancellationToken.IsCancellationRequested;

        /// <summary>
        ///   <para>创建协程操作句柄。</para>
        /// </summary>
        /// <param name="routine">协程。</param>
        /// <param name="token">取消令牌。</param>
        internal CoroutineOperation(Func<CancellationToken, IEnumerator> routine, CancellationToken token)
        {
            Game.ThrowIfNotOnMainThread(nameof(CoroutineOperation));
            m_Runner = CoroutineRunner.Instance;
            m_Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            CancellationToken = m_Cancellation.Token;
            IsRunning = true;
            try
            {
                m_Routine = routine(CancellationToken);
                m_Runner.Operations.Add(this);
                var coroutine = m_Runner.StartCoroutine(Run(m_Routine));
                if (IsRunning) Coroutine = coroutine;
            }
            catch (Exception failure)
            {
                try { Dispose(); }
                catch (Exception cleanup) { throw ExceptionUtility.Combine(failure, cleanup); }
                throw;
            }
        }

        /// <summary>
        ///   <para>停止操作。</para>
        /// </summary>
        public void Stop() => Dispose();

        /// <inheritdoc />
        protected override void OnDispose()
        {
            Game.ThrowIfNotOnMainThread(nameof(CoroutineOperation));
            var cancellation = m_Cancellation;
            m_Cancellation = null;
            List<Exception> errors = null;
            try { cancellation?.Cancel(); }
            catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
            try
            {
                if (IsRunning && Coroutine != null && m_Runner != null) m_Runner.StopCoroutine(Coroutine);
            }
            catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
            try { Complete(); }
            catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
            finally { cancellation?.Dispose(); }
            ExceptionUtility.ThrowIfAny(errors);
        }

        /// <summary>
        ///   <para>执行操作。</para>
        /// </summary>
        /// <param name="routine">协程。</param>
        private IEnumerator Run(IEnumerator routine)
        {
            try
            {
                while (IsRunning && routine.MoveNext() && IsRunning) yield return routine.Current;
            }
            finally { Complete(); }
        }

        /// <summary>
        ///   <para>结束操作并解除执行器所有权。</para>
        /// </summary>
        private void Complete()
        {
            IsRunning = false;
            Coroutine = null;
            m_Runner.Operations.Remove(this);
            var routine = m_Routine;
            m_Routine = null;
            var cancellation = m_Cancellation;
            m_Cancellation = null;
            try { (routine as IDisposable)?.Dispose(); }
            finally { cancellation?.Dispose(); }
        }
    }

    /// <summary>
    ///   <para>协程执行组件。</para>
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("")]
    internal sealed class CoroutineRunner : ComponentInstanceBase<CoroutineRunner>
    {
        /// <summary>
        ///   <para>运行中的操作；执行器销毁时全部停止。</para>
        /// </summary>
        internal readonly List<CoroutineOperation> Operations = new();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        protected override void OnDestroy()
        {
            try { ResourceUtility.ReleaseAll(Operations, operation => operation.Dispose()); }
            finally { base.OnDestroy(); }
        }
    }
}

#endif