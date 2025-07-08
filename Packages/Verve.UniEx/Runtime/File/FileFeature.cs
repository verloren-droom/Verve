#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.File
{
    using Platform;
    using UnityEngine;
    using Verve.Serializable;
    
    
    /// <summary>
    /// 文件功能
    /// </summary>
    [System.Serializable]
    public partial class FileFeature : Verve.File.FileFeature
    {
        protected override void OnLoad()
        {
            base.OnLoad();
            m_FileSubmodule = new GenericFileSubmodule();
            m_FileSubmodule?.OnModuleLoaded();
            m_Serializable = Verve.GameFeaturesSystem.Runtime.GetFeature<SerializableFeature>();
            m_Platform = Verve.GameFeaturesSystem.Runtime.GetFeature<PlatformFeature>();
        }
    }
}

#endif