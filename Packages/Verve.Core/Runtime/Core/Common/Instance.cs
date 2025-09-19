namespace Verve
{
    using System;
    using System.Threading;

    
    /// <summary>
    /// 单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class InstanceBase<T> where T : class, new()
    {
        public static T Instance => s_Lazy.Value;

        private static readonly Lazy<T> s_Lazy = new Lazy<T>(() =>
        {
            var instance = new T();
            (instance as InstanceBase<T>)?.OnInitialized();
            return instance;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        

        protected InstanceBase() { }
        
        /// <summary>
        /// 初始化（单例首次被创建后调用）
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}