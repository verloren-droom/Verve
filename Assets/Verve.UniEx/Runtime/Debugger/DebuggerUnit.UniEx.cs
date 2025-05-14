namespace VerveUniEx.Debugger
{
    
    using Verve.Unit;
    using Verve.Debugger;

    
    [CustomUnit("Debugger")]
    public partial class DebuggerUnit : Verve.Debugger.DebuggerUnit
    {
        protected new IDebugger m_Debugger = new UnityDebugger();
    }
    
}