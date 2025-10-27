namespace Verve.MVC
{
    /// <summary>
    ///  <para>活动接口</para>
    ///  <para>用于管理MVC</para>
    /// </summary>
    public interface IActivity
    {
        /// <summary>
        ///  <para>注册Model</para>
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        void AddModel<TModel>() where TModel : class, IModel, new();
        /// <summary>
        ///  <para>注册Command</para>
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <returns></returns>
        void AddCommand<TCommand>() where TCommand : class, ICommand, new();
        /// <summary>
        ///  <para>获取已经注册的Model</para>
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        TModel GetModel<TModel>() where TModel : class, IModel, new();
        /// <summary>
        ///  <para>执行命令</para>
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        void ExecuteCommand<TCommand>() where TCommand : class, ICommand, new();
        /// <summary>
        ///  <para>撤回命令</para>
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        void UndoCommand<TCommand>() where TCommand : class, ICommand, new();
    }
}