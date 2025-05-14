namespace Verve.MVC
{
    /// <summary>
    /// MVC模型接口，用于存储共享数据
    /// </summary>
    public interface IModel
    {
        void Initialize();
    }
    
    
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