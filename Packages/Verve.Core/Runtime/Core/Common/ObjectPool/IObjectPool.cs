namespace Verve
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>对象池接口</para>
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IObjectPool<T>
    {
        /// <summary>
        ///   <para>对象池当前数量</para>
        /// </summary>
        public int Count { get; }
        
        /// <summary>
        ///   <para>对象池容量</para>
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        ///   <para>从对象池中取出对象</para>
        /// </summary>
        /// <param name="predicate">筛选条件</param>
        /// <returns>
        ///   <para>对象实例</para>
        /// </returns>
        public T Get(Predicate<T> predicate = null);
        
        /// <summary>
        ///   <para>尝试从对象池中取出对象</para>
        /// </summary>
        /// <param name="element">对象实例</param>
        /// <param name="predicate">筛选条件</param>
        /// <returns>
        ///   <para>是否成功</para>
        /// </returns>
        bool TryGet(out T element, Predicate<T> predicate = null);
        
        /// <summary>
        ///   <para>释放对象并放入池内</para>
        /// </summary>
        /// <param name="element">对象实例</param>
        public void Release(T element);
        
        /// <summary>
        ///   <para>批量释放对象到池内</para>
        /// </summary>
        /// <param name="elements">对象实例列表</param>
        public void ReleaseRange(IEnumerable<T> elements);
        
        /// <summary>
        ///   <para>清空对象池</para>
        /// </summary>
        void Clear();
    }
}