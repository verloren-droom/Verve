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
}