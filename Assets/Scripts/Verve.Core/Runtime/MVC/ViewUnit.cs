namespace Verve.MVC
{
    using Unit;
    using Loader;
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 视图单元
    /// </summary>
    [CustomUnit("View"), System.Serializable]
    public sealed partial class ViewUnit : UnitBase, IDisposable
    {
        public override HashSet<System.Type> DependencyUnits => new HashSet<Type>() { typeof(LoaderUnit) };
#if UNITY_5_3_OR_NEWER
        private Transform m_ViewRootParent;
#endif
        private readonly Dictionary<Type, IView> m_CachedView = new Dictionary<Type, IView>();

        private LoaderUnit m_LoaderUnit;
        
        public override void Startup(UnitRules parent, params object[] args)
        {
            base.Startup(parent, args);
#if UNITY_5_3_OR_NEWER
            m_ViewRootParent = args.Length > 0 && args[0] is Transform ? args[0] as Transform : (GameObject.FindObjectOfType<Canvas>().transform ?? new GameObject("ViewRoot").AddComponent<Canvas>().transform);
#endif
            parent.onInitialized += (_) =>
            {
                parent.TryGetDependency<LoaderUnit>(out m_LoaderUnit);
            };
        }

        public override void Shutdown()
        {
            Dispose();
            base.Shutdown();
        }
        
        public void Dispose()
        {
            
        }

        public bool TryOpen<TView>(
            bool isCloseAllOther = false,
#if UNITY_5_3_OR_NEWER
            Transform parent = null,
#endif
            Action<TView> onOpened = null
        ) where TView : ViewBase, IView
        {
            var viewType = typeof(TView);
            var info = GetViewInfo(viewType);
            
            if (LoadView(viewType, info.LoaderType, info.ResourcePath, parent) is TView viewInstance)
            {
                if (isCloseAllOther) CloseAll();
                viewInstance.Open();
                onOpened?.Invoke(viewInstance);
                return true;
            }
            return false;
        }

        public bool TryOpen(
            string resourcePath,
            Type loaderType,
            bool isCloseAllOther = false,
#if UNITY_5_3_OR_NEWER
            Transform parent = null,
#endif
            Action<ViewBase> onOpened = null
            )
        {
            if (LoadView<ViewBase>(loaderType, resourcePath, parent) is ViewBase viewInstance)
            {
                if (isCloseAllOther) CloseAll();
                viewInstance.Open();
                onOpened?.Invoke(viewInstance);
                return true;
            }
            return false;
        }

        private TView LoadView<TView>(
            Type loaderType, string path
#if UNITY_5_3_OR_NEWER
            ,Transform parent = null
#endif
            ) where TView : IView =>
            (TView)LoadView(typeof(TView), loaderType, path, parent);
        
        private IView LoadView(Type viewType, Type loaderType, string path, Transform parent = null)
        {
            var prefab = m_LoaderUnit.LoadAsset<GameObject>(loaderType, path);
            var instance = UnityEngine.Object.Instantiate(prefab, parent ?? m_ViewRootParent);
            return (IView)instance.GetComponent(viewType) ?? throw new InvalidCastException($"Prefab at {path} doesn't contain {viewType.Name} component");
        }

        private async Task<TView> LoadViewAsync<TView>(IAssetLoader loader, string path,
            Transform parent = null)
            where TView : IView => (TView)await LoadViewAsync(typeof(TView), loader, path, parent);
        
        private async Task<object> LoadViewAsync(Type viewType, IAssetLoader loader, string path, Transform parent = null)
        {
            var prefab = await loader.LoadAssetAsync<GameObject>(path);
            var instance = UnityEngine.Object.Instantiate(prefab, parent ?? m_ViewRootParent);
            return instance.GetComponent(viewType) ?? throw new InvalidCastException($"Prefab at {path} doesn't contain {viewType.Name} component");
        }

        public void Close(Type viewType)
        {
            if (m_CachedView.TryGetValue(viewType, out var view))
            {
                view.Close();
            }
        }

        public void Close<TView>() where TView : ViewBase, IView => Close(typeof(TView));

        public void CloseAll()
        {
            foreach (var view in m_CachedView)
            {
                Close(view.Key);
            }
        }
        
        private ViewInfoAttribute GetViewInfo(System.Type viewType)
        {
            return Attribute.GetCustomAttribute(viewType, typeof(ViewInfoAttribute)) as ViewInfoAttribute ?? throw new MissingComponentException($"Missing {nameof(ViewInfoAttribute)} on {viewType.Name}");
        }
        
        private ViewInfoAttribute GetViewInfo<T>() => GetViewInfo(typeof(T));

    }

}