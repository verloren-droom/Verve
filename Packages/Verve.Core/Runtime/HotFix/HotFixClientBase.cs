namespace Verve.HotFix
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Reflection;
    using System.Threading.Tasks;
    

    /// <summary>
    /// 热更客户端基类
    /// </summary>
    public abstract class HotFixClientBase : IHotFixClient
    {
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