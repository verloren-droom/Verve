#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Application
{
    /// <summary>
    /// 应用功能
    /// </summary>
    [System.Serializable]
    public partial class ApplicationFeature : Verve.Application.ApplicationFeature
    {
        public override string PlatformName =>  UnityEngine.Application.platform.ToString();

        
        protected override void OnLoad()
        {
            base.OnLoad();
#if UNITY_EDITOR
            m_Application = new GenericApplicationSubmodule();
#elif UNITY_STANDALONE_WIN
            m_Application = new WindowApplicationSubmodule();
#elif UNITY_STANDALONE_OSX
            m_Application = new MacApplicationSubmodule();
#elif UNITY_WEBGL
            m_Application = new WebGLApplicationSubmodule();
#else
            m_Application = new GenericApplicationSubmodule();
#endif
            m_Application?.OnModuleLoaded();
        }
    }
}

#endif
