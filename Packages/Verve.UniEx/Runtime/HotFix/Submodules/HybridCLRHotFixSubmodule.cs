#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.HotFix
{
    using Net;
    using Verve;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using HybridCLR;
    using Verve.HotFix;
    using VerveUniEx.Platform;
    using VerveUniEx.Serializable;
    
    
    /// <summary>
    /// HybridCLR热更子模块
    /// </summary>
    [System.Serializable]
    public partial class HybridCLRHotFixSubmodule : GenericHotFixSubmodule
    {
        public override async Task ApplyUpdateAsync(string checksum, Version targetVersion = null)
        {
#if UNITY_EDITOR
            if (UnityEditor.PlayerSettings.GetScriptingBackend(UnityEditor.EditorUserBuildSettings
                    .selectedBuildTargetGroup) != UnityEditor.ScriptingImplementation.IL2CPP && UnityEditor.EditorUserBuildSettings
                    .selectedBuildTargetGroup != UnityEditor.BuildTargetGroup.WebGL)
            {
                throw new NotSupportedException("HybridCLRHotFixSubmodule: 请将脚本编译器设置为IL2CPP");
            }
#endif
            var manifest = await CheckForUpdatesAsync(checksum);
            foreach (var asset in manifest.Assets)
            {
                if (Path.GetExtension(asset.Key).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    string assemblyName = Path.GetFileNameWithoutExtension(asset.Key);
                    await ReplaceAssembly(assemblyName,
                        await m_Network.GetSubmodule<HttpClientSubmodule>().DownloadFileToMemoryAsync(
                            asset.Value.RemoteUrl,
                            expectedHash: asset.Value.Checksum)
                    );
                }
            }
        }

        /// <summary>
        /// 替换程序集
        /// </summary>
        private Task ReplaceAssembly(string assemblyName, byte[] bytes)
        {
            var err = RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
            
            if (err != 0)
            {
                throw new Exception($"Failed to load metadata for AOT assembly: {assemblyName}");
            }

            var newAssembly = Assembly.Load(bytes);

            var oldAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (oldAssembly != null)
            {
                RedirectTypeReferences(oldAssembly, newAssembly);
            }
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 重定向类型引用
        /// </summary>
        private void RedirectTypeReferences(Assembly oldAssembly, Assembly newAssembly)
        {
           AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => 
           {
               if (args.Name == oldAssembly.FullName)
                   return newAssembly;
               return null;
           };
           
           var featureSystem = GameFeaturesSystem.Runtime;
           foreach (var feature in featureSystem.GetRegisteredFeatures())
           {
               if (featureSystem.GetFeatureState(feature) >= FeatureState.Loaded)
               {
                   var featureObj = featureSystem.GetFeature(feature);
                   if (featureObj.GetType().Assembly == oldAssembly)
                   {
                       featureSystem.ReloadFeature(feature);
                   }
               }
           }
        }
    }
}

#endif