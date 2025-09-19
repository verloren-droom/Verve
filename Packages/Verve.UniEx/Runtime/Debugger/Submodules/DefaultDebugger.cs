#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Debug
{
    using UnityEngine;
    using Verve.Debug;
    
    
    /// <summary>
    /// 默认调试器
    /// </summary>
    [System.Serializable, SkipInStackTrace, GameFeatureSubmodule(typeof(DebugGameFeature), Description = "默认调试器")]
    public sealed partial class DefaultDebugger : DebuggerSubmodule
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
            if (!IsEnabled || msg == null) return;
            Debug.Assert(condition, msg?.ToString());
        }
    }
}
    
#endif