namespace Verve.MVC
{
    /// <summary>
    ///  <para>MVC控制扩展类</para>
    /// </summary>
    public static class ControllerExtension
    {
        public static T GetModel<T>(this IController self)
            where T : class, IModel, new()
            => self.GetActivity()?.GetModel<T>();

        public static void ExecuteCommand<TCommand>(this IController self)
            where TCommand : class, ICommand, new()
            => self.GetActivity()?.ExecuteCommand<TCommand>();

        public static void UndoCommand<TCommand>(this IController self)
            where TCommand : class, ICommand, new()
            => self.GetActivity()?.UndoCommand<TCommand>();
    }
}