namespace Verve.MVC
{
    public interface IActivity
    {
        void RegisterModel<TModel>() where TModel : class, IModel, new();
        TModel GetModel<TModel>() where TModel : class, IModel, new();
        void ExecuteCommand<TCommand>() where TCommand : class, ICommand, new();
        void UndoCommand<TCommand>() where TCommand : class, ICommand, new();
        void Deinitialize();
    }
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ActivityBase<T> : InstanceBase<T>, IActivity where T : class, new()
    {
        private IOCContainer m_Container = new IOCContainer();

        public void RegisterModel<TModel>() where TModel : class, IModel, new() => m_Container.Register(new TModel());

        public TModel GetModel<TModel>() where TModel : class, IModel, new()
        {
            return m_Container.TryResolve<TModel>(out TModel model) ? model : null;
        }

        public virtual void Deinitialize() { }

        public virtual void ExecuteCommand<TCommand>() where TCommand : class, ICommand, new()
        {
            if (!m_Container.TryResolve<TCommand>(out _))
            {
                m_Container.Register(new TCommand());
            }
            m_Container.Resolve<TCommand>()?.Execute();
        }
        
        public virtual void UndoCommand<TCommand>() where TCommand : class, ICommand, new()
        {
            if (!m_Container.TryResolve<TCommand>(out _))
            {
                m_Container.Register(new TCommand());
            }
            m_Container.Resolve<TCommand>()?.Undo();
        }
    }
}