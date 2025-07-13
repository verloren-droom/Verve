#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.HotFix
{
    using Verve;
    using System;
    using System.IO;
    using Verve.HotFix;
    using System.Threading.Tasks;

    
    /// <summary>
    /// 通用热更新子模块
    /// </summary>
    [System.Serializable]
    public partial class GenericHotFixSubmodule : Verve.HotFix.GenericHotFixSubmodule
    {
        public override string ModuleName => "GenericHotFix.UniEx";

        public override void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            m_Serializable = dependencies.Get<Verve.Serializable.SerializableFeature>();
            m_Platform = dependencies.Get<VerveUniEx.Platform.PlatformFeature>();
            m_Network = dependencies.Get<VerveUniEx.Net.NetworkFeature>();
            m_Application = dependencies.Get<VerveUniEx.Application.ApplicationFeature>();
        }

        public override async Task<HotFixManifest> CheckForUpdatesAsync(string checksum)
        {
            string manifestUrl = Path.Combine(HotFixFeatureData.ServerUrl, HotFixFeatureData.ManifestName);
            if (string.IsNullOrEmpty(checksum))
            {
                throw new ArgumentNullException(nameof(checksum));
            }
            var data = await m_Network.GetSubmodule<VerveUniEx.Net.HttpClientSubmodule>().DownloadFileToMemoryAsync(manifestUrl);
            var initialManifest = m_Serializable.GetSubmodule<Verve.Serializable.JsonSerializableSubmodule>().Deserialize<HotFixManifest>(data);
        
            if (!ValidateFile(data, checksum))
            {
                data = await m_Network.GetSubmodule<VerveUniEx.Net.HttpClientSubmodule>().DownloadFileToMemoryAsync(manifestUrl);
                initialManifest = m_Serializable.GetSubmodule<Verve.Serializable.JsonSerializableSubmodule>().Deserialize<HotFixManifest>(data);
        
                if (!ValidateFile(data, checksum))
                {
                    throw new InvalidOperationException("Manifest checksum mismatch after retry");
                }
            }
            
            return initialManifest;
        }
    }
}

#endif