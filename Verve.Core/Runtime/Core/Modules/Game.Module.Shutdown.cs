namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public static partial class Game
    {
        /// <summary>
        ///   <para>模块会话结束任务。</para>
        /// </summary>
        private static Task s_ModulesShutdown;

        /// <summary>
        ///   <para>结束模块会话；等待框架持有的容器卸载。</para>
        /// </summary>
        public static Task ShutdownModulesAsync()
        {
            ThrowIfNotOnMainThread(nameof(ShutdownModulesAsync));
            if (GameModuleLifecycleScope.IsActive)
                throw new InvalidOperationException("Cannot shut down from a module lifecycle callback.");
            if (s_ModulesShutdown != null) return s_ModulesShutdown;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            s_ModulesShutdown = completion.Task;
            CompleteModulesShutdownAsync(completion);
            return s_ModulesShutdown;
        }

        /// <summary>
        ///   <para>完成模块清理并报告全部错误。</para>
        /// </summary>
        /// <param name="completion">结束任务。</param>
        private static async void CompleteModulesShutdownAsync(TaskCompletionSource<bool> completion)
        {
            List<Exception> errors = null;
            foreach (var handle in Volatile.Read(ref s_ModulesHandleCache))
            {
                try { await handle.DisposeAsync(); }
                catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
            }
            Task[] pending;
            lock (s_ModulesLock) pending = s_ModuleDisposals.ToArray();
            await Task.WhenAll(pending);
            var failure = ExceptionUtility.Combine(errors);
            if (failure == null) completion.SetResult(true);
            else completion.SetException(failure);
        }
    }
}