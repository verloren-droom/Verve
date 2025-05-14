namespace Verve.Pool
{
    
    using System;
    using System.Linq;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Concurrent;


    public interface IObjectPool<T>
    {
        /// <summary>
        /// 从对象池中取出对象
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T Get(Predicate<T> predicate = null);
        /// <summary>
        /// 尝试从对象池中取出对象
        /// </summary>
        /// <param name="element"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        bool TryGet(out T element, Predicate<T> predicate = null);
        /// <summary>
        /// 释放对象并放入池内
        /// </summary>
        /// <param name="element"></param>
        public void Release(T element);
        /// <summary>
        /// 批量释放对象到池内
        /// </summary>
        /// <param name="elements"></param>
        public void ReleaseRange(IEnumerable<T> elements);
        /// <summary>
        /// 清空对象池
        /// </summary>
        /// <param name="isDestroy">是否调用销毁</param>
        void Clear(bool isDestroy = true);
    }

    
    /// <summary>
    /// 通用对象池
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    [System.Serializable]
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private ConcurrentQueue<T> m_Pool = new ConcurrentQueue<T>();
        private int m_MaxCapacity = 0;

        private readonly Func<T> m_OnCreateObject;
        private readonly Action<T> m_OnGetFromPool;
        private readonly Action<T> m_OnReleaseToPool;
        private readonly Action<T> m_OnDestroyObject;
        
        public int MaxCapacity => m_MaxCapacity;
        public int Count => m_Pool.Count;
    
        public ObjectPool(Func<T> onCreateObject, Action<T> onGetFromPool = null, Action<T> onReleaseToPool = null, Action<T> onDestroyObject = null, int preSize = 5, int maxCapacity = 20)
        {
            m_OnCreateObject = onCreateObject ?? throw new ArgumentNullException(nameof(onCreateObject));
            m_OnGetFromPool = onGetFromPool;
            m_OnReleaseToPool = onReleaseToPool;
            m_OnDestroyObject = onDestroyObject;
            m_MaxCapacity = maxCapacity > 0 ? maxCapacity : throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Capacity cannot be negative.");
    
            for (int i = 0; i < Math.Min(Math.Max(preSize, 0), m_MaxCapacity); i++)
            {
                Release(m_OnCreateObject());
            }
        }
    
        public T Get(Predicate<T> predicate = null)
        {
            if (predicate == null)
            {
                if (m_Pool.TryDequeue(out T obj))
                {
                    m_OnGetFromPool?.Invoke(obj);
                    return obj;
                }
                return (m_Pool.Count < m_MaxCapacity) ? m_OnCreateObject() : null;
            }

            int bufferSize = Math.Min(m_Pool.Count, 256);
            T[] tempBuffer = ArrayPool<T>.Shared.Rent(bufferSize);
            int writeIndex = 0;
            bool found = false;
            T result = null;
            
            try 
            {
                while (m_Pool.TryDequeue(out var item))
                {
                    if (!found && predicate(item))
                    {
                        result = item;
                        m_OnGetFromPool?.Invoke(result);
                        found = true;
                        continue;
                    }
                
                    if (writeIndex < bufferSize)
                    {
                        tempBuffer[writeIndex++] = item;
                    }
                    else
                    {
                        m_Pool.Enqueue(item);
                    }
                }
            
                for (int i = 0; i < writeIndex; i++)
                {
                    m_Pool.Enqueue(tempBuffer[i]);
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(tempBuffer);
            }
            return result;
        }
    
        public bool TryGet(out T element, Predicate<T> predicate = null)
        {
            element = Get(predicate);
            return element != null;
        }
        
        public void Release(T element)
        {
            if (element == null) { return; }
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
        
        public void ReleaseRange(IEnumerable<T> elements)
        {
            int availableSlots = m_MaxCapacity - m_Pool.Count;
            int count = 0;
            
            foreach (var element in elements)
            {
                if (count >= availableSlots) break;
                m_Pool.Enqueue(element);
                count++;
            }
            foreach (var element in elements.Skip(count))
            {
                m_OnDestroyObject?.Invoke(element);
            }
        }
        
        public void Clear(bool isDestroy = true)
        {
            if (isDestroy)
            {
                while (m_Pool.TryDequeue(out var obj))
                {
                    try
                    {
                        m_OnDestroyObject?.Invoke(obj);
                    }
                    catch { }
                }
            }
            m_Pool.Clear();
        }
    }
    
}