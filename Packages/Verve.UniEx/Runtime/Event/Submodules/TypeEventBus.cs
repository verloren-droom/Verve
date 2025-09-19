#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Event
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    /// 类型事件总线
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(EventGameFeature), Description = "类型事件总线")]
    public sealed partial class TypeEventBus : GameFeatureSubmodule
    {
        private ConcurrentDictionary<Type, Delegate> m_TypeEventHandlers = new ConcurrentDictionary<Type, Delegate>();

        
        protected override void OnShutdown()
        {
            RemoveAllListener();
            base.OnShutdown();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener<T>(Action<T> handler)
        {
            m_TypeEventHandlers?.AddOrUpdate(
                typeof(T),
                handler,
                (key, existingDelegate) => existingDelegate is Action<T> existingHandler
                    ? Delegate.Combine(existingHandler, handler)
                    : throw new ArgumentException($"Handler parameter type must be {typeof(T).Name}"));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddWeakListener<T>(Action<T> handler)
        {
            if (handler == null) return;
            
            var weakRef = new WeakReference<Action<T>>(handler);
            Action<T> weakHandler = args => 
            {
                if (weakRef.TryGetTarget(out var target))
                    target(args);
            };
            
            AddListener(weakHandler);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveListener<T>(Action<T> handler)
        {
            if (m_TypeEventHandlers == null) return;
            if (m_TypeEventHandlers.TryGetValue(typeof(T), out var existingDelegate) && existingDelegate is Action<T> existingHandler)
            {
                var newHandler = (Action<T>)Delegate.Remove(existingHandler, handler);
                if (newHandler != null)
                {
                    m_TypeEventHandlers[typeof(T)] = newHandler;
                }
                else
                {
                    m_TypeEventHandlers.TryRemove(typeof(T), out _);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<T>(T eventArgs)
        {
            if (m_TypeEventHandlers == null) return;
            if (m_TypeEventHandlers.TryGetValue(typeof(T), out var delegateObj) && delegateObj is Action<T> handler)
            {
                handler.Invoke(eventArgs);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllListener()
        {
            m_TypeEventHandlers.Clear();
        }
    }
}

#endif