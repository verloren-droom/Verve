namespace Verve
{
    using System.Collections.Generic;


    /// <summary>
    ///   <para>游戏功能组件配置文件接口</para>
    ///   <para>用于管理游戏功能组件，提供添加、移除、获取功能组件等</para>
    /// </summary>
    public interface IGameFeatureComponentProfile
    {
        /// <summary>
        ///   <para>所有功能组件</para>
        /// </summary>
        IReadOnlyCollection<IGameFeatureComponent> Components { get; }
    }
}
