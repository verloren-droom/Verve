// #if UNITY_5_3_OR_NEWER
//
// namespace Verve.UniEx.HotFix
// {
//     using Net;
//     using Verve;
//     using System;
//     using HybridCLR;
//     using System.IO;
//     using Verve.File;
//     using System.Linq;
//     using System.Reflection;
//     using System.Threading.Tasks;
//     using System.Collections.Generic;
//
//
//     /// <summary>
//     /// HybridCLR热更子模块
//     /// </summary>
//     [System.Serializable, GameFeatureSubmodule(typeof(HotFixGameFeature))]
//     public partial class HybridCLRHotFixSubmodule : HotFixClientSubmodule
//     {
//         private Dictionary<string, Assembly> m_AssemblyCache = new Dictionary<string, Assembly>();
//         /// <summary>
//         /// 为AOT程序集补充元数据列表
//         /// </summary>
//         public string[] PatchedAOTAssemblyList = Array.Empty<string>();
//         
//         /// <summary> 缓存路径，默认为PersistentDataPath/Dlls </summary>
//         public string CachePath { get; set; }
//
//         
//         // public override void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
//         // {
//         //     base.OnModuleLoaded(dependencies);
//         //     CachePath = Path.Combine(m_Platform.GetPersistentDataPath(), "Dlls");
//         //
//         //     if (!Directory.Exists(CachePath))
//         //     {
//         //         Directory.CreateDirectory(CachePath);
//         //     }
//         //     
//         //     _ = Task.Run(async () =>
//         //     {
//         //         foreach (var file in Directory.GetFiles(CachePath, "*.dll.bytes"))
//         //         {
//         //             string name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
//         //             if (IsAssemblyLoaded(name)) continue;
//         //             await LoadMetadataForAOTAssembly();
//         //             await LoadAssemblyAsync(name);
//         //         }
//         //     });
//         // }
//         //
//         // public override void OnModuleUnloaded()
//         // {
//         //     base.OnModuleUnloaded();
//         //     m_AssemblyCache.Clear();
//         // }
//         
//         /// <summary>
//         /// 加载程序集，支持本地和网络下载
//         /// </summary>
//         /// <param name="name"></param>
//         /// <param name="expectedHash"></param>
//         /// <returns></returns>
//         public virtual async Task<Assembly> LoadAssemblyAsync(string name, string expectedHash = null)
//         {
//             if (string.IsNullOrEmpty(name)) return null;
//             CheckIsIL2CPP();
//             if (m_AssemblyCache.TryGetValue(name, out var assembly))
//             {
//                 return assembly;
//             }
//             
// #if !UNITY_EDITOR
//             byte[] bytes;
//             if (Uri.TryCreate(name, UriKind.Absolute, out var res) && 
//                 (res.Scheme == Uri.UriSchemeHttp || res.Scheme == Uri.UriSchemeHttps))
//             {
//                 bytes = await m_HttpClient.DownloadFileToMemoryAsync(name, expectedHash: expectedHash);
//             }
//             else
//             {
//                 string localPath = Path.Combine(CachePath, $"{name}.dll.bytes");
//                 if (!File.Exists(localPath))
//                 {
//                     return null;
//                 }
//                 bytes = await File.ReadAllBytesAsync(localPath);
//             }
//
//             if (!string.IsNullOrEmpty(expectedHash) && bytes.GetSHA256() != expectedHash)
//             {
//                 return null;
//             }
//
//             await LoadMetadataForAOTAssembly();
//             
//             assembly = Assembly.Load(bytes);
//             m_AssemblyCache[name] = assembly;
//             return assembly;
// #else
//             return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == (Path.GetExtension(name).Equals(".dll", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(name) : name));
// #endif
//         }
//         
//         public override async Task<bool> ApplyUpdatesAsync(Verve.HotFix.HotFixManifest manifest, Action<float> progressCallback = null)
//         {
//             if (manifest == null || manifest.Assets.Count == 0 || manifest.Version == null) return false;
//             CheckIsIL2CPP();
//             await LoadMetadataForAOTAssembly();
//         
//             int total = manifest.Assets.Count;
//             int processed = 0;
//             bool hasUpdates = false;
//
//             foreach (var asset in manifest.Assets)
//             {
//                 if (Path.GetExtension(asset.Key).Equals(".dll", StringComparison.OrdinalIgnoreCase))
//                 {
//                     string assemblyName = Path.GetFileNameWithoutExtension(asset.Key);
//                     if (IsAssemblyLoaded(assemblyName)) continue;
//                     string cacheFile = Path.Combine(CachePath, $"{assemblyName}.dll.bytes");
//
//                     bool shouldUpdate = true;
//                     if (File.Exists(cacheFile))
//                     {
//                         byte[] existingBytes = await File.ReadAllBytesAsync(cacheFile);
//
//                         if (existingBytes.GetSHA256() == asset.Value.Checksum)
//                         {
//                             shouldUpdate = false;
//                         }
//                     }
//
//                     // if (shouldUpdate)
//                     // {
//                     //     byte[] bytes = await m_HttpClient.DownloadFileToMemoryAsync(
//                     //         asset.Value.RemoteUrl,
//                     //         expectedHash: asset.Value.Checksum
//                     //     );
//                     //     
//                     //     SafeDeleteFile(cacheFile);
//                     //     await File.WriteAllBytesAsync(cacheFile, bytes);
//                     //
//                     //     if (!m_AssemblyCache.ContainsKey(assemblyName))
//                     //     {
//                     //         await LoadMetadataForAOTAssembly();
//                     //         var assembly = Assembly.Load(bytes);
//                     //         m_AssemblyCache[assemblyName] = assembly;
//                     //     }
//                     //
//                     //     hasUpdates = true;
//                     // }
//                 }
//             
//                 processed++;
//                 progressCallback?.Invoke((float)processed / total);
//             }
//             
//             return hasUpdates;
//         }
//         
// #if UNITY_EDITOR
//         [UnityEditor.MenuItem("Verve/HotFix/Upload")]
//         private static void UploadAssemblyToServer()
//         {
//             UnityEditor.EditorUtility.DisplayProgressBar("HotFix", "Uploading assemblies...", 0f);
//             
//             try 
//             {
//                 var hotUpdateAssemblies = HybridCLR.Editor.Settings.HybridCLRSettings.Instance.hotUpdateAssemblies;
//                 var hotUpdateAssemblyDefinitions = HybridCLR.Editor.Settings.HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions;
//                 
//                 var assemblyNames = new HashSet<string>();
//                 
//                 if (hotUpdateAssemblies != null)
//                 {
//                     foreach (var assemblyName in hotUpdateAssemblies)
//                     {
//                         if (!string.IsNullOrEmpty(assemblyName))
//                         {
//                             assemblyNames.Add(assemblyName);
//                         }
//                     }
//                 }
//                 
//                 if (hotUpdateAssemblyDefinitions != null)
//                 {
//                     foreach (var assemblyDef in hotUpdateAssemblyDefinitions)
//                     {
//                         if (assemblyDef != null)
//                         {
//                             string assemblyName = assemblyDef.name;
//                             if (!string.IsNullOrEmpty(assemblyName))
//                             {
//                                 assemblyNames.Add(assemblyName);
//                             }
//                         }
//                     }
//                 }
//                 
//                 if (assemblyNames.Count == 0)
//                 {
//                     UnityEditor.EditorUtility.DisplayDialog("HotFix", "No hot update assemblies configured in HybridCLR Settings", "OK");
//                     return;
//                 }
//                 
//                 string hotUpdateDllPath = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).ToString(), HybridCLR.Editor.SettingsUtil.GetHotUpdateDllsOutputDirByTarget(UnityEditor.BuildTarget.StandaloneOSX));
//                 if (!Directory.Exists(hotUpdateDllPath)) 
//                 {
//                     UnityEditor.EditorUtility.DisplayDialog("HotFix", $"No HotUpdateDlls directory found at {hotUpdateDllPath}", "OK");
//                     return;
//                 }
//                 
//                 var dllFilesToUpload = new List<string>();
//                 foreach (var assemblyName in assemblyNames)
//                 {
//                     string dllPath = Path.Combine(hotUpdateDllPath, assemblyName + ".dll");
//                     if (File.Exists(dllPath))
//                     {
//                         dllFilesToUpload.Add(dllPath);
//                     }
//                     else
//                     {
//                         UnityEngine.Debug.LogWarning($"Hot update assembly '{assemblyName}' not found at {dllPath}");
//                     }
//                 }
//                 
//                 if (dllFilesToUpload.Count == 0) 
//                 {
//                     UnityEditor.EditorUtility.DisplayDialog("HotFix", "No configured hot update assemblies found to upload", "OK");
//                     return;
//                 }
//                 
//                 for (int i = 0; i < dllFilesToUpload.Count; i++) 
//                 {
//                     string file = dllFilesToUpload[i];
//                     string fileName = Path.GetFileName(file);
//                     float progress = (float)(i + 1) / dllFilesToUpload.Count;
//                     
//                     // UnityEditor.EditorUtility.DisplayProgressBar("HotFix", $"Uploading {fileName}...", progress);
//
//                     // Verve.GameFeaturesSystem.Runtime.GetFeature<NetworkFeature>()
//                     //     .GetSubmodule<HttpClientSubmodule>().UploadFile(Verve.HotFix.HotFixFeatureData.ServerUrl, file, "GET", progressCallback:
//                     //         (l, l1) =>
//                     //         {
//                     //             UnityEditor.EditorUtility.DisplayProgressBar("HotFix", $"Uploading {fileName} ({l}/{l1})...", progress);
//                     //         });
//                 }
//
//                 UnityEditor.EditorUtility.DisplayDialog("HotFix", "Assemblies uploaded successfully!", "OK");
//             }
//             catch (System.Exception ex)
//             {
//                 UnityEngine.Debug.LogException(ex);
//                 UnityEditor.EditorUtility.DisplayDialog("HotFix", $"Failed to upload assemblies: {ex.Message}", "OK");
//             }
//             finally
//             {
//                 UnityEditor.EditorUtility.ClearProgressBar();
//             }
//         }
// #endif
//
//         private void CheckIsIL2CPP()
//         {
// #if UNITY_EDITOR
//             if (UnityEditor.PlayerSettings.GetScriptingBackend(UnityEditor.EditorUserBuildSettings
//                     .selectedBuildTargetGroup) != UnityEditor.ScriptingImplementation.IL2CPP && UnityEditor.EditorUserBuildSettings
//                     .selectedBuildTargetGroup != UnityEditor.BuildTargetGroup.WebGL)
//             {
//                 throw new NotSupportedException("HybridCLRHotFixSubmodule: 请将脚本编译器设置为IL2CPP");
//             }
// #endif
//         }
//
//         private bool IsAssemblyLoaded(string name)
//         {
// #if UNITY_EDITOR
//             return false;
// #else
//             return AppDomain.CurrentDomain.GetAssemblies()
//                 .Any(a => a.GetName().Name.Equals(name, StringComparison.OrdinalIgnoreCase));
// #endif
//         }
//
//         /// <summary>
//         ///  为 AOT Assembly 加载原始 metadata；注意：补充元数据是给 AOT dll 补充元数据，而不是给热更新 dll 补充元数据。
//         /// </summary>
//         private async Task LoadMetadataForAOTAssembly()
//         {
//             foreach (var aotDllName in PatchedAOTAssemblyList)
//             {
//                 if (string.IsNullOrEmpty(aotDllName)) continue;
//                 string dllPath = Path.Combine(CachePath, $"{aotDllName}.dll.bytes");
//                 if (!File.Exists(dllPath)) continue;
//                 
//                 var err = RuntimeApi.LoadMetadataForAOTAssembly(
//                     await File.ReadAllBytesAsync(dllPath),
//                     HomologousImageMode.SuperSet
//                 );
//             }
//         }
//     }
// }
//
// #endif