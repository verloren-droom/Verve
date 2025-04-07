namespace Verve.MVC
{
    /// <summary>
    /// MVC控制扩展类
    /// </summary>
    public static class ControllerExtension
    {
        public static void ExecuteCommand<TCommand>(this IController self) where TCommand : ICommand, new()
        {
            
        }
    }
}