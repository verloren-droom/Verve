namespace Verve.Debugger
{
    using System;
    using System.Diagnostics;
    
    
    public interface IDebugger : Unit.IUnitService
    {
        bool IsEnable { get; set; }
        LastLogData LastLog { get; }
        [DebuggerHidden, DebuggerStepThrough]
        void Log(object msg);
        [DebuggerHidden, DebuggerStepThrough]
        void Log(string format, params object[] args);
        [DebuggerHidden, DebuggerStepThrough]
        void LogWarning(object msg);
        [DebuggerHidden, DebuggerStepThrough]
        void LogWarning(string format, params object[] args);
        [DebuggerHidden, DebuggerStepThrough]
        void LogError(object msg);
        [DebuggerHidden, DebuggerStepThrough]
        void LogError(string format, params object[] args);
        [DebuggerHidden, DebuggerStepThrough]
        void LogException(Exception exception);
        [DebuggerHidden, DebuggerStepThrough]
        void Assert(bool condition, object msg);
    }
    
    
    public enum LogLevel
    {
        Error,
        Warning,
        Log,
        Exception,
        Assert,
    }
}