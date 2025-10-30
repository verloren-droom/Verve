namespace Verve
{
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>游戏功能组件接口</para>
    ///   <para>用于管理游戏功能参数，仅用于参数声明供功能子模块使用</para>
    /// </summary>
    public interface IGameFeatureComponent
    {
        /// <summary>
        ///   <para>所有功能参数</para>
        /// </summary>
        IReadOnlyCollection<IGameFeatureParameter> Parameters { get; }
    }
}