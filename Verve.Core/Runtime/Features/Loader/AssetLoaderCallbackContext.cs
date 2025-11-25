namespace Verve.Loader
{
    /// <summary>
    ///   <para>资源加载回调上下文</para>
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    public struct AssetLoaderCallbackContext<T>
    {
        /// <summary>
        ///   <para>结果</para>
        /// </summary>
        public T Result { get; }
        
        /// <summary>
        ///   <para>是否完成</para>
        /// </summary>
        public bool IsDone { get; }

        public AssetLoaderCallbackContext(bool isDone = false)
        {
            IsDone = isDone;
            Result = default;
        }

        public AssetLoaderCallbackContext(T result, bool isDone = true)
        {
            Result = result;
            IsDone = isDone;
        }
    }
}