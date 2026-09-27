namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///   <para>任务扩展。</para>
    /// </summary>
    public static class TaskExtension
    {
        /// <summary>
        ///   <para>取消任务等待；保留共享任务运行，适配缺少原生取消等待的运行时。</para>
        /// </summary>
        /// <param name="self">任务。</param>
        /// <param name="ct">取消令牌。</param>
        public static async Task WaitWithCancellationAsync(this Task self, CancellationToken ct = default)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            ct.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
            await task.WaitAsync(cancellationToken);
#else
            if (!self.IsCompleted && ct.CanBeCanceled)
            {
                var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (ct.Register(() => cancelled.TrySetResult(true)))
                {
                    await Task.WhenAny(self, cancelled.Task);
                    ct.ThrowIfCancellationRequested();
                }
            }
            await self;
#endif
        }

        /// <summary>
        ///   <para>异步等待任务；取消仅结束本次等待。</para>
        /// </summary>
        /// <param name="self">任务。</param>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static async Task<T> WaitWithCancellationAsync<T>(this Task<T> self, CancellationToken ct = default)
        {
            await ((Task)self).WaitWithCancellationAsync(ct);
            return self.GetAwaiter().GetResult();
        }
    }
}