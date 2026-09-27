#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;

    /// <summary>
    ///   <para>资源包目录；构建时记录可加载资源，运行时无需扫描全部资源包。</para>
    /// </summary>
    [Serializable]
    internal sealed class AssetBundleCatalog
    {
        /// <summary>
        ///   <para>目录文件名。</para>
        /// </summary>
        internal const string FileName = "verve-bundles.json";
        /// <summary>
        ///   <para>资源包条目。</para>
        /// </summary>
        public Bundle[] bundles;

        /// <summary>
        ///   <para>资源包条目；记录包名及其项目资源路径。</para>
        /// </summary>
        [Serializable]
        internal sealed class Bundle
        {
            /// <summary>
            ///   <para>资源包名称。</para>
            /// </summary>
            public string name;
            /// <summary>
            ///   <para>项目资源路径。</para>
            /// </summary>
            public string[] assets;
        }
    }
}

#endif
