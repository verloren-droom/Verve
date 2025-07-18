namespace Verve.Debug
{
    using System.Diagnostics;
    
    
    /// <summary>
    /// 调试器功能
    /// </summary>
    [System.Serializable]
    public class DebuggerFeature : GameFeature
    {
        protected IDebuggerSubmodule m_DebuggerSubmodule;

        [DebuggerHidden]
        public IDebuggerSubmodule Current => m_DebuggerSubmodule;
        
        
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            m_DebuggerSubmodule = new ConsoleDebuggerSubmodule();
            m_DebuggerSubmodule.OnModuleLoaded(dependencies);
        }

        protected override void OnUnload()
        {
            m_DebuggerSubmodule.OnModuleUnloaded();
            m_DebuggerSubmodule = null;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            m_DebuggerSubmodule.IsActivate = true;
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            m_DebuggerSubmodule.IsActivate = false;
        }
    }
}