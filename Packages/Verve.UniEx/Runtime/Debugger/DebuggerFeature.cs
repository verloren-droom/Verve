#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Debugger
{
    using Verve.Debug;

    
    /// <summary>
    /// 调试器功能
    /// </summary>
    [SkipInStackTrace, System.Serializable]
    public partial class DebuggerFeature : Verve.Debug.DebuggerFeature
    {
        protected override void OnLoad(Verve.IReadOnlyFeatureDependencies dependencies)
        {
            m_DebuggerSubmodule = new GenericDebuggerSubmodule();
            m_DebuggerSubmodule.OnModuleLoaded(dependencies);
        }
    }
}

#endif