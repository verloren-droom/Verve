using System;

namespace Verve.Debugger
{
    using System.Diagnostics;
    
    
    public abstract class DebuggerBase : IDebugger
    {
        public bool IsEnable { get; set; } = true;
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
        protected abstract void Log_Implement(string msg, LogLevel level);
        
        // internal DebuggerBase() {}
    }
    
}