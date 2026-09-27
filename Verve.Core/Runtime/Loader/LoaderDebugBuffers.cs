#if (DEBUG || UNITY_EDITOR) && UNITY_5_3_OR_NEWER

namespace Verve
{
    /// <summary>
    ///   <para>加载器资源调试缓存。</para>
    /// </summary>
    internal readonly struct LoaderDebugAssetBuffer
    {
        /// <summary>
        ///   <para>加载器使用的项目资源路径。</para>
        /// </summary>
        public readonly string path;
        /// <summary>
        ///   <para>资源实际类型。</para>
        /// </summary>
        public readonly string assetType;
        /// <summary>
        ///   <para>Unity 实例编号。</para>
        /// </summary>
        public readonly int instanceId;
        /// <summary>
        ///   <para>当前引用计数。</para>
        /// </summary>
        public readonly int refCount;
        /// <summary>
        ///   <para>资源实例是否仍然有效。</para>
        /// </summary>
        public readonly bool alive;

        /// <summary>
        ///   <para>创建加载器调试资源缓冲区。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        /// <param name="assetType">资源类型。</param>
        /// <param name="instanceId">实例标识。</param>
        /// <param name="refCount">引用数量。</param>
        /// <param name="alive">是否仍然存活。</param>
        public LoaderDebugAssetBuffer(
            string path,
            string assetType,
            int instanceId,
            int refCount,
            bool alive)
        {
            this.path = path;
            this.assetType = assetType;
            this.instanceId = instanceId;
            this.refCount = refCount;
            this.alive = alive;
        }
    }

    /// <summary>
    ///   <para>加载器 <see cref="UnityEngine.AssetBundle"/> 调试缓存。</para>
    /// </summary>
    internal readonly struct LoaderDebugBundleBuffer
    {
        /// <summary>
        ///   <para>内部 <see cref="UnityEngine.AssetBundle"/> 名称，仅用于诊断，不作为外部加载 ID。</para>
        /// </summary>
        public readonly string name;
        /// <summary>
        ///   <para><see cref="UnityEngine.AssetBundle"/> 文件路径。</para>
        /// </summary>
        public readonly string path;
        /// <summary>
        ///   <para>当前引用计数。</para>
        /// </summary>
        public readonly int refCount;
        /// <summary>
        ///   <para><see cref="UnityEngine.AssetBundle"/> 实例是否仍然有效。</para>
        /// </summary>
        public readonly bool loaded;

        /// <summary>
        ///   <para>创建加载器调试资源包缓冲区。</para>
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="path">路径。</param>
        /// <param name="refCount">引用数量。</param>
        /// <param name="loaded">已加载。</param>
        public LoaderDebugBundleBuffer(string name, string path, int refCount, bool loaded)
        {
            this.name = name;
            this.path = path;
            this.refCount = refCount;
            this.loaded = loaded;
        }
    }

    /// <summary>
    ///   <para>加载器场景调试缓存。</para>
    /// </summary>
    internal readonly struct LoaderDebugSceneBuffer
    {
        /// <summary>
        ///   <para>加载器使用的项目场景路径。</para>
        /// </summary>
        public readonly string path;
        /// <summary>
        ///   <para>Unity 场景名称。</para>
        /// </summary>
        public readonly string name;
        /// <summary>
        ///   <para>Unity 当前加载场景路径。</para>
        /// </summary>
        public readonly string loadedPath;
        /// <summary>
        ///   <para>Build Settings 中的场景索引。</para>
        /// </summary>
        public readonly int buildIndex;
        /// <summary>
        ///   <para>当前引用计数。</para>
        /// </summary>
        public readonly int refCount;
        /// <summary>
        ///   <para>场景实例是否有效。</para>
        /// </summary>
        public readonly bool valid;
        /// <summary>
        ///   <para>场景是否已加载并激活。</para>
        /// </summary>
        public readonly bool loaded;

        /// <summary>
        ///   <para>创建加载器调试场景缓冲区。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        /// <param name="name">名称。</param>
        /// <param name="loadedPath">已加载路径。</param>
        /// <param name="buildIndex">构建索引。</param>
        /// <param name="refCount">引用数量。</param>
        /// <param name="valid">是否有效。</param>
        /// <param name="loaded">已加载。</param>
        public LoaderDebugSceneBuffer(
            string path,
            string name,
            string loadedPath,
            int buildIndex,
            int refCount,
            bool valid,
            bool loaded)
        {
            this.path = path;
            this.name = name;
            this.loadedPath = loadedPath;
            this.buildIndex = buildIndex;
            this.refCount = refCount;
            this.valid = valid;
            this.loaded = loaded;
        }
    }
}

#endif
