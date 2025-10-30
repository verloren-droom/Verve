#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.MVC
{
    using Verve.MVC;

    
    /// <summary>
    ///   <para>MVC模型基类</para>
    ///   <para>用于存储共享数据</para>
    /// </summary>
    [System.Serializable]
    public abstract class ModelBase : IModel
    {
        void IModel.Initialize() => OnInitialized();
        
        /// <summary>
        ///   <para>初始化</para>
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}

#endif