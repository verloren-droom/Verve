namespace Verve
{
    using System.Collections.Generic;

    
    /// <summary>
    ///   <para>游戏功能模块接口</para>
    ///   <para>用于管理创建功能子模块，仅负责创建或注册子模块</para>
    /// </summary>
    public interface IGameFeatureModule
    {
        /// <summary>
        ///   <para>获取或设置模块的激活状态</para>
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        ///   <para>所有功能子模块</para>
        /// </summary>
        IReadOnlyCollection<IGameFeatureSubmodule> Submodules { get; }
    }
}