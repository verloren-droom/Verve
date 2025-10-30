#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    
    
    /// <summary>
    ///   <para>协程扩展</para>
    /// </summary>
    public static class CoroutineExtension
    {
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

            // bool isCompleted = false;
            // self.ContinueWith(t => 
            // {
            //     isCompleted = true;
            // }, TaskContinuationOptions.ExecuteSynchronously);
            // while (!isCompleted && !token.IsCancellationRequested)
            // {
            //     yield return null;
            // }
            yield return new WaitUntil(() => self.IsCompleted || token.IsCancellationRequested);
            
            if (self.IsFaulted)
            {
                var exception = self.Exception?.Flatten().InnerException ?? self.Exception;
                if (onError != null) 
                    InvokeOnContext(onError, exception);
                else
                    throw exception;
            }
            
            if (onComplete != null && self.IsCompletedSuccessfully)
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
            
            // bool isCompleted = false;
            // self.ContinueWith(t => 
            // {
            //     isCompleted = true;
            // }, TaskContinuationOptions.ExecuteSynchronously);
            // while (!isCompleted && !token.IsCancellationRequested)
            // {
            //     yield return null;
            // }
            yield return new WaitUntil(() => self.IsCompleted || token.IsCancellationRequested);
            
            if (self.IsFaulted)
            {
                var exception = self.Exception?.Flatten().InnerException ?? self.Exception;
                if (onError != null) 
                    InvokeOnContext(onError, exception);
                else
                    throw exception;
            }
            
            if (onComplete != null && self.IsCompletedSuccessfully)
            {
                InvokeOnContext(onComplete, self.Result);
            }
        }
        
        private static void InvokeOnContext(Action action)
        {
            var context = SynchronizationContext.Current;
            if (context == null) action();
            else context.Post(_ => action(), null);
        }
    
        private static void InvokeOnContext<T>(Action<T> action, T arg)
        {
            var context = SynchronizationContext.Current;
            if (context == null) action(arg);
            else context.Post(_ => action(arg), null);
        }
        
        /// <summary>
        ///   <para>将异步转为Unity协程</para>
        /// </summary>
        /// <param name="self">异步任务</param>
        /// <param name="onComplete">任务完成回调</param>
        /// <param name="onError">任务错误回调</param>
        /// <param name="token">取消任务</param>
        /// <returns>
        ///   <para>协程</para>
        /// </returns>
        public static Coroutine AsCoroutine(this Task self, Action onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            return CoroutineRunner.Instance.StartCoroutine(self.AsIEnumerator(onComplete, onError, token));
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
        ///   <para>协程</para>
        /// </returns>
        public static Coroutine AsCoroutine<T>(this Task<T> self, Action<T> onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            return CoroutineRunner.Instance.StartCoroutine(self.AsIEnumerator(onComplete, onError, token));
        }
    }
}

#endif