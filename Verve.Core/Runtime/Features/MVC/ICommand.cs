namespace Verve.MVC
{
    /// <summary>
    ///   <para>MVC命令接口</para>
    /// </summary>
    public interface ICommand : IBelongToActivity
    {
        /// <summary>
        ///   <para>执行</para>
        /// </summary>
        void Execute();
        
        /// <summary>
        ///   <para>回退</para>
        /// </summary>
        void Undo();
    }
}