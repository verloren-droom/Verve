namespace Verve.Debug
{
    using System;
    using System.Diagnostics;
    
    
    /// <summary>
    /// 调试器接口
    /// </summary>
    public interface IDebugger
    {
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
}