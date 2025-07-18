namespace Verve.Debug
{
    using System;
    using System.Diagnostics;
    
    
    /// <summary>
    /// 调试器子模块基类
    /// </summary>
    [Serializable, SkipInStackTrace("Debugger")]
    public abstract class DebuggerSubmodule : IDebuggerSubmodule
    {
        public abstract string ModuleName { get; }
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            IsEnable = true;
        }
        public virtual void OnModuleUnloaded()
        {
            IsEnable = false;
        }

        public bool IsEnable { get; set; } = true;
        
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
            if (!IsEnable || string.IsNullOrEmpty(msg)) return;

            LastLog = new LastLogData()
            {
                Message = msg,
                Level = level
            };
            
            InternalLog_Implement(msg, level);
        }
        
        [DebuggerHidden, DebuggerStepThrough]
        protected abstract void InternalLog_Implement(string msg, LogLevel level);
        
        // internal DebuggerBase() {}
    }

    
    [Serializable]
    public struct LastLogData
    {
        public string Message;
        public LogLevel Level;
    }
}