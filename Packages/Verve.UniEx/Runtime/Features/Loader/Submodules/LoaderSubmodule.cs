#if UNITY_5_3_OR_NEWER
    
namespace Verve.UniEx.Loader
{
    using System;
    using UnityEngine;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;


    /// <summary>
    ///   <para>加载器子模块基类</para>
    /// </summary>
    [Serializable]
    public abstract class LoaderSubmodule : GameFeatureSubmodule<LoaderGameFeatureComponent>, ILoaderSubmodule
    {
        public abstract TObject LoadAsset<TObject>(string assetPath);
        public virtual async Task<TObject> LoadAssetAsync<TObject>(string assetPath) => await Task.Run(() => LoadAsset<TObject>(assetPath));
        public virtual void UnloadAsset(string assetPath) {}
        public abstract void UnloadAsset<TObject>(TObject asset);
        public virtual void UnloadAllAsset() {}
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
        
        public virtual async Task<SceneLoaderCallbackContext> LoadSceneAsync(
            string sceneName,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(sceneName)) await Task.Yield();
            var operation = SceneManager.LoadSceneAsync(sceneName, parameters);
            operation.allowSceneActivation = allowSceneActivation;
            while (!operation.isDone)
            {
                float currentProgress = Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(currentProgress);
                if (Mathf.Approximately(currentProgress, 0.9f) || currentProgress >= 0.9f)
                    break;
                await Task.Yield();
            }
            return new SceneLoaderCallbackContext(operation);
        }

        public virtual async Task<SceneLoaderCallbackContext> UnloadSceneAsync(
            string sceneName,
            bool allowSceneActivation = true,
            UnloadSceneOptions options = UnloadSceneOptions.None, 
            Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(sceneName) || !SceneManager.GetSceneByName(sceneName).IsValid()) await Task.Yield();
            var operation = SceneManager.UnloadSceneAsync(sceneName, options);
            if (operation == null) await Task.Yield();
            operation.allowSceneActivation = allowSceneActivation;
            while (!operation.isDone)
            {
                float currentProgress = Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(currentProgress);
                if (Mathf.Approximately(currentProgress, 0.9f) || currentProgress >= 0.9f)
                    break;
                await Task.Yield();
            }
            return new SceneLoaderCallbackContext(operation);
        }
        
        // public virtual async Task<HotFixManifest> CheckForUpdatesAsync(
        //     string version,
        //     string manifestUrl,
        //     ITransientConnection transientConnection,
        //     Func<byte[], HotFixManifest> parseManifest,
        //     CancellationToken ct = default)
        // {
        //     if (string.IsNullOrEmpty(version))
        //         throw new ArgumentException("Current version cannot be null or empty", nameof(version));
        //
        //     byte[] manifestData = await transientConnection.DownloadFileToMemoryAsync(manifestUrl, ct: ct);
        //     HotFixManifest manifest = parseManifest(manifestData);
        //     return manifest?.Version > Version.Parse(version) ? manifest : null;
        // }
        //
        // public virtual async Task<bool> ApplyUpdatesAsync(
        //     HotFixManifest manifest,
        //     ITransientConnection transientConnection,
        //     IHotFixClient.WriteFile writeFile,
        //     Action<HotFixProgress> progressCallback = null,
        //     IHotFixClient.ComputeHash computeHash = null,
        //     CancellationToken ct = default)
        // {
        //     if (manifest == null)
        //         throw new ArgumentNullException(nameof(manifest));
        //     
        //     var progress = new HotFixProgress()
        //     {
        //         TotalFiles = manifest.Assets.Count,
        //         TotalBytes = manifest.Assets.Sum(a => a.Value.Size),
        //         ProcessedFiles = 0,
        //         BytesTransferred = 0,
        //     };
        //     
        //     progressCallback?.Invoke(progress);
        //     
        //     foreach (var asset in manifest.Assets)
        //     { 
        //         ct.ThrowIfCancellationRequested();
        //         
        //         byte[] fileData = await transientConnection.DownloadFileToMemoryAsync(asset.Value.RemoteUrl, ct: ct).ConfigureAwait(false);
        //         if (computeHash != null && computeHash(fileData) != asset.Value.Checksum)
        //         {
        //             throw new Exception($"File {asset.Value.RemoteUrl} has been modified.");
        //         }
        //         bool success = await writeFile(asset.Value.LocalPath, fileData, ct).ConfigureAwait(false);
        //
        //         if (success)
        //         {
        //             progress.BytesTransferred += fileData.Length;
        //             progress.ProcessedFiles++;
        //             progressCallback?.Invoke(progress);
        //         }
        //     }
        //     
        //     return true;
        // }
    }
}
    
#endif