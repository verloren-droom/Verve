namespace Verve.Storage
{
    using File;
    using System;
    using Platform;
    using Serializable;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 持久化存储功能
    /// </summary>
    [Serializable]
    public class StorageFeature : ModularGameFeature
    {
        protected SerializableFeature m_Serializable;
        protected FileFeature m_File;
        protected PlatformFeature m_Platform;
        
        protected override void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            m_Serializable = dependencies.Get<SerializableFeature>();
            m_File = dependencies.Get<FileFeature>();
            m_Platform = dependencies.Get<PlatformFeature>();
            
            RegisterSubmodule(new BinaryStorageSubmodule(m_Serializable));
            RegisterSubmodule(new JsonStorageSubmodule(m_Serializable, m_File, m_Platform));
        }
    }
}