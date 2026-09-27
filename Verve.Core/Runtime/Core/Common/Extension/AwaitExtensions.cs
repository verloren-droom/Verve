#if UNITY_5_3_OR_NEWER
    
namespace Verve
{
    using UnityEngine;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>Unity 指令等待扩展；执行器销毁时取消等待。</para>
    /// </summary>
    public static class AwaitExtensions
    {
        /// <summary>
        ///   <para>为 <see cref="UnityEngine.WaitForSeconds"/> 提供 await 支持。</para>
        /// </summary>
        /// <param name="self">指令。</param>
        public static TaskAwaiter<object> GetAwaiter(this WaitForSeconds self) => GetAwaiterReturn(self);
        
        /// <summary>
        ///   <para>获取等待器。</para>
        /// </summary>
        /// <param name="self">指令。</param>
        public static TaskAwaiter<object> GetAwaiter(this WaitForSecondsRealtime self) => GetAwaiterReturn(self);

        /// <summary>
        ///   <para>获取等待器。</para>
        /// </summary>
        /// <param name="self">指令。</param>
        public static TaskAwaiter<object> GetAwaiter(this WaitForFixedUpdate self) => GetAwaiterReturn(self);

        /// <summary>
        ///   <para>等待协程结果。</para>
        /// </summary>
        /// <param name="instruction">指令。</param>
        private static TaskAwaiter<object> GetAwaiterReturn(object instruction)
        {
            var tcs = new TaskCompletionSource<object>();
            _ = new CoroutineOperation(_ => WaitCoroutine(instruction, tcs), default);
            return tcs.Task.GetAwaiter();
        }
        
        /// <summary>
        ///   <para>等待协程。</para>
        /// </summary>
        /// <param name="instruction">指令。</param>
        /// <param name="tcs">任务完成源。</param>
        private static IEnumerator WaitCoroutine(object instruction, TaskCompletionSource<object> tcs)
        {
            try
            {
                yield return instruction;
                tcs.TrySetResult(null);
            }
            finally { tcs.TrySetCanceled(); }
        }
    }
}
    
#endif