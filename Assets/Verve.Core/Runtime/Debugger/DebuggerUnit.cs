namespace Verve.Debugger
{
    
    using Unit;
    using System.Diagnostics;
    
    
    [CustomUnit("Debugger")]
    public partial class DebuggerUnit : UnitBase<IDebugger>
    {
        protected IDebugger m_Debugger =
            new ConsoleDebugger();

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