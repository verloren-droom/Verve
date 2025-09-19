namespace Verve
{
    /// <summary>
    /// 游戏功能子模块组件接口 - 用于绑定具体的游戏功能组件
    /// </summary>
    public interface IComponentGameFeatureSubmodule : IGameFeatureSubmodule
    {
        /// <summary> 组件类型 </summary>
        System.Type ComponentType { get; }
        /// <summary> 获取组件 </summary>
        IGameFeatureComponent GetComponent();
        /// <summary> 设置组件 </summary>
        void SetComponent(IGameFeatureComponent component);
    }
}