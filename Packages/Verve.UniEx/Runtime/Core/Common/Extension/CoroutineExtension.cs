#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>协程扩展</para>
    /// </summary>
    public static class CoroutineExtension
    {
        private static SynchronizationContext s_CurrentContext;
        
        /// <summary>
        ///   <para>检查当前是否在Unity主线程</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOnMainThread()
        {
            return SynchronizationContext.Current == s_CurrentContext;
        }

        private static SynchronizationContext CurrentContext
        {
            get
            {
                s_CurrentContext ??= SynchronizationContext.Current;
                return s_CurrentContext;
            }
        }

        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        public static IEnumerator AsIEnumerator(this Task self, Action onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            if (self == null) yield break;

            // while (!self.IsCompleted && !token.IsCancellationRequested) yield return null;
            yield return new WaitUntil(() => self.IsCompleted || token.IsCancellationRequested);
            
            if (token.IsCancellationRequested)
            {
                onError?.Invoke(new OperationCanceledException(token));
            }
            else if (self.IsFaulted)
            {
                var exception = self.Exception?.Flatten().InnerException ?? self.Exception;
                if (onError != null) InvokeOnContext(onError, exception);
                else InvokeOnContext(() => Debug.LogException(exception));
            }
            else if (onComplete != null && self.IsCompletedSuccessfully)
            {
                InvokeOnContext(onComplete);
            }
        }
        
        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <typeparam name="T">异步结果类型</typeparam>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        public static IEnumerator AsIEnumerator<T>(this Task<T> self, Action<T> onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            if (self == null) yield break;
            
            // while (!self.IsCompleted && !token.IsCancellationRequested) yield return null;
            yield return new WaitUntil(() => self.IsCompleted || token.IsCancellationRequested);
            
            if (token.IsCancellationRequested)
            {
                onError?.Invoke(new OperationCanceledException(token));
            }
            else if (self.IsFaulted)
            {
                var exception = self.Exception?.Flatten().InnerException ?? self.Exception;
                if (onError != null) InvokeOnContext(onError, exception);
                else InvokeOnContext(() => Debug.LogException(exception));
            }
            else if (onComplete != null && self.IsCompletedSuccessfully)
            {
                InvokeOnContext(onComplete, self.Result);
            }
        }

        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <typeparam name="T">异步结果类型</typeparam>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        public static IEnumerator AsIEnumerator(this ValueTask self, Action onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
        {
            if (self == null) yield break;
            
            // while (!self.IsCompleted && !token.IsCancellationRequested) yield return null;
            yield return new WaitUntil(() => self.IsCompleted || token.IsCancellationRequested);
            
            if (token.IsCancellationRequested)
            {
                onError?.Invoke(new OperationCanceledException(token));
            }
            else if (self.IsFaulted)
            {
                try
                {
                    var task = self.Preserve().AsTask();
                    var exception = task.Exception?.Flatten().InnerException ?? task.Exception;
                    if (onError != null) InvokeOnContext(onError, exception);
                    else InvokeOnContext(() => Debug.LogException(exception));
                }
                catch (Exception ex)
                {
                    if (onError != null) InvokeOnContext(onError, ex);
                    else InvokeOnContext(() => Debug.LogException(ex));
                }
            }
            else if (onComplete != null && self.IsCompletedSuccessfully)
            {
                InvokeOnContext(onComplete);
            }
        }

        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <typeparam name="T">异步结果类型</typeparam>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        public static IEnumerator AsIEnumerator<T>(this ValueTask<T> self, Action<T> onComplete = null,
            Action<Exception> onError = null, CancellationToken token = default)
        {
            if (self == null) yield break;
            
            // while (!self.IsCompleted && !token.IsCancellationRequested) yield return null;
            yield return new WaitUntil(() => self.IsCompleted || token.IsCancellationRequested);
            
            if (token.IsCancellationRequested)
            {
                onError?.Invoke(new OperationCanceledException(token));
            }
            else if (self.IsFaulted)
            {
                try
                {
                    var task = self.Preserve().AsTask();
                    var exception = task.Exception?.Flatten().InnerException ?? task.Exception;
                    if (onError != null) InvokeOnContext(onError, exception);
                    else InvokeOnContext(() => Debug.LogException(exception));
                }
                catch (Exception ex)
                {
                    if (onError != null) InvokeOnContext(onError, ex);
                    else InvokeOnContext(() => Debug.LogException(ex));
                }
            }
            else if (onComplete != null && self.IsCompletedSuccessfully)
            {
                InvokeOnContext(onComplete, self.Result);
            }
        }
        
        /// <summary>
        ///   <para>在主线程执行</para>
        /// </summary>
        /// <param name="action"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvokeOnContext(Action action)
        {
            if (action == null) return;
            if (IsOnMainThread()) action();
            else CurrentContext.Post(_ => action(), null);
        }
    
        /// <summary>
        ///   <para>在主线程执行</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvokeOnContext<T>(Action<T> action, T arg)
        {
            if (action == null) return;
            if (IsOnMainThread()) action(arg);
            else CurrentContext.Post(_ => action(arg), null);
        }
        
        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        /// <returns>
        ///   <para>协程句柄</para>
        /// </returns>
        public static CoroutineOperation AsCoroutine(this Task self, Action onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var coroutine = CoroutineRunner.Instance.StartCoroutine(self.AsIEnumerator(onComplete, onError, cts.Token));
            return new CoroutineOperation(coroutine, cts);
        }
        
        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <typeparam name="T">异步结果类型</typeparam>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        /// <returns>
        ///   <para>协程句柄</para>
        /// </returns>
        public static CoroutineOperation AsCoroutine<T>(this Task<T> self, Action<T> onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var coroutine = CoroutineRunner.Instance.StartCoroutine(self.AsIEnumerator(onComplete, onError, cts.Token));
            return new CoroutineOperation(coroutine, cts);
        }
        
        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        /// <returns>
        ///   <para>协程句柄</para>
        /// </returns>
        public static CoroutineOperation AsCoroutine(this ValueTask self, Action onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var coroutine = CoroutineRunner.Instance.StartCoroutine(self.AsIEnumerator(onComplete, onError, cts.Token));
            return new CoroutineOperation(coroutine, cts);
        }

        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <typeparam name="T">异步结果类型</typeparam>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        /// <returns>
        ///   <para>协程句柄</para>
        /// </returns>
        public static CoroutineOperation AsCoroutine<T>(this ValueTask<T> self, Action<T> onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var coroutine = CoroutineRunner.Instance.StartCoroutine(self.AsIEnumerator(onComplete, onError, cts.Token));
            return new CoroutineOperation(coroutine, cts);
        }
    }
    
    
    /// <summary>
    ///   <para>协程操作句柄</para>
    /// </summary>
    public struct CoroutineOperation : IDisposable
    {
        private Coroutine m_Coroutine;
        private CancellationTokenSource m_CancellationTokenSource;
        private bool m_IsDisposed;

        public Coroutine Coroutine => m_Coroutine;
        public CancellationToken CancellationToken => m_CancellationTokenSource?.Token ?? default;
        public bool IsRunning => m_Coroutine != null && !m_IsDisposed;
        public bool IsCancellationRequested => m_CancellationTokenSource?.IsCancellationRequested ?? false;

        internal CoroutineOperation(Coroutine coroutine, CancellationTokenSource cts)
        {
            m_Coroutine = coroutine;
            m_CancellationTokenSource = cts;
            m_IsDisposed = false;
        }

        /// <summary>
        ///   <para>停止协程操作</para>
        /// </summary>
        public void Stop()
        {
            if (m_IsDisposed) return;

            m_CancellationTokenSource?.Cancel();
            
            if (m_Coroutine != null && CoroutineRunner.Instance != null)
            {
                CoroutineRunner.Instance.StopCoroutine(m_Coroutine);
            }
                
            Dispose();
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;
                
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();
            m_CancellationTokenSource = null;
            m_Coroutine = null;
            m_IsDisposed = true;
        }
    }


    /// <summary>
    ///   <para>协程运行器</para>
    /// </summary>
    [DisallowMultipleComponent]
    class CoroutineRunner : ComponentInstanceBase<CoroutineRunner>
    {
        // private static int? s_MainThreadId;
        //
        // /// <summary>
        // ///   <para>当前线程是否是主线程</para>
        // /// </summary>
        // public static bool IsMainThread
        // {
        //     get
        //     {
        //         s_MainThreadId ??= Thread.CurrentThread.ManagedThreadId;
        //         return Thread.CurrentThread.ManagedThreadId == s_MainThreadId.Value;
        //     }
        // }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}

#endif