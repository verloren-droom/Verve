namespace Verve.Debugger
{
    using System.Diagnostics;
    
    
    /// <summary>
    /// 调试器功能
    /// </summary>
    [System.Serializable]
    public class DebuggerFeature : GameFeature
    {
        protected IDebuggerSubmodule m_DebuggerSubmodule;
        
        
        protected override void OnLoad()
        {
            m_DebuggerSubmodule = new ConsoleDebuggerSubmodule();
            m_DebuggerSubmodule.OnModuleLoaded();
        }

        protected override void OnUnload()
        {
            m_DebuggerSubmodule.OnModuleUnloaded();
            m_DebuggerSubmodule = null;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            m_DebuggerSubmodule.IsEnable = true;
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            m_DebuggerSubmodule.IsEnable = false;
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void Log(object msg)
        {
            m_DebuggerSubmodule?.Log(msg);
        }
        
        [DebuggerHidden, DebuggerStepThrough]
        public void Log(string format, params object[] args)
        {
            m_DebuggerSubmodule?.Log(format, args);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void LogWarning(object msg)
        {
            m_DebuggerSubmodule?.LogWarning(msg);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void LogError(object msg)
        {
            m_DebuggerSubmodule?.LogError(msg);
        }
        
        [DebuggerHidden, DebuggerStepThrough]
        public void Assert(bool condition, object msg)
        {
            m_DebuggerSubmodule?.Assert(condition, msg);
        }

        public LastLogData GetLastLog()
        {
            return m_DebuggerSubmodule?.LastLog ?? default;
        }
    }
}