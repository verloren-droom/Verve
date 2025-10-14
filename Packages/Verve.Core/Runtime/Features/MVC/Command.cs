namespace Verve.MVC
{
    /// <summary>
    /// MVC命令接口
    /// </summary>
    public interface ICommand : IBelongToActivity
    {
        /// <summary>
        /// 执行
        /// </summary>
        void Execute();
        /// <summary>
        /// 回退
        /// </summary>
        void Undo();
    }
    
    
    /// <summary>
    /// MVC命令基类
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        void ICommand.Execute() => OnExecute();
        void ICommand.Undo() => OnUndo();
        
        public IActivity Activity { get; set; }
        

        protected abstract void OnExecute();
        protected virtual void OnUndo() {}
    }
}