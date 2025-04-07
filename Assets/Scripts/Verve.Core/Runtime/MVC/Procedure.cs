namespace Verve.MVC
{
    public interface IProcedure
    {
        
    }
    
    public abstract class Procedure<T> : InstanceBase<T>, IProcedure where T : class, new()
    {
        private IOCContainer m_IocContainer = new IOCContainer();
        
        protected void RegisterModel<TModel>(TModel model) where TModel : class, IModel
        {
            model.Initialize();
            m_IocContainer.Register(model);
        }
        
        protected void RegisterModel<TModel>() where TModel : class, IModel, new() => RegisterModel<TModel>(new TModel());

        public TModel GetModel<TModel>() where TModel : class, IModel, new()
        {
            return m_IocContainer.TryResolve<TModel>(out TModel model) ? model : null;
        }

        protected virtual void ExecuteCommand<TCommand>() where TCommand : class, ICommand, new()
        {
            if (!m_IocContainer.TryResolve<TCommand>(out _))
            {
                m_IocContainer.Register(new TCommand());
            }
            m_IocContainer.Resolve<TCommand>()?.Execute();
        }

        protected virtual void UndoCommand<TCommand>() where TCommand : class, ICommand, new()
        {
            if (!m_IocContainer.TryResolve<TCommand>(out _))
            {
                m_IocContainer.Register(new TCommand());
            }
            m_IocContainer.Resolve<TCommand>()?.Undo();
        }
    }
}