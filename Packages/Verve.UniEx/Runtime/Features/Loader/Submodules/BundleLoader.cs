#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Loader
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    
    /// <summary>
    ///   <para>AssetBundle加载器</para>
    ///   <para>通过AssetBundle加载资源</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(LoaderGameFeature), Description = "AssetBundle加载器 - 通过AssetBundle加载资源")]
    public sealed partial class BundleLoader : LoaderSubmodule
    {
        /// <summary>
        ///   <para>分包缓存</para>
        /// </summary>
        private readonly Dictionary<string, BundleRef> m_Loaded = new Dictionary<string, BundleRef>();
        
        /// <summary>
        ///   <para>互斥锁</para>
        /// </summary>
        private readonly SemaphoreSlim m_Semaphore = new SemaphoreSlim(1, 1);
        
        /// <summary>
        ///   <para>分包引用清单</para>
        /// </summary>
        private AssetBundleManifest m_Manifest;

        
        protected override IEnumerator OnStartup()
        {
            if (Application.isPlaying)
            {
                var loadRequest = AssetBundle.LoadFromFileAsync(System.IO.Path.Combine(Component.RootFolder, "AssetBundle", "AssetBundle"));
    
                while (!loadRequest.isDone)
                {
                    yield return null;
                }
        
                var manifestAB = loadRequest.assetBundle;
                if (manifestAB != null)
                {
                    var manifestRequest = manifestAB.LoadAssetAsync<AssetBundleManifest>(Component.ManifestBundleName);
                    while (!manifestRequest.isDone)
                    {
                        yield return null;
                    }
                    m_Manifest = manifestRequest.asset as AssetBundleManifest;
                }
        
                if (manifestAB != null && m_Manifest == null)
                {
                    manifestAB.Unload(false);
                }
            }
        }

        protected override void OnShutdown()
        {
            foreach (var kv in m_Loaded) kv.Value.Bundle.Unload(true);
            m_Loaded.Clear();
            m_Semaphore?.Dispose();
        }
        
        public override TObject LoadAsset<TObject>(string assetPath)
        {
            var (bundle, assetName) = ParsePath(assetPath);
            LoadBundleAndDeps(bundle);
            var ab = m_Loaded[bundle].Bundle;
            var asset = ab.LoadAsset(assetName, typeof(TObject));
            return (TObject)(object)asset;
        }

        public async Task<TObject> LoadAssetAsync<TObject>(string assetPath,
            CancellationToken ct = default) where TObject : Object
        {
            var (bundle, assetName) = ParsePath(assetPath);
            await LoadBundleAndDepsAsync(bundle, ct);
            var ab = m_Loaded[bundle].Bundle;
            var req = ab.LoadAssetAsync<TObject>(assetName);
            while (!req.isDone) await Task.Yield();
            return req.asset as TObject;
        }

        public override void UnloadAsset<TObject>(TObject asset)
        {
            if (asset == null) return;
            foreach (var kv in m_Loaded)
            {
                if (asset is Object obj && kv.Value.ContainsAsset(obj))
                {
                    kv.Value.Release();
                    if (kv.Value.RefCount == 0)
                    {
                        kv.Value.Bundle.Unload(true);
                        m_Loaded.Remove(kv.Key);
                    }
                    break;
                }
            }
        }
        
        private (string bundle, string assetName) ParsePath(string path)
        {
            int first = path.IndexOf('/');
            if (first <= 0) throw new ArgumentException($"非法路径：{path}");
            var bundle = path.Substring(0, first);
            var asset  = path.Substring(first + 1);
            return (bundle.ToLower(), asset);
        }

        /// <summary>
        ///   <para>同步加载Bundle及依赖</para>
        /// </summary>
        /// <param name="bundle">分包</param>
        private void LoadBundleAndDeps(string bundle)
        {
            if (m_Loaded.ContainsKey(bundle)) { m_Loaded[bundle].Retain(); return; }

            if (m_Manifest != null)
                foreach (var dep in m_Manifest.GetAllDependencies(bundle))
                    LoadBundleAndDeps(dep);

            var path = System.IO.Path.Combine(Component.RootFolder, "AssetBundle", bundle);
            var ab = AssetBundle.LoadFromFile(path);
            if (ab == null) throw new KeyNotFoundException($"Bundle 不存在：{bundle}");
            m_Loaded.Add(bundle, new BundleRef(ab));
        }
        
        /// <summary>
        ///   <para>异步加载Bundle及依赖</para>
        /// </summary>
        /// <param name="bundle">分包</param>
        /// <param name="ct">取消令牌</param>
        private async Task LoadBundleAndDepsAsync(string bundle, CancellationToken ct)
        {
            await m_Semaphore.WaitAsync(ct);
            try
            {
                if (m_Loaded.ContainsKey(bundle)) { m_Loaded[bundle].Retain(); return; }

                if (m_Manifest != null)
                {
                    var deps = m_Manifest.GetAllDependencies(bundle);
                    foreach (var d in deps)
                        await LoadBundleAndDepsAsync(d, ct);
                }

                var path = System.IO.Path.Combine(Component.RootFolder, "AssetBundle", bundle);
                var ab = AssetBundle.LoadFromFileAsync(path);
                while (!ab.isDone) await Task.Yield();
                if (ab.assetBundle == null)
                    throw new KeyNotFoundException($"Bundle 不存在：{bundle}");
                m_Loaded.Add(bundle, new BundleRef(ab.assetBundle));
            }
            finally { m_Semaphore.Release(); }
        }

        /// <summary>
        ///   <para>分包引用计数</para>
        /// </summary>
        private sealed class BundleRef
        {
            /// <summary>
            ///   <para>分包</para>
            /// </summary>
            public AssetBundle Bundle { get; }
            
            /// <summary>
            ///   <para>引用计数</para>
            /// </summary>
            public int RefCount { get; private set; }
            
            private readonly HashSet<int> m_InstanceIds = new HashSet<int>();

            public BundleRef(AssetBundle ab) { Bundle = ab; RefCount = 1; }

            /// <summary>
            ///   <para>增加引用计数</para>
            /// </summary>
            public void Retain() => ++RefCount;
            
            /// <summary>
            ///   <para>减少引用计数</para>
            /// </summary>
            public void Release() => --RefCount;

            /// <summary>
            ///   <para>判断分包内是否包含指定资源</para>
            /// </summary>
            /// <param name="asset">资源</param>
            /// <returns>
            ///   <para>包含返回true</para>
            /// </returns>
            public bool ContainsAsset(Object asset)
            {
                if (asset == null) return false;
                int id = asset.GetInstanceID();
                if (m_InstanceIds.Contains(id)) return true;

                foreach (var o in Bundle.LoadAllAssets())
                {
                    m_InstanceIds.Add(o.GetInstanceID());
                }
                return m_InstanceIds.Contains(id);
            }
        }
    }
}

#endif