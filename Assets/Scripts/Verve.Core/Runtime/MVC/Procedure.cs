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
            m_IocContainer.Register(model);
        }
        
        protected void RegisterModel<TModel>() where TModel : class, IModel, new() => RegisterModel<TModel>(new TModel());

        public bool TryGetModel<TModel>(out TModel model) where TModel : class, IModel, new()
        {
            return m_IocContainer.TryResolve<TModel>(out model);
        }
        
        protected virtual void ExecuteCommand(ICommand command)
        {
            
        }
    }
}