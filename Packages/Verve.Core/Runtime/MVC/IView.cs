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
        /// 打开视图
        /// </summary>
        void Open();
        /// <summary>
        /// 关闭视图
        /// </summary>
        void Close();
        /// <summary>
        /// 视图打开事件
        /// </summary>
        event Action<IView> OnOpened;
        /// <summary>
        /// 视图关闭事件
        /// </summary>
        event Action<IView> OnClosed;
    }
}