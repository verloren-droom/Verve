namespace Verve.Pool
{
    using System;
    using System.Collections.Generic;
    
    
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
}