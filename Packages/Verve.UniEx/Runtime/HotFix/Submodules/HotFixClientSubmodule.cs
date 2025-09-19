#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.HotFix
{
    using System;
    using System.IO;
    using System.Linq;
    using Verve.HotFix;
    using System.Threading;
    using System.Reflection;
    using System.Threading.Tasks;


    /// <summary>
    /// 热更新客户端子模块基类
    /// </summary>
    public abstract class HotFixClientSubmodule : GameFeatureSubmodule<HotFixGameFeatureComponent>, IHotFixClient
    {
        public virtual async Task<Assembly> LoadAssemblyAsync(string name, string expectedHash = null)
        {
            return AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == Path.GetFileNameWithoutExtension(name));
        }

        // public virtual async Task<HotFixManifest> CheckForUpdatesAsync(string version)
        // {
        //     // if (string.IsNullOrEmpty(version))
        //     //     throw new ArgumentException("Current version cannot be null or empty", nameof(version));
        //     //
        //     // string versionInfoUrl = $"{HotFixFeatureData.ServerUrl}/{HotFixFeatureData.LastVersionName}";
        //     // string dataResponse = await m_HttpClient.RequestAsync("GET", versionInfoUrl, headers: new Dictionary<string, string>
        //     // {
        //     //     { "Accept", "application/json" }
        //     // });
        //     //
        //     // var versionInfo = m_JsonSerializer.DeserializeFromString<LatestVersionInfo>(dataResponse);
        //     //
        //     // if (!Version.TryParse(versionInfo.Version, out Version remoteVersion) ||
        //     //     !Version.TryParse(version, out Version localVersion))
        //     // {
        //     //     throw new FormatException("Invalid version format");
        //     // }
        //     //
        //     // if (remoteVersion <= localVersion)
        //     //     return null;
        //     //
        //     // string manifestName = versionInfo.ManifestFile;
        //     // string manifestUrl = $"{HotFixFeatureData.ServerUrl}/{manifestName}";
        //     //
        //     // string hashName = $"{Path.GetFileNameWithoutExtension(manifestName)}.sha256";
        //     // string hashUrl = $"{HotFixFeatureData.ServerUrl}/{hashName}";
        //     //
        //     // string tempWithoutExt = Path.Combine(m_Platform.GetTemporaryCachePath(), $"temp_{Guid.NewGuid()}");
        //     // string tempHashPath = $"{tempWithoutExt}.sha256";
        //     // await m_HttpClient.DownloadFileAsync(hashUrl, tempHashPath);
        //     //
        //     // string expectedHash = (await System.IO.File.ReadAllTextAsync(tempHashPath)).Trim();
        //     //
        //     // string tempManifestPath = $"{tempWithoutExt}{Path.GetExtension(manifestName)}";
        //     // await m_HttpClient.DownloadFileAsync(manifestUrl, tempManifestPath, expectedHash: expectedHash);
        //     //
        //     // string data = await System.IO.File.ReadAllTextAsync(tempManifestPath);
        //     // var manifest = m_JsonSerializer.DeserializeFromString<HotFixManifest>(data);
        //     //
        //     // SafeDeleteFile(tempHashPath);
        //     // SafeDeleteFile(tempManifestPath);
        //     //
        //     // return manifest;
        // }
        //
        // public virtual async Task<bool> ApplyUpdatesAsync(HotFixManifest manifest, Action<float> progressCallback = null)
        // {
        //     if (manifest == null || manifest.Assets.Count == 0 || manifest.Version == null) return false;
        //     bool success = false;
        //     foreach (var file in manifest.Assets)
        //     {
        //         float completed = 0;
        //         
        //         string tempPath = Path.Combine(m_Platform.GetTemporaryCachePath(), file.Key);
        //         await m_HttpClient.DownloadFileAsync(
        //             file.Value.RemoteUrl,
        //             tempPath,
        //             progressCallback: (current, total) =>
        //             {
        //                 float fileProgress = (float)current / total;
        //                 float globalProgress = (completed + fileProgress) / total;
        //                 progressCallback?.Invoke(globalProgress);
        //             },
        //             expectedHash: file.Value.Checksum);
        //     
        //         File.Move(tempPath, Path.Combine(m_Platform.GetPersistentDataPath(), file.Key));
        //         completed++;
        //         success = true;
        //     }
        //     return success;
        // }
        //
        // public void GenerateManifest(string version, string outputDir, string[] dllPaths)
        // {
        //     var manifest = new HotFixManifest
        //     {
        //         Version = Version.Parse(version),
        //         Assets = new Dictionary<string, HotFixAssetInfo>()
        //     };
        //     
        //     foreach (var path in dllPaths)
        //     {
        //         if (!File.Exists(path))
        //         {
        //             throw new System.Exception($"File not found: {path}");
        //         }
        //         using var stream = System.IO.File.OpenRead(path);
        //         string hash = stream.GetSHA256();
        //     
        //         manifest.Assets.Add(Path.GetFileName(path), new HotFixAssetInfo
        //         {
        //             Size = new FileInfo(path).Length,
        //             Checksum = hash,
        //             RemoteUrl = $"{HotFixFeatureData.ServerUrl}/{Path.GetFileName(path)}"
        //         });
        //     }
        //     
        //     string data = m_JsonSerializer.SerializeToString(manifest);
        //     string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        //     string manifestName = $"{Path.GetFileNameWithoutExtension(HotFixFeatureData.ManifestName)}_v{version}_{timestamp}${Path.GetExtension(HotFixFeatureData.ManifestName)}";
        //     string manifestPath = Path.Combine(outputDir, manifestName);
        //     System.IO.File.WriteAllText(manifestPath, data);
        //     
        //     using var dataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data));
        //     string manifestHash = dataStream.GetSHA256();
        //     string hashPath = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(manifestName)}.sha256");
        //     System.IO.File.WriteAllText(hashPath, manifestHash);
        // }
        //
        // protected void SafeDeleteFile(string path)
        // {
        //     try
        //     {
        //         if (System.IO.File.Exists(path))
        //             System.IO.File.Delete(path);
        //     }
        //     catch { }
        // }
        //
        // private class LatestVersionInfo
        // {
        //     public string Version { get; set; }
        //     public string ManifestFile { get; set; }
        // }
        
        public virtual async Task<HotFixManifest> CheckForUpdatesAsync(
            string version,
            string manifestUrl,
            IHotFixClient.DownloadFile downloadManifest,
            Func<byte[], HotFixManifest> parseManifest,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("Current version cannot be null or empty", nameof(version));

            byte[] manifestData = await downloadManifest(manifestUrl, ct);
            HotFixManifest manifest = parseManifest(manifestData);
            return manifest?.Version > Version.Parse(version) ? manifest : null;
        }

        public virtual async Task<bool> ApplyUpdatesAsync(
            HotFixManifest manifest,
            IHotFixClient.DownloadFile downloadFile,
            IHotFixClient.WriteFile writeFile,
            Action<HotFixProgress> progressCallback = null,
            IHotFixClient.ComputeHash computeHash = null,
            CancellationToken ct = default)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            
            var progress = new HotFixProgress()
            {
                TotalFiles = manifest.Assets.Count,
                TotalBytes = manifest.Assets.Sum(a => a.Value.Size),
                ProcessedFiles = 0,
                BytesTransferred = 0,
            };
            
            progressCallback?.Invoke(progress);
            
            foreach (var asset in manifest.Assets)
            { 
                ct.ThrowIfCancellationRequested();
                
                byte[] fileData = await downloadFile(asset.Value.RemoteUrl, ct).ConfigureAwait(false);
                if (computeHash != null && computeHash(fileData) != asset.Value.Checksum)
                {
                    throw new Exception($"File {asset.Value.RemoteUrl} has been modified.");
                }
                bool success = await writeFile(asset.Value.LocalPath, fileData, ct).ConfigureAwait(false);

                if (success)
                {
                    progress.BytesTransferred += fileData.Length;
                    progress.ProcessedFiles++;
                    progressCallback?.Invoke(progress);
                }
            }
            
            return true;
        }
    }
}

#endif