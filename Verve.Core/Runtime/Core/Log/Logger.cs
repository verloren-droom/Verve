namespace Verve
{
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
    using System.Diagnostics;

    
    /// <summary>
    ///   <para>日志器</para>
    /// </summary>
    [SkipInStackTrace(nameof(Logger))]
    internal sealed class Logger : InstanceBase<Logger>, ILogger
    {
        public bool IsEnabled { get; set; } = true;

        [DebuggerHidden, DebuggerStepThrough]
        public void Log(object msg) => Log_Implement(msg?.ToString(), LogType.Log);
        [DebuggerHidden, DebuggerStepThrough]
        public void Log(string format, params object[] args) => Log_Implement(string.Format(format, args), LogType.Log);
        [DebuggerHidden, DebuggerStepThrough]
        public void LogWarning(object msg) => Log_Implement(msg?.ToString(), LogType.Warning);
        [DebuggerHidden, DebuggerStepThrough]
        public void LogWarning(string format, params object[] args) => Log_Implement(string.Format(format, args), LogType.Warning);
        [DebuggerHidden, DebuggerStepThrough]
        public void LogError(object msg) => Log_Implement(msg?.ToString(), LogType.Error);
        [DebuggerHidden, DebuggerStepThrough]
        public void LogError(string format, params object[] args) => Log_Implement(string.Format(format, args), LogType.Error);
        [DebuggerHidden, DebuggerStepThrough]
        public void LogException(Exception exception) => Log_Implement(exception?.Message, LogType.Exception);
        [DebuggerHidden, DebuggerStepThrough]
        public void Assert(bool condition, object msg) => Log_Implement(msg?.ToString(), LogType.Assert);
        
        /// <summary>
        ///   <para>内部日志实现</para>
        /// </summary>
        /// <param name="msg">日志信息</param>
        /// <param name="level">日志等级</param>
        [DebuggerHidden, DebuggerStepThrough]
        private void Log_Implement(string msg, LogType level)
        {
            if (string.IsNullOrEmpty(msg) || !IsEnabled) return;

#if UNITY_5_3_OR_NEWER
            if (level == LogType.Log)
                UnityEngine.Debug.Log(msg);
            else if (level == LogType.Warning)
                UnityEngine.Debug.LogWarning(msg);
            else if (level == LogType.Error)
                UnityEngine.Debug.LogError(msg);
            else if (level == LogType.Exception)
                UnityEngine.Debug.LogException(new Exception(msg));
            else if (level == LogType.Assert)
                UnityEngine.Debug.LogAssertion(msg);
#else
            Console.WriteLine($"[{level.ToString().ToUpper()}] " + msg);
#endif
        }
    }
}