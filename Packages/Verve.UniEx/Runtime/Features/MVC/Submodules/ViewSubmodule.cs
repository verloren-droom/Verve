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
    using Object = UnityEngine.Object;

    
    /// <summary>
    /// View子模块 - 用于管理View
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(MVCGameFeature), Description = "View子模块")]
    public sealed partial class ViewSubmodule : GameFeatureSubmodule<MVCGameFeatureComponent>
    {
        [Tooltip("View根节点")] private Transform m_ViewRoot;
        [Tooltip("View缓存")] private readonly Dictionary<Type, WeakReference<IView>> m_CachedView = 
            new Dictionary<Type, WeakReference<IView>>();
        [Tooltip("View堆栈")] private readonly Stack<WeakReference<IView>> m_ViewStack = 
            new Stack<WeakReference<IView>>();
        
        internal Stack<IView> GetViewStack() => new Stack<IView>(m_ViewStack.Select(x => x.TryGetTarget(out var view) ? view : null).Where(x => x != null));

        
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
            if (Application.isPlaying)
            {
                if (m_ViewRoot != null)
                {
                    Object.Destroy(m_ViewRoot.gameObject);
                    yield return null;
                }
                m_ViewRoot = Object.Instantiate(Component.ViewRoot);
            }
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            CloseViewAll();
            if (Application.isPlaying && m_ViewRoot != null)
            {
                Object.Destroy(m_ViewRoot.gameObject);
            }
        }

        /// <summary>
        /// 打开视图
        /// </summary>
        /// <param name="resourcePath">资源路径</param>
        /// <param name="loadAssetAsync">资源加载回调</param>
        /// <param name="isCloseAllOther">是否关闭其他页面</param>
        /// <param name="parent">父物体</param>
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
            if (!viewObj.TryGetComponent(out ViewBase viewInstance))
            {
                return false;
            }
            if (m_CachedView.TryGetValue(viewInstance.GetType(), out var weakRef) && 
                weakRef.TryGetTarget(out var existingView))
            {
                existingView.Close();
            }
            if (isCloseAllOther) CloseViewAll();
            viewInstance.OnClosed += UnregisterView;
            ((IView)viewInstance).Open(args);
            onOpened?.Invoke(viewInstance);
            m_CachedView[viewInstance.GetType()] = new WeakReference<IView>(viewInstance);
            m_ViewStack.Push(new WeakReference<IView>(viewInstance));
            return true;
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
        
        /// <summary>
        /// 返回上一个视图
        /// </summary>
        public void GoBackView()
        {
            if (m_ViewStack.Count <= 1) return;
        
            m_ViewStack.Pop();
        
            if (m_ViewStack.Count > 0 && 
                m_ViewStack.Peek().TryGetTarget(out var view))
            {
                view.Close();
            }
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
            var viewsToClose = m_CachedView
                .Where(kvp => kvp.Value.TryGetTarget(out _))
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
    
            m_CachedView.Clear();
            m_ViewStack.Clear();
    
            foreach (var (key, weakRef) in viewsToClose)
            {
                if (!weakRef.TryGetTarget(out var view) || view == null) continue;
                view.Close();
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