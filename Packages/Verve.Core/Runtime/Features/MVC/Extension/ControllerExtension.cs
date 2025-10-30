namespace Verve.MVC
{
    /// <summary>
    ///   <para>MVC控制扩展类</para>
    /// </summary>
    public static class ControllerExtension
    {
        /// <summary>
        ///   <para>获取模型</para>
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        /// <returns>
        ///   <para>模型实例</para>
        /// </returns>
        public static T GetModel<T>(this IController self)
            where T : class, IModel, new()
            => self.GetActivity()?.GetModel<T>();

        /// <summary>
        ///   <para>执行命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        public static void ExecuteCommand<TCommand>(this IController self)
            where TCommand : class, ICommand, new()
            => self.GetActivity()?.ExecuteCommand<TCommand>();

        /// <summary>
        ///   <para>撤销命令</para>
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        public static void UndoCommand<TCommand>(this IController self)
            where TCommand : class, ICommand, new()
            => self.GetActivity()?.UndoCommand<TCommand>();
    }
}