namespace Verve.MVC
{
    public interface IModel : ILifecycleHandler { }
    
    [System.Serializable]
    public abstract class ModelBase : IModel
    {
        public abstract void Initialize();
        public virtual void Dispose() { }
    }
}