namespace Verve.ObjectPool
{
    using System;
    using System.Collections.Generic;
    
    
    public interface IObjectPool<T>
    {
        public T Get();
        bool TryGet(out T element);
        public void Release(T element);
        void Clear();
    }
    
    
    /// <summary>
    /// 对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Serializable]
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly Queue<T> m_Pool = new Queue<T>();
        private readonly int m_MaxCapacity;

        public int ActiveCount => m_Pool.Count;

        private readonly Func<T> m_OnCreateObject;
        private readonly Action<T> m_OnGetFromPool;
        private readonly Action<T> m_OnReleaseToPool;
        private readonly Action<T> m_OnDestroyObject;

        public ObjectPool(Func<T> onCreateObject, Action<T> onGetFromPool, Action<T> onReleaseToPool, Action<T> onDestroyObject, int initSize = 5, int maxCapacity = 20)
        {
            m_Pool = new Queue<T>();
            m_OnCreateObject = onCreateObject ?? throw new ArgumentNullException(nameof(onCreateObject));
            m_OnGetFromPool = onGetFromPool;
            m_OnReleaseToPool = onReleaseToPool;
            m_OnDestroyObject = onDestroyObject;
            m_MaxCapacity = maxCapacity > 0 ? maxCapacity : throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Capacity cannot be negative.");

            for (int i = 0; i < Math.Min(initSize, m_MaxCapacity); i++)
            {
                var obj = m_OnCreateObject.Invoke();
                m_Pool.Enqueue(obj);
                m_OnReleaseToPool?.Invoke(obj);
            }
        }

        public T Get()
        {
            T obj = null;
            if (m_Pool.Count > 0)
            {
                obj = m_Pool.Dequeue();
            }
            else if (ActiveCount < m_MaxCapacity)
            {
                obj = m_OnCreateObject?.Invoke();
            }
            if (obj != null)
            {
                m_OnGetFromPool?.Invoke(obj);
            }
            return obj;
        }
        
        public bool TryGet(out T element)
        {
            element = Get();
            return element != null;
        }
        
        public void Release(T element)
        {
            if (element == null || m_Pool.Contains(element))
            {
                return;
            }
            if (m_Pool.Count < m_MaxCapacity)
            {
                m_OnReleaseToPool?.Invoke(element);
                m_Pool.Enqueue(element);
            }
            else
            {
                m_OnDestroyObject?.Invoke(element);
            }
        }
        
        public void Clear()
        {
            foreach (var obj in m_Pool)
            {
                m_OnDestroyObject?.Invoke(obj);
            }
            m_Pool.Clear();
        }
    }
}