namespace Verve.Event
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 枚举事件子模块
    /// </summary>
    public sealed partial class EnumEventBusSubmodule : IGameFeatureSubmodule
    {
        public string ModuleName => "EnumEventBus";
        
        private ConcurrentDictionary<Enum, Delegate> m_EnumEventHandlers;
        
        public void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            m_EnumEventHandlers = new ConcurrentDictionary<Enum, Delegate>();
        }

        public void OnModuleUnloaded() => RemoveAllListener();

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<T>(Enum tag, T eventArgs)
        {
            if (m_EnumEventHandlers == null) return;
            if (m_EnumEventHandlers.TryGetValue(tag, out var delegateObj) && delegateObj is Action<T> handler)
            {
                handler.Invoke(eventArgs);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllListener()
        {
            m_EnumEventHandlers.Clear();
        }
    }
}