namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Text.RegularExpressions;
    using System.Runtime.CompilerServices;
    
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
#endif
    
    #region Model
    
    /// <summary>
    ///   <para>MVC模型接口（用于存储和管理共享数据）</para>
    /// </summary>
    public interface IModel : IDisposable
    {
        /// <summary>
        ///   <para>初始化模型</para>
        /// </summary>
        void Initialize();
    }

    /// <summary>
    ///   <para>MVC模型基类</para>
    /// </summary>
    [Serializable]
    public abstract class ModelBase : IModel
    {
        void IModel.Initialize() => OnInitialized();
        void IDisposable.Dispose() => OnDisposed();

        /// <summary>
        ///   <para>模型初始化时的回调</para>
        /// </summary>
        protected virtual void OnInitialized() { }
        /// <summary>
        ///   <para>模型释放时的回调</para>
        /// </summary>
        protected virtual void OnDisposed() { }
    }

    #endregion
    
    #region View

    /// <summary>
    ///   <para>MVC视图接口（定义视图的基本行为和生命周期）</para>
    /// </summary>
    public interface IView : IBelongToActivity
    {
        /// <summary>
        ///   <para>视图名称</para>
        /// </summary>
        string ViewName { get; }
        /// <summary>
        ///   <para>视图打开事件</para>
        /// </summary>
        event Action<IView> OnOpened;
        /// <summary>
        ///   <para>视图关闭事件</para>
        /// </summary>
        event Action<IView> OnClosed;
        /// <summary>
        ///   <para>打开视图</para>
        /// </summary>
        /// <param name="args">传递给视图的参数</param>
        void Open(params object[] args);
        /// <summary>
        ///   <para>关闭视图</para>
        /// </summary>
        void Close();
    }

#if UNITY_5_3_OR_NEWER
    /// <summary>
    ///   <para>MVC视图基类（提供视图生命周期管理和UI事件处理）</para>
    /// </summary>
    public abstract class ViewBase : MonoBehaviour, IView
    {
        [SerializeField, Tooltip("视图名称"), ReadOnly] private string m_ViewName;
        private bool m_IsOpened;
        private int m_OpenSequence;
        private static int s_SequenceCounter;
        
        public virtual string ViewName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_ViewName ??= GetCachedViewName(gameObject.name);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected set => m_ViewName = value;
        }

        public event Action<IView> OnOpened;
        public event Action<IView> OnClosed;
        
        public abstract IActivity GetActivity();

        /// <summary>
        ///   <para>获取视图打开序列号（用于追踪视图的打开顺序）</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetOpenSequence() => m_OpenSequence;

        /// <summary>
        ///   <para>视图打开前的回调</para>
        /// </summary>
        /// <param name="args">传递给视图的参数</param>
        protected virtual void OnOpening(params object[] args) { }

        /// <summary>
        ///   <para>视图关闭前的回调</para>
        /// </summary>
        protected virtual void OnClosing() { }

        private void OnEnable()
        {
            if (!m_IsOpened)
            {
                ((IView)this).Open();
            }
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(m_ViewName))
            {
                m_ViewName = gameObject.name;
            }
        }

        void IView.Open(params object[] args)
        {
            if (m_IsOpened) return;
            m_IsOpened = true;
            m_OpenSequence = Interlocked.Increment(ref s_SequenceCounter);
            gameObject.SetActive(true);
            OnOpening(args);
            OnOpened?.Invoke(this);
        }

        public void Close()
        {
            if (!this || !gameObject) return;

            OnClosing();
            OnClosed?.Invoke(this);
            gameObject.SetActive(false);
            m_IsOpened = false;
            ClearEvents();
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        ///   <para>清理所有事件监听</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearEvents()
        {
            OnOpened = null;
            OnClosed = null;
        }

        /// <summary>
        ///   <para>添加UI事件监听</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        /// <param name="type">事件类型</param>
        /// <param name="callback">回调方法</param>
        protected void AddEventTrigger(EventTrigger eventTrigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            if (eventTrigger == null || callback == null) return;
            
            var triggers = eventTrigger.triggers;
            EventTrigger.Entry entry = null;
            
            for (int i = 0; i < triggers.Count; i++)
            {
                if (triggers[i].eventID == type)
                {
                    entry = triggers[i];
                    break;
                }
            }

            if (entry == null)
            {
                entry = new EventTrigger.Entry { eventID = type };
                triggers.Add(entry);
            }

            entry.callback.AddListener(callback);
        }

        /// <summary>
        ///   <para>为UI图形元素添加事件监听</para>
        /// </summary>
        /// <param name="graphic">图形元素</param>
        /// <param name="type">事件类型</param>
        /// <param name="callback">回调方法</param>
        protected void AddEventTrigger(MaskableGraphic graphic, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            if (graphic == null) return;
            var eventTrigger = graphic.gameObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = graphic.gameObject.AddComponent<EventTrigger>();
            }
            AddEventTrigger(eventTrigger, type, callback);
        }

        /// <summary>
        ///   <para>批量添加多个事件类型</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        /// <param name="events">事件列表</param>
        public void AddEventTriggerRange(
            EventTrigger eventTrigger,
            params (EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)[] events)
        {
            if (eventTrigger == null || events == null) return;
            
            for (int i = 0; i < events.Length; i++)
            {
                AddEventTrigger(eventTrigger, events[i].type, events[i].callback);
            }
        }

        /// <summary>
        ///   <para>移除指定类型的所有事件监听</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        /// <param name="type">事件类型</param>
        protected void RemoveEventTrigger(EventTrigger eventTrigger, EventTriggerType type)
        {
            if (eventTrigger == null) return;
            eventTrigger.triggers.RemoveAll(entry => entry.eventID == type);
        }

        /// <summary>
        ///   <para>移除指定类型的特定回调</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        /// <param name="type">事件类型</param>
        /// <param name="callback">回调方法</param>
        protected void RemoveEventTrigger(EventTrigger eventTrigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            if (eventTrigger == null || callback == null) return;

            var triggers = eventTrigger.triggers;
            for (int i = 0; i < triggers.Count; i++)
            {
                if (triggers[i].eventID == type)
                {
                    triggers[i].callback.RemoveListener(callback);
                }
            }
        }

        /// <summary>
        ///   <para>从UI图形元素移除事件监听</para>
        /// </summary>
        /// <param name="graphic">图形元素</param>
        /// <param name="type">事件类型</param>
        /// <param name="callback">回调方法</param>
        protected void RemoveEventTrigger(MaskableGraphic graphic, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            if (graphic == null || !graphic.TryGetComponent<EventTrigger>(out var eventTrigger)) return;

            if (callback == null)
            {
                RemoveEventTrigger(eventTrigger, type);
            }
            else
            {
                RemoveEventTrigger(eventTrigger, type, callback);
            }
        }

        /// <summary>
        ///   <para>清除事件触发器的所有监听</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        public void RemoveAllTriggers(EventTrigger eventTrigger)
        {
            if (eventTrigger == null) return;

            var triggers = eventTrigger.triggers;
            for (int i = 0; i < triggers.Count; i++)
            {
                triggers[i].callback.RemoveAllListeners();
            }
            triggers.Clear();
        }

        /// <summary>
        ///   <para>清除UI图形元素的所有事件监听</para>
        /// </summary>
        /// <param name="graphic">图形元素</param>
        public void RemoveAllTriggers(MaskableGraphic graphic)
        {
            if (graphic == null || !graphic.TryGetComponent<EventTrigger>(out var eventTrigger)) return;

            var triggers = eventTrigger.triggers;
            for (int i = 0; i < triggers.Count; i++)
            {
                triggers[i].callback.RemoveAllListeners();
            }
            triggers.Clear();
        }

        /// <summary>
        ///   <para>视图名称缓存</para>
        /// </summary>
        private static readonly Dictionary<string, string> s_ViewNameCache = new Dictionary<string, string>();
        private static readonly object s_ViewNameCacheLock = new object();

        /// <summary>
        ///   <para>获取缓存的视图名称</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetCachedViewName(string gameObjectName)
        {
            if (string.IsNullOrEmpty(gameObjectName)) return string.Empty;

            lock (s_ViewNameCacheLock)
            {
                if (s_ViewNameCache.TryGetValue(gameObjectName, out var cachedName))
                {
                    return cachedName;
                }

                var viewName = Regex.Replace(gameObjectName, @"View$", string.Empty, RegexOptions.IgnoreCase);
                s_ViewNameCache[gameObjectName] = viewName;
                return viewName;
            }
        }
    }
#endif

    #endregion
    
    #region Controller

    /// <summary>
    ///   <para>MVC控制器接口（处理用户输入和业务逻辑）</para>
    /// </summary>
    public interface IController : IBelongToActivity { }

    /// <summary>
    ///   <para>MVC控制器基类</para>
    /// </summary>
    [Serializable]
    public abstract class ControllerBase : IController
    {
        /// <summary>
        ///   <para>获取所属的Activity</para>
        /// </summary>
        public abstract IActivity GetActivity();
    }

    /// <summary>
    ///   <para>MVC控制器扩展方法</para>
    /// </summary>
    public static class ControllerExtension
    {
        /// <summary>
        ///   <para>获取指定类型的模型</para>
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        public static T GetModel<T>(this IController self)
            where T : class, IModel, new()
            => self.GetActivity()?.GetModel<T>();

        /// <summary>
        ///   <para>执行指定类型的命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        public static void ExecuteCommand<TCommand>(this IController self)
            where TCommand : class, ICommand, new()
            => self.GetActivity()?.ExecuteCommand<TCommand>();

        /// <summary>
        ///   <para>撤销指定类型的命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        public static void UndoCommand<TCommand>(this IController self)
            where TCommand : class, ICommand, new()
            => self.GetActivity()?.UndoCommand<TCommand>();
    }

    #endregion
    
    #region Model

    /// <summary>
    ///   <para>MVC模型扩展方法</para>
    /// </summary>
    public static class ModelExtension
    {
        /// <summary>
        ///   <para>获取所属的Activity</para>
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        public static IActivity GetActivity<T>(this T self)
            where T : class, IModel
        {
            if (self is IBelongToActivity belongToActivity)
            {
                return belongToActivity.GetActivity();
            }
            return null;
        }
    }

    #endregion
    
    #region Activity

    /// <summary>
    ///   <para>MVC活动接口（管理MVC组件的生命周期和交互）</para>
    /// </summary>
    public interface IActivity : IDisposable
    {
        /// <summary>
        ///   <para>添加模型</para>
        /// </summary>
        /// <typeparam name="TModel">模型类型</typeparam>
        void AddModel<TModel>() where TModel : class, IModel, new();
        /// <summary>
        ///   <para>添加命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        void AddCommand<TCommand>() where TCommand : class, ICommand, new();
        /// <summary>
        ///   <para>获取模型</para>
        /// </summary>
        /// <typeparam name="TModel">模型类型</typeparam>
        TModel GetModel<TModel>() where TModel : class, IModel, new();
        /// <summary>
        ///   <para>执行命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        void ExecuteCommand<TCommand>() where TCommand : class, ICommand, new();
        /// <summary>
        ///   <para>撤销命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        void UndoCommand<TCommand>() where TCommand : class, ICommand, new();
    }

    /// <summary>
    ///   <para>属于Activity的接口（用于获取所属的Activity实例）</para>
    /// </summary>
    public interface IBelongToActivity
    {
        /// <summary>
        ///   <para>获取所属的Activity</para>
        /// </summary>
        IActivity GetActivity();
    }

    /// <summary>
    ///   <para>MVC活动基类（管理模型、命令和视图的生命周期）</para>
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [RequireComponent(typeof(Canvas))]
#endif
    public abstract class ActivityBase :
#if UNITY_5_3_OR_NEWER
        ComponentInstanceBase<ActivityBase>,
#endif
        IActivity
    {
        /// <summary>
        ///   <para字典</para>
        /// </summary>
        private readonly ConcurrentDictionary<Type, IModel> m_Models = new ConcurrentDictionary<Type, IModel>();

        /// <summary>
        ///   <para>命令字典</para>
        /// </summary>
        private readonly ConcurrentDictionary<Type, ICommand> m_Commands = new ConcurrentDictionary<Type, ICommand>();

        /// <summary>
        ///   <para>模型工厂缓存</para>
        /// </summary>
        private readonly ConcurrentDictionary<Type, Func<IModel>> m_ModelFactories = new ConcurrentDictionary<Type, Func<IModel>>();

        /// <summary>
        ///   <para>命令工厂缓存</para>
        /// </summary>
        private readonly ConcurrentDictionary<Type, Func<ICommand>> m_CommandFactories = new ConcurrentDictionary<Type, Func<ICommand>>();

        /// <summary>
        ///   <para>释放状态标志</para>
        /// </summary>
        private int m_IsDisposed;

        /// <summary>
        ///   <para>添加指定类型的模型</para>
        /// </summary>
        /// <param name="type">模型类型</param>
        public void AddModel(Type type)
        {
            if (type == null || !typeof(IModel).IsAssignableFrom(type)) return;
            if (m_IsDisposed != 0) return;

            var factory = GetOrCreateModelFactory(type);
            m_Models.GetOrAdd(type, _ => 
            {
                var model = factory();
                model.Initialize();
                return model;
            });
        }

        public void AddModel<TModel>()
            where TModel : class, IModel, new()
            => AddModel(typeof(TModel));

        /// <summary>
        ///   <para>获取模型</para>
        /// </summary>
        /// <typeparam name="TModel">模型类型</typeparam>
        public IModel GetModel(Type type)
        {
            if (type == null || !typeof(IModel).IsAssignableFrom(type)) return null;
            return m_Models.TryGetValue(type, out var model) ? model : null;
        }
        
        public TModel GetModel<TModel>()
            where TModel : class, IModel, new()
            => (TModel)GetModel(typeof(TModel));

        /// <summary>
        ///   <para>添加命令</para>
        /// </summary>
        /// <param name="type">命令类型</param>
        public void AddCommand(Type type)
        {
            if (type == null || !typeof(ICommand).IsAssignableFrom(type)) return;
            if (m_IsDisposed != 0) return;

            var factory = GetOrCreateCommandFactory(type);
            m_Commands.GetOrAdd(type, _ => factory());
        }
        
        public void AddCommand<TCommand>()
            where TCommand : class, ICommand, new()
            => AddCommand(typeof(TCommand));

        /// <summary>
        ///   <para>执行命令</para>
        /// </summary>
        /// <param name="type">命令类型</param>
        public void ExecuteCommand(Type type)
        {
            if (type == null || !typeof(ICommand).IsAssignableFrom(type)) return;
            if (m_IsDisposed != 0) return;

            if (m_Commands.TryGetValue(type, out var command))
            {
                command.Execute();
            }
        }

        public void ExecuteCommand<TCommand>()
            where TCommand : class, ICommand, new()
            => ExecuteCommand(typeof(TCommand));

        /// <summary>
        ///   <para>撤销命令</para>
        /// </summary>
        /// <param name="type">命令类型</param>
        public void UndoCommand(Type type)
        {
            if (type == null || !typeof(ICommand).IsAssignableFrom(type)) return;
            if (m_IsDisposed != 0) return;

            if (m_Commands.TryGetValue(type, out var command))
            {
                command.Undo();
            }
        }

        public void UndoCommand<TCommand>()
            where TCommand : class, ICommand, new()
            => UndoCommand(typeof(TCommand));

        /// <summary>
        ///   <para>获取或创建模型工厂</para>
        /// </summary>
        /// <param name="type">模型类型</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<IModel> GetOrCreateModelFactory(Type type)
        {
            return m_ModelFactories.GetOrAdd(type, t => 
            {
                var constructor = t.GetConstructor(Type.EmptyTypes);
                if (constructor == null) throw new InvalidOperationException($"Type {t.FullName} must have a parameterless constructor");
                return (Func<IModel>)System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.New(constructor)).Compile();
            });
        }

        /// <summary>
        ///   <para>获取或创建命令工厂</para>
        /// </summary>
        /// <param name="type">命令类型</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<ICommand> GetOrCreateCommandFactory(Type type)
        {
            return m_CommandFactories.GetOrAdd(type, t => 
            {
                var constructor = t.GetConstructor(Type.EmptyTypes);
                if (constructor == null) throw new InvalidOperationException($"Type {t.FullName} must have a parameterless constructor");
                return (Func<ICommand>)System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.New(constructor)).Compile();
            });
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_IsDisposed, 1) != 0) return;

            foreach (var model in m_Models.Values)
            {
                try
                {
                    model.Dispose();
                }
                catch { }
            }
            m_Models.Clear();
            m_Commands.Clear();
            m_ModelFactories.Clear();
            m_CommandFactories.Clear();

#if UNITY_5_3_OR_NEWER
            DisposeViews();
#endif
        }

#if UNITY_5_3_OR_NEWER

        /// <summary>
        ///   <para>缓存的视图字典</para>
        /// </summary>
        private readonly ConcurrentDictionary<Type, ViewReference> m_CachedViews = new ConcurrentDictionary<Type, ViewReference>();

        /// <summary>
        ///   <para>视图栈</para>
        /// </summary>
        private readonly ConcurrentStack<ViewReference> m_ViewStack = new ConcurrentStack<ViewReference>();

        /// <summary>
        ///   <para>打开视图</para>
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <param name="viewPrefab">视图预制体</param>
        /// <param name="isCloseAllOther">是否关闭其他视图</param>
        /// <param name="onOpened">打开回调</param>
        /// <param name="args">传递参数</param>
        public void OpenView<T>(
            GameObject viewPrefab,
            bool isCloseAllOther = false,
            Action<T> onOpened = null,
            params object[] args)
            where T : ViewBase
        {
            if (viewPrefab == null) return;
            if (m_IsDisposed != 0) return;

            var viewObj = Instantiate(viewPrefab, transform);
            if (viewObj == null || !viewObj.TryGetComponent(out T viewInstance)) return;

            if (isCloseAllOther)
            {
                CloseViewAll();
            }

            var viewType = viewInstance.GetType();
            var viewRef = new ViewReference(viewInstance, viewObj, UnregisterView);
            
            if (m_CachedViews.TryRemove(viewType, out var oldRef))
            {
                oldRef.Close();
            }

            viewInstance.OnClosed += UnregisterView;
            ((IView)viewInstance).Open(args);
            onOpened?.Invoke(viewInstance);
            
            m_CachedViews[viewType] = viewRef;
            m_ViewStack.Push(viewRef);
        }

        /// <summary>
        ///   <para>返回上一个视图</para>
        /// </summary>
        public void GoBackView()
        {
            if (m_ViewStack.TryPop(out var currentRef))
            {
                currentRef.Close();
            }

            if (m_ViewStack.TryPeek(out var prevRef))
            {
                prevRef.GetView()?.Open();
            }
        }

        /// <summary>
        ///   <para>尝试获取指定类型的视图</para>
        /// </summary>
        /// <param name="viewType">视图类型</param>
        /// <param name="view">视图实例</param>
        public bool TryGetView(Type viewType, out IView view)
        {
            view = null;
            if (m_CachedViews.TryGetValue(viewType, out var viewRef))
            {
                view = viewRef.GetView();
                return view != null;
            }
            return false;
        }

        /// <summary>
        ///   <para>尝试获取指定类型的视图</para>
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <param name="view">视图实例</param>
        public bool TryGetView<T>(out T view)
            where T : ViewBase
        {
            if (m_CachedViews.TryGetValue(typeof(T), out var viewRef))
            {
                view = viewRef.GetView() as T;
                return view != null;
            }
            view = default;
            return false;
        }

        /// <summary>
        ///   <para>检查指定类型的视图是否已打开</para>
        /// </summary>
        /// <param name="viewType">视图类型</param>
        public bool IsViewOpened(Type viewType)
        {
            if (m_CachedViews.TryGetValue(viewType, out var viewRef))
            {
                var view = viewRef.GetView();
                return view != null;
            }
            return false;
        }

        /// <summary>
        ///   <para>检查指定类型的视图是否已打开</para>
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        public bool IsViewOpened<T>()
            where T : ViewBase
            => IsViewOpened(typeof(T));

        /// <summary>
        ///   <para>关闭指定类型的视图</para>
        /// </summary>
        /// <param name="viewType">视图类型</param>
        public void CloseView(Type viewType)
        {
            if (m_CachedViews.TryRemove(viewType, out var viewRef))
            {
                viewRef.Close();
            }
        }

        /// <summary>
        ///   <para>关闭指定类型的视图</para>
        /// </summary>
        /// <typeparam name="TView">视图类型</typeparam>
        public void CloseView<TView>()
            where TView : ViewBase
            => CloseView(typeof(TView));

        /// <summary>
        ///   <para>关闭所有视图</para>
        /// </summary>
        public void CloseViewAll()
        {
            var viewsToClose = new List<ViewReference>(m_CachedViews.Values);
            m_CachedViews.Clear();
            
            while (m_ViewStack.TryPop(out _)) { }

            for (int i = 0; i < viewsToClose.Count; i++)
            {
                viewsToClose[i].Close();
            }
        }

        /// <summary>
        ///   <para>注销视图</para>
        /// </summary>
        private void UnregisterView(IView view)
        {
            if (view == null) return;
            var viewType = view.GetType();
            m_CachedViews.TryRemove(viewType, out _);
        }

        /// <summary>
        ///   <para>释放所有视图</para>
        /// </summary>
        private void DisposeViews()
        {
            var viewsToDispose = new List<ViewReference>(m_CachedViews.Values);
            m_CachedViews.Clear();
            
            while (m_ViewStack.TryPop(out _))
            {
            }

            for (int i = 0; i < viewsToDispose.Count; i++)
            {
                viewsToDispose[i].Dispose();
            }
        }

        /// <summary>
        ///   <para>视图引用</para>
        /// </summary>
        private readonly struct ViewReference : IDisposable
        {
            private readonly WeakReference<IView> m_ViewRef;
            private readonly WeakReference<GameObject> m_GameObjectRef;
            private readonly Action<IView> m_UnregisterCallback;

            /// <summary>
            ///   <para>构造视图引用</para>
            /// </summary>
            public ViewReference(IView view, GameObject gameObject, Action<IView> unregisterCallback)
            {
                m_ViewRef = new WeakReference<IView>(view);
                m_GameObjectRef = new WeakReference<GameObject>(gameObject);
                m_UnregisterCallback = unregisterCallback;
            }

            /// <summary>
            ///   <para>获取视图</para>
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IView GetView()
            {
                return m_ViewRef.TryGetTarget(out var view) ? view : null;
            }

            /// <summary>
            ///   <para>关闭视图</para>
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Close()
            {
                if (m_ViewRef.TryGetTarget(out var view))
                {
                    view.OnClosed -= m_UnregisterCallback;
                    view.Close();
                }
            }

            /// <summary>
            ///   <para>释放视图引用</para>
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (m_ViewRef.TryGetTarget(out var view))
                {
                    view.OnClosed -= m_UnregisterCallback;
                }
            }
        }

#endif
    }

    #endregion
    
    #region Command
    
    /// <summary>
    ///   <para>MVC命令接口（用于执行和撤销操作）</para>
    /// </summary>
    public interface ICommand : IBelongToActivity
    {
        /// <summary>
        ///   <para>执行命令</para>
        /// </summary>
        void Execute();
        /// <summary>
        ///   <para>撤销命令</para>
        /// </summary>
        void Undo();
    }
    
    /// <summary>
    ///   <para>MVC命令基类</para>
    /// </summary>
    [Serializable]
    public abstract class CommandBase : ICommand
    {
        void ICommand.Execute() => OnExecute();
        void ICommand.Undo() => OnUndo();

        /// <summary>
        ///   <para>获取所属的Activity</para>
        /// </summary>
        public abstract IActivity GetActivity();
        /// <summary>
        ///   <para>命令执行逻辑</para>
        /// </summary>
        protected abstract void OnExecute();
        /// <summary>
        ///   <para>命令撤销逻辑</para>
        /// </summary>
        protected virtual void OnUndo() { }
    }
    
    /// <summary>
    ///   <para>MVC命令扩展方法</para>
    /// </summary>
    public static class CommandExtension
    {
        /// <summary>
        ///   <para>获取指定类型的模型</para>
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        public static T GetModel<T>(this ICommand self)
            where T : class, IModel, new()
            => self.GetActivity()?.GetModel<T>();
    }
    
    #endregion
}