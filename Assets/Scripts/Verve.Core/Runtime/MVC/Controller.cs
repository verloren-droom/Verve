namespace Verve.MVC
{
    /// <summary>
    /// MVC控制接口
    /// </summary>
    public interface IController
    {
        void Initialize();
        void DeInitialize();
    }
    
    
    /// <summary>
    /// MVC控制基类
    /// </summary>
    public abstract class ControllerBase : IController
    {
        public abstract void Initialize();
        public virtual void DeInitialize() { }
    }
}