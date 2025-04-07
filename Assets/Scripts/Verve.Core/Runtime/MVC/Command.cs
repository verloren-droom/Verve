namespace Verve.MVC
{
    /// <summary>
    /// MVC命令接口
    /// </summary>
    public interface ICommand
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
        public abstract void Execute();
        public virtual void Undo() { }
    }
}