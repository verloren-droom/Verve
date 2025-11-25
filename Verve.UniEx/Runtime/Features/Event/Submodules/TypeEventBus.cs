#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Event
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>类型事件总线</para>
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

        /// <summary>
        ///   <para>添加事件监听</para>
        /// </summary>
        /// <param name="handler">事件处理函数</param>
        /// <typeparam name="T">事件参数类型</typeparam>
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
        
        /// <summary>
        ///   <para>添加弱事件监听</para>
        /// </summary>
        /// <param name="handler">事件处理函数</param>
        /// <typeparam name="T">事件参数类型</typeparam>
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
        
        /// <summary>
        ///   <para>移除事件监听</para>
        /// </summary>
        /// <param name="handler">事件处理函数</param>
        /// <typeparam name="T">事件参数类型</typeparam>
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

        /// <summary>
        ///   <para>触发事件</para>
        /// </summary>
        /// <param name="eventArgs">事件参数</param>
        /// <typeparam name="T">事件参数类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<T>(T eventArgs)
        {
            if (m_TypeEventHandlers == null) return;
            if (m_TypeEventHandlers.TryGetValue(typeof(T), out var delegateObj) && delegateObj is Action<T> handler)
            {
                handler.Invoke(eventArgs);
            }
        }
        
        /// <summary>
        ///   <para>移除所有事件监听</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllListener()
        {
            m_TypeEventHandlers.Clear();
        }
    }
}

#endif