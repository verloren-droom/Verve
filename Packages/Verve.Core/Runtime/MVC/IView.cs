namespace Verve.MVC
{
    using System;
    
    
    /// <summary>
    /// MVC视图接口
    /// </summary>
    public interface IView : IBelongToActivity
    {
        /// <summary>
        /// 视图名
        /// </summary>
        string ViewName { get; }
        /// <summary>
        /// 视图打开事件
        /// </summary>
        event Action<IView> OnOpened;
        /// <summary>
        /// 视图关闭事件
        /// </summary>
        event Action<IView> OnClosed;
        /// <summary>
        /// 打开视图
        /// </summary>
        void Open(params object[] args);
        /// <summary>
        /// 关闭视图
        /// </summary>
        void Close();
    }
}