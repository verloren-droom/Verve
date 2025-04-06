namespace Verve.MVC
{
    public interface IController : ILifecycleHandler
    {
        void ExecuteCommand<TCommand>() where TCommand : ICommand, new();
    }
    
    public abstract class ControllerBase : IController
    {
        public abstract void Initialize();
        public virtual void ExecuteCommand<TCommand>() where TCommand : ICommand, new() { }
        public virtual void Dispose() { }
    }
}