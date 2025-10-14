namespace Verve
{
    using System.Collections.Generic;
    

    /// <summary>
    /// 游戏功能模块配置文件接口 - 管理所有游戏功能模块的注册和注销
    /// </summary>
    public interface IGameFeatureModuleProfile
    {
        /// <summary> 所有游戏功能模块 </summary>
        IReadOnlyCollection<IGameFeatureModule> Modules { get; }
    }
}