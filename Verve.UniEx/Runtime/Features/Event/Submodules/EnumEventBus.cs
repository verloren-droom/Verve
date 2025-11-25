#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Event
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>枚举事件总线</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(EventGameFeature), Description = "枚举事件总线")]
    public sealed partial class EnumEventBus : GameFeatureSubmodule
    {
        private readonly ConcurrentDictionary<Enum, Delegate> m_EnumEventHandlers = new ConcurrentDictionary<Enum, Delegate>();

        protected override void OnShutdown()
        {
            RemoveAllListener();
        }

        /// <summary>
        ///   <para>添加事件监听</para>
        /// </summary>
        /// <param name="tag">事件标签</param>
        /// <param name="handler">事件处理函数</param>
        /// <typeparam name="T">事件参数类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener<T>(Enum tag, Action<T> handler)
        {
            m_EnumEventHandlers?.AddOrUpdate(
                tag,
                handler,
                (key, existingDelegate) => existingDelegate is Action<T> existingHandler
                    ? Delegate.Combine(existingHandler, handler)
                    : throw new ArgumentException($"Handler parameter type must be {tag.ToString()}"));
        }
        
        /// <summary>
        ///   <para>添加弱事件监听</para>
        /// </summary>
        /// <param name="tag">事件标签</param>
        /// <param name="handler">事件处理函数</param>
        /// <typeparam name="T">事件参数类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddWeakListener<T>(Enum tag, Action<T> handler)
        {
            if (handler == null) return;
            
            var weakRef = new WeakReference<Action<T>>(handler);
            Action<T> weakHandler = args => 
            {
                if (weakRef.TryGetTarget(out var target))
                    target(args);
            };
            
            AddListener(tag, weakHandler);
        }

        /// <summary>
        ///   <para>移除事件监听</para>
        /// </summary>
        /// <param name="tag">事件标签</param>
        /// <param name="handler">事件处理函数</param>
        /// <typeparam name="T">事件参数类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveListener<T>(Enum tag, Action<T> handler)
        {
            if (m_EnumEventHandlers == null) return;
            if (m_EnumEventHandlers.TryGetValue(tag, out var existingDelegate) && existingDelegate is Action<T> existingHandler)
            {
                var newHandler = (Action<T>)Delegate.Remove(existingHandler, handler);
                if (newHandler != null)
                {
                    m_EnumEventHandlers[tag] = newHandler;
                }
                else
                {
                    m_EnumEventHandlers.TryRemove(tag, out _);
                }
            }
        }

        /// <summary>
        ///   <para>触发事件</para>
        /// </summary>
        /// <param name="tag">事件标签</param>
        /// <param name="eventArgs">事件参数</param>
        /// <typeparam name="T">事件参数类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<T>(Enum tag, T eventArgs)
        {
            if (m_EnumEventHandlers == null) return;
            if (m_EnumEventHandlers.TryGetValue(tag, out var delegateObj) && delegateObj is Action<T> handler)
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
            m_EnumEventHandlers.Clear();
        }
    }
}

#endif