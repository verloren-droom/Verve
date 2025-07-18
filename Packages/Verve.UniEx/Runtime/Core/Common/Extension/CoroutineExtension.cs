#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    
    
    public static class CoroutineExtension
    {
        /// <summary> 将Task异步转为Unity协程 </summary>
        public static IEnumerator AsIEnumerator(this Task self, Action onComplete = null, CancellationToken token = default)
        {
            if (self == null) yield break;
            // while (!self.IsCompleted)
            //     yield return null;
            yield return new WaitUntil(() => self.IsCompleted || token.IsCancellationRequested);
            if (self.IsFaulted || self.Exception != null)
                throw self.Exception?.Flatten().InnerException ?? self.Exception;
            if (onComplete != null && self.IsCompletedSuccessfully)
            {
                var context = SynchronizationContext.Current;
                if (context == null)
                {
                    onComplete();
                }
                else
                {
                    context.Post(_ => onComplete(), null);
                }
            }
        }
        
        /// <summary> 将Task异步转为Unity协程 </summary>
        public static IEnumerator AsIEnumerator<T>(this Task<T> self, Action<T> onComplete = null, CancellationToken token = default)
        {
            if (self == null) yield break;
            // while (!self.IsCompleted)
            //     yield return null;
            yield return new WaitUntil(() => self.IsCompleted || token.IsCancellationRequested);
            if (self.IsFaulted || self.Exception != null)
                throw self.Exception?.Flatten().InnerException ?? self.Exception;
            if (onComplete != null && self.IsCompletedSuccessfully)
            {
                T result = self.Result;
                var context = SynchronizationContext.Current;
                if (context == null)
                {
                    onComplete(result);
                }
                else
                {
                    context.Post(_ => onComplete(result), null);
                }
            }
        }
    }
}

#endif