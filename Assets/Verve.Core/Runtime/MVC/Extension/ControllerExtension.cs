namespace Verve.MVC
{
    /// <summary>
    /// MVC控制扩展类
    /// </summary>
    public static class ControllerExtension
    {
        public static T GetModel<T>(this IController self) where T : class, IModel, new() =>
            self.Activity?.GetModel<T>();

        public static void ExecuteCommand<TCommand>(this IController self) where TCommand : class, ICommand, new() => self.Activity?.ExecuteCommand<TCommand>();
        public static void UndoCommand<TCommand>(this IController self) where TCommand : class, ICommand, new() => self.Activity?.UndoCommand<TCommand>();
    }
}