namespace Verve.Application
{
    /// <summary>
    /// 应用功能
    /// </summary>
    [System.Serializable]
    public class ApplicationFeature : GameFeature
    {
        protected IApplicationSubmodule m_Application;
        
        
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            m_Application = new GenericApplicationSubmodule();
            m_Application.OnModuleLoaded(dependencies);
        }
        
        protected override void OnUnload()
        {
            m_Application?.OnModuleUnloaded();
            m_Application = null;
        }
        
        public virtual string PlatformName => System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        public virtual string AppVersion => m_Application.Version;
        public virtual string DeviceId => m_Application.DeviceId;
        
        public virtual void Quit()
        {
            m_Application.QuitApplication();
        }
        
        public virtual void Restart()
        {
            m_Application.RestartApplication();
        }
    }
}