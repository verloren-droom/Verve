namespace Verve
{
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>游戏功能模块配置文件接口</para>
    ///   <para>用于管理所有游戏功能模块的注册和注销</para>
    /// </summary>
    public interface IGameFeatureModuleProfile
    {
        /// <summary>
        ///   <para>所有游戏功能模块</para>
        /// </summary>
        IReadOnlyCollection<IGameFeatureModule> Modules { get; }
    }
}