#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.MVC
{
    using System;
    using Verve.MVC;
    using System.Linq;
    using UnityEngine;
    using System.Collections;
    using UnityEngine.Assertions;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    
    /// <summary>
    /// View子模块 - 用于管理View
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(MVCGameFeature), Description = "View子模块")]
    public sealed partial class ViewSubmodule : GameFeatureSubmodule<MVCGameFeatureComponent>
    {
        private Transform m_ViewRoot;
        private readonly Dictionary<Type, WeakReference<IView>> m_CachedView = 
            new Dictionary<Type, WeakReference<IView>>();
        
        
        /// <summary>
        /// 异步资源加载委托
        /// </summary>
        public delegate Task<GameObject> LoadAssetAsync(string path);
        /// <summary>
        /// 资源加载委托
        /// </summary>
        public delegate GameObject LoadAsset(string path);

        protected override IEnumerator OnStartup()
        {
            Assert.IsNotNull(Component.ViewRoot);
            if (UnityEngine.Application.isPlaying)
            {
                m_ViewRoot ??= UnityEngine.Object.Instantiate(Component.ViewRoot);
            }
            yield return null;
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            CloseViewAll();
            m_CachedView?.Clear();
        }

        /// <summary>
        /// 打开视图
        /// </summary>
        /// <param name="resourcePath">资源路径</param>
        /// <param name="loadAssetAsync">资源加载回调</param>
        /// <param name="isCloseAllOther">是否关闭其他页面</param>
        /// <param name="parent">夫物体</param>
        /// <param name="onOpened">打开回调</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public async Task<bool> OpenView(
            string resourcePath,
            LoadAssetAsync loadAssetAsync,
            bool isCloseAllOther = false,
            Transform parent = null,
            Action<ViewBase> onOpened = null,
            params object[] args)
        {
            if (string.IsNullOrEmpty(resourcePath)) return false;
            
            var viewObj = UnityEngine.Object.Instantiate(await loadAssetAsync(resourcePath), parent ?? m_ViewRoot);
            if (viewObj.TryGetComponent(out ViewBase viewInstance))
            {
                if (isCloseAllOther) CloseViewAll();
                viewInstance.OnClosed += UnregisterView;
                ((IView)viewInstance).Open(args);
                onOpened?.Invoke(viewInstance);
                m_CachedView[viewInstance.GetType()] = new WeakReference<IView>(viewInstance);
                return true;
            }
            return false;
        }

        public async Task<bool> OpenView<TView>(
            LoadAssetAsync loadAssetAsync,
            bool isCloseAllOther = false,
            Transform parent = null,
            Action<ViewBase> onOpened = null,
            params object[] args)
            where TView : ViewBase
        {
            var viewType = typeof(TView);
            var info = GetViewInfo(viewType);
            return await OpenView(info.ResourcePath, loadAssetAsync, isCloseAllOther, parent, onOpened, args);
        }

        private void UnregisterView(IView view)
        {
            var viewType = view.GetType();
            if (m_CachedView.TryGetValue(viewType, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var cachedView) && cachedView == view)
                {
                    m_CachedView.Remove(viewType);
                }
            }
        }

        public void CloseView(Type viewType)
        {
            if (m_CachedView.TryGetValue(viewType, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var view))
                {
                    view?.Close();
                    m_CachedView.Remove(viewType);
                }
            }
        }

        /// <summary>
        /// 关闭指定类型的View
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        public void CloseView<TView>() where TView : ViewBase
            => CloseView(typeof(TView));

        public void CloseViewAll()
        {
            foreach (var kvp in m_CachedView.ToArray())
            {
                if (kvp.Value.TryGetTarget(out var view))
                {
                    view?.Close();
                }
                m_CachedView.Remove(kvp.Key);
            }
        }
        
        private ViewInfoAttribute GetViewInfo(Type viewType)
        {
            return Attribute.GetCustomAttribute(viewType, typeof(ViewInfoAttribute)) as ViewInfoAttribute ?? throw new MissingComponentException($"Missing {nameof(ViewInfoAttribute)} on {viewType.Name}");
        }
        
        private ViewInfoAttribute GetViewInfo<T>()
            => GetViewInfo(typeof(T));
    }
}

#endif