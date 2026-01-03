#if UNITY_5_3_OR_NEWER

namespace Verve.ScriptRuntime
{
    using System;
    using System.IO;
    using HybridCLR;
    using UnityEngine;
    using System.Linq;
    using System.Threading;
    using System.Reflection;
    using System.Collections;
    using System.Threading.Tasks;


    /// <summary>
    ///   <para>HybridCLR程序集运行时子模块</para>
    ///   <para>仅支持IL2CPP平台</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(ScriptRuntimeGameFeature), Description = "HybridCLR程序集运行时子模块 - 仅支持IL2CPP平台")]
    public sealed class HybridCLRAssemblyRuntime : AssemblyRuntimeSubmodule
    {
        protected override IEnumerator OnStartup()
        {
            if (Application.isPlaying)
            {
                CheckIsIL2CPP();
                yield return LoadMetadataForAOTAssembly().AsIEnumerator();
            }
        }

        /// <summary>
        ///   <para>加载程序集并添加其中的模块和组件</para>
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="assemblyData">程序集数据</param>
        /// <param name="pdbData">PDB数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>
        ///   <para>程序集</para>
        /// </returns>
        public async Task<Assembly> LoadAssemblyAsync(string assemblyName, byte[] assemblyData, Func<string, Task<UnityEngine.Object>> loadAssetAsync, byte[] pdbData = null, CancellationToken ct = default)
        {
            var assembly = await LoadAssemblyAsync(assemblyName, assemblyData, pdbData, ct);
            if  (assembly == null) throw new Exception($"Assembly {assemblyName} not found.");
            
            if (Component.AssemblyFeatureMappings.Length == 0) return assembly;
            
            var mapping = Component.AssemblyFeatureMappings.FirstOrDefault(x => x.assemblyName == assemblyName);

            if (mapping == null) return assembly;

            // var modulePaths = mapping.modulePaths;
            // for (int i = 0; i < (modulePaths?.Length ?? 0); i++)
            // {
            //     var modulePath = modulePaths[i];
            //     if (modulePath == null) continue;
            //     ct.ThrowIfCancellationRequested();
            //     var moduleAsset = await loadAssetAsync(modulePath);
            //     if (moduleAsset is GameFeatureModule module)
            //     {
            //         GameFeaturesRunner.Instance.ModuleProfile.Add(ScriptableObject.Instantiate(module), true);
            //     }
            // }
            //
            // var componentPaths = mapping.componentPaths;
            // for (int i = 0; i < (componentPaths?.Length ?? 0); i++)
            // {
            //     var componentPath = componentPaths[i];
            //     if (componentPath == null) continue;
            //     ct.ThrowIfCancellationRequested();
            //     var componentAsset = await loadAssetAsync(componentPath);
            //     if (componentAsset is GameFeatureComponent component)
            //     {
            //         GameFeaturesRunner.Instance.ComponentProfile.Add(ScriptableObject.Instantiate(component), true);
            //     }
            // }
            
            var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(GameFeatureModule)) || t.IsSubclassOf(typeof(GameFeatureComponent)));
            foreach (var type in types)
            {
                if (type.IsSubclassOf(typeof(GameFeatureModule)))
                {
                    var modulePaths = mapping.modulePaths;
                    for (int i = 0; i < (modulePaths?.Length ?? 0); i++)
                    {
                        var modulePath = modulePaths[i];
                        if (modulePath == null) continue;
                        ct.ThrowIfCancellationRequested();
                        var moduleAsset = await loadAssetAsync(modulePath);
                        
                        if (moduleAsset is GameFeatureModule module)
                        {
                            GameFeaturesRunner.Instance.ModuleProfile.Add(ScriptableObject.Instantiate(module), true);
                        }
                    }
                }
                else if (type.IsSubclassOf(typeof(GameFeatureComponent)))
                {
                    var componentPaths = mapping.componentPaths;
                    for (int i = 0; i < (componentPaths?.Length ?? 0); i++)
                    {
                        var componentPath = componentPaths[i];
                        if (componentPath == null) continue;
                        ct.ThrowIfCancellationRequested();
                        var componentAsset = await loadAssetAsync(componentPath);
                        if (componentAsset is GameFeatureComponent component)
                        {
                            GameFeaturesRunner.Instance.ComponentProfile.Add(ScriptableObject.Instantiate(component), true);
                        }
                    }
                }
            }
            return assembly;
        }
        
        /// <summary>
        ///   <para>检查是否为IL2CPP</para>
        /// </summary>
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
         ///   <para>为 AOT Assembly 加载原始 metadata</para>
         ///   <para>注意：补充元数据是给 AOT dll 补充元数据，而不是给热更新 dll 补充元数据</para>
         /// </summary>
         private async Task LoadMetadataForAOTAssembly(CancellationToken ct = default)
         {
             foreach (var aotDllName in Component.PatchedAOTAssemblyNames)
             {
                 if (string.IsNullOrEmpty(aotDllName)) continue;
                 string dllPath = Path.Combine(string.IsNullOrEmpty(Component.AOTAssemblyFolder) ? Application.persistentDataPath : Component.AOTAssemblyFolder, $"{aotDllName}.dll.bytes");
                 if (!File.Exists(dllPath)) continue;
                 
                 ct.ThrowIfCancellationRequested();
                 
                 var err = RuntimeApi.LoadMetadataForAOTAssembly(
                     await File.ReadAllBytesAsync(dllPath, ct),
                     HomologousImageMode.SuperSet
                 );
                 if (err != LoadImageErrorCode.OK)
                 {
                     Debug.LogError($"Failed to load AOT metadata for {aotDllName}: {err}");
                 }
             }
         }
    }
}

#endif