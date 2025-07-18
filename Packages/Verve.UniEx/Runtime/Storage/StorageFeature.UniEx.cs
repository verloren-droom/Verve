#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Storage
{
    using Verve;
    using System;
    using System.Collections;
    
    
    /// <summary>
    /// 存储功能
    /// </summary>
    [System.Serializable]
    public partial class StorageFeature : Verve.Storage.StorageFeature
    {
        protected override void OnAfterSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            m_Serializable = dependencies.Get<Verve.Serializable.SerializableFeature>();
            m_File = dependencies.Get<VerveUniEx.File.FileFeature>();
            m_Platform = dependencies.Get<VerveUniEx.Platform.PlatformFeature>();
            
            RegisterSubmodule(new Verve.Storage.BinaryStorageSubmodule(m_Serializable));
            RegisterSubmodule(new Verve.Storage.JsonStorageSubmodule(m_Serializable, m_File, m_Platform));
            RegisterSubmodule(new BuiltInStorageSubmodule(m_Serializable));
        }
    }
}

#endif