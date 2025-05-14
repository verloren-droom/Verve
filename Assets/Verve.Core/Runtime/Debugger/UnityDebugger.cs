namespace Verve.Debugger
{
    
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
    
    
    internal sealed partial class UnityDebugger : DebuggerBase
    {
        [System.Diagnostics.DebuggerHidden, System.Diagnostics.DebuggerStepThrough]
        protected override void Log_Implement(string msg, LogLevel level)
        {
            if (!IsEnable || string.IsNullOrEmpty(msg)) return;
            
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
#endif
    
}