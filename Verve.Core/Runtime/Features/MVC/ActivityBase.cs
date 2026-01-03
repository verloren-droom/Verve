#if UNITY_5_3_OR_NEWER

namespace Verve.MVC
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>活动接口</para>
    ///   <para>用于管理MVC，需要继承并挂载到场景中</para>
    /// </summary>
    public abstract class ActivityBase : ComponentInstanceBase<ActivityBase>, IActivity
    {
        private readonly Dictionary<Type, IModel> m_Models = new Dictionary<Type, IModel>();
        private readonly Dictionary<Type, ICommand> m_Commands = new Dictionary<Type, ICommand>();

        
        public void AddModel(Type type)
        {
            if (type == null || !typeof(IModel).IsAssignableFrom(type) || m_Models.ContainsKey(type)) return;
            m_Models.Add(type, (IModel)Activator.CreateInstance(type));
            m_Models[type].Initialize();
        }

        public void AddModel<TModel>()
            where TModel : class, IModel, new()
            => AddModel(typeof(TModel));
        
        public IModel GetModel(Type type)
        {
            if (type == null || !typeof(IModel).IsAssignableFrom(type) || !m_Models.TryGetValue(type, out var model)) return null;
            return model;
        }

        public TModel GetModel<TModel>()
            where TModel : class, IModel, new()
            => (TModel)GetModel(typeof(TModel));

        public void AddCommand(Type type)
        {
            if (type == null || !typeof(ICommand).IsAssignableFrom(type) || m_Commands.ContainsKey(type)) return;
            m_Commands.Add(type, Activator.CreateInstance(type) as ICommand);
        }
        
        public void AddCommand<TCommand>()
            where TCommand : class, ICommand, new()
            => AddCommand(typeof(TCommand));

        public void ExecuteCommand(Type type)
        {
            if (type == null || !typeof(ICommand).IsAssignableFrom(type) || !m_Commands.TryGetValue(type, out var command)) return;
            command.Execute();
        }
        
        public void ExecuteCommand<TCommand>()
            where TCommand : class, ICommand, new()
            => ExecuteCommand(typeof(TCommand));
        
        public void UndoCommand(Type type)
        {
            if (type == null || !typeof(ICommand).IsAssignableFrom(type) || !m_Commands.TryGetValue(type, out var command)) return;
            command.Undo();
        }

        public void UndoCommand<TCommand>()
            where TCommand : class, ICommand, new()
            => UndoCommand(typeof(TCommand));
    }
}

#endif