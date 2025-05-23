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
        void Open();
        void Close();
        event Action<IView> OnOpened;
        event Action<IView> OnClosed;
    }
}