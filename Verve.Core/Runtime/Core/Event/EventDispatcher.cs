namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>事件分发器</para>
    /// </summary>
    /// <typeparam name="TKey">事件索引类型</typeparam>
    [Serializable]
    public class EventDispatcher<TKey> where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///   <para>所有事件处理函数</para>
        /// </summary>
        private readonly Dictionary<TKey, List<Delegate>> m_EventHandlers;
        
        /// <summary>
        ///   <para>事件分发异步锁</para>
        /// </summary>
        private readonly ReaderWriterLockSlim m_Lock;
        
        /// <summary>
        ///   <para>所有事件处理函数</para>
        /// </summary>
        public IReadOnlyDictionary<TKey, List<Delegate>> Handlers => m_EventHandlers;
        
#if UNITY_EDITOR || DEBUG
        /// <summary>
        ///   <para>事件记录回调</para>
        /// </summary>
        public event Action<EventRecord> OnEventRecorded;
#endif

        /// <summary>
        ///   <para>事件分发器构造函数</para>
        /// </summary>
        /// <param name="capacity">容量</param>
        public EventDispatcher(int capacity = 32)
        {
            m_EventHandlers = new Dictionary<TKey, List<Delegate>>(capacity);
            m_Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }
        
        /// <summary>
        ///   <para>事件分发器析构函数</para>
        /// </summary>
        ~EventDispatcher()
        {
            m_Lock.EnterWriteLock();
            try
            {
                m_EventHandlers?.Clear();
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable On(TKey eventKey, Action handler)
            => On_Implement(eventKey, handler);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable On<T>(TKey eventKey, Action<T> handler)
            => On_Implement(eventKey, handler);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable On<T1, T2>(TKey eventKey, Action<T1, T2> handler)
            => On_Implement(eventKey, handler);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable On<T1, T2, T3>(TKey eventKey, Action<T1, T2, T3> handler)
            => On_Implement(eventKey, handler);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable On<T1, T2, T3, T4>(TKey eventKey, Action<T1, T2, T3, T4> handler)
            => On_Implement(eventKey, handler);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable On<T1, T2, T3, T4, T5>(TKey eventKey, Action<T1, T2, T3, T4, T5> handler)
            => On_Implement(eventKey, handler);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable On<T1, T2, T3, T4, T5, T6>(TKey eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => On_Implement(eventKey, handler);
        
        /// <summary>
        ///   <para>监听事件</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <returns>
        ///   <para>取消监听事件处理函数</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IDisposable On_Implement(TKey eventKey, Delegate handler)
        {
            if (eventKey == null)
                throw new ArgumentNullException(nameof(eventKey));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            m_Lock.EnterWriteLock();
            try
            {
                if (!m_EventHandlers.TryGetValue(eventKey, out var handlers))
                {
                    handlers = new List<Delegate>();
                    m_EventHandlers[eventKey] = handlers;
                }
                if (!handlers.Contains(handler))
                {
                    handlers.Add(handler);
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.On, handler));
#endif
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
            return new EventHandlerDisposable(this, eventKey, handler);
        }
        
        /// <summary>
        ///   <para>检查是否包含指定事件键</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">指定事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(TKey eventKey, Delegate handler = null)
        {
            if (eventKey == null) return false;
            
            m_Lock.EnterReadLock();
            try
            {
                if (!m_EventHandlers.TryGetValue(eventKey, out var handlers))
                    return false;
                    
                return handler == null ? handlers.Count > 0 : handlers.Contains(handler);
            }
            finally
            {
                m_Lock.ExitReadLock();
            }
        }

        /// <summary>
        ///   <para>取消监听事件</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Off(TKey eventKey)
        {
            if (eventKey == null) return;

            m_Lock.EnterWriteLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers))
                {
                    handlers.Clear();
                    m_EventHandlers.Remove(eventKey);
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Off));
#endif
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }
        
        /// <summary>
        ///   <para>取消监听事件</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Off(TKey eventKey, Delegate handler)
        {
            if (eventKey == null || handler == null) return;

            m_Lock.EnterWriteLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers) && handlers.Remove(handler) && handlers.Count == 0 && m_EventHandlers.Remove(eventKey))
                {
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Off, handler));
#endif
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///   <para>取消所有监听事件</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OffAll()
        {
            m_Lock.EnterWriteLock();
            try
            {
                foreach (var handlers in m_EventHandlers.Values)
                {
                    handlers?.Clear();
                }
                m_EventHandlers.Clear();
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }

        #region 发送事件

        /// <summary>
        ///   <para>发送事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit(TKey eventKey)
        {
            if (eventKey == null) return;

            Delegate[] handlersArray = null;
            m_Lock.EnterReadLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers) && handlers.Count > 0)
                {
                    handlersArray = handlers.ToArray();
                }
            }
            finally
            {
                m_Lock.ExitReadLock();
            }

            if (handlersArray == null) return;
            for (var i = 0; i < handlersArray.Length; i++)
            {
                if (handlersArray[i] != null && handlersArray[i] is Action handler)
                {
                    handler.Invoke();
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Emit, handler));
#endif
                }
            }
        }

        /// <summary>
        ///   <para>发送事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg">参数1</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit<T>(TKey eventKey, T arg)
        {
            if (eventKey == null) return;

            Delegate[] handlersArray = null;
            m_Lock.EnterReadLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers) && handlers.Count > 0)
                {
                    handlersArray = handlers.ToArray();
                }
            }
            finally
            {
                m_Lock.ExitReadLock();
            }

            if (handlersArray == null) return;
            for (var i = 0; i < handlersArray.Length; i++)
            {
                if (handlersArray[i] != null && handlersArray[i] is Action<T> handler)
                {
                    handler.Invoke(arg);
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Emit, handler, arg));
#endif
                }
            }
        }

        /// <summary>
        ///   <para>发送事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit<T1, T2>(TKey eventKey, T1 arg1, T2 arg2)
        {
            if (eventKey == null) return;

            Delegate[] handlersArray = null;
            m_Lock.EnterReadLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers) && handlers.Count > 0)
                {
                    handlersArray = handlers.ToArray();
                }
            }
            finally
            {
                m_Lock.ExitReadLock();
            }

            if (handlersArray == null) return;
            for (var i = 0; i < handlersArray.Length; i++)
            {
                if (handlersArray[i] != null && handlersArray[i] is Action<T1, T2> handler)
                {
                    handler.Invoke(arg1, arg2);
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Emit, handler, arg1, arg2));
#endif
                }
            }
        }

        /// <summary>
        ///   <para>发送事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit<T1, T2, T3>(TKey eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            if (eventKey == null) return;

            Delegate[] handlersArray = null;
            m_Lock.EnterReadLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers) && handlers.Count > 0)
                {
                    handlersArray = handlers.ToArray();
                }
            }
            finally
            {
                m_Lock.ExitReadLock();
            }

            if (handlersArray == null) return;
            for (var i = 0; i < handlersArray.Length; i++)
            {
                if (handlersArray[i] != null && handlersArray[i] is Action<T1, T2, T3> handler)
                {
                    handler.Invoke(arg1, arg2, arg3);
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Emit, handler, arg1, arg2, arg3));
#endif
                }
            }
        }

        /// <summary>
        ///   <para>发送事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit<T1, T2, T3, T4>(TKey eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (eventKey == null) return;

            Delegate[] handlersArray = null;
            m_Lock.EnterReadLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers) && handlers.Count > 0)
                {
                    handlersArray = handlers.ToArray();
                }
            }
            finally
            {
                m_Lock.ExitReadLock();
            }

            if (handlersArray == null) return;
            for (var i = 0; i < handlersArray.Length; i++)
            {
                if (handlersArray[i] != null && handlersArray[i] is Action<T1, T2, T3, T4> handler)
                {
                    handler.Invoke(arg1, arg2, arg3, arg4);
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Emit, handler, arg1, arg2, arg3, arg4));
#endif
                }
            }
        }

        /// <summary>
        ///   <para>发送事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        /// <param name="arg5">参数5</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit<T1, T2, T3, T4, T5>(TKey eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (eventKey == null) return;

            Delegate[] handlersArray = null;
            m_Lock.EnterReadLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers) && handlers.Count > 0)
                {
                    handlersArray = handlers.ToArray();
                }
            }
            finally
            {
                m_Lock.ExitReadLock();
            }

            if (handlersArray == null) return;
            for (var i = 0; i < handlersArray.Length; i++)
            {
                if (handlersArray[i] != null && handlersArray[i] is Action<T1, T2, T3, T4, T5> handler)
                {
                    handler.Invoke(arg1, arg2, arg3, arg4, arg5);
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Emit, handler, arg1, arg2, arg3, arg4, arg5));
#endif
                }
            }
        }

        /// <summary>
        ///   <para>发送事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        /// <param name="arg5">参数5</param>
        /// <param name="arg6">参数6</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit<T1, T2, T3, T4, T5, T6>(TKey eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (eventKey == null) return;

            Delegate[] handlersArray = null;
            m_Lock.EnterReadLock();
            try
            {
                if (m_EventHandlers.TryGetValue(eventKey, out var handlers) && handlers.Count > 0)
                {
                    handlersArray = handlers.ToArray();
                }
            }
            finally
            {
                m_Lock.ExitReadLock();
            }

            if (handlersArray == null) return;
            for (var i = 0; i < handlersArray.Length; i++)
            {
                if (handlersArray[i] != null && handlersArray[i] is Action<T1, T2, T3, T4, T5, T6> handler)
                {
                    handler.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
#if UNITY_EDITOR || DEBUG
                    OnEventRecorded?.Invoke(new EventRecord(eventKey, EventRecordStatus.Emit, handler, arg1, arg2, arg3, arg4, arg5, arg6));
#endif
                }
            }
        }
        
        #endregion
        
        
        /// <summary>
        ///   <para>事件处理者</para>
        /// </summary>
        private struct EventHandlerDisposable : IDisposable
        {
            private readonly EventDispatcher<TKey> m_Dispatcher;
            private readonly TKey m_EventKey;
            private readonly Delegate m_Handler;
            private bool m_Disposed;

            public EventHandlerDisposable(EventDispatcher<TKey> dispatcher, TKey eventKey, Delegate handler)
            {
                m_Dispatcher = dispatcher;
                m_EventKey = eventKey;
                m_Handler = handler;
                m_Disposed = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose()
            {
                if (m_Disposed) return;
                m_Dispatcher?.Off(m_EventKey, m_Handler);
                m_Disposed = true;
            }
        }


#if UNITY_EDITOR || DEBUG
        /// <summary>
        ///   <para>事件记录信息</para>
        /// </summary>
        public readonly struct EventRecord
        {
            public readonly TKey EventKey;
            public readonly DateTime Timestamp;
            public readonly object[] Arguments;
            public readonly Delegate Handler;
            public readonly EventRecordStatus RecordStatus;
            
            
            public EventRecord(TKey eventKey, EventRecordStatus recordStatus, Delegate handler = null, params object[] args)
            {
                EventKey = eventKey;
                RecordStatus = recordStatus;
                Handler = handler;
                Timestamp = DateTime.Now;
                Arguments = args ?? Array.Empty<object>();
            }
        }
#endif
    }
    
    
#if UNITY_EDITOR || DEBUG
    /// <summary>
    ///   <para>事件记录状态</para>
    /// </summary>
    public enum EventRecordStatus : byte
    {
        Emit,
        On,
        Off,
    }
#endif
}