namespace Verve
{
    using System.Collections.Generic;

    
    /// <summary>
    /// 游戏功能模块接口 - 用于管理创建功能子模块，仅负责创建或注册子模块
    /// </summary>
    public interface IGameFeatureModule
    {
        /// <summary> 获取或设置模块的激活状态 </summary>
        bool IsActive { get; set; }
        /// <summary> 功能子模块 </summary>
        IReadOnlyCollection<IGameFeatureSubmodule> Submodules { get; }
    }
}