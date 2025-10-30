#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Debug
{
    using System;
    using Verve.Debug;
    using System.Diagnostics;
    
    
    /// <summary>
    ///   <para>调试器子模块基类</para>
    /// </summary>
    [Serializable, SkipInStackTrace]
    public abstract class DebuggerSubmodule : TickableGameFeatureSubmodule<DebuggerGameFeatureComponent>, IDebugger
    {
        public LastLogData LastLog { get; protected set; }
        
        [DebuggerHidden, DebuggerStepThrough]
        public virtual void Log(object msg) => Log_Implement(msg?.ToString(), LogLevel.Log);
        [DebuggerHidden, DebuggerStepThrough]
        public virtual void Log(string format, params object[] args) => Log_Implement(string.Format(format, args), LogLevel.Log);
        [DebuggerHidden, DebuggerStepThrough]
        public virtual void LogWarning(object msg) => Log_Implement(msg?.ToString(), LogLevel.Warning);
        [DebuggerHidden, DebuggerStepThrough]
        public virtual void LogWarning(string format, params object[] args) => Log_Implement(string.Format(format, args), LogLevel.Warning);
        [DebuggerHidden, DebuggerStepThrough]
        public virtual void LogError(object msg) => Log_Implement(msg?.ToString(), LogLevel.Error);
        [DebuggerHidden, DebuggerStepThrough]
        public virtual void LogError(string format, params object[] args) => Log_Implement(string.Format(format, args), LogLevel.Error);
        [DebuggerHidden, DebuggerStepThrough]
        public virtual void LogException(Exception exception) => Log_Implement(exception?.Message, LogLevel.Exception);
        [DebuggerHidden, DebuggerStepThrough]
        public virtual void Assert(bool condition, object msg) => Log_Implement(msg?.ToString(), LogLevel.Assert);


        [DebuggerHidden, DebuggerStepThrough]
        protected virtual void Log_Implement(string msg, LogLevel level)
        {
            if (!IsEnabled || string.IsNullOrEmpty(msg)) return;

            LastLog = new LastLogData()
            {
                Message = msg,
                Level = level
            };
            
            InternalLog_Implement(msg, level);
        }
        
        /// <summary>
        ///   <para>内部日志实现</para>
        /// </summary>
        /// <param name="msg">日志信息</param>
        /// <param name="level">日志等级</param>
        [DebuggerHidden, DebuggerStepThrough]
        protected abstract void InternalLog_Implement(string msg, LogLevel level);
        
        // internal DebuggerBase() {}
    }
}

#endif