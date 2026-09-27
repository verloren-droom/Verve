#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;


    /// <summary>
    ///   <para>资源加载模块；实现 <see cref="UnityEngine.AssetBundle"/> 加载，包名与包内路径仅用于内部索引。</para>
    /// </summary>
    public sealed partial class LoaderModule
    {
        /// <summary>
        ///   <para>默认资源包目录名称。</para>
        /// </summary>
        private const string k_DefaultBundleDirectoryName = "AssetBundle";
        /// <summary>
        ///   <para>默认清单资源包文件。</para>
        /// </summary>
        private const string k_DefaultManifestBundleFile = "AssetBundle";
        /// <summary>
        ///   <para>默认清单资源名称。</para>
        /// </summary>
        private const string k_DefaultManifestAssetName = "AssetBundleManifest";

        /// <summary>
        ///   <para>按 Bundle 名称缓存的 <see cref="UnityEngine.AssetBundle"/> 引用。</para>
        /// </summary>
        private readonly Dictionary<string, BundleReference> m_LoadedBundles = new(StringComparer.Ordinal);
        /// <summary>
        ///   <para>项目资源路径到 <see cref="UnityEngine.AssetBundle"/> 内部位置的自动索引。</para>
        /// </summary>
        private readonly Dictionary<string, ResolvedAssetPath> m_AssetIndex = new(Game.PathUtility.ProjectPathComparer);
        /// <summary>
        ///   <para>保护异步 Bundle 加载，避免同一个 Bundle 被并发重复加载。</para>
        /// </summary>
        /// <remarks>不在模块释放时调用 <see cref="SemaphoreSlim.Dispose()"/>，避免尚未结束的异步操作访问已销毁信号量。</remarks>
        private readonly SemaphoreSlim m_BundleLoadGate = new(1, 1);
        /// <summary>
        ///   <para>当前正在异步加载的 Bundle 名称，防止同步路径重复加载同名 Bundle。</para>
        /// </summary>
        private readonly HashSet<string> m_LoadingBundles = new(StringComparer.Ordinal);

        /// <summary>
        ///   <para>资源包目录。</para>
        /// </summary>
        [SerializeField]
        private string m_BundleDirectory;
        /// <summary>
        ///   <para>清单资源名称。</para>
        /// </summary>
        [SerializeField] private string m_ManifestAssetName = k_DefaultManifestAssetName;

        /// <summary>
        ///   <para>清单资源包。</para>
        /// </summary>
        private AssetBundle m_ManifestBundle;
        /// <summary>
        ///   <para>清单。</para>
        /// </summary>
        private AssetBundleManifest m_Manifest;
        /// <summary>
        ///   <para><see cref="UnityEngine.AssetBundle"/> 文件目录；为空时使用 StreamingAssets/AssetBundle。</para>
        /// </summary>
        public string BundleDirectory
        {
            get => string.IsNullOrWhiteSpace(m_BundleDirectory)
                ? Path.Combine(Application.streamingAssetsPath, k_DefaultBundleDirectoryName)
                : m_BundleDirectory;
            set
            {
                ThrowIfLoaderConfigurationLocked(nameof(BundleDirectory));
                m_BundleDirectory = value;
                InvalidateAssetIndex();
            }
        }

        /// <summary>
        ///   <para>Manifest Bundle 内的清单资源名。</para>
        /// </summary>
        public string ManifestAssetName
        {
            get => string.IsNullOrWhiteSpace(m_ManifestAssetName)
                ? k_DefaultManifestAssetName
                : m_ManifestAssetName;
            set
            {
                ThrowIfLoaderConfigurationLocked(nameof(ManifestAssetName));
                m_ManifestAssetName = value;
                InvalidateAssetIndex();
            }
        }

        /// <summary>
        ///   <para>异步初始化 <see cref="UnityEngine.AssetBundle"/> 加载器。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask InstallAssetBundlesAsync(CancellationToken ct) => await RunWithLifecycleCancellationAsync(ct, LoadManifestAsync);

        /// <summary>
        ///   <para>异步卸载 <see cref="UnityEngine.AssetBundle"/> 加载器。</para>
        /// </summary>
        private ValueTask UninstallAssetBundlesAsync()
        {
            UnloadManifestBundle();
            return default;
        }

        /// <summary>
        ///   <para>释放 <see cref="UnityEngine.AssetBundle"/> 加载器。</para>
        /// </summary>
        private void DisposeAssetBundles() => UnloadManifestBundle();

        /// <summary>
        ///   <para>同步创建 <see cref="UnityEngine.AssetBundle"/> 资源引用记录。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="assetPath">规范化项目资源路径。</param>
        private AssetLoadReference CreateAssetBundleAssetReference<TObject>(string assetPath)
            where TObject : UnityEngine.Object
        {
            var resolvedPath = ResolveAssetPath(assetPath);
            var retainedBundles = new List<string>(8);
            try
            {
                LoadBundleAndDependencies(resolvedPath.bundleName, retainedBundles);

                var asset = m_LoadedBundles[resolvedPath.bundleName].Bundle.LoadAsset(resolvedPath.bundleAssetPath, typeof(TObject));
                var typedAsset = CastAsset<TObject>(asset, resolvedPath.assetPath);
                if (typedAsset == null)
                {
                    throw new KeyNotFoundException($"Asset cannot be loaded from AssetBundle. path={resolvedPath.assetPath}, type={typeof(TObject).FullName}");
                }

                return new AssetBundleAssetReference(this, typedAsset, retainedBundles.ToArray());
            }
            catch
            {
                ReleaseBundles(retainedBundles);
                throw;
            }
        }

        /// <summary>
        ///   <para>异步创建 <see cref="UnityEngine.AssetBundle"/> 资源引用记录。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="assetPath">规范化项目资源路径。</param>
        /// <param name="ct">取消令牌。</param>
        private async Task<AssetLoadReference> CreateAssetBundleAssetReferenceAsync<TObject>(
            string assetPath,
            CancellationToken ct)
            where TObject : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();
            var resolvedPath = ResolveAssetPath(assetPath);
            var retainedBundles = new List<string>(8);
            try
            {
                await LoadBundleAndDependenciesAsync(resolvedPath.bundleName, retainedBundles, ct);
                ct.ThrowIfCancellationRequested();

                var request = m_LoadedBundles[resolvedPath.bundleName].Bundle.LoadAssetAsync(resolvedPath.bundleAssetPath, typeof(TObject));
                if (await WaitForUnityOperationAsync(request, ct))
                {
                    ct.ThrowIfCancellationRequested();
                }

                var typedAsset = CastAsset<TObject>(request.asset, resolvedPath.assetPath);
                if (typedAsset == null)
                {
                    throw new KeyNotFoundException($"Asset cannot be loaded from AssetBundle. path={resolvedPath.assetPath}, type={typeof(TObject).FullName}");
                }

                return new AssetBundleAssetReference(this, typedAsset, retainedBundles.ToArray());
            }
            catch
            {
                ReleaseBundles(retainedBundles);
                throw;
            }
        }

        /// <summary>
        ///   <para>释放资源引用清理后仍残留的 Bundle 缓存。</para>
        /// </summary>
        private void UnloadCachedBundles()
        {
            foreach (var bundleRef in m_LoadedBundles.Values)
            {
                if (bundleRef.Bundle != null)
                {
                    bundleRef.Bundle.Unload(true);
                }
            }

            m_LoadedBundles.Clear();
        }

#if DEBUG || UNITY_EDITOR
        /// <summary>
        ///   <para><see cref="UnityEngine.AssetBundle"/> 加载器调试描述。</para>
        /// </summary>
        private string GetAssetBundleDebugDescription() => $"Bundles: {m_LoadedBundles.Count}, Assets: {AssetReferenceCount}, Indexed Paths: {m_AssetIndex.Count}, Manifest: {(m_Manifest == null ? "Missing" : "Loaded")}, Directory: {BundleDirectory}";

        /// <summary>
        ///   <para>复制当前 <see cref="UnityEngine.AssetBundle"/> 文件快照。</para>
        /// </summary>
        /// <param name="output">输出列表。</param>
        private void CopyAssetBundleDebugSnapshots(List<LoaderDebugBundleBuffer> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();

            foreach (var kv in m_LoadedBundles)
            {
                var bundleRef = kv.Value;
                output.Add(new LoaderDebugBundleBuffer(
                    kv.Key,
                    GetBundlePath(kv.Key),
                    bundleRef.RefCount,
                    bundleRef.Bundle != null));
            }
        }
#endif

        /// <summary>
        ///   <para>卸载 Manifest Bundle。</para>
        /// </summary>
        private void UnloadManifestBundle()
        {
            if (m_ManifestBundle != null)
            {
                m_ManifestBundle.Unload(false);
                m_ManifestBundle = null;
            }

            m_Manifest = null;
            InvalidateAssetIndex();
        }

        /// <summary>
        ///   <para>标记资源索引失效。</para>
        /// </summary>
        private void InvalidateAssetIndex()
        {
            m_AssetIndex.Clear();
        }

        /// <summary>
        ///   <para>加载必需的 <see cref="UnityEngine.AssetBundleManifest"/>；清单缺失时停止安装。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private async Task LoadManifestAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var manifestPath = GetBundlePath(GetManifestBundleFileName());
            m_ManifestBundle = await LoadBundleFileAsync(manifestPath, ct);

            var manifestRequest = m_ManifestBundle.LoadAssetAsync<AssetBundleManifest>(ManifestAssetName);
            if (await WaitForUnityOperationAsync(manifestRequest, ct))
            {
                UnloadManifestBundle();
                ct.ThrowIfCancellationRequested();
            }

            m_Manifest = manifestRequest.asset as AssetBundleManifest;
            if (m_Manifest == null)
            {
                m_ManifestBundle.Unload(false);
                m_ManifestBundle = null;
                throw new InvalidDataException($"Manifest asset '{ManifestAssetName}' is missing from {manifestPath}.");
            }
            var json = await Game.HttpUtility.Get(Game.PathUtility.ToRequestUrl(GetBundlePath(AssetBundleCatalog.FileName)), cancellationToken: ct);
            var catalog = JsonUtility.FromJson<AssetBundleCatalog>(json);
            if (catalog?.bundles == null) throw new InvalidDataException("AssetBundle catalog is missing its bundles.");
            var remaining = new HashSet<string>(m_Manifest.GetAllAssetBundles(), StringComparer.Ordinal);
            foreach (var bundle in catalog.bundles)
            {
                if (bundle == null || !remaining.Remove(bundle.name) || bundle.assets == null)
                    throw new InvalidDataException("AssetBundle catalog does not match its manifest.");
                foreach (var path in bundle.assets)
                {
                    var normalized = Game.PathUtility.NormalizeProjectPath(path);
                    m_AssetIndex.Add(normalized, new ResolvedAssetPath(normalized, bundle.name, normalized));
                }
            }
            if (remaining.Count != 0) throw new InvalidDataException("AssetBundle catalog is incomplete.");

        }

        /// <summary>
        ///   <para>同步加载指定 Bundle 及其依赖，并记录本次资源需要持有的 Bundle 列表。</para>
        /// </summary>
        /// <param name="bundleName">Bundle 名称。</param>
        /// <param name="retainedBundles">本次加载已持有的 Bundle 名称列表。</param>
        private void LoadBundleAndDependencies(string bundleName, List<string> retainedBundles)
        {
            if (m_Manifest != null)
            {
                var dependencies = m_Manifest.GetAllDependencies(bundleName);
                for (int i = 0; i < dependencies.Length; i++)
                {
                    RetainOrLoadBundle(NormalizeBundleName(dependencies[i]), retainedBundles);
                }
            }

            RetainOrLoadBundle(bundleName, retainedBundles);
        }

        /// <summary>
        ///   <para>异步加载指定 Bundle 及其依赖，并记录本次资源需要持有的 Bundle 列表。</para>
        /// </summary>
        /// <param name="bundleName">Bundle 名称。</param>
        /// <param name="retainedBundles">本次加载已持有的 Bundle 名称列表。</param>
        /// <param name="ct">取消令牌。</param>
        private async Task LoadBundleAndDependenciesAsync(
            string bundleName,
            List<string> retainedBundles,
            CancellationToken ct)
        {
            if (m_Manifest != null)
            {
                var dependencies = m_Manifest.GetAllDependencies(bundleName);
                for (int i = 0; i < dependencies.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    await RetainOrLoadBundleAsync(NormalizeBundleName(dependencies[i]), retainedBundles, ct);
                }
            }

            ct.ThrowIfCancellationRequested();
            await RetainOrLoadBundleAsync(bundleName, retainedBundles, ct);
        }

        /// <summary>
        ///   <para>同步持有已加载 Bundle，或从文件加载新的 Bundle。</para>
        /// </summary>
        /// <param name="bundleName">Bundle 名称。</param>
        /// <param name="retainedBundles">本次加载已持有的 Bundle 名称列表。</param>
        private void RetainOrLoadBundle(string bundleName, List<string> retainedBundles)
        {
            ThrowIfBundleNameInvalid(bundleName);

            if (m_LoadedBundles.TryGetValue(bundleName, out var existing))
            {
                if (existing.Bundle != null)
                {
                    existing.Retain();
                    retainedBundles.Add(bundleName);
                    return;
                }

                m_LoadedBundles.Remove(bundleName);
            }

            if (m_LoadingBundles.Contains(bundleName))
            {
                throw new InvalidOperationException(
                    $"AssetBundle is already being loaded asynchronously. Use LoadAssetAsync or retry after the pending load completes. bundle={bundleName}");
            }

            var bundlePath = GetBundlePath(bundleName);
            if (bundlePath.Contains("://"))
                throw new NotSupportedException("URL-based AssetBundles require LoadAssetAsync.");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                throw new KeyNotFoundException($"AssetBundle cannot be loaded. bundle={bundleName}, path={bundlePath}");
            }

            m_LoadedBundles.Add(bundleName, new BundleReference(bundle));
            retainedBundles.Add(bundleName);
        }

        /// <summary>
        ///   <para>异步持有已加载 Bundle，或从文件加载新的 Bundle。</para>
        /// </summary>
        /// <param name="bundleName">Bundle 名称。</param>
        /// <param name="retainedBundles">本次加载已持有的 Bundle 名称列表。</param>
        /// <param name="ct">取消令牌。</param>
        private async Task RetainOrLoadBundleAsync(
            string bundleName,
            List<string> retainedBundles,
            CancellationToken ct)
        {
            ThrowIfBundleNameInvalid(bundleName);

            await m_BundleLoadGate.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                if (m_LoadedBundles.TryGetValue(bundleName, out var existing))
                {
                    if (existing.Bundle != null)
                    {
                        existing.Retain();
                        retainedBundles.Add(bundleName);
                        return;
                    }

                    m_LoadedBundles.Remove(bundleName);
                }

                var bundlePath = GetBundlePath(bundleName);
                m_LoadingBundles.Add(bundleName);
                try
                {
                    var bundle = await LoadBundleFileAsync(bundlePath, ct);
                    m_LoadedBundles.Add(bundleName, new BundleReference(bundle));
                    retainedBundles.Add(bundleName);
                }
                finally
                {
                    m_LoadingBundles.Remove(bundleName);
                }
            }
            finally
            {
                m_BundleLoadGate.Release();
            }
        }

        /// <summary>
        ///   <para>批量释放 Bundle 引用计数，并卸载引用归零的 Bundle。</para>
        /// </summary>
        /// <param name="bundleNames">Bundle 名称列表。</param>
        private void ReleaseBundles(IReadOnlyList<string> bundleNames)
        {
            if (bundleNames == null)
            {
                return;
            }

            for (int i = bundleNames.Count - 1; i >= 0; i--)
            {
                var bundleName = bundleNames[i];
                if (!m_LoadedBundles.TryGetValue(bundleName, out var bundleRef)) continue;

                if (bundleRef.Release() > 0) continue;

                if (bundleRef.Bundle != null)
                {
                    bundleRef.Bundle.Unload(true);
                }
                m_LoadedBundles.Remove(bundleName);
            }
        }

        /// <summary>
        ///   <para>解析目录中的资源路径。</para>
        /// </summary>
        /// <param name="assetPath">规范化资源路径。</param>
        private ResolvedAssetPath ResolveAssetPath(string assetPath)
            => m_AssetIndex.TryGetValue(assetPath, out var resolvedPath)
                ? resolvedPath : throw new KeyNotFoundException($"AssetBundle catalog does not contain: {assetPath}");

        /// <summary>
        ///   <para>加载资源包；URL 与 Android APK 使用请求，本地文件使用引擎文件接口。</para>
        /// </summary>
        /// <param name="path">文件路径或 URL。</param>
        /// <param name="ct">取消令牌。</param>
        private static async Task<AssetBundle> LoadBundleFileAsync(string path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (path.Contains("://"))
            {
                using var request = UnityWebRequestAssetBundle.GetAssetBundle(path);
                try
                {
                    await Game.HttpUtility.SendAsync(request, ct);
                    return DownloadHandlerAssetBundle.GetContent(request)
                        ?? throw new InvalidDataException($"Cannot load AssetBundle: {path}");
                }
                catch
                {
                    if (request.isDone && request.result == UnityWebRequest.Result.Success)
                        DownloadHandlerAssetBundle.GetContent(request)?.Unload(true);
                    throw;
                }
            }

            var operation = AssetBundle.LoadFromFileAsync(path);
            if (await WaitForUnityOperationAsync(operation, ct))
            {
                operation.assetBundle?.Unload(true);
                ct.ThrowIfCancellationRequested();
            }
            return operation.assetBundle ?? throw new InvalidDataException($"Cannot load AssetBundle: {path}");
        }

        /// <summary>
        ///   <para>返回 Bundle 文件完整路径。</para>
        /// </summary>
        /// <param name="bundleName">Bundle 名称。</param>
        private string GetBundlePath(string bundleName) => Game.PathUtility.Normalize(BundleDirectory).TrimEnd('/') + "/" + bundleName;

        /// <summary>
        ///   <para>解析根清单包名称；与 Unity 构建时使用的输出目录名称一致。</para>
        /// </summary>
        private string GetManifestBundleFileName()
        {
            var bundleRoot = BundleDirectory?.TrimEnd('/', '\\');
            var manifestBundleFileName = Path.GetFileName(bundleRoot);
            return string.IsNullOrWhiteSpace(manifestBundleFileName)
                ? k_DefaultManifestBundleFile
                : manifestBundleFileName;
        }

        /// <summary>
        ///   <para>规范化 Bundle 名称。</para>
        /// </summary>
        /// <param name="bundleName">原始 Bundle 名称。</param>
        private static string NormalizeBundleName(string bundleName) => bundleName?.Trim().ToLowerInvariant();

        /// <summary>
        ///   <para>资源包名称无效时抛出异常。</para>
        /// </summary>
        /// <param name="bundleName">资源包名称。</param>
        private static void ThrowIfBundleNameInvalid(string bundleName)
        {
            if (string.IsNullOrWhiteSpace(bundleName))
            {
                throw new ArgumentException("AssetBundle name cannot be empty.", nameof(bundleName));
            }
        }

        /// <summary>
        ///   <para>已解析的资源路径。</para>
        /// </summary>
        private readonly struct ResolvedAssetPath
        {
            /// <summary>
            ///   <para>资源路径。</para>
            /// </summary>
            public readonly string assetPath;
            /// <summary>
            ///   <para>资源包名称。</para>
            /// </summary>
            public readonly string bundleName;
            /// <summary>
            ///   <para>资源包资源路径。</para>
            /// </summary>
            public readonly string bundleAssetPath;

            /// <summary>
            ///   <para>创建已解析的资源路径。</para>
            /// </summary>
            /// <param name="assetPath">资源路径。</param>
            /// <param name="bundleName">资源包名称。</param>
            /// <param name="bundleAssetPath">资源包资源路径。</param>
            public ResolvedAssetPath(string assetPath, string bundleName, string bundleAssetPath)
            {
                this.assetPath = assetPath;
                this.bundleName = bundleName;
                this.bundleAssetPath = bundleAssetPath;
            }
        }

        /// <summary>
        ///   <para>单个 <see cref="UnityEngine.AssetBundle"/> 资源引用计数记录。</para>
        /// </summary>
        private sealed class AssetBundleAssetReference : AssetLoadReference
        {
            /// <summary>
            ///   <para>所有者。</para>
            /// </summary>
            private readonly LoaderModule m_Owner;
            /// <summary>
            ///   <para>已释放。</para>
            /// </summary>
            private bool m_Released;

            /// <summary>
            ///   <para>资源依赖的 Bundle 名称列表。</para>
            /// </summary>
            public string[] BundleNames { get; }

            /// <summary>
            ///   <para>创建 <see cref="UnityEngine.AssetBundle"/> 资源引用。</para>
            /// </summary>
            /// <param name="owner">所有者。</param>
            /// <param name="asset">资源。</param>
            /// <param name="bundleNames">Bundle 名称列表。</param>
            public AssetBundleAssetReference(LoaderModule owner, UnityEngine.Object asset, string[] bundleNames)
                : base(asset)
            {
                m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                BundleNames = bundleNames ?? Array.Empty<string>();
            }

            /// <inheritdoc />
            public override void ReleaseResources()
            {
                if (m_Released)
                {
                    return;
                }

                m_Released = true;
                m_Owner.ReleaseBundles(BundleNames);
            }
        }

        /// <summary>
        ///   <para>单个 <see cref="UnityEngine.AssetBundle"/> 文件引用计数记录。</para>
        /// </summary>
        private sealed class BundleReference : LoadReference
        {
            /// <summary>
            ///   <para><see cref="UnityEngine.AssetBundle"/> 实例。</para>
            /// </summary>
            public AssetBundle Bundle { get; }

            /// <summary>
            ///   <para>创建资源包引用。</para>
            /// </summary>
            /// <param name="bundle">资源包。</param>
            public BundleReference(AssetBundle bundle) => Bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
        }
    }
}

#endif
