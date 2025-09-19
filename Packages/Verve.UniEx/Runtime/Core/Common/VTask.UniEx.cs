namespace Verve.UniEx
{
    using System;
    using System.Threading;
    using System.Collections.Generic;

    
    internal class VTaskScheduler : ComponentInstanceBase<VTaskScheduler>
    {
        private static int? s_MainThreadId;
        
        public static bool IsMainThread
        {
            get
            {
                s_MainThreadId ??= Thread.CurrentThread.ManagedThreadId;
                return Thread.CurrentThread.ManagedThreadId == s_MainThreadId.Value;
            }
        }
    }
}