#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    
    
    public static class CoroutineExtension
    {
        /// <summary> 将Task异步转为Unity协程 </summary>
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
        
        /// <summary> 将Task异步转为Unity协程 </summary>
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
        
        public static Coroutine AsCoroutine(this Task self, Action onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            return CoroutineRunner.Instance.StartCoroutine(self.AsIEnumerator(onComplete, onError, token));
        }
        
        public static Coroutine AsCoroutine<T>(this Task<T> self, Action<T> onComplete = null, Action<Exception> onError = null, CancellationToken token = default)
        {
            return CoroutineRunner.Instance.StartCoroutine(self.AsIEnumerator(onComplete, onError, token));
        }
    }
}

#endif