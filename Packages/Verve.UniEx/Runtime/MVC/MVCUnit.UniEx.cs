#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.MVC
{
    using System;
    using Verve.MVC;
    using Verve.Unit;
    using System.Linq;
    using UnityEngine;
    using Verve.Loader;
    using System.Threading.Tasks;
    using System.Collections.Generic;


    [Serializable]
    public class MVCUnitConfig
    {
        public Transform ViewRootParent;
    }
    
    
    /// <summary>
    /// MVC 单元
    /// </summary>
    [CustomUnit("MVC", dependencyUnits: typeof(VerveUniEx.Loader.LoaderUnit)), System.Serializable]
    public partial class MVCUnit : UnitBase
    {
        private readonly Dictionary<Type, WeakReference<IView>> m_CachedView = 
            new Dictionary<Type, WeakReference<IView>>();
        
        private MVCUnitConfig m_Config;
        
        private Transform m_ViewRootParent;
        
        protected LoaderUnit m_LoaderUnit;
        
        public Transform Root
        {
            get => m_ViewRootParent;
            set => m_ViewRootParent = value;
        }

        
        protected override void OnPostStartup(UnitRules parent)
        {
            base.OnPostStartup(parent);
            parent.TryGetDependency<LoaderUnit>(out m_LoaderUnit);
        }
        
        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            m_Config = args.Length > 0 && args[0] is MVCUnitConfig ? args[0] as MVCUnitConfig : new MVCUnitConfig();
            m_ViewRootParent = m_Config?.ViewRootParent ?? GameObject.FindObjectOfType<Canvas>()?.transform ?? new GameObject("ViewRoot").AddComponent<Canvas>().transform;
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
            var prefab = m_LoaderUnit.LoadAsset<GameObject>(loaderType, path);
            var obj = UnityEngine.Object.Instantiate(prefab, parent ?? m_ViewRootParent);
            return (IView)obj?.GetComponent(viewType) ?? throw new InvalidCastException($"Prefab at {path} doesn't contain {viewType.Name} component");
        }

        private async Task<TView> LoadViewAsync<TView>(IAssetLoader loader, string path, Transform parent = null)
            where TView : class, IView
            => (TView)await LoadViewAsync(typeof(TView), loader, path, parent);
        
        private async Task<object> LoadViewAsync(Type viewType, IAssetLoader loader, string path, Transform parent = null)
        {
            var prefab = await loader.LoadAssetAsync<GameObject>(path);
            var instance = UnityEngine.Object.Instantiate(prefab, parent ?? m_ViewRootParent);
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