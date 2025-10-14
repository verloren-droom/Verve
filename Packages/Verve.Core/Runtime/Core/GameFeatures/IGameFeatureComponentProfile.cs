namespace Verve
{
    using System.Collections.Generic;


    /// <summary>
    /// 游戏功能组件配置文件接口 - 用于管理游戏功能组件，提供添加、移除、获取功能组件等
    /// </summary>
    public interface IGameFeatureComponentProfile
    {
        /// <summary> 所有功能组件 </summary>
        IReadOnlyCollection<IGameFeatureComponent> Components { get; }
    }
}
