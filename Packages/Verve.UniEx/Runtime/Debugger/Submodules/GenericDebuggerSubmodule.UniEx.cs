#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Debugger
{
    using UnityEngine;
    using Verve.Debug;
    
    
    /// <summary>
    /// Unity通用调试子模块
    /// </summary>
    [SkipInStackTrace]
    public sealed partial class GenericDebuggerSubmodule : DebuggerSubmodule
    {
        public override string ModuleName => "GenericDebugger.UniEx";
        
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
            if (!IsActivate || msg == null) return;
            Debug.Assert(condition, msg?.ToString());
        }
    }
}
    
#endif