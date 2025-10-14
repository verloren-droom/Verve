namespace Verve
{
    using System.Collections.Generic;
    

    /// <summary>
    /// 游戏功能组件接口 - 用于管理游戏功能参数，仅用于参数声明供功能子模块使用
    /// </summary>
    public interface IGameFeatureComponent
    {
        /// <summary> 所有功能参数 </summary>
        IReadOnlyCollection<IGameFeatureParameter> Parameters { get; }
    }
}