#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    ///   <para>UI 管理扩展；提供泛型页面操作。</para>
    /// </summary>
    public static class UIManagerExtensions
    {
        /// <summary>
        ///   <para>按页面配置打开页面。</para>
        /// </summary>
        /// <typeparam name="TView">页面类型。</typeparam>
        /// <param name="self">UI 管理器。</param>
        /// <param name="args">打开参数；无参数页面留空。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="openMode">打开模式。</param>
        public static TView Open<TView>(
            this IUIManager self,
            ViewArgs args = null,
            Transform parent = null,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
            where TView : ViewBase => (TView)self.Open(typeof(TView), args, parent, openMode);

        /// <summary>
        ///   <para>按页面配置异步打开页面。</para>
        /// </summary>
        /// <typeparam name="TView">页面类型。</typeparam>
        /// <param name="self">UI 管理器。</param>
        /// <param name="args">打开参数；无参数页面留空。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="openMode">打开模式。</param>
        public static Task<TView> OpenAsync<TView>(
            this IUIManager self,
            ViewArgs args = null,
            Transform parent = null,
            CancellationToken ct = default,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
            where TView : ViewBase => AwaitOpenAsync<TView>(self.OpenAsync(typeof(TView), args, parent, ct, openMode));

        /// <summary>
        ///   <para>按资源路径打开页面。</para>
        /// </summary>
        /// <typeparam name="TView">页面类型。</typeparam>
        /// <param name="self">UI 管理器。</param>
        /// <param name="assetPath">Prefab 资源路径。</param>
        /// <param name="layer">显示层。</param>
        /// <param name="cacheMode">关闭后的缓存模式。</param>
        /// <param name="args">打开参数；无参数页面留空。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="openMode">打开模式。</param>
        public static TView Open<TView>(
            this IUIManager self,
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode,
            ViewArgs args = null,
            Transform parent = null,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
            where TView : ViewBase => (TView)self.Open(typeof(TView), assetPath, layer, cacheMode, args, parent, openMode);

        /// <summary>
        ///   <para>按资源路径异步打开页面。</para>
        /// </summary>
        /// <typeparam name="TView">页面类型。</typeparam>
        /// <param name="self">UI 管理器。</param>
        /// <param name="assetPath">Prefab 资源路径。</param>
        /// <param name="layer">显示层。</param>
        /// <param name="cacheMode">关闭后的缓存模式。</param>
        /// <param name="args">打开参数；无参数页面留空。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="openMode">打开模式。</param>
        public static Task<TView> OpenAsync<TView>(
            this IUIManager self,
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode,
            ViewArgs args = null,
            Transform parent = null,
            CancellationToken ct = default,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
            where TView : ViewBase => AwaitOpenAsync<TView>(self.OpenAsync(typeof(TView), assetPath, layer, cacheMode, args, parent, ct, openMode));

        /// <summary>
        ///   <para>取得指定类型的唯一页面。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <param name="view">找到的页面。</param>
        /// <remarks>存在多个实例时抛出歧义异常。</remarks>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static bool TryGet<TView>(this IUIManager self, out TView view) where TView : ViewBase
        {
            if (!self.TryGetView(typeof(TView), out var candidate))
            {
                view = null;
                return false;
            }

            view = (TView)candidate;
            return true;
        }

        /// <summary>
        ///   <para>返回指定类型页面当前是否处于打开状态。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static bool IsOpen<TView>(this IUIManager self) where TView : ViewBase => TryGet(self, out TView view) && view.IsOpen;

        /// <summary>
        ///   <para>关闭指定类型页面。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static bool Close<TView>(this IUIManager self) where TView : ViewBase => self.Close(typeof(TView));

        /// <summary>
        ///   <para>关闭指定页面实例。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <param name="view">页面实例。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static bool Close<TView>(this IUIManager self, TView view) where TView : ViewBase => self.Close(view);

        /// <summary>
        ///   <para>立即释放指定类型页面。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static bool Release<TView>(this IUIManager self) where TView : ViewBase => self.Release(typeof(TView));

        /// <summary>
        ///   <para>释放指定页面实例。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <param name="view">页面实例。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static bool Release<TView>(this IUIManager self, TView view) where TView : ViewBase => self.Release(view);

        /// <summary>
        ///   <para>将指定类型的全部 <see cref="ViewBase"/> 实例写入强类型列表，不创建结果数组。</para>
        /// </summary>
        /// <typeparam name="TView"><see cref="ViewBase"/> 类型。</typeparam>
        /// <param name="self">UI 管理器。</param>
        /// <param name="output">接收实例的列表；调用前会清空。</param>
        public static void GetViews<TView>(
            this IUIManager self,
            List<TView> output)
            where TView : ViewBase
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            self.GetViews(typeof(TView), output);
        }

        /// <summary>
        ///   <para>异步打开页面，随后串行执行一次可等待操作。</para>
        /// </summary>
        /// <typeparam name="TView">页面类型。</typeparam>
        /// <param name="self">UI 管理器。</param>
        /// <param name="onAfterOpen">打开完成后的操作；参数从具体页面读取。</param>
        /// <param name="args">打开参数；无参数页面留空。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="openMode">打开模式。</param>
        public static Task<TView> OpenAsync<TView>(
            this IUIManager self,
            Func<TView, CancellationToken, Task> onAfterOpen,
            ViewArgs args = null,
            Transform parent = null,
            CancellationToken ct = default,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
            where TView : ViewBase
        {
            if (onAfterOpen == null) throw new ArgumentNullException(nameof(onAfterOpen));
            return AwaitOpenAsync(self.OpenAsync(typeof(TView), args, parent, ct, openMode), onAfterOpen, ct);
        }

        /// <summary>
        ///   <para>异步打开页面，随后串行执行一次可等待操作。</para>
        /// </summary>
        /// <typeparam name="TView">页面类型。</typeparam>
        /// <param name="self">UI 管理器。</param>
        /// <param name="assetPath">Prefab 资源路径。</param>
        /// <param name="layer">显示层。</param>
        /// <param name="cacheMode">关闭后的缓存模式。</param>
        /// <param name="onAfterOpen">打开完成后的操作；参数从具体页面读取。</param>
        /// <param name="args">打开参数；无参数页面留空。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="openMode">打开模式。</param>
        public static Task<TView> OpenAsync<TView>(
            this IUIManager self,
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode,
            Func<TView, CancellationToken, Task> onAfterOpen,
            ViewArgs args = null,
            Transform parent = null,
            CancellationToken ct = default,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
            where TView : ViewBase
        {
            if (onAfterOpen == null) throw new ArgumentNullException(nameof(onAfterOpen));
            return AwaitOpenAsync(self.OpenAsync(typeof(TView), assetPath, layer, cacheMode, args, parent, ct, openMode), onAfterOpen, ct);
        }

        /// <summary>
        ///   <para>取得强类型打开结果。</para>
        /// </summary>
        /// <typeparam name="TView">页面类型。</typeparam>
        /// <param name="opening">打开任务。</param>
        private static async Task<TView> AwaitOpenAsync<TView>(Task<ViewBase> opening) where TView : ViewBase =>
            (TView)await opening;

        /// <summary>
        ///   <para>等待打开并执行后续操作；与页面关闭、释放互斥。</para>
        /// </summary>
        /// <typeparam name="TView">页面类型。</typeparam>
        /// <param name="opening">打开任务。</param>
        /// <param name="onAfterOpen">打开后的操作。</param>
        /// <param name="ct">取消令牌。</param>
        private static async Task<TView> AwaitOpenAsync<TView>(Task<ViewBase> opening,
            Func<TView, CancellationToken, Task> onAfterOpen, CancellationToken ct) where TView : ViewBase
        {
            var view = (TView)await opening;
            await view.RunAsyncOperation(async () =>
            {
                ct.ThrowIfCancellationRequested();
                await onAfterOpen(view, ct);
                return true;
            }, ct);
            return view;
        }

        /// <summary>
        ///   <para>取得唯一页面实例，执行关闭前操作后关闭页面。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <param name="onBeforeClose">关闭前的操作。</param>
        /// <param name="ct">取消令牌。</param>
        /// <remarks>存在多个同类型实例时会抛出歧义异常，请使用带实例参数的重载。</remarks>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static async Task<bool> CloseAsync<TView>(
            this IUIManager self,
            Func<TView, CancellationToken, Task> onBeforeClose,
            CancellationToken ct = default)
            where TView : ViewBase
        {
            if (onBeforeClose == null) throw new ArgumentNullException(nameof(onBeforeClose));
            if (!self.TryGet<TView>(out var view) || !view.IsOpen)
            {
                return false;
            }

            return await view.RunAsyncOperation(
                () => CloseAsyncImpl(self, view, onBeforeClose, ct), ct);
        }

        /// <summary>
        ///   <para>执行指定实例的关闭前操作，再关闭该实例。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <param name="view">页面实例。</param>
        /// <param name="onBeforeClose">关闭前的操作。</param>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static async Task<bool> CloseAsync<TView>(
            this IUIManager self,
            TView view,
            Func<TView, CancellationToken, Task> onBeforeClose,
            CancellationToken ct = default)
            where TView : ViewBase
        {
            if (view == null) return false;
            if (onBeforeClose == null) throw new ArgumentNullException(nameof(onBeforeClose));
            return await view.RunAsyncOperation(
                () => CloseAsyncImpl(self, view, onBeforeClose, ct), ct);
        }

        /// <summary>
        ///   <para>取得唯一页面实例，执行释放前操作后释放页面。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <param name="onBeforeRelease">释放前的操作。</param>
        /// <param name="ct">取消令牌。</param>
        /// <remarks>存在多个同类型实例时会抛出歧义异常，请使用带实例参数的重载。</remarks>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static async Task<bool> ReleaseAsync<TView>(
            this IUIManager self,
            Func<TView, CancellationToken, Task> onBeforeRelease,
            CancellationToken ct = default)
            where TView : ViewBase
        {
            if (onBeforeRelease == null) throw new ArgumentNullException(nameof(onBeforeRelease));
            if (!self.TryGet<TView>(out var view))
            {
                return false;
            }

            return await view.RunAsyncOperation(
                () => ReleaseAsyncImpl(self, view, onBeforeRelease, ct), ct);
        }

        /// <summary>
        ///   <para>执行指定实例的释放前操作，再释放该实例。</para>
        /// </summary>
        /// <param name="self">UI 管理器。</param>
        /// <param name="view">页面实例。</param>
        /// <param name="onBeforeRelease">释放前的操作。</param>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        public static async Task<bool> ReleaseAsync<TView>(
            this IUIManager self,
            TView view,
            Func<TView, CancellationToken, Task> onBeforeRelease,
            CancellationToken ct = default)
            where TView : ViewBase
        {
            if (view == null) return false;
            if (onBeforeRelease == null) throw new ArgumentNullException(nameof(onBeforeRelease));
            return await view.RunAsyncOperation(
                () => ReleaseAsyncImpl(self, view, onBeforeRelease, ct), ct);
        }

        /// <summary>
        ///   <para>异步关闭。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="view">页面。</param>
        /// <param name="callback">回调。</param>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        private static async Task<bool> CloseAsyncImpl<TView>(
            IUIManager self,
            TView view,
            Func<TView, CancellationToken, Task> callback,
            CancellationToken ct)
            where TView : ViewBase
        {
            if (!view.IsOpen)
            {
                return false;
            }

            ct.ThrowIfCancellationRequested();
            await callback(view, ct);
            return view.IsOpen && self.Close(view);
        }

        /// <summary>
        ///   <para>异步释放对象。</para>
        /// </summary>
        /// <param name="self">目标对象。</param>
        /// <param name="view">页面。</param>
        /// <param name="callback">回调。</param>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="TView">页面类型。</typeparam>
        private static async Task<bool> ReleaseAsyncImpl<TView>(
            IUIManager self,
            TView view,
            Func<TView, CancellationToken, Task> callback,
            CancellationToken ct)
            where TView : ViewBase
        {
            if (view.IsReleased)
            {
                return false;
            }

            ct.ThrowIfCancellationRequested();
            await callback(view, ct);
            return !view.IsReleased && self.Release(view);
        }
    }
}

#endif
