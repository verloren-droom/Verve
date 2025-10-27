#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.MVC
{
    using Verve.MVC;

    
    /// <summary>
    /// MVC模型基类，用于存储共享数据
    /// </summary>
    [System.Serializable]
    public abstract class ModelBase : IModel
    {
        void IModel.Initialize() => OnInitialized();
        protected virtual void OnInitialized() { }
    }
}

#endif