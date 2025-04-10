namespace Verve.MVC
{
    /// <summary>
    /// MVC控制接口
    /// </summary>
    public interface IController : IBelongToActivity
    {
        /// <summary>
        /// 反初始化
        /// </summary>
        void Deinitialize();
    }
}