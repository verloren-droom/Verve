#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Debugger
{
    using Verve.Unit;
    using Verve.Debugger;

    
    [CustomUnit("Debugger"), SkipInStackTrace]
    public partial class DebuggerUnit : Verve.Debugger.DebuggerUnit
    {
        protected override IDebugger Debug => GetService<UnityDebugger>();

        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            AddService(new UnityDebugger());
        }
    }
}

#endif