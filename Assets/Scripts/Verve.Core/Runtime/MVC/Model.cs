namespace Verve.MVC
{
    /// <summary>
    /// MVC模型接口，用于共享数据存储
    /// </summary>
    public interface IModel
    {
        void Deinitialize();
    }
    
    
    /// <summary>
    /// MVC模型基类，用于共享数据存储
    /// </summary>
    [System.Serializable]
    public abstract class ModelBase : IModel
    {
        public virtual void Deinitialize() { }
    }
}