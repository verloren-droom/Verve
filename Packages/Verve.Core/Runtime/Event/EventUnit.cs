namespace Verve.Event
{
    using Unit;
    using System;
    using System.Collections.Concurrent;

    
    /// <summary>
    /// 全局事件单元
    /// </summary>
    [CustomUnit("Event", 100), System.Serializable]
    public partial class EventUnit : UnitBase
    {
        protected override void OnShutdown()
        {
            base.OnShutdown();
            RemoveAllListener();
        }
        
        public void RemoveAllListener()
        {
            m_EnumEventHandlers.Clear();
            m_StringEventHandlers.Clear();
        }

        #region 枚举事件 
        private readonly ConcurrentDictionary<Enum, Delegate> m_EnumEventHandlers = new ConcurrentDictionary<Enum, Delegate>();

        public void AddListener<TEventType, THandler>(TEventType eventType, Action<THandler> handler)
            where TEventType : Enum
            where THandler : EventArgsBase
        {
            m_EnumEventHandlers.AddOrUpdate(
                eventType,
                handler,
                (key, existingDelegate) => existingDelegate is Action<THandler> existingHandler
                    ? Delegate.Combine(existingHandler, handler)
                    : throw new ArgumentException($"Handler parameter type must be {typeof(TEventType).Name}"));
        }
        
        public void BindListener<TEventType, THandler>(TEventType eventType, Action<THandler> handler, bool overwrite = true)
            where TEventType : Enum
            where THandler : EventArgsBase
        {
            m_EnumEventHandlers.AddOrUpdate(
                eventType,
                handler,
                (_, oldHandler) => overwrite ? handler : oldHandler);
        }
        
        public void RemoveListener<TEventType, THandler>(TEventType eventType, Action<THandler> handler)
            where TEventType : Enum
            where THandler : EventArgsBase
        {
            if (m_EnumEventHandlers.TryGetValue(eventType, out var existingDelegate) && existingDelegate is Action<THandler> existingHandler)
            {
                var newHandler = (Action<THandler>)Delegate.Remove(existingHandler, handler);
                if (newHandler != null)
                {
                    m_EnumEventHandlers[eventType] = newHandler;
                }
                else
                {
                    m_EnumEventHandlers.TryRemove(eventType, out _);
                }
            }
        }

        public void Invoke<TEventType, THandler>(TEventType eventType, THandler eventArgs)
            where TEventType : Enum
            where THandler : EventArgsBase
        {
            if (m_EnumEventHandlers.TryGetValue(eventType, out var delegateObj) && delegateObj is Action<THandler> handler)
            {
                handler.Invoke(eventArgs);
            }
        }
        #endregion

        #region 字符串事件
        private readonly ConcurrentDictionary<string, Delegate> m_StringEventHandlers = new ConcurrentDictionary<string, Delegate>();
        
        public void AddListener(string tag, Action<object> handler)
        {
            m_StringEventHandlers.AddOrUpdate(
                tag,
                handler,
                (key, existingDelegate) => existingDelegate is Action<object> existingHandler
                    ? Delegate.Combine(existingHandler, handler)
                    : throw new ArgumentException($"Handler parameter type must be {tag}"));
        }
        
        public void RemoveListener(string tag, Action<object> handler)
        {
            if (m_StringEventHandlers.TryGetValue(tag, out var existingDelegate) && existingDelegate is Action<object> existingHandler)
            {
                var newHandler = (Action<object>)Delegate.Remove(existingHandler, handler);
                if (newHandler != null)
                {
                    m_StringEventHandlers[tag] = newHandler;
                }
                else
                {
                    m_StringEventHandlers.TryRemove(tag, out _);
                }
            }
        }
        
        public void Invoke(string tag, object eventArgs)
        {
            if (m_StringEventHandlers.TryGetValue(tag, out var delegateObj) && delegateObj is Action<object> handler)
            {
                handler.Invoke(eventArgs);
            }
        }
        #endregion
    }

    
    [System.Serializable]
    public abstract partial class EventArgsBase : EventArgs { }
}