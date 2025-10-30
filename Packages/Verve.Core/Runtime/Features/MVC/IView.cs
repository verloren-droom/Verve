namespace Verve.MVC
{
    using System;
    
    
    /// <summary>
    ///   <para>MVC视图接口</para>
    /// </summary>
    public interface IView : IBelongToActivity
    {
        /// <summary>
        ///   <para>视图名</para>
        /// </summary>
        string ViewName { get; }
        
        /// <summary>
        ///   <para>视图打开事件</para>
        /// </summary>
        event Action<IView> OnOpened;
        
        /// <summary>
        ///   <para>视图关闭事件</para>
        /// </summary>
        event Action<IView> OnClosed;
        
        /// <summary>
        ///   <para>打开视图</para>
        /// </summary>
        /// <param name="args">参数</param>
        void Open(params object[] args);
        
        /// <summary>
        ///   <para>关闭视图</para>
        /// </summary>
        void Close();
    }
}