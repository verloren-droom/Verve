#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Debugger
{
    using UnityEngine;
    using Verve.Debugger;
    
    
    [SkipInStackTrace]
    public sealed partial class UnityDebugger : DebuggerBase
    {
        [System.Diagnostics.DebuggerHidden, System.Diagnostics.DebuggerStepThrough]
        protected override void InternalLog_Implement(string msg, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(msg);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(msg);
                    break;
                case LogLevel.Log:
                    Debug.Log(msg);
                    break;
            }
        }

        [System.Diagnostics.DebuggerHidden, System.Diagnostics.DebuggerStepThrough]
        public override void Assert(bool condition, object msg)
        {
            if (!IsEnable || msg == null) return;
            Debug.Assert(condition, msg?.ToString());
        }
    }
}
    
#endif