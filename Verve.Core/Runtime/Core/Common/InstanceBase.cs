namespace Verve
{
    using System;
    using System.Threading;

    
    /// <summary>
    ///   <para>单例基类</para>
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class InstanceBase<T> where T : class, new()
    {
        /// <summary>
        ///   <para>单例实例</para>
        /// </summary>
        public static T Instance => s_Lazy.Value;

        private static readonly Lazy<T> s_Lazy = new Lazy<T>(() =>
        {
            var instance = new T();
            (instance as InstanceBase<T>)?.OnInitialized();
            return instance;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        

        protected InstanceBase() { }
        
        /// <summary>
        ///   <para>单例初始化</para>
        ///   <para>仅在单例首次被创建后调用</para>
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}