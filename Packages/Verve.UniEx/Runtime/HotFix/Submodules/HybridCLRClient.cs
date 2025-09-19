#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.HotFix
{
    using System;
    using System.IO;
    using HybridCLR;
    using System.Linq;
    using Verve.HotFix;
    using System.Threading;
    using System.Reflection;
    using System.Threading.Tasks;

    
    /// <summary>
    /// HybridCLR热更客户端
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(HotFixGameFeature), Description = "HybridCLR热更客户端")]
    public sealed class HybridCLRClient : HotFixClientSubmodule
    {
        public override async Task<HotFixManifest> CheckForUpdatesAsync(
            string version,
            string manifestUrl,
            IHotFixClient.DownloadFile downloadManifest,
            Func<byte[], HotFixManifest> parseManifest,
            CancellationToken ct = default)
        {
            CheckIsIL2CPP();
            return await base.CheckForUpdatesAsync(version, manifestUrl, downloadManifest, parseManifest, ct);
        }

        public override async Task<bool> ApplyUpdatesAsync(
            HotFixManifest manifest,
            IHotFixClient.DownloadFile downloadFile,
            IHotFixClient.WriteFile writeFile,
            Action<HotFixProgress> progressCallback = null,
            IHotFixClient.ComputeHash computeHash = null,
            CancellationToken ct = default)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            
            CheckIsIL2CPP();
            
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

                if (Path.GetExtension(asset.Key).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // string assemblyName = Path.GetFileNameWithoutExtension(asset.Key);
                    
                    byte[] fileData = await downloadFile(asset.Value.RemoteUrl, ct).ConfigureAwait(false);
                    if (computeHash != null && computeHash(fileData) != asset.Value.Checksum)
                    {
                        throw new Exception($"File {asset.Value.RemoteUrl} has been modified.");
                    }
                    bool success = await writeFile(Path.Combine(Component.CachePath, asset.Value.LocalPath), fileData, ct).ConfigureAwait(false);
    
                    if (!success)
                    {
                        return false;
                    }
                    progress.BytesTransferred += fileData.Length;
                    progress.ProcessedFiles++;
                    progressCallback?.Invoke(progress);
                    
                    await LoadMetadataForAOTAssembly();
                    Assembly.Load(fileData);
                }
            }
            
            return true;
        }
        
        private void CheckIsIL2CPP()
        {
#if UNITY_EDITOR
            if (UnityEditor.PlayerSettings.GetScriptingBackend(UnityEditor.EditorUserBuildSettings
                    .selectedBuildTargetGroup) != UnityEditor.ScriptingImplementation.IL2CPP && UnityEditor.EditorUserBuildSettings
                    .selectedBuildTargetGroup != UnityEditor.BuildTargetGroup.WebGL)
            {
                throw new NotSupportedException("请将脚本编译器设置为IL2CPP");
            }
#endif
        }

         /// <summary>
         ///  为 AOT Assembly 加载原始 metadata；注意：补充元数据是给 AOT dll 补充元数据，而不是给热更新 dll 补充元数据。
         /// </summary>
         private async Task LoadMetadataForAOTAssembly()
         {
             foreach (var aotDllName in Component.PatchedAOTAssemblyNames)
             {
                 if (string.IsNullOrEmpty(aotDllName)) continue;
                 string dllPath = Path.Combine(Component.CachePath, $"{aotDllName}.dll.bytes");
                 if (!File.Exists(dllPath)) continue;
                 
                 var err = RuntimeApi.LoadMetadataForAOTAssembly(
                     await File.ReadAllBytesAsync(dllPath),
                     HomologousImageMode.SuperSet
                 );
             }
         }
    }
}

#endif