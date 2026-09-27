namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>同步事件分发器；订阅变更时复制快照，发布时不分配监听器数组。</para>
    /// </summary>
    /// <typeparam name="TKey">键类型。</typeparam>
    public sealed class EventDispatcher<TKey> : DisposableObject
    {
        /// <summary>
        ///   <para>锁。</para>
        /// </summary>
        private readonly object m_Lock = new();
        /// <summary>
        ///   <para>处理函数。</para>
        /// </summary>
        private readonly Dictionary<TKey, Registration[]> m_Handlers;
        /// <summary>
        ///   <para>创建事件分发器。</para>
        /// </summary>
        /// <param name="capacity">事件表初始容量。</param>
        public EventDispatcher(int capacity = 32) => m_Handlers = new(capacity);

        /// <summary>
        ///   <para>处理函数。</para>
        /// </summary>
        public IReadOnlyDictionary<TKey, IReadOnlyList<Delegate>> Handlers
        {
            get
            {
                lock (m_Lock)
                {
                    ThrowIfDisposed();
                    var result = new Dictionary<TKey, IReadOnlyList<Delegate>>(m_Handlers.Count);
                    foreach (var pair in m_Handlers)
                        result.Add(pair.Key, Array.ConvertAll(pair.Value, item => item.Handler));
                    return result;
                }
            }
        }

#if (DEBUG || DEVELOPMENT_BUILD)
        /// <summary>
        ///   <para>事件记录通知。</para>
        /// </summary>
        internal event Action<EventRecord> OnEventRecorded;
#endif

        /// <inheritdoc />
        protected override void OnDispose()
        {
            lock (m_Lock)
            {
                foreach (var entries in m_Handlers.Values)
                    foreach (var entry in entries) entry.Subscription.Detach();
                m_Handlers.Clear();
#if (DEBUG || DEVELOPMENT_BUILD)
                OnEventRecorded = null;
#endif
            }
        }

        /// <summary>
        ///   <para>订阅。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        private IDisposable Subscribe(TKey key, Delegate handler)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var subscription = new Subscription(this, key);
            lock (m_Lock)
            {
                ThrowIfDisposed();
                if (!m_Handlers.TryGetValue(key, out var current)) current = Array.Empty<Registration>();
                foreach (var item in current)
                {
                    if (item.Handler.GetType() != handler.GetType())
                        throw new InvalidOperationException($"Event '{key}' is already registered with a different payload signature.");
                    if (item.Handler.Equals(handler))
                        throw new InvalidOperationException($"Handler is already subscribed to event '{key}'.");
                }
                var next = new Registration[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = new Registration(subscription, handler);
                m_Handlers[key] = next;
            }
#if (DEBUG || DEVELOPMENT_BUILD)
            try { OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.On, handler)); }
            catch (Exception failure)
            {
                try { subscription.Dispose(); }
                catch (Exception cleanup) { throw ExceptionUtility.Combine(failure, cleanup); }
                throw;
            }
#endif
            return subscription;
        }

        /// <summary>
        ///   <para>判断事件是否存在订阅。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        public bool Has(TKey key, Delegate handler = null)
        {
            lock (m_Lock)
            {
                ThrowIfDisposed();
                if (!m_Handlers.TryGetValue(key, out var current)) return false;
                if (handler == null) return true;
                return Array.Exists(current, item => item.Handler.Equals(handler));
            }
        }

        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="key">键。</param>
        public void Off(TKey key)
        {
            bool removed;
            lock (m_Lock)
            {
                ThrowIfDisposed();
                removed = m_Handlers.Remove(key, out var entries);
                if (removed)
                    foreach (var entry in entries) entry.Subscription.Detach();
            }
#if (DEBUG || DEVELOPMENT_BUILD)
            if (removed) OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Off));
#endif
        }

        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        public void Off(TKey key, Delegate handler)
        {
            Subscription subscription;
            lock (m_Lock)
            {
                ThrowIfDisposed();
                if (!m_Handlers.TryGetValue(key, out var current)) return;
                subscription = Array.Find(current, item => item.Handler.Equals(handler)).Subscription;
            }
            subscription?.Dispose();
        }

        /// <summary>
        ///   <para>取消全部事件订阅。</para>
        /// </summary>
        public void OffAll()
        {
            lock (m_Lock)
            {
                ThrowIfDisposed();
                foreach (var entries in m_Handlers.Values)
                    foreach (var entry in entries) entry.Subscription.Detach();
                m_Handlers.Clear();
            }
        }

        /// <summary>
        ///   <para>获取快照。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <typeparam name="THandler">处理函数类型。</typeparam>
        private Registration[] GetSnapshot<THandler>(TKey key) where THandler : Delegate
        {
            lock (m_Lock)
            {
                ThrowIfDisposed();
                if (!m_Handlers.TryGetValue(key, out var snapshot)) return Array.Empty<Registration>();
                if (snapshot[0].Handler.GetType() != typeof(THandler))
                    throw new InvalidOperationException($"Event '{key}' was emitted with a different payload signature.");
                return snapshot;
            }
        }

        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="subscription">订阅。</param>
        private void Unsubscribe(Subscription subscription)
        {
            TKey key;
            Delegate handler;
            lock (m_Lock)
            {
                // 取消或释放已解除所有权，旧句柄不会影响后续同键订阅。
                if (subscription.Owner != this) return;
                key = subscription.Key;
                var current = m_Handlers[key];
                int index = Array.FindIndex(current, entry => ReferenceEquals(entry.Subscription, subscription));
                handler = current[index].Handler;
                if (current.Length == 1) m_Handlers.Remove(key);
                else
                {
                    var next = new Registration[current.Length - 1];
                    Array.Copy(current, 0, next, 0, index);
                    Array.Copy(current, index + 1, next, index, current.Length - index - 1);
                    m_Handlers[key] = next;
                }
                subscription.Detach();
            }
#if (DEBUG || DEVELOPMENT_BUILD)
            OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Off, handler));
#endif
        }

        /// <summary>
        ///   <para>订阅快照条目；当前发布不受中途取消订阅影响。</para>
        /// </summary>
        private readonly struct Registration
        {
            /// <summary>
            ///   <para>订阅句柄。</para>
            /// </summary>
            internal readonly Subscription Subscription;
            /// <summary>
            ///   <para>处理函数。</para>
            /// </summary>
            internal readonly Delegate Handler;

            /// <summary>
            ///   <para>创建快照条目。</para>
            /// </summary>
            /// <param name="subscription">订阅句柄。</param>
            /// <param name="handler">处理函数。</param>
            internal Registration(Subscription subscription, Delegate handler)
            {
                Subscription = subscription;
                Handler = handler;
            }
        }

        /// <summary>
        ///   <para>事件订阅句柄；不持有回调，取消后解除所有者和键引用。</para>
        /// </summary>
        private sealed class Subscription : DisposableObject
        {
            /// <summary>
            ///   <para>所有者。</para>
            /// </summary>
            internal EventDispatcher<TKey> Owner { get; private set; }
            /// <summary>
            ///   <para>键。</para>
            /// </summary>
            internal TKey Key { get; private set; }

            /// <summary>
            ///   <para>创建订阅。</para>
            /// </summary>
            /// <param name="owner">所有者。</param>
            /// <param name="key">键。</param>
            internal Subscription(EventDispatcher<TKey> owner, TKey key)
            {
                Owner = owner;
                Key = key;
            }

            /// <summary>
            ///   <para>在所有者锁内解除引用。</para>
            /// </summary>
            internal void Detach()
            {
                Owner = null;
                Key = default;
            }

            /// <inheritdoc />
            protected override void OnDispose() => Owner?.Unsubscribe(this);
        }

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        public IDisposable On(TKey key, Action handler) => Subscribe(key, handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        public void Emit(TKey key)
        {
            foreach (var subscription in GetSnapshot<Action>(key))
            {
                var handler = (Action)subscription.Handler;
#if (DEBUG || DEVELOPMENT_BUILD)
                OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Emit, handler));
#endif
                handler();
            }
        }

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public IDisposable On<T1>(TKey key, Action<T1> handler) => Subscribe(key, handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public void Emit<T1>(TKey key, T1 arg1)
        {
            foreach (var subscription in GetSnapshot<Action<T1>>(key))
            {
                var handler = (Action<T1>)subscription.Handler;
#if (DEBUG || DEVELOPMENT_BUILD)
                OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Emit, handler, arg1));
#endif
                handler(arg1);
            }
        }

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public IDisposable On<T1, T2>(TKey key, Action<T1, T2> handler) => Subscribe(key, handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public void Emit<T1, T2>(TKey key, T1 arg1, T2 arg2)
        {
            foreach (var subscription in GetSnapshot<Action<T1, T2>>(key))
            {
                var handler = (Action<T1, T2>)subscription.Handler;
#if (DEBUG || DEVELOPMENT_BUILD)
                OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Emit, handler, arg1, arg2));
#endif
                handler(arg1, arg2);
            }
        }

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public IDisposable On<T1, T2, T3>(TKey key, Action<T1, T2, T3> handler) => Subscribe(key, handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public void Emit<T1, T2, T3>(TKey key, T1 arg1, T2 arg2, T3 arg3)
        {
            foreach (var subscription in GetSnapshot<Action<T1, T2, T3>>(key))
            {
                var handler = (Action<T1, T2, T3>)subscription.Handler;
#if (DEBUG || DEVELOPMENT_BUILD)
                OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Emit, handler, arg1, arg2, arg3));
#endif
                handler(arg1, arg2, arg3);
            }
        }

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public IDisposable On<T1, T2, T3, T4>(TKey key, Action<T1, T2, T3, T4> handler) => Subscribe(key, handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public void Emit<T1, T2, T3, T4>(TKey key, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            foreach (var subscription in GetSnapshot<Action<T1, T2, T3, T4>>(key))
            {
                var handler = (Action<T1, T2, T3, T4>)subscription.Handler;
#if (DEBUG || DEVELOPMENT_BUILD)
                OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Emit, handler, arg1, arg2, arg3, arg4));
#endif
                handler(arg1, arg2, arg3, arg4);
            }
        }

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public IDisposable On<T1, T2, T3, T4, T5>(TKey key, Action<T1, T2, T3, T4, T5> handler) => Subscribe(key, handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <param name="arg5">第五个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public void Emit<T1, T2, T3, T4, T5>(TKey key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            foreach (var subscription in GetSnapshot<Action<T1, T2, T3, T4, T5>>(key))
            {
                var handler = (Action<T1, T2, T3, T4, T5>)subscription.Handler;
#if (DEBUG || DEVELOPMENT_BUILD)
                OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Emit, handler, arg1, arg2, arg3, arg4, arg5));
#endif
                handler(arg1, arg2, arg3, arg4, arg5);
            }
        }

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public IDisposable On<T1, T2, T3, T4, T5, T6>(TKey key, Action<T1, T2, T3, T4, T5, T6> handler) => Subscribe(key, handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <param name="arg5">第五个参数。</param>
        /// <param name="arg6">第六个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public void Emit<T1, T2, T3, T4, T5, T6>(TKey key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            foreach (var subscription in GetSnapshot<Action<T1, T2, T3, T4, T5, T6>>(key))
            {
                var handler = (Action<T1, T2, T3, T4, T5, T6>)subscription.Handler;
#if (DEBUG || DEVELOPMENT_BUILD)
                OnEventRecorded?.Invoke(new EventRecord(key, EventRecordStatus.Emit, handler, arg1, arg2, arg3, arg4, arg5, arg6));
#endif
                handler(arg1, arg2, arg3, arg4, arg5, arg6);
            }
        }

#if (DEBUG || DEVELOPMENT_BUILD)
        /// <summary>
        ///   <para>事件记录。</para>
        /// </summary>
        internal readonly struct EventRecord
        {
            /// <summary>
            ///   <para>事件键。</para>
            /// </summary>
            public readonly TKey eventKey;
            /// <summary>
            ///   <para>时间戳。</para>
            /// </summary>
            public readonly DateTime timestamp;
            /// <summary>
            ///   <para>参数。</para>
            /// </summary>
            public readonly object[] arguments;
            /// <summary>
            ///   <para>处理函数。</para>
            /// </summary>
            public readonly Delegate handler;
            /// <summary>
            ///   <para>记录状态。</para>
            /// </summary>
            public readonly EventRecordStatus recordStatus;

            /// <summary>
            ///   <para>创建事件记录。</para>
            /// </summary>
            /// <param name="eventKey">事件键。</param>
            /// <param name="recordStatus">记录状态。</param>
            /// <param name="handler">处理函数。</param>
            /// <param name="args">事件参数。</param>
            public EventRecord(TKey eventKey, EventRecordStatus recordStatus, Delegate handler = null, params object[] args)
            {
                this.eventKey = eventKey;
                this.recordStatus = recordStatus;
                this.handler = handler;
                timestamp = DateTime.Now;
                arguments = args;
            }
        }
#endif
    }

#if (DEBUG || DEVELOPMENT_BUILD)
    /// <summary>
    ///   <para>事件记录状态。</para>
    /// </summary>
    public enum EventRecordStatus : byte
    {
        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        Emit,
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        On,
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        Off,
    }
#endif
}
