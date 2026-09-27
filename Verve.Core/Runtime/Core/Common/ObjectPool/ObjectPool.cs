namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>基础对象池。</para>
    /// </summary>
    /// <remarks><see cref="Capacity"/> 限制闲置对象数量，已借出的对象仍由借用方归还。</remarks>
    /// <typeparam name="T">池对象类型。</typeparam>
    public class ObjectPool<T> : DisposableObject, IObjectPool<T>
    {
        /// <summary>
        ///   <para>闲置对象列表。</para>
        /// </summary>
        private readonly List<T> m_Items;
        /// <summary>
        ///   <para>对象创建委托。</para>
        /// </summary>
        private readonly Func<T> m_Create;
        /// <summary>
        ///   <para>借出回调。</para>
        /// </summary>
        private readonly Action<T> m_OnGet;
        /// <summary>
        ///   <para>归还回调。</para>
        /// </summary>
        private readonly Action<T> m_OnRelease;
        /// <summary>
        ///   <para>销毁回调。</para>
        /// </summary>
        private readonly Action<T> m_OnDestroy;
        /// <summary>
        ///   <para>闲置对象容量。</para>
        /// </summary>
        private int m_Capacity;

        /// <inheritdoc />
        public int Count => m_Items.Count;

        /// <inheritdoc />
        public int Capacity
        {
            get => m_Capacity;
            set
            {
                ThrowIfDisposed();
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                m_Capacity = value;
                Trim(value);
            }
        }

        /// <summary>
        ///   <para>创建对象池。</para>
        /// </summary>
        /// <param name="onCreateObject">对象创建委托。</param>
        /// <param name="onGetFromPool">对象借出回调。</param>
        /// <param name="onReleaseToPool">对象归还回调。</param>
        /// <param name="onDestroyObject">对象销毁回调。</param>
        /// <param name="preSize">预创建数量。</param>
        /// <param name="capacity">闲置对象容量上限。</param>
        public ObjectPool(Func<T> onCreateObject, Action<T> onGetFromPool = null,
            Action<T> onReleaseToPool = null, Action<T> onDestroyObject = null, int preSize = 5, int capacity = 20)
        {
            m_Create = onCreateObject ?? throw new ArgumentNullException(nameof(onCreateObject));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (preSize < 0 || preSize > capacity) throw new ArgumentOutOfRangeException(nameof(preSize));
            m_OnGet = onGetFromPool;
            m_OnRelease = onReleaseToPool;
            m_OnDestroy = onDestroyObject;
            m_Capacity = capacity;
            m_Items = new List<T>(capacity);
            try
            {
                for (int i = 0; i < preSize; i++) Release(m_Create());
            }
            catch (Exception failure)
            {
                try { Dispose(); }
                catch (Exception cleanup) { throw ExceptionUtility.Combine(failure, cleanup); }
                throw;
            }
        }

        /// <inheritdoc />
        public T Get(Predicate<T> predicate = null)
        {
            if (TryGet(out var item, predicate)) return item;
            throw new InvalidOperationException("No matching pooled object is available, or the factory returned null.");
        }

        /// <inheritdoc />
        public bool TryGet(out T element, Predicate<T> predicate = null)
        {
            ThrowIfDisposed();
            int index = predicate == null ? m_Items.Count - 1 : m_Items.FindLastIndex(predicate);
            if (index >= 0)
            {
                element = m_Items[index];
                m_Items.RemoveAt(index);
            }
            else if (predicate != null)
            {
                element = default;
                return false;
            }
            else
            {
                element = m_Create();
                if (element is null) return false;
            }
            try
            {
                ThrowIfDisposed();
                m_OnGet?.Invoke(element);
                ThrowIfDisposed();
            }
            catch (Exception failure)
            {
                try { m_OnDestroy?.Invoke(element); }
                catch (Exception cleanup) { throw ExceptionUtility.Combine(failure, cleanup); }
                throw;
            }
            return true;
        }

        /// <inheritdoc />
        public void Release(T element)
        {
            if (element is null) throw new ArgumentNullException(nameof(element));
#if UNITY_EDITOR || DEBUG
            if (!typeof(T).IsValueType)
                foreach (var item in m_Items)
                    if (ReferenceEquals(item, element))
                        throw new InvalidOperationException("Object is already in the pool.");
#endif
            if (IsDisposed || m_Items.Count >= m_Capacity)
            {
                m_OnDestroy?.Invoke(element);
                return;
            }
            try { m_OnRelease?.Invoke(element); }
            catch (Exception failure)
            {
                try { m_OnDestroy?.Invoke(element); }
                catch (Exception cleanup) { throw ExceptionUtility.Combine(failure, cleanup); }
                throw;
            }
            // 回调可能释放池或归还其他对象，按回调后的状态决定是否接管。
            if (IsDisposed || m_Items.Count >= m_Capacity) m_OnDestroy?.Invoke(element);
            else m_Items.Add(element);
        }

        /// <inheritdoc />
        public void ReleaseRange(IEnumerable<T> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            List<Exception> failures = null;
            foreach (var element in elements)
            {
                try { Release(element); }
                catch (Exception failure) { ExceptionUtility.Add(ref failures, failure); }
            }
            ExceptionUtility.ThrowIfAny(failures);
        }

        /// <inheritdoc />
        public void Clear()
        {
            ThrowIfDisposed();
            Trim(0);
        }

        /// <inheritdoc />
        protected override void OnDispose() => Trim(0);

        /// <summary>
        ///   <para>缩减闲置对象数量。</para>
        /// </summary>
        /// <param name="count">保留的闲置对象数量。</param>
        private void Trim(int count)
        {
            List<Exception> failures = null;
            while (m_Items.Count > count)
            {
                int index = m_Items.Count - 1;
                var item = m_Items[index];
                m_Items.RemoveAt(index);
                try { m_OnDestroy?.Invoke(item); }
                catch (Exception failure) { ExceptionUtility.Add(ref failures, failure); }
            }
            ExceptionUtility.ThrowIfAny(failures);
        }
    }
}