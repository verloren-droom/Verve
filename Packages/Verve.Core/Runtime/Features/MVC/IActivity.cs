namespace Verve.MVC
{
    /// <summary>
    ///   <para>活动接口</para>
    ///   <para>用于管理MVC</para>
    /// </summary>
    public interface IActivity
    {
        /// <summary>
        ///   <para>注册Model</para>
        /// </summary>
        /// <typeparam name="TModel">模型类型</typeparam>
        void AddModel<TModel>() where TModel : class, IModel, new();
        
        /// <summary>
        ///   <para>注册Command</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <returns>
        ///   <para>命令实例</para>
        /// </returns>
        void AddCommand<TCommand>() where TCommand : class, ICommand, new();
        
        /// <summary>
        ///   <para>获取已经注册的Model</para>
        /// </summary>
        /// <typeparam name="TModel">模型类型</typeparam>
        /// <returns>
        ///   <para>模型实例</para>
        /// </returns>
        TModel GetModel<TModel>() where TModel : class, IModel, new();
        
        /// <summary>
        ///   <para>执行命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        void ExecuteCommand<TCommand>() where TCommand : class, ICommand, new();
        
        /// <summary>
        ///   <para>撤回命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        void UndoCommand<TCommand>() where TCommand : class, ICommand, new();
    }
}