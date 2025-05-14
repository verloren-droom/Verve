namespace Verve.Debugger
{
    using Unit;
    using System.Diagnostics;
    using System;
    using System.Collections.Generic;
    
    
    public sealed partial class DebuggerUnit : UnitBase
    {
        private readonly IDebugger m_Debugger =
#if UNITY_5_3_OR_NEWER
            new UnityDebugger();
#else
            new ConsoleDebugger();
#endif

        [DebuggerHidden, DebuggerStepThrough]
        public void Log(object msg)
        {
            m_Debugger?.Log(msg);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void LogWarning(object msg)
        {
            m_Debugger?.LogWarning(msg);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void LogError(object msg)
        {
            m_Debugger?.LogError(msg);
        }
        
        [DebuggerHidden, DebuggerStepThrough, Conditional("UNITY_ASSERTIONS")]
        public void Assert(bool condition, object msg)
        {
            m_Debugger?.Assert(condition, msg);
        }
    }
    
}