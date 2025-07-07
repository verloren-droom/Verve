#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Debugger
{
    using Verve.Debugger;

    
    /// <summary>
    /// 调试器功能
    /// </summary>
    [SkipInStackTrace, System.Serializable]
    public partial class DebuggerFeature : Verve.Debugger.DebuggerFeature
    {
        protected override void OnLoad()
        {
            m_DebuggerSubmodule = new GenericDebuggerSubmodule();
            m_DebuggerSubmodule?.OnModuleLoaded();
        }
    }
}

#endif