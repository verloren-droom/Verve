namespace Verve.Event
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    

    /// <summary>
    /// 字符串事件总线
    /// </summary>
    [System.Serializable]
    public sealed partial class StringEventBusSubmodule : IGameFeatureSubmodule
    {
        public string ModuleName => "StringEventBus";
        
        private ConcurrentDictionary<string, Delegate> m_StringEventHandlers;

        public void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            m_StringEventHandlers = new ConcurrentDictionary<string, Delegate>();
        }

        public void OnModuleUnloaded() => RemoveAllListener();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener<T>(string tag, Action<T> handler)
        {
            m_StringEventHandlers?.AddOrUpdate(
                tag,
                handler,
                (key, existingDelegate) => existingDelegate is Action<T> existingHandler
                    ? Delegate.Combine(existingHandler, handler)
                    : throw new ArgumentException($"Handler parameter type must be {tag.ToString()}"));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddWeakListener<T>(string tag, Action<T> handler)
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
        public void RemoveListener<T>(string tag, Action<T> handler)
        {
            if (m_StringEventHandlers == null) return;
            if (m_StringEventHandlers.TryGetValue(tag, out var existingDelegate) && existingDelegate is Action<T> existingHandler)
            {
                var newHandler = (Action<T>)Delegate.Remove(existingHandler, handler);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<T>(string tag, T eventArgs)
        {
            if (m_StringEventHandlers == null) return;
            if (m_StringEventHandlers.TryGetValue(tag, out var delegateObj) && delegateObj is Action<T> handler)
            {
                handler.Invoke(eventArgs);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllListener()
        {
            m_StringEventHandlers.Clear();
        }
    }
}