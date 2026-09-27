#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    ///   <para>由 UI 模块统一管理的固定显示层；数值同时表示根节点下的绘制顺序。</para>
    /// </summary>
    public enum UILayer : byte
    {
        /// <summary>
        ///   <para>背景。</para>
        /// </summary>
        Background = 0,
        /// <summary>
        ///   <para>主层级。</para>
        /// </summary>
        Main = 1,
        /// <summary>
        ///   <para>弹窗。</para>
        /// </summary>
        Popup = 2,
        /// <summary>
        ///   <para>覆盖层。</para>
        /// </summary>
        Overlay = 3,
    }

    /// <summary>
    ///   <para>关闭视图后的缓存策略，仅针对页面实例。</para>
    /// </summary>
    public enum UIViewCacheMode : byte
    {
        /// <summary>
        ///   <para>关闭后保留实例与资源句柄，由实例级 <see cref="IUIManager.Release(ViewBase)"/> 或模块卸载时回收。</para>
        /// </summary>
        KeepAlive = 0,

        /// <summary>
        ///   <para>关闭后立即销毁实例并释放资源句柄，适合低频且占用较大的页面。</para>
        /// </summary>
        DestroyOnClose = 1,
    }

    /// <summary>
    ///   <para>打开页面时的实例策略；资源配置不决定运行时实例数量。</para>
    /// </summary>
    public enum UIViewOpenMode : byte
    {
        /// <summary>
        ///   <para>按类型复用配置相同的已有实例；配置冲突时须先释放或选择 <see cref="New"/>。</para>
        /// </summary>
        Reuse = 0,

        /// <summary>
        ///   <para>每次调用都创建独立实例。</para>
        /// </summary>
        New = 1,
    }

    /// <summary>
    ///   <para>UI 视图生命周期管理器。</para>
    /// </summary>
    public interface IUIManager : IGameModule
    {
        /// <summary>
        ///   <para>打开页面。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="args">打开参数；无参数页面留空，有参数页面须传入匹配实例。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="openMode">打开模式。</param>
        ViewBase Open(
            Type viewType,
            ViewArgs args = null,
            Transform parent = null,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse);

        /// <summary>
        ///   <para>异步打开页面。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="args">打开参数；无参数页面留空，有参数页面须传入匹配实例。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="openMode">打开模式。</param>
        Task<ViewBase> OpenAsync(
            Type viewType,
            ViewArgs args = null,
            Transform parent = null,
            CancellationToken ct = default,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse);

        /// <summary>
        ///   <para>按资源路径打开页面。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="assetPath">Prefab 资源路径。</param>
        /// <param name="layer">显示层。</param>
        /// <param name="cacheMode">关闭后的缓存模式。</param>
        /// <param name="args">打开参数；无参数页面留空，有参数页面须传入匹配实例。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="openMode">打开模式。</param>
        ViewBase Open(
            Type viewType,
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode,
            ViewArgs args = null,
            Transform parent = null,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse);

        /// <summary>
        ///   <para>按资源路径异步打开页面。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="assetPath">Prefab 资源路径。</param>
        /// <param name="layer">显示层。</param>
        /// <param name="cacheMode">关闭后的缓存模式。</param>
        /// <param name="args">打开参数；无参数页面留空，有参数页面须传入匹配实例。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="openMode">打开模式。</param>
        Task<ViewBase> OpenAsync(
            Type viewType,
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode,
            ViewArgs args = null,
            Transform parent = null,
            CancellationToken ct = default,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse);

        /// <summary>
        ///   <para>取得指定类型的唯一视图。</para>
        /// </summary>
        /// <param name="viewType">视图类型。</param>
        /// <param name="view">找到的视图。</param>
        /// <remarks>包含 <see cref="UIViewCacheMode.KeepAlive"/> 缓存的关闭视图；存在多个实例时抛出歧义异常。</remarks>
        bool TryGetView(Type viewType, out ViewBase view);

        /// <summary>
        ///   <para>将指定类型的全部 <see cref="ViewBase"/> 实例写入列表，包含已关闭但仍缓存的实例。</para>
        /// </summary>
        /// <param name="viewType"><see cref="ViewBase"/> 类型。</param>
        /// <param name="output">接收实例的列表；调用前会清空，必须是可写的 <see cref="ViewBase"/> 列表。</param>
        /// <remarks>必须在 Unity 主线程调用；查询按运行时具体类型匹配。</remarks>
        void GetViews(Type viewType, IList output);

        /// <summary>
        ///   <para>将视图状态写入指定列表。</para>
        /// </summary>
        /// <param name="output">接收结果的列表；调用前会清空。</param>
        void CopyViewDebugInfoTo(List<UIViewDebugInfo> output);

        /// <summary>
        ///   <para>关闭指定类型的唯一视图。</para>
        /// </summary>
        /// <param name="viewType">视图类型。</param>
        /// <remarks>存在多个实例时抛出歧义异常。</remarks>
        bool Close(Type viewType);

        /// <summary>
        ///   <para>关闭指定视图实例。</para>
        /// </summary>
        /// <param name="view">视图实例。</param>
        bool Close(ViewBase view);

        /// <summary>
        ///   <para>释放指定类型的唯一视图。</para>
        /// </summary>
        /// <param name="viewType">视图类型。</param>
        /// <remarks>存在多个实例时抛出歧义异常。</remarks>
        bool Release(Type viewType);

        /// <summary>
        ///   <para>释放指定视图实例。</para>
        /// </summary>
        /// <param name="view">视图实例。</param>
        bool Release(ViewBase view);

        /// <summary>
        ///   <para>关闭全部视图并保留 <see cref="UIViewCacheMode.KeepAlive"/> 缓存。</para>
        /// </summary>
        void CloseAll();

        /// <summary>
        ///   <para>关闭指定层中的全部视图。</para>
        /// </summary>
        /// <param name="layer">显示层。</param>
        void CloseAll(UILayer layer);

        /// <summary>
        ///   <para>释放全部视图和资源。</para>
        /// </summary>
        void ReleaseAll();

        /// <summary>
        ///   <para>释放指定层中的全部视图和资源。</para>
        /// </summary>
        /// <param name="layer">显示层。</param>
        void ReleaseAll(UILayer layer);
    }
}

#endif