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
    ///  <para>视图控制器子模块 - 用于管理视图</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(MVCGameFeature), Description = "视图控制器子模块 - 用于管理视图")]
    public sealed partial class ViewController : GameFeatureSubmodule<MVCGameFeatureComponent>
    {
        // [SerializeField, Tooltip("View根节点")] private Transform m_ViewRoot;
        [Tooltip("View缓存")] private readonly Dictionary<Type, WeakReference<IView>> m_CachedView = 
            new Dictionary<Type, WeakReference<IView>>();
        [Tooltip("View堆栈")] private readonly Stack<WeakReference<IView>> m_ViewStack = 
            new Stack<WeakReference<IView>>();


        protected override IEnumerator OnStartup()
        {
            // Assert.IsNotNull(Component.ViewRoot);
            // if (Application.isPlaying)
            // {
            //     if (m_ViewRoot != null)
            //     {
            //         Object.Destroy(m_ViewRoot.gameObject);
            //         yield return null;
            //     }
            //     m_ViewRoot = Object.Instantiate(Component.ViewRoot);
            //     Object.DontDestroyOnLoad(m_ViewRoot.gameObject);
            //     var views = m_ViewRoot.GetComponentsInChildren<ViewBase>();
            //     for (int i = 0; i < views.Length; i++)
            //     {
            //         m_CachedView[views[i].GetType()] = new WeakReference<IView>(views[i]);
            //     }
            // }
            yield break;
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            CloseViewAll();
            // if (Application.isPlaying && m_ViewRoot != null)
            // {
            //     Object.Destroy(m_ViewRoot.gameObject);
            // }
        }

        /// <summary>
        /// 打开视图
        /// </summary>
        /// <param name="viewPrefab">视图预制体</param>
        /// <param name="isCloseAllOther">是否关闭其他页面</param>
        /// <param name="parent">父物体</param>
        /// <param name="onOpened">打开回调</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public void OpenView<T>(
            GameObject viewPrefab,
            bool isCloseAllOther = false,
            Transform parent = null,
            Action<T> onOpened = null,
            params object[] args)
            where T : ViewBase
        {
            var viewObj = Object.Instantiate(viewPrefab, parent);
            if (!viewObj.TryGetComponent(out T viewInstance)) return;
            if (viewInstance.GetActivity() is MonoBehaviour activity && activity.gameObject != null && activity.TryGetComponent(out Transform activityParent))
            {
                viewObj.transform.SetParent(activityParent, false);
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

        /// <summary>
        /// 获取指定类型的视图
        /// </summary>
        public bool TryGetView(Type viewType, out IView view)
        {
            view = null;
            return m_CachedView.TryGetValue(viewType, out var weakRef) && weakRef.TryGetTarget(out view);
        }
       
        /// <summary>
        /// 获取指定类型的视图
        /// </summary>
        public bool TryGetView<T>(out T view)
        {
            if (m_CachedView.TryGetValue(typeof(T), out var weakRef) && weakRef.TryGetTarget(out var target))
            {
                view = (T)target;
                return true;
            }
            view = default;
            return false;
        }
        
        /// <summary>
        /// 关闭指定类型的视图
        /// </summary>
        public void CloseView(Type viewType)
        {
            if (m_CachedView.TryGetValue(viewType, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var view))
                {
                    view.OnClosed -= UnregisterView;
                    view.Close();
                    m_CachedView.Remove(viewType);
                }
            }
        }

        /// <summary>
        /// 关闭指定类型的视图类型
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        public void CloseView<TView>() where TView : ViewBase
            => CloseView(typeof(TView));

        /// <summary>
        /// 关闭所有视图
        /// </summary>
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

                if (view is Object unityObject && !ReferenceEquals(unityObject, null))
                {
                    view.Close();
                }
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

        // private ViewInfoAttribute GetViewInfo(Type viewType)
        // {
        //     return Attribute.GetCustomAttribute(viewType, typeof(ViewInfoAttribute)) as ViewInfoAttribute ?? throw new MissingComponentException($"Missing {nameof(ViewInfoAttribute)} on {viewType.Name}");
        // }
        //
        // private ViewInfoAttribute GetViewInfo<T>()
        //     => GetViewInfo(typeof(T));
    }
}

#endif