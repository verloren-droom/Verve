namespace Verve.MVC
{
    /// <summary>
    /// MVC模型接口，用于共享数据存储
    /// </summary>
    public interface IModel
    {
        void Initialize();
        void DeInitialize();
    }
    
    /// <summary>
    /// MVC模型基类，用于共享数据存储
    /// </summary>
    [System.Serializable]
    public abstract class ModelBase : IModel
    {
        public abstract void Initialize();
        public virtual void DeInitialize() { }
    }
}