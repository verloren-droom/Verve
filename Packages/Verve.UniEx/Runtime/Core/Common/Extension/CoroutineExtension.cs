#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using System;
    using UnityEngine;
    using System.Collections;
    using System.Threading.Tasks;
    
    
    public static class CoroutineExtension
    {
        /// <summary> 将Task异步转为Unity协程 </summary>
        public static IEnumerator AsCoroutine(this Task self, Action onComplete = null)
        {
            // while (!self.IsCompleted)
            //     yield return null;
            yield return new WaitUntil(() => self.IsCompleted);
            if (self.IsFaulted || self.Exception != null)
                throw self.Exception;
            onComplete?.Invoke();
        }
        
        /// <summary> 将Task异步转为Unity协程 </summary>
        public static IEnumerator AsCoroutine<T>(this Task<T> self, Action<T> onComplete = null)
        {
            // while (!self.IsCompleted)
            //     yield return null;
            yield return new WaitUntil(() => self.IsCompleted);
            if (self.IsFaulted || self.Exception != null)
                throw self.Exception;
            onComplete?.Invoke(self.Result);
        }
    }
}

#endif