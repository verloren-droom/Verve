#if UNITY_5_3_OR_NEWER
    
namespace Verve
{
    using UnityEngine;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>协程扩展类</para>
    /// </summary>
    public static class AwaitExtensions
    {
        /// <summary>
        ///   <para>为WaitForSeconds添加GetAwaiter扩展方法，这样就能直接await了</para>
        /// </summary>
        public static TaskAwaiter<object> GetAwaiter(this WaitForSeconds instruction)
        {
            return GetAwaiterReturn(instruction);
        }
        
        public static TaskAwaiter<object> GetAwaiter(this WaitForSecondsRealtime instruction)
        {
            return GetAwaiterReturn(instruction);
        }

        public static TaskAwaiter<object> GetAwaiter(this WaitForFixedUpdate instruction)
        {
            return GetAwaiterReturn(instruction);
        }

        private static TaskAwaiter<object> GetAwaiterReturn(object instruction)
        {
            var tcs = new TaskCompletionSource<object>();
            CoroutineRunner.Instance.StartCoroutine(WaitCoroutine(instruction, tcs));
            return tcs.Task.GetAwaiter();
        }
        
        private static TaskAwaiter<object> GetAwaiterReturn(object instruction, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<object>();
            ct.Register(() => tcs.TrySetCanceled());
            CoroutineRunner.Instance.StartCoroutine(WaitCoroutine(instruction, tcs));
            return tcs.Task.GetAwaiter();
        }

        private static IEnumerator WaitCoroutine(object instruction, TaskCompletionSource<object> tcs)
        {
            yield return instruction;
            if (SynchronizationContext.Current == null)
            {
                // TODO: 
            }
            else
            {
                tcs.TrySetResult(null);
            }
        }
    }
}
    
#endif
