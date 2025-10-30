namespace Verve
{
    /// <summary>
    ///   <para>游戏功能子模块接口</para>
    ///   <para>用于功能具体逻辑的实现</para>
    /// </summary>
    public interface IGameFeatureSubmodule : System.IDisposable
    {
        /// <summary>
        ///   <para>获取或设置子模块的启用状态</para>
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        ///   <para>启动子模块</para>
        /// </summary>
        void Startup();
        
        /// <summary>
        ///   <para>关闭子模块</para>
        /// </summary>
        void Shutdown();
    }
}