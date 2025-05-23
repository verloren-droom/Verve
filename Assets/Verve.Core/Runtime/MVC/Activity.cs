namespace Verve.MVC
{
    public interface IActivity
    {
        /// <summary>
        /// 注册Model
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        void RegisterModel<TModel>() where TModel : class, IModel, new();

        void RegisterCommand<TCommand>() where TCommand : class, ICommand, new();
        /// <summary>
        /// 获取已经注册的Model
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        TModel GetModel<TModel>() where TModel : class, IModel, new();
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        void ExecuteCommand<TCommand>() where TCommand : class, ICommand, new();
        /// <summary>
        /// 撤回命令
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        void UndoCommand<TCommand>() where TCommand : class, ICommand, new();
    }
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ActivityBase<T> : InstanceBase<T>, IActivity where T : class, new()
    {
        private readonly IOCContainer m_Container = new IOCContainer();
        
        public void RegisterModel<TModel>() where TModel : class, IModel, new()
        {
            if (!m_Container.TryResolve<TModel>(out _))
            {
                var model = new TModel();
                m_Container.Register(model);
                model.Initialize();
            }
        }

        public TModel GetModel<TModel>() where TModel : class, IModel, new()
        {
            return m_Container.TryResolve<TModel>(out TModel model) ? model : null;
        }
        
        public void RegisterCommand<TCommand>() where TCommand : class, ICommand, new()
        {
            if (!m_Container.TryResolve<TCommand>(out _))
            {
                var command = new TCommand
                {
                    Activity = this
                };
                m_Container.Register(command);
            }
        }
        
        public virtual void ExecuteCommand<TCommand>() where TCommand : class, ICommand, new()
        {
            if (!m_Container.TryResolve<TCommand>(out _))
            {
                m_Container.Register(new TCommand());
            }

            var command = m_Container.Resolve<TCommand>();
            command.Activity = this;
            command.Execute();
        }

        public virtual void UndoCommand<TCommand>() where TCommand : class, ICommand, new()
        {
            if (!m_Container.TryResolve<TCommand>(out _))
            {
                m_Container.Register(new TCommand());
            }

            var command = m_Container.Resolve<TCommand>();
            command.Activity = this;
            command.Undo();
        }
    }
}