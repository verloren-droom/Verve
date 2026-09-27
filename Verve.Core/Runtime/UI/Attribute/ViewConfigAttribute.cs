#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;

    /// <summary>
    ///   <para>页面配置特性；声明资源、显示层和缓存策略。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ViewConfigAttribute : Attribute
    {
        /// <summary>
        ///   <para>资源项目路径，例如 <c>Assets/UI/LoginView.prefab</c>。</para>
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        ///   <para>未显式传入父节点时使用的页面层。</para>
        /// </summary>
        public UILayer Layer { get; }

        /// <summary>
        ///   <para>页面关闭后是否保留实例和资源句柄。</para>
        /// </summary>
        public UIViewCacheMode CacheMode { get; set; } = UIViewCacheMode.KeepAlive;

        /// <summary>
        ///   <para>声明 <see cref="ViewBase"/> 默认资源和显示层。</para>
        /// </summary>
        /// <param name="assetPath"><see cref="ViewBase"/> Prefab 项目路径。</param>
        /// <param name="layer">未显式传入父节点时使用的显示层。</param>
        public ViewConfigAttribute(string assetPath, UILayer layer = UILayer.Main)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("View 资源路径不能为空。", nameof(assetPath));
            }

            if (!Enum.IsDefined(typeof(UILayer), layer))
            {
                throw new ArgumentOutOfRangeException(nameof(layer), layer, "无效的 UI 显示层。");
            }

            AssetPath = Game.PathUtility.Normalize(assetPath);
            Layer = layer;
        }
    }
}

#endif
