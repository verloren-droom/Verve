// namespace Verve.HotFix
// {
//     using Net;
//     using File;
//     using System;
//     using Platform;
//     using System.IO;
//     using System.Linq;
//     using Application;
//     using Serializable;
//     using System.Security;
//     using System.Reflection;
//     using System.Threading.Tasks;
//     using System.Collections.Generic;
//     using System.Security.Cryptography;
//     
//
//     /// <summary>
//     /// 热更新基类
//     /// </summary>
//     [Serializable]
//     public class HotFixBase : IHotFix
//     {
//         public virtual async Task<Assembly> LoadAssemblyAsync(string name, string expectedHash = null)
//         {
//             return AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == Path.GetFileNameWithoutExtension(name));
//         }
//
//         public virtual async Task<HotFixManifest> CheckForUpdatesAsync(string version)
//         {
//             throw new NotImplementedException();
//             // if (string.IsNullOrEmpty(version))
//             //     throw new ArgumentException("Current version cannot be null or empty", nameof(version));
//             //
//             // string versionInfoUrl = $"{HotFixFeatureData.ServerUrl}/{HotFixFeatureData.LastVersionName}";
//             // string dataResponse = await m_HttpClient.RequestAsync("GET", versionInfoUrl, headers: new Dictionary<string, string>
//             // {
//             //     { "Accept", "application/json" }
//             // });
//             //
//             // var versionInfo = m_JsonSerializer.DeserializeFromString<LatestVersionInfo>(dataResponse);
//             //
//             // if (!Version.TryParse(versionInfo.Version, out Version remoteVersion) ||
//             //     !Version.TryParse(version, out Version localVersion))
//             // {
//             //     throw new FormatException("Invalid version format");
//             // }
//             //
//             // if (remoteVersion <= localVersion)
//             //     return null;
//             //
//             // string manifestName = versionInfo.ManifestFile;
//             // string manifestUrl = $"{HotFixFeatureData.ServerUrl}/{manifestName}";
//             //
//             // string hashName = $"{Path.GetFileNameWithoutExtension(manifestName)}.sha256";
//             // string hashUrl = $"{HotFixFeatureData.ServerUrl}/{hashName}";
//             //
//             // string tempWithoutExt = Path.Combine(m_Platform.GetTemporaryCachePath(), $"temp_{Guid.NewGuid()}");
//             // string tempHashPath = $"{tempWithoutExt}.sha256";
//             // await m_HttpClient.DownloadFileAsync(hashUrl, tempHashPath);
//             //
//             // string expectedHash = (await System.IO.File.ReadAllTextAsync(tempHashPath)).Trim();
//             //
//             // string tempManifestPath = $"{tempWithoutExt}{Path.GetExtension(manifestName)}";
//             // await m_HttpClient.DownloadFileAsync(manifestUrl, tempManifestPath, expectedHash: expectedHash);
//             //
//             // string data = await System.IO.File.ReadAllTextAsync(tempManifestPath);
//             // var manifest = m_JsonSerializer.DeserializeFromString<HotFixManifest>(data);
//             //
//             // SafeDeleteFile(tempHashPath);
//             // SafeDeleteFile(tempManifestPath);
//             //
//             // return manifest;
//         }
//
//         public virtual async Task<bool> ApplyUpdatesAsync(HotFixManifest manifest, Action<float> progressCallback = null)
//         {
//             if (manifest == null || manifest.Assets.Count == 0 || manifest.Version == null) return false;
//             bool success = false;
//             // foreach (var file in manifest.Assets)
//             // {
//             //     float completed = 0;
//             //     
//             //     string tempPath = Path.Combine(m_Platform.GetTemporaryCachePath(), file.Key);
//             //     await m_HttpClient.DownloadFileAsync(
//             //         file.Value.RemoteUrl,
//             //         tempPath,
//             //         progressCallback: (current, total) =>
//             //         {
//             //             float fileProgress = (float)current / total;
//             //             float globalProgress = (completed + fileProgress) / total;
//             //             progressCallback?.Invoke(globalProgress);
//             //         },
//             //         expectedHash: file.Value.Checksum);
//             //
//             //     File.Move(tempPath, Path.Combine(m_Platform.GetPersistentDataPath(), file.Key));
//             //     completed++;
//             //     success = true;
//             // }
//             return success;
//         }
//
//         public void GenerateManifest(string version, string outputDir, string[] dllPaths)
//         {
//             // var manifest = new HotFixManifest
//             // {
//             //     Version = Version.Parse(version),
//             //     Assets = new Dictionary<string, HotFixAssetInfo>()
//             // };
//             //
//             // foreach (var path in dllPaths)
//             // {
//             //     if (!File.Exists(path))
//             //     {
//             //         throw new System.Exception($"File not found: {path}");
//             //     }
//             //     using var stream = System.IO.File.OpenRead(path);
//             //     string hash = stream.GetSHA256();
//             //
//             //     manifest.Assets.Add(Path.GetFileName(path), new HotFixAssetInfo
//             //     {
//             //         Size = new FileInfo(path).Length,
//             //         Checksum = hash,
//             //         RemoteUrl = $"{HotFixFeatureData.ServerUrl}/{Path.GetFileName(path)}"
//             //     });
//             // }
//             //
//             // string data = m_JsonSerializer.SerializeToString(manifest);
//             // string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
//             // string manifestName = $"{Path.GetFileNameWithoutExtension(HotFixFeatureData.ManifestName)}_v{version}_{timestamp}${Path.GetExtension(HotFixFeatureData.ManifestName)}";
//             // string manifestPath = Path.Combine(outputDir, manifestName);
//             // System.IO.File.WriteAllText(manifestPath, data);
//             //
//             // using var dataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data));
//             // string manifestHash = dataStream.GetSHA256();
//             // string hashPath = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(manifestName)}.sha256");
//             // System.IO.File.WriteAllText(hashPath, manifestHash);
//         }
//         
//         protected void SafeDeleteFile(string path)
//         {
//             try
//             {
//                 if (System.IO.File.Exists(path))
//                     System.IO.File.Delete(path);
//             }
//             catch { }
//         }
//         
//         private class LatestVersionInfo
//         {
//             public string Version { get; set; }
//             public string ManifestFile { get; set; }
//         }
//     }
// }