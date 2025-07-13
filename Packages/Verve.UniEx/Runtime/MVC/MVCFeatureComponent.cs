#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.MVC
{
    using Verve;
    using Loader;
    using System;
    using Verve.MVC;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    
    
    /// <summary>
    /// MVC功能
    /// </summary>
    public partial class MVCFeatureComponent : GameFeatureComponent
    {
        private readonly Dictionary<Type, WeakReference<IView>> m_CachedView = 
            new Dictionary<Type, WeakReference<IView>>();

        private Transform m_Root;
        private LoaderFeature m_Loader;

        
        protected override void OnLoad(Verve.IReadOnlyFeatureDependencies dependencies)
        {
            m_Loader = dependencies.Get<LoaderFeature>();
            base.OnLoad(dependencies);
            m_Root = gameObject.transform;
        }
        
        protected override void OnUnload()
        {
            base.OnUnload();
            CloseViewAll();
            m_CachedView.Clear();
        }

        public bool TryOpenView<TView>(bool isCloseAllOther = false, Transform parent = null,
            Action<TView> onOpened = null, params object[] args)
            where TView : class, IView
        {
            var viewType = typeof(TView);
            var info = GetViewInfo(viewType);
            
            if (LoadView(viewType, info.LoaderType, info.ResourcePath, parent) is TView viewInstance)
            {
                if (isCloseAllOther) CloseViewAll();
                viewInstance.OnClosed += UnregisterView;
                viewInstance.Open(args);
                onOpened?.Invoke(viewInstance);
                m_CachedView[viewType] = new WeakReference<IView>(viewInstance);
                return true;
            }
            return false;
        }

        public bool TryOpenView(string resourcePath, Type loaderType, bool isCloseAllOther = false, Transform parent = null, Action<IView> onOpened = null, params object[] args)
        {
            if (LoadView<IView>(loaderType, resourcePath, parent) is IView viewInstance)
            {
                var viewType = viewInstance.GetType();
                if (isCloseAllOther) CloseViewAll();
                viewInstance.OnClosed += UnregisterView;
                viewInstance.Open(args);
                onOpened?.Invoke(viewInstance);
                m_CachedView[viewType] = new WeakReference<IView>(viewInstance);
                return true;
            }
            return false;
        }

        private TView LoadView<TView>(Type loaderType, string path,Transform parent = null) 
            where TView : class, IView
            => (TView)LoadView(typeof(TView), loaderType, path, parent);
        
        private IView LoadView(Type viewType, Type loaderType, string path, Transform parent = null)
        {
            var prefab = m_Loader.LoadAsset<GameObject>(loaderType, path);
            var obj = UnityEngine.Object.Instantiate(prefab, parent ?? m_Root);
            return (IView)obj?.GetComponent(viewType) ?? throw new InvalidCastException($"Prefab at {path} doesn't contain {viewType.Name} component");
        }

        private async Task<TView> LoadViewAsync<TView>(IAssetLoader loader, string path, Transform parent = null)
            where TView : class, IView
            => (TView)await LoadViewAsync(typeof(TView), loader, path, parent);
        
        private async Task<object> LoadViewAsync(Type viewType, IAssetLoader loader, string path, Transform parent = null)
        {
            var prefab = await loader.LoadAssetAsync<GameObject>(path);
            var instance = UnityEngine.Object.Instantiate(prefab, parent ?? m_Root);
            return instance?.GetComponent(viewType) ?? throw new InvalidCastException($"Prefab at {path} doesn't contain {viewType.Name} component");
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

        public void CloseView<TView>() where TView : VerveUniEx.MVC.ViewBase, IView
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
        
        private ViewInfoAttribute GetViewInfo(System.Type viewType)
        {
            return Attribute.GetCustomAttribute(viewType, typeof(ViewInfoAttribute)) as ViewInfoAttribute ?? throw new MissingComponentException($"Missing {nameof(ViewInfoAttribute)} on {viewType.Name}");
        }
        
        private ViewInfoAttribute GetViewInfo<T>()
            => GetViewInfo(typeof(T));
    }
}

#endif