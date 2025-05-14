namespace Verve.Loader
{
    
    /// <summary>
    /// 资源加载回调上下文
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct AssetLoaderCallbackContext<T>
    {
        public T Result { get; }
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