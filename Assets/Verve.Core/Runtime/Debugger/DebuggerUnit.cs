namespace Verve.Debugger
{
    
    using Unit;
    using System.Diagnostics;
    
    
    [CustomUnit("Debugger"), SkipInStackTrace]
    public partial class DebuggerUnit : UnitBase<IDebugger>
    {
        protected virtual IDebugger Debug => GetService<ConsoleDebugger>();

        
        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            AddService(new ConsoleDebugger());
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void Log(object msg)
        {
            Debug?.Log(msg);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void LogWarning(object msg)
        {
            Debug?.LogWarning(msg);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void LogError(object msg)
        {
            Debug?.LogError(msg);
        }
        
        [DebuggerHidden, DebuggerStepThrough, Conditional("UNITY_ASSERTIONS")]
        public void Assert(bool condition, object msg)
        {
            Debug?.Assert(condition, msg);
        }
    }
}