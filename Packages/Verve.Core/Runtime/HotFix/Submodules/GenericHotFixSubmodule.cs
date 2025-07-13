namespace Verve.HotFix
{
    using Net;
    using Loader;
    using System;
    using Platform;
    using System.IO;
    using Application;
    using Serializable;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    

    /// <summary>
    /// 通用热更子模块
    /// </summary>
    [Serializable]
    public class GenericHotFixSubmodule : IHotFixSubmodule
    {
        protected NetworkFeature m_Network;
        protected PlatformFeature m_Platform;
        protected SerializableFeature m_Serializable;
        protected ApplicationFeature m_Application;

        
        public virtual string ModuleName => "GenericHotFix";

        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            m_Network = dependencies.Get<NetworkFeature>();
            m_Platform = dependencies.Get<PlatformFeature>();
            m_Serializable = dependencies.Get<SerializableFeature>();
            m_Application = dependencies.Get<ApplicationFeature>();
        }
        
        public virtual void OnModuleUnloaded() { }

        public virtual async Task<HotFixManifest> CheckForUpdatesAsync(string checksum)
        {
            string manifestUrl = Path.Combine(HotFixFeatureData.ServerUrl, HotFixFeatureData.ManifestName);
            string tempPath = Path.Combine(m_Platform.GetTemporaryCachePath(), "hotfix_manifest.json");
            await m_Network.GetSubmodule<HttpClientSubmodule>().DownloadFileAsync(manifestUrl, tempPath);
            string json = await File.ReadAllTextAsync(tempPath);
            var initialManifest = m_Serializable.GetSubmodule<JsonSerializableSubmodule>().Deserialize<HotFixManifest>(System.Text.Encoding.UTF8.GetBytes(json));

            if (!ValidateFile(tempPath, checksum))
            {
                await m_Network.GetSubmodule<HttpClientSubmodule>().DownloadFileAsync(manifestUrl, tempPath);
                json = await File.ReadAllTextAsync(tempPath);
                initialManifest = m_Serializable.GetSubmodule<JsonSerializableSubmodule>().Deserialize<HotFixManifest>(System.Text.Encoding.UTF8.GetBytes(json));
        
                if (!ValidateFile(tempPath, checksum))
                {
                    throw new InvalidOperationException("Manifest checksum mismatch after retry");
                }
            }
    
            File.Delete(tempPath);
            return initialManifest;
        }

        public virtual async Task ApplyUpdateAsync(string checksum, Version targetVersion = null)
        {
            var manifest = await CheckForUpdatesAsync(checksum);
            if (targetVersion != null && manifest.Version != targetVersion)
                throw new InvalidOperationException("Manifest version mismatch");
            // if (manifest.Version <= Version.Parse(m_Application.AppVersion))
            //     return;
            await ApplyManifestUpdate(manifest, targetVersion);
        }

        private async Task ApplyManifestUpdate(HotFixManifest manifest, Version targetVersion)
        {
            foreach (var file in manifest.Assets)
            {
                string tempPath = Path.Combine(m_Platform.GetTemporaryCachePath(), file.Key);
                await m_Network.GetSubmodule<HttpClientSubmodule>().DownloadFileAsync
                    (
                        file.Value.RemoteUrl,
                        tempPath,
                        file.Value.Checksum);
                
                if (!ValidateFile(tempPath, file.Value.Checksum))
                    throw new InvalidOperationException($"{file.Key} File validation failed");
                
                File.Move(tempPath, Path.Combine(m_Platform.GetPersistentDataPath(), file.Key));
            }
        }

        protected bool ValidateFile(string path, string expectedHash)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(path);
                var hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
                return hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        
        protected bool ValidateFile(byte[] bytes, string expectedHash)
        {
            try
            {
                using var md5 = MD5.Create();
                var hash = BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "");
                return hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}