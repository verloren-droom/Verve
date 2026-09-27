#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    
    /// <summary>
    ///   <para>资源加载扩展；按作用域或 Unity 宿主生命周期托管资源。</para>
    /// </summary>
    public static class LoaderAssetExtensions
    {
        /// <summary>
        ///   <para>同步加载资源，并由指定作用域托管返回的资源引用。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="loader">资源加载器。</param>
        /// <param name="assetPath">资源路径；必须使用 Assets/... 或 Packages/... 项目路径。</param>
        /// <param name="scope">资源加载作用域。</param>
        public static TObject LoadAsset<TObject>(this ILoader loader, string assetPath, AssetLoadScope scope)
            where TObject : UnityEngine.Object
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            using var handle = loader.LoadAsset<TObject>(assetPath);
            return scope.Track(handle);
        }

        /// <summary>
        ///   <para>异步加载资源，并由指定作用域托管返回的资源引用。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="loader">资源加载器。</param>
        /// <param name="assetPath">资源路径；必须使用 Assets/... 或 Packages/... 项目路径。</param>
        /// <param name="scope">资源加载作用域。</param>
        /// <param name="ct">取消令牌。</param>
        public static async Task<TObject> LoadAssetAsync<TObject>(
            this ILoader loader,
            string assetPath,
            AssetLoadScope scope,
            CancellationToken ct = default)
            where TObject : UnityEngine.Object
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            using var handle = await loader.LoadAssetAsync<TObject>(assetPath, ct);
            ct.ThrowIfCancellationRequested();
            return scope.Track(handle);
        }

        /// <summary>
        ///   <para>同步加载资源，并绑定到指定组件的 <see cref="UnityEngine.GameObject"/> 生命周期。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="loader">资源加载器。</param>
        /// <param name="assetPath">资源路径；必须使用 Assets/... 或 Packages/... 项目路径。</param>
        /// <param name="owner">资源使用者组件。</param>
        public static TObject LoadAsset<TObject>(this ILoader loader, string assetPath, Component owner)
            where TObject : UnityEngine.Object
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            ThrowIfOwnerInvalid(owner, nameof(owner));

            using var handle = loader.LoadAsset<TObject>(assetPath);
            return GetOrCreateScope(owner).Track(handle);
        }

        /// <summary>
        ///   <para>异步加载资源，并绑定到指定组件的 <see cref="UnityEngine.GameObject"/> 生命周期。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="loader">资源加载器。</param>
        /// <param name="assetPath">资源路径；必须使用 Assets/... 或 Packages/... 项目路径。</param>
        /// <param name="owner">资源使用者组件。</param>
        /// <param name="ct">取消令牌。</param>
        public static async Task<TObject> LoadAssetAsync<TObject>(
            this ILoader loader,
            string assetPath,
            Component owner,
            CancellationToken ct = default)
            where TObject : UnityEngine.Object
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            ThrowIfOwnerInvalid(owner, nameof(owner));

            using var handle = await loader.LoadAssetAsync<TObject>(assetPath, ct);
            ct.ThrowIfCancellationRequested();
            return GetOrCreateScope(owner).Track(handle);
        }

        /// <summary>
        ///   <para>同步加载资源，并绑定到指定 <see cref="UnityEngine.GameObject"/> 生命周期。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="loader">资源加载器。</param>
        /// <param name="assetPath">资源路径；必须使用 Assets/... 或 Packages/... 项目路径。</param>
        /// <param name="owner">资源使用者 <see cref="UnityEngine.GameObject"/>。</param>
        public static TObject LoadAsset<TObject>(this ILoader loader, string assetPath, GameObject owner)
            where TObject : UnityEngine.Object
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            ThrowIfOwnerInvalid(owner, nameof(owner));

            using var handle = loader.LoadAsset<TObject>(assetPath);
            return GetOrCreateScope(owner).Track(handle);
        }

        /// <summary>
        ///   <para>异步加载资源，并绑定到指定 <see cref="UnityEngine.GameObject"/> 生命周期。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="loader">资源加载器。</param>
        /// <param name="assetPath">资源路径；必须使用 Assets/... 或 Packages/... 项目路径。</param>
        /// <param name="owner">资源使用者 <see cref="UnityEngine.GameObject"/>。</param>
        /// <param name="ct">取消令牌。</param>
        public static async Task<TObject> LoadAssetAsync<TObject>(
            this ILoader loader,
            string assetPath,
            GameObject owner,
            CancellationToken ct = default)
            where TObject : UnityEngine.Object
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            ThrowIfOwnerInvalid(owner, nameof(owner));

            using var handle = await loader.LoadAssetAsync<TObject>(assetPath, ct);
            ct.ThrowIfCancellationRequested();
            return GetOrCreateScope(owner).Track(handle);
        }

        /// <summary>
        ///   <para>获取或创建资源作用域。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        private static AssetLoadScope GetOrCreateScope(Component owner)
        {
            ThrowIfOwnerInvalid(owner, nameof(owner));
            return GetOrCreateScope(owner.gameObject);
        }

        /// <summary>
        ///   <para>获取或创建资源作用域。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        private static AssetLoadScope GetOrCreateScope(GameObject owner)
        {
            ThrowIfOwnerInvalid(owner, nameof(owner));

            var scopeComponent = owner.GetComponent<AssetLoadScopeComponent>();
            if (scopeComponent == null)
            {
                // 从未激活的宿主不会可靠地收到 OnDestroy，不能承诺自动释放。
                if (!owner.activeInHierarchy)
                    throw new InvalidOperationException("Activate the owner before binding asset loads.");
                scopeComponent = owner.AddComponent<AssetLoadScopeComponent>();
                scopeComponent.hideFlags |= HideFlags.HideInInspector;
            }

            return scopeComponent.Scope;
        }

        /// <summary>
        ///   <para>所有者无效时抛出异常。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="paramName">参数名称。</param>
        private static void ThrowIfOwnerInvalid(UnityEngine.Object owner, string paramName)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(paramName, "Asset load owner cannot be null or destroyed.");
            }
        }
    }
}

#endif
