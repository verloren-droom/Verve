#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;

    /// <summary>
    ///   <para>页面调试信息；提供实例状态的只读快照。</para>
    /// </summary>
    public readonly struct UIViewDebugInfo
    {
        /// <summary>
        ///   <para>模块内唯一实例编号，仅用于诊断和编辑器显示。</para>
        /// </summary>
        public readonly long InstanceId;

        /// <summary>
        ///   <para>页面类型。</para>
        /// </summary>
        public readonly Type ViewType;

        /// <summary>
        ///   <para>页面名称。</para>
        /// </summary>
        public readonly string ViewName;

        /// <summary>
        ///   <para>页面 Prefab 资源路径。</para>
        /// </summary>
        public readonly string AssetPath;

        /// <summary>
        ///   <para>页面显示层。</para>
        /// </summary>
        public readonly UILayer Layer;

        /// <summary>
        ///   <para>页面关闭后的缓存策略。</para>
        /// </summary>
        public readonly UIViewCacheMode CacheMode;

        /// <summary>
        ///   <para>页面当前是否打开。</para>
        /// </summary>
        public readonly bool IsOpen;

        /// <summary>
        ///   <para>创建 UI 页面调试信息。</para>
        /// </summary>
        /// <param name="instanceId">实例标识。</param>
        /// <param name="viewType">页面类型。</param>
        /// <param name="viewName">页面名称。</param>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="layer">层级。</param>
        /// <param name="cacheMode">关闭后的缓存模式。</param>
        /// <param name="isOpen">是否打开。</param>
        internal UIViewDebugInfo(
            long instanceId,
            Type viewType,
            string viewName,
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode,
            bool isOpen)
        {
            InstanceId = instanceId;
            ViewType = viewType;
            ViewName = viewName;
            AssetPath = assetPath;
            Layer = layer;
            CacheMode = cacheMode;
            IsOpen = isOpen;
        }
    }
}

#endif
