namespace Verve
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>对象池接口。</para>
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    public interface IObjectPool<T> : IDisposable
    {
        /// <summary>
        ///   <para>闲置对象数量。</para>
        /// </summary>
        public int Count { get; }
        
        /// <summary>
        ///   <para>闲置对象容量上限。</para>
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        ///   <para>借出对象；无筛选条件且池为空时创建新对象。</para>
        /// </summary>
        /// <param name="predicate">筛选条件。</param>
        /// <returns>
        ///   <para>对象实例</para>
        /// </returns>
        public T Get(Predicate<T> predicate = null);
        
        /// <summary>
        ///   <para>尝试借出对象；筛选条件仅匹配闲置对象。</para>
        /// </summary>
        /// <param name="element">对象实例。</param>
        /// <param name="predicate">筛选条件。</param>
        /// <returns>
        ///   <para>是否成功</para>
        /// </returns>
        bool TryGet(out T element, Predicate<T> predicate = null);
        
        /// <summary>
        ///   <para>归还对象；池已满或已释放时销毁对象。</para>
        /// </summary>
        /// <param name="element">对象实例。</param>
        public void Release(T element);
        
        /// <summary>
        ///   <para>批量归还对象；完成清理后汇总错误。</para>
        /// </summary>
        /// <param name="elements">对象实例列表。</param>
        public void ReleaseRange(IEnumerable<T> elements);
        
        /// <summary>
        ///   <para>销毁全部闲置对象。</para>
        /// </summary>
        void Clear();
    }
}